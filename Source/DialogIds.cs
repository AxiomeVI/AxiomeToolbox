namespace Celeste.Mod.AxiomeToolbox;
public static class DialogIds {
    // Menu
    public const string AxiomeToolboxId = "AT_MOD";
    public const string EnabledId = "AT_ENABLED";
    public const string StopTimerWhenPausedId = "AT_STOP_TIMER_WHEN_PAUSED";
    public const string ClearCheckpointId = "AT_CLEAR_CHECKPOINT";
    public const string PlaceCheckpointId = "AT_PLACE_CHECKPOINT";
    public const string KeybindComboSubId  = "AT_KEYBIND_COMBO_SUB";
    public const string CheckpointColorId = "AT_CHECKPOINT_COLOR";
    public const string CheckpointColorSubTextId = "AT_CHECKPOINT_COLOR_SUB";
    public const string KeybindConfigId = "AT_KEYBIND_CONFIG";

    // Vanilla Celeste keybind UI keys (used in KeybindConfigUi)
    public const string KeyConfigTitle      = "KEY_CONFIG_TITLE";
    public const string BtnConfigTitle      = "BTN_CONFIG_TITLE";
    public const string KeyConfigChanging   = "KEY_CONFIG_CHANGING";
    public const string BtnConfigChanging   = "BTN_CONFIG_CHANGING";
    public const string BtnConfigNoController = "BTN_CONFIG_NOCONTROLLER";

    // Settings — detectors
    public const string DetectionRulesId        = "AT_DETECTION_RULES";
    public const string DetectBadCornerBoostId  = "AT_DETECT_BAD_CORNER_BOOST";
    public const string DetectLostDeathFramesId = "AT_DETECT_LOST_DEATH_FRAMES";
    public const string DetectMenuTimingLossId  = "AT_DETECT_MENU_TIMING_LOSS";
    public const string DetectFailedWaterBoostId = "AT_DETECT_FAILED_WATER_BOOST";

    // Timeline
    public const string TimelineSubmenuId    = "AT_TIMELINE";
    public const string TimelinePositionId   = "AT_TIMELINE_POSITION";
    public const string TimelineWindowSizeId = "AT_TIMELINE_WINDOW_SIZE";
    public const string TimelineInspectId    = "AT_TIMELINE_INSPECT";

    // Bad Corner Boost
    public const string BadCBDetectedId = "AT_BAD_CB_DETECTED";

    // Death Confirm
    public const string LostDeathFramesId       = "AT_LOST_DEATH_FRAMES";
    public const string LostDeathFramesPluralId = "AT_LOST_DEATH_FRAMES_PLURAL";

    // Menu Timing
    public const string LostMenuFramesId       = "AT_LOST_MENU_FRAMES";
    public const string LostMenuFramesPluralId = "AT_LOST_MENU_FRAMES_PLURAL";

    // Water Boost
    public const string FailedWaterBoostId       = "AT_FAILED_WATER_BOOST";
    public const string FailedWaterBoostPluralId = "AT_FAILED_WATER_BOOST_PLURAL";
}
