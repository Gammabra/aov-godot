using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     The conductor of a level. The <c>GameManager</c> handles everything that a level needs to work correctly.
/// </summary>
public partial class GameManager : BaseManager
{
    public new static GameManager? Instance { get; private set; }

    /// <summary>
    ///     Initializes the GameManager singleton instance.
    ///     Ensures only one instance exists and sets up the initial state.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is ready.
    ///     It checks for duplicate instances and initializes the game system.
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
        GD.Print("GameManager initialized successfully");
    }
}
