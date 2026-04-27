using Monocle;
using MonoMod.ModInterop;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.AxiomeToolbox.Integration;

[ModImportName("SpeedrunTool.SaveLoad")]
public static class SaveLoadIntegration
{
    public static Func<Action<Dictionary<Type, Dictionary<string, object>>, Level>,
        Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action,
        Action<Level>, Action<Level>, Action, object> RegisterSaveLoadAction;
    public static Action<object> Unregister;
    // WHY: Registers the entity with SRT's IgnoreSaveLoadComponent — removed before
    // deep-cloning and re-added after, so it never enters any save state snapshot.
    // 'based' false = unconditional remove/re-add (use for the timeline overlay).
    public static Action<Entity, bool> IgnoreSaveState;
}

[ModImportName("SpeedrunTool.RoomTimer")]
public static class RoomTimerIntegration {
    public static Func<long> GetRoomTime;
}