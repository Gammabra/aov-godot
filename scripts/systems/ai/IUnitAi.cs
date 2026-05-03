using System.Collections.Generic;
using System.Threading.Tasks;

namespace AshesOfVelsingrad.systems.ai;

/// <summary>
///     Strategy interface for AI-controlled units.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="TurnManager" /> calls <see cref="TakeTurnAsync" /> when the active
///         unit's faction is non-Player (i.e. <see cref="battle.Faction.Ally" /> or
///         <see cref="battle.Faction.Enemy" />). The AI picks a target, optionally moves,
///         then either casts a skill or attacks. The method should resolve only after the
///         action has completed so the turn loop can move on.
///     </para>
///     <para>
///         Implementations should be small and stateless — share one instance per faction
///         via <see cref="AiRegistry" />.
///     </para>
/// </remarks>
public interface IUnitAi
{
    /// <summary>
    ///     Decide and perform the unit's action for this turn.
    /// </summary>
    /// <param name="self">The unit whose turn it is.</param>
    /// <param name="allies">Units friendly to <paramref name="self" /> (excluding self).</param>
    /// <param name="enemies">Units hostile to <paramref name="self" />.</param>
    /// <param name="map">Active map.</param>
    /// <returns>A task that completes when the AI is done.</returns>
    Task TakeTurnAsync(
        UnitSystem self,
        IReadOnlyList<UnitSystem> allies,
        IReadOnlyList<UnitSystem> enemies,
        MapSystem? map
    );
}
