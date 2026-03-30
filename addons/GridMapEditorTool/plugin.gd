@tool
extends EditorPlugin

var grid_map: GridMap = null
var root: Node = null
var hovered_cell: HoveredCell = null
var popup: AcceptDialog = null
var current_cell: Variant = null
var spawner_gizmo: SpawnerGizmo

func _enter_tree() -> void:
	set_input_event_forwarding_always_enabled()
	add_custom_type(
		"HoveredCell",
		"Node",
		preload("res://addons/gridMapEditorTool/src/hovered_cell.gd"),
		preload("res://addons/gridMapEditorTool/assets/icons/mouse.png")
	)
	_create_popup()
	spawner_gizmo = SpawnerGizmo.new()
	add_node_3d_gizmo_plugin(spawner_gizmo)

func _exit_tree() -> void:
	remove_custom_type("HoveredCell")
	if popup:
		popup.queue_free()
	remove_node_3d_gizmo_plugin(spawner_gizmo)

func _process(delta: float) -> void:
	var scene_root: Node = EditorInterface.get_edited_scene_root()
	if root != scene_root:
		root = scene_root
		grid_map = Utils.try_find_gridmap(root)
		hovered_cell = Utils.find_hovered_cell(grid_map)

func _forward_3d_gui_input(camera: Camera3D, event: InputEvent):
	if event is InputEventMouseMotion:
		_handle_mouse_hovering(camera, event)

	elif event is InputEventMouseButton:
		if event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			return _handle_mouse_button_left_click(event)

# --------------------------
# Forward 3D GUI Input Methods
# --------------------------
func _handle_mouse_hovering(camera: Camera3D, event: InputEvent) -> void:
	if grid_map == null:
		return
	var from: Vector3 = camera.project_ray_origin(event.position)
	var to: Vector3 = from + camera.project_ray_normal(event.position) * 1000

	var space_state: PhysicsDirectSpaceState3D = camera.get_world_3d().direct_space_state
	var result: Dictionary = space_state.intersect_ray(
		PhysicsRayQueryParameters3D.create(from, to)
	)

	if result:
		var cell: Vector3i = grid_map.local_to_map(result.position)
		current_cell = cell
		if hovered_cell != null:
			hovered_cell.set_hovered_cell(cell)
	else:
		current_cell = null
		hovered_cell.name = "No hovered cell"

func _handle_mouse_button_left_click(event: InputEvent) -> bool:
	var selection: EditorSelection = EditorInterface.get_selection()
	var selected_nodes: Array[Node] = selection.get_selected_nodes()

	for node in selected_nodes:
		if node == grid_map:
			return false

	if current_cell && hovered_cell:
		_show_popup(current_cell)
		return true
	return false

# --------------------------
# POPUP
# --------------------------
func _create_popup():
	popup = AcceptDialog.new()
	popup.name = "Cell Action Popup"
	popup.title = "Cell Action Popup"
	popup.dialog_text = ""
	popup.ok_button_text = "Return"
	popup.exclusive = false

	EditorInterface.get_base_control().add_child(popup)
	popup.hide()

func _show_popup(cell: Vector3i):
	if popup == null or not is_instance_valid(popup):
		return
	var res: CellData = _get_or_create_resource()

	if res:
		var exists: bool = res.is_player_spawner_exist(current_cell)
		if not exists:
			_create_button_to_popup("Add a spawner at %s" % str(cell), _on_add_player_spawner_pressed)
		else:
			_create_button_to_popup("Remove a spawner at %s" % str(cell), _on_remove_player_spawner_pressed)

	popup.popup_centered()

func _create_button_to_popup(button_text: String, callable: Callable):
	var remove_spawner_btn: Button = Button.new()
	remove_spawner_btn.text = button_text
	remove_spawner_btn.pressed.connect(callable)
	popup.add_child(remove_spawner_btn)

func _close_popup():
	for child in popup.get_children():
		if child is Button:
			child.queue_free()
	popup.hide()

# --------------------------
# .TRES Handling
# --------------------------
func _get_or_create_resource() -> CellData:
	var scene: Node = get_tree().edited_scene_root
	if not scene:
		return null

	var tres_path: String = scene.scene_file_path.get_basename() + ".tres"
	var res: CellData
	if ResourceLoader.exists(tres_path):
		res = load(tres_path)
	else:
		res = CellData.new()
		ResourceSaver.save(res, tres_path)
	return res

func _on_add_player_spawner_pressed():
	var res: CellData = _get_or_create_resource()
	if res:
		res.add_player_spawner(current_cell)
		ResourceSaver.save(res, get_tree().edited_scene_root.scene_file_path.get_basename() + ".tres")
	_close_popup()

func _on_remove_player_spawner_pressed():
	var res: CellData = _get_or_create_resource()
	if res:
		res.remove_player_spawner(current_cell)
		ResourceSaver.save(res, get_tree().edited_scene_root.scene_file_path.get_basename() + ".tres")
	_close_popup()
