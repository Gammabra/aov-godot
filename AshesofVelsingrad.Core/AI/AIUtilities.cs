using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.AI;

/// <summary>
/// Static utility methods for AI decision-making.
/// </summary>
public static class AIUtilities
{
    #region Distance Calculations

    /// <summary>
    /// Calculates Manhattan distance between two grid positions.
    /// Uses 3D Euclidean distance for more accurate pathfinding.
    /// </summary>
    public static int CalculateManhattanDistance((int, int, int) pos1, (int, int, int) pos2)
    {
        int dx = Math.Abs(pos1.Item1 - pos2.Item1);
        int dy = Math.Abs(pos1.Item2 - pos2.Item2);
        int dz = Math.Abs(pos1.Item3 - pos2.Item3);

        // Use Euclidean distance for 3D movement
        double hypoXZ = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dz, 2));
        double distance = Math.Sqrt(Math.Pow(hypoXZ, 2) + Math.Pow(dy, 2));
        return (int)Math.Ceiling(distance);
    }

    #endregion

    #region Movement Calculations

    /// <summary>
    /// Calculates the best position to move to get within skill range of the target.
    /// </summary>
    public static (int, int, int)? CalculateMoveToRange(
        IUnitSystem unit,
        BattleState battleState,
        (int, int, int) targetPos,
        int skillRange = 0)
    {
        List<(int, int, int)> possibleMoves = unit.GetPossibleMoves(battleState.MapSystem);
        if (possibleMoves.Count == 0)
            return null;

        (int, int, int)? bestMove = null;
        float bestScore = float.MinValue;

        foreach ((int, int, int) move in possibleMoves)
        {
            float score = ScoreMovePosition(unit, move, targetPos, skillRange, battleState);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    /// <summary>
    /// Calculates the best position to move away from the target.
    /// </summary>
    public static (int, int, int)? CalculateMoveAway(
        IUnitSystem unit,
        BattleState battleState,
        (int, int, int) targetPos,
        int minDistance)
    {
        var possibleMoves = unit.GetPossibleMoves(battleState.MapSystem);
        (int, int, int)? bestMove = null;
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
    /// Scores a potential movement position.
    /// </summary>
    private static float ScoreMovePosition(
        IUnitSystem unit,
        (int, int, int) position,
        (int, int, int) targetPos,
        int skillRange,
        BattleState battleState)
    {
        float score = 0f;
        int distance = CalculateManhattanDistance(position, targetPos);

        // Prefer positions that put us in attack range
        if (skillRange > 0)
        {
            if (distance == skillRange)
                score += 100f; // Perfect range!
            else if (distance < skillRange)
                score += 50f - (skillRange - distance) * 5f;
            else
                score += 20f - (distance - skillRange) * 10f;
        }
        else
        {
            // Just move closer
            score += (20 - distance) * 5f;
        }

        // Terrain bonuses
        AovDataStructures.CellType terrain = battleState.MapSystem.GetCellType(position);
        switch (terrain)
        {
            case AovDataStructures.CellType.Grass:
                score += 10f;
                break;
        }

        // Tactical positioning
        if (unit.Personality.Equals(AIPersonality.Defensive) || unit.Personality.Equals(AIPersonality.Balanced))
        {
            int alliesNearby = CountEnemyAlliesNear(unit, position, battleState, 2);
            score += alliesNearby * 5f;
        }

        // Avoid being surrounded
        int enemiesNearby = CountPlayerUnitsNear(unit, position, battleState, 2);
        if (enemiesNearby > 2)
            score -= (enemiesNearby - 1) * 15f;

        // Prefer higher ground
        score += position.Item2 * 2f;

        return score;
    }

    #endregion

    #region Unit Counting

    /// <summary>
    /// Counts the number of enemy allies near a given position within a specified range.
    /// </summary>
    public static int CountEnemyAlliesNear(
        IUnitSystem unit,
        (int, int, int) position,
        BattleState battleState,
        int range)
    {
        int count = 0;
        foreach (var ally in battleState.EnemyUnits)
        {
            if (ally == unit) continue;
            (int, int, int)? allyPos = battleState.MapSystem.GetUnitPosition(ally);
            if (allyPos != null && CalculateManhattanDistance(position, allyPos.Value) <= range)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Counts the number of player units near a given position within a specified range.
    /// </summary>
    public static int CountPlayerUnitsNear(
        IUnitSystem unit,
        (int, int, int) position,
        BattleState battleState,
        int range)
    {
        int count = 0;
        foreach (var enemy in battleState.PlayerUnits)
        {
            (int, int, int)? enemyPos = battleState.MapSystem.GetUnitPosition(enemy);
            if (enemyPos != null && CalculateManhattanDistance(position, enemyPos.Value) <= range)
                count++;
        }
        return count;
    }

    #endregion

    #region Threat Assessment

    /// <summary>
    /// Determines if the AI can likely kill the target this turn.
    /// </summary>
    public static bool CanKillThisTurn(IUnitSystem attacker, IUnitSystem target)
    {
        // Rough estimate: can we deal enough damage?
        float estimatedDamage = attacker.BaseAtk - target.BaseDef;

        if (estimatedDamage < 0)
            estimatedDamage = 0;

        return target.Hp <= estimatedDamage * 1.5f; // 1.5x buffer for skill bonuses
    }

    /// <summary>
    /// Finds the nearest threatening enemy unit.
    /// </summary>
    public static IUnitSystem? FindNearestThreat(IUnitSystem unit, BattleState battleState)
    {
        (int, int, int)? myPos = battleState.MapSystem.GetUnitPosition(unit);
        if (myPos == null)
            return null;

        IUnitSystem? nearestThreat = null;
        int minDistance = int.MaxValue;

        foreach (var enemy in battleState.PlayerUnits)
        {
            (int, int, int)? enemyPos = battleState.MapSystem.GetUnitPosition(enemy);
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
    /// Calculates the total threat level of nearby enemies.
    /// </summary>
    public static float CalculateThreatLevel(
        (int, int, int) position,
        BattleState battleState,
        int range)
    {
        float threatLevel = 0f;

        foreach (var enemy in battleState.PlayerUnits)
        {
            (int, int, int)? enemyPos = battleState.MapSystem.GetUnitPosition(enemy);
            if (enemyPos == null) continue;

            int distance = CalculateManhattanDistance(position, enemyPos.Value);
            if (distance <= range)
            {
                // Closer enemies are more threatening
                float proximityMultiplier = 1f + (range - distance) * 0.2f;
                threatLevel += enemy.BaseAtk * proximityMultiplier;
            }
        }

        return threatLevel;
    }

    #endregion

    #region Pathfinding Helpers

    /// <summary>
    /// Checks if there's a clear line of sight between two positions.
    /// </summary>
    public static bool HasLineOfSight(
        (int, int, int) from,
        (int, int, int) to,
        IMapSystem mapSystem)
    {
        // Simple implementation - can be enhanced with actual raycasting
        int dx = Math.Abs(to.Item1 - from.Item1);
        int dz = Math.Abs(to.Item3 - from.Item3);

        // For now, just check if the path is relatively clear
        // TODO: This is a placeholder - implement Bresenham's line algorithm here
        return dx <= 5 && dz <= 5;
    }

    /// <summary>
    /// Gets all units within a certain range of a position.
    /// </summary>
    public static List<IUnitSystem> GetUnitsInRange(
        (int, int, int) position,
        int range,
        List<IUnitSystem> units,
        IMapSystem mapSystem)
    {
        List<IUnitSystem> unitsInRange = new();

        foreach (var unit in units)
        {
            (int, int, int)? unitPos = mapSystem.GetUnitPosition(unit);
            if (unitPos == null) continue;

            if (CalculateManhattanDistance(position, unitPos.Value) <= range)
            {
                unitsInRange.Add(unit);
            }
        }

        return unitsInRange;
    }

    #endregion

    #region Position Analysis

    /// <summary>
    /// Evaluates how defensible a position is.
    /// </summary>
    public static float EvaluatePositionDefensibility(
        (int, int, int) position,
        BattleState battleState)
    {
        float score = 0f;

        // Higher ground is more defensible
        score += position.Item2 * 5f;

        // Positions with fewer adjacent walkable cells are more defensible (chokepoints)
        int adjacentWalkable = CountAdjacentWalkableCells(position, battleState.MapSystem);
        score += (8 - adjacentWalkable) * 3f; // Max 8 adjacent cells in 3D

        return score;
    }

    /// <summary>
    /// Counts how many adjacent cells are walkable.
    /// </summary>
    private static int CountAdjacentWalkableCells((int, int, int) position, IMapSystem mapSystem)
    {
        int count = 0;
        (int, int, int)[] directions =
        [
            (-1, 0, 0),  // Left
			(1, 0, 0),   // Right
			(0, 1, 0),   // Up
			(0, -1, 0),  // Down
			(0, 0, 1),   // Forward
			(0, 0, -1),  // Backward
		];

        foreach (var dir in directions)
        {
            (int, int, int) checkPos = (position.Item1 + dir.Item1, position.Item2 + dir.Item2, position.Item3 + dir.Item3);
            try
            {
                if (mapSystem.IsWalkable(checkPos.Item1, checkPos.Item2, checkPos.Item3))
                    count++;
            }
            catch (ArgumentOutOfRangeException)
            {
                // Position is out of bounds, don't count it
            }
        }

        return count;
    }

    /// <summary>
    /// Finds the center point between multiple units (useful for AOE positioning).
    /// </summary>
    public static (int, int, int)? FindCenterPoint(List<IUnitSystem> units, IMapSystem mapSystem)
    {
        if (units.Count == 0)
            return null;

        int sumX = 0, sumY = 0, sumZ = 0;
        int validUnits = 0;

        foreach (var unit in units)
        {
            (int, int, int)? pos = mapSystem.GetUnitPosition(unit);
            if (pos != null)
            {
                sumX += pos.Value.Item1;
                sumY += pos.Value.Item2;
                sumZ += pos.Value.Item3;
                validUnits++;
            }
        }

        if (validUnits == 0)
            return null;

        return (
            sumX / validUnits,
            sumY / validUnits,
            sumZ / validUnits
        );
    }

    #endregion
}
