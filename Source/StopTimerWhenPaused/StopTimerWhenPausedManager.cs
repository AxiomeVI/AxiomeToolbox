using Monocle;

namespace Celeste.Mod.AxiomeToolbox.StopTimerWhenPaused;

public static class StopTimerWhenPausedManager {

    public static void Load() {
        On.Celeste.Level.Update += OnUpdate;
    }

    public static void Unload() {
        On.Celeste.Level.Update -= OnUpdate;
    }

    public static void Reset() {
        if (Engine.Scene is Level level) level.TimerStopped = false;
    }

    private static void OnUpdate(On.Celeste.Level.orig_Update orig, Level self) {
        orig(self);
        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.StopTimerWhenPaused) return;
        self.TimerStopped = self.Paused || self.wasPaused;
    }
}
