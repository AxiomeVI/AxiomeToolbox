using System;
using Celeste.Mod.AxiomeQoL.Menu;
using FMOD.Studio;

namespace Celeste.Mod.AxiomeQoL;

public class AxiomeQoLModule : EverestModule {
    public static AxiomeQoLModule Instance { get; private set; }

    public override Type SettingsType => typeof(AxiomeQoLModuleSettings);
    public static AxiomeQoLModuleSettings Settings => (AxiomeQoLModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(AxiomeQoLModuleSession);
    public static AxiomeQoLModuleSession Session => (AxiomeQoLModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(AxiomeQoLModuleSaveData);
    public static AxiomeQoLModuleSaveData SaveData => (AxiomeQoLModuleSaveData) Instance._SaveData;

    public AxiomeQoLModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(AxiomeQoLModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(AxiomeQoLModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        On.Celeste.Level.Update += Level_OnUpdate;
    }

    public override void Unload() {
        On.Celeste.Level.Update -= Level_OnUpdate;
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance pauseSnapshot)
    {
        CreateModMenuSectionHeader(menu, inGame, pauseSnapshot);
        ModMenuOptions.CreateMenu(menu);
        // CreateModMenuSectionKeyBindings(menu, inGame, pauseSnapshot);
    }

    private static void Level_OnUpdate(On.Celeste.Level.orig_Update orig, Level self) {
        orig(self);
        if (!Settings.StopTimerWhenPaused) 
            return;

        if (self.Paused || self.wasPaused) 
            self.TimerStopped = true;
        else 
            self.TimerStopped = false;
    }
}