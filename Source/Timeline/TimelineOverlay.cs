using System.Collections.Generic;
using Celeste.Mod.AxiomeToolbox.Hotkeys;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using static Celeste.Mod.AxiomeToolbox.Timeline.TimelineConstants;

namespace Celeste.Mod.AxiomeToolbox.Timeline;

[Tracked]
public class TimelineOverlay : Entity {
    // ── Inspect mode state ──────────────────────────────────────────────
    private bool _isInspecting;
    private int  _frozenAtFrame;
    private int  _scrollOffset;

    // ── Mouse tracking ──────────────────────────────────────────────────
    // WHY: Celeste's Monocle doesn't expose mouse scroll/position via MInput.
    // Use XNA Mouse.GetState() directly, same pattern as CelesteTAS.
    private int   _prevScrollValue;
    private float _mouseHudX;
    private float _mouseHudY;

    // ── Inspect hotkey ───────────────────────────────────────────────────
    private readonly ComboHotkey _inspectHotkey = new(AxiomeToolboxModule.Settings.TimelineInspect);

    // ── Highlight state ─────────────────────────────────────────────────
    private bool   _hasHighlight;
    private string _highlightLabel = "";
    private float  _hlX, _hlW, _hlY, _hlH;

    public TimelineOverlay() {
        Depth = OverlayDepth;
        // WHY: PauseUpdate/FrozenUpdate/TransitionUpdate so Update() runs during
        // pause, freeze, and room transition — inspect mode works when frames aren't advancing.
        // WHY Global not Persistent: survives full level unloads through SRT's save/load cycle.
        // EnsureOverlay() registers with SRT's IgnoreSaveState so it's never cloned.
        Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.FrozenUpdate | Tags.TransitionUpdate;
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        TimelineTracker.InspectNow = -1;
        // WHY: Don't null Overlay during SRT's IgnoreSaveState remove/re-add cycle.
        // SRT removes the entity before cloning and re-adds it after — if we null
        // the reference here, it's never restored and the overlay goes missing post-load.
        if (!TimelineTracker.SrtIsBusy)
            TimelineTracker.OnOverlayRemoved();
    }

    private static (float x, float y) ComputeBarPosition(float barW) =>
        AxiomeToolboxModule.Settings.TimelinePosition switch {
            HudCorner.TopLeft      => (BarMarginX,                   BarMarginY),
            HudCorner.TopCenter    => ((HudWidth - barW) / 2f,       BarMarginY),
            HudCorner.TopRight     => (HudWidth - BarMarginX - barW, BarMarginY),
            HudCorner.BottomLeft   => (BarMarginX,                   HudHeight - BarMarginY - BarHeight),
            HudCorner.BottomCenter => ((HudWidth - barW) / 2f,       HudHeight - BarMarginY - BarHeight),
            _                      => (HudWidth - BarMarginX - barW, HudHeight - BarMarginY - BarHeight),
        };

    private int EffectiveNow => _isInspecting
        ? _frozenAtFrame - _scrollOffset
        : TimelineTracker.Frame;

    public override void Update() {
        base.Update();

        var mouse = Mouse.GetState();
        int scrollDelta = (mouse.ScrollWheelValue - _prevScrollValue) / 120;
        _prevScrollValue = mouse.ScrollWheelValue;

        // WHY: ComboHotkey.UpdateStates() is called in TimelineTracker.OnEngineUpdate
        // (once per frame, before any entity Update() calls). Do NOT call it here.
        _inspectHotkey.Update();

        if (_inspectHotkey.IsDown && !_isInspecting) {
            _isInspecting  = true;
            _frozenAtFrame = TimelineTracker.Frame;
            _scrollOffset  = 0;
            _hasHighlight  = false;
        }

        if (!_inspectHotkey.IsDown && _isInspecting) {
            _isInspecting              = false;
            _hasHighlight              = false;
            TimelineTracker.InspectNow = -1;
            return;
        }

        if (!_isInspecting) return;

        var vp = Engine.Viewport;
        _mouseHudX = (mouse.X - vp.X) * (HudWidth  / vp.Width);
        _mouseHudY = (mouse.Y - vp.Y) * (HudHeight / vp.Height);

        float barW = TimelineTracker.WindowFrames * PixelsPerFrame;
        var (barX, barY) = ComputeBarPosition(barW);

        int maxScroll = System.Math.Max(0,
            _frozenAtFrame - (TimelineTracker.Frame - TimelineTracker.RetentionFrames + TimelineTracker.WindowFrames));
        if (scrollDelta != 0)
            _scrollOffset = System.Math.Clamp(_scrollOffset - scrollDelta * ScrollSpeed, 0, maxScroll);
        int effectiveNow = EffectiveNow;

        TimelineTracker.InspectNow = effectiveNow;

        HitTest(_mouseHudX, _mouseHudY, barX, barY, barW, effectiveNow);
    }

