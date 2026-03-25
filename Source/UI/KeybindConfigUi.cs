using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System.Linq;

namespace Celeste.Mod.AxiomeToolbox.UI;

[Tracked]
internal class KeybindConfigUi : TextMenu {
    private enum Slot { PlaceKeyboard, ClearKeyboard, PlaceController, ClearController }

    private bool _closing;
    private float _inputDelay;
    private bool _remapping;
    private float _remappingEase;
    private bool _remappingKeyboard;
    private Slot _remappingSlot;
    private float _timeout;

    private static readonly Buttons[] AllButtons = {
        Buttons.A, Buttons.B, Buttons.X, Buttons.Y,
        Buttons.LeftShoulder, Buttons.RightShoulder,
        Buttons.LeftTrigger, Buttons.RightTrigger,
        Buttons.Back, Buttons.Start,
        Buttons.LeftStick, Buttons.RightStick,
        Buttons.DPadUp, Buttons.DPadDown, Buttons.DPadLeft, Buttons.DPadRight,
    };

    public KeybindConfigUi() {
        Reload();
        OnESC = OnCancel = () => { Focused = false; _closing = true; };
        MinWidth = 600f;
        Position.Y = ScrollTargetY;
        Alpha = 0f;
    }

    private void Reload(int index = -1) {
        Clear();
        var s = AxiomeToolboxModule.Settings;

        Add(new Header(Dialog.Clean(DialogIds.KeybindConfigId)));

        Add(new SubHeader(Dialog.Clean("KEY_CONFIG_TITLE")));
        Add(new Setting(Dialog.Clean(DialogIds.PlaceCheckpointId), s.PlaceCheckpoint.Keys)
            .Pressed(() => StartRemap(Slot.PlaceKeyboard)));
        Add(new Setting(Dialog.Clean(DialogIds.ClearCheckpointId), s.ClearCheckpoints.Keys)
            .Pressed(() => StartRemap(Slot.ClearKeyboard)));

        Add(new SubHeader(Dialog.Clean("BTN_CONFIG_TITLE")));
        Add(new Setting(Dialog.Clean(DialogIds.PlaceCheckpointId), s.PlaceCheckpoint.Buttons)
            .Pressed(() => StartRemap(Slot.PlaceController)));
        Add(new Setting(Dialog.Clean(DialogIds.ClearCheckpointId), s.ClearCheckpoints.Buttons)
            .Pressed(() => StartRemap(Slot.ClearController)));

        if (index >= 0) Selection = index;
    }

    private void StartRemap(Slot slot) {
        _remapping = true;
        _remappingKeyboard = slot is Slot.PlaceKeyboard or Slot.ClearKeyboard;
        _remappingSlot = slot;
        _timeout = 5f;
        Focused = false;
    }

    private void ApplyRemap(Keys key) {
        _remapping = false;
        _inputDelay = 0.25f;
        var list = _remappingSlot is Slot.PlaceKeyboard
            ? AxiomeToolboxModule.Settings.PlaceCheckpoint.Keys
            : AxiomeToolboxModule.Settings.ClearCheckpoints.Keys;
        if (list.Contains(key)) list.Remove(key);
        else list.Add(key);
        Reload(Selection);
    }

    private void ApplyRemap(Buttons button) {
        _remapping = false;
        _inputDelay = 0.25f;
        var list = _remappingSlot is Slot.PlaceController
            ? AxiomeToolboxModule.Settings.PlaceCheckpoint.Buttons
            : AxiomeToolboxModule.Settings.ClearCheckpoints.Buttons;
        if (list.Contains(button)) list.Remove(button);
        else list.Add(button);
        Reload(Selection);
    }

    public override void Update() {
        base.Update();

        if (_inputDelay > 0f && !_remapping) {
            _inputDelay -= Engine.DeltaTime;
            if (_inputDelay <= 0f) Focused = true;
        }

        _remappingEase = Calc.Approach(_remappingEase, _remapping ? 1f : 0f, Engine.DeltaTime * 4f);

        if (_remappingEase > 0.5f && _remapping) {
            if (Input.ESC.Pressed || Input.MenuCancel || _timeout <= 0f) {
                Input.ESC.ConsumePress();
                _remapping = false;
                Focused = true;
            } else if (_remappingKeyboard) {
                Keys[] pressed = MInput.Keyboard.CurrentState.GetPressedKeys();
                if (pressed?.LastOrDefault() is { } k && MInput.Keyboard.Pressed(k))
                    ApplyRemap(k);
            } else {
                var cur  = MInput.GamePads[Input.Gamepad].CurrentState;
                var prev = MInput.GamePads[Input.Gamepad].PreviousState;
                foreach (var btn in AllButtons)
                    if (cur.IsButtonDown(btn) && !prev.IsButtonDown(btn)) { ApplyRemap(btn); break; }
            }
            _timeout -= Engine.DeltaTime;
        }

        Alpha = Calc.Approach(Alpha, _closing ? 0f : 1f, Engine.DeltaTime * 8f);
        if (!_closing || Alpha > 0f) return;

        OnClose?.Invoke();
        Close();
    }

    public override void Render() {
        Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * Ease.CubeOut(Alpha));
        base.Render();
        if (_remappingEase <= 0f) return;

        Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * 0.95f * Ease.CubeInOut(_remappingEase));
        Vector2 pos = new Vector2(1920f, 1080f) * 0.5f;

        if (_remappingKeyboard || Input.GuiInputController()) {
            ActiveFont.Draw(
                Dialog.Clean(DialogIds.KeybindComboSubId),
                pos + new Vector2(0f, -32f),
                new Vector2(0.5f, 2f), Vector2.One * 0.7f,
                Color.LightGray * Ease.CubeIn(_remappingEase));
            ActiveFont.Draw(
                Dialog.Clean(_remappingKeyboard ? "KEY_CONFIG_CHANGING" : "BTN_CONFIG_CHANGING"),
                pos + new Vector2(0f, -8f),
                new Vector2(0.5f, 1f), Vector2.One * 0.7f,
                Color.LightGray * Ease.CubeIn(_remappingEase));
        } else {
            ActiveFont.Draw(
                Dialog.Clean("BTN_CONFIG_NOCONTROLLER"),
                pos, new Vector2(0.5f, 0.5f), Vector2.One,
                Color.White * Ease.CubeIn(_remappingEase));
        }
    }
}
