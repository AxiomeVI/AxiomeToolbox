using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.AxiomeToolbox.Hotkeys;
using Celeste.Mod.AxiomeToolbox.Integration;
using SrtStateManager = global::Celeste.Mod.SpeedrunTool.SaveLoad.StateManager;
using SrtState = global::Celeste.Mod.SpeedrunTool.SaveLoad.State;

namespace Celeste.Mod.AxiomeToolbox.Timeline;

/// <summary>
/// Tracks gameplay events and maintains the data consumed by TimelineLayout and TimelineOverlay.
///
/// DESIGN: We hook Engine.Update instead of Level.Update because Level.Update is skipped
/// entirely by Monocle when Engine.FreezeTimer > 0 (i.e. during freeze frames). Engine.Update
/// fires every tick regardless, so it is the only reliable place to count frames and detect
/// freezes.
/// </summary>
public static class TimelineTracker {
    internal static int WindowFrames => AxiomeToolboxModule.Settings.TimelineWindowSize;

    // WHY: RetentionFrames bounds memory while keeping extra history for inspect-mode scrollback.
    internal const int RetentionFrames = 600;

    // Set by the overlay when inspect mode is active. CullAll() uses this
    // to protect the inspected window from being culled.
    internal static int InspectNow { get; set; } = -1;

    // A closed span: [Start, End) in timeline frame numbers.
    internal readonly record struct FrameSpan(int Start, int End);

    // ── SpanTracker ────────────────────────────────────────────────────────
    // DESIGN: Encapsulates the repeating (wasActive, startFrame, spans, openStart)
    // edge-detection pattern shared by freeze, pause, coyote, halfGrav, and jumpHeld.
    // Mutable struct — must be stored as a static field, never copied to a local variable.
    internal struct SpanTracker {
        private bool _wasActive;
        private int  _startFrame;
        private readonly List<FrameSpan> _spans;

        public SpanTracker(int capacity = 8) {
            _wasActive = false; _startFrame = 0; _spans = new(capacity);
        }

        public readonly IReadOnlyList<FrameSpan> Spans => _spans;
        public readonly int OpenStart => _wasActive ? _startFrame : -1;

        public void Update(bool active, int frame) {
            if (active && !_wasActive)       _startFrame = frame;
            else if (!active && _wasActive)  _spans.Add(new FrameSpan(_startFrame, frame));
            _wasActive = active;
        }

        public readonly void Cull(Predicate<FrameSpan> pred) {
            if (_spans.Count > 0) _spans.RemoveAll(pred);
        }

        public readonly void ReplaceSpans(List<FrameSpan> spans) {
            _spans.Clear();
            _spans.AddRange(spans);
        }

        public readonly void SaveState(Dictionary<string, object> d, string name) {
            d[$"{name}.was"]   = _wasActive;
            d[$"{name}.start"] = _startFrame;
        }

        public void LoadState(Dictionary<string, object> d, string name) {
            _wasActive  = (bool)d[$"{name}.was"];
            _startFrame = (int)d[$"{name}.start"];
        }
    }

    // ── MarkTracker ────────────────────────────────────────────────────────
    // DESIGN: Encapsulates the (wasPressed, marks) rising-edge detection pattern.
    // Mutable struct — same storage constraints as SpanTracker.
    internal struct MarkTracker {
        private bool _wasPressed;
        private readonly List<int> _marks;

        public MarkTracker(int capacity = 8) {
            _wasPressed = false; _marks = new(capacity);
        }

        public readonly IReadOnlyList<int> Marks => _marks;

        public void Update(bool pressed, int frame) {
            if (pressed && !_wasPressed) _marks.Add(frame);
            _wasPressed = pressed;
        }

        public readonly void Cull(Predicate<int> pred) {
            if (_marks.Count > 0) _marks.RemoveAll(pred);
        }

        public readonly void ReplaceMarks(List<int> marks) {
            _marks.Clear();
            _marks.AddRange(marks);
        }

