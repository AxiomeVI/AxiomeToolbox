using System;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Celeste.Mod.AxiomeToolbox.Integration;

namespace Celeste.Mod.AxiomeToolbox.BadCornerBoost;

public static class BadCornerBoostDetector {

    private static bool    cornerboostReady = false;
    private static ILHook  ilHook;

    public static void Load() {
        ilHook = new ILHook(
            typeof(Player).GetMethod("orig_Update", BindingFlags.Instance | BindingFlags.Public),
            ResetCornerboostReady
        );
        On.Celeste.Player.ClimbJump   += OnClimbJump;
        IL.Celeste.Player.OnCollideH  += SetCornerboostReady;
    }

    public static void Reset() {
        cornerboostReady = false;
    }

    public static void Unload() {
        ilHook?.Dispose();
        On.Celeste.Player.ClimbJump   -= OnClimbJump;
        IL.Celeste.Player.OnCollideH  -= SetCornerboostReady;
    }

    private static void ResetCornerboostReady(ILContext il) {
        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.DetectBadCornerBoost) return;
        var cursor = new ILCursor(il);
        cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Actor>("Update"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(DisableCornerboostReady);
    }

    private static void DisableCornerboostReady(Player _) {
        cornerboostReady = false;
    }

    private static void SetCornerboostReady(ILContext il) {
        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.DetectBadCornerBoost) return;
        var cursor = new ILCursor(il);
        cursor.GotoNext(MoveType.After, instr => instr.MatchStfld<Player>("wallSpeedRetained"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate(EnableCornerboostReady);
    }

    private static void EnableCornerboostReady(Player player, CollisionData data) {
        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.DetectBadCornerBoost) return;
        if (Math.Abs(data.Moved.X) <= 2) return;
        int state = player.StateMachine.State;
        if (state == Player.StNormal || state == Player.StDash || state == Player.StRedDash)
            cornerboostReady = true;
    }

    private static void OnClimbJump(On.Celeste.Player.orig_ClimbJump orig, Player self) {
        if (cornerboostReady && AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectBadCornerBoost) {
            float retained = self.wallSpeedRetained;
            if (Input.MoveX == Math.Sign(retained))
                NotificationUtils.Show(Dialog.Clean(DialogIds.BadCBDetectedId));
            cornerboostReady = false;
        }
        orig(self);
    }
}
