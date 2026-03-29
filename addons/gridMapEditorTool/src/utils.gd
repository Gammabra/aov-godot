@tool
class_name Utils

# --------------------------
# Process Utils
# --------------------------

## Find a GridMap recursively in a node
static func _find_gridmap(node) -> GridMap:
	if node is GridMap:
		return node

	for child in node.get_children():
		var result: GridMap = _find_gridmap(child)
		if result:
			return result

	return null

## Try to find the GridMap in the root scene
static func try_find_gridmap(root: Node) -> GridMap:
	if root:
		return _find_gridmap(root)
	return null

## Find the HoveredCell node in the children of a GridMap
static func find_hovered_cell(grid_map: GridMap) -> HoveredCell:
	if grid_map == null:
		return null
	for child in grid_map.get_children():
		if child is HoveredCell:
			return child
	return null
