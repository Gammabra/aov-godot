using System.Collections.Generic;
using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.systems.items;

/// <summary>
///     Context passed to an item behaviour at use time.
/// </summary>
/// <param name="User">The unit consuming the item.</param>
/// <param name="Targets">The resolved target list.</param>
/// <param name="Definition">Authored item data.</param>
public readonly record struct ItemUseContext(
    UnitSystem User,
    IReadOnlyList<UnitSystem> Targets,
    ItemDefinition Definition
);

/// <summary>
///     Strategy interface for item behaviours.
/// </summary>
/// <remarks>
///     Same pattern as <see cref="systems.skills.ISkillBehaviour" />: one behaviour can
///     power many items. For example, a single <c>HealingItemBehaviour</c> serves both
///     "Healing Potion" (Magnitude 30) and "Greater Healing Potion" (Magnitude 60).
/// </remarks>
public interface IItemBehaviour
{
    /// <summary>
    ///     Apply the item's effect to the targets.
    /// </summary>
    /// <param name="context">Use-time context.</param>
    void Apply(in ItemUseContext context);
}
