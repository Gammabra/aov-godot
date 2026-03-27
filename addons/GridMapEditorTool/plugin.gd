@tool
extends EditorPlugin

var gridmap: GridMap = null
var root: Node = null
var hoveredCell: HoveredCell = null

func _enter_tree() -> void:
	set_input_event_forwarding_always_enabled()
	add_custom_type(
		"HoveredCell",
		"Node",
		preload("res://addons/gridMapEditorTool/src/hovered_cell.gd"),
		preload("res://addons/gridMapEditorTool/assets/icons/mouse.png")
	)
	gridmap = Utils.update_gridmap(root)

func _exit_tree() -> void:
	remove_custom_type("HoveredCell")

func _process(delta: float) -> void:
	var scene_root = get_editor_interface().get_edited_scene_root()
	if root != scene_root:
		root = scene_root
		gridmap = Utils.update_gridmap(root)
		hoveredCell = Utils.find_hovered_cell(gridmap)

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
			if hoveredCell != null:
				hoveredCell.set_hovered_cell(cell)
