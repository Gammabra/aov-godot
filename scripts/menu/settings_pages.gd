extends Node

func show_page(page_name):
	for p in get_children():
		p.visible = false

	get_node(page_name).visible = true

func _on_button_subtitle_pressed():
	show_page("PageSubtitle")

func _on_button_video_pressed():
	show_page("PageVideo")

func _on_button_visual_pressed():
	show_page("PageVisual")

func _on_button_command_pressed():
	show_page("PageCommand")

func _on_button_audio_pressed():
	show_page("PageAudio")


# SUBTITLE

# VIDEO
func _on_resolution_item_selected(index: int) -> void:
	match index:
		0:
			DisplayServer.window_set_size(Vector2i(1920, 1080))
		1:
			DisplayServer.window_set_size(Vector2i(1280, 720))

# VISUAL

# COMMAND

# AUDIO
func _on_volume_value_changed(value: float) -> void:
	AudioServer.set_bus_volume_db(
		AudioServer.get_bus_index("Master"),
		linear_to_db(value)
	)
