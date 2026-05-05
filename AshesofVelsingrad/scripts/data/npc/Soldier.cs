using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.data.npc;

public partial class Soldier : CharacterBody3D, IInteractable
{
    [Export]
    private NodePath? InteractTextPath;

    private Label3D InteractText;

    public override void _Ready()
    {
        InteractText = GetNode<Label3D>(InteractTextPath);
    }

    public void Interact(IInteractor interactor)
    {
        // Implement Interact logic
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