        public readonly void SaveState(Dictionary<string, object> d, string name) {
            d[$"{name}.was"] = _wasPressed;
        }

        public void LoadState(Dictionary<string, object> d, string name) {
            _wasPressed = (bool)d[$"{name}.was"];
        }
    }

    // Monotonic frame counter. Increments once per non-paused Engine.Update tick,
    // including ticks that occur during freeze frames.
    internal static int Frame { get; private set; }

    public static TimelineOverlay Overlay { get; internal set; }

    // ── Span trackers ──────────────────────────────────────────────────────
    private static SpanTracker _freeze   = new(16);
    private static SpanTracker _pause    = new(4);
    private static SpanTracker _coyote   = new(8);
    private static SpanTracker _halfGrav = new(8);
    private static SpanTracker _jumpHeld = new(8);

    internal static IReadOnlyList<FrameSpan> FreezeSpans   => _freeze.Spans;
    internal static IReadOnlyList<FrameSpan> PauseSpans    => _pause.Spans;
    internal static IReadOnlyList<FrameSpan> CoyoteSpans   => _coyote.Spans;
    internal static IReadOnlyList<FrameSpan> HalfGravSpans => _halfGrav.Spans;
    internal static IReadOnlyList<FrameSpan> JumpHeldSpans => _jumpHeld.Spans;

    internal static int OpenFreezeStart   => _freeze.OpenStart;
    internal static int OpenPauseStart    => _pause.OpenStart;
    internal static int OpenCoyoteStart   => _coyote.OpenStart;
    internal static int OpenHalfGravStart => _halfGrav.OpenStart;
    internal static int OpenJumpHeldStart => _jumpHeld.OpenStart;

    // ── Mark trackers ──────────────────────────────────────────────────────
    private static MarkTracker _dashInput = new(8);
    private static MarkTracker _jumpPress = new(8);

    internal static IReadOnlyList<int> DashInputMarks => _dashInput.Marks;
    internal static IReadOnlyList<int> JumpPressMarks => _jumpPress.Marks;

    // ── Hook-populated marks ───────────────────────────────────────────────
    private static readonly List<int> _jumpFiredMarks  = [];
    private static readonly List<int> _superFiredMarks = [];
    internal static IReadOnlyList<int> JumpFiredMarks  => _jumpFiredMarks;
    internal static IReadOnlyList<int> SuperFiredMarks => _superFiredMarks;

    // ── Transition marks ──────────────────────────────────────────────────
    private static readonly List<int> _transitionMarks = [];
    internal static IReadOnlyList<int> TransitionMarks => _transitionMarks;
    private static bool _wasTransitioning;

    // ── Cutscene marks ────────────────────────────────────────────────────
    private static readonly List<int> _cutsceneMarks = [];
    internal static IReadOnlyList<int> CutsceneMarks => _cutsceneMarks;

    // ── Dash tracking ───────────────────────────────────────────────────────
    internal readonly record struct DashSpan(int Start, int End, int ExtStart, int ExtEnd);

    private static readonly List<DashSpan> _dashSpans = [];
    internal static IReadOnlyList<DashSpan> DashSpans => _dashSpans;

    private static bool    _wasDashing;
    private static int     _dashStart;
    private static Vector2 _dashDir;
    private static int     _dashGameplayFrames;
    private static int     _dashExtAbsStart;
    private static int     _dashExtAbsEnd;

    internal static int  OpenDashStart        => _wasDashing ? _dashStart : -1;
    internal static int  OpenDashExtStart     => _wasDashing ? _dashExtAbsStart : -1;
    internal static int  OpenDashExtEnd       => _wasDashing ? _dashExtAbsEnd : -1;
    internal static bool OpenDashHasExtension => _wasDashing && _dashDir != Vector2.Zero && HasExtensionFrames(_dashDir);

    private static bool HasExtensionFrames(Vector2 dir) => dir.Y == 0f || (dir.Y > 0f && dir.X != 0f);