    private void HitTest(float hudX, float hudY, float barX, float barY, float barW, int effectiveNow) {
        if (hudX < barX || hudX >= barX + barW || hudY < barY || hudY >= barY + BarHeight) {
            _hasHighlight = false;
            return;
        }

        // WHY: ceiling division — frame k occupies pixels [barX+barW-k·PPF, barX+barW-(k-1)·PPF).
        int hoverFrame = effectiveNow - (int)System.Math.Ceiling((barX + barW - hudX) / PixelsPerFrame);
        float relY     = hudY - barY;

        bool inDashLane = relY >= LaneHeight + LaneDividerH;
        bool inJumpLane = relY < LaneHeight;

        if (inDashLane) {
            var r = FindHoveredDash(hoverFrame, effectiveNow);
            if (r.HasValue) { SetHighlight("Dash", r.Value.start, r.Value.end, barX, barW, effectiveNow,
                barY + LaneHeight + LaneDividerH + LaneMargin, LaneInnerHeight); return; }
        }

        if (inJumpLane) {
            bool jhActive = FindHoveredSpan(TimelineTracker.JumpHeldSpans, TimelineTracker.OpenJumpHeldStart, effectiveNow, hoverFrame).HasValue;
            bool coActive = FindHoveredSpan(TimelineTracker.CoyoteSpans,   TimelineTracker.OpenCoyoteStart,   effectiveNow, hoverFrame).HasValue;
            bool hgActive = FindHoveredSpan(TimelineTracker.HalfGravSpans, TimelineTracker.OpenHalfGravStart, effectiveNow, hoverFrame).HasValue;
            int activeCount = (jhActive ? 1 : 0) + (coActive ? 1 : 0) + (hgActive ? 1 : 0);

            if (activeCount > 0) {
                float stripH   = LaneInnerHeight / activeCount;
                int   stripIdx = System.Math.Clamp((int)((relY - LaneMargin) / stripH), 0, activeCount - 1);

                int slot = 0;
                foreach (var (active, name, spans, openStart) in new[] {
                    (jhActive, "Jump Held", TimelineTracker.JumpHeldSpans, TimelineTracker.OpenJumpHeldStart),
                    (coActive, "Coyote",    TimelineTracker.CoyoteSpans,   TimelineTracker.OpenCoyoteStart),
                    (hgActive, "Half Grav", TimelineTracker.HalfGravSpans, TimelineTracker.OpenHalfGravStart),
                }) {
                    if (!active) continue;
                    if (slot == stripIdx) {
                        var r = FindHoveredSpan(spans, openStart, effectiveNow, hoverFrame);
                        if (r.HasValue) {
                            SetHighlight(name, r.Value.start, r.Value.end, barX, barW, effectiveNow,
                                barY + LaneMargin + slot * stripH, stripH);
                            return;
                        }
                    }
                    slot++;
                }
            }
        }

        {
            var r = FindHoveredSpan(TimelineTracker.FreezeSpans, TimelineTracker.OpenFreezeStart, effectiveNow, hoverFrame);
            if (r.HasValue) { SetHighlight("Freeze", r.Value.start, r.Value.end, barX, barW, effectiveNow,
                barY, BarHeight); return; }

            r = FindHoveredSpan(TimelineTracker.PauseSpans, TimelineTracker.OpenPauseStart, effectiveNow, hoverFrame);
            if (r.HasValue) { SetHighlight("Pause", r.Value.start, r.Value.end, barX, barW, effectiveNow,
                barY, BarHeight); return; }
        }

        _hasHighlight = false;
    }

    private static (int start, int end)? FindHoveredSpan(
        IReadOnlyList<TimelineTracker.FrameSpan> spans, int openStart, int effectiveNow, int hoverFrame)
    {
        for (int i = 0; i < spans.Count; i++) {
            if (spans[i].Start > hoverFrame) break;
            if (hoverFrame < spans[i].End) return (spans[i].Start, spans[i].End);
        }
        if (openStart >= 0 && hoverFrame >= openStart && hoverFrame < effectiveNow)
            return (openStart, effectiveNow);
        return null;
    }

    private static (int start, int end)? FindHoveredDash(int hoverFrame, int effectiveNow) {
        var dashes = TimelineTracker.DashSpans;
        for (int i = 0; i < dashes.Count; i++) {
            if (dashes[i].Start > hoverFrame) break;
            if (dashes[i].End > hoverFrame) return (dashes[i].Start, dashes[i].End);
        }
        int os = TimelineTracker.OpenDashStart;
        if (os >= 0 && hoverFrame >= os && hoverFrame < effectiveNow)
            return (os, effectiveNow);
        return null;
    }

