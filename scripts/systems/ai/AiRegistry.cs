using AshesOfVelsingrad.systems.battle;

namespace AshesOfVelsingrad.systems.ai;

/// <summary>
///     Static lookup of AI strategies by faction.
/// </summary>
/// <remarks>
///     The <see cref="TurnManager" /> calls <see cref="ResolveFor" /> when it needs
///     to run an AI turn for a non-player unit. By default, allies use
///     <see cref="AlliedAi" /> and enemies use <see cref="BasicEnemyAi" />.
///     Override per-unit by setting <c>UnitSystem.OverrideAi</c>.
/// </remarks>
public static class AiRegistry
{
    private static readonly IUnitAi AllyAi = new AlliedAi();
    private static readonly IUnitAi EnemyAi = new BasicEnemyAi();

    /// <summary>
    ///     Resolve the AI implementation for the given unit.
    /// </summary>
    /// <param name="unit">Unit needing an AI strategy.</param>
    /// <returns>The strategy for the unit's faction. Player units never get an AI.</returns>
    public static IUnitAi? ResolveFor(UnitSystem unit)
    {
        if (unit.OverrideAi is not null)
            return unit.OverrideAi;

        return unit.Faction switch
        {
            Faction.Ally => AllyAi,
            Faction.Enemy => EnemyAi,
            _ => null
        };
    }
}
