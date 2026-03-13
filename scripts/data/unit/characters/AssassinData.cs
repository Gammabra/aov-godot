using System.Collections.Generic;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
/// Critical Strike – Deals 200% ATK to a single enemy. 3-turn cooldown.
/// </summary>
public sealed class CriticalStrike : SkillSystem
{
	public CriticalStrike()
	{
		Name = "Critical Strike";
		Description = "Deal 200% ATK damage to one enemy.";
		ManaCost = 30;
		TotalCooldown = 3;
		Cooldown = 0;
		Range = 1;
		MagicType = AovDataStructures.MagicType.None;
		EffectType = AovDataStructures.EffectType.Damage;
		TargetType = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0)
			return;

		targets[0].TakeDamage(caster.TotalAtk * 2.0f);
		GD.Print($"{caster.UnitName}: {Name} – double strike on {targets[0].UnitName}");
	}
}

/// <summary>
/// Instant Kill – Instantly defeats a target below 15% HP.
/// Deals normal ATK damage otherwise.
/// </summary>
public sealed class InstantKill : SkillSystem
{
	public InstantKill()
	{
		Name = "Instant Kill";
		Description = "Instantly defeat an enemy below 15% HP. Otherwise deal 100% ATK.";
		ManaCost = 40;
		TotalCooldown = 4;
		Cooldown = 0;
		Range = 1;
		MagicType = AovDataStructures.MagicType.None;
		EffectType = AovDataStructures.EffectType.Damage;
		TargetType = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0)
			return;

		UnitSystem target = targets[0];
		bool belowThreshold = target.Hp / target.MaxHp <= 0.15f;

		if (belowThreshold)
		{
			target.BypassDamage(target.Hp);
			GD.Print($"{caster.UnitName}: {Name} – instantly defeated {target.UnitName}!");
		}
		else
		{
			target.TakeDamage(caster.TotalAtk);
		}
	}
}

/// <summary>
/// Shadow Strike – Attacks at range 2 from stealth (raw damage, no DEF reduction).
/// </summary>
public sealed class ShadowStrike : SkillSystem
{
	public ShadowStrike()
	{
		Name = "Shadow Strike";
		Description = "Strike from the shadows at range 2 for 150% ATK, ignoring DEF.";
		ManaCost = 25;
		TotalCooldown = 2;
		Cooldown = 0;
		Range = 2;
		MagicType = AovDataStructures.MagicType.Dark;
		EffectType = AovDataStructures.EffectType.Damage;
		TargetType = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0)
			return;

		float rawDamage = caster.TotalAtk * 1.5f;

		targets[0].BypassDamage(rawDamage);
		GD.Print($"{caster.UnitName}: {Name} – {rawDamage} raw damage on {targets[0].UnitName}");
	}
}

/// <summary>
/// Blood Drain – Drains life from the target: deals ATK damage and heals caster for 30%.
/// </summary>
public sealed class BloodDrain : SkillSystem
{
	public BloodDrain()
	{
		Name = "Blood Drain";
		Description = "Deal ATK damage and heal caster for 30% of damage dealt.";
		ManaCost = 20;
		TotalCooldown = 2;
		Cooldown = 0;
		Range = 1;
		MagicType = AovDataStructures.MagicType.Dark;
		EffectType = AovDataStructures.EffectType.Damage;
		TargetType = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0) return;
		float damage = caster.TotalAtk;
		targets[0].TakeDamage(damage);
		float heal = damage * 0.3f;
		caster.OnEffectHeal(heal);
		GD.Print($"{caster.UnitName}: {Name} – drained {heal} HP from {targets[0].UnitName}");
	}
}

/// <summary>
/// Poison Blade – Deals light damage and applies BurningEffect as a stand-in for Poison
/// (to be replaced with PoisonEffect once implemented).
/// </summary>
public sealed class PoisonBlade : SkillSystem
{
	public PoisonBlade()
	{
		Name = "Poison Blade";
		Description = "Deal 80% ATK and apply poison (DOT) for 3 turns.";
		ManaCost = 15;
		TotalCooldown = 2;
		Cooldown = 0;
		Range = 1;
		MagicType = AovDataStructures.MagicType.None; // TODO: change to Poison when implemented
		EffectType = AovDataStructures.EffectType.Debuff;
		TargetType = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0) return;
		targets[0].TakeDamage(caster.TotalAtk * 0.8f);
		// Placeholder: replace BurningEffect with PoisonEffect when available
		targets[0].SetStatusEffectOnUnit(new BurningEffect(3, AovDataStructures.ModifierType.Flat, 12));
		GD.Print($"{caster.UnitName}: {Name} – poisoned {targets[0].UnitName}");
	}
}

/// <summary>
/// Assassin – Glass cannon unit. Extremely high ATK with low HP and DEF.
/// Specialises in burst damage, execution, and life steal.
/// </summary>
public sealed partial class AssassinData : UnitSystem
{
	protected override void Initialize()
	{
		UnitName = "Assassin";
		Description = "A deadly shadow operative who eliminates targets before they can react.";
		Type = AovDataStructures.UnitType.Assassin;
		MaxHp = 600;
		Hp = MaxHp;
		BaseAtk = 280;
		BaseDef = 15;
		BaseSpeed = 200;
		Intelligence = 80;
		ManaMax = 180;
		Mana = ManaMax;
		IsAlive = true;
		PossibleMovesRange = 4;
		Curse = 0;

		ActiveSkills.Add(new CriticalStrike());
		ActiveSkills.Add(new InstantKill());
		ActiveSkills.Add(new ShadowStrike());
		ActiveSkills.Add(new BloodDrain());
		ActiveSkills.Add(new PoisonBlade());

		base.Initialize();

		// Create and inject StatusEffectSystem so buffs/heals work
		var statusEffectSystem = new StatusEffectSystem();
		InjectDependencies(statusEffectSystem);
	}
}
