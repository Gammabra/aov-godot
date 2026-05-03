using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.progression;
using AshesOfVelsingrad.systems.status_effects;
using Godot;

namespace AshesOfVelsingrad.systems.corruption;

/// <summary>
///     Static facade that orchestrates corruption mechanics: backlash rolls, level changes,
///     transformation, and faction conversion.
/// </summary>
/// <remarks>
///     <para>
///         This is the only place that mutates <see cref="CharacterProfile.CorruptionLevel" />
///         and <see cref="CharacterProfile.CorruptionPoints" />. Other systems should call
///         <see cref="RollBacklash" /> when a corruption-source skill is cast and
///         <see cref="Cleanse" /> when an item / Light spell mitigates corruption.
///     </para>
///     <para>
///         Karma modulation: per the design, negative karma (more corrupt-leaning)
///         <b>reduces</b> backlash chance — the character has embraced the dark arts.
///         Positive karma (virtuous) <b>increases</b> backlash. The factor is a linear
///         shift of <c>±0.5</c> across the karma range, applied on top of the spell's
///         configured base chance.
///     </para>
/// </remarks>
public static class CorruptionSystem
{
    /// <summary>
    ///     Maximum karma-driven shift applied to a backlash roll.
    ///     A unit at +100 karma rolls 0.5 higher than its base chance; -100 rolls 0.5 lower.
    /// </summary>
    public const float MaxKarmaShift = 0.5f;

    #region Public API

    /// <summary>
    ///     Roll a corruption backlash for a dark-magic cast.
    /// </summary>
    /// <param name="caster">The unit that just cast the spell.</param>
    /// <param name="baseChance">Base probability in <c>[0, 1]</c> from the skill definition.</param>
    /// <returns><c>true</c> if a backlash occurred and corruption was added.</returns>
    public static bool RollBacklash(UnitSystem caster, float baseChance)
    {
        CharacterProfile? profile = caster.Profile;
        if (profile is null)
            return false;

        float adjusted = AdjustChanceWithKarma(baseChance, profile.Karma);
        if (adjusted <= 0f)
            return false;

        if (GD.Randf() > adjusted)
            return false;

        AddCorruptionPoint(caster, profile);
        return true;
    }

    /// <summary>
    ///     Reduce corruption by one full level on the given unit (clamped at 0).
    /// </summary>
    /// <param name="unit">The unit being purified.</param>
    /// <remarks>
    ///     Used by the Purifying Elixir item and the Light spell <c>Éclat Purificateur</c>
    ///     (when configured with the <c>cleanse_corruption</c> flag).
    /// </remarks>
    public static void Cleanse(UnitSystem unit)
    {
        CharacterProfile? profile = unit.Profile;
        if (profile is null) return;

        int oldLevel = profile.CorruptionLevel;
        if (oldLevel <= 0)
            return;

        profile.CorruptionLevel = oldLevel - 1;
        profile.CorruptionPoints = 0;

        SyncRuntimeEffects(unit, oldLevel, profile.CorruptionLevel);
        BattleEventBus.Instance?.Publish(new BattleEvents.CorruptionChanged(
            unit, oldLevel, profile.CorruptionLevel
        ));
    }

    /// <summary>
    ///     Set the corruption level directly. Used by save/load and tests.
    /// </summary>
    /// <param name="unit">The unit to mutate.</param>
    /// <param name="level">Target level in <c>[0, MaxCorruptionLevel]</c>.</param>
    public static void SetLevel(UnitSystem unit, int level)
    {
        CharacterProfile? profile = unit.Profile;
        if (profile is null) return;
        int clamped = Mathf.Clamp(level, 0, CharacterProfile.MaxCorruptionLevel);
        int old = profile.CorruptionLevel;
        if (old == clamped) return;

        profile.CorruptionLevel = clamped;
        profile.CorruptionPoints = 0;

        SyncRuntimeEffects(unit, old, clamped);
        BattleEventBus.Instance?.Publish(new BattleEvents.CorruptionChanged(unit, old, clamped));
    }

    /// <summary>
    ///     Apply a temporary faction conversion to a unit.
    /// </summary>
    /// <param name="target">Unit to convert.</param>
    /// <param name="newFaction">Faction the unit becomes (typically <see cref="Faction.Enemy" />).</param>
    /// <param name="duration">Number of turns the conversion lasts.</param>
    /// <remarks>
    ///     Implements the user-requested "corrupted ally" mechanic. The conversion is
    ///     attached as a <see cref="CorruptedTransformationEffect" /> so the
    ///     <see cref="StatusEffectSystem" /> automatically reverts it when its duration
    ///     expires. Adds 1 corruption level to the affected unit on top of the conversion
    ///     so the spell has a lasting consequence.
    /// </remarks>
    public static void ApplyTemporaryConversion(UnitSystem target, Faction newFaction, int duration)
    {
        Faction original = target.Faction;
        if (original == newFaction)
            return;

        CorruptedTransformationEffect effect = new(original, duration);
        target.ApplyEffect(effect);

        target.AssignFaction(newFaction);
        BattleEventBus.Instance?.Publish(new BattleEvents.FactionChanged(target, original, newFaction, duration));

        // Lasting cost: target gains a corruption point.
        if (target.Profile is { } profile)
            AddCorruptionPoint(target, profile);
    }

