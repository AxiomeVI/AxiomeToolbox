using Celeste.Mod.AxiomeToolbox.Integration;

namespace Celeste.Mod.AxiomeToolbox.DeathConfirm;

public static class DeathConfirmDetector {

    private static long startTime;
    private static bool isTracking;

    public static void Load() {
        On.Celeste.Player.Die         += OnDie;
        On.Celeste.PlayerDeadBody.End += OnEnd;
    }

    public static void Unload() {
        On.Celeste.Player.Die         -= OnDie;
        On.Celeste.PlayerDeadBody.End -= OnEnd;
    }

    public static void Reset() {
        isTracking = false;
        startTime  = 0;
    }

    private static PlayerDeadBody OnDie(On.Celeste.Player.orig_Die orig, Player self,
        Microsoft.Xna.Framework.Vector2 direction, bool evenIfInvincible, bool registerDeathInStats) {
        var body = orig(self, direction, evenIfInvincible, registerDeathInStats);
        // body is null when player is invincible — death didn't register, don't track
        if (body != null && AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectLostDeathFrames) {
            startTime  = 170000 + RoomTimerIntegration.GetRoomTime();
            isTracking = true;
        }
        return body;
    }

    private static void OnEnd(On.Celeste.PlayerDeadBody.orig_End orig, PlayerDeadBody self) {
        if (AxiomeToolboxModule.Settings.Enabled && AxiomeToolboxModule.Settings.DetectLostDeathFrames) {
            if (isTracking) {
                int lost = (int)((RoomTimerIntegration.GetRoomTime() - startTime) / 170000);
                if (lost > 0)
                    NotificationUtils.ShowFrameLoss(DialogIds.LostDeathFramesId, DialogIds.LostDeathFramesPluralId, lost);
            }
            isTracking = false;
        }
        orig(self);
    }
}
