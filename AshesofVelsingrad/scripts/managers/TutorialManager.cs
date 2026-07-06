using System.Threading.Tasks;
using AshesOfVelsingrad.data.npc;
using AshesOfVelsingrad.player;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.tutorial;
using AshesOfVelsingrad.ui;
using Godot;

namespace AshesOfVelsingrad.Managers;

public enum TutorialStep
{
	Start,
	IntroSequence,
	IntroDialog,
	GuardDialog,
}

public partial class TutorialManager : Node
{
	private TutorialStep _step = TutorialStep.Start;

	[Export]
	private NodePath _introDialogPath = null!;

	[Export]
	private NodePath _guardDialogPath = null!;

	[Export]
	private NodePath _foundFirstItemDialogPath = null!;

	[Export]
	private NodePath _interactionExplanationDialogPath = null!;

	[Export]
	private NodePath _inventoryExplanationDialogPath = null!;

	[Export]
	private NodePath _playerPath = null!;

	[Export]
	private NodePath _miniMercenaryPath = null!;

	[Export]
	private NodePath _introSequencePath = null!;

	[Export]
	private NodePath _firstItemDetectionAreaPath = null!;

	[Export]
	private NodePath _firstItemPath = null!;

	[Export]
	private NodePath _triggerInteractionExplanationAreaPath = null!;

	[Export]
	private NodePath _tutorialLayerPath = null!;

	[Export]
	private NodePath _movableDoorPath = null!;

	private Node _introDialog = null!;
	private Node _guardDialog = null!;
	private Node _foundFirstItemDialog = null!;
	private Node _interactionExplanationDialog = null!;
	private Node _inventoryExplanationDialog = null!;
	private AovPlayer _player = null!;
	private MiniMercenary _miniMercenary = null!;
	private TextSequence _introSequence = null!;
	private ItemDetectionArea _firstItemDetectionArea = null!;
	private Area3D _triggerInteractionExplanationArea = null!;
	private ItemSystem _firstItem = null!;
	private CanvasLayer _tutorialLayer = null!;
	private MovableDoor _movableDoor = null!;

	private readonly (string, int, float)[] _sequences = [
		("Prologue", 50, 3),
		("Kingdom of Velsingrad, 16th Century", 40, 3),
		("The great kingdom of Velsingrad is one of the most powerful on the continent.", 25, 7),
		("From its capital, Sarkavel, King Edric reigns over a people \nwho have long lived in security against the horror that surrounds them.", 25, 7),
		("The kingdom owes much of this stability to the Bronze Ravens.", 25, 7),
		("This prestigious guild of mercenaries is sent \nwhere the army cannot suffice: \nmonsters, bandits, forgotten creatures...", 25, 7),
		("For generations, they have protected Velsingrad \nfrom the most dangerous threats.", 25, 7),
		("At their head stood a man whom the king \ntrusted absolutely: \nCaptain Armand Voss.", 25, 7),
		("Upon his death, his son Kaelen inherited command \nand became the new captain of the Bronze Ravens.", 25, 7),
		("As time went on, the king's great paranoia grew even further.", 25, 7),
		("Taking advantage of this, shadows whispered to him \nthat the Bronze Ravens were plotting his assassination.", 25, 7),
		("Convinced of these words, he ordered their execution. \nIn a single night, the guild was annihilated without any trial.", 25, 7),
		("Thanks to the intervention and love of Princess Elyra, \nKaelen escaped death, but was condemned to life imprisonment.", 25, 7),
		("It is after three years of rotting in a \nseedy cell with a young recruit that the fate of the \nlast captain of the Bronze Ravens is about to change...", 25, 7)
	];

	public bool CanMove { get; private set; }
	public bool CanToggleInventory { get; private set; }
	public bool IsOnlyToggleInventory { get; private set; }

	private void DoIntroDialogStep()
	{
		_introDialog.Call("talk");
	}

	private async Task DoGuardDialog()
	{
		await ToSignal(GetTree().CreateTimer(1f),
			SceneTreeTimer.SignalName.Timeout);
		_guardDialog.Call("talk");
	}

	private void DoIntroSequences()
	{
		_ = _introSequence.PlaySequence(_sequences);
	}

	private void OnFirstItemDetectionAreaBodyEntered(Node3D body)
	{
		if (body == _miniMercenary)
		{
			_miniMercenary.ToggleNoticed();
			_miniMercenary.CollisionLayer = 2;
			_miniMercenary.CollisionMask = 2;
			CanMove = false;
			_miniMercenary.OnSpecificPointReached += DoFoundFirstItemDialog;
		}
	}

