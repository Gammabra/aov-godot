extends Node

signal dialog_ended()

var dio=Dialog.new()
var dialog:=dio.start(self)

var mercenary = dialog.Character(
		"Mercenary",
		Color.YELLOW_GREEN,
		"res://assets/Krita/icone_mercenaire.png"
	)

func _ready() -> void:
	dialog.typewriter_speed=30

func first():
	dialog.say("Good! You'll find it in your inventory.", mercenary)
	dialog.say("You can open it with the (I) keybind.", mercenary)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func second():
	dialog.say("On the left, you'll find your exploration inventory. Any items you pick up will appear here.", mercenary)
	dialog.say("On the right, you'll see the personal inventory of each party member.", mercenary)
	dialog.say("This will be very useful during battles, as items can only be used during a party member's turn.", mercenary)
	dialog.say("You can equip a party member simply by dragging an item into one of their equipment slots.", mercenary)
	dialog.say("Remember, you won't have access to the exploration inventory during battle, so make sure everyone is properly equipped before heading into combat.", mercenary)
	dialog.say("That's all. I think you're ready now.", mercenary)

	dialog.action("_emit_dialog_end")
	dialog.start_convo()

func _emit_dialog_end():
	dialog_ended.emit()
