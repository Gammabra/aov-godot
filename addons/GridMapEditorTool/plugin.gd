@tool
extends EditorPlugin

var gridmap: GridMap = null
var root: Node = get_editor_interface().get_edited_scene_root()

func _enter_tree() -> void:
	set_input_event_forwarding_always_enabled()
	update_gridmap()

func _process(delta: float) -> void:
	var scene_root = get_editor_interface().get_edited_scene_root()
	if root != scene_root:
		root = scene_root
		update_gridmap()

func update_gridmap():
	var root = get_editor_interface().get_edited_scene_root()
	if root:
		gridmap = find_gridmap(root)

func _forward_3d_gui_input(camera, event):
	if event is InputEventMouseMotion:
		if gridmap == null:
			return
		var from = camera.project_ray_origin(event.position)
		var to = from + camera.project_ray_normal(event.position) * 1000

		var space_state = camera.get_world_3d().direct_space_state
		var result = space_state.intersect_ray(
			PhysicsRayQueryParameters3D.create(from, to)
		)

		if result:
			var cell = gridmap.local_to_map(result.position)
			print(cell)

func find_gridmap(node):
	if node is GridMap:
		return node

	for child in node.get_children():
		var result = find_gridmap(child)
		if result:
			return result

	return null