	private void OnTriggerInteractionExplanationAreaBodyEntered(Node3D body)
	{
		if (body == _player)
		{
			CanMove = false;
			_interactionExplanationDialog.Call("talk");
			_triggerInteractionExplanationArea.QueueFree();
		}
	}

	private void DoFoundFirstItemDialog()
	{
		_miniMercenary.ToIdle(true, "IdleBackwardState");
		_foundFirstItemDialog.Call("talk");
	}

	private void OnFirstItemInteracted()
	{
		CanMove = false;
		_inventoryExplanationDialog.Call("first");
	}

	private void HandleFoundFirstItemDialogEnd()
	{
		_miniMercenary.CollisionLayer = 4;
		_miniMercenary.CollisionMask = 1;
		CanMove = true;
		_miniMercenary.OnSpecificPointReached -= DoFoundFirstItemDialog;
		_firstItemDetectionArea.QueueFree();
	}

	private void HandleInteractionExplanationDialogEnd()
	{
		CanMove = true;
		_player.ToggleInteraction(true);
	}

	private void HandleFirstInventoryExplanationDialogEnd()
	{
		IsOnlyToggleInventory = true;
		_inventoryExplanationDialog.Disconnect("dialog_ended", Callable.From(HandleFirstInventoryExplanationDialogEnd));
		_inventoryExplanationDialog.Connect("dialog_ended", Callable.From(HandleSecondInventoryExplanationDialogEnd));
		_tutorialLayer.Show();
	}

	private void HandleSecondInventoryExplanationDialogEnd()
	{
		CanMove = true;
		CanToggleInventory = true;
		_miniMercenary.ToFollowingMovingEntity(_player);
	}

	public override async void _Ready()
	{
		_player = GetNode<AovPlayer>(_playerPath);
		_miniMercenary = GetNode<MiniMercenary>(_miniMercenaryPath);
		_tutorialLayer = GetNode<CanvasLayer>(_tutorialLayerPath);
		_tutorialLayer.Hide();
		_firstItem = GetNode<ItemSystem>(_firstItemPath);
		_firstItem.Interacted += OnFirstItemInteracted;
		_introDialog = GetNode<Node>(_introDialogPath);
		_introDialog.Connect("dialog_ended", Callable.From(GoToNextStep));
		_guardDialog = GetNode<Node>(_guardDialogPath);
		_guardDialog.Connect("open_door", Callable.From(() => _movableDoor.ToggleDoor()));
		_guardDialog.Connect("dialog_ended", Callable.From(() => CanMove = true));
		_foundFirstItemDialog = GetNode<Node>(_foundFirstItemDialogPath);
		_foundFirstItemDialog.Connect("dialog_ended", Callable.From(HandleFoundFirstItemDialogEnd));
		_interactionExplanationDialog = GetNode<Node>(_interactionExplanationDialogPath);
		_interactionExplanationDialog.Connect("dialog_ended", Callable.From(HandleInteractionExplanationDialogEnd));
		_inventoryExplanationDialog = GetNode<Node>(_inventoryExplanationDialogPath);
		_inventoryExplanationDialog.Connect("dialog_ended", Callable.From(HandleFirstInventoryExplanationDialogEnd));
		_introSequence = GetNode<TextSequence>(_introSequencePath);
		_introSequence.OnSequenceEnded += GoToNextStep;
		_firstItemDetectionArea = GetNode<ItemDetectionArea>(_firstItemDetectionAreaPath);
		_firstItemDetectionArea.BodyEntered += OnFirstItemDetectionAreaBodyEntered;
		_firstItemDetectionArea.OnStartedToMove += () => _miniMercenary.ToggleNoticed();
		_triggerInteractionExplanationArea = GetNode<Area3D>(_triggerInteractionExplanationAreaPath);
		_triggerInteractionExplanationArea.BodyEntered += OnTriggerInteractionExplanationAreaBodyEntered;
		_movableDoor = GetNode<MovableDoor>(_movableDoorPath);
		_movableDoor.TogglingDoorFinished += () => _guardDialog.Call("talk2");

		await ToSignal(_introSequence, Node.SignalName.Ready);
		GoToNextStep();
	}

	public void GoToNextStep()
	{
		_step++;

		switch (_step)
		{
			case TutorialStep.IntroSequence:
				DoIntroSequences();
				break;
			case TutorialStep.IntroDialog:
				DoIntroDialogStep();
				break;
			case TutorialStep.GuardDialog:
				_ = DoGuardDialog();
				break;
		}
	}

	public void DoSecondInventoryExplationDialog()
	{
		_tutorialLayer.Hide();
		IsOnlyToggleInventory = false;
		_inventoryExplanationDialog.Call("second");
	}
}
