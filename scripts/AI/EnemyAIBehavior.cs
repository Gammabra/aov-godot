using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.AI;

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
	/// Returns the AI's decision without executing it.
	/// Execution is handled by EnemyAIManager.
	/// </summary>
	virtual public async Task<AIDecision> DecideTurn(BattleState battleState)
	{
		if (_unit == null || _decisionGenerator == null || _evaluator == null)
		{
			GD.PrintErr("EnemyAIBehavior: Components not properly initialized");
			return new AIDecision { Action = AIAction.Pass };
		}

		float thinkingTime = (float)GD.RandRange(ThinkingDelayMin, ThinkingDelayMax);
		await Task.Delay((int)(thinkingTime * 1000));

		AIDecision decision = MakeDecision(battleState);

		return decision;
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

		return bestDecision;
	}

	#endregion
}
