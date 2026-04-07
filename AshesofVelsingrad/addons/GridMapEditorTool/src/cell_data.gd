@tool
extends Resource
class_name CellData

@export var player_spawners: Array[Dictionary] = []
@export var enemy_spawners: Array[Dictionary] = []
@export var max_player_spawners: int = 0
@export var max_enemy_spawners: int = 0

enum SpawnerType {
	PLAYER,
	ENEMY
}

## Add a spawner to a one of a spawner dictionary save slot depending on the spawner type
func add_spawner(cell: Vector3i, world_pos: Vector3, spawner_type: SpawnerType) -> bool:
	var spawner_dict: Dictionary = {"cell": cell, "world_pos": world_pos}

	if check_if_spawner_exist(cell, SpawnerType.PLAYER) || check_if_spawner_exist(cell, SpawnerType.ENEMY):
		return false
	if spawner_type == SpawnerType.PLAYER:
		player_spawners.append(spawner_dict)
		max_player_spawners += 1
		return true
	if spawner_type == SpawnerType.ENEMY:
		enemy_spawners.append(spawner_dict)
		max_enemy_spawners += 1
		return true
	return false

## Remove a spawner from one of the spawner dictionary save slot depending on the spawner type
func remove_spawner(cell: Vector3i, spawner_type: SpawnerType) -> bool:
	if spawner_type == SpawnerType.PLAYER:
		for spawner in player_spawners:
			if spawner["cell"] == cell:
				player_spawners.erase(spawner)
				max_player_spawners -= 1
				return true
	if spawner_type == SpawnerType.ENEMY:
		for spawner in enemy_spawners:
			if spawner["cell"] == cell:
				enemy_spawners.erase(spawner)
				max_enemy_spawners -= 1
				return true
	return false

## Get the number of spawners depending on the spawner type
func get_spawners_dictionary_length(spawner_type: SpawnerType) -> int:
	if spawner_type == SpawnerType.PLAYER:
		return player_spawners.size()
	if spawner_type == SpawnerType.ENEMY:
		return enemy_spawners.size()
	return -1

## Check if a spawner exists at a given cell depending on the spawner type
func check_if_spawner_exist(cell: Vector3i, spawner_type: SpawnerType) -> bool:
	if spawner_type == SpawnerType.PLAYER:
		for spawner in player_spawners:
			if spawner["cell"] == cell:
				return true
	if spawner_type == SpawnerType.ENEMY:
		for spawner in enemy_spawners:
			if spawner["cell"] == cell:
				return true
	return false
