using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.Systems;
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
}
