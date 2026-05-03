using System.Collections.Generic;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Systems.Battle;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.Data.Corruption;

/// <summary>
///     Static facade owning every corruption mechanic: backlash rolls, level changes,
///     transformation, faction conversion.
/// </summary>
public static class CorruptionSystem
{
    /// <summary>Maximum karma-driven shift to a backlash chance roll.</summary>
    public const float MaxKarmaShift = 0.5f;

    /// <summary>Roll a corruption backlash for a dark-magic cast.</summary>
    /// <param name="caster">Unit that just cast the spell.</param>
    /// <param name="baseChance">Base probability in [0, 1] from the skill definition.</param>
    /// <returns><c>true</c> when a backlash occurred and corruption was added.</returns>
    public static bool RollBacklash(IUnitSystem caster, float baseChance)
    {
        int karma = KarmaManager.Instance?.Karma ?? 0;
        float adjusted = AdjustChanceWithKarma(baseChance, karma);
        if (adjusted <= 0f) return false;
        if (GD.Randf() > adjusted) return false;

        AddCorruptionPoint(caster);
        BattleNotifications.Post(
            $"{caster.UnitName} suffers a corruption backlash.",
            BattleNotifications.Severity.Negative);
        return true;
    }

    /// <summary>Reduce corruption by one tier on the unit (clamped at 0).</summary>
    /// <param name="unit">Unit being purified.</param>
    public static void Cleanse(IUnitSystem unit)
    {
        if (unit is not UnitSystem u) return;
        int oldLevel = u.CorruptionLevel;
        if (oldLevel <= 0) return;

        u.CorruptionLevel = oldLevel - 1;
        u.CorruptionPoints = 0;

        SyncRuntimeMarker(unit, oldLevel, u.CorruptionLevel);
        BattleNotifications.Post(
            $"{u.UnitName} is purified — corruption now level {u.CorruptionLevel}.",
            BattleNotifications.Severity.Positive);
    }

    /// <summary>Set the corruption level directly. Used by save/load and tests.</summary>
    /// <param name="unit">Unit to mutate.</param>
    /// <param name="level">Target level in [0, MaxCorruptionLevel].</param>
    public static void SetLevel(IUnitSystem unit, int level)
    {
        if (unit is not UnitSystem u) return;
        int clamped = Mathf.Clamp(level, 0, UnitSystem.MaxCorruptionLevel);
        int old = u.CorruptionLevel;
        if (old == clamped) return;

        u.CorruptionLevel = clamped;
        u.CorruptionPoints = 0;
        SyncRuntimeMarker(unit, old, clamped);
    }

    /// <summary>Apply a temporary faction conversion (for the "Conversion Corrompue" spell).</summary>
    /// <param name="target">Unit to convert.</param>
    /// <param name="newFaction">Faction the unit becomes.</param>
    /// <param name="duration">Number of turns the conversion lasts.</param>
    public static void ApplyTemporaryConversion(IUnitSystem target, Faction newFaction, int duration)
    {
        if (target is not UnitSystem u) return;
        Faction original = u.Faction;
        if (original == newFaction) return;

        CorruptedTransformationEffect effect = new(original, duration);
        target.SetStatusEffectOnUnit(effect);

        u.SetFaction(newFaction);
        BattleNotifications.Post(
            $"{u.UnitName} is twisted to {newFaction} for {duration} turns!",
            BattleNotifications.Severity.Critical);

        AddCorruptionPoint(target);
    }

    /// <summary>Internal: called by <see cref="CorruptedTransformationEffect.OnApply" />.</summary>
    public static void OnTransformationStart(IUnitSystem unit)
    {
        BattleNotifications.Post(
            $"{unit.UnitName} succumbs to corruption!",
            BattleNotifications.Severity.Critical);
    }

    /// <summary>Internal: called by <see cref="CorruptedTransformationEffect.OnRemove" />.</summary>
    public static void OnTransformationEnd(IUnitSystem unit, Faction originalFaction)
    {
        if (unit is not UnitSystem u) return;
        u.SetFaction(originalFaction);
        BattleNotifications.Post(
            $"{u.UnitName} regains their senses.",
            BattleNotifications.Severity.Positive);
    }

    private static float AdjustChanceWithKarma(float baseChance, int karma)
    {
        float shift = karma / 100.0f * MaxKarmaShift;
        return Mathf.Clamp(baseChance + shift, 0f, 1f);
    }

    private static void AddCorruptionPoint(IUnitSystem unit)
    {
        if (unit is not UnitSystem u) return;

        if (u.CorruptionLevel >= UnitSystem.MaxCorruptionLevel)
        {
            if (!u.HasEffect<CorruptedTransformationEffect>())
            {
                Faction original = u.Faction;
                CorruptedTransformationEffect effect = new(original, 2);
                u.SetStatusEffectOnUnit(effect);
                u.SetFaction(Faction.Enemy);
            }
            return;
        }

        u.CorruptionPoints++;
        if (u.CorruptionPoints < UnitSystem.CorruptionPointsPerLevel) return;

        int oldLevel = u.CorruptionLevel;
        u.CorruptionLevel = oldLevel + 1;
        u.CorruptionPoints = 0;
        SyncRuntimeMarker(unit, oldLevel, u.CorruptionLevel);

        if (u.CorruptionLevel >= UnitSystem.MaxCorruptionLevel)
        {
            Faction original = u.Faction;
            CorruptedTransformationEffect effect = new(original, 2);
            u.SetStatusEffectOnUnit(effect);
            u.SetFaction(Faction.Enemy);
        }
    }

    private static void SyncRuntimeMarker(IUnitSystem unit, int oldLevel, int newLevel)
    {
        var snapshot = new List<StatusEffect<IUnitSystem>>(unit.GetActiveEffects());
        foreach (StatusEffect<IUnitSystem> existing in snapshot)
        {
            if (existing is CorruptionLevelEffect)
                unit.RemoveEffect(existing);
        }

        StatusEffect<IUnitSystem>? marker = newLevel switch
        {
            1 => new CorruptionLevel1Effect(),
            2 => new CorruptionLevel2Effect(),
            3 => new CorruptionLevel3Effect(),
            _ => null,
        };
        if (marker is not null)
            unit.SetStatusEffectOnUnit(marker);
    }
}
