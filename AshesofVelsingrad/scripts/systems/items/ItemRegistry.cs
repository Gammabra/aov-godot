using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.systems.items;

/// <summary>
///     Singleton lookup for item definitions and behaviours.
/// </summary>
/// <remarks>
///     Mirrors <see cref="systems.skills.SkillRegistry" />. Populated at boot by the
///     <c>DefaultDatabaseSeeder</c> and by any <c>.tres</c> resources discovered under
///     <c>res://data/items/</c>.
/// </remarks>
public sealed partial class ItemRegistry : Node
{
    private readonly Dictionary<string, ItemDefinition> _definitions = new();
    private readonly Dictionary<string, IItemBehaviour> _behaviours = new();

    /// <summary>Active singleton instance.</summary>
    public static ItemRegistry? Instance { get; private set; }

    /// <inheritdoc />
    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {nameof(ItemRegistry)} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Instance = this;
    }

    /// <summary>Register an item definition. <c>ItemId</c> is the key.</summary>
    /// <param name="definition">Definition to register.</param>
    public void RegisterDefinition(ItemDefinition definition)
    {
        if (string.IsNullOrEmpty(definition.ItemId))
        {
            GD.PrintErr("ItemRegistry: cannot register an ItemDefinition with empty ItemId.");
            return;
        }
        _definitions[definition.ItemId] = definition;
    }

    /// <summary>Register a behaviour under a string id.</summary>
    /// <param name="behaviourId">The behaviour id (matches <see cref="ItemDefinition.BehaviourId" />).</param>
    /// <param name="behaviour">The implementation.</param>
    public void RegisterBehaviour(string behaviourId, IItemBehaviour behaviour)
    {
        if (string.IsNullOrEmpty(behaviourId))
        {
            GD.PrintErr("ItemRegistry: cannot register a behaviour with empty id.");
            return;
        }
        _behaviours[behaviourId] = behaviour;
    }

    /// <summary>Look up an item definition. Returns null with a log on miss.</summary>
    /// <param name="itemId">The item id.</param>
    /// <returns>Definition or null.</returns>
    public ItemDefinition? GetDefinition(string itemId)
    {
        if (_definitions.TryGetValue(itemId, out ItemDefinition? def))
            return def;
        GD.PrintErr($"ItemRegistry: unknown item id '{itemId}'.");
        return null;
    }

    /// <summary>Look up a behaviour. Returns null with a log on miss.</summary>
    /// <param name="behaviourId">The behaviour id.</param>
    /// <returns>Behaviour or null.</returns>
    public IItemBehaviour? GetBehaviour(string behaviourId)
    {
        if (_behaviours.TryGetValue(behaviourId, out IItemBehaviour? b))
            return b;
        GD.PrintErr($"ItemRegistry: unknown behaviour id '{behaviourId}'.");
        return null;
    }

    /// <summary>All registered definitions, keyed by item id.</summary>
    public IReadOnlyDictionary<string, ItemDefinition> AllDefinitions => _definitions;
}
