using Microsoft.Xna.Framework;

namespace Celeste.Mod.AxiomeToolbox.Timeline;

public enum HudCorner { TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight }

internal static class TimelineConstants {
    // ── Timing ────────────────────────────────────────────────────────────────
    internal const float  PixelsPerFrame = 4f;
    internal static float BarWidth       => TimelineTracker.WindowFrames * PixelsPerFrame;

    // ── Lane geometry ─────────────────────────────────────────────────────────
    internal const int   LaneCount       = 2;
    internal const float LaneInnerHeight = 24f;
    internal const float LaneMargin      = 1f;
    internal const float LaneDividerH    = 1f;
    internal const float LaneHeight      = LaneInnerHeight + 2 * LaneMargin;        // 26
    internal const float BarHeight       = LaneCount * LaneHeight + LaneDividerH;   // 53

    internal const float BarMarginX = 6f;
    internal const float BarMarginY = 6f;
    internal const float HudWidth   = 1920f;
    internal const float HudHeight  = 1080f;

    // ── HUD frame ────────────────────────────────────────────────────────────
    internal const int   OverlayDepth       = -10000;
    internal const int   RulerMajorInterval = 10;
    internal const int   RulerMinorInterval = 5;
    internal const float RulerMinorTickH    = 12f;
    internal const float RulerSmallTickH    = 7f;

    internal static readonly Color ColorBackground  = Color.Black * 0.75f;
    internal static readonly Color ColorLaneDivider = Color.Gray * 0.3f;
    internal static readonly Color ColorFreezeFrame = Color.White * 0.4f;
    internal static readonly Color ColorPauseFrame  = Color.Gray * 0.3f;
    internal static readonly Color ColorRulerMajor  = Color.Gray * 0.8f;
    internal static readonly Color ColorRulerMinor  = Color.Gray * 0.65f;
    internal static readonly Color ColorRulerSmall  = Color.Gray * 0.55f;

    // ── Event colors (Paul Tol "Vibrant" colorblind-safe palette) ─────────────
    // All 7 Vibrant colors are distinguishable under deuteranopia, protanopia,
    // and tritanopia. Reference: https://sronpersonalpages.nl/~pault/
    //
    // Allocation:
    //   ColorTransition — White    outside palette, universally readable
    //   ColorCutscene   — Grey     #BBBBBB  cutscene start
    //   ColorJump       — Cyan     #33BBEE  jump press/held/fired
    //   ColorCoyote     — Magenta  #EE3377  coyote window
    //   ColorHalfGrav   — Orange   #EE7733  half-gravity window
    //   ColorDash       — Blue     #0077BB  dash
    //   (spare)         — Teal     #009988
    //   (spare)         — Red      #CC3311
    internal static readonly Color ColorTransition = Color.White;
    internal static readonly Color ColorCutscene   = new Color(187, 187, 187); // Grey    #BBBBBB
    internal static readonly Color ColorJump       = new Color( 51, 187, 238); // Cyan    #33BBEE
    internal static readonly Color ColorCoyote     = new Color(238,  51, 119); // Magenta #EE3377
    internal static readonly Color ColorHalfGrav   = new Color(238, 119,  51); // Orange  #EE7733
    internal static readonly Color ColorDash       = new Color(  0, 119, 187); // Blue    #0077BB

    internal static readonly Color ColorDashSpan      = ColorDash * 0.5f;
    internal static readonly Color ColorDashExtension = ColorDash * 0.3f;
    internal static readonly Color ColorCoyoteSpan    = ColorCoyote * 0.45f;
    internal static readonly Color ColorHalfGravSpan  = ColorHalfGrav * 0.55f;
    internal static readonly Color ColorJumpHeldSpan  = ColorJump * 0.5f;

    // ── Mouse interaction ─────────────────────────────────────────────────
    internal const int   ScrollSpeed          = 5;
    internal static readonly Color ColorHighlightOutline = Color.White * 0.8f;
    internal static readonly Color ColorInspectBorder    = Color.White * 0.4f;
    internal const float LabelScale   = 0.5f;
    internal const float LabelPadding = 4f;
    internal const float LabelStroke  = 2f;

    // ── Jump lane mark geometry ─────────────────────────────────────────────
    // The lane is symmetric (equal margins top and bottom), so press-mark height,
    // fired-mark height, and fired-mark Y offset are all the same value.
    internal const float JumpPressMarkH    = LaneMargin + LaneInnerHeight / 2f;               // 13
    internal const float JumpFiredMarkH    = JumpPressMarkH;                                  // 13
    internal const float SuperFiredMarkH   = JumpFiredMarkH + LaneDividerH + LaneHeight / 2f; // 27
    internal const float JumpFiredMarkOffY = JumpPressMarkH;                                  // 13

    // ── Dash extension ──────────────────────────────────────────────────────
    // Gameplay frames 11-15 (inclusive, 1-indexed) of StDash are extension frames
    // for horizontal and down-diagonal dashes. Freeze frames count; pause and transition frames don't.
    // WHY: _dashGameplayFrames starts at 0 and increments to 1 on the dash-start frame,
    // so gameplay frame 1 == _dashGameplayFrames == 1. Extension begins at frame 11.
    internal const int DashExtStartFrame = 11;
    internal const int DashExtFrameCount = 5;

    /// Convert a frame number to an x pixel position on the bar.
    /// Right edge == "now"; older frames are to the left.
    internal static float FrameX(float barX, int now, int frame) => barX + BarWidth - (now - frame) * PixelsPerFrame;
}
