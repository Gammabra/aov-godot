using System.Collections.Generic;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
/// Sacred Ray – Deals Light damage to one enemy and heals the first ally in line.
/// Implemented as a targeted damage + self-heal hybrid.
/// </summary>
public sealed class SacredRay : SkillSystem
{
    public SacredRay()
    {
        Name        = "Sacred Ray";
        Description = "Deal 100% INT damage to one enemy and heal the caster for 20% of damage dealt.";
        ManaCost    = 20;
        TotalCooldown = 1;
        Cooldown    = 0;
        Range       = 4;
        MagicType   = AovDataStructures.MagicType.Light;
        EffectType  = AovDataStructures.EffectType.Damage;
        TargetType  = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        if (targets.Count == 0) return;
        float damage = caster.Intelligence;
        targets[0].TakeDamage(damage);
        caster.OnEffectHeal(damage * 0.2f);
        GD.Print($"{caster.UnitName}: {Name} – {damage} to {targets[0].UnitName}, healed self");
    }
}

/// <summary>
/// Healing Touch – Restores a flat amount of HP to one ally.
/// </summary>
public sealed class HealingTouch : SkillSystem
{
    public HealingTouch()
    {
        Name        = "Healing Touch";
        Description = "Restore 150 HP to one ally.";
        ManaCost    = 25;
        TotalCooldown = 1;
        Cooldown    = 0;
        Range       = 3;
        MagicType   = AovDataStructures.MagicType.Light;
        EffectType  = AovDataStructures.EffectType.Heal;
        TargetType  = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].OnEffectHeal(150f);
        GD.Print($"{caster.UnitName}: {Name} – healed {targets[0].UnitName} for 150 HP");
    }
}

/// <summary>
/// Cleansing Light – Removes all status effects from one ally.
/// Currently removes all active effects via the effect list.
/// </summary>
public sealed class CleansingLight : SkillSystem
{
    public CleansingLight()
    {
        Name        = "Cleansing Light";
        Description = "Remove all negative status effects from one ally.";
        ManaCost    = 30;
        TotalCooldown = 3;
        Cooldown    = 0;
        Range       = 3;
        MagicType   = AovDataStructures.MagicType.Light;
        EffectType  = AovDataStructures.EffectType.RemoveModifier;
        TargetType  = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        if (targets.Count == 0) return;
        UnitSystem target = targets[0];
        var effects = target.GetActiveEffects();
        foreach (var effect in effects)
            target.RemoveEffect(effect);
        GD.Print($"{caster.UnitName}: {Name} – cleansed {target.UnitName} ({effects.Count} effects removed)");
    }
}

/// <summary>
/// Resurrection – Revives a fallen ally with 50% HP.
/// Uses OnEffectRevive which checks IsAlive internally.
/// </summary>
public sealed class Resurrection : SkillSystem
{
    public Resurrection()
    {
        Name        = "Resurrection";
        Description = "Revive a fallen ally with 50% of their max HP.";
        ManaCost    = 60;
        TotalCooldown = 5;
        Cooldown    = 0;
        Range       = 2;
        MagicType   = AovDataStructures.MagicType.Light;
        EffectType  = AovDataStructures.EffectType.Revive;
        TargetType  = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        if (targets.Count == 0) return;
        UnitSystem target = targets[0];
        float reviveAmount = target.MaxHp * 0.5f;
        target.OnEffectRevive(AovDataStructures.ModifierType.Flat, reviveAmount);
        GD.Print($"{caster.UnitName}: {Name} – revived {target.UnitName} with {reviveAmount} HP");
    }
}

/// <summary>
/// Divine Prayer – Boosts ATK of all allies for 3 turns.
/// </summary>
public sealed class DivinePrayer : SkillSystem
{
    public DivinePrayer()
    {
        Name        = "Divine Prayer";
        Description = "Boost ATK of all allies by 25 (flat) for 3 turns.";
        ManaCost    = 40;
        TotalCooldown = 4;
        Cooldown    = 0;
        Range       = 0;
        MagicType   = AovDataStructures.MagicType.Light;
        EffectType  = AovDataStructures.EffectType.Buff;
        TargetType  = AovDataStructures.TargetTypes.AllAllies;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        foreach (UnitSystem ally in targets)
        {
            ally.SetStatusEffectOnUnit(new AtkBuffer(3, AovDataStructures.ModifierType.Flat, 25));
            GD.Print($"{caster.UnitName}: {Name} – blessed {ally.UnitName}");
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Unit
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Light Mage – Support/healer unit. Low ATK and DEF but high Intelligence and Mana.
/// Specialises in healing, cleansing, reviving and buffing allies.
/// </summary>
public sealed partial class LightMageData : UnitSystem
{
    protected override void Initialize()
    {
        base.Initialize();

        UnitName    = "Light Mage";
        Description = "A radiant spellcaster dedicated to protecting and restoring allies.";
        Type        = AovDataStructures.UnitType.Mage;
        MaxHp       = 700;
        Hp          = MaxHp;
        BaseAtk     = 100;
        BaseDef     = 20;
        BaseSpeed   = 110;
        Intelligence = 250;
        ManaMax     = 300;
        Mana        = ManaMax;
        IsAlive     = true;
        PossibleMovesRange = 2;
        Curse       = 0;

        ActiveSkills.Add(new SacredRay());
        ActiveSkills.Add(new HealingTouch());
        ActiveSkills.Add(new CleansingLight());
        ActiveSkills.Add(new Resurrection());
        ActiveSkills.Add(new DivinePrayer());
    }

    public override void TakeDamage(float damage)
    {
        float realDamage = damage - TotalDef;
        if (realDamage < 0) realDamage = 0;

        Hp -= realDamage;
        GD.Print($"{UnitName} took {realDamage} damage (raw: {damage}), HP: {Hp}/{MaxHp}");

        if (Hp <= 0)
        {
            Hp = 0;
            IsAlive = false;
            GD.Print($"{UnitName} has been defeated!");
        }
    }
}
