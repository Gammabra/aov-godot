using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
/// Blade Dance – Attacks up to 3 adjacent enemies in sequence.
/// </summary>
public sealed class BladeDance : SkillSystem
{
	public BladeDance()
	{
		Name        = "Blade Dance";
		Description = "Strike up to 3 adjacent enemies for 80% ATK each.";
		ManaCost    = 20;
		TotalCooldown = 2;
		Cooldown    = 0;
		Range       = 1;
		MagicType   = AovDataStructures.MagicType.None;
		EffectType  = AovDataStructures.EffectType.Damage;
		TargetType  = AovDataStructures.TargetTypes.AllEnemies;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		// Cap at first 3 targets
		int count = Math.Min(targets.Count, 3);
		for (int i = 0; i < count; i++)
		{
			targets[i].TakeDamage(caster.TotalAtk * 0.8f);
			GD.Print($"{caster.UnitName}: {Name} hit {targets[i].UnitName}");
		}
	}
}

/// <summary>
/// Phantom Strike – Ranged sword slash that ignores DEF partially.
/// Deals 120% ATK at range 2 with a DEF penetration flavour (implemented as raw damage).
/// </summary>
public sealed class PhantomStrike : SkillSystem
{
	public PhantomStrike()
	{
		Name        = "Phantom Strike";
		Description = "Strike an enemy at range 2 for 120% ATK, bypassing part of their defense.";
		ManaCost    = 25;
		TotalCooldown = 3;
		Cooldown    = 0;
		Range       = 2;
		MagicType   = AovDataStructures.MagicType.None;
		EffectType  = AovDataStructures.EffectType.Damage;
		TargetType  = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0)
			return;
		// Raw damage to simulate armor bypass
		float rawDamage = caster.TotalAtk * 1.2f;
		targets[0].BypassDamage(rawDamage); // Bypass TakeDamage to ignore DEF

		GD.Print($"{caster.UnitName}: {Name} penetrated armor for {rawDamage} on {targets[0].UnitName}");
	}
}

/// <summary>
/// Execution Blade – Deals 200% ATK to a target below 30% HP.
/// No bonus if the target is above the threshold.
/// </summary>
public sealed class ExecutionBlade : SkillSystem
{
	public ExecutionBlade()
	{
		Name        = "Execution Blade";
		Description = "Deal 200% ATK damage to a target below 30% HP, 100% otherwise.";
		ManaCost    = 30;
		TotalCooldown = 3;
		Cooldown    = 0;
		Range       = 1;
		MagicType   = AovDataStructures.MagicType.None;
		EffectType  = AovDataStructures.EffectType.Damage;
		TargetType  = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0) return;
		UnitSystem target = targets[0];
		bool isLowHp = target.Hp / target.MaxHp < 0.3f;
		float multiplier = isLowHp ? 2.0f : 1.0f;
		target.TakeDamage(caster.TotalAtk * multiplier);
		GD.Print($"{caster.UnitName}: {Name} on {target.UnitName} (low HP: {isLowHp}, x{multiplier})");
	}
}

/// <summary>
/// Counter Stance – Boosts own DEF and ATK for 2 turns (self-buff).
/// </summary>
public sealed class CounterStance : SkillSystem
{
	public CounterStance()
	{
		Name        = "Counter Stance";
		Description = "Boost own ATK by 40 (flat) for 2 turns.";
		ManaCost    = 15;
		TotalCooldown = 3;
		Cooldown    = 0;
		Range       = 0;
		MagicType   = AovDataStructures.MagicType.None;
		EffectType  = AovDataStructures.EffectType.Buff;
		TargetType  = AovDataStructures.TargetTypes.SingleAlly;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0) return;
		targets[0].SetStatusEffectOnUnit(new AtkBuffer(2, AovDataStructures.ModifierType.Flat, 40));
		GD.Print($"{caster.UnitName}: {Name} – ATK buffed for {targets[0].UnitName}");
	}
}

/// <summary>
/// Burning Slash – Slashes the enemy and applies Burning for 2 turns.
/// </summary>
public sealed class BurningSlash : SkillSystem
{
	public BurningSlash()
	{
		Name        = "Burning Slash";
		Description = "Deal ATK damage and apply Burning for 2 turns.";
		ManaCost    = 20;
		TotalCooldown = 2;
		Cooldown    = 0;
		Range       = 1;
		MagicType   = AovDataStructures.MagicType.Fire;
		EffectType  = AovDataStructures.EffectType.Damage;
		TargetType  = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0) return;
		targets[0].TakeDamage(caster.TotalAtk);
		targets[0].SetStatusEffectOnUnit(new BurningEffect(2, AovDataStructures.ModifierType.Flat, 15));
		GD.Print($"{caster.UnitName}: {Name} burned {targets[0].UnitName}");
	}
}

/// <summary>
/// Swordsman – Balanced melee unit with solid offense and versatile skills.
/// </summary>
public sealed partial class SwordsmanData : UnitSystem
{
	protected override void Initialize()
	{
		base.Initialize();

		UnitName    = "Swordsman";
		Description = "A versatile blade fighter who adapts to any situation on the battlefield.";
		Type        = AovDataStructures.UnitType.Swordsman;
		MaxHp       = 900;
		Hp          = MaxHp;
		BaseAtk     = 200;
		BaseDef     = 35;
		BaseSpeed   = 130;
		Intelligence = 60;
		ManaMax     = 150;
		Mana        = ManaMax;
		IsAlive     = true;
		PossibleMovesRange = 3;
		Curse       = 0;

		ActiveSkills.Add(new BladeDance());
		ActiveSkills.Add(new PhantomStrike());
		ActiveSkills.Add(new ExecutionBlade());
		ActiveSkills.Add(new CounterStance());
		ActiveSkills.Add(new BurningSlash());
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
