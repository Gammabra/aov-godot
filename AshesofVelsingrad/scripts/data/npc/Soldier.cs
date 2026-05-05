using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.data.npc;

public partial class Soldier : CharacterBody3D, IInteractable
{
    [Export]
    private NodePath? _interactTextPath;

    [Export]
    private string? _battleSceneToCharge;

    private Label3D InteractText;

    public override void _Ready()
    {
        InteractText = GetNode<Label3D>(_interactTextPath);
    }

    public void Interact(IInteractor interactor)
    {
        GetTree().ChangeSceneToFile(_battleSceneToCharge);
    }

    public bool CanInteract()
    {
        return true;
    }

    public void ShowPrompt()
    {
        InteractText.Visible = true;
    }

    public void HidePrompt()
    {
        InteractText.Visible = false;
    }
}