    // ── Culling ──────────────────────────────────────────────────────────────
    private static int _cullBefore;
    private static readonly Predicate<FrameSpan> _cullSpan = s => s.End < _cullBefore;
    private static readonly Predicate<DashSpan>  _cullDash = s => s.End < _cullBefore;
    private static readonly Predicate<int>       _cullMark = f => f < _cullBefore;

    private static void CullList<T>(List<T> list, Predicate<T> pred) {
        if (list.Count > 0) list.RemoveAll(pred);
    }

    private static void ReplaceList<T>(List<T> list, List<T> source) {
        list.Clear();
        list.AddRange(source);
    }

    private static void CullAll() {
        // WHY: Two-tier culling. Normally retain RetentionFrames of data.
        // When the user is inspecting (InspectNow >= 0), also protect the viewed window.
        int retentionCull = Frame - RetentionFrames;
        _cullBefore = InspectNow >= 0
            ? Math.Min(retentionCull, InspectNow - WindowFrames)
            : retentionCull;
        _freeze.Cull(_cullSpan);
        _pause.Cull(_cullSpan);
        _coyote.Cull(_cullSpan);
        _halfGrav.Cull(_cullSpan);
        _jumpHeld.Cull(_cullSpan);
        _dashInput.Cull(_cullMark);
        _jumpPress.Cull(_cullMark);
        CullList(_dashSpans, _cullDash);
        CullList(_transitionMarks, _cullMark);
        CullList(_cutsceneMarks, _cullMark);
        CullList(_jumpFiredMarks, _cullMark);
        CullList(_superFiredMarks, _cullMark);
    }

    // =========================================================================
    // Load / Unload / Reset
    // =========================================================================

    private static object _srtSaveLoadAction;
    private static Dictionary<string, object> _pendingVisualSnapshot;
    private static bool _srtWasBusy;

    private const string SnapFrame           = "snap.frame";
    private const string SnapFreezeSpans     = "snap.freeze";
    private const string SnapPauseSpans      = "snap.pause";
    private const string SnapCoyoteSpans     = "snap.coyote";
    private const string SnapHalfGravSpans   = "snap.halfGrav";
    private const string SnapJumpHeldSpans   = "snap.jumpHeld";
    private const string SnapDashSpans       = "snap.dash";
    private const string SnapDashInputMarks  = "snap.dashInput";
    private const string SnapJumpPressMarks  = "snap.jumpPress";
    private const string SnapJumpFiredMarks  = "snap.jumpFired";
    private const string SnapSuperFiredMarks = "snap.superFired";
    private const string SnapTransitionMarks = "snap.transition";
    private const string SnapCutsceneMarks   = "snap.cutscene";

    public static void Load() {
        On.Monocle.Engine.Update         += OnEngineUpdate;
        On.Celeste.Level.StartCutscene   += OnLevelStartCutscene;
        On.Celeste.Player.Jump           += OnPlayerJump;
        On.Celeste.Player.WallJump       += OnPlayerWallJump;
        On.Celeste.Player.SuperJump      += OnPlayerSuperJump;
        On.Celeste.Player.SuperWallJump  += OnPlayerSuperWallJump;
        On.Celeste.Player.StartDash       += OnPlayerStartDash;
        Everest.Events.Level.OnLoadLevel += OnLoadLevel;
        Everest.Events.Level.OnExit      += OnLevelExit;
        // WHY: SaveLoadIntegration is already ModInterop'd in AxiomeToolboxModule.Load()
        // before TimelineTracker.Load() is called — safe to use directly here.
        _srtSaveLoadAction = SaveLoadIntegration.RegisterSaveLoadAction(
            OnSrtSaveState, OnSrtLoadState, null, null, null, null);
    }

