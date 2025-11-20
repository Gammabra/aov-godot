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
