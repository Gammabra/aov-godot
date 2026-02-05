using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.AI;

/// <summary>
/// Defines different AI personality types that determine decision-making behavior.
/// </summary>
public enum AIPersonality
{
	/// <summary>Attacks the nearest target, prefers close combat.</summary>
	Aggressive,

	/// <summary>Targets weakest enemies, tries to finish off low HP units.</summary>
	Opportunistic,

	/// <summary>Maintains distance, avoids direct combat when possible.</summary>
	Defensive,

	/// <summary>Balances offense and positioning, adapts to situation.</summary>
	Balanced
}

/// <summary>
/// Represents the types of actions an AI can take.
/// </summary>
public enum AIAction
{
	/// <summary>Move to a new position then use a skill.</summary>
	MoveAndSkill,

	/// <summary>Move to a new position only.</summary>
	Move,

	/// <summary>Use a skill or ability without moving.</summary>
	UseSkill,

	/// <summary>Do nothing and end turn.</summary>
	Pass
}

/// <summary>
/// Represents a decision made by the AI, including the action and relevant parameters.
/// </summary>
public class AIDecision
{
	/// <summary>The action to perform.</summary>
	public AIAction Action { get; set; }

	/// <summary>The target unit (for attacks or skills).</summary>
	public UnitSystem? Target { get; set; }

	/// <summary>The position to move to (for movement).</summary>
	public Vector3I? MovePosition { get; set; }

	/// <summary>The skill to use (for skill actions).</summary>
	public SkillSystem? Skill { get; set; }

	/// <summary>The evaluation score for this decision. Higher is better.</summary>
	public float Score { get; set; }

	/// <summary>Debug description of why this decision was scored this way.</summary>
	public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// Base class for enemy AI behaviors. Attach this as a child node to enemy UnitSystem nodes.
/// Encapsulates decision-making logic for a single enemy unit.
/// </summary>
/// <remarks>
/// This component pattern allows each enemy to have customized AI behavior
/// without modifying the core UnitSystem class. The AI makes decisions based
/// on the current BattleState and executes actions through the GameManager.
/// </remarks>
public partial class EnemyAIBehavior : Node
{
	#region Godot Export Fields

	[Export]
	public float ThinkingDelayMin { get; set; } = 0.5f;

	[Export]
	public float ThinkingDelayMax { get; set; } = 2.0f;

	#endregion

	#region Public Fields

	public UnitSystem? _unit;

	#endregion

	#region Public Properties

	/// <summary>
	/// Read-only access to the UnitSystem this AI is attached to.
	/// Useful for external systems that need to query the AI's unit.
	/// </summary>
	public UnitSystem? Unit => _unit;

	#endregion

	#region Class Initialization

