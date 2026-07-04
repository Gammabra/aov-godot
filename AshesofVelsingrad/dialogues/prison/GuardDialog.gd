extends Node

signal open_door()
signal dialog_ended()

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

func talk() -> void:
	dialog.say("Des bruits de pas précipités se dirigent droit vers votre porte de cellule.", narrateur)
	dialog.say("P-pitié ! Sortez-nous de là !", mercenary)
	dialog.say("D-du calme, du calme !", guard)
	dialog.say("Le garde reprend son souffle. Une odeur de fer et de cendre emplit vos narines.", narrateur)
	dialog.say("Vous êtes blessé ?", kaelen)
	dialog.say("Si ce n'était que ça... La ville est à feu et à sang. Les habitants essayent de fuir, mais ils n'épargnent personne.", guard)
	dialog.say("Qui sont-ils ?", mercenary)
	dialog.say("Nous ne savons pas... Je n'avais jamais vu des brigands comme ça.", guard)
	dialog.say("Des brigands... capables de faire trembler Sarkavel ? Mais c'est impossible !", kaelen)
	dialog.say("Le garde rit nerveusement alors que vous l'entendez se tenir le ventre.", narrateur)
	dialog.say("Pas de simples pillards. Ils chargent comme des fous, sans peur, criant leur rage, prêts à mourir.", guard)
	dialog.say("Ils font brûler les maisons, éventrent les rues... et leur magie, je ne l'ai jamais vue. Ils sont équipés du meilleur matériel.", guard)
	dialog.say("J'ai connu ces murs plus solides que des montagnes.", kaelen)
	dialog.say("Ils n'ont pas tenu contre ça.", guard)
	dialog.say("Un silence lourd tombe entre vous.", narrateur)
	dialog.say("Alors pourquoi venir jusqu'ici ?", kaelen)
	dialog.say("Parce que nous n'avons plus assez de soldats.", guard)
	dialog.say("Vous en êtes donc là...", kaelen)
	dialog.say("Mais tout n'est pas perdu !", guard)
	dialog.say("Nous avons besoin de tous les combattants possibles.", guard)
	dialog.say("Kaelen Voss, ancien capitaine des Corbeaux d'Airain.", guard)
	dialog.say("Ce nom ne pèse plus grand-chose derrière ces barreaux.", kaelen)
	dialog.say("Vous êtes le fils prodige du grand capitaine qu'était votre père.", guard)
	dialog.say("L'homme qui a refusé d'abandonner la porte de Brumefer quand tout le front reculait.", guard)
	dialog.say("C'était il y a longtemps. Je n'étais encore qu'un garçon avec une lame trop lourde à cette époque.", kaelen)
	dialog.say("Justement. Vous avez ça dans le sang.", guard)
	dialog.say("Et moi ?", mercenary)
	dialog.say("Arthur, c'est ça ?", guard)
	dialog.say("Oui.", mercenary)
	dialog.say("Tu viens aussi. Si tu peux tenir une lame, tu peux sauver quelqu'un.", guard)
	dialog.say("Arthur avale sa salive. Il jette un regard vers vous, comme s'il attendait votre autorisation.", narrateur)
	dialog.say("Kaelen...", mercenary)
	dialog.say("Je sais, Arthur... je sais.", kaelen)
	dialog.say("Alors ? E-et si votre coeur vous commande la fuite... nul ne pourra vous blâmer aujourd'hui.", guard)
	dialog.say("Je veux que Kaelen Voss sorte d'ici avant que cette prison devienne son tombeau.", guard)
	dialog.say("Et après ?", kaelen)
	dialog.say("Après, vous choisirez ce que vous voulez être.", guard)
	dialog.say("Alors... ouvrez. S'il vous plaît.", kaelen)
	dialog.say("Le verrou hurle dans la serrure. La porte s'ouvre sur un couloir noyé de fumée.", narrateur)
	
	dialog.action("_emit_open_door")
	
	dialog.start_convo()

func talk2():
	dialog.say("Dépêchons-nous de sortir d'ici.", guard)
	dialog.say("Il y a un trou au fond du couloir, nous sortirons par-là.", guard)
	dialog.say("Le garde fait alors demi-tour.", narrateur)
	dialog.say("Reste près de moi. On avance ensemble.", kaelen)
	dialog.say("Oui capitaine !", mercenary)

	dialog.action("_emit_dialog_end")
	
	dialog.start_convo()

func _emit_open_door():
	open_door.emit()

func _emit_dialog_end():
	dialog_ended.emit()
