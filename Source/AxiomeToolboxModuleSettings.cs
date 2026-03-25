using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.AxiomeToolbox;
[SettingName(DialogIds.AxiomeToolboxId)]
public class AxiomeToolboxModuleSettings : EverestModuleSettings {

    public bool Enabled { get; set; } = true;
    public bool StopTimerWhenPaused { get; set; } = false;

    [SettingName(DialogIds.PlaceCheckpointId)]
    [SettingSubText(DialogIds.KeybindComboSubId)]
    [DefaultButtonBinding(0, Keys.None)]
    public ButtonBinding PlaceCheckpoint { get; set; }

    [SettingName(DialogIds.ClearCheckpointId)]
    [SettingSubText(DialogIds.KeybindComboSubId)]
    [DefaultButtonBinding(0, Keys.None)]
    public ButtonBinding ClearCheckpoints { get; set; }

    [SettingName(DialogIds.CheckpointColorId)]
    [SettingSubText(DialogIds.CheckpointColorSubTextId)]
    public string CheckpointColor { get; set; } = "00FFFF";  // Cyan

    [SettingName(DialogIds.DetectBadCornerBoostId)]
    public bool DetectBadCornerBoost { get; set; } = true;

    [SettingName(DialogIds.DetectLostDeathFramesId)]
    public bool DetectLostDeathFrames { get; set; } = true;

    [SettingName(DialogIds.DetectMenuTimingLossId)]
    public bool DetectMenuTimingLoss { get; set; } = true;

    [SettingName(DialogIds.DetectFailedWaterBoostId)]
    public bool DetectFailedWaterBoost { get; set; } = true;
}
