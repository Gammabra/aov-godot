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
	dialog.say("Des bruits d'explosion vous réveillent en sursaut. Tu es dans cette cellule froide que tu ne connais que trop bien avec ton proche compagnon, Arthur.", narrateur)
	dialog.say("Kaelen ! Q-que se passe-t-il ?!", mercenary)
	dialog.say("Vous vous levez tous les deux, choqués, le coeur battant à mille à l'heure.", narrateur)
	dialog.say("Sarkavel ... L-la capitale se fait attaquer !?", kaelen)
	dialog.say("Mais comment cela est possible ? Personne ne peut briser ses murs.", mercenary)
	dialog.say("Jusqu'à aujourd'hui apparemment.", kaelen)
	dialog.say("Un nouveau fracas secoue les pierres. De la poussière tombe du plafond, fine comme de la cendre.", narrateur)
	dialog.say("Arthur saute pour essayer de voir à travère les barreaux trop haut. Pendant un instant, vous revoyez le gamin arrivé ici trois ans plus tôt, encore trop jeune pour porter le nom des Corbeaux d'Airain.", narrateur)
	dialog.say("Il faut qu'on sorte d'ici ! EH OH ! IL Y A QUELQU'UN !!?", mercenary)
	dialog.say("Non d'un chien...", mercenary)
	dialog.say("Arthur retombe au sol. Vous aviez plusieurs fois parlé de la mort ensemble. De la peur de mourir sans ne rien pouvoir faire. Mourir de famine ou de maladie. De finir dans un coin de la prison de Karst-Vel à se putréfié petit à petit et dévoré par les rats.", narrateur)
	dialog.say("Trois jours...", mercenary)
	dialog.say("Quoi ?", kaelen)
	dialog.say("Je n'avais porté l'insigne que trois jours quand ils nous ont enchaînés.", mercenary)
	dialog.say("Nous en avont déjà parlé Arthur.", kaelen)
	dialog.say("Les autres ont eu la hache. Moi, la pitié du bourreau.", mercenary)
	dialog.say("Son visage se plisse alors que les larmes lui monte.", narrateur)
	dialog.say("Ce n'était pas de la pitié.", kaelen)
	dialog.say("Alors quoi ?! Une blague cruelle ?", mercenary)
	dialog.say("Un témoin qu'ils ne savaient pas où enterrer.", kaelen)
	dialog.say("Arthur serre les dents. La peur est là, mais elle ne le fait plus trembler comme avant.", narrateur)
	dialog.say("Je croyais que je mourrais dans cette cellule avec toi.", mercenary)
	dialog.say("Peut être un signe pour nous dire que notre heure n'est pas encore venue ?.", kaelen)
	dialog.say("Je ne te connaissais pas aussi pieux.", mercenary)
	dialog.say("Peut être mais mes prière ne serve à rien si le plafond nous tombe sur la tête.", kaelen)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func _emit_dialog_end():
	dialog_ended.emit()
