using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.tutorial;

public partial class ExitPrisonArea : StaticBody3D, IInteractable
{
	[Export]
	private NodePath _exitLabelPath = null!;

	private Label3D _exitLabel = null!;

	public override void _Ready()
	{
		_exitLabel = GetNode<Label3D>(_exitLabelPath);
		GD.Print(_exitLabel.Text);
	}

	public bool CanInteract() => true;

	public void Interact(IInteractor interactor)
	{
		GetTree().ChangeSceneToFile("res://scenes/Level/OpenWorld.tscn");
	}

	public void ShowPrompt()
	{
		_exitLabel.Visible = true;
	}

	public void HidePrompt()
	{
		_exitLabel.Visible = false;
	}
}
