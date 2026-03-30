extends EditorNode3DGizmoPlugin
class_name SpawnerGizmo

func _init():
	create_material("blue", Color.BLUE)


func _get_gizmo_name() -> String:
	return "Spawner Gizmo"

func _has_gizmo(for_node_3d: Node3D) -> bool:
	return for_node_3d is GridMap

func _redraw(gizmo: EditorNode3DGizmo) -> void:
	var node: Node = gizmo.get_node_3d()
	
	if not node or not node.has_method("GetPlayerSpawners"):
		print("Method GetPlayerSpawners not found")
		return

	for cell in node.GetPlayerSpawners():
		var pos: Vector3 = node.map_to_world(cell)
		var size = 0.5

		var lines = PackedVector3Array([
			pos + Vector3(-size, 0, -size), pos + Vector3(size, 0, -size),
			pos + Vector3(size, 0, -size), pos + Vector3(size, 0, size),
			pos + Vector3(size, 0, size), pos + Vector3(-size, 0, size),
			pos + Vector3(-size, 0, size), pos + Vector3(-size, 0, -size),
		])

		gizmo.add_lines(lines, get_material("blue", gizmo))