    /// <summary>
    ///     Internal: called by <see cref="CorruptedTransformationEffect.OnApply" />.
    /// </summary>
    /// <param name="unit">The unit just transformed.</param>
    public static void OnTransformationStart(UnitSystem unit)
    {
        BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
            $"{unit.UnitName} succumbs to corruption!", LogSeverity.Critical
        ));
    }

    /// <summary>
    ///     Internal: called by <see cref="CorruptedTransformationEffect.OnRemove" />.
    /// </summary>
    /// <param name="unit">The unit returning to normal.</param>
    /// <param name="originalFaction">The faction to restore.</param>
    public static void OnTransformationEnd(UnitSystem unit, Faction originalFaction)
    {
        Faction current = unit.Faction;
        unit.AssignFaction(originalFaction);
        BattleEventBus.Instance?.Publish(new BattleEvents.FactionChanged(unit, current, originalFaction, 0));
        BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
            $"{unit.UnitName} regains their senses.", LogSeverity.Positive
        ));
    }

    #endregion

    #region Private helpers

    /// <summary>
    ///     Apply karma to a base backlash chance.
    /// </summary>
    /// <param name="baseChance">Spell-defined base chance.</param>
    /// <param name="karma">Caster's karma in <c>[-100, +100]</c>.</param>
    /// <returns>Adjusted chance, clamped to <c>[0, 1]</c>.</returns>
    private static float AdjustChanceWithKarma(float baseChance, int karma)
    {
        // Linear mapping: karma = +100 -> +MaxKarmaShift; karma = -100 -> -MaxKarmaShift.
        float shift = karma / 100.0f * MaxKarmaShift;
        return Mathf.Clamp(baseChance + shift, 0f, 1f);
    }

    /// <summary>
    ///     Increase the unit's corruption point counter, advancing its level when full.
    /// </summary>
    /// <param name="unit">Affected unit.</param>
    /// <param name="profile">Cached profile reference.</param>
    private static void AddCorruptionPoint(UnitSystem unit, CharacterProfile profile)
    {
        if (profile.CorruptionLevel >= CharacterProfile.MaxCorruptionLevel)
        {
            // Already at max: trigger the transformation if not already berserk.
            if (!unit.HasEffect<CorruptedTransformationEffect>())
            {
                CorruptedTransformationEffect effect = new(unit.Faction, 2);
                unit.ApplyEffect(effect);
                Faction original = unit.Faction;
                unit.AssignFaction(Faction.Enemy);
                BattleEventBus.Instance?.Publish(new BattleEvents.FactionChanged(unit, original, Faction.Enemy, 2));
            }
            return;
        }

        profile.CorruptionPoints++;
        if (profile.CorruptionPoints < CharacterProfile.CorruptionPointsPerLevel)
        {
            // Same level, just more points.
            BattleEventBus.Instance?.Publish(new BattleEvents.CorruptionChanged(
                unit, profile.CorruptionLevel, profile.CorruptionLevel
            ));
            return;
        }

        // Level up corruption.
        int oldLevel = profile.CorruptionLevel;
        profile.CorruptionLevel = oldLevel + 1;
        profile.CorruptionPoints = 0;

        SyncRuntimeEffects(unit, oldLevel, profile.CorruptionLevel);

        BattleEventBus.Instance?.Publish(new BattleEvents.CorruptionChanged(unit, oldLevel, profile.CorruptionLevel));
        BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
            $"{unit.UnitName}'s corruption rises to level {profile.CorruptionLevel}.",
            profile.CorruptionLevel >= CharacterProfile.MaxCorruptionLevel ? LogSeverity.Critical : LogSeverity.Negative
        ));

        if (profile.CorruptionLevel >= CharacterProfile.MaxCorruptionLevel)
        {
            // Tipping over to level 3 immediately triggers the transformation.
            CorruptedTransformationEffect effect = new(unit.Faction, 2);
            unit.ApplyEffect(effect);
            Faction original = unit.Faction;
            unit.AssignFaction(Faction.Enemy);
            BattleEventBus.Instance?.Publish(new BattleEvents.FactionChanged(unit, original, Faction.Enemy, 2));
        }

        // KarmaManager no longer required here; previously this forced initialization.
    }

    /// <summary>
    ///     Replace the runtime "level" status effect on the unit so the level matches the profile.
    /// </summary>
    /// <param name="unit">The affected unit.</param>
    /// <param name="oldLevel">Previous level.</param>
    /// <param name="newLevel">New level.</param>
    private static void SyncRuntimeEffects(UnitSystem unit, int oldLevel, int newLevel)
    {
        // Remove the old level marker.
        foreach (StatusEffect existing in unit.GetActiveEffects().ToArray())
        {
            if (existing is CorruptionLevelEffect)
                unit.RemoveEffect(existing);
        }

        // Add the new one (if any).
        StatusEffect? marker = newLevel switch
        {
            1 => new CorruptionLevel1Effect(),
            2 => new CorruptionLevel2Effect(),
            3 => new CorruptionLevel3Effect(),
            _ => null
        };
        if (marker is not null)
            unit.ApplyEffect(marker);
    }

    #endregion
}
