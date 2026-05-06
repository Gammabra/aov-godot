using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.systems.battle;
using Godot;

namespace AshesOfVelsingrad.systems.items;

/// <summary>
///     Shared bag of items available to the player party during combat and exploration.
/// </summary>
/// <remarks>
///     <para>
///         The inventory is a dictionary of <c>itemId → count</c>. Adding a stackable
///         item increments the count; consuming decrements and removes it when zero.
///     </para>
///     <para>
///         The class is a Godot <see cref="Node" /> so it can be registered as an AutoLoad
///         singleton (one inventory per save). Use <see cref="Instance" /> to access it
///         from any combat or HUD code.
///     </para>
///     <para>
///         All write operations emit <see cref="InventoryChanged" /> for the HUD to react.
///     </para>
/// </remarks>
public sealed partial class PartyInventory : Node
{
    #region Singleton

    /// <summary>Active singleton instance.</summary>
    public static PartyInventory? Instance { get; private set; }

    #endregion

    #region Events

    /// <summary>
    ///     Fired whenever the contents change (add, remove, use, clear).
    /// </summary>
    public event Action? InventoryChanged;

    #endregion

    #region State

    private readonly Dictionary<string, int> _items = new();

    /// <summary>Read-only view of the current contents.</summary>
    public IReadOnlyDictionary<string, int> Items => _items;

    #endregion

    #region Godot Lifecycle

    /// <inheritdoc />
    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {nameof(PartyInventory)} detected. Removing duplicate.");
            QueueFree();
            return;
        }
        Instance = this;
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region Public API

    /// <summary>
    ///     Add <paramref name="count" /> copies of <paramref name="itemId" /> to the bag.
    /// </summary>
    /// <param name="itemId">The item id (must be registered).</param>
    /// <param name="count">Number of copies to add (must be &gt; 0).</param>
    public void Add(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0)
            return;
        _items[itemId] = _items.GetValueOrDefault(itemId, 0) + count;
        InventoryChanged?.Invoke();
    }

    /// <summary>
    ///     Remove up to <paramref name="count" /> copies of <paramref name="itemId" /> from the bag.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="count">Number of copies to remove.</param>
    /// <returns>Actual number removed (0 if absent).</returns>
    public int Remove(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0)
            return 0;
        if (!_items.TryGetValue(itemId, out int existing))
            return 0;

        int removed = Math.Min(existing, count);
        int remaining = existing - removed;
        if (remaining <= 0)
            _items.Remove(itemId);
        else
            _items[itemId] = remaining;

        InventoryChanged?.Invoke();
        return removed;
    }

    /// <summary>How many of <paramref name="itemId" /> are currently held.</summary>
    /// <param name="itemId">The item id.</param>
    /// <returns>Count, or 0 if absent.</returns>
    public int CountOf(string itemId)
    {
        return _items.GetValueOrDefault(itemId, 0);
    }

    /// <summary>
    ///     Use an item from the bag.
    /// </summary>
    /// <param name="user">The unit using the item.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="targets">The targets the item should affect.</param>
    /// <returns><c>true</c> if the use succeeded; <c>false</c> if the item is missing or invalid.</returns>
    /// <remarks>
    ///     Decrements the item count, applies the behaviour, and publishes
    ///     <see cref="BattleEvents.ItemUsed" /> on success.
    /// </remarks>
    public bool Use(UnitSystem user, string itemId, IReadOnlyList<UnitSystem> targets)
    {
        ItemRegistry? registry = ItemRegistry.Instance;
        if (registry is null)
        {
            GD.PrintErr("PartyInventory.Use: ItemRegistry not initialised.");
            return false;
        }

        ItemDefinition? def = registry.GetDefinition(itemId);
        if (def is null)
            return false;

        if (CountOf(itemId) <= 0)
        {
            GD.PrintErr($"PartyInventory.Use: '{itemId}' is not in the bag.");
            return false;
        }

        IItemBehaviour? behaviour = registry.GetBehaviour(def.BehaviourId);
        if (behaviour is null)
            return false;

        ItemUseContext ctx = new(user, targets, def);
        behaviour.Apply(in ctx);

        if (def.Consumable)
            Remove(itemId);

        BattleEventBus.Instance?.Publish(new BattleEvents.ItemUsed(user, itemId, targets));
        return true;
    }

    /// <summary>Empty the bag. Useful for new-game flows or test setup.</summary>
    public void Clear()
    {
        _items.Clear();
        InventoryChanged?.Invoke();
    }

    #endregion
}
