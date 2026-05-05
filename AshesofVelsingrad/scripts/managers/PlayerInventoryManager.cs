using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Inventory;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     Autoload singleton that owns the player's persistent inventory.
///     Survives scene transitions. Both the exploration layer and the
///     battle layer read from / write to this single instance.
/// </summary>
public sealed partial class PlayerInventoryManager : Node
{
    public static PlayerInventoryManager? Instance { get; private set; }

    /// <summary>
    ///     The one global inventory. Capacity can be tuned here or made
    ///     data-driven later.
    /// </summary>
    public InventorySystem Inventory { get; } = new(capacity: InventoryConstants.ExplorationCapacity);

    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr("Duplicate PlayerInventoryManager — removing.");
            QueueFree();
            return;
        }

        Instance = this;
        GD.Print("PlayerInventoryManager ready.");
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }
}
