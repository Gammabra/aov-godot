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
	public AIPersonality Personality { get; set; } = AIPersonality.Balanced;

	[Export]
	public float ThinkingDelayMin { get; set; } = 0.5f;

	[Export]
	public float ThinkingDelayMax { get; set; } = 2.0f;

	[Export]
	public int AttackRange { get; set; } = 1;

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
			GD.Print($"EnemyAIBehavior attached to {_unit.Name} (Personality: {Personality})");
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

		// Check if there are any valid targets
		if (battleState.PlayerUnits.Count == 0)
		{
			GD.Print($"{_unit.Name}: No player units available, passing turn");
			return new AIDecision { Action = AIAction.Pass };
		}

		// Choose target based on personality
		UnitSystem? target = SelectTarget(battleState);

		if (target == null)
		{
			GD.Print($"{_unit.Name}: Could not find valid target, passing turn");
			return new AIDecision { Action = AIAction.Pass };
		}

		// Get skill to use (for now, just get first available skill)
		SkillSystem? skill = GetBestSkill(battleState, target);

		if (skill == null)
		{
			GD.Print($"{_unit.Name}: No usable skill available, passing turn");
			return new AIDecision { Action = AIAction.Pass };
		}

		// Decide between move and attack
		Vector3I? myPos = battleState.MapSystem.GetUnitPosition(_unit);
		Vector3I? targetPos = battleState.MapSystem.GetUnitPosition(target);

		if (myPos == null || targetPos == null)
		{
			return new AIDecision { Action = AIAction.Pass };
		}

		// Calculate distance to target
		int distance = CalculateManhattanDistance(myPos.Value, targetPos.Value);

		// Decision logic based on distance and unit capabilities
		if (distance <= AttackRange)
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
		else if (distance <= _unit.PossibleMovesRange + AttackRange)
		{
			// Can move closer and possibly attack
			Vector3I? movePos = CalculateMoveTowardsTarget(battleState, targetPos.Value);

			if (movePos.HasValue)
			{
				int distanceAfterMove = CalculateManhattanDistance(movePos.Value, targetPos.Value);

				if (distanceAfterMove <= AttackRange)
				{
					// Can move and attack
					GD.Print($"{_unit.Name}: Moving to {movePos.Value} and attacking {target.Name}");
					return new AIDecision
					{
						Action = AIAction.MoveAndSkill,
						Target = target,
						MovePosition = movePos.Value,
						Skill = skill
					};
				}
				else
				{
					// Just move closer
					GD.Print($"{_unit.Name}: Moving closer to {target.Name}");
					return new AIDecision
					{
						Action = AIAction.Move,
						MovePosition = movePos.Value
					};
				}
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
	private UnitSystem? SelectTarget(BattleState battleState)
	{
		return Personality switch
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
		// Defensive AI prioritizes nearby threats
		List<UnitSystem> nearbyUnits = battleState.GetPlayerUnitsInRange(AttackRange + 2);

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
	/// Selects the best skill to use against a target.
	/// For now returns the first active skill, but can be enhanced with more logic.
	/// </summary>
	public SkillSystem? GetBestSkill(BattleState battleState, UnitSystem target)
	{
		if (_unit == null || _unit.ActiveSkills.Count == 0)
			return null;

		// Simple implementation: return first skill
		// TODO: Add logic to choose best skill based on:
		// - Skill damage/effect type
		// - Mana cost vs available mana
		// - Target's resistances/weaknesses
		// - Skill cooldowns
		return _unit.ActiveSkills[0];
	}

	/// <summary>
	/// Calculates the best position to move towards the target.
	/// </summary>
	/// <param name="battleState">Current battle state.</param>
	/// <param name="targetPos">The target's grid position.</param>
	/// <returns>The grid position to move to, or null if no valid move exists.</returns>
	private Vector3I? CalculateMoveTowardsTarget(
		BattleState battleState,
		Vector3I targetPos)
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
