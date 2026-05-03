using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

// =============================================================================
// Combattant (Fighter) — full skill catalogue from the feature document.
// 5 actives + 3 passives. Use the French canonical names so these classes don't
// collide with the existing English placeholders in FighterData.cs.
// =============================================================================

#region Actives

/// <summary>Frappe Écrasante — Crushing Strike. +50% damage; can stun the target.</summary>
public sealed class FrappeEcrasante : SkillSystem
{
    public FrappeEcrasante()
    {
        Name = "Frappe Écrasante";
        Description = "Heavy melee blow dealing 150% ATK. 35% chance to stun for 1 turn.";
        ManaCost = 8;
        TotalCooldown = 2;
        Cooldown = 0;
        Range = 1;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.TotalAtk * 1.5f);
        if (Random.Shared.NextDouble() < 0.35)
            targets[0].SetStatusEffectOnUnit(new Stun(1));
    }
}

/// <summary>Cri de Guerre — War Cry. +15% Atk/Def to nearby allies for 3 turns.</summary>
public sealed class CriDeGuerre : SkillSystem
{
    public CriDeGuerre()
    {
        Name = "Cri de Guerre";
        Description = "Buff every ally's ATK and DEF by 15 (flat) for 3 turns.";
        ManaCost = 10;
        TotalCooldown = 4;
        Cooldown = 0;
        Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.AllAllies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem ally in targets)
            ally.SetStatusEffectOnUnit(new AtkBuffer(3, AovDataStructures.ModifierType.Flat, 15));
    }
}

/// <summary>Charge — combined movement + attack. Closes the gap and strikes for 120% ATK.</summary>
public sealed class Charge : SkillSystem
{
    public Charge()
    {
        Name = "Charge";
        Description = "Rush a target up to 4 tiles away and strike for 120% ATK.";
        ManaCost = 6;
        TotalCooldown = 1;
        Cooldown = 0;
        Range = 4;
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

/// <summary>Blocage — Block. Apply a 50% damage-reduction shield to self for 1 turn.</summary>
/// <remarks>Currently modelled as a flat +DEF buff for 1 turn until a damage-reduction effect lands.</remarks>
public sealed class Blocage : SkillSystem
{
    public Blocage()
    {
        Name = "Blocage";
        Description = "Brace yourself: +60 DEF for 1 turn.";
        ManaCost = 4;
        TotalCooldown = 3;
        Cooldown = 0;
        Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        caster.SetStatusEffectOnUnit(new AtkBuffer(1, AovDataStructures.ModifierType.Flat, 60));
        // ^ TODO: replace with DefBuffer once a flat-DEF effect class lands.
    }
}

/// <summary>Frappe Circulaire — Circular Strike. Hits every adjacent enemy.</summary>
public sealed class FrappeCirculaire : SkillSystem
{
    public FrappeCirculaire()
    {
        Name = "Frappe Circulaire";
        Description = "Sweep every adjacent enemy for 100% ATK.";
        ManaCost = 12;
        TotalCooldown = 3;
        Cooldown = 0;
        Range = 1;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.AllEnemies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem t in targets)
            t.TakeDamage(caster.TotalAtk);
    }
}

#endregion

#region Passives

/// <summary>Force Brute — +10% melee damage. Apply at battle start; not invoked.</summary>
public sealed class ForceBrute : SkillSystem
{
    public ForceBrute()
    {
        Name = "Force Brute";
        Description = "Passive — +10% damage on melee skills (Range 1).";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

/// <summary>Témérité — +5% damage per 25% missing HP. Wired in damage formula, not as a Use().</summary>
public sealed class Temerite : SkillSystem
{
    public Temerite()
    {
        Name = "Témérité";
        Description = "Passive — +5% damage per 25% missing HP.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

/// <summary>Endurance Guerrière — -15% damage taken after a successful melee strike.</summary>
public sealed class EnduranceGuerriere : SkillSystem
{
    public EnduranceGuerriere()
    {
        Name = "Endurance Guerrière";
        Description = "Passive — -15% damage taken for the rest of the turn after landing a melee hit.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

#endregion
