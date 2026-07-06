extends Node

signal dialog_ended()

var dio=Dialog.new()
var dialog:=dio.start(self)

var narrateur = dialog.Character(
		"",
		Color("ebede9"),
		" "
	)

func _ready() -> void:
	dialog.typewriter_speed=30

func talk():
	dialog.say("Welcome to combat. Let's take a moment to learn how the battle interface works.", narrateur)
	dialog.say("In the top-left corner, you'll find a description of the current combat phase. It tells you what is expected of you.", narrateur)
	dialog.say("At the top-center of the screen is the turn order. It shows which character or enemy will act next.", narrateur)
	dialog.say("In the top-right corner, you'll find the list of enemies. Their HP represents their Health Points, while their MP represents their Mana Points. An enemy is defeated when their HP reaches 0.", narrateur)
	dialog.say("In the bottom-left corner, you can see the HP and MP of the character whose turn it currently is.", narrateur)
	dialog.say("At the bottom-center is the action bar. From here, you can use skills, open your inventory, or end your turn.", narrateur)
	dialog.say("In the bottom-right corner, the combat log records every action that happens during the battle.", narrateur)
	dialog.say("The battlefield is displayed in the center of the screen. During your turn, click on the green or red tiles to perform actions depending on the selected skill.", narrateur)
	dialog.say("Your turn ends as soon as you attack or choose to pass your turn.", narrateur)
	dialog.say("You won't fight alone. Your allies act automatically and will make their own decisions during their turns.", narrateur)
	dialog.say("Make good use of your skills. Some deal damage, while others can protect or support your allies.", narrateur)
	dialog.say("Don't forget about your inventory. Using the right item at the right time can completely change the outcome of a battle.", narrateur)
	dialog.say("That's everything you need to know to get started. Good luck!", narrateur)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func _emit_dialog_end():
	dialog_ended.emit()
