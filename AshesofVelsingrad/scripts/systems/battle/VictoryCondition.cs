using System.Collections.Generic;
using System.Linq;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     Snapshot passed to victory conditions on each evaluation.
/// </summary>
/// <param name="PlayerUnits">All units in the player faction (alive or dead).</param>
/// <param name="Allies">All units in the ally faction.</param>
/// <param name="Enemies">All units in the enemy faction.</param>
/// <param name="Turn">The current global turn number.</param>
public readonly record struct BattleStateSnapshot(
    IReadOnlyList<UnitSystem> PlayerUnits,
    IReadOnlyList<UnitSystem> Allies,
    IReadOnlyList<UnitSystem> Enemies,
    int Turn
);

/// <summary>
///     Base class for victory conditions, defined as Godot Resources so designers
///     can compose them in the editor and assign them to a <see cref="BattleConfig" />.
/// </summary>
/// <remarks>
///     A battle ends as soon as <see cref="IsVictoryReached" /> or
///     <see cref="IsDefeatReached" /> returns <c>true</c> for the active condition.
///     The default condition is <see cref="DefeatAllEnemiesCondition" />, which is
///     the standard "wipe out the enemy team" rule and also handles party wipe as defeat.
/// </remarks>
[GlobalClass]
public partial class VictoryCondition : Resource
{
    /// <summary>
    ///     Human-readable label shown on the HUD ("Defeat all enemies", "Survive 5 turns", etc.).
    /// </summary>
    [Export]
    public string DisplayLabel { get; set; } = "Defeat all enemies";

    /// <summary>
    ///     Returns <c>true</c> when the player has fulfilled the victory criterion.
    /// </summary>
    /// <param name="state">Current battle snapshot.</param>
    /// <returns><c>true</c> if the condition is met.</returns>
    public virtual bool IsVictoryReached(in BattleStateSnapshot state)
    {
        return state.Enemies.All(u => !u.IsAlive);
    }

    /// <summary>
    ///     Returns <c>true</c> when the player has irrecoverably failed.
    /// </summary>
    /// <param name="state">Current battle snapshot.</param>
    /// <returns><c>true</c> if defeat is reached.</returns>
    public virtual bool IsDefeatReached(in BattleStateSnapshot state)
    {
        return state.PlayerUnits.All(u => !u.IsAlive);
    }
}

/// <summary>
///     Default condition: victory when every enemy is dead, defeat when every player unit is dead.
/// </summary>
[GlobalClass]
public sealed partial class DefeatAllEnemiesCondition : VictoryCondition
{
    /// <summary>
    ///     Initializes a new "defeat all enemies" condition.
    /// </summary>
    public DefeatAllEnemiesCondition()
    {
        DisplayLabel = "Defeat all enemies";
    }
}

/// <summary>
///     Survive a number of turns, then victory is awarded regardless of enemy state.
/// </summary>
[GlobalClass]
public sealed partial class SurviveTurnsCondition : VictoryCondition
{
    /// <summary>
    ///     Number of turns the player must endure.
    /// </summary>
    [Export]
    public int TurnsToSurvive { get; set; } = 5;

    /// <summary>
    ///     Initializes a new survive condition.
    /// </summary>
    public SurviveTurnsCondition()
    {
        DisplayLabel = "Survive 5 turns";
    }

    /// <inheritdoc />
    public override bool IsVictoryReached(in BattleStateSnapshot state)
    {
        return state.Turn >= TurnsToSurvive;
    }
}

/// <summary>
///     Victory once a specific designated enemy ("boss") is defeated.
///     Other enemies may still be alive.
/// </summary>
[GlobalClass]
public sealed partial class DefeatBossCondition : VictoryCondition
{
    /// <summary>
    ///     Identifier (UnitName) of the boss unit. The first enemy with this name
    ///     is treated as the target.
    /// </summary>
    [Export]
    public string BossUnitName { get; set; } = string.Empty;

    /// <summary>
    ///     Initializes a new boss-defeat condition.
    /// </summary>
    public DefeatBossCondition()
    {
        DisplayLabel = "Defeat the boss";
    }

    /// <inheritdoc />
    public override bool IsVictoryReached(in BattleStateSnapshot state)
    {
        UnitSystem? boss = state.Enemies.FirstOrDefault(u => u.UnitName == BossUnitName);
        return boss is not null && !boss.IsAlive;
    }
}
