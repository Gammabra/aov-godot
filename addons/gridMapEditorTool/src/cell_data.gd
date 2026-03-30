@tool
extends Resource
class_name CellData

@export var player_spawners: Array[Dictionary] = []
@export var max_player_spawners: int = 0

## Add a player spawner to the "player_spawners" save slot
func add_player_spawner(cell: Vector3i, world_pos: Vector3) -> void:
	var spawner_dict: Dictionary = {"cell": cell, "world_pos": world_pos}

	if not is_player_spawner_exist(cell):
		player_spawners.append(spawner_dict)
		max_player_spawners += 1

## Remove a player spawner from the "player_spawners" save slot
func remove_player_spawner(cell: Vector3i) -> void:
	for spawner in player_spawners:
		if spawner["cell"] == cell:
			player_spawners.erase(spawner)
			max_player_spawners -= 1
			return

## Get the number of spawners for the player units
func get_player_spawners_length() -> int:
	return player_spawners.size()

## Check if a player spawner exists at a given cell
func is_player_spawner_exist(cell: Vector3i) -> bool:
	for spawner in player_spawners:
		if spawner["cell"] == cell:
			return true
	return false
