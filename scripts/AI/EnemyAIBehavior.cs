using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.systems;
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
/// Base class for enemy AI behaviors. Attach this as a child node to enemy UnitSystem nodes.
/// Encapsulates decision-making logic for a single enemy unit.
/// </summary>
/// <remarks>
/// This component pattern allows each enemy to have customized AI behavior
/// without modifying the core UnitSystem class. The AI makes decisions based
/// on the current BattleState and executes actions through the unit's methods.
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

    #endregion

    #region Private Fields

    private UnitSystem? _unit;

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
    private AIDecision MakeDecision(BattleState battleState)
    {
        if (_unit == null)
        {
            GD.PrintErr("EnemyAIBehavior: Unit reference is null during decision making");
            return new AIDecision { Action = AIAction.Pass };
        }

        // Check if there are any valid targets
        if (battleState.PlayerUnits.Count == 0)
        {
            GD.Print($"{_unit!.Name}: No player units available, passing turn");
            return new AIDecision { Action = AIAction.Pass };
        }

        // Choose target based on personality
        UnitSystem? target = SelectTarget(battleState);

        if (target == null)
        {
            GD.Print($"{_unit!.Name}: Could not find valid target, passing turn");
            return new AIDecision { Action = AIAction.Pass };
        }

        // Decide between move and attack
        (int, int, int)? myPos = battleState.MapSystem.GetUnitPosition(_unit!);
        (int, int, int)? targetPos = battleState.MapSystem.GetUnitPosition(target);

        if (myPos == null || targetPos == null)
        {
            return new AIDecision { Action = AIAction.Pass };
        }

        // Calculate distance to target
        int distance = CalculateManhattanDistance(myPos.Value, targetPos.Value);

        // Simple decision: if in range, attack; otherwise move closer
        int attackRange = 1; // Default melee range, could be made configurable

        if (distance <= attackRange)
        {
            GD.Print($"{_unit.Name}: Target in range, attacking {target.Name}");
            return new AIDecision
            {
                Action = AIAction.Attack,
                Target = target
            };
        }
        else
        {
            GD.Print($"{_unit.Name}: Moving towards {target.Name}");
            return new AIDecision
            {
                Action = AIAction.Move,
                Target = target,
                MovePosition = CalculateMoveTowardsTarget(battleState, targetPos.Value)
            };
        }
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
        // For now, just pick nearest (can be enhanced later)
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
    /// Calculates the best position to move towards the target.
    /// </summary>
    /// <param name="battleState">Current battle state.</param>
    /// <param name="targetPos">The target's grid position.</param>
    /// <returns>The grid position to move to, or null if no valid move exists.</returns>
    private (int, int, int)? CalculateMoveTowardsTarget(
        BattleState battleState,
        (int, int, int) targetPos)
    {
        if (_unit == null)
            return null;

        // Get all possible moves for this unit
        List<(int, int, int)> possibleMoves = _unit.GetPossibleMoves(battleState.MapSystem);

        if (possibleMoves.Count == 0)
            return null;

        // Find the move that gets us closest to the target
        (int, int, int)? bestMove = null;
        int bestDistance = int.MaxValue;

        foreach ((int, int, int) move in possibleMoves)
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
    /// </summary>
    private static int CalculateManhattanDistance((int, int, int) pos1, (int, int, int) pos2)
    {
        return Math.Abs(pos1.Item1 - pos2.Item1) +
               Math.Abs(pos1.Item2 - pos2.Item2) +
               Math.Abs(pos1.Item3 - pos2.Item3);
    }

    #endregion

    #region Private Methods - Execution

    /// <summary>
    /// Executes the chosen AI decision by calling appropriate unit methods.
    /// </summary>
    /// <param name="decision">The decision to execute.</param>
    /// <param name="battleState">Current battle state.</param>
    private async Task ExecuteDecision(AIDecision decision, BattleState battleState)
    {
        if (_unit == null)
            return;

        switch (decision.Action)
        {
            case AIAction.Move:
                if (decision.MovePosition.HasValue)
                {
                    bool moved = _unit.MoveTo(
                        decision.MovePosition.Value.Item1,
                        decision.MovePosition.Value.Item2,
                        decision.MovePosition.Value.Item3,
                        battleState.MapSystem
                    );
                    
                    if (!moved)
                    {
                        GD.PrintErr($"{_unit.Name}: Failed to move, passing turn instead");
                        _unit.PassTurn();
                    }
                }
                else
                {
                    GD.PrintErr($"{_unit.Name}: No move position specified, passing turn");
                    _unit.PassTurn();
                }
                break;

            case AIAction.Attack:
                if (decision.Target != null)
                {
                    List<UnitSystem> targets = [decision.Target];
                    _unit.Attack(targets, battleState.MapSystem);
                    
                    // Add delay for attack animation
                    await Task.Delay(500);
                    _unit.PassTurn();
                }
                else
                {
                    GD.PrintErr($"{_unit.Name}: No attack target specified, passing turn");
                    _unit.PassTurn();
                }
                break;

            case AIAction.Pass:
            default:
                _unit.PassTurn();
                break;
        }
    }

    #endregion
}

/// <summary>
/// Represents the types of actions an AI can take.
/// </summary>
public enum AIAction
{
    /// <summary>Move to a new position.</summary>
    Move,

    /// <summary>Attack a target unit.</summary>
    Attack,

    /// <summary>Use a skill or ability.</summary>
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
    public (int, int, int)? MovePosition { get; set; }

    /// <summary>The skill to use (for skill actions).</summary>
    public SkillSystem? Skill { get; set; }
}
