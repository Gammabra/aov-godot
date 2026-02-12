using Godot;
using System;
using Godot.Collections;

public partial class settings_save : Button
{
	private const string CONFIG_PATH = "user://settings.cfg";

	private Node settings;

	public override void _Ready()
	{
		settings = GetNode<Node>("../MainContent");
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

		// ---------- SUBTITLE ----------
		cfg.SetValue("subtitle", "enabled", settings.Get("subtitles_enabled"));
		cfg.SetValue("subtitle", "size", settings.Get("subtitle_size"));
		cfg.SetValue("subtitle", "font_index", settings.Get("subtitle_font_index"));
		cfg.SetValue("subtitle", "text_color", settings.Get("subtitle_text_color"));
		cfg.SetValue("subtitle", "bg_color", settings.Get("subtitle_bg_color"));
		cfg.SetValue("subtitle", "language", settings.Get("subtitle_language"));
		cfg.SetValue("subtitle", "opacity", settings.Get("subtitle_opacity"));

		// ---------- VIDEO ----------
		cfg.SetValue("video", "contrast", settings.Get("contrast"));
		cfg.SetValue("video", "brightness", settings.Get("brightness"));
		cfg.SetValue("video", "animations", settings.Get("animations_enabled"));
		cfg.SetValue("video", "resolution_index", settings.Get("resolution_index"));
		cfg.SetValue("video", "resolution", settings.Get("resolution"));
		cfg.SetValue("video", "texture_quality", settings.Get("texture_quality"));

		// ---------- VISUAL ----------
		cfg.SetValue("visual", "interface_size", settings.Get("interface_size"));
		cfg.SetValue("visual", "blurry", settings.Get("blurry_enabled"));
		cfg.SetValue("visual", "camera_shake", settings.Get("camera_shake_enabled"));
		cfg.SetValue("visual", "indicators", settings.Get("visual_indicators_enabled"));
		cfg.SetValue("visual", "color_blindness", settings.Get("color_blindness"));

		// ---------- INPUT ----------
		var actions = (Dictionary)settings.Get("actions");

		foreach (string action in actions.Keys)
		{
			var events = InputMap.ActionGetEvents(action);
			if (events.Count == 0)
				continue;

			var ev = events[0];

			if (ev is InputEventKey key)
			{
				cfg.SetValue("input", action, new Dictionary
				{
					{ "type", "key" },
					{ "keycode", (long)key.PhysicalKeycode }
				});
			}
			else if (ev is InputEventMouseButton mouse)
			{
				cfg.SetValue("input", action, new Dictionary
				{
					{ "type", "mouse" },
					{ "button", (long)mouse.ButtonIndex }
				});
			}
		}

		// ---------- AUDIO ----------
		cfg.SetValue("audio", "master", settings.Get("master_volume"));
		cfg.SetValue("audio", "music", settings.Get("music_volume"));
		cfg.SetValue("audio", "voices", settings.Get("voices_volume"));
		cfg.SetValue("audio", "sfx", settings.Get("sfx_volume"));

		cfg.Save(CONFIG_PATH);
		GD.Print("SETTINGS SAVED");
	}

	// ================= LOAD =================
	private void LoadSettings()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(CONFIG_PATH) != Error.Ok)
			return;

		// ---------- SUBTITLE ----------
		settings.Set("subtitles_enabled", cfg.GetValue("subtitle", "enabled", settings.Get("subtitles_enabled")));
		settings.Set("subtitle_size", cfg.GetValue("subtitle", "size", settings.Get("subtitle_size")));
		settings.Set("subtitle_font_index", cfg.GetValue("subtitle", "font_index", settings.Get("subtitle_font_index")));
		settings.Set("subtitle_text_color", cfg.GetValue("subtitle", "text_color", settings.Get("subtitle_text_color")));
		settings.Set("subtitle_bg_color", cfg.GetValue("subtitle", "bg_color", settings.Get("subtitle_bg_color")));
		settings.Set("subtitle_language", cfg.GetValue("subtitle", "language", settings.Get("subtitle_language")));
		settings.Set("subtitle_opacity", cfg.GetValue("subtitle", "opacity", settings.Get("subtitle_opacity")));

		// ---------- VIDEO ----------
		settings.Set("contrast", cfg.GetValue("video", "contrast", settings.Get("contrast")));
		settings.Set("brightness", cfg.GetValue("video", "brightness", settings.Get("brightness")));
		settings.Set("animations_enabled", cfg.GetValue("video", "animations", settings.Get("animations_enabled")));
		settings.Set("resolution_index", cfg.GetValue("video", "resolution_index", settings.Get("resolution_index")));
		settings.Set("resolution", cfg.GetValue("video", "resolution", settings.Get("resolution")));
		settings.Set("texture_quality", cfg.GetValue("video", "texture_quality", settings.Get("texture_quality")));

		// ---------- VISUAL ----------
		settings.Set("interface_size", cfg.GetValue("visual", "interface_size", settings.Get("interface_size")));
		settings.Set("blurry_enabled", cfg.GetValue("visual", "blurry", settings.Get("blurry_enabled")));
		settings.Set("camera_shake_enabled", cfg.GetValue("visual", "camera_shake", settings.Get("camera_shake_enabled")));
		settings.Set("visual_indicators_enabled", cfg.GetValue("visual", "indicators", settings.Get("visual_indicators_enabled")));
		settings.Set("color_blindness", cfg.GetValue("visual", "color_blindness", settings.Get("color_blindness")));

		// ---------- INPUT ----------
		var actions = (Dictionary)settings.Get("actions");

		foreach (string action in actions.Keys)
		{
			if (!cfg.HasSectionKey("input", action))
				continue;

			var data = (Dictionary)cfg.GetValue("input", action);

			InputMap.ActionEraseEvents(action);

			string type = (string)data["type"];

			if (type == "key")
			{
				var ev = new InputEventKey
				{
					PhysicalKeycode = (Key)(long)data["keycode"]
				};
				InputMap.ActionAddEvent(action, ev);
			}
			else if (type == "mouse")
			{
				var ev = new InputEventMouseButton
				{
					ButtonIndex = (MouseButton)(long)data["button"]
				};
				InputMap.ActionAddEvent(action, ev);
			}
		}

		// ---------- AUDIO ----------
		settings.Set("master_volume", cfg.GetValue("audio", "master", settings.Get("master_volume")));
		settings.Set("music_volume", cfg.GetValue("audio", "music", settings.Get("music_volume")));
		settings.Set("voices_volume", cfg.GetValue("audio", "voices", settings.Get("voices_volume")));
		settings.Set("sfx_volume", cfg.GetValue("audio", "sfx", settings.Get("sfx_volume")));

		settings.Call("apply_settings_to_ui");
		settings.Call("apply_audio_to_ui");
		settings.Call("apply_font_selection");

		GD.Print("SETTINGS LOADED");
	}
}
