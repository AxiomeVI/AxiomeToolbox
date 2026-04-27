using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Celeste.Mod.AxiomeToolbox.Timeline.TimelineConstants;
using static Celeste.Mod.AxiomeToolbox.Timeline.TimelineTracker;

namespace Celeste.Mod.AxiomeToolbox.Timeline;

internal readonly record struct VisualRect(float X, float Y, float W, float H, Color Color);

internal static class TimelineLayout {
    private static readonly List<VisualRect> _pauseRects    = new(4);
    private static readonly List<VisualRect> _freezeRects   = new(16);
    private static readonly List<VisualRect> _dashRects     = new(8);
    private static readonly List<VisualRect> _coyoteRects   = new(8);
    private static readonly List<VisualRect> _halfGravRects = new(8);
    private static readonly List<VisualRect> _jumpHeldRects = new(8);
    private static readonly List<VisualRect> _markRects     = new(8);
    internal static IReadOnlyList<VisualRect> PauseRects    => _pauseRects;
    internal static IReadOnlyList<VisualRect> FreezeRects   => _freezeRects;
    internal static IReadOnlyList<VisualRect> DashRects     => _dashRects;
    internal static IReadOnlyList<VisualRect> CoyoteRects   => _coyoteRects;
    internal static IReadOnlyList<VisualRect> HalfGravRects => _halfGravRects;
    internal static IReadOnlyList<VisualRect> JumpHeldRects => _jumpHeldRects;
    internal static IReadOnlyList<VisualRect> MarkRects     => _markRects;

    private static int   _lastNow          = -1;
    private static int   _lastWindowFrames = -1;
    private static float _lastBarX         = float.NaN;
    private static float _lastBarY         = float.NaN;

    internal static void Compute(float barX, float barY, int effectiveNow) {
        int now = effectiveNow;
        int wf  = WindowFrames;
        if (now == _lastNow && wf == _lastWindowFrames
            && barX == _lastBarX && barY == _lastBarY) return;
        _lastNow          = now;
        _lastWindowFrames = wf;
        _lastBarX         = barX;
        _lastBarY         = barY;

        _pauseRects.Clear();
        _freezeRects.Clear();
        _dashRects.Clear();
        _coyoteRects.Clear();
        _halfGravRects.Clear();
        _jumpHeldRects.Clear();
        _markRects.Clear();

        var pauses = PauseSpans;
        for (int i = 0; i < pauses.Count; i++)
            EmitSpanRect(_pauseRects, barX, barY, now, pauses[i].Start, pauses[i].End, ColorPauseFrame);
        if (OpenPauseStart >= 0)
            EmitSpanRect(_pauseRects, barX, barY, now, OpenPauseStart, now, ColorPauseFrame);

        var spans = FreezeSpans;
        for (int i = 0; i < spans.Count; i++)
            EmitSpanRect(_freezeRects, barX, barY, now, spans[i].Start, spans[i].End, ColorFreezeFrame);
        if (OpenFreezeStart >= 0)
            EmitSpanRect(_freezeRects, barX, barY, now, OpenFreezeStart, now, ColorFreezeFrame);

        EmitMarkRects(_markRects, TransitionMarks, ColorTransition, barX, barY, now);
        EmitMarkRects(_markRects, CutsceneMarks, ColorCutscene, barX, barY, now);
        EmitMarkRects(_markRects, DashInputMarks, ColorDash, barX, barY + LaneHeight + LaneDividerH, now, LaneHeight);

        EmitJumpLaneRects(barX, barY + LaneMargin, now, wf);

        EmitMarkRects(_markRects, JumpPressMarks, ColorJump, barX, barY, now, JumpPressMarkH);
        EmitMarkRects(_markRects, JumpFiredMarks, ColorJump, barX, barY + JumpFiredMarkOffY, now, JumpFiredMarkH);
        EmitMarkRects(_markRects, SuperFiredMarks, ColorJump, barX, barY + JumpFiredMarkOffY, now, SuperFiredMarkH);

        var dashes = DashSpans;
        for (int i = 0; i < dashes.Count; i++)
            EmitDashSpanRects(_dashRects, barX, barY, now, dashes[i]);
        if (OpenDashStart >= 0) {
            int es = OpenDashHasExtension && OpenDashExtStart >= 0 ? OpenDashExtStart : now;
            int ee = OpenDashHasExtension && OpenDashExtEnd   >= 0 ? OpenDashExtEnd   : now;
            EmitDashSpanRects(_dashRects, barX, barY, now, new DashSpan(OpenDashStart, now, es, ee));
        }
    }

