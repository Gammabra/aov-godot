using Godot;

namespace AshesOfVelsingrad.systems.items;

/// <summary>
///     Targeting pattern for an item. Mirrors <c>TargetTypes</c> but adds a
///     "self only" option (most healing potions).
/// </summary>
public enum ItemTargetType
{
    /// <summary>Used on the unit consuming the item.</summary>
    Self,

    /// <summary>Used on a single ally (party or guest).</summary>
    SingleAlly,

    /// <summary>Used on a single enemy (offensive items like bombs).</summary>
    SingleEnemy,

    /// <summary>Used on every ally on the field.</summary>
    AllAllies
}

/// <summary>
///     Authored data for a usable item.
/// </summary>
/// <remarks>
///     <para>
///         As with skills, items are split into a data half (<see cref="ItemDefinition" />)
///         and a behaviour half (<see cref="IItemBehaviour" /> resolved through
///         <see cref="ItemRegistry" /> by <see cref="BehaviourId" />).
///     </para>
///     <para>
///         Per the user's design, items are consumed from the <see cref="PartyInventory" />
///         (a single shared bag) and using one consumes the unit's main action — same as
///         casting a skill.
///     </para>
/// </remarks>
[GlobalClass]
public sealed partial class ItemDefinition : Resource
{
    /// <summary>Stable identifier (e.g. "healing_potion", "purifying_elixir").</summary>
    [Export] public string ItemId { get; set; } = string.Empty;

    /// <summary>Display name shown in inventory and tooltips.</summary>
    [Export] public string DisplayName { get; set; } = string.Empty;

    /// <summary>Long-form description for tooltips.</summary>
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Behaviour id to resolve in <see cref="ItemRegistry" />.</summary>
    [Export] public string BehaviourId { get; set; } = string.Empty;

    /// <summary>Targeting pattern.</summary>
    [Export] public ItemTargetType TargetType { get; set; } = ItemTargetType.Self;

    /// <summary>Numerical magnitude — interpretation depends on the behaviour (HP healed, damage, etc.).</summary>
    [Export] public float Magnitude { get; set; }

    /// <summary>Optional icon for HUD.</summary>
    [Export] public Texture2D? Icon { get; set; }

    /// <summary>Vendor sale price in gold.</summary>
    [Export] public int Price { get; set; } = 10;

    /// <summary>
    ///     If true, the item is consumed on use. Set to false for reusable utility items
    ///     (e.g. lockpicks); the combat layer ignores those.
    /// </summary>
    [Export] public bool Consumable { get; set; } = true;
}
