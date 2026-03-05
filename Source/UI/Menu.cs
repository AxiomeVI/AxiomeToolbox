using Celeste.Mod.AxiomeToolbox.BadCornerBoost;
using Celeste.Mod.AxiomeToolbox.Checkpoint;
using Celeste.Mod.AxiomeToolbox.DeathConfirm;
using Celeste.Mod.AxiomeToolbox.MenuTiming;
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

        TextMenu.Button placeButton = null;
        TextMenu.Button clearButton = null;

        if (inGame) {
            Level level = Engine.Scene as Level;

            placeButton = new TextMenu.Button(Dialog.Clean(DialogIds.PlaceCheckpointId)) {
                Visible = _settings.Enabled,
                OnPressed = () => CheckpointPlacementManager.PlaceCheckpointAtPlayer(level)
            };

            clearButton = new TextMenu.Button(Dialog.Clean(DialogIds.ClearCheckpointId)) {
                Visible = _settings.Enabled,
                OnPressed = () => CheckpointPlacementManager.ClearRoomCheckpoints(level)
            };
        }

        menu.Add(new TextMenu.OnOff(Dialog.Clean(DialogIds.EnabledId), _settings.Enabled).Change(
            value =>
            {
                _settings.Enabled = value;
                _stopTimerWhenPaused.Visible = value;
                detectBadCB.Visible = value;
                detectDeathFrames.Visible = value;
                detectMenuTiming.Visible = value;
                if (inGame) {
                    placeButton.Visible = value;
                    clearButton.Visible = value;
                }
                if (!value) {
                    BadCornerBoostDetector.Reset();
                    DeathConfirmDetector.Reset();
                    MenuTimingDetector.Reset();
                    CheckpointPlacementManager.ClearAll(Engine.Scene as Level);
                    StopTimerWhenPausedManager.Reset();
                }
            }
        ));

        menu.Add(_stopTimerWhenPaused);
        menu.Add(detectBadCB);
        menu.Add(detectDeathFrames);
        menu.Add(detectMenuTiming);

        if (inGame) {
            menu.Add(placeButton);
            menu.Add(clearButton);
        }
    }
}
