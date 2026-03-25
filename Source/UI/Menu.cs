using Celeste.Mod.AxiomeToolbox.BadCornerBoost;
using Celeste.Mod.AxiomeToolbox.Checkpoint;
using Celeste.Mod.AxiomeToolbox.DeathConfirm;
using Celeste.Mod.AxiomeToolbox.MenuTiming;
using Celeste.Mod.AxiomeToolbox.WaterBoost;
using Celeste.Mod.AxiomeToolbox.StopTimerWhenPaused;
using Monocle;

namespace Celeste.Mod.AxiomeToolbox.UI;

public static class ModMenuOptions {
    private static readonly AxiomeToolboxModuleSettings _settings = AxiomeToolboxModule.Settings;

    public static void CreateMenu(TextMenu menu, bool inGame)
    {
        TextMenu.OnOff _stopTimerWhenPaused = (TextMenu.OnOff)new TextMenu.OnOff(
            Dialog.Clean(DialogIds.StopTimerWhenPausedId),
            _settings.StopTimerWhenPaused) { Visible = _settings.Enabled }.Change(
                value =>
                {
                    _settings.StopTimerWhenPaused = value;
                    if (!value) StopTimerWhenPausedManager.Reset();
                }
        );

        TextMenu.OnOff detectBadCB = (TextMenu.OnOff)new TextMenu.OnOff(
            Dialog.Clean(DialogIds.DetectBadCornerBoostId), _settings.DetectBadCornerBoost)
            { Visible = _settings.Enabled }
            .Change(v => { _settings.DetectBadCornerBoost = v; if (!v) BadCornerBoostDetector.Reset(); });

        TextMenu.OnOff detectDeathFrames = (TextMenu.OnOff)new TextMenu.OnOff(
            Dialog.Clean(DialogIds.DetectLostDeathFramesId), _settings.DetectLostDeathFrames)
            { Visible = _settings.Enabled }
            .Change(v => { _settings.DetectLostDeathFrames = v; if (!v) DeathConfirmDetector.Reset(); });

        TextMenu.OnOff detectMenuTiming = (TextMenu.OnOff)new TextMenu.OnOff(
            Dialog.Clean(DialogIds.DetectMenuTimingLossId), _settings.DetectMenuTimingLoss)
            { Visible = _settings.Enabled }
            .Change(v => { _settings.DetectMenuTimingLoss = v; if (!v) MenuTimingDetector.Reset(); });

        TextMenu.OnOff detectWaterBoost = (TextMenu.OnOff)new TextMenu.OnOff(
            Dialog.Clean(DialogIds.DetectFailedWaterBoostId), _settings.DetectFailedWaterBoost)
            { Visible = _settings.Enabled }
            .Change(v => { _settings.DetectFailedWaterBoost = v; if (!v) WaterBoostDetector.Reset(); });

        TextMenu.Button placeButton = new TextMenu.Button(Dialog.Clean(DialogIds.PlaceCheckpointId)) {
            Visible = _settings.Enabled,
            Disabled = !inGame,
            OnPressed = () => { if (Engine.Scene is Level l) CheckpointPlacementManager.PlaceCheckpointAtPlayer(l); }
        };

        TextMenu.Button clearButton = new TextMenu.Button(Dialog.Clean(DialogIds.ClearCheckpointId)) {
            Visible = _settings.Enabled,
            Disabled = !inGame,
            OnPressed = () => CheckpointPlacementManager.ClearAll(Engine.Scene as Level)
        };

        TextMenu.Button keybindButton = new TextMenu.Button(Dialog.Clean(DialogIds.KeybindConfigId)) {
            Visible = _settings.Enabled
        };
        keybindButton.Pressed(() => {
            menu.Focused = false;
            var ui = new KeybindConfigUi();
            ui.OnClose = () => menu.Focused = true;
            Engine.Scene.Add(ui);
            Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
        });

        menu.Add(new TextMenu.OnOff(Dialog.Clean(DialogIds.EnabledId), _settings.Enabled).Change(
            value =>
            {
                _settings.Enabled = value;
                _stopTimerWhenPaused.Visible = value;
                detectBadCB.Visible = value;
                detectDeathFrames.Visible = value;
                detectMenuTiming.Visible = value;
                detectWaterBoost.Visible = value;
                placeButton.Visible = value;
                clearButton.Visible = value;
                keybindButton.Visible = value;
                if (!value) {
                    BadCornerBoostDetector.Reset();
                    DeathConfirmDetector.Reset();
                    MenuTimingDetector.Reset();
                    WaterBoostDetector.Reset();
                    CheckpointPlacementManager.ClearAll(Engine.Scene as Level);
                    StopTimerWhenPausedManager.Reset();
                }
            }
        ));

        menu.Add(_stopTimerWhenPaused);
        menu.Add(detectBadCB);
        menu.Add(detectDeathFrames);
        menu.Add(detectMenuTiming);
        menu.Add(detectWaterBoost);
        menu.Add(placeButton);
        menu.Add(clearButton);
        menu.Add(keybindButton);
    }
}
