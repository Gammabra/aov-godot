using System;
using System.Collections.Generic;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad;

/// <summary>
/// Enemy Light Mage's basic magic attack.
/// </summary>
public sealed class LightBolt : SkillSystem
{
    public LightBolt()
    {
        Name = "Light Bolt";
        Description = "Fire a bolt of light energy dealing 80 damage.";
        ManaCost = 0;
        TotalCooldown = 0;
        Cooldown = 0;
        Range = 4;
        MagicType = AovDataStructures.MagicType.Light;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(80f);
        Console.WriteLine($"{caster.UnitName}: {Name} hit {targets[0].UnitName} for 80");
    }
}

/// <summary>
/// Enemy Light Mage's special: heals the most wounded ally.
/// </summary>
public sealed class EnemyHealAlly : SkillSystem
{
    public EnemyHealAlly()
    {
        Name = "Mending Light";
        Description = "Restore 100 HP to one ally.";
        ManaCost = 25;
        TotalCooldown = 3;
        Cooldown = 0;
        Range = 3;
        MagicType = AovDataStructures.MagicType.Light;
        EffectType = AovDataStructures.EffectType.Heal;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].OnEffectHeal(100f);
        Console.WriteLine($"{caster.UnitName}: {Name} healed {targets[0].UnitName} for 100 HP");
    }
}

/// <summary>
/// Enemy Light Mage – Support enemy that heals allies and fires light bolts.
/// Mirrors <see cref="LightMageData"/> in role and stat profile.
/// Assigned Defensive personality so the AI prioritises keeping allies alive.
/// </summary>
public sealed partial class EnemyLightMage : UnitSystem
{
    protected override void Initialize()
    {
        UnitName = "Enemy Light Mage";
        Description = "A radiant enemy healer who keeps the enemy team alive.";
        Type = AovDataStructures.UnitType.Mage;
        MaxHp = 600;
        Hp = MaxHp;
        BaseAtk = 80;
        BaseDef = 15;
        BaseSpeed = 100;
        Intelligence = 200;
        ManaMax = 250;
        Mana = ManaMax;
        IsAlive = true;
        PossibleMovesRange = 2;
        Curse = 0;
        Personality = AIPersonality.Defensive;

        ActiveSkills.Add(new LightBolt());
        ActiveSkills.Add(new EnemyHealAlly());

        Console.WriteLine($"{UnitName} initialized with {ActiveSkills.Count} skills");

        base.Initialize();

        var statusEffectSystem = new StatusEffectSystem();
        InjectDependencies(statusEffectSystem);
    }
}
