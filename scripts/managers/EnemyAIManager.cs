using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.AI;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
/// Manages AI decision-making for enemy units during combat.
/// This is NOT a singleton - it's created per battle by GameManager.
/// </summary>
public class EnemyAIManager
{
	#region Private Fields

	private MapSystem? _mapSystem;
	private List<UnitSystem> _playerUnits = [];
	private List<UnitSystem> _enemyUnits = [];
	private GameManager? _gameManager;

	#endregion

	#region Constructor

	/// <summary>
	/// Creates a new EnemyAIManager instance for a specific battle.
	/// </summary>
	/// <param name="gameManager">Reference to the GameManager controlling this battle.</param>
	public EnemyAIManager(GameManager gameManager)
	{
		_gameManager = gameManager;
		GD.Print("EnemyAIManager created for battle");
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Sets the MapSystem reference for AI pathfinding and queries.
	/// </summary>
	public void SetMapSystem(MapSystem map)
	{
		_mapSystem = map;
		GD.Print("EnemyAIManager: MapSystem reference set");
	}

	/// <summary>
	/// Sets the references to player and enemy unit collections.
	/// </summary>
	public void SetUnitReferences(List<UnitSystem> playerUnits, List<UnitSystem> enemyUnits)
	{
		_playerUnits = playerUnits;
		_enemyUnits = enemyUnits;
		GD.Print($"EnemyAIManager: Tracking {_playerUnits.Count} player units and {_enemyUnits.Count} enemy units");
	}

	/// <summary>
	/// Executes AI logic for a specific enemy unit's turn.
	/// </summary>
	public async Task ExecuteAITurn(UnitSystem unit)
	{
		if (_mapSystem == null)
		{
			GD.PrintErr("EnemyAIManager: MapSystem not set");
			unit.PassTurn();
			return;
		}

		if (_gameManager == null)
		{
			GD.PrintErr("EnemyAIManager: GameManager reference lost");
			unit.PassTurn();
			return;
		}

		GD.Print($"EnemyAIManager: {unit.Name} is thinking...");

		// Get the AI component attached to this unit
		EnemyAIBehavior? aiBehavior = GetAIBehavior(unit);
		
		if (aiBehavior == null)
		{
			GD.PrintErr($"EnemyAIManager: No AI behavior found for {unit.Name}, using default behavior");
			await ExecuteDefaultBehavior(unit);
			return;
		}

		// Create battle state snapshot for AI decision-making
		BattleState battleState = CreateBattleState(unit);

		// Let the AI behavior decide and execute
		await aiBehavior.ExecuteTurn(battleState);
	}

	/// <summary>
	/// Gets all player units that are currently alive.
	/// </summary>
	public List<UnitSystem> GetAlivePlayerUnits()
	{
		return _playerUnits.Where(u => u.IsAlive).ToList();
	}

	/// <summary>
	/// Gets all enemy units that are currently alive.
	/// </summary>
	public List<UnitSystem> GetAliveEnemyUnits()
	{
		return _enemyUnits.Where(u => u.IsAlive).ToList();
	}

	#endregion

	#region Private Methods

	/// <summary>
	/// Retrieves the AI behavior component attached to a unit.
	/// </summary>
	private static EnemyAIBehavior? GetAIBehavior(UnitSystem unit)
	{
		foreach (Node child in unit.GetChildren())
		{
			if (child is EnemyAIBehavior behavior)
				return behavior;
		}
		return null;
	}

	/// <summary>
	/// Creates a snapshot of the current battle state for AI decision-making.
	/// </summary>
	private BattleState CreateBattleState(UnitSystem actingUnit)
	{
		return new BattleState
		{
			ActingUnit = actingUnit,
			MapSystem = _mapSystem!,
			PlayerUnits = GetAlivePlayerUnits(),
			EnemyUnits = GetAliveEnemyUnits(),
			GameManager = _gameManager!
		};
	}

	/// <summary>
	/// Executes a simple default AI behavior when no specific AI component is found.
	/// </summary>
	private async Task ExecuteDefaultBehavior(UnitSystem unit)
	{
		await Task.Delay(1000);
		GD.Print($"EnemyAIManager: {unit.Name} passes turn (default behavior)");
		unit.PassTurn();
	}

	#endregion
}

/// <summary>
/// Represents a snapshot of the battle state at a specific moment.
/// Used by AI to make decisions based on current conditions.
/// </summary>
public class BattleState
{
	/// <summary>The unit currently making decisions.</summary>
	public required UnitSystem ActingUnit { get; init; }

