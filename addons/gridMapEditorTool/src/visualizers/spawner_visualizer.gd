@tool
extends Node3D
class_name SpawnerVisualizer

func get_player_spawner() -> Array[Vector3i]:
	var result: Array[Vector3i] = []
	var cell_data: CellData
	var scene_root: Node = get_tree().edited_scene_root

	if not scene_root:
		return result

	var tres_path: String = scene_root.scene_file_path.get_basename() + ".tres"
	if ResourceLoader.exists(tres_path):
		cell_data = load(tres_path)
	else:
		return result

	if cell_data != null and cell_data.player_spawners != null:
		var player_spawners: Array[Vector3i] = cell_data.player_spawners

		for spawner in player_spawners:
			result.append(spawner)
	return result
