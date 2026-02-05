using System.Linq;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.AI;

/// <summary>
/// Evaluates and scores AI actions and targets.
/// </summary>
public class AIEvaluator
{
	private readonly UnitSystem _unit;

	public AIEvaluator(UnitSystem unit)
	{
		_unit = unit;
	}

	#region Action Evaluation

    /// <summary>
	/// Evaluates the value of an offensive action.
	/// </summary>
	/// <param name="target">The target being attacked.</param>
	/// <param name="skill">The skill being used.</param>
	/// <param name="attackerPos">Position attacker will be in when using skill.</param>
	/// <param name="targetPos">Position of the target.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <param name="requiresMovement">Whether this action requires movement first.</param>
	/// <returns>Score representing the value of this action.</returns>
	public float EvaluateOffensiveAction(
		UnitSystem target, 
		SkillSystem skill, 
		Vector3I attackerPos, 
		Vector3I targetPos, 
		BattleState battleState,
		bool requiresMovement)
	{
		float score = 0f;

		// Base score from skill against this target
		score += ScoreSkill(skill, target, battleState);

		// Target priority multiplier
		float targetValue = ScoreTarget(target, battleState);
		score += targetValue * 0.5f;

		// Bonus for potentially killing the target
		if (AIUtilities.CanKillThisTurn(_unit, target))
			score += 100f;

		// Position evaluation
		float positionScore = ScorePosition(attackerPos, targetPos, skill.Range, battleState);
		score += positionScore * 0.3f;

		// Movement penalty
		if (requiresMovement)
			score -= 5f;

		// Danger assessment
		int threatsNearNewPosition = AIUtilities.CountPlayerUnitsNear(_unit, attackerPos, battleState, 3);

		if (threatsNearNewPosition >= 2)
			score -= threatsNearNewPosition * 10f;

		return score;
	}

    /// <summary>
	/// Evaluates the value of a support action.
	/// </summary>
	/// <param name="ally">The ally being supported.</param>
	/// <param name="skill">The skill being used.</param>
	/// <param name="casterPos">Position caster will be in when using skill.</param>
	/// <param name="targetPos">Position of the ally.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <param name="requiresMovement">Whether this action requires movement first.</param>
	/// <returns>Score representing the value of this action.</returns>
	public float EvaluateSupportAction(
		UnitSystem ally, 
		SkillSystem skill, 
		Vector3I casterPos,
		Vector3I targetPos, 
		BattleState battleState,
		bool requiresMovement)
	{
		float score = 0f;

		// Base score from skill type
		score += ScoreSkill(skill, ally, battleState);

		// Urgency multiplier based on ally's health
		float hpPercentage = ally.Hp / ally.MaxHp;
		
		if (skill.EffectType == EffectType.Heal)
		{
			if (hpPercentage < 0.2f)
				score += 150f;
			else if (hpPercentage < 0.4f)
				score += 80f;
			else if (hpPercentage < 0.7f)
				score += 30f;
			else
				score -= 20f;
		}

		// Ally value
		score += ally.BaseAtk * 0.5f;

		// Position evaluation
		float positionScore = ScorePosition(casterPos, targetPos, skill.Range, battleState);
		score += positionScore * 0.3f;

		// Movement penalty
		if (requiresMovement)
			score -= 5f;

		// Personality modifier
		if (_unit.Personality == AIPersonality.Defensive)
			score *= 1.3f;
		else if (_unit.Personality == AIPersonality.Aggressive)
			score *= 0.7f;

		return score;
	}

    /// <summary>
	/// Evaluates the value of a defensive/retreat action.
	/// </summary>
	/// <param name="currentPos">Current position.</param>
	/// <param name="newPos">Position to retreat to.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>Score representing the value of this action.</returns>
	public float EvaluateDefensiveAction(Vector3I currentPos, Vector3I newPos, BattleState battleState)
	{
		float score = 50f;
		float hpPercentage = _unit.Hp / _unit.MaxHp;

		// More valuable when low on HP
		if (hpPercentage < 0.3f)
			score += 100f;
		else if (hpPercentage < 0.5f)
			score += 50f;

		// Compare threat levels
		int currentThreats = AIUtilities.CountPlayerUnitsNear(_unit, currentPos, battleState, 3);
		int newThreats = AIUtilities.CountPlayerUnitsNear(_unit, newPos, battleState, 3);
		score += (currentThreats - newThreats) * 25f;

		// Prefer positions near allies
		int alliesNearby = AIUtilities.CountEnemyAlliesNear(_unit, newPos, battleState, 2);
		score += alliesNearby * 15f;

		// Personality modifier
		if (_unit.Personality == AIPersonality.Defensive)
			score *= 1.5f;
		else if (_unit.Personality == AIPersonality.Aggressive)
			score *= 0.5f;

		return score;
	}

