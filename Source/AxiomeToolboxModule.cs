using System;
using Celeste.Mod.AxiomeToolbox.UI;
using FMOD.Studio;
using Celeste.Mod.AxiomeToolbox.Checkpoint;
using MonoMod.ModInterop;
using Celeste.Mod.AxiomeToolbox.Integration;
using System.Collections.Generic;

namespace Celeste.Mod.AxiomeToolbox;

public class AxiomeToolboxModule : EverestModule {
    public static AxiomeToolboxModule Instance { get; private set; }

    public override Type SettingsType => typeof(AxiomeToolboxModuleSettings);
    public static AxiomeToolboxModuleSettings Settings => (AxiomeToolboxModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(AxiomeToolboxModuleSession);
    public static AxiomeToolboxModuleSession Session => (AxiomeToolboxModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(AxiomeToolboxModuleSaveData);
    public static AxiomeToolboxModuleSaveData SaveData => (AxiomeToolboxModuleSaveData) Instance._SaveData;

    public object SaveLoadInstance = null;

    public AxiomeToolboxModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(AxiomeToolboxModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(AxiomeToolboxModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        On.Celeste.Level.Update += Level_OnUpdate;
        On.Celeste.Level.End    += OnLevelEnd;
        CheckpointPlacementManager.Load();
        typeof(SaveLoadIntegration).ModInterop();
        SaveLoadInstance = SaveLoadIntegration.RegisterSaveLoadAction(
            null, 
            OnLoadState, 
            null, 
            OnBeforeSaveState,
            null,
            null
        );
    }

    public override void Unload() {
        On.Celeste.Level.Update -= Level_OnUpdate;
        On.Celeste.Level.End    -= OnLevelEnd;
        CheckpointPlacementManager.Unload();
        SaveLoadIntegration.Unregister(SaveLoadInstance);
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance pauseSnapshot)
    {
        CreateModMenuSectionHeader(menu, inGame, pauseSnapshot);
        ModMenuOptions.CreateMenu(menu, inGame);
        CreateModMenuSectionKeyBindings(menu, inGame, pauseSnapshot);
    }

    private static void Level_OnUpdate(On.Celeste.Level.orig_Update orig, Level self) {
        orig(self);
        if (!Settings.Enabled) return;
        
        CheckpointPlacementManager.Update(self);

        if (!Settings.StopTimerWhenPaused) return;

        if (self.Paused || self.wasPaused) 
            self.TimerStopped = true;
        else 
            self.TimerStopped = false;
    }

    private static void OnLevelEnd(On.Celeste.Level.orig_End orig, Level self) {
        orig(self);
        if (Settings.Enabled) CheckpointPlacementManager.ClearAll();
    }

    private static void OnLoadState(Dictionary<Type, Dictionary<string, object>> dictionary, Level level) {
        if (Settings.Enabled) CheckpointPlacementManager.ResetAllTriggeredStates();
    }

    private static void OnBeforeSaveState(Level level) {
        if (Settings.Enabled) CheckpointPlacementManager.ResetAllTriggeredStates();
    }
}