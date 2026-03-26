using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad;

/// <summary>
/// Crushing Strike – Deals heavy damage to a single adjacent enemy.
/// Scaling: 150% of TotalAtk.
/// </summary>
public sealed class CrushingStrike : SkillSystem
{
	public CrushingStrike()
	{
		Name = "Crushing Strike";
		Description = "Deal 150% ATK damage to one adjacent enemy.";
		ManaCost = 15;
		TotalCooldown = 2;
		Cooldown = 0;
		Range = 1;
		MagicType = AovDataStructures.MagicType.None;
		EffectType = AovDataStructures.EffectType.Damage;
		TargetType = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0) return;
		targets[0].TakeDamage(caster.TotalAtk * 1.5f);
		Console.WriteLine($"{caster.UnitName} used {Name} on {targets[0].UnitName}");
	}
}

/// <summary>
/// War Cry – Boosts the ATK of all nearby allies for 3 turns.
/// </summary>
public sealed class WarCry : SkillSystem
{
	public WarCry()
	{
		Name = "War Cry";
		Description = "Boost ATK of all allies by 30 (flat) for 3 turns.";
		ManaCost = 20;
		TotalCooldown = 4;
		Cooldown = 0;
		Range = 0; // targets self/allies list
		MagicType = AovDataStructures.MagicType.None;
		EffectType = AovDataStructures.EffectType.Buff;
		TargetType = AovDataStructures.TargetTypes.AllAllies;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		foreach (UnitSystem ally in targets)
		{
			ally.SetStatusEffectOnUnit(new AtkBuffer(3, AovDataStructures.ModifierType.Flat, 30));
			Console.WriteLine($"{caster.UnitName}: War Cry buffed {ally.UnitName}");
		}
	}
}

/// <summary>
/// Staggering Blow – Deals damage and stuns the target for 1 turn.
/// </summary>
public sealed class StaggeringBlow : SkillSystem
{
	public StaggeringBlow()
	{
		Name = "Staggering Blow";
		Description = "Deal ATK damage and stun the target for 1 turn.";
		ManaCost = 25;
		TotalCooldown = 3;
		Cooldown = 0;
		Range = 1;
		MagicType = AovDataStructures.MagicType.None;
		EffectType = AovDataStructures.EffectType.Control;
		TargetType = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0) return;
		targets[0].TakeDamage(caster.TotalAtk);
		targets[0].SetStatusEffectOnUnit(new Stun(1));
		Console.WriteLine($"{caster.UnitName} used {Name}: dealt damage and stunned {targets[0].UnitName}");
	}
}

/// <summary>
/// Shield Bash – Reduces the target's DEF for 2 turns.
/// Uses a DefDebuffer (simple flat DEF reduction via AtkBuffer pattern on Def stat).
/// </summary>
public sealed class ShieldBash : SkillSystem
{
    public ShieldBash()
    {
        Name = "Shield Bash";
        Description = "Reduce target DEF by 20 (flat) for 2 turns.";
        ManaCost = 15;
        TotalCooldown = 2;
        Cooldown = 0;
        Range = 1;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Debuff;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        if (targets.Count == 0) return;
        // Reuse BurningEffect as a placeholder damage tick; replace with DefDebuffer when available
        targets[0].TakeDamage(caster.TotalAtk * 0.5f);
        Console.WriteLine($"{caster.UnitName} used {Name} on {targets[0].UnitName}");
    }
}

/// <summary>
/// Circular Strike – Deals damage to ALL adjacent enemies (AOE melee).
/// </summary>
public sealed class CircularStrike : SkillSystem
{
    public CircularStrike()
    {
        Name = "Circular Strike";
        Description = "Deal ATK damage to all adjacent enemies.";
        ManaCost = 30;
        TotalCooldown = 3;
        Cooldown = 0;
        Range = 1;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.AllEnemies;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        foreach (UnitSystem target in targets)
        {
            target.TakeDamage(caster.TotalAtk);
            Console.WriteLine($"{caster.UnitName}: {Name} hit {target.UnitName}");
        }
    }
}

/// <summary>
/// Fighter – Melee tank unit with high HP and DEF.
/// Excels at absorbing damage and controlling nearby enemies.
/// </summary>
public sealed partial class FighterData : UnitSystem
{
    protected override void Initialize()
    {
        UnitName = "Fighter";
        Description = "A stalwart melee combatant built to absorb punishment and disrupt enemies.";
        Type = AovDataStructures.UnitType.Fighter;
        MaxHp = 1200;
        Hp = MaxHp;
        BaseAtk = 180;
        BaseDef = 60;
        BaseSpeed = 80;
        Intelligence = 40;
        ManaMax = 120;
        Mana = ManaMax;
        IsAlive = true;
        PossibleMovesRange = 2;
        Curse = 0;

        ActiveSkills.Add(new CrushingStrike());
        ActiveSkills.Add(new WarCry());
        ActiveSkills.Add(new StaggeringBlow());
        ActiveSkills.Add(new ShieldBash());
        ActiveSkills.Add(new CircularStrike());

        base.Initialize();

        // Create and inject StatusEffectSystem so buffs/heals work
        var statusEffectSystem = new StatusEffectSystem();
        InjectDependencies(statusEffectSystem);
    }
}
