extends Node

signal battle_started(interactor)

var interactor: Node
var dio=Dialog.new()
var dialog:=dio.start(self)
#Character(NPC_name, font color, npc image)
var s=dialog.Character("Solder", Color.REBECCA_PURPLE, "res://assets/Krita/icone_solder.png") 
var kaelen=dialog.Character("Kaelen Voss", Color.BROWN, "res://assets/Krita/icone_player.png")
var ym=dialog.Character("Arthur", Color.YELLOW_GREEN, "res://assets/Krita/icone_mercenaire.png")

func _ready() -> void:
	dialog.typewriter_speed=30

func set_interactor(i) -> void:
	print("[npc_solder] set_interactor called with: ", i)
	interactor = i

func talk() -> void:
	print("[npc_solder] talk() invoked — interactor=", interactor)
	dialog.say("Mr. Voss... finally. Hm... you're looking more and more like your father.", s)
	dialog.say("It's been three years since you were locked up here...", s)
	dialog.say("But we don’t have time for that.", s)

	dialog.say("What’s that noise outside? Sounds like... a battle.", ym)
	dialog.say("Thanks for getting us out. But tell us—what’s going on?", kaelen)

	dialog.say("Sarkavel is under attack.", s)
	dialog.say("...What? That’s impossible. That city can’t fall.", ym)

	dialog.say("I wish you were right... but you’re not.", s)
	dialog.say("These aren’t just bandits. They charge without thinking, like they don’t fear death anymore.", s)
	dialog.say("They’re burning everything. Houses, streets... all of it.", s)
	dialog.say("And their magic... something’s wrong. I’ve never seen anything like it.", s)

	dialog.say("We won’t hold much longer.", s)
	dialog.say("We need you, Mr. Voss.", s)

	dialog.say("What do we do?", ym)

	dialog.menu("Your decision?", {
			"We help defend the capital": "yes_function",
			"I need time to think": "no_function",
		})

func yes_function() -> void:
	print("[npc_solder] yes_function() — adding 2 lines + start_fight action")
	dialog.say("Alright. Then we move. Now.", s)
	dialog.say("Good. Let them come.", ym)
	dialog.action("start_fight")

func no_function() -> void:
	print("[npc_solder] no_function() — peace path")
	dialog.say("Alright... but make it quick. We don’t have much time.", s)

func _on_pressed() -> void:
	print("[npc_solder] _on_pressed() — restarting talk()")
	talk()

func start_fight():
	print("[npc_solder] start_fight() called — interactor=", interactor)
	if interactor == null:
		push_error("battle_started: interactor is null")
		return
	print("[npc_solder] emitting battle_started signal")
	battle_started.emit(interactor);
