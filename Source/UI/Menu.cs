using Monocle;

namespace Celeste.Mod.AxiomeQoL.Menu;

public static class ModMenuOptions {
    private static AxiomeQoLModuleSettings _settings = AxiomeQoLModule.Settings;

    public static void CreateMenu(TextMenu menu)
    {     
        menu.Add(new TextMenu.OnOff(Dialog.Clean(DialogIds.StopTimerWhenPaused), _settings.StopTimerWhenPaused).Change(
            value =>
            {
                _settings.StopTimerWhenPaused = value;
                if (!value && Engine.Scene is Level level) {
                    level.TimerStopped = false;
                    Logger.Log(LogLevel.Info, "HERE MEY MOD", level.TimerStopped.ToString());
                }
            }
        ));
    }
}