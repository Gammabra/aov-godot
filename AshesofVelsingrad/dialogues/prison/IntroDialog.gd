extends Node

signal dialog_ended()

var dio=Dialog.new()
var dialog:=dio.start(self)

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

func _ready() -> void:
	dialog.typewriter_speed=30

func talk():
	print("{Introduction to prison life} [Kaelen, Arthur]")
	dialog.say("The sound of explosions jolts you awake. You are in this cold cell you know all too well with your close companion, Arthur.", narrateur)
	dialog.say("Kaelen! Wh-what's happening?!", mercenary)
	dialog.say("You both get up, shocked, your hearts racing.", narrateur)
	dialog.say("Sarkavel ... The capital is being attacked!?!", kaelen)
	dialog.say("But how is that possible? No one can break its walls.", mercenary)
	dialog.say("Apparently, until today.", kaelen)
	dialog.say("A new crash shakes the stones. Dust falls from the ceiling, fine as ash.", narrateur)
	dialog.say("Arthur jumps to try to see through the bars too high. For a moment, you remember the kid who arrived here three years ago, still too young to bear the name of the Bronze Ravens.", narrateur)
	dialog.say("We have to get out of here! HEY! IS THERE ANYONE?!", mercenary)
	dialog.say("Good God...", mercenary)
	dialog.say("Arthur falls back to the ground. You had spoken about death together many times. The fear of dying without being able to do anything. Dying of hunger or disease. Ending up in a corner of Karst-Vel prison, rotting away slowly and eaten by rats.", narrateur)
	dialog.say("Three days...", mercenary)
	dialog.say("What?", kaelen)
	dialog.say("I had only worn the insignia for three days when they chained us.", mercenary)
	dialog.say("We've already talked about this, Arthur.", kaelen)
	dialog.say("The others got the axe. Me, the executioner's pity.", mercenary)
	dialog.say("His face tightens as tears well up.", narrateur)
	dialog.say("That wasn't pity.", kaelen)
	dialog.say("Then what?! A cruel joke?", mercenary)
	dialog.say("A witness they didn't know where to bury.", kaelen)
	dialog.say("Arthur grits his teeth. Fear is there, but it no longer makes him tremble like before.", narrateur)
	dialog.say("I thought I would die in this cell with you.", mercenary)
	dialog.say("Maybe a sign that our time hasn't come yet?", kaelen)
	dialog.say("I didn't know you were so pious.", mercenary)
	dialog.say("Maybe, but my prayers are useless if the ceiling collapses on our heads.", kaelen)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func _emit_dialog_end():
	dialog_ended.emit()
