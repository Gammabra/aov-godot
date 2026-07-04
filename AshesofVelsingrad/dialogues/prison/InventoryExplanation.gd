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

func first():
	dialog.say("Good. Keep that close, we don't know what awaits us.", mercenary)
	dialog.say("Collected items are stored in your inventory. Press I to open it.", narrateur)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func second():
	dialog.say("On the left is the exploration inventory. Items collected during your journey will appear there.", narrateur)
	dialog.say("On the right is each party member's personal inventory.", narrateur)
	dialog.say("In combat, items can only be used during the turn of the character carrying them.", narrateur)
	dialog.say("To equip a character, drag an item to one of their equipment slots.", narrateur)
	dialog.say("The exploration inventory is not accessible during battles. Prepare your party before engaging the enemy.", narrateur)
	dialog.say("Everything in order?", mercenary)
	dialog.say("Then let's move on.", mercenary)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func _emit_dialog_end():
	dialog_ended.emit()