	#endregion

	#region Target Scoring

    /// <summary>
	/// Scores a potential target based on distance, health, and threat level.
	/// Higher score means more desirable to target.
	/// </summary>
	/// <param name="target">The target unit to score.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>A float score representing the desirability of targeting this unit.</returns>
	private float ScoreTarget(UnitSystem target, BattleState battleState)
	{
		Vector3I? myPos = battleState.MapSystem.GetUnitPosition(_unit);
		Vector3I? targetPos = battleState.MapSystem.GetUnitPosition(target);
		
		if (myPos == null || targetPos == null)
			return float.MinValue;

		float score = 0f;
		int distance = AIUtilities.CalculateManhattanDistance(myPos.Value, targetPos.Value);

		// Personality-based scoring
		switch (_unit.Personality)
		{
			case AIPersonality.Aggressive:
				score += (10 - distance) * 5f;
				score += target.BaseAtk * 2f;
				break;
				
			case AIPersonality.Opportunistic:
				float hpPercentage = target.Hp / target.MaxHp;
				score += (1f - hpPercentage) * 100f;
				if (AIUtilities.CanKillThisTurn(_unit, target))
					score += 50f;
				break;
				
			case AIPersonality.Defensive:
				if (distance <= 3)
					score += 40f;
				score += target.BaseAtk * 3f;
				break;
				
			case AIPersonality.Balanced:
				score += (1f - target.Hp / target.MaxHp) * 30f;
				score += (10 - distance) * 2f;
				score += target.BaseAtk * 1.5f;
				break;
		}

        // General factors
		// Prefer targets with status effects we can exploit
		// TODO: Add when status effect system is more developed

		// Prefer isolated targets
		int alliesNearTarget = AIUtilities.CountPlayerUnitsNear(_unit, targetPos.Value, battleState, 2);
		score -= alliesNearTarget * 10f;

		return score;
	}

	#endregion

	#region Skill Scoring

    /// <summary>
	/// Scores a skill based on its effectiveness against the target and current battle state.
	/// Higher score means more desirable to use.
	/// </summary>
	/// <param name="skill">The skill to score.</param>
	/// <param name="target">The target unit.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>A float score representing the desirability of using the skill.</returns>
	private float ScoreSkill(SkillSystem skill, UnitSystem target, BattleState battleState)
	{
		float score = 0f;

		// Base score by effect type
		switch (skill.EffectType)
		{
			case EffectType.Damage:
				score += ScoreDamageSkill(skill, target);
				break;
			case EffectType.Heal:
				score += ScoreHealSkill(skill, battleState);
				break;
			case EffectType.Buff:
				score += ScoreBuffSkill(skill, battleState);
				break;
			case EffectType.Debuff:
			case EffectType.Control:
				score += ScoreDebuffSkill(skill, target);
				break;
		}

		// Personality modifiers
		score *= GetPersonalitySkillMultiplier(skill);

		// Prefer lower mana cost
		score -= skill.ManaCost * 0.1f;

		// Bonus for AOE
		if (skill.AreaEffect.Count > 0)
		{
			int targetsInAOE = CountTargetsInAOE(skill, target, battleState);
			score += targetsInAOE * 10f;
		}

		return score;
	}

    /// <summary>
	/// Scores a damage-dealing skill based on target's health and resistances.
	/// </summary>
	/// <param name="skill">The damage skill.</param>
	/// <param name="target">The target unit.</param>
	/// <returns>A float score representing the desirability of using the damage skill.</returns>
	private float ScoreDamageSkill(SkillSystem skill, UnitSystem target)
	{
		float score = 50f;
		float hpPercentage = target.Hp / target.MaxHp;
		
		if (hpPercentage < 0.3f)
			score += 30f;

        // TODO: Add elemental effectiveness when we implement resistances
		// if (target.IsWeakTo(skill.MagicType))
		//     score += 25f;
		
		return score;
	}

    /// <summary>
	/// Scores a healing skill based on the most damaged ally's health.
	/// </summary>
	/// <param name="skill">The healing skill.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>A float score representing the desirability of using the healing skill.</returns>
	private float ScoreHealSkill(SkillSystem skill, BattleState battleState)
	{
		var mostDamagedAlly = battleState.EnemyUnits
			.OrderBy(u => u.Hp / u.MaxHp)
			.FirstOrDefault();
		
		if (mostDamagedAlly == null)
			return 0f;
		
		float hpPercentage = mostDamagedAlly.Hp / mostDamagedAlly.MaxHp;
		
		if (hpPercentage < 0.3f)
			return 80f;
		if (hpPercentage < 0.6f)
			return 40f;
		
		return 10f;
	}

