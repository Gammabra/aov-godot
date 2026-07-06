using System;
using System.Collections.Generic;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;
using Catalog = AshesOfVelsingrad.Data.Skills;

namespace AshesOfVelsingrad;

/// <summary>
/// Archer enemy unit with ranged attack AI behavior.
/// Uses the existing AI system to make tactical decisions.
/// </summary>
public sealed partial class ThiefArcher : UnitSystem
{
    /// <summary>Display name shown in the HUD. Overridable per-instance.</summary>
    [Export]
    public string ThiefName { get; set; } = "Thief Archer";

    /// <summary>Level shown next to the name in the turn-queue chip / status panel.</summary>
    [Export]
    public int ThiefLevel { get; set; } = 1;

    /// <summary>Portrait <c>res://</c> path. Falls back to a coloured square if empty.</summary>
    [Export(PropertyHint.File, "*.png,*.jpg,*.svg")]
    public string ThiefPortraitPath { get; set; } = "res://assets/Krita/icone_bandit.png";

    /// <summary>Max HP. Adjust per-instance for tougher / weaker variants.</summary>
    [Export]
    public float ThiefMaxHp { get; set; } = 600f;

    /// <summary>Base attack. Default sized to be a fair fight for a level-1 fighter.</summary>
    [Export]
    public float ThiefBaseAtk { get; set; } = 100f;

    /// <summary>Base defence.</summary>
    [Export]
    public float ThiefBaseDef { get; set; } = 20f;

    /// <summary>Base speed — drives turn order. Lower than Kaelen's 180 so the player acts first.</summary>
    [Export]
    public float ThiefBaseSpeed { get; set; } = 140f;

    /// <summary>Tactical-AI personality: Aggressive / Defensive / Opportunistic / Balanced.</summary>
    [Export]
    public AIPersonality ThiefPersonality { get; set; } = AIPersonality.Defensive;


    protected override void Initialize()
    {
        UnitName = ThiefName;
        Description = "Ranged enemy unit that attacks from distance";
        MaxHp = ThiefMaxHp;
        Hp = MaxHp;
        BaseAtk = ThiefBaseAtk;
        BaseDef = ThiefBaseDef;
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
        ActiveSkills.Add(new BasicAttackSkill());
        ActiveSkills.Add(new Catalog.MultiShot());
        ActiveSkills.Add(new Catalog.HawkEye());

        base.Initialize();

        var statusEffectSystem = new StatusEffectSystem();
        InjectDependencies(statusEffectSystem);

        SetEntityProfile(new EntityProfile
        {
            DisplayName = ThiefName,
            ClassName = "Archer",
            Level = ThiefLevel,
            PortraitPath = ThiefPortraitPath,
        });
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
