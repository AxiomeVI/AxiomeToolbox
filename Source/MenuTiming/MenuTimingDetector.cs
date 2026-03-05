using Celeste.Mod.AxiomeToolbox.Integration;
using Monocle;

namespace Celeste.Mod.AxiomeToolbox.MenuTiming;

public static class MenuTimingDetector {

    private enum State { Idle, Paused, DownPressed, UpConfirmPressed }

    private static State state = State.Idle;
    private static ulong pauseFrame;

    public static void Load() {
        On.Celeste.Level.Pause  += OnPause;
        On.Celeste.Level.Update += OnUpdate;
    }

    public static void Reset() {
        state = State.Idle;
        pauseFrame = 0;
    }

    public static void Unload() {
        On.Celeste.Level.Pause  -= OnPause;
        On.Celeste.Level.Update -= OnUpdate;
    }

    private static void OnPause(On.Celeste.Level.orig_Pause orig, Level self, int startIndex, bool minimal, bool quickReset) {
        orig(self, startIndex, minimal, quickReset);
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectMenuTimingLoss) {
            state = State.Paused;
            pauseFrame = Engine.FrameCounter;
        }
    }

    private static void OnUpdate(On.Celeste.Level.orig_Update orig, Level self) {
        orig(self);
        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.DetectMenuTimingLoss || state == State.Idle) return;

        bool down    = Input.MenuDown.Pressed;
        bool up      = Input.MenuUp.Pressed;
        bool confirm = Input.MenuConfirm.Pressed;

        switch (state) {
            case State.Paused:
                if (down && confirm)    { Report(2); return; }
                else if (down)          { state = State.DownPressed; }
                else if (up && confirm) { state = State.UpConfirmPressed; }
                break;

            case State.DownPressed:
                if (down && confirm)    { Report(3); return; }
                break;

            case State.UpConfirmPressed:
                if (confirm)            { Report(3); return; }
                break;
        }

        if (!self.Paused) state = State.Idle;
    }

    private static void Report(int minimumFrames) {
        int lost = (int)(Engine.FrameCounter - pauseFrame + 1) - minimumFrames;
        state = State.Idle;
        if (lost > 0)
            NotificationUtils.ShowFrameLoss(DialogIds.LostMenuFramesId, DialogIds.LostMenuFramesPluralId, lost);
    }
}
