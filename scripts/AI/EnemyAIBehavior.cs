using System;
using System.Collections.Generic;
using System.Linq;
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

		// Choose target based on personality
		UnitSystem? target = SelectTarget(battleState);

		if (target == null)
		{
			GD.Print($"{_unit.Name}: Could not find valid target, passing turn");
			return new AIDecision { Action = AIAction.Pass };
		}

		// Decide between move and attack
		Vector3I? myPos = battleState.MapSystem.GetUnitPosition(_unit);
		Vector3I? targetPos = battleState.MapSystem.GetUnitPosition(target);

		if (myPos == null || targetPos == null)
			return new AIDecision { Action = AIAction.Pass };

		// Calculate distance to target
		int distance = CalculateManhattanDistance(myPos.Value, targetPos.Value);

		// Get skill to use (for now, just get first available skill)
		SkillSystem? skill = GetBestSkill(battleState, target, distance);

		// Decision logic based on distance and unit capabilities
		if (skill != null && distance <= skill.Range)
		{
			// In attack range - just attack
			GD.Print($"{_unit.Name}: Target in range, attacking {target.Name}");
			return new AIDecision
			{
				Action = AIAction.UseSkill,
				Target = target,
				Skill = skill
			};
		}
		else if (skill != null && distance <= _unit.PossibleMovesRange + skill.Range)
		{
			// Can move closer and possibly attack
			Vector3I? movePos = CalculateMoveToRange(battleState, targetPos.Value);

			if (movePos.HasValue)
			{
				GD.Print($"{_unit.Name}: Moving to {movePos.Value} and attacking {target.Name}");
				return new AIDecision
				{
					Action = AIAction.MoveAndSkill,
					Target = target,
					MovePosition = movePos.Value,
					Skill = skill
				};
			}
		}
		else if (_unit.Hp < _unit.MaxHp * 0.3f) // TODO: check 1/3 of hp is balanced or not, check if personality should be required for this move
		{
			// Defensive AI tries to maintain distance
			Vector3I? movePos = CalculateMoveAway(battleState, targetPos.Value, _unit.PossibleMovesRange);
			if (movePos.HasValue)
			{
				return new AIDecision
				{
					Action = AIAction.Move,
					MovePosition = movePos.Value
				};
			}
		}
		else
		{
			// go closer to target
			Vector3I? movePos = CalculateMoveToRange(battleState, targetPos.Value);
			if (movePos.HasValue)
			{
				return new AIDecision
				{
					Action = AIAction.Move,
					MovePosition = movePos.Value
				};
			}
		}

		// Can't reach target effectively
		GD.Print($"{_unit.Name}: Cannot reach {target.Name}, passing turn");
		return new AIDecision
		{
			Action = AIAction.Pass
		};
	}

	/// <summary>
	/// Selects a target unit based on the AI's personality.
	/// </summary>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>The selected target unit, or null if none available.</returns>

	//TODO: Upgrade the target selection to consider more factors like threat level, distance, status effects, skill choosen, etc.
	private UnitSystem? SelectTarget(BattleState battleState)
	{
		if (_unit == null)
			return null;

		return _unit.Personality switch
		{
			AIPersonality.Aggressive => battleState.GetNearestPlayerUnit(),
			AIPersonality.Opportunistic => battleState.GetWeakestPlayerUnit(),
			AIPersonality.Defensive => SelectDefensiveTarget(battleState),
			AIPersonality.Balanced => SelectBalancedTarget(battleState),
			_ => battleState.GetNearestPlayerUnit()
		};
	}

	/// <summary>
	/// Defensive AI prefers targets that are closer but also considers threat level.
	/// </summary>
	private UnitSystem? SelectDefensiveTarget(BattleState battleState)
	{
		if (_unit == null)
			return null;
		// Defensive AI prioritizes nearby threats
		List<UnitSystem> nearbyUnits = battleState.GetPlayerUnitsInRange(_unit.PossibleMovesRange + 1);

		if (nearbyUnits.Count > 0)
		{
			// Among nearby units, prefer the one with highest attack
			return nearbyUnits.OrderByDescending(u => u.BaseAtk).FirstOrDefault();
		}

		// No immediate threats, pick nearest
		return battleState.GetNearestPlayerUnit();
	}

	/// <summary>
	/// Balanced AI weighs multiple factors: distance, health, threat.
	/// </summary>
	private UnitSystem? SelectBalancedTarget(BattleState battleState)
	{
		List<UnitSystem> nearbyUnits = battleState.GetPlayerUnitsInRange(5);

		if (nearbyUnits.Count > 0)
		{
			// Among nearby units, prefer weaker ones
			return nearbyUnits.OrderBy(u => u.Hp / u.MaxHp).FirstOrDefault();
		}

		// Otherwise, go for nearest
		return battleState.GetNearestPlayerUnit();
	}

	/// <summary>
	/// Selects the best skill to use against the target based on distance and effectiveness.
	/// </summary>
	/// <param name="battleState">Current battle state.</param>
	/// <param name="target">The target unit.</param>
	/// <param name="distance">Distance to the target.</param>
	/// <returns>The best skill to use, or null if none suitable.</returns>
	public SkillSystem? GetBestSkill(BattleState battleState, UnitSystem target, int distance)
	{
		if (_unit == null || _unit.ActiveSkills.Count == 0)
			return null;

		SkillSystem? bestSkill = null;
		float bestScore = float.MinValue;

		foreach (var skill in _unit.ActiveSkills)
		{
			// Skip if can't use
			if (skill.ManaCost > _unit.Mana || skill.Cooldown != 0)
				continue;
			
			// Skip if out of range (unless we can move closer)
			if (distance > skill.Range + _unit.PossibleMovesRange)
				continue;

			float score = ScoreSkill(skill, target, battleState);
			
			if (score > bestScore)
			{
				bestScore = score;
				bestSkill = skill;
			}
		}

		return bestSkill;
	}

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

	/// <summary>
	/// Calculates the best position to move towards the target.
	/// </summary>
	/// <param name="battleState">Current battle state.</param>
	/// <param name="targetPos">The target's grid position.</param>
	/// <returns>The grid position to move to, or null if no valid move exists.</returns>
	//TODO: improve this to consider obstacles and pathfinding and take in account attack range to optimize distance better
	private Vector3I? CalculateMoveToRange(BattleState battleState, Vector3I targetPos)
	{
		if (_unit == null)
			return null;

		// Get all possible moves for this unit
		List<Vector3I> possibleMoves = _unit.GetPossibleMoves(battleState.MapSystem);

		if (possibleMoves.Count == 0)
			return null;

		// Find the move that gets us closest to the target
		Vector3I? bestMove = null;
		int bestDistance = int.MaxValue;

		foreach (Vector3I move in possibleMoves)
		{
			int distance = CalculateManhattanDistance(move, targetPos);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				bestMove = move;
			}
		}

		return bestMove;
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
