using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.player;

public partial class InteractionComponent : Area3D
{
    [Export]
    private NodePath _animatedSprite3DPath;

    private AnimatedSprite3D _animatedSprite3D;
    private readonly List<Node3D> _interactableObjects = [];

    public Node3D? ClosestInteractable { get; private set; }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not IInteractable)
            return;
        if (_interactableObjects.Contains(body))
            return;
        _interactableObjects.Add(body);
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not IInteractable interactable)
            return;
        if (!_interactableObjects.Contains(body))
            return;
        _interactableObjects.Remove(body);
        interactable?.HidePrompt();
        if (ClosestInteractable == body || _interactableObjects.Count == 0)
            ClosestInteractable = null;
    }

    public override void _Ready()
    {
        _animatedSprite3D = GetNode<AnimatedSprite3D>(_animatedSprite3DPath);
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _Process(double delta)
    {
        float closest = 0;

        foreach (Node3D body in _interactableObjects)
        {
            float distance = _animatedSprite3D.GlobalPosition.DistanceTo(body.GlobalPosition);

            if (closest == 0 || distance < closest)
            {
                closest = distance;
                ClosestInteractable = body;
            }
        }

        foreach (Node3D body in _interactableObjects)
        {
            IInteractable? interactable = body as IInteractable;
            if (body == ClosestInteractable)
            {
                interactable?.ShowPrompt();
                continue;
            }
            interactable?.HidePrompt();
        }
    }
}
