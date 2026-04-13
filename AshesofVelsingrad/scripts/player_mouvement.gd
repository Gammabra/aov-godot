extends Sprite3D


func _process(delta):
	var input_h = Input.get_action_strength("ui_right") - Input.get_action_strength("ui_left")
	var input_v = Input.get_action_strength("ui_down") - Input.get_action_strength("ui_up")
	
	transform.origin.x += input_h * delta * 5
	transform.origin.z += input_v * delta * 5
