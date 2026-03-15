using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     Manages the turn-based battle flow between player and enemy units.
///     Handles turn order, state transitions, and async waiting for unit actions.
/// </summary>
/// <remarks>
///     This class acts as the core of the turn-based combat loop.
///     It determines which unit acts next, triggers player input events,
///     and handles asynchronous waiting for both player and AI actions.
/// </remarks>
public partial class TurnManager : BaseManager
{
    #region Private Fields

    private AovDataStructures.TurnState _currentTurnState = AovDataStructures.TurnState.Waiting;
    private int _turn;
    private List<KeyValuePair<UnitSystem, AovDataStructures.TurnState>> _unitsTurnOrder = [];
    private int _currentIndex;
    private EnemyAIManager? _aiManager;

    #endregion

    #region Private Properties

    /// <summary>
    ///     Singleton instance of the <see cref="TurnManager" />.
    ///     Ensures only one instance exists in the scene tree.
    /// </summary>
    private new static TurnManager? Instance { get; set; }

    #endregion

    #region Public Properties

    /// <summary>
    ///     Triggered when the player's turn begins.
    /// </summary>
    public event Action? OnPlayerTurn;

    /// <summary>
    ///     Triggered when the player's turn ends.
    /// </summary>
    public event Action? OnPlayerTurnEnd;

    /// <summary>
    ///     Triggered when the enemy's turn ends
    /// </summary>
    public event Action? OnEnemyTurnEnd;