	public override void _Ready()
	{
		// Get the parent UnitSystem
		if (GetParent() is UnitSystem unit)
		{
			_unit = unit;
			GD.Print($"EnemyAIBehavior attached to {_unit.Name} (Personality: {_unit.Personality})");
		}
		else
		{
			GD.PrintErr("EnemyAIBehavior must be a child of a UnitSystem node");
		}
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Executes the AI's turn logic based on the current battle state.
	/// </summary>
	/// <param name="battleState">Snapshot of the current battle situation.</param>
	/// <returns>A task that completes when the AI has finished its turn.</returns>
	public async Task ExecuteTurn(BattleState battleState)
	{
		if (_unit == null)
		{
			GD.PrintErr("EnemyAIBehavior: Unit reference is null");
			return;
		}

		// Simulate thinking time for more natural AI behavior
		float thinkingTime = (float)GD.RandRange(ThinkingDelayMin, ThinkingDelayMax);
		await Task.Delay((int)(thinkingTime * 1000));

		// Decide on action based on personality
		AIDecision decision = MakeDecision(battleState);

		// Execute the decision
		await ExecuteDecision(decision, battleState);
	}

	#endregion

	#region Private Methods - Decision Making

	/// <summary>
	/// Main decision-making method that determines what action the AI should take.
	/// Evaluates multiple possible actions and picks the best one.
	/// </summary>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>An AI decision containing the chosen action and target.</returns>
	public AIDecision MakeDecision(BattleState battleState)
	{
		if (_unit == null)
		{
			GD.PrintErr("EnemyAIBehavior: Unit reference is null during decision making");
			return new AIDecision { Action = AIAction.Pass };
		}

		// Generate all possible actions
		List<AIDecision> possibleActions = GenerateAllPossibleActions(battleState);

		if (possibleActions.Count == 0)
		{
			GD.Print($"{_unit.Name}: No valid actions available, passing turn");
			return new AIDecision { Action = AIAction.Pass };
		}

		// Pick the highest-scoring action
		AIDecision bestDecision = possibleActions.OrderByDescending(a => a.Score).First();

		GD.Print($"{_unit.Name}: Chose {bestDecision.Action} (Score: {bestDecision.Score:F1}) - {bestDecision.Reasoning}");

		return bestDecision;
	}
	
	/// <summary>
	/// Generates all possible actions the AI could take this turn.
	/// </summary>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>A list of all valid actions with their scores.</returns>
	private List<AIDecision> GenerateAllPossibleActions(BattleState battleState)
	{
		List<AIDecision> actions = new();

		if (_unit == null)
			return actions;

		Vector3I? myPos = battleState.MapSystem.GetUnitPosition(_unit);
		if (myPos == null)
			return actions;

		// 1. Evaluate offensive actions against each enemy
		foreach (var target in battleState.PlayerUnits)
		{
			actions.AddRange(GenerateOffensiveActions(target, myPos.Value, battleState));
		}

		// 2. Evaluate support actions for allies (healing, buffs)
		foreach (var ally in battleState.EnemyUnits)
		{
			actions.AddRange(GenerateSupportActions(ally, myPos.Value, battleState));
		}

		// 3. Evaluate defensive/positioning actions
		actions.AddRange(GenerateDefensiveActions(myPos.Value, battleState));

		// 4. Always include "pass turn" as a fallback option
		actions.Add(new AIDecision
		{
			Action = AIAction.Pass,
			Score = 0f, // Passing is always worth 0 - only chosen if nothing better
			Reasoning = "No better options available"
		});

		// Debug: Print top 5 options for transparency
		foreach (var action in actions.OrderByDescending(a => a.Score).Take(5))
			GD.Print($"  Option: {action.Reasoning} (Score: {action.Score:F1})");

		return actions;
	}

	/// <summary>
	/// Generates all possible offensive actions against a specific target.
	/// </summary>
	/// <param name="target">The enemy target.</param>
	/// <param name="myPos">The AI unit's current position.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>List of possible offensive actions.</returns>
	private List<AIDecision> GenerateOffensiveActions(UnitSystem target, Vector3I myPos, BattleState battleState)
	{
		List<AIDecision> actions = new();
		Vector3I? targetPos = battleState.MapSystem.GetUnitPosition(target);
		
		if (targetPos == null)
			return actions;

		int distance = CalculateManhattanDistance(myPos, targetPos.Value);

		// Try each offensive skill
		foreach (var skill in _unit!.ActiveSkills)
		{
			// Skip non-offensive skills
			if (skill.EffectType != EffectType.Damage && 
				skill.EffectType != EffectType.Debuff && 
				skill.EffectType != EffectType.Control)
				continue;

			// Skip if can't afford or on cooldown
			if (skill.ManaCost > _unit.Mana || skill.Cooldown != 0)
				continue;

			// Option 1: Use skill without moving (if in range)
			if (distance <= skill.Range)
			{
				float score = EvaluateOffensiveAction(target, skill, myPos, targetPos.Value, battleState, false);
				actions.Add(new AIDecision
				{
					Action = AIAction.UseSkill,
					Target = target,
					Skill = skill,
					Score = score,
					Reasoning = $"Attack {target.UnitName} with {skill.Name} from current position"
				});
			}

			// Option 2: Move closer and use skill
			if (distance <= _unit.PossibleMovesRange + skill.Range)
			{
				Vector3I? movePos = CalculateMoveToRange(battleState, targetPos.Value, skill.Range);
				
				if (movePos.HasValue)
				{
					float score = EvaluateOffensiveAction(target, skill, movePos.Value, targetPos.Value, battleState, true);
					actions.Add(new AIDecision
					{
						Action = AIAction.MoveAndSkill,
						Target = target,
						Skill = skill,
						MovePosition = movePos.Value,
						Score = score,
						Reasoning = $"Move to {movePos.Value}, then attack {target.UnitName} with {skill.Name}"
					});
				}
			}
		}

		return actions;
	}

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
	private float EvaluateOffensiveAction(
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
		score += targetValue * 0.5f; // Add 50% of target value to skill score

		// Bonus for potentially killing the target
		if (CanKillThisTurn(target))
		{
			score += 100f; // HUGE bonus for securing a kill
		}

		// Position evaluation
		float positionScore = ScorePosition(attackerPos, targetPos, skill.Range, battleState);
		score += positionScore * 0.3f; // Position matters, but less than the action itself

		// Movement penalty (slight preference for not moving if scores are equal)
		if (requiresMovement)
		{
			score -= 5f;
		}

		// Danger assessment - penalize if we'll be in danger after this action
		int threatsNearNewPosition = CountPlayerUnitsNear(attackerPos, battleState, 3);
		if (threatsNearNewPosition >= 2)
		{
			score -= threatsNearNewPosition * 10f; // Don't walk into danger unless really worth it
		}

		return score;
	}

	/// <summary>
	/// Generates all possible support actions for an ally.
	/// </summary>
	/// <param name="ally">The ally to potentially support.</param>
	/// <param name="myPos">The AI unit's current position.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>List of possible support actions.</returns>
	private List<AIDecision> GenerateSupportActions(UnitSystem ally, Vector3I myPos, BattleState battleState)
	{
		List<AIDecision> actions = new();
		
		// Don't support yourself (handled separately)
		if (ally == _unit)
			return actions;

		Vector3I? allyPos = battleState.MapSystem.GetUnitPosition(ally);
		if (allyPos == null)
			return actions;

		int distance = CalculateManhattanDistance(myPos, allyPos.Value);

		// Try each support skill
		foreach (var skill in _unit!.ActiveSkills)
		{
			// Only consider support skills
			if (skill.EffectType != EffectType.Heal && 
				skill.EffectType != EffectType.Buff &&
				skill.EffectType != EffectType.Revive)
				continue;

			// Skip if can't afford or on cooldown
			if (skill.ManaCost > _unit.Mana || skill.Cooldown != 0)
				continue;

			// Option 1: Use skill without moving (if in range)
			if (distance <= skill.Range)
			{
				float score = EvaluateSupportAction(ally, skill, myPos, allyPos.Value, battleState, false);
				actions.Add(new AIDecision
				{
					Action = AIAction.UseSkill,
					Target = ally,
					Skill = skill,
					Score = score,
					Reasoning = $"Support {ally.UnitName} with {skill.Name} from current position"
				});
			}

			// Option 2: Move closer and use skill
			if (distance <= _unit.PossibleMovesRange + skill.Range)
			{
				Vector3I? movePos = CalculateMoveToRange(battleState, allyPos.Value, skill.Range);
				
				if (movePos.HasValue)
				{
					float score = EvaluateSupportAction(ally, skill, movePos.Value, allyPos.Value, battleState, true);
					actions.Add(new AIDecision
					{
						Action = AIAction.MoveAndSkill,
						Target = ally,
						Skill = skill,
						MovePosition = movePos.Value,
						Score = score,
						Reasoning = $"Move to {movePos.Value}, then support {ally.UnitName} with {skill.Name}"
					});
				}
			}
		}

		return actions;
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
	private float EvaluateSupportAction(
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
			{
				score += 150f; // CRITICAL - ally about to die!
			}
			else if (hpPercentage < 0.4f)
			{
				score += 80f; // Very important
			}
			else if (hpPercentage < 0.7f)
			{
				score += 30f; // Moderate priority
			}
			else
			{
				score -= 20f; // Low priority if mostly healthy
			}
		}

		// Ally value - prefer supporting stronger/more valuable allies
		score += ally.BaseAtk * 0.5f; // Worth more to keep damage dealers alive

		// Position evaluation
		float positionScore = ScorePosition(casterPos, targetPos, skill.Range, battleState);
		score += positionScore * 0.3f; // Position matters, but less than the action itself

		// Movement penalty
		if (requiresMovement)
		{
			score -= 5f;
		}

		// Personality modifier
		if (_unit!.Personality == AIPersonality.Defensive)
		{
			score *= 1.3f; // Defensive personalities value support more
		}
		else if (_unit.Personality == AIPersonality.Aggressive)
		{
			score *= 0.7f; // Aggressive personalities value support less
		}

		return score;
	}

	/// <summary>
	/// Generates defensive/positioning actions like retreating or repositioning.
	/// </summary>
	/// <param name="myPos">The AI unit's current position.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>List of possible defensive actions.</returns>
	private List<AIDecision> GenerateDefensiveActions(Vector3I myPos, BattleState battleState)
	{
		List<AIDecision> actions = new();

		// Only consider defensive moves if we're in danger
		float hpPercentage = _unit!.Hp / _unit.MaxHp;
		int nearbyEnemies = CountPlayerUnitsNear(myPos, battleState, 3);

		// Not in danger - skip defensive actions
		if (hpPercentage > 0.5f && nearbyEnemies <= 1)
			return actions;

		// Find nearest threat to retreat from
		UnitSystem? nearestThreat = FindNearestThreat(battleState);
		if (nearestThreat == null)
			return actions;

		Vector3I? threatPos = battleState.MapSystem.GetUnitPosition(nearestThreat);
		if (threatPos == null)
			return actions;

		// Generate retreat move
		Vector3I? retreatPos = CalculateMoveAway(battleState, threatPos.Value, 2);
		
		if (retreatPos.HasValue)
		{
			float score = EvaluateDefensiveAction(myPos, retreatPos.Value, battleState);
			actions.Add(new AIDecision
			{
				Action = AIAction.Move,
				MovePosition = retreatPos.Value,
				Score = score,
				Reasoning = $"Retreat to {retreatPos.Value} away from threats"
			});
		}

		return actions;
	}

	/// <summary>
	/// Finds the nearest threatening enemy unit.
	/// </summary>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>The nearest threatening unit, or null if none found.</returns>
	private UnitSystem? FindNearestThreat(BattleState battleState)
	{
		if (_unit == null)
			return null;

		Vector3I? myPos = battleState.MapSystem.GetUnitPosition(_unit);
		if (myPos == null)
			return null;

		UnitSystem? nearestThreat = null;
		int minDistance = int.MaxValue;

		foreach (var enemy in battleState.PlayerUnits)
		{
			Vector3I? enemyPos = battleState.MapSystem.GetUnitPosition(enemy);
			if (enemyPos == null) continue;

			int distance = CalculateManhattanDistance(myPos.Value, enemyPos.Value);
			if (distance < minDistance)
			{
				minDistance = distance;
				nearestThreat = enemy;
			}
		}

		return nearestThreat;
	}

	/// <summary>
	/// Evaluates the value of a defensive/retreat action.
	/// </summary>
	/// <param name="currentPos">Current position.</param>
	/// <param name="newPos">Position to retreat to.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>Score representing the value of this action.</returns>
	private float EvaluateDefensiveAction(Vector3I currentPos, Vector3I newPos, BattleState battleState)
	{
		float score = 50f; // Base retreat value

		float hpPercentage = _unit!.Hp / _unit.MaxHp;

		// More valuable when low on HP
		if (hpPercentage < 0.3f)
		{
			score += 100f; // Critical HP - retreating is very important
		}
		else if (hpPercentage < 0.5f)
		{
			score += 50f; // Low HP - retreating is important
		}

		// Compare threat levels at old vs new position
		int currentThreats = CountPlayerUnitsNear(currentPos, battleState, 3);
		int newThreats = CountPlayerUnitsNear(newPos, battleState, 3);
		
		score += (currentThreats - newThreats) * 25f; // Big bonus for reducing nearby threats

		// Prefer positions near allies (safety in numbers)
		int alliesNearby = CountEnemyAlliesNear(newPos, battleState, 2);
		score += alliesNearby * 15f;

		// Personality modifier
		if (_unit.Personality == AIPersonality.Defensive)
		{
			score *= 1.5f;
		}
		else if (_unit.Personality == AIPersonality.Aggressive)
		{
			score *= 0.5f; // Aggressive units don't like retreating
		}

		return score;
	}

	#endregion

	#region Private Methods - Targeting

	/// <summary>
	/// Scores a potential target based on distance, health, and threat level.
	/// Higher score means more desirable to target.
	/// </summary>
	/// <param name="target">The target unit to score.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>A float score representing the desirability of targeting this unit.</returns>
	private float ScoreTarget(UnitSystem target, BattleState battleState)
	{
		if (_unit == null)
			return float.MinValue;

		Vector3I? myPos = battleState.MapSystem.GetUnitPosition(_unit);
		Vector3I? targetPos = battleState.MapSystem.GetUnitPosition(target);
		
		if (myPos == null || targetPos == null)
			return float.MinValue;

		float score = 0f;
		int distance = CalculateManhattanDistance(myPos.Value, targetPos.Value);

		// Personality-based scoring
		switch (_unit!.Personality)
		{
			case AIPersonality.Aggressive:
				// Prefer close targets
				score += (10 - distance) * 5f;
				// Prefer high-threat targets
				score += target.BaseAtk * 2f;
				break;
				
			case AIPersonality.Opportunistic:
				// Strongly prefer low HP targets
				float hpPercentage = target.Hp / target.MaxHp;
				score += (1f - hpPercentage) * 100f;
				// Prefer targets we can kill this turn
				if (CanKillThisTurn(target))
					score += 50f;
				break;
				
			case AIPersonality.Defensive:
				// Prefer nearby threats
				if (distance <= 3)
					score += 40f;
				// Prefer high-attack enemies
				score += target.BaseAtk * 3f;
				break;
				
			case AIPersonality.Balanced:
				// Balance distance, HP, and threat
				score += (1f - target.Hp / target.MaxHp) * 30f;
				score += (10 - distance) * 2f;
				score += target.BaseAtk * 1.5f;
				break;
		}

		// General factors
		// Prefer targets with status effects we can exploit
		// TODO: Add when status effect system is more developed
		
		// Prefer targets that are isolated (easier to gang up on)
		int alliesNearTarget = CountPlayerUnitsNear(targetPos.Value, battleState, 2);
		score -= alliesNearTarget * 10f;

		return score;
	}

	/// <summary>
	/// Determines if the AI can likely kill the target this turn.
	/// </summary>
	/// <param name="target">The target unit.</param>
	/// <returns>True if the target can be killed this turn, false otherwise.</returns>
	private bool CanKillThisTurn(UnitSystem target)
	{
		if (_unit == null) return false;
		
		// Rough estimate: can we deal enough damage?
		float estimatedDamage = _unit.BaseAtk - target.BaseDef;
	
		if (estimatedDamage < 0)
			estimatedDamage = 0;
		
		return target.Hp <= estimatedDamage * 1.5f; // 1.5x buffer for skill bonuses
	}

	/// <summary>
	/// Counts the number of enemy allies near a given position within a specified range.
	/// </summary>
	/// <param name="position">The position to check around.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <param name="range">The range to consider.</param>
	/// <returns>The number of enemy allies within the specified range.</returns>
	private int CountEnemyAlliesNear(Vector3I position, BattleState battleState, int range)
	{
		int count = 0;
		foreach (var ally in battleState.EnemyUnits)
		{
			if (ally == _unit) continue;
			Vector3I? allyPos = battleState.MapSystem.GetUnitPosition(ally);
			if (allyPos != null && CalculateManhattanDistance(position, allyPos.Value) <= range)
				count++;
		}
		return count;
	}

	/// <summary>
	/// Counts the number of allies near a given position within a specified range.
	/// </summary>
	/// <param name="position">The position to check around.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <param name="range">The range to consider.</param>
	/// <returns>The number of allies within the specified range.</returns>
	private int CountPlayerUnitsNear(Vector3I position, BattleState battleState, int range)
	{
		int count = 0;
		foreach (var enemy in battleState.PlayerUnits)
		{
			Vector3I? enemyPos = battleState.MapSystem.GetUnitPosition(enemy);
			if (enemyPos != null && CalculateManhattanDistance(position, enemyPos.Value) <= range)
				count++;
		}
		return count;
	}

	#endregion

	#region Private Methods - Skill Selection

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

		// Prefer lower mana cost if similar effectiveness
		score -= skill.ManaCost * 0.1f;

		// Bonus for AOE if multiple targets in range
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
		float score = 50f; // Base damage skill value
		
		// Prefer damage against low HP targets
		float hpPercentage = target.Hp / target.MaxHp;
		if (hpPercentage < 0.3f)
			score += 30f; // Finish them off!
		
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
		// Find most damaged ally
		var mostDamagedAlly = battleState.EnemyUnits
			.OrderBy(u => u.Hp / u.MaxHp)
			.FirstOrDefault();
		
		if (mostDamagedAlly == null)
			return 0f;
		
		float hpPercentage = mostDamagedAlly.Hp / mostDamagedAlly.MaxHp;
		
		// High value if ally is critical
		if (hpPercentage < 0.3f)
			return 80f;
		if (hpPercentage < 0.6f)
			return 40f;
		
		return 10f; // Low priority if everyone is healthy
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
		
		// Prefer debuffing high-threat targets
		if (target.BaseAtk > _unit!.BaseAtk * 1.5f)
			score += 20f;
		
		// Control is valuable against high HP enemies
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
		if (_unit == null) return 1f;
		
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

	#region Private Methods - Movement

	/// <summary>
	/// Calculates the best position to move to get within skill range of the target.
	/// </summary>
	/// <param name="battleState">The current battle state.</param>
	/// <param name="targetPos">The target's grid position.</param>
	/// <param name="skillRange">The range of the skill to get within.</param>
	/// <returns>The grid position to move to, or null if no valid move exists.</returns>
	private Vector3I? CalculateMoveToRange(BattleState battleState, Vector3I targetPos, int skillRange = 0)
	{
		if (_unit == null)
			return null;

		List<Vector3I> possibleMoves = _unit.GetPossibleMoves(battleState.MapSystem);
		if (possibleMoves.Count == 0)
			return null;

		Vector3I? bestMove = null;
		float bestScore = float.MinValue;

		foreach (Vector3I move in possibleMoves)
		{
			float score = ScorePosition(move, targetPos, skillRange, battleState);
			if (score > bestScore)
			{
				bestScore = score;
				bestMove = move;
			}
		}

		return bestMove;
	}

	private float ScorePosition(Vector3I position, Vector3I targetPos, int skillRange, BattleState battleState)
	{
		float score = 0f;
		int distance = CalculateManhattanDistance(position, targetPos);

		// Prefer positions that put us in attack range
		if (skillRange > 0)
		{
			if (distance == skillRange)
				score += 100f; // Perfect range!
			else if (distance < skillRange)
				score += 50f - (skillRange - distance) * 5f; // Closer is okay but not ideal
			else
				score += 20f - (distance - skillRange) * 10f; // Further is bad
		}
		else
		{
			// Just move closer
			score += (20 - distance) * 5f;
		}

		// Terrain bonuses
		CellType terrain = battleState.MapSystem.GetCellType(position);
		switch (terrain)
		{
			case CellType.Grass:
				score += 10f; // Slight preference for grass
				break;
			// Add more terrain types as your game develops
		}

		// Tactical positioning
		// Prefer positions with allies nearby (for defensive personalities)
		if (_unit!.Personality == AIPersonality.Defensive || _unit.Personality == AIPersonality.Balanced)
		{
			int alliesNearby = CountEnemyAlliesNear(position, battleState, 2);
			score += alliesNearby * 5f;
		}

		// Avoid being surrounded by enemies
		int enemiesNearby = CountPlayerUnitsNear(position, battleState, 2);
		if (enemiesNearby > 2)
			score -= (enemiesNearby - 1) * 15f; // Penalty for being surrounded

		// Prefer higher ground (future: could give attack/defense bonuses)
		score += position.Y * 2f;

		return score;
	}

	/// <summary>
	/// Calculates the best position to move away from the target.
	/// </summary>
	/// <param name="battleState">The current battle state.</param>
	/// <param name="targetPos">The target's grid position.</param>
	/// <param name="minDistance">The minimum distance to move away from the target.</param>
	/// <returns>The grid position to move to, or null if no valid move exists.</returns>
	private Vector3I? CalculateMoveAway(BattleState battleState, Vector3I targetPos, int minDistance)
	{
		if (_unit == null) return null;

		var possibleMoves = _unit.GetPossibleMoves(battleState.MapSystem);
		Vector3I? bestMove = null;
		int maxDistance = 0;

		foreach (var move in possibleMoves)
		{
			int distance = CalculateManhattanDistance(move, targetPos);
			if (distance > maxDistance && distance >= minDistance)
			{
				maxDistance = distance;
				bestMove = move;
			}
		}

		return bestMove;
	}

	/// <summary>
	/// Calculates Manhattan distance between two grid positions.
	/// Uses 3D Euclidean distance for more accurate pathfinding.
	/// </summary>
	public static int CalculateManhattanDistance(Vector3I pos1, Vector3I pos2)
	{
		int dx = Math.Abs(pos1.X - pos2.X);
		int dy = Math.Abs(pos1.Y - pos2.Y);
		int dz = Math.Abs(pos1.Z - pos2.Z);

		// Use Euclidean distance for 3D movement
		double hypoXZ = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dz, 2));
		double distance = Math.Sqrt(Math.Pow(hypoXZ, 2) + Math.Pow(dy, 2));
		return (int)Math.Ceiling(distance);
	}

