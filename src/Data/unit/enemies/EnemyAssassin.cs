using System;
using System.Collections.Generic;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad;

/// <summary>
/// Enemy Assassin's basic ranged stab (slightly extended reach).
/// </summary>
public sealed class AssassinStab : SkillSystem
{
    public AssassinStab()
    {
        Name = "Shadow Stab";
        Description = "A precise stab from a short distance dealing 100% ATK.";
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
        Console.WriteLine($"{caster.UnitName}: {Name} hit {targets[0].UnitName}");
    }
}

/// <summary>
/// Enemy Assassin's special: a poison-laced strike (placeholder using BurningEffect).
/// Replace with PoisonEffect when implemented.
/// </summary>
public sealed class EnemyPoisonStrike : SkillSystem
{
	public EnemyPoisonStrike()
	{
		Name = "Venomous Fang";
		Description = "Deal 80% ATK and apply poison (DOT) for 3 turns.";
		ManaCost = 15;
		TotalCooldown = 3;
		Cooldown = 0;
		Range = 1;
		MagicType = AovDataStructures.MagicType.None; //TODO change to Poison when implemented
		EffectType = AovDataStructures.EffectType.Debuff;
		TargetType = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0) return;
		targets[0].TakeDamage(caster.TotalAtk * 0.8f);
		// Placeholder: replace with PoisonEffect when implemented
		targets[0].SetStatusEffectOnUnit(new BurningEffect(3, AovDataStructures.ModifierType.Flat, 10));
		Console.WriteLine($"{caster.UnitName}: {Name} poisoned {targets[0].UnitName}");
	}
}

/// <summary>
/// Enemy Assassin – Fast, fragile enemy that poisons and deals burst damage.
/// Mirrors <see cref="AssassinData"/> in role and stat profile.
/// </summary>
public sealed partial class EnemyAssassin : UnitSystem
{
	protected override void Initialize()
	{
		UnitName = "Enemy Assassin";
		Description = "A swift enemy rogue who strikes fast and poisons their prey.";
		Type = AovDataStructures.UnitType.Assassin;
		MaxHp = 500;
		Hp = MaxHp;
		BaseAtk = 230;
		BaseDef = 10;
		BaseSpeed = 190;
		Intelligence = 50;
		ManaMax = 120;
		Mana = ManaMax;
		IsAlive = true;
		PossibleMovesRange = 4;
		Curse = 0;
		Personality = AIPersonality.Opportunistic;

		ActiveSkills.Add(new AssassinStab());
		ActiveSkills.Add(new EnemyPoisonStrike());

		Console.WriteLine($"{UnitName} initialized with {ActiveSkills.Count} skills");

		base.Initialize();

		var statusEffectSystem = new StatusEffectSystem();
		InjectDependencies(statusEffectSystem);
	}
}
