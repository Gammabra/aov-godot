extends Node

# =================================================
# VALUES
# =================================================

# ---------- SUBTITLE ----------
var subtitles_enabled: bool = true
var subtitle_size: float = 1.0
var subtitle_font_index: int = 0
var subtitle_text_color: Color = Color.WHITE
var subtitle_bg_color: Color = Color.BLACK
var subtitle_language: String = ""
var subtitle_display_time: float = 3.0

# ---------- VIDEO ----------
var contrast: float = 1.0
var brightness: float = 1.0
var animations_enabled: bool = true
var resolution_index: int = 0
var resolution: Vector2i = Vector2i(1920, 1080)
var texture_quality: int = 0 # (0=Low 1=Medium 2=High)

# ---------- VISUAL ----------
var interface_size: float = 1.0
var blurry_enabled: bool = false
var camera_shake_enabled: bool = true
var visual_indicators_enabled: bool = true
var color_blindness: String = "Deuteranopia"

# ---------- AUDIO ----------
var master_volume: float = 0.8
var music_volume: float = 0.8
var voices_volume: float = 0.8
var sfx_volume: float = 0.8

# =================================================
# PAGES
# =================================================

func show_page(page_name):
	for p in get_children():
		p.visible = false
	get_node(page_name).visible = true

func _on_button_subtitle_pressed(): show_page("PageSubtitle")
func _on_button_video_pressed(): show_page("PageVideo")
func _on_button_visual_pressed(): show_page("PageVisual")
func _on_button_command_pressed(): show_page("PageCommand")
func _on_button_audio_pressed(): show_page("PageAudio")

# =================================================
# SUBTITLE
# =================================================

func _on_subtitles_toggled(enabled: bool):
	subtitles_enabled = enabled

func _on_subtitle_size_changed(value: float):
	subtitle_size = value

func _on_subtitle_font_item_selected(index: int):
	subtitle_font_index = index

func _on_subtitle_text_color_changed(color: Color):
	subtitle_text_color = color

func _on_subtitle_bg_color_changed(color: Color):
	subtitle_bg_color = color

func _on_subtitle_language_item_selected(index: int):
	subtitle_language = $SubtitleLanguageOption.get_item_text(index)

func _on_subtitle_display_time_changed(value: float):
	subtitle_display_time = value

# =================================================
# VIDEO
# =================================================

func _on_contrast_changed(value: float):
	contrast = value

func _on_brightness_changed(value: float):
	brightness = value

func _on_animations_toggled(enabled: bool):
	animations_enabled = enabled

func _on_resolution_item_selected(index: int):
	resolution_index = index
	match index:
		0: resolution = Vector2i(1920, 1080)
		1: resolution = Vector2i(1280, 720)
	DisplayServer.window_set_size(resolution)

func _on_texture_item_selected(index: int):
	texture_quality = index

# =================================================
# VISUAL
# =================================================

func _on_interface_size_changed(value: float):
	interface_size = value

func _on_blurry_toggled(enabled: bool):
	blurry_enabled = enabled

func _on_camera_shake_toggled(enabled: bool):
	camera_shake_enabled = enabled

func _on_visual_indicators_toggled(enabled: bool):
	visual_indicators_enabled = enabled

func _on_color_blindness_item_selected(index: int):
	color_blindness = $VisualColorBlindnessOption.get_item_text(index)

# =================================================
# AUDIO
# =================================================

func _set_bus_volume(bus_name: String, value: float):
	AudioServer.set_bus_volume_db(
		AudioServer.get_bus_index(bus_name),
		linear_to_db(value)
	)

func _on_master_volume_changed(value: float):
	master_volume = value
	_set_bus_volume("Master", value)

func _on_music_volume_changed(value: float):
	music_volume = value
	_set_bus_volume("Music", value)

func _on_voices_volume_changed(value: float):
	voices_volume = value
	_set_bus_volume("Voices", value)

func _on_sfx_volume_changed(value: float):
	sfx_volume = value
	_set_bus_volume("SFX", value)