	#endregion

	#region Private Methods - Execution

	/// <summary>
	/// Executes the chosen AI decision by calling appropriate GameManager methods.
	/// </summary>
	/// <param name="decision">The decision to execute.</param>
	/// <param name="battleState">Current battle state.</param>
	private async Task ExecuteDecision(AIDecision decision, BattleState battleState)
	{
		if (_unit == null)
		{
			GD.PrintErr("EnemyAIBehavior: Unit reference is null during decision execution");
			return;
		}

		GD.Print($"{_unit.Name}: Executing decision {decision.Action}");

		switch (decision.Action)
		{
			case AIAction.Move:
				if (decision.MovePosition.HasValue)
				{
					battleState.MoveUnitTo(decision.MovePosition.Value);
				}
				else
				{
					GD.PrintErr($"{_unit.Name}: No move position specified, passing turn");
					_unit.PassTurn();
				}
				break;

			case AIAction.MoveAndSkill:
				// First move
				if (decision.MovePosition.HasValue)
				{
					battleState.MoveUnitTo(decision.MovePosition.Value);
					// Wait a moment for move animation
					await Task.Delay(300);
				}
				else
				{
					GD.PrintErr($"{_unit.Name}: No move position specified for MoveAndSkill, passing turn");
					_unit.PassTurn();
					break;
				}

				// Then attack
				if (decision.Target != null && decision.Skill != null)
				{
					battleState.UseSkillOn(decision.Target, decision.Skill);
				}
				else
				{
					GD.PrintErr($"{_unit.Name}: Missing target or skill for MoveAndSkill, turn already used by move");
				}
				break;

			case AIAction.UseSkill:
				if (decision.Target != null && decision.Skill != null)
				{
					battleState.UseSkillOn(decision.Target, decision.Skill);
				}
				else
				{
					GD.PrintErr($"{_unit.Name}: No attack target or skill specified, passing turn");
					_unit.PassTurn();
				}
				break;

			case AIAction.Pass:
			default:
				GD.Print($"{_unit.Name}: Passing turn");
				_unit.PassTurn();
				break;
		}

		GD.Print($"{_unit.Name}: Decision {decision.Action} executed");
	}

	#endregion
}
