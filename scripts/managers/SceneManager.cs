using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     The <c>SceneManager</c> handles every inside level related informations.
/// </summary>
/// <remarks>
///     This manager handles the <c>TurnSystem</c>, <c>InventorySystem</c>, <c>MapSystem</c> and the <c>DialogSystem</c>
/// </remarks>
public partial class SceneManager : BaseManager
{
    public new static SceneManager? Instance { get; private set; }

    /// <summary>
    ///     Initializes the SceneManager singleton instance.
    ///     Ensures only one instance exists and sets up the initial state.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is ready.
    ///     It checks for duplicate instances and initializes the scene system.
    ///     If a duplicate instance is found, it removes the duplicate.
    /// </remarks>
    protected override void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Instance = this;
        GD.Print("SceneManager initialized successfully");
    }
}
