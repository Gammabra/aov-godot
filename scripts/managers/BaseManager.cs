using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
/// Base class for all manager singletons in the game.
/// Implements the Manager Pattern from your architecture documentation.
/// </summary>
public abstract partial class BaseManager : Node
{
    public static BaseManager? Instance { get; protected set; }

    public override void _Ready()
    {
        // Pour AutoLoad, l'initialisation se fait immédiatement
        if (IsInsideTree() && GetParent() == GetTree().Root)
        {
            Initialize();
        }
        // Pour les instances manuelles, vérifier les doublons
        else if (Instance == null)
        {
            Initialize();
        }
        else
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
            QueueFree();
        }
    }

    protected abstract void Initialize();

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Cleanup();
            Instance = null;
        }
    }

    protected virtual void Cleanup()
    {
        // Override in derived classes for cleanup logic
    }
}
