using AshesOfVelsingrad.Systems;
using System.Collections.Generic;

namespace AshesOfVelsingrad.AI;

/// <summary>
/// Read-only snapshot of battle state for AI decision making.
/// Does not reference GameManager - actions are executed by the caller.
/// </summary>
public class BattleState
{
    /// <summary>The unit currently making decisions.</summary>
    public required IUnitSystem ActingUnit { get; init; }
    /// <summary>Reference to the map system for position queries.</summary>
    public required IMapSystem MapSystem { get; init; }
    /// <summary>All alive player units.</summary>
    public required List<IUnitSystem> PlayerUnits { get; init; }
    /// <summary>All alive enemy units.</summary>
    public required List<IUnitSystem> EnemyUnits { get; init; }
}