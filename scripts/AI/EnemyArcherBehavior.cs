using System.Linq;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.AI;

/// <summary>
/// Specialized AI behavior for archer enemies.
/// Prefers to maintain distance and use ranged attacks.
/// </summary>
public partial class ArcherAIBehavior : EnemyAIBehavior
{
    [Export]
    public int PreferredRange { get; set; } = 3;

    public override void _Ready()
    {
        base._Ready();
        Personality = AIPersonality.Defensive;
        AttackRange = 4; // Archer specific range
        GD.Print($"ArcherAIBehavior initialized with preferred range {PreferredRange}");
    }

    protected UnitSystem? SelectTarget(BattleState battleState)
    {
        // Archers prefer targets that are in their optimal range
        var targetsInRange = battleState.GetPlayerUnitsInRange(AttackRange);
        
        if (targetsInRange.Count > 0)
        {
            // Prefer the most damaged target within range
            return targetsInRange.OrderBy(t => t.Hp / t.MaxHp).FirstOrDefault();
        }

        // If no targets in range, get the nearest one to consider movement
        return battleState.GetNearestPlayerUnit();
    }

    protected new AIDecision MakeDecision(BattleState battleState)
    {
        if (_unit == null)
            return new AIDecision { Action = AIAction.Pass };

        var target = SelectTarget(battleState);
        if (target == null)
            return new AIDecision { Action = AIAction.Pass };

        var myPos = battleState.MapSystem.GetUnitPosition(_unit);
        var targetPos = battleState.MapSystem.GetUnitPosition(target);

        if (myPos == null || targetPos == null)
            return new AIDecision { Action = AIAction.Pass };

        int distance = CalculateManhattanDistance(myPos.Value, targetPos.Value);
        var skill = GetBestSkill(battleState, target);

        // Archer-specific logic: maintain optimal range
        if (distance <= AttackRange && distance >= PreferredRange)
        {
            // Perfect range - just attack
            GD.Print($"{_unit.Name}: Target at perfect range ({distance}), attacking");
            return new AIDecision
            {
                Action = AIAction.UseSkill,
                Target = target,
                Skill = skill
            };
        }
        else if (distance > AttackRange)
        {
            // Too far - move closer
            var movePos = CalculateMoveToRange(battleState, targetPos.Value, PreferredRange);
            if (movePos.HasValue)
            {
                return new AIDecision
                {
                    Action = AIAction.Move,
                    MovePosition = movePos.Value
                };
            }
        }
        else if (distance < PreferredRange)
        {
            // Too close - move away to optimal range
            var movePos = CalculateMoveAway(battleState, targetPos.Value, PreferredRange);
            if (movePos.HasValue)
            {
                return new AIDecision
                {
                    Action = AIAction.Move,
                    MovePosition = movePos.Value
                };
            }
        }

        return base.MakeDecision(battleState);
    }

    private Vector3I? CalculateMoveToRange(BattleState battleState, Vector3I targetPos, int desiredRange)
    {
        if (_unit == null) return null;

        var possibleMoves = _unit.GetPossibleMoves(battleState.MapSystem);
        Vector3I? bestMove = null;
        int bestDistanceDifference = int.MaxValue;

        foreach (var move in possibleMoves)
        {
            int distance = CalculateManhattanDistance(move, targetPos);
            int distanceDifference = Mathf.Abs(distance - desiredRange);

            if (distanceDifference < bestDistanceDifference)
            {
                bestDistanceDifference = distanceDifference;
                bestMove = move;
            }
        }

        return bestMove;
    }

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
}
