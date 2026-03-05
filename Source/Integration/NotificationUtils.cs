using System;
using System.Linq;
using System.Reflection;
using Celeste.Mod.SpeedrunTool.Message;

namespace Celeste.Mod.AxiomeToolbox.Integration;

public static class NotificationUtils {
    private static bool      initialized = false;
    private static MethodInfo tooltipShow = null;

    private static void Initialize() {
        if (initialized) return;
        initialized = true;
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "MovementLinter");
        tooltipShow = assembly
            ?.GetType("Celeste.Mod.MovementLinter.Tooltip")
            ?.GetMethod("Show", [typeof(string), typeof(float)]);
    }

    public static void Show(string message) {
        Initialize();
        if (tooltipShow != null)
            tooltipShow.Invoke(null, [message, 2f]);
        else
            PopupMessageUtils.Show(message, null);
    }

    public static void ShowFrameLoss(string singularId, string pluralId, int count) {
        string message = count == 1
            ? Dialog.Clean(singularId)
            : string.Format(Dialog.Get(pluralId), count);
        Show(message);
    }
}
