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

	[Export]
	public bool EnableDebugVisualization { get; set; } = false;

	#endregion

	#region Public Fields

	public UnitSystem? _unit;

	#endregion

	#region Private Fields

	private AIDecisionGenerator? _decisionGenerator;
	private AIEvaluator? _evaluator;
	private AIDebugVisualizer? _debugVisualizer;

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

			// Initialize AI components
			_decisionGenerator = new AIDecisionGenerator(_unit);
			_evaluator = new AIEvaluator(_unit);

			// Initialize debug visualizer if enabled
			if (EnableDebugVisualization)
			{
				_debugVisualizer = new AIDebugVisualizer
				{
					Name = "AIDebugVisualizer" // Give it a name for easier debugging
				};
				AddChild(_debugVisualizer);
				GD.Print($"Debug visualizer enabled for {_unit.Name}");
			}

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
	public virtual async Task ExecuteTurn(BattleState battleState)
	{
		if (_unit == null || _decisionGenerator == null || _evaluator == null)
		{
			GD.PrintErr("EnemyAIBehavior: Components not properly initialized");
			return;
		}

		// Simulate thinking time for more natural AI behavior
		float thinkingTime = (float)GD.RandRange(ThinkingDelayMin, ThinkingDelayMax);
		await Task.Delay((int)(thinkingTime * 1000));

		// Decide on action
		AIDecision decision = MakeDecision(battleState);

		// Visualize decision if debug enabled
		if (_debugVisualizer != null && EnableDebugVisualization)
		{
			_debugVisualizer.VisualizeDecision(decision, battleState);
		}

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
	private AIDecision MakeDecision(BattleState battleState)
	{
		if (_unit == null || _decisionGenerator == null)
		{
			GD.PrintErr("EnemyAIBehavior: Components not initialized during decision making");
			return new AIDecision { Action = AIAction.Pass };
		}

		// Generate all possible actions
		var possibleActions = _decisionGenerator.GenerateAllPossibleActions(battleState);

		if (possibleActions.Count == 0)
		{
			GD.Print($"{_unit.Name}: No valid actions available, passing turn");
			return new AIDecision { Action = AIAction.Pass };
		}

		// Pick the highest-scoring action
		var bestDecision = possibleActions.OrderByDescending(a => a.Score).First();

		// Log decision
		GD.Print($"{_unit.Name}: Chose {bestDecision.Action} (Score: {bestDecision.Score:F1}) - {bestDecision.Reasoning}");

		// Debug output top options
		if (_debugVisualizer != null && EnableDebugVisualization)
			_debugVisualizer.VisualizeActionScores(possibleActions, battleState);

		return bestDecision;
	}

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
