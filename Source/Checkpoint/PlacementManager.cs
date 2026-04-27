using System;
using System.Collections.Generic;
using Celeste.Mod.AxiomeToolbox.Hotkeys;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.AxiomeToolbox.Checkpoint {

    public static class CheckpointPlacementManager {

        private static readonly Dictionary<string, List<AxiomeCheckpointData>> placements = [];
        private static readonly List<CheckpointTrigger> _activeTriggers = [];

        private static ComboHotkey _placeHotkey;
        private static ComboHotkey _clearHotkey;

        public static void Load() {
            On.Celeste.Level.LoadLevel += OnLoadLevel;
            On.Celeste.Level.Update    += OnUpdate;
            On.Celeste.Level.End       += OnLevelEnd;
            _placeHotkey = new ComboHotkey(AxiomeToolboxModule.Settings.PlaceCheckpoint);
            _clearHotkey = new ComboHotkey(AxiomeToolboxModule.Settings.ClearCheckpoints);
        }

        public static void Unload() {
            On.Celeste.Level.LoadLevel -= OnLoadLevel;
            On.Celeste.Level.Update    -= OnUpdate;
            On.Celeste.Level.End       -= OnLevelEnd;
            _placeHotkey = null;
            _clearHotkey = null;
        }

        public static void ClearAll() {
            placements.Clear();
            foreach (var trigger in _activeTriggers)
                trigger.RemoveSelf();
            _activeTriggers.Clear();
            Audio.Play("event:/ui/main/button_select");
        }

        public static void ResetAllTriggeredStates() {
            foreach (var room in placements.Values) {
                foreach (var data in room) {
                    data.IsTriggered = false;
                }
            }
        }

        public static void Update(Level level) {
            if (!AxiomeToolboxModule.Settings.Enabled) return;

            ComboHotkey.UpdateStates();
            _placeHotkey.Update();
            _clearHotkey.Update();

            if (_placeHotkey.Pressed) PlaceCheckpointAtPlayer(level);
            if (_clearHotkey.Pressed) ClearAll();
        }

        public static void PlaceCheckpointAtPlayer(Level level) {
            Player player = level.Tracker.GetEntity<Player>();
            if (player == null) return;

            string roomID = GetRoomID(level);
            Vector2 pos = player.Position;

            if (!placements.ContainsKey(roomID))
                placements[roomID] = [];

            var data = new AxiomeCheckpointData { Position = pos, IsTriggered = false };
            placements[roomID].Add(data);

            var trigger = new CheckpointTrigger(GetCheckpointColor(), data);
            level.Add(trigger);
            _activeTriggers.Add(trigger);

            Audio.Play("event:/ui/main/button_select");
        }

        // Called by CheckpointTrigger.Removed so _activeTriggers stays in sync with
        // entities the engine removes on room transitions.
        public static void UntrackTrigger(CheckpointTrigger trigger) =>
            _activeTriggers.Remove(trigger);

        private static void OnUpdate(On.Celeste.Level.orig_Update orig, Level self) {
            orig(self);
            if (AxiomeToolboxModule.Settings.Enabled) Update(self);
        }

        private static void OnLevelEnd(On.Celeste.Level.orig_End orig, Level self) {
            orig(self);
            if (AxiomeToolboxModule.Settings.Enabled) ClearAll();
        }

        private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);
            if (AxiomeToolboxModule.Settings.Enabled) PlaceCheckpointInLevel(self);
        }

        public static void RemoveCheckpointEntitiesFromLevel() {
            foreach (var trigger in _activeTriggers)
                trigger.RemoveSelf();
            _activeTriggers.Clear();
        }

        public static void PlaceCheckpointInLevel(Level level) {
            string roomID = GetRoomID(level);
            if (placements.TryGetValue(roomID, out var checkpoints)) {
                Color color = GetCheckpointColor();
                foreach (var data in checkpoints) {
                    var trigger = new CheckpointTrigger(color, data);
                    level.Add(trigger);
                    _activeTriggers.Add(trigger);
                }
            }
        }

        private static Color GetCheckpointColor() =>
            Calc.HexToColor(AxiomeToolboxModule.Settings.CheckpointColor ?? "00FFFF");

        private static string GetRoomID(Level level) {
            AreaKey area = level.Session.Area;
            return $"{area.SID ?? area.ID.ToString()}:{level.Session.Level}";
        }

        public static void OnSaveState(Dictionary<Type, Dictionary<string, object>> _, Level level) {
            ResetAllTriggeredStates();
            PlaceCheckpointInLevel(level);
        }

        public static void OnLoadState(Dictionary<Type, Dictionary<string, object>> _, Level level) {
            ResetAllTriggeredStates();
            PlaceCheckpointInLevel(level);
        }

        public static void OnBeforeSaveState(Level _) {
            ResetAllTriggeredStates();
            RemoveCheckpointEntitiesFromLevel();
        }

        public class AxiomeCheckpointData {
            public Vector2 Position { get; set; }
            public bool IsTriggered { get; set; }
        }
    }
}
