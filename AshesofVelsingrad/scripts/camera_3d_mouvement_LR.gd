extends Camera3D


func _process(delta):
	var input_h = Input.get_action_strength("ui_right") - Input.get_action_strength("ui_left")
	
	transform.origin.x += input_h * delta * 5
