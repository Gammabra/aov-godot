using System.Collections.Generic;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
/// Enemy Swordsman's basic melee attack.
/// </summary>
public sealed class SwordsmanSlash : SkillSystem
{
    public SwordsmanSlash()
    {
        Name = "Quick Slash";
        Description = "A swift slash dealing 100% ATK damage.";
        ManaCost = 0;
        TotalCooldown = 0;
        Cooldown = 0;
        Range = 1;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.TotalAtk);
        GD.Print($"{caster.UnitName}: {Name} hit {targets[0].UnitName}");
    }
}

/// <summary>
/// Enemy Swordsman's special: a burning slash with fire damage-over-time.
/// </summary>
public sealed class EnemyBurningSlash : SkillSystem
{
    public EnemyBurningSlash()
    {
        Name = "Flame Slash";
        Description = "Deal ATK damage and apply Burning for 2 turns.";
        ManaCost = 20;
        TotalCooldown = 3;
        Cooldown = 0;
        Range = 1;
        MagicType = AovDataStructures.MagicType.Fire;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.TotalAtk);
        targets[0].SetStatusEffectOnUnit(new BurningEffect(2, AovDataStructures.ModifierType.Flat, 12));
        GD.Print($"{caster.UnitName}: {Name} burned {targets[0].UnitName}");
    }
}

/// <summary>
/// Enemy Swordsman – Balanced melee enemy with a burning slash special.
/// Mirrors <see cref="SwordsmanData"/> in role and stat profile.
/// </summary>
public sealed partial class EnemySwordsman : UnitSystem
{
    protected override void Initialize()
    {
        UnitName = "Enemy Swordsman";
        Description = "A skilled enemy blade fighter who pressures with fire.";
        Type = AovDataStructures.UnitType.Swordsman;
        MaxHp = 750;
        Hp = MaxHp;
        BaseAtk = 170;
        BaseDef = 30;
        BaseSpeed = 120;
        Intelligence = 30;
        ManaMax = 100;
        Mana = ManaMax;
        IsAlive = true;
        PossibleMovesRange = 3;
        Curse = 0;
        Personality = AIPersonality.Balanced;

        ActiveSkills.Add(new SwordsmanSlash());
        ActiveSkills.Add(new EnemyBurningSlash());

        GD.Print($"{UnitName} initialized with {ActiveSkills.Count} skills");

        base.Initialize();

        var statusEffectSystem = new StatusEffectSystem();
        InjectDependencies(statusEffectSystem);
    }
}
