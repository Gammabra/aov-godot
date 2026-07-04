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
	dialog.say("Bien. Garde ça près de toi, on ne sait pas ce qui nous attend.", mercenary)
	dialog.say("Les objets ramassés sont rangés dans ton inventaire. Appuie sur I pour l'ouvrir.", narrateur)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func second():
	dialog.say("À gauche se trouve l'inventaire d'exploration. Les objets ramassés pendant ton voyage y apparaîtront.", narrateur)
	dialog.say("À droite se trouve l'inventaire personnel de chaque membre du groupe.", narrateur)
	dialog.say("En combat, les objets ne peuvent être utilisés que pendant le tour du personnage qui les porte.", narrateur)
	dialog.say("Pour équiper un personnage, fais glisser un objet vers l'un de ses emplacements d'équipement.", narrateur)
	dialog.say("L'inventaire d'exploration n'est pas accessible pendant les combats. Prépare ton groupe avant d'engager l'ennemi.", narrateur)
	dialog.say("Tout est en ordre ?", mercenary)
	dialog.say("Alors on avance.", mercenary)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func _emit_dialog_end():
	dialog_ended.emit()
