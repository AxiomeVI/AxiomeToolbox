using Celeste.Mod.AxiomeToolbox.Checkpoint;
using Monocle;

namespace Celeste.Mod.AxiomeToolbox.UI;

public static class ModMenuOptions {
    private static readonly AxiomeToolboxModuleSettings _settings = AxiomeToolboxModule.Settings;

    public static void CreateMenu(TextMenu menu, bool inGame)
    {
        TextMenu.OnOff _stopTimerWhenPaused = (TextMenu.OnOff)new TextMenu.OnOff(
            Dialog.Clean(DialogIds.StopTimerWhenPausedId),
            _settings.StopTimerWhenPaused).Change(
                value =>
                {
                    _settings.StopTimerWhenPaused = value;
                    if (!value && Engine.Scene is Level level) {
                        level.TimerStopped = false;
                    }
                }
        );

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
                if (inGame) {
                    placeButton.Visible = value;
                    clearButton.Visible = value;
                }
                if (!value) {
                    CheckpointPlacementManager.ClearAll(Engine.Scene as Level);
                    if (_settings.StopTimerWhenPaused && Engine.Scene is Level level) {
                        level.TimerStopped = false;
                    }
                }
            }
        ));

        menu.Add(_stopTimerWhenPaused);

        if (inGame) {
            menu.Add(placeButton);
            menu.Add(clearButton);
        }
    }
}
