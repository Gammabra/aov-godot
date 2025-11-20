extends Node

func show_page(page_name):
	print("Changement de page vers :", page_name)
	for p in get_children():
		p.visible = false
	get_node(page_name).visible = true

func _on_button_subtitle_pressed():
	show_page("PageSubtitle")

func _on_button_video_pressed() -> void:
	show_page("PageVideo")

func _on_button_visual_pressed() -> void:
	show_page("PageVisual")

func _on_button_command_pressed() -> void:
	show_page("PageCom")

func _on_button_audio_pressed() -> void:
	show_page("PageAudio")