    public static void Unload() {
        On.Monocle.Engine.Update         -= OnEngineUpdate;
        On.Celeste.Level.StartCutscene   -= OnLevelStartCutscene;
        On.Celeste.Player.Jump           -= OnPlayerJump;
        On.Celeste.Player.WallJump       -= OnPlayerWallJump;
        On.Celeste.Player.SuperJump      -= OnPlayerSuperJump;
        On.Celeste.Player.SuperWallJump  -= OnPlayerSuperWallJump;
        On.Celeste.Player.StartDash      -= OnPlayerStartDash;
        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
        Everest.Events.Level.OnExit      -= OnLevelExit;
        SaveLoadIntegration.Unregister?.Invoke(_srtSaveLoadAction);
        _srtSaveLoadAction = null;
        Overlay?.RemoveSelf();
        Overlay = null;
    }

    /// Clear all tracked state and remove the overlay. Called when master Enabled toggles off.
    public static void Reset() {
        _freeze   = new SpanTracker(16);
        _pause    = new SpanTracker(4);
        _coyote   = new SpanTracker(8);
        _halfGrav = new SpanTracker(8);
        _jumpHeld = new SpanTracker(8);
        _dashInput = new MarkTracker(8);
        _jumpPress = new MarkTracker(8);
        _dashSpans.Clear();
        _transitionMarks.Clear();
        _cutsceneMarks.Clear();
        _jumpFiredMarks.Clear();
        _superFiredMarks.Clear();
        Frame               = 0;
        _wasDashing         = false;
        _dashStart          = 0;
        _dashDir            = Vector2.Zero;
        _dashGameplayFrames = 0;
        _dashExtAbsStart    = -1;
        _dashExtAbsEnd      = -1;
        _wasTransitioning   = false;
        _pendingVisualSnapshot = null;
        _srtWasBusy         = false;
        InspectNow          = -1;
        Overlay?.RemoveSelf();
        Overlay = null;
    }

    internal static void OnOverlayRemoved() => Overlay = null;

    // True while SRT is in the middle of a save/load cycle.
    internal static bool SrtIsBusy => SrtStateManager.Instance.State != SrtState.None;

    // Creates, adds, and SRT-excludes the overlay. Idempotent.
    // WHY: Every add site must call IgnoreSaveState immediately after adding —
    // centralising this prevents it being forgotten.
    internal static void EnsureOverlay(Level level) {
        if (Overlay != null) return;
        Overlay = new TimelineOverlay();
        level.Add(Overlay);
        SaveLoadIntegration.IgnoreSaveState?.Invoke(Overlay, false);
    }

    // =========================================================================
    // Hooks
    // =========================================================================

