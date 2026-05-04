using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

// =============================================================================
// Épéiste (Swordsman) — 5 actives + 3 passives.
// =============================================================================

#region Actives

/// <summary>Frappe Éclair — Lightning Strike. Rush + +20% damage at range 5.</summary>
public sealed class FrappeEclair : SkillSystem
{
    public FrappeEclair()
    {
        Name = SkillStrings.LightningStrikeName;
        Description = SkillStrings.LightningStrikeDesc;
        ManaCost = 10; TotalCooldown = 2; Cooldown = 0; Range = 5;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.TotalAtk * 1.2f);
    }
}

/// <summary>Danse des Lames — Dance of Blades. Strikes up to 3 adjacent enemies.</summary>
public sealed class DanseDesLames : SkillSystem
{
    public DanseDesLames()
    {
        Name = SkillStrings.BladeDanceName;
        Description = SkillStrings.BladeDanceDesc;
        ManaCost = 14; TotalCooldown = 3; Cooldown = 0; Range = 1;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.AllEnemies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        int hits = 0;
        foreach (IUnitSystem t in targets)
        {
            if (hits >= 3) break;
            t.TakeDamage(caster.TotalAtk);
            hits++;
        }
    }
}

/// <summary>Coup de Bravoure — Brave Strike. Self-buff: -50% damage taken + counter for 1 turn.</summary>
public sealed class CoupDeBravoure : SkillSystem
{
    public CoupDeBravoure()
    {
        Name = SkillStrings.GallantStrikeName;
        Description = SkillStrings.GallantStrikeDesc;
        ManaCost = 8; TotalCooldown = 4; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        caster.SetStatusEffectOnUnit(new AtkBuffer(1, AovDataStructures.ModifierType.Flat, 80));
    }
}

/// <summary>Frappe Fantôme — Phantom Strike. Reach attack at 2 tiles distance.</summary>
public sealed class FrappeFantome : SkillSystem
{
    public FrappeFantome()
    {
        Name = SkillStrings.PhantomStrikeName;
        Description = SkillStrings.PhantomStrikeDesc;
        ManaCost = 6; TotalCooldown = 1; Cooldown = 0; Range = 2;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.TotalAtk * 1.3f);
    }
}

/// <summary>Lame d'Exécution — Execution Blade. +50% damage to targets below 30% HP.</summary>
public sealed class LameDExecution : SkillSystem
{
    public LameDExecution()
    {
        Name = SkillStrings.ExecutionerBladeName;
        Description = SkillStrings.ExecutionerBladeDesc;
        ManaCost = 12; TotalCooldown = 3; Cooldown = 0; Range = 1;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        IUnitSystem t = targets[0];
        bool low = t.MaxHp > 0 && t.Hp / t.MaxHp <= 0.30f;
        t.TakeDamage(caster.TotalAtk * (low ? 2.5f : 1.0f));
    }
}

#endregion

#region Passives

/// <summary>Riposte — 30% chance to counterattack after a melee hit.</summary>
public sealed class Riposte : SkillSystem
{
    public Riposte()
    {
        Name = SkillStrings.RiposteName;
        Description = SkillStrings.RiposteDesc;
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

/// <summary>Danse du Lame (passive) — +10% crit chance if the unit moved before attacking.</summary>
public sealed class DanseDuLame : SkillSystem
{
    public DanseDuLame()
    {
        Name = SkillStrings.BladeDancePassiveName;
        Description = SkillStrings.BladeDancePassiveDesc;
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

/// <summary>Lame Spectrale — ignores 15% of enemy armor.</summary>
public sealed class LameSpectrale : SkillSystem
{
    public LameSpectrale()
    {
        Name = SkillStrings.SpectralBladePassiveName;
        Description = SkillStrings.SpectralBladePassiveDesc;
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

#endregion
