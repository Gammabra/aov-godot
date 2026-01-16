extends Button

const CONFIG_PATH := "user://settings.cfg"
@onready var settings = get_node("../MainContent")

func _ready():
	load_settings()

func _pressed():
	save_settings()

# ================= SAVE =================
func save_settings():
	var cfg = ConfigFile.new()

	# ---------- SUBTITLE ----------
	cfg.set_value("subtitle", "enabled", settings.subtitles_enabled)
	cfg.set_value("subtitle", "size", settings.subtitle_size)
	cfg.set_value("subtitle", "font_index", settings.subtitle_font_index)
	cfg.set_value("subtitle", "text_color", settings.subtitle_text_color)
	cfg.set_value("subtitle", "bg_color", settings.subtitle_bg_color)
	cfg.set_value("subtitle", "language", settings.subtitle_language)
	cfg.set_value("subtitle", "display_time", settings.subtitle_display_time)

	# ---------- VIDEO ----------
	cfg.set_value("video", "contrast", settings.contrast)
	cfg.set_value("video", "brightness", settings.brightness)
	cfg.set_value("video", "animations", settings.animations_enabled)
	cfg.set_value("video", "resolution_index", settings.resolution_index)
	cfg.set_value("video", "resolution", settings.resolution)
	cfg.set_value("video", "texture_quality", settings.texture_quality)

	# ---------- VISUAL ----------
	cfg.set_value("visual", "interface_size", settings.interface_size)
	cfg.set_value("visual", "blurry", settings.blurry_enabled)
	cfg.set_value("visual", "camera_shake", settings.camera_shake_enabled)
	cfg.set_value("visual", "indicators", settings.visual_indicators_enabled)
	cfg.set_value("visual", "color_blindness", settings.color_blindness)

	# ---------- INPUT ----------
	for action in settings.actions.keys():
		var events := InputMap.action_get_events(action)
		if events.is_empty():
			continue
		var ev := events[0]
		if ev is InputEventKey:
			cfg.set_value("input", action, {
				"type": "key",
				"keycode": ev.physical_keycode
			})
		elif ev is InputEventMouseButton:
			cfg.set_value("input", action, {
				"type": "mouse",
				"button": ev.button_index
			})

	# ---------- AUDIO ----------
	cfg.set_value("audio", "master", settings.master_volume)
	cfg.set_value("audio", "music", settings.music_volume)
	cfg.set_value("audio", "voices", settings.voices_volume)
	cfg.set_value("audio", "sfx", settings.sfx_volume)

	cfg.save(CONFIG_PATH)
	print("SETTINGS SAVED")

# ================= LOAD =================
func load_settings():
	var cfg = ConfigFile.new()
	if cfg.load(CONFIG_PATH) != OK:
		return

	# ---------- SUBTITLE ----------
	settings.subtitles_enabled = cfg.get_value("subtitle", "enabled", settings.subtitles_enabled)
	settings.subtitle_size = cfg.get_value("subtitle", "size", settings.subtitle_size)
	settings.subtitle_font_index = cfg.get_value("subtitle", "font_index", settings.subtitle_font_index)
	settings.subtitle_text_color = cfg.get_value("subtitle", "text_color", settings.subtitle_text_color)
	settings.subtitle_bg_color = cfg.get_value("subtitle", "bg_color", settings.subtitle_bg_color)
	settings.subtitle_language = cfg.get_value("subtitle", "language", settings.subtitle_language)
	settings.subtitle_display_time = cfg.get_value("subtitle", "display_time", settings.subtitle_display_time)

	# ---------- VIDEO ----------
	settings.contrast = cfg.get_value("video", "contrast", settings.contrast)
	settings.brightness = cfg.get_value("video", "brightness", settings.brightness)
	settings.animations_enabled = cfg.get_value("video", "animations", settings.animations_enabled)
	settings.resolution_index = cfg.get_value("video", "resolution_index", settings.resolution_index)
	settings.resolution = cfg.get_value("video", "resolution", settings.resolution)
	settings.texture_quality = cfg.get_value("video", "texture_quality", settings.texture_quality)

	# ---------- VISUAL ----------
	settings.interface_size = cfg.get_value("visual", "interface_size", settings.interface_size)
	settings.blurry_enabled = cfg.get_value("visual", "blurry", settings.blurry_enabled)
	settings.camera_shake_enabled = cfg.get_value("visual", "camera_shake", settings.camera_shake_enabled)
	settings.visual_indicators_enabled = cfg.get_value("visual", "indicators", settings.visual_indicators_enabled)
	settings.color_blindness = cfg.get_value("visual", "color_blindness", settings.color_blindness)

	# ---------- INPUT ----------
	for action in settings.actions.keys():
		if not cfg.has_section_key("input", action):
			continue
		var data: Dictionary = cfg.get_value("input", action)
		InputMap.action_erase_events(action)
		if data["type"] == "key":
			var ev := InputEventKey.new()
			ev.physical_keycode = data["keycode"]
			InputMap.action_add_event(action, ev)
		elif data["type"] == "mouse":
			var ev := InputEventMouseButton.new()
			ev.button_index = data["button"]
			InputMap.action_add_event(action, ev)

	# ---------- AUDIO ----------
	settings.master_volume = cfg.get_value("audio", "master", settings.master_volume)
	settings.music_volume = cfg.get_value("audio", "music", settings.music_volume)
	settings.voices_volume = cfg.get_value("audio", "voices", settings.voices_volume)
	settings.sfx_volume = cfg.get_value("audio", "sfx", settings.sfx_volume)

	settings.apply_settings_to_ui()	
	settings.apply_audio_to_ui()
	settings.apply_font_selection()
	print("SETTINGS LOADED")
