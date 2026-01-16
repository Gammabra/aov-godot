extends Node

# =================================================
# VALUES
# =================================================

# ---------- SUBTITLE ----------
var subtitles_enabled: bool = false
var subtitle_size: float = 50.0
var subtitle_font_index: int = 0
var subtitle_text_color: int = 0
var subtitle_bg_color: int = 0
var subtitle_language: String = "English"
var subtitle_display_time: float = 3.0

# ---------- VIDEO ----------
var contrast: float = 50.0
var brightness: float = 50.0
var animations_enabled: bool = false
var resolution_index: int = 0
var resolution: Vector2i = Vector2i(1920, 1080)
var texture_quality: int = 0 # (0=Low 1=Medium 2=High)

# ---------- VISUAL ----------
var interface_size: float = 1.0
var blurry_enabled: bool = false
var camera_shake_enabled: bool = false
var visual_indicators_enabled: bool = false
var color_blindness: String = "None"

# ---------- COMMAND ----------
@onready var page_command := $PageCommand

var waiting_action := ""
var waiting_button: Button = null

var actions := {
	"move_up": "move_up",
	"move_down": "move_down",
	"move_left": "move_left",
	"move_right": "move_right",
	"battle_move_unit_to": "battle_move_unit_to",
	"battle_select_skill1": "battle_select_skill1",
	"battle_select_skill2": "battle_select_skill2",
	"battle_select_skill3": "battle_select_skill3",
	"battle_select_skill4": "battle_select_skill4",
	"battle_select_skill5": "battle_select_skill5",
	"battle_pass_turn": "battle_pass_turn",
	"toggle_options": "toggle_options"
}

# ---------- AUDIO ----------
var master_volume: float = 50.0
var music_volume: float = 50.0
var voices_volume: float = 50.0
var sfx_volume: float = 50.0

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

func _on_subtitle_text_color_changed(index: int):
	subtitle_text_color = index

func _on_subtitle_bg_color_changed(index: int):
	subtitle_bg_color = index

func _on_subtitle_language_item_selected(index: int):
	subtitle_language = $PageSubtitle/Langage/OptionButton.get_item_text(index)

func _on_subtitle_display_time_changed(index: int):
	var text: String = $PageSubtitle/DisplayTime/OptionButton.get_item_text(index)
	subtitle_display_time = text.to_float()

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
	color_blindness = $PageVisual/ColorBlindness/OptionButton.get_item_text(index)

# =================================================
# COMMAND
# =================================================

func _ready():
	for action in actions.keys():
		var label := page_command.get_node(actions[action]) as Label
		var btn := label.get_node("Button") as Button
		btn.text = _get_action_key(action)
		btn.pressed.connect(_on_rebind_pressed.bind(action, btn))

func _on_rebind_pressed(action: String, btn: Button):
	waiting_action = action
	waiting_button = btn
	btn.text = "Press key..."

func _input(event):
	if waiting_action == "":
		return
	if event is InputEventKey and event.pressed:
		_bind_event(event)
	if event is InputEventMouseButton and event.pressed:
		_bind_event(event)

func _bind_event(event):
	InputMap.action_erase_events(waiting_action)
	InputMap.action_add_event(waiting_action, event)

	waiting_button.text = _get_action_key(waiting_action)
	waiting_action = ""
	waiting_button = null

func _get_action_key(action_name: String) -> String:
	var events = InputMap.action_get_events(action_name)
	if events.is_empty():
		return "None"
	var ev = events[0]
	if ev is InputEventKey:
		return OS.get_keycode_string(ev.physical_keycode)
	if ev is InputEventMouseButton:
		return "Mouse " + str(ev.button_index)
	return "Unknown"

# =================================================
# AUDIO
# =================================================

func _set_bus_volume(bus_name: String, value: float):
	var idx := AudioServer.get_bus_index(bus_name)
	if idx == -1:
		return
	AudioServer.set_bus_volume_db(idx, linear_to_db(value))

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

# =================================================
# LOAD VALUES
# =================================================

func apply_font_selection():
	await get_tree().process_frame
	await get_tree().create_timer(0.1).timeout
	var font_btn := $PageSubtitle/Font/OptionButton
	if font_btn.item_count > 0:
		font_btn.select(subtitle_font_index)

func apply_settings_to_ui():
# ---------- SUBTITLE ----------
	$PageSubtitle/Subtitles/CheckBox.button_pressed = subtitles_enabled
	$PageSubtitle/Size/HSlider.value = subtitle_size
	$PageSubtitle/TextColor/OptionButton.select(subtitle_text_color)
	$PageSubtitle/BgColor/OptionButton.select(subtitle_bg_color)

	var lang_btn := $PageSubtitle/Langage/OptionButton
	if lang_btn.item_count > 0:
		for i in range(lang_btn.item_count):
			if lang_btn.get_item_text(i) == subtitle_language:
				lang_btn.select(i)
				break

	var time_btn := $PageSubtitle/DisplayTime/OptionButton
	for i in time_btn.item_count:
		if float(time_btn.get_item_text(i)) == subtitle_display_time:
			time_btn.select(i)
			break

	# ---------- VIDEO ----------
	$PageVideo/Contrast/HSlider.value = contrast
	$PageVideo/Brightness/HSlider.value = brightness
	$PageVideo/Animations/CheckBox.button_pressed = animations_enabled
	$PageVideo/Resolution/OptionButton.select(resolution_index)
	$PageVideo/Texture/OptionButton.select(texture_quality)
	
	# ---------- VISUAL ----------
	$PageVisual/InterfaceSize/HSlider.value = interface_size
	$PageVisual/Blurry/CheckBox.button_pressed = blurry_enabled
	$PageVisual/CameraShake/CheckBox.button_pressed = camera_shake_enabled
	$PageVisual/VisualIndicators/CheckBox.button_pressed = visual_indicators_enabled

	var cb := $PageVisual/ColorBlindness/OptionButton
	for i in cb.item_count:
		if cb.get_item_text(i) == color_blindness:
			cb.select(i)
			break

# ---------- AUDIO ----------
func apply_audio_to_ui():
	$PageAudio/Master/HSlider.value = master_volume
	$PageAudio/Music/HSlider.value  = music_volume
	$PageAudio/Voices/HSlider.value = voices_volume
	$PageAudio/SFX/HSlider.value    = sfx_volume

	_set_bus_volume("Master", master_volume)
	_set_bus_volume("Music", music_volume)
	_set_bus_volume("Voices", voices_volume)
	_set_bus_volume("SFX", sfx_volume)
