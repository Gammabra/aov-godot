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
/// Coordinates between the TurnManager and individual enemy AI behaviors.
/// </summary>
/// <remarks>
/// This manager follows the singleton pattern and acts as the orchestrator
/// for all enemy AI operations. It queries battle state, evaluates threats,
/// and delegates execution to individual enemy AI components.
/// </remarks>
public partial class EnemyAIManager : BaseManager
{
	#region Private Fields

	private MapSystem? _mapSystem;
	private List<UnitSystem> _playerUnits = [];
	private List<UnitSystem> _enemyUnits = [];

	#endregion

	#region Private Properties

	/// <summary>
	/// Singleton instance of the <see cref="EnemyAIManager"/>.
	/// </summary>
	private new static EnemyAIManager? Instance { get; set; }

	#endregion

	#region Godot Export Fields

	[Export]
	private NodePath? _mapSystemPath;

	#endregion

	#region Class Initialization

	/// <summary>
	/// Initializes the EnemyAIManager singleton instance.
	/// </summary>
	protected override void Initialize()
	{
		if (Instance != null && Instance != this)
		{
			GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
			QueueFree();
			return;
		}

		Instance = this;
		
		if (_mapSystemPath != null)
			_mapSystem = GetNode<MapSystem>(_mapSystemPath);
		
		GD.Print("EnemyAIManager initialized successfully");
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Sets the references to player and enemy unit collections.
	/// Should be called by GameManager during initialization.
	/// </summary>
	/// <param name="playerUnits">List of all player units in the battle.</param>
	/// <param name="enemyUnits">List of all enemy units in the battle.</param>
	public void SetUnitReferences(List<UnitSystem> playerUnits, List<UnitSystem> enemyUnits)
	{
		_playerUnits = playerUnits;
		_enemyUnits = enemyUnits;
		GD.Print($"EnemyAIManager: Tracking {_playerUnits.Count} player units and {_enemyUnits.Count} enemy units");
	}

	/// <summary>
	/// Executes AI logic for a specific enemy unit's turn.
	/// This is called by TurnManager's WaitForEnemyAction method.
	/// </summary>
	/// <param name="unit">The enemy unit that needs to act.</param>
	/// <returns>A task that completes when the AI has finished its turn.</returns>
	public async Task ExecuteAITurn(UnitSystem unit)
	{
		if (_mapSystem == null)
		{
			GD.PrintErr("EnemyAIManager: MapSystem not set");
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
	/// <returns>List of alive player units.</returns>
	public List<UnitSystem> GetAlivePlayerUnits()
	{
		return _playerUnits.Where(u => u.IsAlive).ToList();
	}

	/// <summary>
	/// Gets all enemy units that are currently alive.
	/// </summary>
	/// <returns>List of alive enemy units.</returns>
	public List<UnitSystem> GetAliveEnemyUnits()
	{
		return _enemyUnits.Where(u => u.IsAlive).ToList();
	}

	#endregion

	#region Private Methods

	/// <summary>
	/// Retrieves the AI behavior component attached to a unit.
	/// </summary>
	/// <param name="unit">The unit to check.</param>
	/// <returns>The AI behavior component, or null if none exists.</returns>
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
	/// <param name="actingUnit">The unit that is currently acting.</param>
	/// <returns>A BattleState object containing all relevant information.</returns>
	private BattleState CreateBattleState(UnitSystem actingUnit)
	{
		return new BattleState
		{
			ActingUnit = actingUnit,
			MapSystem = _mapSystem!,
			PlayerUnits = GetAlivePlayerUnits(),
			EnemyUnits = GetAliveEnemyUnits()
		};
	}

	/// <summary>
	/// Executes a simple default AI behavior when no specific AI component is found.
	/// </summary>
	/// <param name="unit">The unit to control.</param>
	/// <returns>A task that completes when the action is finished.</returns>
	private async Task ExecuteDefaultBehavior(UnitSystem unit)
	{
		// Simple default: wait a moment then pass turn
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

	/// <summary>
	/// Finds the nearest player unit to the acting unit.
	/// </summary>
	/// <returns>The closest player unit, or null if none exist.</returns>
	public UnitSystem? GetNearestPlayerUnit()
	{
		(int, int, int)? actingPos = MapSystem.GetUnitPosition(ActingUnit);
		if (actingPos == null || PlayerUnits.Count == 0)
			return null;

		UnitSystem? nearest = null;
		float minDistance = float.MaxValue;

		foreach (UnitSystem player in PlayerUnits)
		{
			(int, int, int)? playerPos = MapSystem.GetUnitPosition(player);
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
	/// <returns>The weakest player unit, or null if none exist.</returns>
	public UnitSystem? GetWeakestPlayerUnit()
	{
		return PlayerUnits
			.OrderBy(u => u.Hp / u.MaxHp)
			.FirstOrDefault();
	}

	/// <summary>
	/// Calculates Manhattan distance between two grid positions.
	/// </summary>
	private static float CalculateDistance((int, int, int) pos1, (int, int, int) pos2)
	{
		return Math.Abs(pos1.Item1 - pos2.Item1) +
			   Math.Abs(pos1.Item2 - pos2.Item2) +
			   Math.Abs(pos1.Item3 - pos2.Item3);
	}

	/// <summary>
	/// Gets all player units within a specific range of the acting unit.
	/// </summary>
	/// <param name="range">The maximum distance to consider.</param>
	/// <returns>List of player units within range.</returns>
	public List<UnitSystem> GetPlayerUnitsInRange(int range)
	{
		(int, int, int)? actingPos = MapSystem.GetUnitPosition(ActingUnit);
		if (actingPos == null)
			return [];

		return PlayerUnits
			.Where(player =>
			{
				(int, int, int)? playerPos = MapSystem.GetUnitPosition(player);
				return playerPos != null &&
					   CalculateDistance(actingPos.Value, playerPos.Value) <= range;
			})
			.ToList();
	}
}