    /// <summary>
    ///     Triggered when the current turn ends.
    /// </summary>
    public event Action? OnCurrentTurnEnd;

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
    ///     Main asynchronous turn processing loop.
    ///     Handles turn progression for all units in the battle.
    /// </summary>
    /// <remarks>
    ///     This method runs indefinitely while the battle is ongoing.
    ///     It alternates between player and enemy turns, invoking events and awaiting actions.
    /// </remarks>
    private async Task ProcessTurn()
    {
        while (true)
        {
            GD.Print($"{_unitsTurnOrder[_currentIndex].Key.Name} turn");
            foreach (KeyValuePair<UnitSystem, AovDataStructures.TurnState> unit in _unitsTurnOrder)
                GD.Print($"{unit.Key.Name} (HP: {unit.Key.Hp})");
            switch (_currentTurnState)
            {
                case AovDataStructures.TurnState.PlayerTurn:
                    if (!_unitsTurnOrder[_currentIndex].Key.IsControlled)
                    {
                        OnPlayerTurn?.Invoke();
                        await _unitsTurnOrder[_currentIndex].Key.WaitForActionAsync();
                    }

                    OnPlayerTurnEnd?.Invoke();
                    break;
                case AovDataStructures.TurnState.EnemyTurn:
                    if (!_unitsTurnOrder[_currentIndex].Key.IsControlled)
                        await WaitForEnemyAction(_unitsTurnOrder[_currentIndex].Key);
                    OnEnemyTurnEnd?.Invoke();

                    break;
            }

            if (_currentTurnState == AovDataStructures.TurnState.Finished)
                break;

            _currentIndex++;
            for (; _currentIndex < _unitsTurnOrder.Count; _currentIndex++)
            {
                if (_unitsTurnOrder[_currentIndex].Key.IsAlive)
                    break;
            }

            if (_currentIndex >= _unitsTurnOrder.Count)
            {
                _currentIndex = 0;
                _turn++;
                OnCurrentTurnEnd?.Invoke();
            }

            GD.Print($"Current index after trying reset to 0 loop : {_currentIndex}");

            _currentTurnState = _unitsTurnOrder[_currentIndex].Value;
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

        //Get the EnemyAIManager instance
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

    /// <summary>
    /// Compute Fisher-Yates shuffle Algorithm
    /// </summary>
    /// <param name="unitsToRand">The unit list to randomize</param>
    /// <param name="indexToStart">The index where the replacement start in <c>_unitsTurnOrder</c>.<br/>
    /// NOTICE: The value is decremented</param>
    /// <param name="r">The random class to use</param>
    private void ComputeFisherYatesShuffleAlgorithm(
        List<KeyValuePair<UnitSystem, AovDataStructures.TurnState>> unitsToRand,
        int indexToStart,
        Random r
    )
    {
        if (unitsToRand.Count > 1)
        {
            for (int idx = unitsToRand.Count - 1; idx > 0; idx--)
            {
                int j = r.Next(0, idx + 1);

                (unitsToRand[idx], unitsToRand[j]) = (unitsToRand[j], unitsToRand[idx]);
            }

            int indexToReplace = indexToStart;
            for (int idx = unitsToRand.Count - 1; idx >= 0; idx--, indexToReplace--)
            {
                _unitsTurnOrder[indexToReplace] = unitsToRand[idx];
            }
        }
    }

    /// <summary>
    /// Randomize the units turn orders with the same speed using the Fisher-Yates shuffle Algorithm
    /// </summary>
    private void RandTurnOrderOnSameSpeed()
    {
        Random r = new();
        List<KeyValuePair<UnitSystem, AovDataStructures.TurnState>> unitsToRand = [];
        float speed = 0;

        unitsToRand.Add(_unitsTurnOrder.First());
        speed = unitsToRand.First().Key.BaseSpeed;
        for (int i = 1; i < _unitsTurnOrder.Count; i++)
        {
            if (Math.Abs(_unitsTurnOrder[i].Key.BaseSpeed - speed) < 0.0001)
            {
                unitsToRand.Add(_unitsTurnOrder[i]);
            }
            else
            {
                ComputeFisherYatesShuffleAlgorithm(unitsToRand, i - 1, r);

                unitsToRand.Clear();
                speed = _unitsTurnOrder[i].Key.BaseSpeed;
            }
        }

        // Compute again if there is still units in "unitsToRand" list and the loop reached the end of "_unitsTurnOrder"
        ComputeFisherYatesShuffleAlgorithm(unitsToRand, _unitsTurnOrder.Count - 1, r);
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
    ///     Initializes the turn order list based on all participating units.
    /// </summary>
    /// <param name="playerUnits">List of all player-controlled units.</param>
    /// <param name="enemyUnits">List of all enemy-controlled units.</param>
    /// <remarks>
    ///     The order is determined by each unit's <see cref="UnitSystem.BaseSpeed" /> value,
    ///     sorted from highest to lowest.
    /// </remarks>
    public void InitializeTurnOrder(List<UnitSystem> playerUnits, List<UnitSystem> enemyUnits)
    {
        foreach (UnitSystem unit in playerUnits)
            _unitsTurnOrder.Add(
                new KeyValuePair<UnitSystem, AovDataStructures.TurnState>(unit, AovDataStructures.TurnState.PlayerTurn)
            );
        foreach (UnitSystem unit in enemyUnits)
            _unitsTurnOrder.Add(
                new KeyValuePair<UnitSystem, AovDataStructures.TurnState>(unit, AovDataStructures.TurnState.EnemyTurn)
            );
        _unitsTurnOrder = _unitsTurnOrder.OrderByDescending(unit => unit.Key.BaseSpeed).ToList();

        RandTurnOrderOnSameSpeed();
        GD.Print("Turn order initialized:");
        foreach (KeyValuePair<UnitSystem, AovDataStructures.TurnState> unit in _unitsTurnOrder)
            GD.Print($"{unit.Key.Name} (Speed: {unit.Key.BaseSpeed})");
    }

    /// <summary>
    ///     Starts the turn-based battle loop asynchronously.
    /// </summary>
    /// <returns>A task representing the battle loop’s lifetime.</returns>
    public async Task StartBattle()
    {
        GD.Print("Starting Battle");
        _currentTurnState = _unitsTurnOrder[_currentIndex].Value;
        _turn++;
        await Task.Run(async () => await ProcessTurn());
    }

    /// <summary>
    /// Gets the unit currently taking its turn.
    /// </summary>
    /// <returns>The <see cref="UnitSystem"/> that is currently active.</returns>
    public UnitSystem GetCurrentUnit()
    {
        if (_unitsTurnOrder.Count == 0)
        {
            GD.PrintErr("No units in turn order!");
            throw new InvalidOperationException("Turn order is not initialized or is empty.");
        }

        if (_currentIndex < 0 || _currentIndex >= _unitsTurnOrder.Count)
        {
            GD.PrintErr($"Current index {_currentIndex} out of range for turn order of size {_unitsTurnOrder.Count}");
            throw new IndexOutOfRangeException($"Current index is out of range");
        }

        return _unitsTurnOrder[_currentIndex].Key;
    }

    /// <summary>
    ///     Called by the <see cref="GameManager" /> to inform the <see cref="TurnManager" />
    ///     the game is finished
    /// </summary>
    public virtual void EndTurnManagerLoop()
    {
        _currentTurnState = AovDataStructures.TurnState.Finished;
    }

    #endregion
}
