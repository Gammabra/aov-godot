extends Node

signal battle_started(interactor)
signal dialog_ended(interactor)

var interactor: Node
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

func _ready() -> void:
	dialog.typewriter_speed=30

func set_interactor(i) -> void:
	print("[npc_solder] set_interactor called with: ", i)
	interactor = i

func talk() -> void:
	print("[npc_solder] talk() invoked — interactor=", interactor)
	dialog.say("There you are.", guard)
	dialog.say("The guard stands near a wide breach in the wall. Beyond it, the cries and flames of Sarkavel await you.", narrateur)
	dialog.say("By the gods... How could they make such a hole?", mercenary)
	dialog.say("We don't know, but the prison wasn't their target. It just happened to be in their way.", guard)
	dialog.say("So the city is really breached.", kaelen)
	dialog.say("Yes. And if we still want to save someone, we must go through this breach before they return.", guard)

	dialog.menu("Your decision?", {
			"Defend the capital": "yes_function",
			"I need a moment": "no_function",
		})
	dialog.start_convo()

func yes_function() -> void:
	print("[npc_solder] yes_function() — adding 2 lines + start_fight action")
	dialog.say("Perfect! Take these weapons and let's go.", guard)
	dialog.say("Stay behind me, Arthur.", kaelen)
	dialog.say("I'll do my best.", mercenary)
	dialog.action("start_fight")

func no_function() -> void:
	print("[npc_solder] no_function() — peace path")
	dialog.say("Then hurry. We can't stay here.", guard)
	dialog.action("_emit_dialog_ended")

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

func _emit_dialog_ended():
	dialog_ended.emit(interactor)
