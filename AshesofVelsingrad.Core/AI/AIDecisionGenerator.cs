using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.AI;

/// <summary>
/// Generates all possible AI actions for evaluation.
/// </summary>
public class AIDecisionGenerator
{
    private readonly IUnitSystem _unit;
    private readonly AIEvaluator _evaluator;

    public AIDecisionGenerator(IUnitSystem unit)
    {
        _unit = unit;
        _evaluator = new AIEvaluator(unit);
    }

    /// <summary>
    /// Generates all possible actions the AI could take this turn.
    /// </summary>
    /// <param name="battleState">Current state of the battle for context.</param>
    /// <returns>List of potential AI decisions with associated scores.</returns>
    public List<AIDecision> GenerateAllPossibleActions(BattleState battleState)
    {
        List<AIDecision> actions = new();
        (int, int, int)? myPos = battleState.MapSystem.GetUnitPosition(_unit);

        if (myPos == null)
            return actions;

        // 1. Evaluate offensive actions against each enemy
        foreach (var target in battleState.PlayerUnits)
            actions.AddRange(GenerateOffensiveActions(target, myPos.Value, battleState));

        // 2. Evaluate support actions for allies (healing, buffs)
        foreach (var ally in battleState.EnemyUnits)
            actions.AddRange(GenerateSupportActions(ally, myPos.Value, battleState));

        // 3. Evaluate defensive/positioning actions
        actions.AddRange(GenerateDefensiveActions(myPos.Value, battleState));

        // 4. Always include "pass turn" as a fallback option
        actions.Add(new AIDecision
        {
            Action = AIAction.Pass,
            Score = 0f,
            Reasoning = "No better options available"
        });

        return actions;
    }

    /// <summary>
    /// Generates all possible offensive actions against a specific target.
    /// </summary>
    /// <param name="target">The enemy target.</param>
    /// <param name="myPos">The AI unit's current position.</param>
    /// <param name="battleState">Current battle state.</param>
    /// <returns>List of possible offensive actions.</returns>
    private List<AIDecision> GenerateOffensiveActions(IUnitSystem target, (int, int, int) myPos, BattleState battleState)
    {
        List<AIDecision> actions = new();
        (int, int, int)? targetPos = battleState.MapSystem.GetUnitPosition(target);

        if (targetPos == null)
            return actions;

        int distance = AIUtilities.CalculateManhattanDistance(myPos, targetPos.Value);

        // Try each offensive skill
        foreach (var skill in _unit.ActiveSkills)
        {
            // Skip non-offensive skills
            if (!skill.EffectType.Equals(AovDataStructures.EffectType.Damage) &&
                !skill.EffectType.Equals(AovDataStructures.EffectType.Debuff) &&
                !skill.EffectType.Equals(AovDataStructures.EffectType.Control))
                continue;

            // Skip if can't afford or on cooldown
            if (skill.ManaCost > _unit.Mana || skill.Cooldown != 0)
                continue;

            // Option 1: Use skill without moving (if in range)
            if (distance <= skill.Range)
            {
                float score = _evaluator.EvaluateOffensiveAction(target, skill, myPos, targetPos.Value, battleState, false);
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
                (int, int, int)? movePos = AIUtilities.CalculateMoveToRange(_unit, battleState, targetPos.Value, skill.Range);

                if (movePos.HasValue)
                {
                    float score = _evaluator.EvaluateOffensiveAction(target, skill, movePos.Value, targetPos.Value, battleState, true);
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
    /// Generates all possible support actions for an ally.
    /// </summary>
    /// <param name="ally">The ally to potentially support.</param>
    /// <param name="myPos">The AI unit's current position.</param>
    /// <param name="battleState">Current battle state.</param>
    /// <returns>List of possible support actions.</returns>
    private List<AIDecision> GenerateSupportActions(IUnitSystem ally, (int, int, int) myPos, BattleState battleState)
    {
        List<AIDecision> actions = new();

        // Don't support yourself
        if (ally == _unit)
            return actions;

        (int, int, int)? allyPos = battleState.MapSystem.GetUnitPosition(ally);
        if (allyPos == null)
            return actions;

        int distance = AIUtilities.CalculateManhattanDistance(myPos, allyPos.Value);

        // Try each support skill
        foreach (var skill in _unit.ActiveSkills)
        {
            // Only consider support skills
            if (!skill.EffectType.Equals(AovDataStructures.EffectType.Heal) &&
                !skill.EffectType.Equals(AovDataStructures.EffectType.Buff) &&
                !skill.EffectType.Equals(AovDataStructures.EffectType.Revive))
                continue;

            // Skip if can't afford or on cooldown
            if (skill.ManaCost > _unit.Mana || skill.Cooldown != 0)
                continue;

            // Option 1: Use skill without moving (if in range)
            if (distance <= skill.Range)
            {
                float score = _evaluator.EvaluateSupportAction(ally, skill, myPos, allyPos.Value, battleState, false);
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
                (int, int, int)? movePos = AIUtilities.CalculateMoveToRange(_unit, battleState, allyPos.Value, skill.Range);

                if (movePos.HasValue)
                {
                    float score = _evaluator.EvaluateSupportAction(ally, skill, movePos.Value, allyPos.Value, battleState, true);
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
	/// Generates defensive/positioning actions like retreating or repositioning.
	/// </summary>
	/// <param name="myPos">The AI unit's current position.</param>
	/// <param name="battleState">Current battle state.</param>
	/// <returns>List of possible defensive actions.</returns>
	private List<AIDecision> GenerateDefensiveActions((int, int, int) myPos, BattleState battleState)
    {
        List<AIDecision> actions = new();

        // Only consider defensive moves if we're in danger
        float hpPercentage = _unit.Hp / _unit.MaxHp;
        int nearbyEnemies = AIUtilities.CountPlayerUnitsNear(_unit, myPos, battleState, 3);

        // Not in danger - skip defensive actions
        if (hpPercentage > 0.5f && nearbyEnemies <= 1)
            return actions;

        // Find nearest threat to retreat from
        IUnitSystem? nearestThreat = AIUtilities.FindNearestThreat(_unit, battleState);
        if (nearestThreat == null)
            return actions;

        (int, int, int)? threatPos = battleState.MapSystem.GetUnitPosition(nearestThreat);
        if (threatPos == null)
            return actions;

        // Generate retreat move
        (int, int, int)? retreatPos = AIUtilities.CalculateMoveAway(_unit, battleState, threatPos.Value, 2);

        if (retreatPos.HasValue)
        {
            float score = _evaluator.EvaluateDefensiveAction(myPos, retreatPos.Value, battleState);
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
}
