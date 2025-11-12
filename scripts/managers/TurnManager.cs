using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
/// Defines the possible turn states in the battle system.
/// </summary>
public enum TurnState
{
	/// <summary>The player's turn to act.</summary>
    PlayerTurn,

	/// <summary>The enemy's turn to act.</summary>
	EnemyTurn,

	/// <summary>Idle state while waiting for setup or transitions.</summary>
	Waiting
}

/// <summary>
/// Manages the turn-based battle flow between player and enemy units.
/// Handles turn order, state transitions, and async waiting for unit actions.
/// </summary>
/// <remarks>
/// This class acts as the core of the turn-based combat loop.
/// It determines which unit acts next, triggers player input events,
/// and handles asynchronous waiting for both player and AI actions.
/// </remarks>
public partial class TurnManager : BaseManager
{
	#region Private Fields

	private TurnState _currentTurnState = TurnState.Waiting;
	private int _turn;
	private List<KeyValuePair<UnitSystem, TurnState>> _unitsTurnOrder = [];
	private int _currentIndex;
	private EnemyAIManager? _aiManager;

	#endregion

	#region Private Properties

	/// <summary>
	/// Singleton instance of the <see cref="TurnManager"/>.
	/// Ensures only one instance exists in the scene tree.
	/// </summary>
	private new static TurnManager? Instance { get; set; }
	

	#endregion

	#region Public Properties

	/// <summary>
	/// Triggered when the player's turn begins.
    /// </summary>
    public event Action? OnPlayerTurn;

    /// <summary>
	/// Triggered when the player's turn ends.
	/// </summary>
	public event Action? OnPlayerEndTurn;

	#endregion

	#region Class Initialization

	/// <summary>
	///     Initializes the TurnManager singleton instance.
	///     Ensures only one instance exists and sets up the initial state.
	/// </summary>
	/// <remarks>
	///     This method is called automatically by Godot when the node is ready.
	///     It checks for duplicate instances and initializes the game system.
	///     If a duplicate instance is found, it removes the duplicate.
	/// </remarks>
	protected override void Initialize()
	{
		if (Instance != null && Instance != this)
		{
			GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
			QueueFree();
			return;
		}

		Instance = this;
		GD.Print("TurnManager initialized successfully");
	}

	#endregion

	#region Private Methods

	/// <summary>
	/// Main asynchronous turn processing loop.
	/// Handles turn progression for all units in the battle.
	/// </summary>
	/// <remarks>
	/// This method runs indefinitely while the battle is ongoing.
	/// It alternates between player and enemy turns, invoking events and awaiting actions.
	/// </remarks>
	private async Task ProcessTurn()
	{
		while (true)
		{
			GD.Print($"{_unitsTurnOrder[_currentIndex].Key.Name} turn");
			switch (_currentTurnState)
			{
				case TurnState.PlayerTurn:
					OnPlayerTurn?.Invoke();
					await _unitsTurnOrder[_currentIndex].Key.WaitForActionAsync();
					OnPlayerEndTurn?.Invoke();
					break;
				case TurnState.EnemyTurn:
					await WaitForEnemyAction(_unitsTurnOrder[_currentIndex].Key);
					break;
			}

			_currentIndex++;
			if (_currentIndex == _unitsTurnOrder.Count)
				_currentIndex = 0;
			_currentTurnState = _unitsTurnOrder[_currentIndex].Value;
			_turn++;
		}
	}

	/// <summary>
	/// Executes enemy AI logic asynchronously.
	/// </summary>
	/// <param name="unit">The enemy unit performing the action.</param>
	/// <returns>A task that completes when the enemy finishes its action.</returns>
	private async Task WaitForEnemyAction(UnitSystem unit)
	{
		GD.Print($"{unit.Name} start thinking...");

		// Get the EnemyAIManager instance


		if (_aiManager != null)
		{
			await _aiManager.ExecuteAITurn(unit);
		}
		else
		{
			GD.PrintErr("EnemyAIManager not found, using fallback delay");
			await Task.Delay(2000);
			unit.PassTurn();
		}

		GD.Print($"{unit.Name} played.");
	}

	#endregion

	#region Public Methods
	
	/// <summary>
	/// Sets the reference to the EnemyAIManager instance.
	/// </summary>
	/// <param name="aiManager">The EnemyAIManager instance to use.</param>
	public void SetAIManager(EnemyAIManager aiManager)
	{
		_aiManager = aiManager;
	}

	/// <summary>
	/// Initializes the turn order list based on all participating units.
	/// </summary>
	/// <param name="playerUnits">List of all player-controlled units.</param>
	/// <param name="enemyUnits">List of all enemy-controlled units.</param>
	/// <remarks>
	/// The order is determined by each unit's <see cref="UnitSystem.BaseSpeed"/> value,
    /// sorted from highest to lowest.
    /// </remarks>
    public void InitializeTurnOrder(List<UnitSystem> playerUnits, List<UnitSystem> enemyUnits)
    {
        foreach (UnitSystem unit in playerUnits)
            _unitsTurnOrder.Add(new KeyValuePair<UnitSystem, TurnState>(unit, TurnState.PlayerTurn));
        foreach (UnitSystem unit in enemyUnits)
            _unitsTurnOrder.Add(new KeyValuePair<UnitSystem, TurnState>(unit, TurnState.EnemyTurn));
        _unitsTurnOrder = _unitsTurnOrder.OrderByDescending(unit => unit.Key.BaseSpeed).ToList();

        GD.Print("Turn order initialized:");
        foreach (KeyValuePair<UnitSystem, TurnState> unit in _unitsTurnOrder)
            GD.Print($"{unit.Key.Name} (Speed: {unit.Key.BaseSpeed})");
    }

    /// <summary>
    /// Starts the turn-based battle loop asynchronously.
    /// </summary>
    /// <returns>A task representing the battle loop’s lifetime.</returns>
    public async Task StartBattle()
    {
        GD.Print("Starting Battle");
        _currentTurnState = _unitsTurnOrder[_currentIndex].Value;
        _turn++;
        await ProcessTurn();
    }

    /// <summary>
    /// Gets the unit currently taking its turn.
    /// </summary>
    /// <returns>The <see cref="UnitSystem"/> that is currently active.</returns>
    public UnitSystem GetCurrentUnit()
    {
        return _unitsTurnOrder[_currentIndex].Key;
    }

    #endregion
}
