using Celeste.Mod.AxiomeToolbox.Integration;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.AxiomeToolbox.SecondBlockless;

public static class SecondBlocklessDetector {

    private enum DetectorState { Idle, WaitingForDemoDash, DemoDashing, FirstJumpDone, SecondJumpFired }

    private static DetectorState _state          = DetectorState.Idle;
    private static int           _dashFrame;      // engine ticks since StartDash, incremented post-orig
    private static float         _dashX;          // player X captured before StartDash fires
    private static bool          _landed;         // true once player.onGround after first jump
    private static int           _coyoteFrame;    // coyote ticks counted post-orig after landing
    private static int           _postLandFrames; // engine ticks since landing, for timeout and press tracking
    private static int           _jumpHeldFrames; // Input.Jump.Check ticks post-SecondJumpFired
    private static int           _jumpPressPostLand = -1; // _postLandFrames when jump was pressed (-1 = not yet pressed)

    private const float ArmX         = 8182f;
    private const float MinDashX     = 8196f;
    private const int   JumpFrameMin = 14;
    private const int   JumpFrameMax = 15;
    private const int   CoyoteMin    = 2;
    private const int   CoyoteMax    = 3;
    private const int   DashTimeout     = 20;
    private const int   LandTimeout     = 60;
    private const int   JumpHeldTimeout = 30;

    public static void Load() {
        On.Monocle.Engine.Update         += OnEngineUpdate;
        On.Celeste.Player.StartDash      += OnPlayerStartDash;
        On.Celeste.Player.SuperJump      += OnPlayerSuperJump;
        On.Celeste.Player.Jump           += OnPlayerJump;
        Everest.Events.Level.OnLoadLevel += OnLoadLevel;
        Everest.Events.Level.OnExit      += OnLevelExit;
    }

    public static void Unload() {
        On.Monocle.Engine.Update         -= OnEngineUpdate;
        On.Celeste.Player.StartDash      -= OnPlayerStartDash;
        On.Celeste.Player.SuperJump      -= OnPlayerSuperJump;
        On.Celeste.Player.Jump           -= OnPlayerJump;
        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
        Everest.Events.Level.OnExit      -= OnLevelExit;
    }

    public static void Reset() {
        _state             = DetectorState.Idle;
        _dashFrame         = 0;
        _dashX             = 0f;
        _landed            = false;
        _coyoteFrame       = 0;
        _postLandFrames    = 0;
        _jumpHeldFrames    = 0;
        _jumpPressPostLand = -1;
    }

    private static void OnLoadLevel(Level level, Player.IntroTypes intro, bool fromLoader) => Reset();

    private static void OnLevelExit(Level level, LevelExit exit, LevelExit.Mode mode,
                                    Session session, HiresSnow snow) => Reset();

    private static bool IsInRoom(Level level) =>
        level.Session.Area.ID == 4 && level.Session.Level == "c-09";

    private static void OnEngineUpdate(On.Monocle.Engine.orig_Update orig,
                                       Engine self, GameTime gameTime) {
        orig(self, gameTime);

        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.DetectSecondBlockless) return;
        if (Engine.Scene is not Level level) return;

        var player = level.Tracker.GetEntity<Player>();
        if (player == null) return;

