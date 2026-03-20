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
        On.Celeste.Player.DashBegin    += OnDashBegin;
        On.Celeste.Player.Boost        += OnBoost;
        On.Celeste.Player.RedBoost     += OnRedBoost;
        On.Celeste.Player.ClimbBegin   += OnClimbBegin;
        On.Celeste.Player.SuperJump     += OnSuperJump;
        On.Celeste.Player.WallJump      += OnWallJump;
        On.Celeste.Player.SuperWallJump += OnSuperWallJump;
        On.Celeste.Player.Die           += OnDie;
        On.Celeste.Player.OnTransition += OnTransition;
        On.Celeste.Level.Update        += OnUpdate;
    }

    public static void Unload() {
        On.Celeste.Player.Jump         -= OnJump;
        On.Celeste.Player.DashBegin    -= OnDashBegin;
        On.Celeste.Player.Boost        -= OnBoost;
        On.Celeste.Player.RedBoost     -= OnRedBoost;
        On.Celeste.Player.ClimbBegin   -= OnClimbBegin;
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
        jumpInputs     = 0;
        jumpsFired     = 0;
        framesUntilEnd = -1;
        wasOnSurface   = false;
    }

    private static bool IsSequenceActive() => jumpInputs > 0 || framesUntilEnd >= 0;

    private static int CountJumpPressed() {
        int count = 0;
        foreach (var key in Input.Jump.Binding.Keyboard)
            if (MInput.Keyboard.Pressed(key)) count++;
        foreach (var padButton in Input.Jump.Binding.Controller)
            if (MInput.GamePads[Input.Jump.GamepadIndex].Pressed(padButton, Input.Jump.Threshold)) count++;
        foreach (var mouseButton in Input.Jump.Binding.Mouse) // Just in case someone uses mouse (wtf)
            if (MInput.Mouse.Pressed(mouseButton)) count++;
        return count;
    }

    private static void OnJump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playSfx) {
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectFailedWaterBoost) {
            if (self.CollideFirst<Water>(self.Position + Vector2.UnitY * 2f) != null)
                jumpsFired++;
            else if (IsSequenceActive())
                Evaluate();
        }
        orig(self, particles, playSfx);
    }

    private static void OnSuperJump(On.Celeste.Player.orig_SuperJump orig, Player self) {
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectFailedWaterBoost)
            if (IsSequenceActive()) jumpsFired++;
        orig(self);
    }

    private static void OnWallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir) {
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectFailedWaterBoost)
            if (IsSequenceActive()) jumpsFired++;
        orig(self, dir);
    }

    private static void OnSuperWallJump(On.Celeste.Player.orig_SuperWallJump orig, Player self, int dir) {
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectFailedWaterBoost)
            if (IsSequenceActive()) jumpsFired++;
        orig(self, dir);
    }

    private static void OnDashBegin(On.Celeste.Player.orig_DashBegin orig, Player self) {
        orig(self);
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectFailedWaterBoost)
            if (IsSequenceActive()) Evaluate();
    }

    private static void OnBoost(On.Celeste.Player.orig_Boost orig, Player self, Booster booster) {
        orig(self, booster);
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectFailedWaterBoost)
            if (IsSequenceActive()) Evaluate();
    }

    private static void OnRedBoost(On.Celeste.Player.orig_RedBoost orig, Player self, Booster booster) {
        orig(self, booster);
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectFailedWaterBoost)
            if (IsSequenceActive()) Evaluate();
    }

    private static void OnClimbBegin(On.Celeste.Player.orig_ClimbBegin orig, Player self) {
        orig(self);
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectFailedWaterBoost)
            if (IsSequenceActive()) Evaluate();
    }

    private static PlayerDeadBody OnDie(On.Celeste.Player.orig_Die orig, Player self,
        Vector2 direction, bool evenIfInvincible, bool registerDeathInStats) {
        var body = orig(self, direction, evenIfInvincible, registerDeathInStats);
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectFailedWaterBoost)
            if (IsSequenceActive()) Evaluate();
        return body;
    }

    private static void OnTransition(On.Celeste.Player.orig_OnTransition orig, Player self) {
        orig(self);
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectFailedWaterBoost)
            if (IsSequenceActive()) Evaluate();
    }

    private static void OnUpdate(On.Celeste.Level.orig_Update orig, Level self) {
        if (!AxiomeToolboxModule.Settings.Enabled || !AxiomeToolboxModule.Settings.DetectFailedWaterBoost) {
            orig(self);
            return;
        }

        // Read jump input BEFORE orig (before Player.Update consumes the buffer)
        var player = self.Tracker.GetEntity<Player>();
        bool onSurface = player != null
            && player.StateMachine.State != Player.StSwim
            && player.CollideFirst<Water>(player.Position + Vector2.UnitY * 2f) != null;

        if (onSurface && !self.Paused) {
            int pressed = CountJumpPressed();
            if (pressed > 0) jumpInputs += pressed;
        }

        orig(self);

        // Immediate end: landed, or entered a state that ends the waterboost context
        if (player != null && IsSequenceActive()) {
            int state = player.StateMachine.State;
            if (state == Player.StSwim)
                Reset();
            else if (player.onGround
                || state == Player.StDummy
                || state == Player.StStarFly
                || state == Player.StLaunch
                || state == Player.StFlingBird
                || state == Player.StCassetteFly
                || state == Player.StRedDash)
                Evaluate();
        }

        // Start countdown when player leaves surface
        if (player.StateMachine.State != Player.StSwim && wasOnSurface && !onSurface && framesUntilEnd < 0)
            framesUntilEnd = 6;

        // Tick countdown
        if (framesUntilEnd > 0)
            framesUntilEnd--;
        else if (framesUntilEnd == 0)
            Evaluate();

        wasOnSurface = onSurface;
    }
}
