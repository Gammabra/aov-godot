extends Node

signal dialog_ended()

var dio=Dialog.new()
var dialog:=dio.start(self)

var guard = dialog.Character(
		"Guard",
		Color.CORNFLOWER_BLUE,
		"res://assets/Krita/icone_solder.png"
	)
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

func talk() -> void:
	dialog.say("Mr. Voss... Velsingrad is being attacked!", guard)
	dialog.say("The city is in chaos. Fighting has broken out in every district.", guard)

	dialog.say("Slow down. Tell me what's happening.", kaelen)

	dialog.say("We're losing control of the city.", guard)
	dialog.say("I've served here twelve years... I've never seen it like this.", guard)

	dialog.say("The walls still stand?", kaelen)

	dialog.say("No.", guard)
	dialog.say("They're through. The outer defenses are gone.", guard)

	dialog.say("I see.", kaelen)
	dialog.say("And you came here for help.", kaelen)

	dialog.say("Yes. We need fighters.", guard)
	dialog.say("Experienced ones.", guard)

	dialog.say("You came to a prison for that?", kaelen)

	dialog.say("Not prisoners.", guard)
	dialog.say("Mercenaries.", guard)

	dialog.say("You're asking for our help.", kaelen)

	dialog.say("Anyone who can still fight.", guard)
	dialog.say("We can't hold them back without it.", guard)

	dialog.say("What are we facing?", kaelen)

	dialog.say("Not a normal army.", guard)
	dialog.say("They charge without fear... and their magic breaks stone like glass.", guard)

	dialog.say("...", kaelen)
	dialog.say("So Velsingrad is already falling.", kaelen)

	dialog.say("It's collapsing.", guard)
	dialog.say("But there's still time to save people.", guard)

	dialog.say("And you came here anyway.", kaelen)

	dialog.say("Because your name still matters out there.", guard)
	dialog.say("And because your father believed in you.", guard)

	dialog.say("...", kaelen)
	dialog.say("Open the cell.", kaelen)

	dialog.say("I'll wait for you outside.", guard)

	dialog.action("_emit_dialog_end")
	
	dialog.start_convo()


func _emit_dialog_end():
	dialog_ended.emit()