    private static void OnEngineUpdate(On.Monocle.Engine.orig_Update orig,
                                        Engine self, GameTime gameTime) {
        if (!AxiomeToolboxModule.Settings.Enabled) {
            orig(self, gameTime);
            return;
        }

        // WHY: UpdateStates() must still run even when timeline is disabled —
        // checkpoint hotkeys share the same ComboHotkey frame contract.
        ComboHotkey.UpdateStates();

        // WHY: also gates Overlay?.UpdateInspect() in the srtIsBusy branch below —
        // if the timeline is disabled, inspect mode should not run either.
        if (!AxiomeToolboxModule.Settings.TimelineEnabled) {
            orig(self, gameTime);
            return;
        }

        // IMPORTANT: capture isFrozen BEFORE calling orig.
        // orig() decrements FreezeTimer, so after orig the last freeze tick shows timer == 0.
        bool isFrozen = Engine.FreezeTimer > 0f;

        // WHY: capture isDashing BEFORE orig — Player.Update() runs inside orig.
        // On the last StDash frame the player exits StDash during orig, so a post-orig
        // check returns false and fires the falling edge one frame too early.
        bool isDashingBeforeOrig =
            (Engine.Scene as Level)?.Tracker.GetEntity<Player>()?.StateMachine.State == Player.StDash;

        orig(self, gameTime);

        if (Engine.Scene is not Level level) return;

        bool srtIsBusy = SrtIsBusy;
        if (!srtIsBusy && _srtWasBusy && _pendingVisualSnapshot != null) {
            // WHY: SRT freeze window just ended. Apply the deferred visual snapshot,
            // discarding anything accumulated during the freeze. This snaps the
            // timeline back to the exact moment the save state was created.
            var d = _pendingVisualSnapshot;
            Frame = (int)d[SnapFrame];
            _freeze.ReplaceSpans((List<FrameSpan>)d[SnapFreezeSpans]);
            _pause.ReplaceSpans((List<FrameSpan>)d[SnapPauseSpans]);
            _coyote.ReplaceSpans((List<FrameSpan>)d[SnapCoyoteSpans]);
            _halfGrav.ReplaceSpans((List<FrameSpan>)d[SnapHalfGravSpans]);
            _jumpHeld.ReplaceSpans((List<FrameSpan>)d[SnapJumpHeldSpans]);
            ReplaceList(_dashSpans,        (List<DashSpan>)d[SnapDashSpans]);
            _dashInput.ReplaceMarks((List<int>)d[SnapDashInputMarks]);
            _jumpPress.ReplaceMarks((List<int>)d[SnapJumpPressMarks]);
            ReplaceList(_jumpFiredMarks,   (List<int>)d[SnapJumpFiredMarks]);
            ReplaceList(_superFiredMarks,  (List<int>)d[SnapSuperFiredMarks]);
            ReplaceList(_transitionMarks,  (List<int>)d[SnapTransitionMarks]);
            ReplaceList(_cutsceneMarks,    (List<int>)d[SnapCutsceneMarks]);
            _pendingVisualSnapshot = null;
        }
        _srtWasBusy = srtIsBusy;
        if (srtIsBusy) {
            Overlay?.UpdateInspect();
            return;
        }

        // WHY: Pause tracking during inspect mode so the view stays stable.
        // Side-effect: events during inspection are unrecorded; edge-detection state
        // stays frozen — the first post-inspection frame may fire one catch-up edge.
        if (InspectNow >= 0) return;

        _dashInput.Update(Input.Dash.Pressed || Input.CrouchDash.Pressed, Frame);
        _jumpPress.Update(Input.Jump.Pressed, Frame);

        // Pause span: covers both the pause-menu ticks and the wasPaused transition tick.
        _pause.Update(level.Paused || level.wasPaused, Frame);

        _freeze.Update(isFrozen, Frame);

        bool nowTransitioning = level.Transitioning;

        var player = level.Tracker.GetEntity<Player>();
        bool isDashing = isDashingBeforeOrig ||
                         player?.StateMachine.State == Player.StDash;

        if (isDashing && !_wasDashing) {
            _dashStart = Frame;
            _dashDir = Vector2.Zero;
            _dashGameplayFrames = 0;
            _dashExtAbsStart = -1;
            _dashExtAbsEnd = -1;
        }
        if (isDashing) {
            if (_dashDir == Vector2.Zero && player.DashDir != Vector2.Zero)
                _dashDir = player.DashDir;

            if (!level.Paused && !level.wasPaused && !nowTransitioning) {
                _dashGameplayFrames++;
                if (_dashGameplayFrames == TimelineConstants.DashExtStartFrame)
                    _dashExtAbsStart = Frame;
                else if (_dashGameplayFrames == TimelineConstants.DashExtStartFrame + TimelineConstants.DashExtFrameCount)
                    _dashExtAbsEnd = Frame;
            }
        }
        if (!isDashing && _wasDashing) {
            bool hasExt  = HasExtensionFrames(_dashDir);
            int  extStart = (hasExt && _dashExtAbsStart >= 0) ? _dashExtAbsStart : Frame;
            int  extEnd   = (hasExt && _dashExtAbsEnd   >= 0) ? _dashExtAbsEnd   : Frame;
            _dashSpans.Add(new DashSpan(_dashStart, Frame, extStart, extEnd));
        }
        _wasDashing = isDashing;

        _coyote.Update(player != null && player.jumpGraceTimer > 0f && !player.onGround, Frame);

        // WHY: avoids a phantom half-grav frame on certain landings (e.g. ultra).
        // OnCollideV zeros Speed.Y before onGround is updated, so for one frame
        // Speed.Y == 0 while onGround is still false. Mirror onGround's geometry
        // check to detect that case and suppress it.
        bool isHalfGravActive = player != null
                             && player.StateMachine.State == Player.StNormal
                             && !player.onGround
                             && Math.Abs(player.Speed.Y) < 40f
                             && !(player.Speed.Y == 0f
                                  && (player.CollideCheck<Solid>(player.Position + Vector2.UnitY)
                                      || player.CollideCheckOutside<JumpThru>(player.Position + Vector2.UnitY)))
                             && !player.AutoJump;
        _halfGrav.Update(isHalfGravActive, Frame);

        _jumpHeld.Update(Input.Jump.Check, Frame);

        if (nowTransitioning != _wasTransitioning)
            _transitionMarks.Add(Frame);
        _wasTransitioning = nowTransitioning;

        CullAll();

        // ── Advance frame counter (last) ──────────────────────────────────────
        // Kept at the end so all event recording above can use Frame directly without
        // Frame-1 corrections. Increments every tick including freeze and pause ticks.
        Frame++;
    }

