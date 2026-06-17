extends Node

signal dialog_ended()

var dio=Dialog.new()
var dialog:=dio.start(self)
var guard=dialog.Character("Guard", Color.CORNFLOWER_BLUE, "res://assets/Krita/icone_solder.png") 

func _ready() -> void:
	dialog.typewriter_speed=30

func talk() -> void:
	dialog.say("Mr. Voss... Velsingrad is being attacked !", guard)
	dialog.say("I've never seen the city so vulnerable.", guard)
	dialog.say("We cannot beat the ennemies, we need you.", guard)
	dialog.action("_emit_dialog_end")

func _emit_dialog_end() -> void:
	dialog_ended.emit()
