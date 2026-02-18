using System.Collections.Generic;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
/// Enemy Fighter's melee basic attack.
/// </summary>
public sealed class FighterMeleeAttack : SkillSystem
{
	public FighterMeleeAttack()
	{
		Name        = "Heavy Swing";
		Description = "Slam the target with a heavy blow for 100% ATK.";
		ManaCost    = 0;
		TotalCooldown = 0;
		Cooldown    = 0;
		Range       = 1;
		MagicType   = AovDataStructures.MagicType.None;
		EffectType  = AovDataStructures.EffectType.Damage;
		TargetType  = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0) return;
		targets[0].TakeDamage(caster.TotalAtk);
		GD.Print($"{caster.UnitName}: {Name} hit {targets[0].UnitName} for {caster.TotalAtk}");
	}
}

/// <summary>
/// Enemy Fighter's special: a stun blow that also deals damage.
/// </summary>
public sealed class EnemyStaggeringBlow : SkillSystem
{
	public EnemyStaggeringBlow()
	{
		Name        = "Iron Bash";
		Description = "Deal 90% ATK damage and stun the target for 1 turn.";
		ManaCost    = 20;
		TotalCooldown = 3;
		Cooldown    = 0;
		Range       = 1;
		MagicType   = AovDataStructures.MagicType.None;
		EffectType  = AovDataStructures.EffectType.Control;
		TargetType  = AovDataStructures.TargetTypes.SingleEnemy;
	}

	public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0) return;
		targets[0].TakeDamage(caster.TotalAtk * 0.9f);
		targets[0].SetStatusEffectOnUnit(new Stun(1));
		GD.Print($"{caster.UnitName}: {Name} stunned {targets[0].UnitName}");
	}
}

/// <summary>
/// Enemy Fighter – Melee tank enemy with high HP, DEF, and a stun special.
/// Mirrors <see cref="FighterData"/> in role and stat profile.
/// </summary>
public sealed partial class EnemyFighter : UnitSystem
{
	protected override void Initialize()
	{
		base.Initialize();

		UnitName    = "Enemy Fighter";
		Description = "A heavily armoured enemy bruiser who crushes and stuns opponents.";
		Type        = AovDataStructures.UnitType.Fighter;
		MaxHp       = 1000;
		Hp          = MaxHp;
		BaseAtk     = 150;
		BaseDef     = 50;
		BaseSpeed   = 70;
		Intelligence = 20;
		ManaMax     = 80;
		Mana        = ManaMax;
		IsAlive     = true;
		PossibleMovesRange = 2;
		Curse       = 0;
		Personality = AIPersonality.Aggressive;

		ActiveSkills.Add(new FighterMeleeAttack());
		ActiveSkills.Add(new EnemyStaggeringBlow());

		GD.Print($"{UnitName} initialized with {ActiveSkills.Count} skills");
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
