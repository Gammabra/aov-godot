extends ColorRect

var colors = [
	Color.WHITE,
	Color.YELLOW,
	Color.RED,
	Color.GREEN,
	Color.BLUE
]

var index := 0

func _ready():
	color = colors[index]

func _gui_input(event):
	if event is InputEventMouseButton and event.pressed:
		index = (index + 1) % colors.size()
		color = colors[index]
