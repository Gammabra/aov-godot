using AshesOfVelsingrad.player;
using AshesOfVelsingrad.ui;
using Godot;
using System.Threading.Tasks;
using AshesOfVelsingrad.data.npc;
using AshesOfVelsingrad.Systems;

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

	private Node _introDialog = null!;
	private Node _guardDialog = null!;
	private Node _foundFirstItemDialog = null!;
	private Node _interactionExplanationDialog = null!;
	private Node _inventoryExplanationDialog = null!;
	private AovPlayer _player = null!;
	private MiniMercenary _miniMercenary = null!;
	private TextSequence _introSequence = null!;
	private Area3D _firstItemDetectionArea = null!;
	private Area3D _triggerInteractionExplanationArea = null!;
	private ItemSystem _firstItem = null!;
	private CanvasLayer _tutorialLayer = null!;
	// TODO: Fill the tuple to have the complete intro sequence
	private readonly (string, int, float)[] _sequences = [
		("Prologue", 50, 3)
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
		_guardDialog.Connect("dialog_ended", Callable.From(() => CanMove = true));
		_foundFirstItemDialog = GetNode<Node>(_foundFirstItemDialogPath);
		_foundFirstItemDialog.Connect("dialog_ended", Callable.From(HandleFoundFirstItemDialogEnd));
		_interactionExplanationDialog = GetNode<Node>(_interactionExplanationDialogPath);
		_interactionExplanationDialog.Connect("dialog_ended", Callable.From(HandleInteractionExplanationDialogEnd));
		_inventoryExplanationDialog = GetNode<Node>(_inventoryExplanationDialogPath);
		_inventoryExplanationDialog.Connect("dialog_ended", Callable.From(HandleFirstInventoryExplanationDialogEnd));
		_introSequence = GetNode<TextSequence>(_introSequencePath);
		_introSequence.OnSequenceEnded += GoToNextStep;
		_firstItemDetectionArea = GetNode<Area3D>(_firstItemDetectionAreaPath);
		_firstItemDetectionArea.BodyEntered += OnFirstItemDetectionAreaBodyEntered;
		_triggerInteractionExplanationArea = GetNode<Area3D>(_triggerInteractionExplanationAreaPath);
		_triggerInteractionExplanationArea.BodyEntered += OnTriggerInteractionExplanationAreaBodyEntered;

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
