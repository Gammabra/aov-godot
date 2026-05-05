extends Button

var dio=Dialog.new()
var dialog:=dio.start(self)
#Character(NPC_name, font color, npc image)
var s=dialog.Character("Solder", Color.REBECCA_PURPLE) 
var kaelen=dialog.Character("Kaelen Voss", Color.BROWN)
var ym=dialog.Character("Arthur", Color.YELLOW_GREEN)

func _ready() -> void:
	dialog.typewriter_speed=30
	
func talk():
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
			"I need time to think": "No_function",
		})

func yes_function():
	dialog.say("Alright. Then we move. Now.", s)
	dialog.say("Good. Let them come.", ym)
	dialog.action("start_Fight")

func No_function():
	dialog.say("Alright... but make it quick. We don’t have much time.", s)
	
func _on_pressed() -> void:
	talk()

func start_Fight():
	print("démarrer le combat !")
