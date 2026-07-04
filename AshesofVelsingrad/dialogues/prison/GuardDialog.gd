extends Node

signal open_door()
signal dialog_ended()

var dio=Dialog.new()
var dialog:=dio.start(self)

var guard = dialog.Character(
		"Guard of Velsingrad",
		Color("a53030"),
		"res://assets/Krita/icone_solder.png"
	)
var narrateur = dialog.Character(
		"",
		Color("ebede9"),
		" "
	)
var kaelen = dialog.Character(
		"Kaelen",
		Color("75a743"),
		"res://assets/Krita/icone_player.png"
	)
var mercenary = dialog.Character(
		"Arthur",
		Color("be772b"),
		"res://assets/Krita/icone_mercenaire.png"
	)

func talk() -> void:
	dialog.say("The sound of hurried footsteps heads straight for your cell door.", narrateur)
	dialog.say("P-please! Get us out of here!", mercenary)
	dialog.say("C-calm down, calm down!", guard)
	dialog.say("The guard catches his breath. A smell of iron and ash fills your nostrils.", narrateur)
	dialog.say("Are you hurt?", kaelen)
	dialog.say("If only it were just that... The city is burning and bleeding. The people are trying to flee, but they spare no one.", guard)
	dialog.say("Who are they?", mercenary)
	dialog.say("We don't know... I had never seen brigands like that.", guard)
	dialog.say("Brigands... capable of shaking Sarkavel? But it's impossible!", kaelen)
	dialog.say("The guard laughs nervously as you hear him clutch his stomach.", narrateur)
	dialog.say("Not mere looters. They charge like madmen, fearless, shouting their rage, ready to die.", guard)
	dialog.say("They burn houses, tear open the streets... and their magic, I've never seen it before. They are equipped with the best gear.", guard)
	dialog.say("I knew these walls to be stronger than mountains.", kaelen)
	dialog.say("They didn't hold against that.", guard)
	dialog.say("A heavy silence falls between you.", narrateur)
	dialog.say("Then why come all the way here?", kaelen)
	dialog.say("Because we don't have enough soldiers left.", guard)
	dialog.say("So it has come to this...", kaelen)
	dialog.say("But not all is lost!", guard)
	dialog.say("We need every fighter we can get.", guard)
	dialog.say("Kaelen Voss, former captain of the Bronze Ravens.", guard)
	dialog.say("That name doesn't carry much weight behind these bars.", kaelen)
	dialog.say("You are the prodigy son of the great captain your father was.", guard)
	dialog.say("The man who refused to abandon the Mistgate when the whole front fell back.", guard)
	dialog.say("That was a long time ago. I was still a boy with a blade too heavy for me back then.", kaelen)
	dialog.say("Exactly. You've got it in your blood.", guard)
	dialog.say("And me?", mercenary)
	dialog.say("Arthur, is that right?", guard)
	dialog.say("Yes.", mercenary)
	dialog.say("You come too. If you can hold a blade, you can save someone.", guard)
	dialog.say("Arthur swallows hard. He glances at you as if waiting for your permission.", narrateur)
	dialog.say("Kaelen...", mercenary)
	dialog.say("I know, Arthur... I know.", kaelen)
	dialog.say("So? A-and if your heart tells you to flee... no one can blame you today.", guard)
	dialog.say("I want Kaelen Voss out of here before this prison becomes his tomb.", guard)
	dialog.say("And after?", kaelen)
	dialog.say("Afterwards, you'll choose what you want to be.", guard)
	dialog.say("So... open up. Please.", kaelen)
	dialog.say("The lock screams in the keyhole. The door opens onto a corridor drowned in smoke.", narrateur)
	
	dialog.action("_emit_open_door")
	
	dialog.start_convo()

func talk2():
	dialog.say("Let's hurry and get out of here.", guard)
	dialog.say("There's a hole at the end of the corridor, we'll escape through there.", guard)
	dialog.say("The guard then turns back.", narrateur)
	dialog.say("Stay close to me. We move together.", kaelen)
	dialog.say("Yes, captain!", mercenary)

	dialog.action("_emit_dialog_end")
	
	dialog.start_convo()

func _emit_open_door():
	open_door.emit()

func _emit_dialog_end():
	dialog_ended.emit()
