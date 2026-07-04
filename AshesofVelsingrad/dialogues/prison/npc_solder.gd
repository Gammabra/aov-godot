extends Node

signal battle_started(interactor)

var interactor: Node
var dio=Dialog.new()
var dialog:=dio.start(self)

var guard = dialog.Character(
		"Garde",
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
	dialog.say("Vous voilà.", guard)
	dialog.say("Le garde se tient près d'une large brèche ouverte dans le mur. Au-delà, les cries et les flammes de Sarkavel vous attendent.", narrateur)
	dialog.say("Par les dieux... Comment ont-ils pu faire un troue paraille ?", mercenary)
	dialog.say("Nous ne savons pas mais la prison n'était pas leur cible. Elle s'est seulement trouvée sur leur chemin.", guard)
	dialog.say("Alors la ville est vraiment percée.", kaelen)
	dialog.say("Oui. Et si nous voulons encore sauver quelqu'un, il faut passer par cette brèche avant qu'ils ne reviennent.", guard)

	dialog.menu("Votre décision ?", {
			"Défendre la capitale": "yes_function",
			"J'ai besoin d'un instant": "no_function",
		})
	dialog.start_convo()

func yes_function() -> void:
	print("[npc_solder] yes_function() — adding 2 lines + start_fight action")
	dialog.say("Parfait ! Prenez ces armes et allons-y.", guard)
	dialog.say("Reste derrière moi, Arthur.", kaelen)
	dialog.say("Je ferai de mon mieux.", mercenary)
	dialog.action("start_fight")

func no_function() -> void:
	print("[npc_solder] no_function() — peace path")
	dialog.say("Alors faites vite. Nous ne pouvons nous éterniser ici.", guard)

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
