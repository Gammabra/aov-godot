class_name Utils

static func update_gridmap(root: Node) -> GridMap:
	if root:
		return find_gridmap(root)
	return null

static func find_gridmap(node) -> GridMap:
	if node is GridMap:
		return node

	for child in node.get_children():
		var result: GridMap = find_gridmap(child)
		if result:
			return result

	return null

static func find_hovered_cell(gridmap: GridMap) -> HoveredCell:
	for child in gridmap.get_children():
		if child is HoveredCell:
			return child
	return null
