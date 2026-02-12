using Godot;
using System;
using Godot.Collections;

public partial class settings_pages : Node
{
	// =================================================
	// VALUES
	// =================================================

	// ---------- SUBTITLE ----------
	public bool subtitles_enabled = false;
	public float subtitle_size = 50.0f;
	public int subtitle_font_index = 0;
	public int subtitle_text_color = 0;
	public int subtitle_bg_color = 0;
	public string subtitle_language = "English";
	public float subtitle_display_time = 3.0f;

	// ---------- VIDEO ----------
	public float contrast = 50.0f;
	public float brightness = 50.0f;
	public bool animations_enabled = false;
	public int resolution_index = 0;
	public Vector2I resolution = new Vector2I(1920, 1080);
	public int texture_quality = 0;

	// ---------- VISUAL ----------
	public float interface_size = 1.0f;
	public bool blurry_enabled = false;
	public bool camera_shake_enabled = false;
	public bool visual_indicators_enabled = false;
	public string color_blindness = "None";

	// ---------- COMMAND ----------
	private Node page_command;

	private string waiting_action = "";
	private Button waiting_button = null;

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
		page_command = GetNode("PageCommand");

		foreach (string action in actions.Keys)
		{
			var label = page_command.GetNode<Label>((string)actions[action]);
			var btn = label.GetNode<Button>("Button");

			btn.Text = GetActionKey(action);
			btn.Pressed += () => OnRebindPressed(action, btn);
		}
	}

	// =================================================
	// SUBTITLE
	// =================================================

	public void _on_subtitles_toggled(bool enabled) => subtitles_enabled = enabled;
	public void _on_subtitle_size_changed(double value) => subtitle_size = (float)value;
	public void _on_subtitle_font_item_selected(long index) => subtitle_font_index = (int)index;
	public void _on_subtitle_text_color_changed(long index) => subtitle_text_color = (int)index;
	public void _on_subtitle_bg_color_changed(long index) => subtitle_bg_color = (int)index;

	public void _on_subtitle_language_item_selected(long index)
	{
		var btn = GetNode<OptionButton>("PageSubtitle/Langage/OptionButton");
		subtitle_language = btn.GetItemText((int)index);
	}

	public void _on_subtitle_display_time_changed(long index)
	{
		var btn = GetNode<OptionButton>("PageSubtitle/DisplayTime/OptionButton");
		subtitle_display_time = float.Parse(btn.GetItemText((int)index));
	}

	// =================================================
	// VIDEO
	// =================================================

	public void _on_contrast_changed(double value) => contrast = (float)value;
	public void _on_brightness_changed(double value) => brightness = (float)value;
	public void _on_animations_toggled(bool enabled) => animations_enabled = enabled;

	public void _on_resolution_item_selected(long index)
	{
		resolution_index = (int)index;

		switch (index)
		{
			case 0: resolution = new Vector2I(1920, 1080); break;
			case 1: resolution = new Vector2I(1280, 720); break;
		}

		DisplayServer.WindowSetSize(resolution);
	}

	public void _on_texture_item_selected(long index) => texture_quality = (int)index;

	// =================================================
	// VISUAL
	// =================================================

	public void _on_interface_size_changed(double value) => interface_size = (float)value;
	public void _on_blurry_toggled(bool enabled) => blurry_enabled = enabled;
	public void _on_camera_shake_toggled(bool enabled) => camera_shake_enabled = enabled;
	public void _on_visual_indicators_toggled(bool enabled) => visual_indicators_enabled = enabled;

	public void _on_color_blindness_item_selected(long index)
	{
		var btn = GetNode<OptionButton>("PageVisual/ColorBlindness/OptionButton");
		color_blindness = btn.GetItemText((int)index);
	}

	// =================================================
	// COMMAND
	// =================================================

	private void OnRebindPressed(string action, Button btn)
	{
		waiting_action = action;
		waiting_button = btn;
		btn.Text = "Press key...";
	}

	public override void _Input(InputEvent @event)
	{
		if (waiting_action == "")
			return;

		if (@event is InputEventKey key && key.Pressed)
			BindEvent(@event);

		if (@event is InputEventMouseButton mouse && mouse.Pressed)
			BindEvent(@event);
	}

	private void BindEvent(InputEvent @event)
	{
		InputMap.ActionEraseEvents(waiting_action);
		InputMap.ActionAddEvent(waiting_action, @event);

		waiting_button.Text = GetActionKey(waiting_action);
		waiting_action = "";
		waiting_button = null;
	}

	private string GetActionKey(string actionName)
	{
		var events = InputMap.ActionGetEvents(actionName);
		if (events.Count == 0)
			return "None";

		var ev = events[0];

		if (ev is InputEventKey key)
			return OS.GetKeycodeString(key.PhysicalKeycode);

		if (ev is InputEventMouseButton mouse)
			return "Mouse " + mouse.ButtonIndex;

		return "Unknown";
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

	public void _on_master_volume_changed(double value)
	{
		master_volume = (float)value;
		SetBusVolume("Master", master_volume);
	}

	public void _on_music_volume_changed(double value)
	{
		music_volume = (float)value;
		SetBusVolume("Music", music_volume);
	}

	public void _on_voices_volume_changed(double value)
	{
		voices_volume = (float)value;
		SetBusVolume("Voices", voices_volume);
	}

	public void _on_sfx_volume_changed(double value)
	{
		sfx_volume = (float)value;
		SetBusVolume("SFX", sfx_volume);
	}

	// =================================================
	// APPLY VALUES
	// =================================================

	public async void apply_font_selection()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);

		var fontBtn = GetNode<OptionButton>("PageSubtitle/Font/OptionButton");
		if (fontBtn.ItemCount > 0)
			fontBtn.Select(subtitle_font_index);
	}

	public void apply_settings_to_ui()
	{
		GetNode<CheckBox>("PageSubtitle/Subtitles/CheckBox").ButtonPressed = subtitles_enabled;
		GetNode<HSlider>("PageSubtitle/Size/HSlider").Value = subtitle_size;
		GetNode<OptionButton>("PageSubtitle/TextColor/OptionButton").Select(subtitle_text_color);
		GetNode<OptionButton>("PageSubtitle/BgColor/OptionButton").Select(subtitle_bg_color);

		GetNode<HSlider>("PageVideo/Contrast/HSlider").Value = contrast;
		GetNode<HSlider>("PageVideo/Brightness/HSlider").Value = brightness;
		GetNode<CheckBox>("PageVideo/Animations/CheckBox").ButtonPressed = animations_enabled;
		GetNode<OptionButton>("PageVideo/Resolution/OptionButton").Select(resolution_index);
		GetNode<OptionButton>("PageVideo/Texture/OptionButton").Select(texture_quality);

		GetNode<HSlider>("PageVisual/InterfaceSize/HSlider").Value = interface_size;
		GetNode<CheckBox>("PageVisual/Blurry/CheckBox").ButtonPressed = blurry_enabled;
		GetNode<CheckBox>("PageVisual/CameraShake/CheckBox").ButtonPressed = camera_shake_enabled;
		GetNode<CheckBox>("PageVisual/VisualIndicators/CheckBox").ButtonPressed = visual_indicators_enabled;

		apply_audio_to_ui();
	}

	public void apply_audio_to_ui()
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
