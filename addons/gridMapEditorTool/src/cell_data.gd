@tool
extends Resource
class_name CellData

@export var player_spawners: Array[Vector3i] = []
@export var max_player_spawners: int = 0

## Add a player spawner to the "player_spawners" save slot
func add_player_spawner(cell: Vector3i) -> void:
	if cell not in player_spawners:
		player_spawners.append(cell)
		max_player_spawners += 1

## Remove a player spawner from the "player_spawners" save slot
func remove_player_spawner(cell: Vector3i) -> void:
	if cell in player_spawners:
		player_spawners.erase(cell)
		max_player_spawners -= 1

## Get the number of spawners for the player units
func get_player_spawners_length() -> int:
	return player_spawners.size()
