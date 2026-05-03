using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

// =============================================================================
// Assassin — 5 actives + 3 passives.
// "Coup Critique" is the only doc-fixed cooldown (CD = 3).
// =============================================================================

#region Actives

/// <summary>Coup Critique — Critical Strike. Double damage. CD 3 (doc-fixed).</summary>
public sealed class CoupCritique : SkillSystem
{
    public CoupCritique()
    {
        Name = "Coup Critique";
        Description = "Deal 200% ATK to one enemy.";
        ManaCost = 10; TotalCooldown = 3; Cooldown = 0; Range = 1;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.TotalAtk * 2.0f);
    }
}

/// <summary>Disparition — Vanish. Become invisible until end of next turn.</summary>
/// <remarks>Modeled as a +DEF buff; replace with a stealth flag once one exists.</remarks>
public sealed class Disparition : SkillSystem
{
    public Disparition()
    {
        Name = "Disparition";
        Description = "Vanish — +120 DEF for 2 turns (placeholder for stealth).";
        ManaCost = 8; TotalCooldown = 4; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        caster.SetStatusEffectOnUnit(new AtkBuffer(2, AovDataStructures.ModifierType.Flat, 120));
    }
}

/// <summary>Coup d'Ombre — Shadow Strike. Attacks without breaking invisibility.</summary>
public sealed class CoupDOmbre : SkillSystem
{
    public CoupDOmbre()
    {
        Name = "Coup d'Ombre";
        Description = "Strike from the shadows for 130% ATK without breaking stealth.";
        ManaCost = 8; TotalCooldown = 2; Cooldown = 0; Range = 1;
        MagicType = AovDataStructures.MagicType.Dark;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].BypassDamage(caster.TotalAtk * 1.3f);
    }
}

/// <summary>Exécution — Execute. Instantly kills an enemy below 15% HP.</summary>
public sealed class Execution : SkillSystem
{
    public Execution()
    {
        Name = "Exécution";
        Description = "Instantly defeat an enemy below 15% HP. Otherwise deal 100% ATK.";
        ManaCost = 14; TotalCooldown = 5; Cooldown = 0; Range = 1;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        IUnitSystem t = targets[0];
        bool below = t.MaxHp > 0 && t.Hp / t.MaxHp <= 0.15f;
        if (below) t.BypassDamage(t.Hp);
        else t.TakeDamage(caster.TotalAtk);
    }
}

/// <summary>Frappe Sanguine — Blood Strike. Damages and heals the caster for 20% of damage dealt.</summary>
public sealed class FrappeSanguine : SkillSystem
{
    public FrappeSanguine()
    {
        Name = "Frappe Sanguine";
        Description = "Strike for 100% ATK and heal yourself for 20% of damage dealt.";
        ManaCost = 7; TotalCooldown = 2; Cooldown = 0; Range = 1;
        MagicType = AovDataStructures.MagicType.Dark;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        float dmg = caster.TotalAtk;
        targets[0].TakeDamage(dmg);
        caster.OnEffectHeal(dmg * 0.2f);
    }
}

#endregion

#region Passives

public sealed class FrappeSournoise : SkillSystem
{
    public FrappeSournoise()
    {
        Name = "Frappe Sournoise";
        Description = "Passive — +30% damage if the target hasn't yet attacked you.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

public sealed class OmbreFurtive : SkillSystem
{
    public OmbreFurtive()
    {
        Name = "Ombre Furtive";
        Description = "Passive — pass through enemy units without being blocked.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

public sealed class ToxinesMortelles : SkillSystem
{
    public ToxinesMortelles()
    {
        Name = "Toxines Mortelles";
        Description = "Passive — 20% chance to poison on every successful hit.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

#endregion