    // ── Jump lane N-event split ───────────────────────────────────────────────

    private readonly record struct JumpLaneEvent(
        IReadOnlyList<FrameSpan> Spans, Func<int> OpenStart, Color Color, List<VisualRect> Target);

    private static readonly JumpLaneEvent[] _jumpLaneEvents = [
        new(JumpHeldSpans, () => OpenJumpHeldStart, ColorJumpHeldSpan, _jumpHeldRects),
        new(CoyoteSpans,   () => OpenCoyoteStart,   ColorCoyoteSpan,   _coyoteRects),
        new(HalfGravSpans, () => OpenHalfGravStart, ColorHalfGravSpan, _halfGravRects),
    ];
    private static readonly int[]  _jumpLaneOpenStarts  = new int[_jumpLaneEvents.Length];
    private static readonly bool[] _jumpLaneActiveCache = new bool[_jumpLaneEvents.Length];

    private static void EmitJumpLaneRects(float barX, float laneY, int now, int wf) {
        for (int ei = 0; ei < _jumpLaneEvents.Length; ei++)
            _jumpLaneOpenStarts[ei] = _jumpLaneEvents[ei].OpenStart();

        for (int f = now - wf; f < now; f++) {
            int activeCount = 0;
            for (int ei = 0; ei < _jumpLaneEvents.Length; ei++) {
                bool a = IsFrameActive(f, _jumpLaneEvents[ei].Spans, _jumpLaneOpenStarts[ei], now);
                _jumpLaneActiveCache[ei] = a;
                if (a) activeCount++;
            }
            if (activeCount == 0) continue;

            float stripH = LaneInnerHeight / activeCount;
            float stripY = laneY;
            float x = FrameX(barX, now, f);
            if (x < barX || x >= barX + BarWidth) continue;
            for (int ei = 0; ei < _jumpLaneEvents.Length; ei++) {
                if (!_jumpLaneActiveCache[ei]) continue;
                _jumpLaneEvents[ei].Target.Add(new VisualRect(x, stripY, PixelsPerFrame, stripH, _jumpLaneEvents[ei].Color));
                stripY += stripH;
            }
        }
    }

    private static bool IsFrameActive(int frame, IReadOnlyList<FrameSpan> spans, int openStart, int now) {
        for (int i = 0; i < spans.Count; i++) {
            if (spans[i].Start > frame) break;
            if (frame < spans[i].End) return true;
        }
        return openStart >= 0 && frame >= openStart && frame < now;
    }

    private static void EmitMarkRects(List<VisualRect> target, IReadOnlyList<int> marks,
                                      Color color, float barX, float barY, int now,
                                      float height = BarHeight) {
        for (int i = 0; i < marks.Count; i++) {
            float x = FrameX(barX, now, marks[i]);
            if (x < barX || x >= barX + BarWidth) continue;
            target.Add(new VisualRect(x, barY, 1f, height, color));
        }
    }

    private static void EmitDashSpanRects(List<VisualRect> target, float barX, float barY,
                                           int now, DashSpan span) {
        float dashY = barY + LaneHeight + LaneDividerH + LaneMargin;
        if (span.ExtStart >= span.ExtEnd) {
            EmitSpanRect(target, barX, dashY, now, span.Start, span.End, ColorDashSpan, LaneInnerHeight);
        } else {
            if (span.ExtStart > span.Start)
                EmitSpanRect(target, barX, dashY, now, span.Start, span.ExtStart, ColorDashSpan, LaneInnerHeight);
            EmitSpanRect(target, barX, dashY, now, Math.Max(span.ExtStart, span.Start),
                         span.End, ColorDashExtension, LaneInnerHeight);
        }
    }

    private static void EmitSpanRect(List<VisualRect> target, float barX, float barY,
                                     int now, int start, int end, Color color,
                                     float height = BarHeight) {
        float xStart = FrameX(barX, now, start);
        float xEnd   = FrameX(barX, now, end);
        float drawX  = Math.Max(xStart, barX);
        float drawW  = Math.Min(xEnd, barX + BarWidth) - drawX;
        if (drawW < 1f) drawW = 1f;
        if (drawX >= barX + BarWidth) return;
        target.Add(new VisualRect(drawX, barY, drawW, height, color));
    }
}
