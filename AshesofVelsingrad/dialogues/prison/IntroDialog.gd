extends Node

signal dialog_ended()

var dio=Dialog.new()
var dialog:=dio.start(self)

var kaelen = dialog.Character(
		"Kaelen",
		Color.BROWN,
		"res://assets/Krita/icone_player.png"
	)
var mercenary = dialog.Character(
		"Mercenary",
		Color.YELLOW_GREEN,
		"res://assets/Krita/icone_mercenaire.png"
	)

func _ready() -> void:
	dialog.typewriter_speed=30

func talk():
	dialog.say("Mr. Voss...", mercenary)
	dialog.say("Mr. Voss ! Wake up !", mercenary)
	dialog.say("I can hear gunshots outside. Lots of them.", mercenary)

	dialog.say("What are you talking about?", kaelen)
	dialog.say("We're in a prison right in the heart of Velsingrad.", kaelen)
	dialog.say("Those walls have stood longer than either of us.", kaelen)

	dialog.say("I know what I heard.", mercenary)
	dialog.say("Gunfire, explosions... and the alarm bells haven't stopped ringing.", mercenary)
	dialog.say("Something's happening out there.", mercenary)
	dialog.say("After three years in this cell, you've finally started imagining things?", kaelen)

	dialog.say("No, sir. Listen carefully.", mercenary)

	dialog.say("...", kaelen)
	dialog.say("By the gods...", kaelen)
	dialog.say("You're right.", kaelen)

	dialog.say("Do you think we're under attack?", mercenary)
	dialog.say("I don't know.", kaelen)
	dialog.say("But nobody attacks Velsingrad without a reason.", kaelen)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func _emit_dialog_end():
	dialog_ended.emit()
