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

func talk():
	dialog.say("Regardez ici, sur le sol.", mercenary)
	dialog.say("Ça pourrait nous servir, non ?", mercenary)
	dialog.say("Lorsque tu te trouves près d'un objet ou d'une personne avec qui interagir, appuie sur E pour agir.", narrateur)
	dialog.say("Certains éléments du décor, personnages ou objets pourront être examinés, ramassés ou activés de cette manière.", narrateur)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func _emit_dialog_end():
	dialog_ended.emit()
