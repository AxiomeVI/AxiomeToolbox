using Celeste.Mod.AxiomeToolbox.BadCornerBoost;
using Celeste.Mod.AxiomeToolbox.Checkpoint;
using Celeste.Mod.AxiomeToolbox.DeathConfirm;
using Celeste.Mod.AxiomeToolbox.MenuTiming;
using Celeste.Mod.AxiomeToolbox.WaterBoost;
using Celeste.Mod.AxiomeToolbox.StopTimerWhenPaused;
using Celeste.Mod.AxiomeToolbox.Timeline;
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

        TextMenuExt.SubMenu detectionSubMenu = new(Dialog.Clean(DialogIds.DetectionRulesId), false)
        {
            Visible = _settings.Enabled
        };

        detectionSubMenu.Add(new TextMenu.OnOff(
            Dialog.Clean(DialogIds.DetectBadCornerBoostId), _settings.DetectBadCornerBoost)
            .Change(v => { _settings.DetectBadCornerBoost = v; if (!v) BadCornerBoostDetector.Reset(); }));

        detectionSubMenu.Add(new TextMenu.OnOff(
            Dialog.Clean(DialogIds.DetectLostDeathFramesId), _settings.DetectLostDeathFrames)
            .Change(v => { _settings.DetectLostDeathFrames = v; if (!v) DeathConfirmDetector.Reset(); }));

        detectionSubMenu.Add(new TextMenu.OnOff(
            Dialog.Clean(DialogIds.DetectMenuTimingLossId), _settings.DetectMenuTimingLoss)
            .Change(v => { _settings.DetectMenuTimingLoss = v; if (!v) MenuTimingDetector.Reset(); }));

        detectionSubMenu.Add(new TextMenu.OnOff(
            Dialog.Clean(DialogIds.DetectFailedWaterBoostId), _settings.DetectFailedWaterBoost)
            .Change(v => { _settings.DetectFailedWaterBoost = v; if (!v) WaterBoostDetector.Reset(); }));

        // ── Timeline submenu ──────────────────────────────────────────────────
        int[] windowSizes = [30, 60, 120, 300];
        int currentWindowIdx = System.Array.IndexOf(windowSizes, _settings.TimelineWindowSize);
        if (currentWindowIdx < 0) currentWindowIdx = 2; // default to 120

        TextMenuExt.SubMenu timelineSubMenu = new(Dialog.Clean(DialogIds.TimelineSubmenuId), false) {
            Visible = _settings.Enabled
        };

        timelineSubMenu.Add(new TextMenu.Slider(
            Dialog.Clean(DialogIds.TimelinePositionId),
            i => ((HudCorner)i).ToString(),
            (int)HudCorner.TopLeft,
            (int)HudCorner.BottomRight,
            (int)_settings.TimelinePosition)
            .Change(v => _settings.TimelinePosition = (HudCorner)v));

        timelineSubMenu.Add(new TextMenu.Slider(
            Dialog.Clean(DialogIds.TimelineWindowSizeId),
            i => windowSizes[i].ToString(),
            0, windowSizes.Length - 1,
            currentWindowIdx)
            .Change(v => _settings.TimelineWindowSize = windowSizes[v]));

        TextMenu.Button placeButton = new TextMenu.Button(Dialog.Clean(DialogIds.PlaceCheckpointId)) {
            Visible = _settings.Enabled,
            Disabled = !inGame,
            OnPressed = () => { if (Engine.Scene is Level l) CheckpointPlacementManager.PlaceCheckpointAtPlayer(l); }
        };

        TextMenu.Button clearButton = new TextMenu.Button(Dialog.Clean(DialogIds.ClearCheckpointId)) {
            Visible = _settings.Enabled,
            Disabled = !inGame,
            OnPressed = () => CheckpointPlacementManager.ClearAll()
        };

        TextMenu.Button keybindButton = new TextMenu.Button(Dialog.Clean(DialogIds.KeybindConfigId)) {
            Visible = _settings.Enabled
        };
        keybindButton.Pressed(() => {
            menu.Focused = false;
            var ui = new KeybindConfigUi([
                new(DialogIds.PlaceCheckpointId, _settings.PlaceCheckpoint),
                new(DialogIds.ClearCheckpointId, _settings.ClearCheckpoints),
                new(DialogIds.TimelineInspectId, _settings.TimelineInspect),
            ]);
            ui.OnClose = () => menu.Focused = true;
            Engine.Scene.Add(ui);
            Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
        });

        menu.Add(new TextMenu.OnOff(Dialog.Clean(DialogIds.EnabledId), _settings.Enabled).Change(
            value =>
            {
                _settings.Enabled = value;
                _stopTimerWhenPaused.Visible = value;
                detectionSubMenu.Visible = value;
                timelineSubMenu.Visible = value;
                placeButton.Visible = value;
                clearButton.Visible = value;
                keybindButton.Visible = value;
                if (!value) {
                    BadCornerBoostDetector.Reset();
                    DeathConfirmDetector.Reset();
                    MenuTimingDetector.Reset();
                    WaterBoostDetector.Reset();
                    CheckpointPlacementManager.ClearAll();
                    StopTimerWhenPausedManager.Reset();
                    TimelineTracker.Reset();
                }
            }
        ));

        menu.Add(_stopTimerWhenPaused);
        menu.Add(detectionSubMenu);
        menu.Add(timelineSubMenu);
        menu.Add(placeButton);
        menu.Add(clearButton);
        menu.Add(keybindButton);
    }
}
