using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.AxiomeToolbox.Hotkeys;

/// Wraps a ButtonBinding and detects combo presses (all bound keys held simultaneously).
/// Pressed: rising-edge only (true for exactly one frame when combo activates).
/// IsDown: true every frame all bound keys/buttons are held.
/// Pattern taken from CelesteTAS Hotkeys.cs / SpeedrunTool HotkeyRebase.cs.
internal class ComboHotkey(ButtonBinding binding) {
    // Shared input states — updated once per frame by UpdateStates()
    private static KeyboardState _kbState;
    private static GamePadState _padState;

    /// Call once per frame before updating any ComboHotkey instances.
    internal static void UpdateStates() {
        _kbState = Keyboard.GetState();
        _padState = GetGamePadState();
    }

    private static GamePadState GetGamePadState() {
        for (int i = 0; i < 4; i++) {
            var state = GamePad.GetState((PlayerIndex) i);
            if (state.IsConnected) return state;
        }
        return default;
    }

    private bool Check() {
        if (binding.Keys.Count > 0 && _kbState != default && binding.Keys.All(_kbState.IsKeyDown))
            return true;
        if (binding.Buttons.Count > 0 && _padState != default && binding.Buttons.All(_padState.IsButtonDown))
            return true;
        return false;
    }

    /// Call once per frame per instance, after UpdateStates().
    public void Update() {
        bool current = Check();
        // IsDown still holds last frame's value here — use it for edge detection.
        Pressed = !IsDown && current;
        IsDown = current;
    }

    public bool Pressed { get; private set; }
    public bool IsDown  { get; private set; }
}