    private void SetHighlight(string name, int start, int end,
                               float barX, float barW, int effectiveNow,
                               float spanY, float spanH)
    {
        _hasHighlight   = true;
        _highlightLabel = $"{name} {end - start}f";
        float xStart    = FrameX(barX, effectiveNow, start);
        float xEnd      = FrameX(barX, effectiveNow, end);
        _hlX = System.Math.Max(xStart, barX);
        _hlW = System.Math.Min(xEnd, barX + barW) - _hlX;
        if (_hlW < 1f) _hlW = 1f;
        _hlY = spanY;
        _hlH = spanH;
    }

    public override void Render() {
        int   wf         = TimelineTracker.WindowFrames;
        float barW       = wf * PixelsPerFrame;
        int   now        = EffectiveNow;
        var (barX, barY) = ComputeBarPosition(barW);

        Draw.Rect(barX, barY, barW, BarHeight, ColorBackground);

        Draw.Line(new Vector2(barX, barY + LaneHeight),
                  new Vector2(barX + barW, barY + LaneHeight),
                  ColorLaneDivider);

        TimelineLayout.Compute(barX, barY, now);
        DrawRects(TimelineLayout.DashRects);
        DrawRects(TimelineLayout.JumpHeldRects);
        DrawRects(TimelineLayout.CoyoteRects);
        DrawRects(TimelineLayout.HalfGravRects);
        DrawRects(TimelineLayout.PauseRects);
        DrawRects(TimelineLayout.FreezeRects);

        for (int f = 0; f <= wf; f++) {
            float x = FrameX(barX, now, now - f);
            if (x < barX) break;

            if (f % RulerMajorInterval == 0) {
                Draw.Line(new Vector2(x, barY), new Vector2(x, barY + BarHeight), ColorRulerMajor);
            } else if (f % RulerMinorInterval == 0) {
                Draw.Line(new Vector2(x, barY), new Vector2(x, barY + RulerMinorTickH), ColorRulerMinor);
                Draw.Line(new Vector2(x, barY + BarHeight - RulerMinorTickH), new Vector2(x, barY + BarHeight), ColorRulerMinor);
            } else {
                Draw.Line(new Vector2(x, barY), new Vector2(x, barY + RulerSmallTickH), ColorRulerSmall);
                Draw.Line(new Vector2(x, barY + BarHeight - RulerSmallTickH), new Vector2(x, barY + BarHeight), ColorRulerSmall);
            }
        }

        DrawRects(TimelineLayout.MarkRects);

        if (_isInspecting)
            Draw.HollowRect(barX - 1, barY - 1, barW + 2, BarHeight + 2, ColorInspectBorder);

        if (_hasHighlight) {
            Draw.HollowRect(_hlX, _hlY, _hlW, _hlH, ColorHighlightOutline);

            Vector2 labelSize = ActiveFont.Measure(_highlightLabel) * LabelScale;
            bool    barInBottom = barY > HudHeight / 2f;
            float   labelY = barInBottom
                ? barY - LabelPadding - labelSize.Y
                : barY + BarHeight + LabelPadding;
            float   labelX = System.Math.Clamp(
                _hlX + _hlW / 2f,
                barX + labelSize.X / 2f,
                barX + barW - labelSize.X / 2f);

            ActiveFont.DrawOutline(
                _highlightLabel,
                new Vector2(labelX, labelY),
                new Vector2(0.5f, 0f),
                Vector2.One * LabelScale,
                Color.White,
                LabelStroke,
                Color.Black);
        }

        if (_isInspecting)
            DrawCursor(_mouseHudX, _mouseHudY);
    }

    private static void DrawCursor(float x, float y) {
        int scale = Settings.Instance.Fullscreen ? 6 : System.Math.Min(6, Engine.ViewWidth / 320);
        Color color = Color.Yellow;
        for (int i = -scale / 2; i <= scale / 2; i++) {
            Draw.Line(x - 4f * scale, y + i,       x - 2f * scale,       y + i, color);
            Draw.Line(x + 2f * scale - 1f, y + i,  x + 4f * scale - 1f,  y + i, color);
            Draw.Line(x + i, y - 4f * scale + 1f,  x + i, y - 2f * scale + 1f,  color);
            Draw.Line(x + i, y + 2f * scale,        x + i, y + 4f * scale,        color);
        }
        Draw.Line(x - 3f, y,      x + 2f, y,      color);
        Draw.Line(x,      y - 2f, x,      y + 3f, color);
    }

    private static void DrawRects(IReadOnlyList<VisualRect> rects) {
        for (int i = 0; i < rects.Count; i++) {
            var r = rects[i];
            Draw.Rect(r.X, r.Y, r.W, r.H, r.Color);
        }
    }
}