    /// <summary>
	/// Scores a buff skill based on current battle state.
	/// </summary>
	/// <param name="skill">The buff skill.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>A float score representing the desirability of using the buff skill.</returns>
	private float ScoreBuffSkill(SkillSystem skill, BattleState battleState)
	{
        // Buffs are more valuable early in combat
		// TODO: Track turn count in BattleState
		return 30f;
	}

    /// <summary>
	/// Scores a debuff or control skill based on target threat level.
	/// </summary>
	/// <param name="skill">The debuff/control skill.</param>
	/// <param name="target">The target unit.</param>
	/// <returns>A float score representing the desirability of using the debuff/control skill.</returns>
	private float ScoreDebuffSkill(SkillSystem skill, UnitSystem target)
	{
		float score = 40f;
		
		if (target.BaseAtk > _unit.BaseAtk * 1.5f)
			score += 20f;
		
		if (skill.EffectType == EffectType.Control && target.Hp > target.MaxHp * 0.7f)
			score += 15f;
		
		return score;
	}

    /// <summary>
	/// Gets a personality-based multiplier for skill scoring.
	/// </summary>
	/// <param name="skill">The skill being evaluated.</param>
	/// <returns>A float multiplier to adjust skill score.</returns>
	private float GetPersonalitySkillMultiplier(SkillSystem skill)
	{
		return _unit.Personality switch
		{
			AIPersonality.Aggressive => skill.EffectType == EffectType.Damage ? 1.3f : 0.8f,
			AIPersonality.Defensive => skill.EffectType == EffectType.Heal ? 1.3f : 
									skill.EffectType == EffectType.Buff ? 1.2f : 0.9f,
			AIPersonality.Opportunistic => skill.EffectType == EffectType.Damage ? 1.2f : 1.0f,
			AIPersonality.Balanced => 1.0f,
			_ => 1.0f
		};
	}

    /// <summary>
	/// Counts how many units would be affected by the skill's area of effect.
	/// </summary>
	/// <param name="skill">The skill being evaluated.</param>
	/// <param name="primaryTarget">The primary target unit.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>The number of units affected by the skill's AOE.</returns>
	private int CountTargetsInAOE(SkillSystem skill, UnitSystem primaryTarget, BattleState battleState)
	{
		Vector3I? targetPos = battleState.MapSystem.GetUnitPosition(primaryTarget);
		if (targetPos == null) return 0;
		
		int count = 0;
		var targetList = skill.TargetType == TargetTypes.AllEnemies || skill.TargetType == TargetTypes.SingleEnemy
			? battleState.PlayerUnits 
			: battleState.EnemyUnits;
		
		foreach (var aoeOffset in skill.AreaEffect)
		{
			Vector3I checkPos = targetPos.Value + aoeOffset;
			var unitAtPos = battleState.MapSystem.GetUnitAt(checkPos.X, checkPos.Y, checkPos.Z);
			
			if (unitAtPos != null && targetList.Contains(unitAtPos))
				count++;
		}
		
		return count;
	}

	#endregion

	#region Position Scoring

    /// <summary>
    /// Scores a potential position based on distance to target, terrain, and tactical considerations.
    /// </summary>
    /// <param name="position">The position to score.</param>
    /// <param name="targetPos">The target's grid position.</param>
    /// <param name="skillRange">The range of the skill to get within.</param>
    /// <param name="battleState">Current battle state.</param>
    /// <returns>The score for the given position.</returns>
	private float ScorePosition(Vector3I position, Vector3I targetPos, int skillRange, BattleState battleState)
	{
		float score = 0f;
		int distance = AIUtilities.CalculateManhattanDistance(position, targetPos);

		// Prefer positions that put us in attack range
		if (skillRange > 0)
		{
			if (distance == skillRange)
				score += 100f;
			else if (distance < skillRange)
				score += 50f - (skillRange - distance) * 5f;
			else
				score += 20f - (distance - skillRange) * 10f;
		}
		else
			score += (20 - distance) * 5f;

		// Terrain bonuses
		CellType terrain = battleState.MapSystem.GetCellType(position);
		switch (terrain)
		{
			case CellType.Grass:
				score += 10f;
				break;
		}

		// Tactical positioning
		if (_unit.Personality == AIPersonality.Defensive || _unit.Personality == AIPersonality.Balanced)
		{
			int alliesNearby = AIUtilities.CountEnemyAlliesNear(_unit, position, battleState, 2);
			score += alliesNearby * 5f;
		}

		// Avoid being surrounded
		int enemiesNearby = AIUtilities.CountPlayerUnitsNear(_unit, position, battleState, 2);
		if (enemiesNearby > 2)
			score -= (enemiesNearby - 1) * 15f;

		// Prefer higher ground
		score += position.Y * 2f;

		return score;
	}

	#endregion
}