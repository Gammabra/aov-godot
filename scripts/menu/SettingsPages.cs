using System;
using Godot;
using Godot.Collections;

public partial class SettingsPages : Node
{
    // =================================================
    // VALUES
    // =================================================

    // ---------- SUBTITLE ----------
    public bool subtitles_enabled = false;
    public float subtitle_size = 30.0f;
    public int subtitle_font_index = 0;
    public int subtitle_text_color = 0;
    public int subtitle_bg_color = 0;
    public int subtitle_language = 0;
    public float subtitle_opacity = 50.0f;

    // ---------- VIDEO ----------
    public float contrast = 50.0f;
    public float brightness = 50.0f;
    public bool animations_enabled = false;
    public int resolution_index = 0;
    public Vector2I resolution = new Vector2I(1920, 1080);
    public int window_mode = 0;
    public int texture_quality = 0;

    // ---------- VISUAL ----------
    public float interface_size = 1.0f;
    public bool blurry_enabled = false;
    public bool camera_shake_enabled = false;
    public bool visual_indicators_enabled = false;
    public int color_blindness = 0; // 0 = none, 1 = protanopia, 2 = deuteranopia, 3 = tritanopia, 4 = achromatopsia

    // ---------- COMMAND ----------
    private Node? _page_command;

    private string _waiting_action = "";
    private Button? _waiting_button = null;
    private string _waiting_action_pad = "";
    private Button? _waiting_button_pad = null;

    public Dictionary actions = new Dictionary
    {
        { "move_up", "move_up" },
        { "move_down", "move_down" },
        { "move_left", "move_left" },
        { "move_right", "move_right" },
        { "battle_move_unit_to", "battle_move_unit_to" },
        { "battle_select_skill1", "battle_select_skill1" },
        { "battle_select_skill2", "battle_select_skill2" },
        { "battle_select_skill3", "battle_select_skill3" },
        { "battle_select_skill4", "battle_select_skill4" },
        { "battle_select_skill5", "battle_select_skill5" },
        { "battle_pass_turn", "battle_pass_turn" },
        { "toggle_options", "toggle_options" }
    };

    // ---------- AUDIO ----------
    public float master_volume = 50.0f;
    public float music_volume = 50.0f;
    public float voices_volume = 50.0f;
    public float sfx_volume = 50.0f;

    // =================================================
    // READY
    // =================================================

    public override void _Ready()
    {
        _page_command = GetNodeOrNull("PageCommand");

        if (_page_command == null)
        {
            CallDeferred(nameof(UpdateSubtitlePreview));
            return;
        }

        foreach (string action in actions.Keys)
        {
            var label = _page_command.GetNode<Label>((string)actions[action]);
            var btn = label.GetNode<Button>("Button");

            btn.Text = GetActionKey(action);
            btn.Pressed += () => OnRebindPressed(action, btn);
        }

        foreach (string action in actions.Keys)
        {
            var label = _page_command.GetNode<Label>((string)actions[action]);
            var btn2 = label.GetNode<Button>("Button2");

            btn2.Text = GetActionKeyPad(action);
            btn2.Pressed += () => OnRebindPadPressed(action, btn2);
        }

        var resetBtn = _page_command.GetNodeOrNull<Button>("ButtonReset");
        if (resetBtn != null)
            resetBtn.Pressed += OnResetCommandsPressed;

        CallDeferred(nameof(UpdateSubtitlePreview));
    }

    // =================================================
    // SUBTITLE
    // =================================================

    public void OnSubtitlesToggled(bool enabled) { subtitles_enabled = enabled; UpdateSubtitlePreview(); }
    public void OnSubtitleSizeChanged(double value) { subtitle_size = (float)value; UpdateSubtitlePreview(); }
    public void OnSubtitleFontItemSelected(long index) { subtitle_font_index = (int)index; UpdateSubtitlePreview(); }
    public void OnSubtitleTextColorChanged(long index) { subtitle_text_color = (int)index; UpdateSubtitlePreview(); }
    public void OnSubtitleBgColorChanged(long index) { subtitle_bg_color = (int)index; UpdateSubtitlePreview(); }
    public void OnSubtitleOpacityChanged(double value) { subtitle_opacity = (float)value; UpdateSubtitlePreview(); }
    public void OnSubtitleLanguageItemSelected(long index) { subtitle_language = (int)index; UpdateSubtitlePreview(); }

