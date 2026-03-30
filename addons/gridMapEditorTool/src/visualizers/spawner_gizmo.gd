@tool
extends EditorNode3DGizmoPlugin
class_name SpawnerGizmo

var grid_map: GridMap = null

func _init():
	create_material("blue", Color.BLUE)

func _get_gizmo_name() -> String:
	return "Spawner Gizmo"

func _has_gizmo(for_node_3d: Node3D) -> bool:
	return for_node_3d is SpawnerVisualizer

func _redraw(gizmo: EditorNode3DGizmo) -> void:
	var node = gizmo.get_node_3d()
	
	if not node or not node.has_method("get_player_spawner"):
		return

	var spawners = node.get_player_spawner()
	if spawners == null:
		return
	for cell in spawners:
		var pos: Vector3 = node.global_transform.origin + Vector3(cell.x, cell.y, cell.z)
		var size = 0.5

		var lines = PackedVector3Array([
			pos + Vector3(-size, 0, -size), pos + Vector3(size, 0, -size),
			pos + Vector3(size, 0, -size), pos + Vector3(size, 0, size),
			pos + Vector3(size, 0, size), pos + Vector3(-size, 0, size),
			pos + Vector3(-size, 0, size), pos + Vector3(-size, 0, -size),
		])

		gizmo.add_lines(lines, get_material("blue", gizmo))
