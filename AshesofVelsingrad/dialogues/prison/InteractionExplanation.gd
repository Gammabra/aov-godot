extends Node

signal dialog_ended()

var dio=Dialog.new()
var dialog:=dio.start(self)

var mercenary = dialog.Character(
		"Mercenary",
		Color.YELLOW_GREEN,
		"res://assets/Krita/icone_mercenaire.png"
	)

func _ready() -> void:
	dialog.typewriter_speed=30

func talk():
	dialog.say("This is a Health Potion.", mercenary)
	dialog.say("This is going to be very useful for later.", mercenary)
	dialog.say("You can pick it up with the (E) keybind.", mercenary)
	dialog.say("Keep in mind that there will be a lot of things you can interact with in the future.", mercenary)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func _emit_dialog_end():
	dialog_ended.emit()
