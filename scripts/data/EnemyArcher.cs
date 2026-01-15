using AshesOfVelsingrad.Systems;
using Godot;
using System.Collections.Generic;

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
		ManaPoint = 50;
		IsAlive = true;
		PossibleMovesRange = 3;
		Curse = 0;
		Type = UnitType.Archer;

		// Initialize skills
		_basicAttack = new BasicAttackSkill();
		ActiveSkills = new List<SkillSystem> { _basicAttack };

		GD.Print($"EnemyArcher {UnitName} initialized with {ActiveSkills.Count} skills");
	}

	public override void TakeDamage(float damage)
	{
		float realDamage = damage - BaseDef;
		if (realDamage < 0) realDamage = 0;
		
		Hp -= realDamage;
		GD.Print($"{UnitName} took {realDamage} damage, HP: {Hp}/{MaxHp}");

		if (Hp <= 0)
		{
			IsAlive = false;
			GD.Print($"{UnitName} has been defeated!");
		}
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
		AreaEffect = new List<Vector3I> { new Vector3I(0, 0, 0) }; // Single target
		MagicType = MagicType.None;
		EffectType = EffectType.Damage;
		TargetType = TargetTypes.SingleEnemy;
	}

	public override void Use(List<UnitSystem> targets, MapSystem? map)
	{
		if (targets.Count == 0) return;

		foreach (UnitSystem target in targets)
		{
			if (target.IsAlive)
			{
				float damage = 25f; // Base damage
				target.TakeDamage(damage);
				GD.Print($"Archer used {Name} on {target.UnitName} for {damage} damage");
			}
		}
	}
}
