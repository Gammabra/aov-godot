extends Button

func _on_OptionsButton_pressed():
	get_tree().change_scene_to_file("res://scenes/settings_beta.tscn")

func _on_ExitButton_pressed():
	get_tree().quit()
