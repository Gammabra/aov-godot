using System;
using System.Collections.Generic;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad;

/// <summary>
/// Archer enemy unit with ranged attack AI behavior.
/// Uses the existing AI system to make tactical decisions.
/// </summary>
public sealed partial class EnemyArcher : UnitSystem
{
    private BasicAttackSkill _basicAttack = null!;

    protected override void Initialize()
    {
        UnitName = "Archer";
        Description = "Ranged enemy unit that attacks from distance";
        MaxHp = 80;
        Hp = MaxHp;
        BaseAtk = 25;
        BaseDef = 5;
        BaseSpeed = 120;
        Intelligence = 15;
        ManaMax = 50;
        Mana = ManaMax;
        IsAlive = true;
        PossibleMovesRange = 3;
        Curse = 0;
        Type = AovDataStructures.UnitType.Archer;
        Personality = AIPersonality.Defensive;

        // Initialize skills
        _basicAttack = new BasicAttackSkill();
        ActiveSkills = new List<ISkillSystem> { _basicAttack };

        Console.WriteLine($"EnemyArcher {UnitName} initialized with {ActiveSkills.Count} skills");

        base.Initialize();

        var statusEffectSystem = new StatusEffectSystem();
        InjectDependencies(statusEffectSystem);
    }
}

/// <summary>
/// Basic ranged attack skill for the archer enemy.
/// </summary>
public class BasicAttackSkill : SkillSystem
{
    public BasicAttackSkill()
    {
        Name = "Arrow Shot";
        Description = "Basic ranged attack";
        ManaCost = 0;
        TotalCooldown = 0;
        Cooldown = 0;
        Range = 4; // Archer has longer range
        AreaEffect = new List<(int, int, int)> { (0, 0, 0) }; // Single target
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;

        foreach (IUnitSystem target in targets)
        {
            if (target.IsAlive)
            {
                float damage = 25f; // Base damage
                target.TakeDamage(damage);
                Console.WriteLine($"Archer used {Name} on {target.UnitName} for {damage} damage");
            }
        }
    }
}