    private void UpdateSubtitlePreview()
    {
        var label = GetNodeOrNull<Label>("PageSubtitle/ExampleLabel");
        if (label == null) return;

        label.Visible = true;
        var style = new StyleBoxFlat();
        style.BorderColor = Colors.Red;
        style.SetBorderWidthAll(2);
        if (!subtitles_enabled)
        {
            style.BgColor = new Color(0, 0, 0, 0);
            label.AddThemeColorOverride("font_color", new Color(0, 0, 0, 0));
            label.AddThemeStyleboxOverride("normal", style);
            return;
        }

        label.AddThemeFontSizeOverride("font_size", (int)subtitle_size);

        float alpha = subtitle_opacity / 100.0f;

        Color[] textColors = { Colors.White, Colors.Red, Colors.Green };
        Color textColor = textColors[subtitle_text_color];
        label.AddThemeColorOverride("font_color", textColor);

        if (subtitle_bg_color == 4)
        {
            style.BgColor = new Color(0, 0, 0, 0);
        }
        else
        {
            Color[] bgColors = { Colors.White, Colors.Red, Colors.Green, Colors.Black };
            Color bgColor = bgColors[subtitle_bg_color];
            bgColor.A = alpha;
            style.BgColor = bgColor;
        }

        label.AddThemeStyleboxOverride("normal", style);

        var fontBtn = GetNodeOrNull<OptionButton>("PageSubtitle/Font/OptionButton");
        if (fontBtn != null && fontBtn.ItemCount > 0 && subtitle_font_index < fontBtn.ItemCount)
        {
            var font = fontBtn.GetItemMetadata(subtitle_font_index).As<FontFile>();
            if (font != null) label.AddThemeFontOverride("font", font);
        }

        string[] previewTexts = { "Subtitle", "Sous-Titre" };
        label.Text = subtitle_language < previewTexts.Length ? previewTexts[subtitle_language] : "Subtitle";
    }

    // =================================================
    // VIDEO
    // =================================================

    public void OnContrastChanged(double value) => contrast = (float)value;
    public void OnBrightnessChanged(double value) => brightness = (float)value;
    public void OnAnimationsToggled(bool enabled) => animations_enabled = enabled;

    public void OnResolutionItemSelected(long index)
    {
        resolution_index = (int)index;

        switch (index)
        {
            case 0: resolution = new Vector2I(1920, 1080); break;
            case 1: resolution = new Vector2I(1280, 720); break;
        }

        DisplayServer.WindowSetSize(resolution);
    }

