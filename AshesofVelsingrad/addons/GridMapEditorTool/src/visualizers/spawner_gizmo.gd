@tool
extends EditorNode3DGizmoPlugin
class_name SpawnerGizmo

func _init():
	create_material("blue", Color.BLUE)
	create_material("red", Color.RED)

func _get_gizmo_name() -> String:
	return "Spawner Gizmo"

func _has_gizmo(for_node_3d: Node3D) -> bool:
	return for_node_3d is SpawnerVisualizer

func _draw_spawners(gizmo: EditorNode3DGizmo, node: Node3D, color: String) -> void:
	var spawners = null
	if color == "blue":
		spawners = node.get_player_spawners()
	if color == "red":
		spawners = node.get_enemy_spawners()
	if spawners == null:
		return

	var cell_size = 1
	var half = cell_size * 0.5
	var size = 0.5 * cell_size

	for spawner in spawners:
		var pos = spawner.get("world_pos", null)

		var p0 = pos + Vector3(-size, -size, -size)
		var p1 = pos + Vector3(size, -size, -size)
		var p2 = pos + Vector3(size, -size, size)
		var p3 = pos + Vector3(-size, -size, size)
		var p4 = pos + Vector3(-size, size, -size)
		var p5 = pos + Vector3(size, size, -size)
		var p6 = pos + Vector3(size, size, size)
		var p7 = pos + Vector3(-size, size, size)

		var lines = PackedVector3Array([
			# bottom face
			p0,p1, p1,p2, p2,p3, p3,p0,
			# top face
			p4,p5, p5,p6, p6,p7, p7,p4,
			# vertical edges
			p0,p4, p1,p5, p2,p6, p3,p7
		])

		gizmo.add_lines(lines, get_material(color, gizmo))

func _redraw(gizmo: EditorNode3DGizmo) -> void:
	gizmo.clear()
	var node = gizmo.get_node_3d()

	if not node or not node.has_method("get_player_spawners"):
		return
	_draw_spawners(gizmo, node, "blue")
	
	if not node or not node.has_method("get_enemy_spawners"):
		return
	_draw_spawners(gizmo, node, "red")
