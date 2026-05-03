using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

// =============================================================================
// Mage Lumière (Light) — heal / buffs + purification.
// =============================================================================

/// <summary>Rayon Sacré — Sacred Ray. Damages enemies, heals allies in its line.</summary>
public sealed class RayonSacre : SkillSystem
{
    public RayonSacre()
    {
        Name = "Rayon Sacré";
        Description = "Beam a sacred ray dealing 100% INT to one enemy. (Healing path TODO.)";
        ManaCost = 12; TotalCooldown = 2; Cooldown = 0; Range = 6;
        MagicType = AovDataStructures.MagicType.Light;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.Intelligence);
    }
}

/// <summary>Éclat Purificateur — Purifying Burst. Removes every status effect from one ally.</summary>
public sealed class EclatPurificateur : SkillSystem
{
    public EclatPurificateur()
    {
        Name = "Éclat Purificateur";
        Description = "Remove every status effect from a single ally.";
        ManaCost = 12; TotalCooldown = 4; Cooldown = 0; Range = 3;
        MagicType = AovDataStructures.MagicType.Light;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        IUnitSystem ally = targets[0];
        // Iterate over a copy because RemoveEffect mutates the underlying list.
        var snapshot = new List<StatusEffect<IUnitSystem>>(ally.GetActiveEffects());
        foreach (StatusEffect<IUnitSystem> effect in snapshot)
            ally.RemoveEffect(effect);
    }
}

/// <summary>Jugement Divin — Divine Judgment. Lightning strike, +200% damage vs corrupted.</summary>
public sealed class JugementDivin : SkillSystem
{
    public JugementDivin()
    {
        Name = "Jugement Divin";
        Description = "Smite for 130% INT (TODO: triple damage on corrupted units).";
        ManaCost = 18; TotalCooldown = 4; Cooldown = 0; Range = 6;
        MagicType = AovDataStructures.MagicType.Light;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.Intelligence * 1.3f);
        // TODO: detect corruption and apply +200% damage.
    }
}

/// <summary>Résurrection — Resurrection. Revives a fallen ally with 50% HP.</summary>
public sealed class Resurrection : SkillSystem
{
    public Resurrection()
    {
        Name = "Résurrection";
        Description = "Revive a fallen ally with 50% of their max HP.";
        ManaCost = 30; TotalCooldown = 8; Cooldown = 0; Range = 3;
        MagicType = AovDataStructures.MagicType.Light;
        EffectType = AovDataStructures.EffectType.Revive;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].OnEffectRevive(AovDataStructures.ModifierType.Percent, 50);
    }
}

/// <summary>Prière Divine — Divine Prayer. +Status resistance for 3 turns.</summary>
public sealed class PriereDivine : SkillSystem
{
    public PriereDivine()
    {
        Name = "Prière Divine";
        Description = "Bless every ally with +20 DEF for 3 turns. (Status-resist TODO.)";
        ManaCost = 14; TotalCooldown = 4; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.Light;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.AllAllies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem ally in targets)
            ally.SetStatusEffectOnUnit(new AtkBuffer(3, AovDataStructures.ModifierType.Flat, 20));
    }
}
