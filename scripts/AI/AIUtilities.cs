using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

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

	#region Movement Calculations

	/// <summary>
	/// Calculates the best position to move to get within skill range of the target.
	/// </summary>
	public static Vector3I? CalculateMoveToRange(
		UnitSystem unit,
		BattleState battleState, 
		Vector3I targetPos, 
		int skillRange = 0)
	{
		List<Vector3I> possibleMoves = unit.GetPossibleMoves(battleState.MapSystem);
		if (possibleMoves.Count == 0)
			return null;

		Vector3I? bestMove = null;
		float bestScore = float.MinValue;

		foreach (Vector3I move in possibleMoves)
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
	public static Vector3I? CalculateMoveAway(
		UnitSystem unit,
		BattleState battleState, 
		Vector3I targetPos, 
		int minDistance)
	{
		var possibleMoves = unit.GetPossibleMoves(battleState.MapSystem);
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
	/// Scores a potential movement position.
	/// </summary>
	private static float ScoreMovePosition(
		UnitSystem unit,
		Vector3I position, 
		Vector3I targetPos, 
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
		if (unit.Personality == AIPersonality.Defensive || unit.Personality == AIPersonality.Balanced)
		{
			int alliesNearby = CountEnemyAlliesNear(unit, position, battleState, 2);
			score += alliesNearby * 5f;
		}

		// Avoid being surrounded
		int enemiesNearby = CountPlayerUnitsNear(unit, position, battleState, 2);
		if (enemiesNearby > 2)
			score -= (enemiesNearby - 1) * 15f;

		// Prefer higher ground
		score += position.Y * 2f;

		return score;
	}

	#endregion

	#region Unit Counting

	/// <summary>
	/// Counts the number of enemy allies near a given position within a specified range.
	/// </summary>
	public static int CountEnemyAlliesNear(
		UnitSystem unit,
		Vector3I position, 
		BattleState battleState, 
		int range)
	{
		int count = 0;
		foreach (var ally in battleState.EnemyUnits)
		{
			if (ally == unit) continue;
			Vector3I? allyPos = battleState.MapSystem.GetUnitPosition(ally);
			if (allyPos != null && CalculateManhattanDistance(position, allyPos.Value) <= range)
				count++;
		}
		return count;
	}

	/// <summary>
	/// Counts the number of player units near a given position within a specified range.
	/// </summary>
	public static int CountPlayerUnitsNear(
		UnitSystem unit,
		Vector3I position, 
		BattleState battleState, 
		int range)
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

	#region Threat Assessment

	/// <summary>
	/// Determines if the AI can likely kill the target this turn.
	/// </summary>
	public static bool CanKillThisTurn(UnitSystem attacker, UnitSystem target)
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
	public static UnitSystem? FindNearestThreat(UnitSystem unit, BattleState battleState)
	{
		Vector3I? myPos = battleState.MapSystem.GetUnitPosition(unit);
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
	/// Calculates the total threat level of nearby enemies.
	/// </summary>
	public static float CalculateThreatLevel(
		Vector3I position, 
		BattleState battleState, 
		int range)
	{
		float threatLevel = 0f;

		foreach (var enemy in battleState.PlayerUnits)
		{
			Vector3I? enemyPos = battleState.MapSystem.GetUnitPosition(enemy);
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
		Vector3I from, 
		Vector3I to, 
		MapSystem mapSystem)
	{
		// Simple implementation - can be enhanced with actual raycasting
		int dx = Math.Abs(to.X - from.X);
		int dz = Math.Abs(to.Z - from.Z);
		
		// For now, just check if the path is relatively clear
		// TODO: This is a placeholder - implement Bresenham's line algorithm here
		return dx <= 5 && dz <= 5;
	}

	/// <summary>
	/// Gets all units within a certain range of a position.
	/// </summary>
	public static List<UnitSystem> GetUnitsInRange(
		Vector3I position,
		int range,
		List<UnitSystem> units,
		MapSystem mapSystem)
	{
		List<UnitSystem> unitsInRange = new();

		foreach (var unit in units)
		{
			Vector3I? unitPos = mapSystem.GetUnitPosition(unit);
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
		Vector3I position,
		BattleState battleState)
	{
		float score = 0f;

		// Higher ground is more defensible
		score += position.Y * 5f;

		// Positions with fewer adjacent walkable cells are more defensible (chokepoints)
		int adjacentWalkable = CountAdjacentWalkableCells(position, battleState.MapSystem);
		score += (8 - adjacentWalkable) * 3f; // Max 8 adjacent cells in 3D

		return score;
	}

	/// <summary>
	/// Counts how many adjacent cells are walkable.
	/// </summary>
	private static int CountAdjacentWalkableCells(Vector3I position, MapSystem mapSystem)
	{
		int count = 0;
		Vector3I[] directions =
		[
			new Vector3I(-1, 0, 0),  // Left
			new Vector3I(1, 0, 0),   // Right
			new Vector3I(0, 1, 0),   // Up
			new Vector3I(0, -1, 0),  // Down
			new Vector3I(0, 0, 1),   // Forward
			new Vector3I(0, 0, -1),  // Backward
		];

		foreach (var dir in directions)
		{
			Vector3I checkPos = position + dir;
			try
			{
				if (mapSystem.IsWalkable(checkPos.X, checkPos.Y, checkPos.Z))
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
	public static Vector3I? FindCenterPoint(List<UnitSystem> units, MapSystem mapSystem)
	{
		if (units.Count == 0)
			return null;

		int sumX = 0, sumY = 0, sumZ = 0;
		int validUnits = 0;

		foreach (var unit in units)
		{
			Vector3I? pos = mapSystem.GetUnitPosition(unit);
			if (pos != null)
			{
				sumX += pos.Value.X;
				sumY += pos.Value.Y;
				sumZ += pos.Value.Z;
				validUnits++;
			}
		}

		if (validUnits == 0)
			return null;

		return new Vector3I(
			sumX / validUnits,
			sumY / validUnits,
			sumZ / validUnits
		);
	}

	#endregion
}
