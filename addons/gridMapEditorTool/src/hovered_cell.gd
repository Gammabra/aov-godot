@tool
extends Node
class_name HoveredCell

func set_hovered_cell(cell: Vector3i) -> void:
	if self == null:
		return
	self.name = str(cell)
