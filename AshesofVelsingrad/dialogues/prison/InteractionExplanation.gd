extends Node

signal dialog_ended()

var dio=Dialog.new()
var dialog:=dio.start(self)

var narrateur = dialog.Character(
		"",
		Color("ebede9"),
		" "
	)
var mercenary = dialog.Character(
		"Arthur",
		Color("be772b"),
		"res://assets/Krita/icone_mercenaire.png"
	)

func _ready() -> void:
	dialog.typewriter_speed=30

func talk():
	dialog.say("Look here, on the ground.", mercenary)
	dialog.say("This could be useful to us, right?", mercenary)
	dialog.say("When you are near an object or person you can interact with, press E to act.", narrateur)
	dialog.say("Certain parts of the scenery, characters, or objects can be examined, picked up, or activated this way.", narrateur)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func _emit_dialog_end():
	dialog_ended.emit()
