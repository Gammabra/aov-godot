using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Inventory;
using Godot;

namespace AshesOfVelsingrad.Managers;

public sealed partial class PlayerInventoryManager : Node
{
    public const int MaxPartySize = 4;

    public static PlayerInventoryManager? Instance { get; private set; }

    public InventorySystem GlobalInventory { get; } =
        new(InventoryConstants.ExplorationCapacity);

    /// <summary>
    ///     One persistent 5-slot loadout per party slot.
    ///     Index 0 = slot for first unit, 1 = second, etc.
    ///     These survive scene transitions; units read from them at battle start.
    /// </summary>
    public InventorySystem[] PartyLoadouts { get; } = new InventorySystem[MaxPartySize];

    /// <summary>Display names for each party slot, set when units are registered.</summary>
    public string[] PartyNames { get; } = new string[MaxPartySize];

    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;

        for (int i = 0; i < MaxPartySize; i++)
        {
            PartyLoadouts[i] = new InventorySystem(InventoryConstants.BattleCapacity);
            PartyNames[i] = $"Unit {i + 1}";
        }

        GD.Print("PlayerInventoryManager ready.");

        #if DEBUG
        CallDeferred(MethodName.SeedDebugItems);
        #endif
    }

    #if DEBUG
    private void SeedDebugItems()
    {
        // Runs one frame after _Ready, by which point all autoloads including
        // ItemCatalogSeeder have completed their own _Ready calls
        if (ItemCatalog.TryGet(1, out _))
            GlobalInventory.AddItem(1, 3); // 3x Potion
        else
            GD.PrintErr("[PlayerInventoryManager] DEBUG: ItemCatalog not ready, skipping seed.");

        if (ItemCatalog.TryGet(4, out _))
            GlobalInventory.AddItem(4, 2); // 2x Ether
            
        GD.Print("[PlayerInventoryManager] DEBUG: seeded test items.");
    }
    #endif

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    ///     Register a unit into a party slot. Called by GameManager at battle start.
    ///     Copies the persistent loadout into the unit's live inventory.
    /// </summary>
    public void RegisterUnit(int partySlot, IUnitSystem unit)
    {
        if (partySlot < 0 || partySlot >= MaxPartySize) return;
        PartyNames[partySlot] = unit.UnitName;
        unit.Inventory.CopyFrom(PartyLoadouts[partySlot]);
    }

    /// <summary>
    ///     Sync a unit's live inventory back into its persistent loadout.
    ///     Called by GameManager at battle end.
    /// </summary>
    public void SyncBack(int partySlot, IUnitSystem unit)
    {
        if (partySlot < 0 || partySlot >= MaxPartySize) return;
        PartyLoadouts[partySlot].CopyFrom(unit.Inventory);
    }
}