        switch (_state) {
            case DetectorState.Idle:
                if (IsInRoom(level) && player.Position.X > ArmX)
                    _state = DetectorState.WaitingForDemoDash;
                break;

            case DetectorState.WaitingForDemoDash:
                if (player.Position.X <= ArmX)
                    _state = DetectorState.Idle;
                // StartDash hook handles → DemoDashing
                break;

            case DetectorState.DemoDashing:
                _dashFrame++;

                if (_dashFrame == 1 && _dashX < MinDashX) {
                    NotificationUtils.Show(string.Format(Dialog.Get(DialogIds.TwoBLBadPositionId), (int)(MinDashX - _dashX)));
                    Reset();
                    break;
                }

                if (_dashFrame > DashTimeout)
                    Reset();
                // SuperJump hook handles → FirstJumpDone; Jump hook handles late first jump
                break;

            case DetectorState.FirstJumpDone:
                if (!_landed) {
                    if (player.onGround) {
                        _landed            = true;
                        _coyoteFrame       = 0;
                        _postLandFrames    = 0;
                        _jumpPressPostLand = -1;
                    }
                    break;
                }

                _postLandFrames++;

                if (player.jumpGraceTimer > 0f && !player.onGround) _coyoteFrame++;

                // Track when jump is first pressed (for buffered-press detection in OnPlayerJump)
                if (_jumpPressPostLand < 0 && Input.Jump.Pressed)
                    _jumpPressPostLand = _postLandFrames;

                if (_postLandFrames > LandTimeout)
                    Reset();
                // Jump hook handles → SecondJumpFired
                break;

            case DetectorState.SecondJumpFired:
                if (Input.Jump.Check) {
                    _jumpHeldFrames++;
                    if (_jumpHeldFrames > JumpHeldTimeout)
                        Reset();
                } else {
                    if (_jumpHeldFrames != 2)
                        NotificationUtils.ShowFrameLoss(DialogIds.TwoBLJumpHeldId, DialogIds.TwoBLJumpHeldPluralId, _jumpHeldFrames);
                    Reset();
                }
                break;
        }
    }

    private static int OnPlayerStartDash(On.Celeste.Player.orig_StartDash orig, Player self) {
        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.DetectSecondBlockless || _state != DetectorState.WaitingForDemoDash)
            return orig(self);

        // WHY: Capture position and onGround BEFORE orig — orig can mutate both.
        bool  wasOnGround = self.onGround;
        float capturedX   = self.Position.X;
        int   result      = orig(self);

        if (wasOnGround) return result; // strat requires an aerial crouchdash

        _dashX     = capturedX;
        _dashFrame = 0;
        _state     = DetectorState.DemoDashing;
        return result;
    }

    // SuperJump fires when jump is pressed during dash state (frames 1-15).
    // Regular Jump fires when jump is pressed after dash state ends (frame 16+).
    private static void OnPlayerSuperJump(On.Celeste.Player.orig_SuperJump orig, Player self) {
        orig(self);

        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.DetectSecondBlockless) return;
        if (_state != DetectorState.DemoDashing) return;

        // _dashFrame was last incremented at END of previous tick; +1 gives the 1-based frame number.
        int frame = _dashFrame + 1;

        if (frame >= JumpFrameMin && frame <= JumpFrameMax) {
            _landed            = false;
            _coyoteFrame       = 0;
            _postLandFrames    = 0;
            _jumpPressPostLand = -1;
            _state             = DetectorState.FirstJumpDone;
        } else if (frame < JumpFrameMin) {
            NotificationUtils.ShowFrameLoss(DialogIds.TwoBLFirstJumpEarlyId, DialogIds.TwoBLFirstJumpEarlyPluralId, JumpFrameMin - frame);
            Reset();
        }
        // SuperJump cannot fire after frame 15 — late case handled by OnPlayerJump below
    }

    private static void OnPlayerJump(On.Celeste.Player.orig_Jump orig, Player self,
                                     bool particles, bool playSfx) {
        orig(self, particles, playSfx);

        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.DetectSecondBlockless) return;

        if (_state == DetectorState.DemoDashing) {
            // Regular Jump during DemoDashing = dash state ended = first jump is late
            int frame = _dashFrame + 1;
            NotificationUtils.ShowFrameLoss(DialogIds.TwoBLFirstJumpLateId, DialogIds.TwoBLFirstJumpLatePluralId, frame - JumpFrameMax);
            Reset();
        } else if (_state == DetectorState.FirstJumpDone && _landed) {
            // _coyoteFrame is incremented post-orig; +1 gives the 1-based Timeline coyote frame number.
            int timelineFrame = _coyoteFrame + 1;

            if (timelineFrame >= CoyoteMin && timelineFrame <= CoyoteMax) {
                _jumpHeldFrames = 0;
                _state          = DetectorState.SecondJumpFired;
            } else if (timelineFrame < CoyoteMin) {
                // WHY: A buffered jump always fires on coyote frame 1, but the button may have been
                // pressed several frames earlier. Use the recorded press frame so the error reflects
                // when the player actually pressed the button, not when the engine fired the jump.
                int early;
                if (Input.Jump.Pressed) {
                    early = CoyoteMin - timelineFrame;
                } else {
                    // pressCoyoteEquiv = _jumpPressPostLand - _postLandFrames (0 or negative = before coyote)
                    early = CoyoteMin - (_jumpPressPostLand - _postLandFrames);
                }
                NotificationUtils.ShowFrameLoss(DialogIds.TwoBLSecondJumpEarlyId, DialogIds.TwoBLSecondJumpEarlyPluralId, early);
                Reset();
            } else {
                NotificationUtils.ShowFrameLoss(DialogIds.TwoBLSecondJumpLateId, DialogIds.TwoBLSecondJumpLatePluralId, timelineFrame - CoyoteMax);
                Reset();
            }
        }
    }
}
