using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.AxiomeToolbox.UI;

public record KeybindEntry(string LabelDialogId, ButtonBinding Binding);

[Tracked]
internal class KeybindConfigUi : TextMenu {
    private readonly IList<KeybindEntry> _entries;

    private bool  _closing;
    private float _inputDelay;
    private bool  _remapping;
    private float _remappingEase;
    private int   _remappingEntry;
    private bool  _remappingIsKeyboard;
    private float _timeout;

    private bool   IsRemappingKeyboard => _remappingIsKeyboard;
    private string RemappingLabel      => Dialog.Clean(_entries[_remappingEntry].LabelDialogId);

    private static readonly Buttons[] AllButtons = {
        Buttons.A, Buttons.B, Buttons.X, Buttons.Y,
        Buttons.LeftShoulder, Buttons.RightShoulder,
        Buttons.LeftTrigger, Buttons.RightTrigger,
        Buttons.Back, Buttons.Start,
        Buttons.LeftStick, Buttons.RightStick,
        Buttons.DPadUp, Buttons.DPadDown, Buttons.DPadLeft, Buttons.DPadRight,
    };

    public KeybindConfigUi(IList<KeybindEntry> entries) {
        _entries = entries;
        Reload();
        OnESC = OnCancel = () => { Focused = false; _closing = true; };
        MinWidth = 600f;
        Position.Y = ScrollTargetY;
        Alpha = 0f;
    }

    private void Reload(int index = -1) {
        Clear();
        Add(new Header(Dialog.Clean(DialogIds.KeybindConfigId)));

        Add(new SubHeader(Dialog.Clean(DialogIds.KeyConfigTitle)));
        for (int i = 0; i < _entries.Count; i++) {
            int ei = i;
            Add(new Setting(Dialog.Clean(_entries[i].LabelDialogId), _entries[i].Binding.Keys)
                .Pressed(() => StartRemap(ei, true)));
        }

        Add(new SubHeader(Dialog.Clean(DialogIds.BtnConfigTitle)));
        for (int i = 0; i < _entries.Count; i++) {
            int ei = i;
            Add(new Setting(Dialog.Clean(_entries[i].LabelDialogId), _entries[i].Binding.Buttons)
                .Pressed(() => StartRemap(ei, false)));
        }

        if (index >= 0) Selection = index;
    }

    private void StartRemap(int entryIndex, bool isKeyboard) {
        _remapping = true;
        _remappingEntry = entryIndex;
        _remappingIsKeyboard = isKeyboard;
        _timeout = 5f;
        Focused = false;
    }

    private void ApplyRemap<T>(T input, List<T> list) {
        _remapping = false;
        _inputDelay = 0.25f;
        if (!list.Remove(input)) list.Add(input);
        Reload(Selection);
    }

    private void ApplyRemap(Keys key) =>
        ApplyRemap(key, _entries[_remappingEntry].Binding.Keys);

    private void ApplyRemap(Buttons button) =>
        ApplyRemap(button, _entries[_remappingEntry].Binding.Buttons);

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
            } else if (IsRemappingKeyboard) {
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

        if (IsRemappingKeyboard || Input.GuiInputController()) {
            ActiveFont.Draw(
                Dialog.Clean(DialogIds.KeybindComboSubId),
                pos + new Vector2(0f, -32f),
                new Vector2(0.5f, 2f), Vector2.One * 0.7f,
                Color.LightGray * Ease.CubeIn(_remappingEase));
            ActiveFont.Draw(
                Dialog.Clean(IsRemappingKeyboard ? DialogIds.KeyConfigChanging : DialogIds.BtnConfigChanging),
                pos + new Vector2(0f, -8f),
                new Vector2(0.5f, 1f), Vector2.One * 0.7f,
                Color.LightGray * Ease.CubeIn(_remappingEase));
            ActiveFont.Draw(
                RemappingLabel,
                pos + new Vector2(0f, 8f),
                new Vector2(0.5f, 0f), Vector2.One * 2f,
                Color.White * Ease.CubeIn(_remappingEase));
        } else {
            ActiveFont.Draw(
                Dialog.Clean(DialogIds.BtnConfigNoController),
                pos, new Vector2(0.5f, 0.5f), Vector2.One,
                Color.White * Ease.CubeIn(_remappingEase));
        }
    }
}
