using Celeste.Mod.AxiomeToolbox.Integration;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.AxiomeToolbox.WaterBoost;

public static class WaterBoostDetector {

    private static bool wasOnSurface;
    private static int  jumpInputs;
    private static int  jumpsFired;
    private static int  framesUntilEnd; // -1 = inactive; 0+ = counting down

    public static void Load() {
        On.Celeste.Player.Jump         += OnJump;
        On.Celeste.Player.SuperJump     += OnSuperJump;
        On.Celeste.Player.WallJump      += OnWallJump;
        On.Celeste.Player.SuperWallJump += OnSuperWallJump;
        On.Celeste.Player.Die           += OnDie;
        On.Celeste.Player.OnTransition += OnTransition;
        On.Celeste.Level.Update        += OnUpdate;
    }

    public static void Unload() {
        On.Celeste.Player.Jump         -= OnJump;
        On.Celeste.Player.SuperJump     -= OnSuperJump;
        On.Celeste.Player.WallJump      -= OnWallJump;
        On.Celeste.Player.SuperWallJump -= OnSuperWallJump;
        On.Celeste.Player.Die           -= OnDie;
        On.Celeste.Player.OnTransition -= OnTransition;
        On.Celeste.Level.Update        -= OnUpdate;
    }

    public static void Reset() {
        wasOnSurface   = false;
        jumpInputs     = 0;
        jumpsFired     = 0;
        framesUntilEnd = -1;
    }

    private static void Evaluate() {
        if (jumpsFired < jumpInputs) {
            string msg = jumpsFired == 1
                ? string.Format(Dialog.Get(DialogIds.FailedWaterBoostId), jumpInputs)
                : string.Format(Dialog.Get(DialogIds.FailedWaterBoostPluralId), jumpsFired, jumpInputs);
            NotificationUtils.Show(msg);
        }
        Reset();
    }

    private static bool IsSequenceActive() => jumpsFired > 0 || framesUntilEnd >= 0;

    private static int CountJumpPressed() {
        int count = 0;
        foreach (var key in Input.Jump.Binding.Keyboard)
            if (MInput.Keyboard.Pressed(key)) count++;
        foreach (var padButton in Input.Jump.Binding.Controller)
            if (MInput.GamePads[Input.Jump.GamepadIndex].Pressed(padButton, Input.Jump.Threshold)) count++;
        foreach (var mouseButton in Input.Jump.Binding.Mouse)
            if (MInput.Mouse.Pressed(mouseButton)) count++;
        return count;
    }

    private static void HandleJumpHook(Player self) {
        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.DetectFailedWaterBoost)
            return;
        if (self.CollideFirst<Water>(self.Position + Vector2.UnitY * 2f) != null) {
            if (!IsSequenceActive()) jumpInputs = CountJumpPressed(); // seed inputs on sequence start
            jumpsFired++;
        } else if (IsSequenceActive())
            Evaluate();
    }

    private static void EvaluateIfActive() {
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectFailedWaterBoost)
            if (IsSequenceActive()) Evaluate();
    }

    private static void OnJump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playSfx) {
        HandleJumpHook(self);
        orig(self, particles, playSfx);
    }

    private static void OnSuperJump(On.Celeste.Player.orig_SuperJump orig, Player self) {
        HandleJumpHook(self);
        orig(self);
    }

    private static void OnWallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir) {
        HandleJumpHook(self);
        orig(self, dir);
    }

    private static void OnSuperWallJump(On.Celeste.Player.orig_SuperWallJump orig, Player self, int dir) {
        HandleJumpHook(self);
        orig(self, dir);
    }

    private static PlayerDeadBody OnDie(On.Celeste.Player.orig_Die orig, Player self,
        Vector2 direction, bool evenIfInvincible, bool registerDeathInStats) {
        var body = orig(self, direction, evenIfInvincible, registerDeathInStats);
        EvaluateIfActive();
        return body;
    }

    private static void OnTransition(On.Celeste.Player.orig_OnTransition orig, Player self) {
        orig(self);
        EvaluateIfActive();
    }

    private static void OnUpdate(On.Celeste.Level.orig_Update orig, Level self) {
        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.DetectFailedWaterBoost) {
            orig(self);
            return;
        }

        // Read jump input BEFORE orig — MInput.Pressed is true for exactly 1 frame
        var player = self.Tracker.GetEntity<Player>();
        bool onSurface = player != null
            && player.StateMachine.State != Player.StSwim // StSwim = fully submerged, not on surface
            && player.CollideFirst<Water>(player.Position + Vector2.UnitY * 2f) != null;

        if (onSurface && !self.Paused && IsSequenceActive()) {
            int pressed = CountJumpPressed();
            if (pressed > 0) jumpInputs += pressed;
        }

        orig(self);

        if (player != null && IsSequenceActive()) {
            int state = player.StateMachine.State;

            // Immediate end: landed, or entered a state that ends the waterboost context
            if (state == Player.StSwim) // Submerged: end sequence silently, no alert
                Reset();
            else if (player.onGround
                || (state != Player.StNormal
                    && state != Player.StPickup
                    && state != Player.StAttract))
                Evaluate();

            // Start countdown when player leaves surface
            else if (state != Player.StSwim && wasOnSurface && !onSurface && framesUntilEnd < 0)
                framesUntilEnd = 6;
        }

        // Tick countdown
        if (framesUntilEnd > 0)
            framesUntilEnd--;
        else if (framesUntilEnd == 0)
            Evaluate();

        wasOnSurface = onSurface;
    }
}
