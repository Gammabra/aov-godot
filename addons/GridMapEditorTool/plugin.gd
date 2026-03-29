@tool
extends EditorPlugin

var grid_map: GridMap = null
var root: Node = null
var hovered_cell: HoveredCell = null
var popup: AcceptDialog = null
var current_cell: Variant = null

func _enter_tree() -> void:
	set_input_event_forwarding_always_enabled()
	add_custom_type(
		"HoveredCell",
		"Node",
		preload("res://addons/gridMapEditorTool/src/hovered_cell.gd"),
		preload("res://addons/gridMapEditorTool/assets/icons/mouse.png")
	)
	_create_popup()

func _exit_tree() -> void:
	remove_custom_type("HoveredCell")
	if popup:
		popup.queue_free()

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
			_handle_mouse_button_left_click(event)
			return true

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

func _handle_mouse_button_left_click(event: InputEvent):
	if current_cell && hovered_cell:
		_show_popup(current_cell)

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

	var add_spawner_btn: Button = Button.new()
	add_spawner_btn.text = "Add a spawner"
	add_spawner_btn.pressed.connect(_on_add_spawner_pressed)
	popup.add_child(add_spawner_btn)

	EditorInterface.get_base_control().add_child(popup)
	popup.hide()

func _show_popup(cell: Vector3i):
	if popup == null or not is_instance_valid(popup):
		return
	popup.dialog_text = "Action on the %s cell" % str(cell)
	popup.popup_centered()

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

func _on_add_spawner_pressed():
	var res: CellData = _get_or_create_resource()
	if res:
		res.add_player_spawner(current_cell)
		ResourceSaver.save(res, get_tree().edited_scene_root.scene_file_path.get_basename() + ".tres")
	popup.hide()
