using System;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class SettingsSave : Button
{
    private const string _configPath = "user://settings.cfg";

    private Node? _settings;

    public override void _Ready()
    {
        _settings = GetNodeOrNull<Node>("../MainContent");
        if (_settings == null)
            return;
        LoadSettings();
    }

    public override void _Pressed()
    {
        SaveSettings();
    }

    // ================= SAVE =================
    private void SaveSettings()
    {
        var cfg = new ConfigFile();

        if (_settings == null)
            return;

        // ---------- SUBTITLE ----------
        cfg.SetValue("subtitle", "enabled", _settings.Get("subtitles_enabled"));
        cfg.SetValue("subtitle", "size", _settings.Get("subtitle_size"));
        cfg.SetValue("subtitle", "font_index", _settings.Get("subtitle_font_index"));
        cfg.SetValue("subtitle", "text_color", _settings.Get("subtitle_text_color"));
        cfg.SetValue("subtitle", "bg_color", _settings.Get("subtitle_bg_color"));
        cfg.SetValue("subtitle", "language", _settings.Get("subtitle_language"));
        cfg.SetValue("subtitle", "opacity", _settings.Get("subtitle_opacity"));

        // ---------- VIDEO ----------
        cfg.SetValue("video", "contrast", _settings.Get("contrast"));
        cfg.SetValue("video", "brightness", _settings.Get("brightness"));
        cfg.SetValue("video", "animations", _settings.Get("animations_enabled"));
        cfg.SetValue("video", "resolution_index", _settings.Get("resolution_index"));
        cfg.SetValue("video", "resolution", _settings.Get("resolution"));
        cfg.SetValue("video", "window_mode", _settings.Get("window_mode"));
        cfg.SetValue("video", "texture_quality", _settings.Get("texture_quality"));

        // ---------- VISUAL ----------
        cfg.SetValue("visual", "interface_size", _settings.Get("interface_size"));
        cfg.SetValue("visual", "blurry", _settings.Get("blurry_enabled"));
        cfg.SetValue("visual", "camera_shake", _settings.Get("camera_shake_enabled"));
        cfg.SetValue("visual", "indicators", _settings.Get("visual_indicators_enabled"));
        cfg.SetValue("visual", "color_blindness", _settings.Get("color_blindness"));

        // ---------- INPUT ----------
        var actions = _settings?.Get("actions").As<Dictionary>();
        if (actions == null)
            return;

        foreach (string action in actions.Keys)
        {
            var events = InputMap.ActionGetEvents(action);
            foreach (var ev in events)
            {
                if (ev is InputEventKey key)
                    cfg.SetValue("input", action + "_key", new Dictionary { { "type", "key" }, { "keycode", (long)key.PhysicalKeycode } });
                else if (ev is InputEventMouseButton mouse)
                    cfg.SetValue("input", action + "_key", new Dictionary { { "type", "mouse" }, { "button", (long)mouse.ButtonIndex } });
                else if (ev is InputEventJoypadButton pad)
                    cfg.SetValue("input", action + "_pad", new Dictionary { { "type", "pad" }, { "button", (long)pad.ButtonIndex } });
                else if (ev is InputEventJoypadMotion motion)
                {
                    cfg.SetValue("input", action + "_pad", new Dictionary {
                        { "type", "motion" },
                        { "axis", (long)motion.Axis },
                        { "value", motion.AxisValue }
                    });
                }
            }
        }

        if (_settings == null)
            return;

        // ---------- AUDIO ----------
        cfg.SetValue("audio", "master", _settings.Get("master_volume"));
        cfg.SetValue("audio", "music", _settings.Get("music_volume"));
        cfg.SetValue("audio", "voices", _settings.Get("voices_volume"));
        cfg.SetValue("audio", "sfx", _settings.Get("sfx_volume"));

        cfg.Save(_configPath);
        GD.Print("SETTINGS SAVED");
    }

    // ================= LOAD =================
    private void LoadSettings()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(_configPath) != Error.Ok)
            return;

        if (_settings == null)
            return;

        // ---------- SUBTITLE ----------
        _settings.Set("subtitles_enabled", cfg.GetValue("subtitle", "enabled", _settings.Get("subtitles_enabled")));
        _settings.Set("subtitle_size", cfg.GetValue("subtitle", "size", _settings.Get("subtitle_size")));
        _settings.Set("subtitle_font_index", cfg.GetValue("subtitle", "font_index", _settings.Get("subtitle_font_index")));
        _settings.Set("subtitle_text_color", cfg.GetValue("subtitle", "text_color", _settings.Get("subtitle_text_color")));
        _settings.Set("subtitle_bg_color", cfg.GetValue("subtitle", "bg_color", _settings.Get("subtitle_bg_color")));
        _settings.Set("subtitle_language", cfg.GetValue("subtitle", "language", _settings.Get("subtitle_language")));
        _settings.Set("subtitle_opacity", cfg.GetValue("subtitle", "opacity", _settings.Get("subtitle_opacity")));

        // ---------- VIDEO ----------
        _settings.Set("contrast", cfg.GetValue("video", "contrast", _settings.Get("contrast")));
        _settings.Set("brightness", cfg.GetValue("video", "brightness", _settings.Get("brightness")));
        _settings.Set("animations_enabled", cfg.GetValue("video", "animations", _settings.Get("animations_enabled")));
        _settings.Set("resolution_index", cfg.GetValue("video", "resolution_index", _settings.Get("resolution_index")));
        _settings.Set("resolution", cfg.GetValue("video", "resolution", _settings.Get("resolution")));
        _settings.Set("window_mode", cfg.GetValue("video", "window_mode", _settings.Get("window_mode")));
        _settings.Set("texture_quality", cfg.GetValue("video", "texture_quality", _settings.Get("texture_quality")));

        // ---------- VISUAL ----------
        _settings.Set("interface_size", cfg.GetValue("visual", "interface_size", _settings.Get("interface_size")));
        _settings.Set("blurry_enabled", cfg.GetValue("visual", "blurry", _settings.Get("blurry_enabled")));
        _settings.Set("camera_shake_enabled", cfg.GetValue("visual", "camera_shake", _settings.Get("camera_shake_enabled")));
        _settings.Set("visual_indicators_enabled", cfg.GetValue("visual", "indicators", _settings.Get("visual_indicators_enabled")));
        _settings.Set("color_blindness", cfg.GetValue("visual", "color_blindness", _settings.Get("color_blindness")));

        // ---------- INPUT ----------
        var actions = _settings?.Get("actions").As<Dictionary>();
        if (actions == null)
            return;

        foreach (string action in actions.Keys)
        {
            InputMap.ActionEraseEvents(action);

            if (cfg.HasSectionKey("input", action + "_key"))
            {
                var data = (Dictionary)cfg.GetValue("input", action + "_key");
                string type = (string)data["type"];
                if (type == "key")
                    InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = (Key)(long)data["keycode"] });
                else if (type == "mouse")
                    InputMap.ActionAddEvent(action, new InputEventMouseButton { ButtonIndex = (MouseButton)(long)data["button"] });
            }

            if (cfg.HasSectionKey("input", action + "_pad"))
            {
                var data = (Dictionary)cfg.GetValue("input", action + "_pad");
                string type = (string)data["type"];
                if (type == "pad")
                    InputMap.ActionAddEvent(action, new InputEventJoypadButton { ButtonIndex = (JoyButton)(long)data["button"] });
                else if (type == "motion")
                    InputMap.ActionAddEvent(action, new InputEventJoypadMotion
                    {
                        Axis = (JoyAxis)(long)data["axis"],
                        AxisValue = (float)(double)data["value"]
                    });
            }
        }

        if (_settings == null)
            return;

        // ---------- AUDIO ----------
        _settings.Set("master_volume", cfg.GetValue("audio", "master", _settings.Get("master_volume")));
        _settings.Set("music_volume", cfg.GetValue("audio", "music", _settings.Get("music_volume")));
        _settings.Set("voices_volume", cfg.GetValue("audio", "voices", _settings.Get("voices_volume")));
        _settings.Set("sfx_volume", cfg.GetValue("audio", "sfx", _settings.Get("sfx_volume")));

        _settings.Call("ApplySettingsToUI");
        _settings.Call("ApplyAudioToUi");
        _settings.Call("ApplyFontSelection");

        var actionsDict = _settings?.Get("actions").As<Dictionary>();

        if (actionsDict == null || _settings == null)
            return;

        var pageCmd = _settings.GetNodeOrNull("PageCommand");

        if (pageCmd == null)
            return;

        foreach (string action in actionsDict.Keys)
        {
            var lbl = pageCmd.GetNode<Label>((string)actionsDict[action]);
            var btn2 = lbl.GetNode<Button>("Button2");
            var sp = _settings as SettingsPages;
            if (sp != null)
                btn2.Text = sp.GetActionKeyPad(action); ((SettingsPages)_settings).GetActionKeyPad(action);
        }

        GD.Print("SETTINGS LOADED");
    }
}
