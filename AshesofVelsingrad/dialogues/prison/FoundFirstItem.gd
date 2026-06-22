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
	dialog.say("Mr. Voss! Come here!", mercenary)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func _emit_dialog_end():
	dialog_ended.emit()