	/// <summary>Reference to the map system for position queries.</summary>
	public required MapSystem MapSystem { get; init; }

	/// <summary>All alive player units.</summary>
	public required List<UnitSystem> PlayerUnits { get; init; }

	/// <summary>All alive enemy units.</summary>
	public required List<UnitSystem> EnemyUnits { get; init; }

	/// <summary>Reference to GameManager for executing actions.</summary>
	public required GameManager GameManager { get; init; }

	/// <summary>
	/// Moves the acting unit to a target position using GameManager.
	/// </summary>
	public void MoveUnitTo(Vector3I targetCell)
	{
		GameManager.MoveUnit(targetCell);
	}

	/// <summary>
	/// Uses a skill on a target through GameManager.
	/// </summary>
	public void UseSkillOn(UnitSystem target, SkillSystem skill)
	{
		GameManager.UseSkill(ActingUnit, target, skill);
	}

	/// <summary>
	/// Finds the nearest player unit to the acting unit.
	/// </summary>
	public UnitSystem? GetNearestPlayerUnit()
	{
		Vector3I? actingPos = MapSystem.GetUnitPosition(ActingUnit);
		if (actingPos == null || PlayerUnits.Count == 0)
			return null;

		UnitSystem? nearest = null;
		float minDistance = float.MaxValue;

		foreach (UnitSystem player in PlayerUnits)
		{
			Vector3I? playerPos = MapSystem.GetUnitPosition(player);
			if (playerPos == null) continue;

			float distance = CalculateDistance(actingPos.Value, playerPos.Value);
			if (distance < minDistance)
			{
				minDistance = distance;
				nearest = player;
			}
		}

		return nearest;
	}

	/// <summary>
	/// Finds the weakest player unit (lowest HP percentage).
	/// </summary>
	public UnitSystem? GetWeakestPlayerUnit()
	{
		return PlayerUnits
			.OrderBy(u => u.Hp / u.MaxHp)
			.FirstOrDefault();
	}

	/// <summary>
	/// Calculates Manhattan distance between two grid positions.
	/// </summary>
	private static float CalculateDistance(Vector3I pos1, Vector3I pos2)
	{
		return Math.Abs(pos1.X - pos2.X) +
			   Math.Abs(pos1.Y - pos2.Y) +
			   Math.Abs(pos1.Z - pos2.Z);
	}

	/// <summary>
	/// Gets all player units within a specific range of the acting unit.
	/// </summary>
	public List<UnitSystem> GetPlayerUnitsInRange(int range)
	{
		Vector3I? actingPos = MapSystem.GetUnitPosition(ActingUnit);
		if (actingPos == null)
			return [];

		return PlayerUnits
			.Where(player =>
			{
				Vector3I? playerPos = MapSystem.GetUnitPosition(player);
				return playerPos != null &&
					   CalculateDistance(actingPos.Value, playerPos.Value) <= range;
			})
			.ToList();
	}

	/// <summary>
	/// Gets the best path to move toward a target unit.
	/// Returns the next cell to move to, or null if can't move closer.
	/// </summary>
	public Vector3I? GetMoveTowardTarget(UnitSystem target)
	{
		Vector3I? currentPos = MapSystem.GetUnitPosition(ActingUnit);
		Vector3I? targetPos = MapSystem.GetUnitPosition(target);

		if (currentPos == null || targetPos == null)
			return null;

		// Get all possible moves for this unit
		List<Vector3I> possibleMoves = ActingUnit.GetPossibleMoves(MapSystem);

		if (possibleMoves.Count == 0)
			return null;

		// Find the move that gets closest to target
		Vector3I? bestMove = null;
		float bestDistance = float.MaxValue;

		foreach (Vector3I move in possibleMoves)
		{
			float distance = CalculateDistance(move, targetPos.Value);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				bestMove = move;
			}
		}

		return bestMove;
	}
}
