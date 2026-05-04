using System.Collections.Generic;
using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     Strongly-typed event payloads emitted by <see cref="BattleEventBus" />.
/// </summary>
/// <remarks>
///     Each record is a tiny immutable DTO — the bus passes them to subscribers
///     so that HUD widgets, audio, analytics, save-state recorders, etc. can
///     react without ever taking a direct reference on the systems that produce
///     the data. This keeps combat logic and presentation properly decoupled.
/// </remarks>
public static class BattleEvents
{
    /// <summary>
    ///     Emitted once when a battle begins, after units and HUD are ready.
    /// </summary>
    /// <param name="PlayerUnits">Units in the player's party.</param>
    /// <param name="Allies">AI-controlled friendly units.</param>
    /// <param name="Enemies">AI-controlled hostile units.</param>
    public sealed record BattleStarted(
        IReadOnlyList<UnitSystem> PlayerUnits,
        IReadOnlyList<UnitSystem> Allies,
        IReadOnlyList<UnitSystem> Enemies
    );

    /// <summary>
    ///     Emitted when the battle ends (victory, defeat, retreat).
    /// </summary>
    /// <param name="Result">Outcome describing winner and rewards.</param>
    public sealed record BattleEnded(BattleResult Result);

    /// <summary>
    ///     Emitted when a new unit's turn begins.
    /// </summary>
    /// <param name="Unit">The unit whose turn just started.</param>
    /// <param name="TurnNumber">Global turn counter (1-based).</param>
    /// <param name="Faction">The faction of the active unit.</param>
    public sealed record TurnStarted(UnitSystem Unit, int TurnNumber, Faction Faction);

    /// <summary>
    ///     Emitted when the active unit finishes its turn.
    /// </summary>
    /// <param name="Unit">The unit whose turn just ended.</param>
    public sealed record TurnEnded(UnitSystem Unit);

    /// <summary>
    ///     Emitted when the recomputed turn order changes
    ///     (initial sort, speed change, unit death, etc.). HUD turn-queue uses this.
    /// </summary>
    /// <param name="UpcomingUnits">
    ///     The next units to act, in order. The first entry is the unit currently acting.
    /// </param>
    public sealed record TurnOrderChanged(IReadOnlyList<UnitSystem> UpcomingUnits);

    /// <summary>
    ///     Emitted whenever a unit's HP changes.
    /// </summary>
    /// <param name="Unit">The unit that received the change.</param>
    /// <param name="Delta">Signed change (negative = damage, positive = heal).</param>
    /// <param name="CurrentHp">HP after the change.</param>
    /// <param name="MaxHp">Unit's maximum HP.</param>
    public sealed record HpChanged(UnitSystem Unit, float Delta, float CurrentHp, float MaxHp);

    /// <summary>
    ///     Emitted whenever a unit's mana changes.
    /// </summary>
    /// <param name="Unit">The unit affected.</param>
    /// <param name="Delta">Signed change in mana.</param>
    /// <param name="CurrentMana">Mana after the change.</param>
    public sealed record ManaChanged(UnitSystem Unit, float Delta, float CurrentMana);

    /// <summary>
    ///     Emitted when a unit dies (HP reaches zero).
    /// </summary>
    /// <param name="Unit">The unit that died.</param>
    public sealed record UnitDied(UnitSystem Unit);

    /// <summary>
    ///     Emitted when a unit casts a skill.
    /// </summary>
    /// <param name="Caster">The unit casting.</param>
    /// <param name="SkillId">The string id of the skill (matches <c>SkillDefinition.SkillId</c>).</param>
    /// <param name="Targets">Targets affected.</param>
    public sealed record SkillUsed(UnitSystem Caster, string SkillId, IReadOnlyList<UnitSystem> Targets);

    /// <summary>
    ///     Emitted when a unit consumes an item from the shared inventory.
    /// </summary>
    /// <param name="User">The unit consuming the item.</param>
    /// <param name="ItemId">The string id of the item.</param>
    /// <param name="Targets">Units affected by the item.</param>
    public sealed record ItemUsed(UnitSystem User, string ItemId, IReadOnlyList<UnitSystem> Targets);

    /// <summary>
    ///     Emitted when a status effect is applied to a unit.
    /// </summary>
    /// <param name="Target">The unit gaining the effect.</param>
    /// <param name="EffectName">Human-readable name (used by HUD/log).</param>
    /// <param name="Duration">Number of turns the effect will last (-1 = permanent).</param>
    public sealed record StatusEffectApplied(UnitSystem Target, string EffectName, int Duration);

    /// <summary>
    ///     Emitted when a status effect ends or is cleansed.
    /// </summary>
    /// <param name="Target">The unit losing the effect.</param>
    /// <param name="EffectName">Effect name.</param>
    public sealed record StatusEffectRemoved(UnitSystem Target, string EffectName);

    /// <summary>
    ///     Emitted when a unit's corruption level changes.
    /// </summary>
    /// <param name="Unit">The corrupted unit.</param>
    /// <param name="OldLevel">Previous corruption level (0..3).</param>
    /// <param name="NewLevel">New corruption level (0..3).</param>
    public sealed record CorruptionChanged(UnitSystem Unit, int OldLevel, int NewLevel);

    /// <summary>
    ///     Emitted when global karma changes (mostly outside combat,
    ///     but the HUD may want to display it in case dialogue happens mid-fight).
    /// </summary>
    /// <param name="OldValue">Karma before the change, in [-100, +100].</param>
    /// <param name="NewValue">Karma after the change, in [-100, +100].</param>
    /// <param name="Reason">Optional human-readable reason (e.g. "spared the captive").</param>
    public sealed record KarmaChanged(int OldValue, int NewValue, string Reason);

    /// <summary>
    ///     Emitted when a unit switches faction
    ///     (e.g. a corruption spell turning an ally into an enemy for 3 turns).
    /// </summary>
    /// <param name="Unit">The unit changing sides.</param>
    /// <param name="OldFaction">Previous faction.</param>
    /// <param name="NewFaction">New faction.</param>
    /// <param name="DurationTurns">Turns until the change reverts (-1 = permanent).</param>
    public sealed record FactionChanged(UnitSystem Unit, Faction OldFaction, Faction NewFaction, int DurationTurns);

    /// <summary>
    ///     Free-form log line for the on-screen battle log.
    /// </summary>
    /// <param name="Message">Localized text to append.</param>
    /// <param name="Severity">Log severity for colour-coding.</param>
    public sealed record LogMessage(string Message, LogSeverity Severity = LogSeverity.Info);
}

/// <summary>
///     Severity of a battle log entry, used by the HUD to colourize lines.
/// </summary>
public enum LogSeverity
{
    /// <summary>Neutral message (a unit moved, a turn started, etc.).</summary>
    Info,

    /// <summary>Positive event (heal, buff applied, victory).</summary>
    Positive,

    /// <summary>Negative event (damage, debuff, status applied).</summary>
    Negative,

    /// <summary>Critical event (death, level-3 corruption, defeat).</summary>
    Critical
}
