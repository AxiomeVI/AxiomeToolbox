using System;
using System.Collections.Generic;
using Celeste.Mod.AxiomeToolbox.BadCornerBoost;
using Celeste.Mod.AxiomeToolbox.Checkpoint;
using Celeste.Mod.AxiomeToolbox.DeathConfirm;
using Celeste.Mod.AxiomeToolbox.Integration;
using Celeste.Mod.AxiomeToolbox.MenuTiming;
using Celeste.Mod.AxiomeToolbox.StopTimerWhenPaused;
using Celeste.Mod.AxiomeToolbox.UI;
using FMOD.Studio;
using MonoMod.ModInterop;

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
        CheckpointPlacementManager.Load();
        BadCornerBoostDetector.Load();
        DeathConfirmDetector.Load();
        MenuTimingDetector.Load();
        StopTimerWhenPausedManager.Load();
        typeof(SaveLoadIntegration).ModInterop();
        typeof(RoomTimerIntegration).ModInterop();
        SaveLoadInstance = SaveLoadIntegration.RegisterSaveLoadAction(
            OnSaveState,
            OnLoadState,
            null,
            OnBeforeSaveState,
            null,
            null
        );
    }

    public override void Unload() {
        CheckpointPlacementManager.Unload();
        BadCornerBoostDetector.Unload();
        DeathConfirmDetector.Unload();
        MenuTimingDetector.Unload();
        StopTimerWhenPausedManager.Unload();
        SaveLoadIntegration.Unregister(SaveLoadInstance);
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance pauseSnapshot)
    {
        CreateModMenuSectionHeader(menu, inGame, pauseSnapshot);
        ModMenuOptions.CreateMenu(menu, inGame);
        CreateModMenuSectionKeyBindings(menu, inGame, pauseSnapshot);
    }

    private static void OnSaveState(Dictionary<Type, Dictionary<string, object>> d, Level level) {
        if (!Settings.Enabled) return;
        CheckpointPlacementManager.OnSaveState(d, level);
    }

    private static void OnLoadState(Dictionary<Type, Dictionary<string, object>> d, Level level) {
        if (!Settings.Enabled) return;
        CheckpointPlacementManager.OnLoadState(d, level);
    }

    private static void OnBeforeSaveState(Level level) {
        if (!Settings.Enabled) return;
        CheckpointPlacementManager.OnBeforeSaveState(level);
    }
}
