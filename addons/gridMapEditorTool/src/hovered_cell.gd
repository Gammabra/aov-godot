@tool
extends Node
class_name HoveredCell

var hovered_cell: Vector3i

## Set the hovered cell
func set_hovered_cell(cell: Vector3i) -> void:
	if self == null:
		return
	self.name = str(cell)
	hovered_cell = cell

## Get the hovered cell
func get_hovered_cell() -> Vector3i:
	return hovered_cell