    public void OnWindowModeItemSelected(long index)
    {
        window_mode = (int)index;
        switch (index)
        {
            case 0: DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed); break;
            case 1: DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen); break;
        }
    }

    public void OnTextureItemSelected(long index) => texture_quality = (int)index;

    // =================================================
    // VISUAL
    // =================================================

    public void OnInterfaceSizeChanged(double value) => interface_size = (float)value;
    public void OnBlurryToggled(bool enabled) => blurry_enabled = enabled;
    public void OnCameraShakeToggled(bool enabled) => camera_shake_enabled = enabled;
    public void OnVisualIndicatorsToggled(bool enabled) => visual_indicators_enabled = enabled;

    public void OnColorBlindnessItemSelected(long index)
    {
        color_blindness = (int)index;
    }

    // =================================================
    // COMMAND
    // =================================================

    private void OnRebindPressed(string action, Button btn)
    {
        if (_waiting_button_pad != null)
        {
            var prevBtn = _waiting_button_pad;
            var prevAction = _waiting_action_pad;
            _waiting_action_pad = "";
            _waiting_button_pad = null;
            prevBtn.Text = GetActionKeyPad(prevAction);
        }

        _waiting_action = action;
        _waiting_button = btn;
        btn.Text = "Press key...";
        btn.ReleaseFocus();
    }

    public override void _Input(InputEvent @event)
    {
        if (_waiting_action != "")
        {
            if (@event is InputEventKey key && key.Pressed)
                BindEvent(@event);
            if (@event is InputEventMouseButton mouse && mouse.Pressed)
                BindEvent(@event);
        }

        if (_waiting_action_pad != "")
        {
            if (@event is InputEventJoypadButton pad && pad.Pressed)
                BindPadEvent(@event);
            if (@event is InputEventJoypadMotion motion && Mathf.Abs(motion.AxisValue) > 0.8f)
                BindPadEvent(@event);
        }
    }

    private void BindEvent(InputEvent @event)
    {
        if (@event is InputEventKey key)
        {
            Key kc = key.PhysicalKeycode;
            if (kc == Key.Print ||
                kc == Key.Meta || kc == Key.Menu ||
                kc == Key.Pause || kc == Key.Scrolllock)
                return;
        }

        var events = InputMap.ActionGetEvents(_waiting_action);
        foreach (var ev in new Godot.Collections.Array<InputEvent>(events))
        {
            if (ev is InputEventKey || ev is InputEventMouseButton)
                InputMap.ActionEraseEvent(_waiting_action, ev);
        }

        InputMap.ActionAddEvent(_waiting_action, @event);

        _waiting_button.Text = GetActionKey(_waiting_action);
        _waiting_action = "";
        _waiting_button = null;
    }

    private string GetActionKey(string actionName)
    {
        var events = InputMap.ActionGetEvents(actionName);
        foreach (var ev in events)
        {
            if (ev is InputEventKey key)
                return OS.GetKeycodeString(key.PhysicalKeycode);
            if (ev is InputEventMouseButton mouse)
                return "Mouse " + mouse.ButtonIndex;
        }
        return "None";
    }

    private void OnRebindPadPressed(string action, Button btn)
    {
        if (_waiting_button != null)
        {
            var prevBtn = _waiting_button;
            var prevAction = _waiting_action;
            _waiting_action = "";
            _waiting_button = null;
            prevBtn.Text = GetActionKey(prevAction);
        }

        _waiting_action_pad = action;
        _waiting_button_pad = btn;
        btn.Text = "Press button...";
    }

    public string GetActionKeyPad(string actionName)
    {
        var events = InputMap.ActionGetEvents(actionName);
        foreach (var ev in events)
        {
            if (ev is InputEventJoypadButton pad)
                return ((JoyButton)pad.ButtonIndex).ToString();
            if (ev is InputEventJoypadMotion motion)
                return "Axis " + (int)motion.Axis + (motion.AxisValue > 0 ? " +" : " -");
        }
        return "None";
    }

    private void BindPadEvent(InputEvent @event)
    {
        var events = InputMap.ActionGetEvents(_waiting_action_pad);
        foreach (var ev in new Godot.Collections.Array<InputEvent>(events))
            if (ev is InputEventJoypadButton || ev is InputEventJoypadMotion)
                InputMap.ActionEraseEvent(_waiting_action_pad, ev);

        InputMap.ActionAddEvent(_waiting_action_pad, @event);

        _waiting_button_pad.Text = GetActionKeyPad(_waiting_action_pad);
        _waiting_action_pad = "";
        _waiting_button_pad = null;

        GetViewport().SetInputAsHandled();
    }

    private void OnResetCommandsPressed()
    {
        InputMap.LoadFromProjectSettings();
        if (_page_command == null)
            return;

        foreach (string action in actions.Keys)
        {
            var label = _page_command.GetNode<Label>((string)actions[action]);
            label.GetNode<Button>("Button").Text = GetActionKey(action);
            label.GetNode<Button>("Button2").Text = GetActionKeyPad(action);
        }
    }

    // =================================================
    // AUDIO
    // =================================================

    private void SetBusVolume(string busName, float value)
    {
        int idx = AudioServer.GetBusIndex(busName);
        if (idx == -1)
            return;

        AudioServer.SetBusVolumeDb(idx, Mathf.LinearToDb(value));
    }

    public void OnMasterVolumeChanged(double value)
    {
        master_volume = (float)value;
        SetBusVolume("Master", master_volume);
    }

    public void OnMusicVolumeChanged(double value)
    {
        music_volume = (float)value;
        SetBusVolume("Music", music_volume);
    }

    public void OnVoicesVolumeChanged(double value)
    {
        voices_volume = (float)value;
        SetBusVolume("Voices", voices_volume);
    }

    public void OnSfxVolumeChanged(double value)
    {
        sfx_volume = (float)value;
        SetBusVolume("SFX", sfx_volume);
    }

    // =================================================
    // APPLY VALUES
    // =================================================

    public async void ApplyFontSelection()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);

        var fontBtn = GetNode<OptionButton>("PageSubtitle/Font/OptionButton");
        if (fontBtn.ItemCount > 0)
            fontBtn.Select(subtitle_font_index);
        UpdateSubtitlePreview();
    }

    public void ApplySettingsToUI()
    {
        GetNode<CheckBox>("PageSubtitle/Subtitles/CheckBox").ButtonPressed = subtitles_enabled;
        GetNode<HSlider>("PageSubtitle/Size/HSlider").Value = subtitle_size;
        GetNode<OptionButton>("PageSubtitle/TextColor/OptionButton").Select(subtitle_text_color);
        GetNode<OptionButton>("PageSubtitle/BgColor/OptionButton").Select(subtitle_bg_color);
        GetNode<HSlider>("PageSubtitle/Opacity/HSlider").Value = subtitle_opacity;
        GetNode<OptionButton>("PageSubtitle/Langage/OptionButton").Select(subtitle_language);

        GetNode<HSlider>("PageVideo/Contrast/HSlider").Value = contrast;
        GetNode<HSlider>("PageVideo/Brightness/HSlider").Value = brightness;
        GetNode<CheckBox>("PageVideo/Animations/CheckBox").ButtonPressed = animations_enabled;
        GetNode<OptionButton>("PageVideo/Resolution/OptionButton").Select(resolution_index);
        GetNode<OptionButton>("PageVideo/WindowMode/OptionButton").Select(window_mode);
        GetNode<OptionButton>("PageVideo/Texture/OptionButton").Select(texture_quality);

        GetNode<HSlider>("PageVisual/InterfaceSize/HSlider").Value = interface_size;
        GetNode<CheckBox>("PageVisual/Blurry/CheckBox").ButtonPressed = blurry_enabled;
        GetNode<CheckBox>("PageVisual/CameraShake/CheckBox").ButtonPressed = camera_shake_enabled;
        GetNode<CheckBox>("PageVisual/VisualIndicators/CheckBox").ButtonPressed = visual_indicators_enabled;
        GetNode<OptionButton>("PageVisual/ColorBlindness/OptionButton").Select(color_blindness);

        ApplyAudioToUi();
        OnWindowModeItemSelected(window_mode);
        UpdateSubtitlePreview();
    }

    public void ApplyAudioToUi()
    {
        GetNode<HSlider>("PageAudio/Master/HSlider").Value = master_volume;
        GetNode<HSlider>("PageAudio/Music/HSlider").Value = music_volume;
        GetNode<HSlider>("PageAudio/Voices/HSlider").Value = voices_volume;
        GetNode<HSlider>("PageAudio/SFX/HSlider").Value = sfx_volume;

        SetBusVolume("Master", master_volume);
        SetBusVolume("Music", music_volume);
        SetBusVolume("Voices", voices_volume);
        SetBusVolume("SFX", sfx_volume);
    }
}
