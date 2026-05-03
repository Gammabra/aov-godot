using System.Collections.Generic;

namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     The outcome of a battle, returned by <see cref="BattleLauncher" />
///     and embedded in the <see cref="BattleEvents.BattleEnded" /> event.
/// </summary>
public enum BattleOutcome
{
    /// <summary>The player party survived and met the victory condition.</summary>
    Victory,

    /// <summary>The player party was wiped out or failed the victory condition.</summary>
    Defeat,

    /// <summary>The player retreated voluntarily.</summary>
    Retreat,

    /// <summary>The battle ended for narrative reasons (cutscene, scripted event).</summary>
    Aborted
}

/// <summary>
///     Snapshot of a finished battle. Carries the data exploration code needs
///     to award XP, drop loot, and trigger follow-up dialogue.
/// </summary>
public sealed class BattleResult
{
    /// <summary>
    ///     Why the battle ended.
    /// </summary>
    public BattleOutcome Outcome { get; init; } = BattleOutcome.Victory;

    /// <summary>
    ///     Total XP to distribute to surviving player units.
    /// </summary>
    public int ExperienceGained { get; init; }

    /// <summary>
    ///     Item ids dropped from defeated enemies.
    /// </summary>
    public IReadOnlyList<string> LootIds { get; init; } = [];

    /// <summary>
    ///     Number of full rounds elapsed (used for achievements / pacing metrics).
    /// </summary>
    public int TurnsElapsed { get; init; }

    /// <summary>
    ///     Convenience: <c>true</c> for <see cref="BattleOutcome.Victory" />.
    /// </summary>
    public bool IsVictory => Outcome == BattleOutcome.Victory;
}