    private static void OnLevelExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
        // WHY: Clear overlay on exit so OnLoadLevel always creates a fresh entity.
        // Without this, Overlay is non-null (Tags.Global keeps it alive) and EnsureOverlay no-ops.
        Overlay?.RemoveSelf();
        Overlay = null;
    }

    private static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
        if (!isFromLoader) return;
        if (!AxiomeToolboxModule.Settings.Enabled) return;
        if (!AxiomeToolboxModule.Settings.TimelineEnabled) return;
        // WHY: Always replace on a fresh loader — OnLevelExit doesn't fire reliably before
        // all level transitions (e.g. chapter-select jumps go straight to LevelLoader).
        Overlay?.RemoveSelf();
        Overlay = null;
        EnsureOverlay(level);
    }

    private static void OnLevelStartCutscene(
        On.Celeste.Level.orig_StartCutscene orig, Level self,
        Action<Level> onSkip, bool fadeInOnSkip, bool endingChapterAfterCutscene, bool resetZoomOnSkip) {
        orig(self, onSkip, fadeInOnSkip, endingChapterAfterCutscene, resetZoomOnSkip);
        if (Overlay != null) _cutsceneMarks.Add(Frame);
    }

    private static void OnPlayerJump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playSfx) {
        orig(self, particles, playSfx);
        if (Overlay != null) _jumpFiredMarks.Add(Frame);
    }

    private static void OnPlayerWallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir) {
        orig(self, dir);
        if (Overlay != null) _jumpFiredMarks.Add(Frame);
    }

    private static void OnPlayerSuperJump(On.Celeste.Player.orig_SuperJump orig, Player self) {
        orig(self);
        if (Overlay != null) _superFiredMarks.Add(Frame);
    }

    private static void OnPlayerSuperWallJump(On.Celeste.Player.orig_SuperWallJump orig, Player self, int dir) {
        orig(self, dir);
        if (Overlay != null) _superFiredMarks.Add(Frame);
    }

    private static int OnPlayerStartDash(On.Celeste.Player.orig_StartDash orig, Player self) {
        int result = orig(self);
        // WHY: StartDash fires for every new dash, including one that starts immediately after
        // the previous dash ends (within the same Player.Update call). In that case _wasDashing
        // is still true — the existing edge-detection never saw isDashing go false, so it would
        // merge both dashes into one span. Close the outgoing span here and open a new one.
        if (result != 0 && Overlay != null && _wasDashing) {
            bool hasExt  = HasExtensionFrames(_dashDir);
            int  extStart = (hasExt && _dashExtAbsStart >= 0) ? _dashExtAbsStart : Frame + 1;
            int  extEnd   = (hasExt && _dashExtAbsEnd   >= 0) ? _dashExtAbsEnd   : Frame + 1;
            _dashSpans.Add(new DashSpan(_dashStart, Frame + 1, extStart, extEnd));
            _dashStart          = Frame;
            _dashDir            = Vector2.Zero;
            _dashGameplayFrames = 0;
            _dashExtAbsStart    = -1;
            _dashExtAbsEnd      = -1;
        }
        return result;
    }

    // =========================================================================
    // SpeedrunTool save/load callbacks
    // =========================================================================

    private static void OnSrtSaveState(Dictionary<Type, Dictionary<string, object>> saved, Level level) {
        var d = new Dictionary<string, object>();
        _freeze.SaveState(d, "freeze");
        _pause.SaveState(d, "pause");
        _coyote.SaveState(d, "coyote");
        _halfGrav.SaveState(d, "halfGrav");
        _jumpHeld.SaveState(d, "jumpHeld");
        _dashInput.SaveState(d, "dashInput");
        _jumpPress.SaveState(d, "jumpPress");
        d[nameof(_wasDashing)]         = _wasDashing;
        d[nameof(_dashStart)]          = _dashStart;
        d[nameof(_dashDir)]            = _dashDir;
        d[nameof(_dashGameplayFrames)] = _dashGameplayFrames;
        d[nameof(_dashExtAbsStart)]    = _dashExtAbsStart;
        d[nameof(_dashExtAbsEnd)]      = _dashExtAbsEnd;
        d[nameof(_wasTransitioning)]   = _wasTransitioning;
        // Visual state — copied so the live lists can keep evolving during the freeze
        // window without corrupting the snapshot. Applied when SRT exits to None.
        d[SnapFrame]           = Frame;
        d[SnapFreezeSpans]     = new List<FrameSpan>(_freeze.Spans);
        d[SnapPauseSpans]      = new List<FrameSpan>(_pause.Spans);
        d[SnapCoyoteSpans]     = new List<FrameSpan>(_coyote.Spans);
        d[SnapHalfGravSpans]   = new List<FrameSpan>(_halfGrav.Spans);
        d[SnapJumpHeldSpans]   = new List<FrameSpan>(_jumpHeld.Spans);
        d[SnapDashSpans]       = new List<DashSpan>(_dashSpans);
        d[SnapDashInputMarks]  = new List<int>(_dashInput.Marks);
        d[SnapJumpPressMarks]  = new List<int>(_jumpPress.Marks);
        d[SnapJumpFiredMarks]  = new List<int>(_jumpFiredMarks);
        d[SnapSuperFiredMarks] = new List<int>(_superFiredMarks);
        d[SnapTransitionMarks] = new List<int>(_transitionMarks);
        d[SnapCutsceneMarks]   = new List<int>(_cutsceneMarks);
        saved[typeof(TimelineTracker)] = d;
    }

    private static void OnSrtLoadState(Dictionary<Type, Dictionary<string, object>> saved, Level level) {
        if (!saved.TryGetValue(typeof(TimelineTracker), out var d)) return;
        _freeze.LoadState(d, "freeze");
        _pause.LoadState(d, "pause");
        _coyote.LoadState(d, "coyote");
        _halfGrav.LoadState(d, "halfGrav");
        _jumpHeld.LoadState(d, "jumpHeld");
        _dashInput.LoadState(d, "dashInput");
        _jumpPress.LoadState(d, "jumpPress");
        _wasDashing         = (bool)d[nameof(_wasDashing)];
        _dashStart          = (int)d[nameof(_dashStart)];
        _dashDir            = (Vector2)d[nameof(_dashDir)];
        _dashGameplayFrames = (int)d[nameof(_dashGameplayFrames)];
        _dashExtAbsStart    = (int)d[nameof(_dashExtAbsStart)];
        _dashExtAbsEnd      = (int)d[nameof(_dashExtAbsEnd)];
        _wasTransitioning   = (bool)d[nameof(_wasTransitioning)];
        // Defer visual restore until SRT freeze window ends (State → None).
        // WHY: guard against save states created by older mod versions that lacked snap keys.
        _pendingVisualSnapshot = d.ContainsKey(SnapFrame) ? d : null;
    }
}
