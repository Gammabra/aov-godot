@tool
extends Node3D
class_name SpawnerVisualizer

func get_player_spawners() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
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
		var player_spawners: Array[Dictionary] = cell_data.player_spawners

		for spawner in player_spawners:
			result.append(spawner)
	return result

func get_enemy_spawners() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	var cell_data: CellData
	var scene_root: Node = get_tree().edited_scene_root

	if not scene_root:
		return result

	var tres_path: String = scene_root.scene_file_path.get_basename() + ".tres"
	if ResourceLoader.exists(tres_path):
		cell_data = load(tres_path)
	else:
		return result

	if cell_data != null and cell_data.enemy_spawners != null:
		var enemy_spawners: Array[Dictionary] = cell_data.enemy_spawners

		for spawner in enemy_spawners:
			result.append(spawner)
	return result

func refresh_gizmos(spawner_gizmo: SpawnerGizmo, gizmos: Array[Node3DGizmo]) -> void:
	if spawner_gizmo == null || gizmos == null:
		return
	for gizmo in gizmos:
		spawner_gizmo._redraw(gizmo)
