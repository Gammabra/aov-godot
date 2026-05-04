using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

// =============================================================================
// Mage Eau (Water) — defensive / control.
// =============================================================================

/// <summary>Jet d'Eau — Water Jet. Moderate damage + slow.</summary>
public sealed class WaterJet : SkillSystem
{
    public WaterJet()
    {
        Name = SkillStrings.WaterJetName;
        Description = "Strike for 80% INT and slow the target.";
        ManaCost = 8; TotalCooldown = 1; Cooldown = 0; Range = 5;
        MagicType = AovDataStructures.MagicType.Water;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.Intelligence * 0.8f);
        // TODO: replace with a SpeedDebuff effect once one exists.
    }
}

/// <summary>Pluie Guérisseuse — Healing Rain. Light heal on every ally.</summary>
public sealed class HealingRain : SkillSystem
{
    public HealingRain()
    {
        Name = "Healing Rain";
        Description = "Heal every ally for 60% INT.";
        ManaCost = 16; TotalCooldown = 4; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.Water;
        EffectType = AovDataStructures.EffectType.Heal;
        TargetType = AovDataStructures.TargetTypes.AllAllies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        float heal = caster.Intelligence * 0.6f;
        foreach (IUnitSystem ally in targets)
            ally.OnEffectHeal(heal);
    }
}

/// <summary>Vague Déferlante — Crashing Wave. Knocks enemies back, extinguishes fires.</summary>
public sealed class CrashingWave : SkillSystem
{
    public CrashingWave()
    {
        Name = "Crashing Wave";
        Description = "Wash a wave dealing 70% INT to every enemy in range.";
        ManaCost = 12; TotalCooldown = 3; Cooldown = 0; Range = 4;
        MagicType = AovDataStructures.MagicType.Water;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.AllEnemies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem t in targets)
            t.TakeDamage(caster.Intelligence * 0.7f);
        // TODO: extinguish burning effects and apply knockback once those systems exist.
    }
}

/// <summary>Bulle de Protection — Protection Bubble. Shield an ally for 1 turn.</summary>
public sealed class ProtectiveBubble : SkillSystem
{
    public ProtectiveBubble()
    {
        Name = "Protective Bubble";
        Description = "Wrap an ally in a bubble: +100 DEF for 1 turn.";
        ManaCost = 10; TotalCooldown = 4; Cooldown = 0; Range = 3;
        MagicType = AovDataStructures.MagicType.Water;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].SetStatusEffectOnUnit(new AtkBuffer(1, AovDataStructures.ModifierType.Flat, 100));
    }
}

/// <summary>Glaciation Instantanée — Instant Glaciation. 50% chance to stun in a small area.</summary>
public sealed class FlashFreeze : SkillSystem
{
    public FlashFreeze()
    {
        Name = "Flash Freeze";
        Description = "Freeze a small zone for 50% INT and a 50% chance to stun for 1 turn.";
        ManaCost = 14; TotalCooldown = 4; Cooldown = 0; Range = 4;
        MagicType = AovDataStructures.MagicType.Water;
        EffectType = AovDataStructures.EffectType.Control;
        TargetType = AovDataStructures.TargetTypes.AllEnemies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem t in targets)
        {
            t.TakeDamage(caster.Intelligence * 0.5f);
            if (System.Random.Shared.NextDouble() < 0.5)
                t.SetStatusEffectOnUnit(new Stun(1));
        }
    }
}
