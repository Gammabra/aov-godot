using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

// =============================================================================
// Archer — 5 actives + 3 passives.
// =============================================================================

#region Actives

/// <summary>Flèche Perforante — Piercing Arrow. Pierces up to 2 aligned enemies.</summary>
public sealed class PiercingShot : SkillSystem
{
    public PiercingShot()
    {
        Name = SkillStrings.PiercingShotName;
        Description = "Pierce up to 2 aligned enemies for 110% ATK.";
        ManaCost = 8; TotalCooldown = 2; Cooldown = 0; Range = 6;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        int hits = 0;
        foreach (IUnitSystem t in targets)
        {
            if (hits >= 2) break;
            t.TakeDamage(caster.TotalAtk * 1.1f);
            hits++;
        }
    }
}

/// <summary>Pluie de Flèches — Arrow Rain. AoE damage in a 3×3 zone.</summary>
public sealed class MultiShot : SkillSystem
{
    public MultiShot()
    {
        Name = SkillStrings.MultiShotName;
        Description = "Rain arrows in a 3×3 zone for 80% ATK each.";
        ManaCost = 14; TotalCooldown = 4; Cooldown = 0; Range = 7;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.AllEnemies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem t in targets)
            t.TakeDamage(caster.TotalAtk * 0.8f);
    }
}

/// <summary>Flèche Empoisonnée — Poison Arrow. Burning DOT 3 turns (poison stand-in).</summary>
public sealed class PoisonArrow : SkillSystem
{
    public PoisonArrow()
    {
        Name = SkillStrings.PoisonArrowName;
        Description = "Strike for 80% ATK + apply burning (poison stand-in) for 3 turns.";
        ManaCost = 7; TotalCooldown = 2; Cooldown = 0; Range = 6;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Debuff;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.TotalAtk * 0.8f);
        targets[0].SetStatusEffectOnUnit(new BurningEffect(3, AovDataStructures.ModifierType.Flat, 8));
    }
}

/// <summary>Œil de Faucon — Hawk's Eye. +2 range for 1 turn (modelled as ATK buff stand-in).</summary>
public sealed class HawkEye : SkillSystem
{
    public HawkEye()
    {
        Name = SkillStrings.HawkEyeName;
        Description = "Sharpen aim: +20 ATK for 2 turns (range-buff placeholder).";
        ManaCost = 4; TotalCooldown = 3; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        caster.SetStatusEffectOnUnit(new AtkBuffer(2, AovDataStructures.ModifierType.Flat, 20));
    }
}

/// <summary>Flèche de Givre — Frost Arrow. Stuns the target for 1 turn.</summary>
public sealed class FrostArrow : SkillSystem
{
    public FrostArrow()
    {
        Name = "Frost Arrow";
        Description = "Strike for 60% ATK and freeze the target for 1 turn.";
        ManaCost = 9; TotalCooldown = 3; Cooldown = 0; Range = 6;
        MagicType = AovDataStructures.MagicType.Water;
        EffectType = AovDataStructures.EffectType.Control;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.TotalAtk * 0.6f);
        targets[0].SetStatusEffectOnUnit(new Stun(1));
    }
}

#endregion

#region Passives

public sealed class PreciseShot : SkillSystem
{
    public PreciseShot()
    {
        Name = "Precise Shot";
        Description = "Passive — +15% accuracy when the unit didn't move this turn.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

public sealed class LethalAim : SkillSystem
{
    public LethalAim()
    {
        Name = "Lethal Aim";
        Description = "Passive — +10% damage to wounded enemies (HP < 50%).";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

public sealed class MobileShot : SkillSystem
{
    public MobileShot()
    {
        Name = "Mobile Shot";
        Description = "Passive — no penalty when attacking after moving.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

#endregion
