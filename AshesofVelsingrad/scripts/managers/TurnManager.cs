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

    /// <summary>
    ///     Public accessor for the active <see cref="TurnManager" /> in the scene.
    ///     Returns null when no battle is active.
    /// </summary>
    public static TurnManager? Active => Instance;

    #endregion

    #region Battle wiring (used by BattleLauncher / HUD)

    /// <summary>
    ///     The unit currently taking its turn, or null if the turn order is not set up yet.
    /// </summary>
    public UnitSystem? CurrentUnit =>
        _unitsTurnOrder.Count > 0
        && _currentIndex >= 0
        && _currentIndex < _unitsTurnOrder.Count
            ? _unitsTurnOrder[_currentIndex].Key
            : null;

    /// <summary>
    ///     Active victory condition installed by <see cref="SetVictoryCondition" />.
    ///     The battle loop checks this each turn to decide whether to end the fight.
    /// </summary>
    private systems.battle.VictoryCondition? _victoryCondition;

    /// <summary>
    ///     Optional outcome request set by <see cref="RequestAbort" />.
    /// </summary>
    private systems.battle.BattleOutcome? _pendingAbort;

    /// <summary>
    ///     Install the victory condition the battle loop should evaluate each turn.
    /// </summary>
    /// <param name="condition">Condition implementation, or <c>null</c> to clear.</param>
    public void SetVictoryCondition(systems.battle.VictoryCondition? condition)
    {
        _victoryCondition = condition;
    }

    /// <summary>
    ///     Run the battle loop and return the final result.
    /// </summary>
    /// <remarks>
    ///     Bridges <see cref="StartBattle" /> (the legacy entry point) to the
    ///     <see cref="BattleResult" /> contract that <c>BattleLauncher</c> expects.
    /// </remarks>
    /// <returns>The outcome of the battle once the loop terminates.</returns>
    public async Task<systems.battle.BattleResult> RunBattleLoop()
    {
        await StartBattle();

        if (_pendingAbort is { } aborted)
        {
            _pendingAbort = null;
            return new systems.battle.BattleResult { Outcome = aborted };
        }

        return new systems.battle.BattleResult { Outcome = systems.battle.BattleOutcome.Victory };
    }

    /// <summary>
    ///     Request the battle loop to terminate with a specific outcome.
    /// </summary>
    /// <param name="outcome">Outcome to surface to the caller.</param>
    public void RequestAbort(systems.battle.BattleOutcome outcome)
    {
        _pendingAbort = outcome;
        EndTurnManagerLoop();
    }

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
    /// Triggered when the enemy's turn begins.
    /// </summary>
    public event Action? OnEnemyTurn;

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

    /// <inheritdoc />
    /// <remarks>
    ///     Clear the static singleton when this node leaves the tree (e.g. on
    ///     <c>ReloadCurrentScene</c>) so the new <see cref="TurnManager" /> on the new
    ///     scene's first <c>_Ready</c> doesn't see a stale <c>Instance</c> and self-destruct
    ///     as a duplicate.
    /// </remarks>
    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
        // Tell the async ProcessTurn loop to exit at the next iteration so we don't
        // continue invoking events on dead unit references after the scene unloaded.
        _currentTurnState = AovDataStructures.TurnState.Finished;
        base._ExitTree();
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
                    OnPlayerTurn?.Invoke();
                    if (!_unitsTurnOrder[_currentIndex].Key.IsControlled)
                    {
                        await _unitsTurnOrder[_currentIndex].Key.WaitForActionAsync();
                    }

                    OnPlayerTurnEnd?.Invoke();
                    break;
                case AovDataStructures.TurnState.EnemyTurn:
                {
                    bool isAlly = _unitsTurnOrder[_currentIndex].Key.Faction == Faction.Ally;
                    if (isAlly) OnAllyTurn?.Invoke();
                    else OnEnemyTurn?.Invoke();

                    if (!_unitsTurnOrder[_currentIndex].Key.IsControlled)
                        await WaitForEnemyAction(_unitsTurnOrder[_currentIndex].Key);

                    if (isAlly) OnAllyTurnEnd?.Invoke();
                    else OnEnemyTurnEnd?.Invoke();

                    break;
                }
            }

            if (_currentTurnState == AovDataStructures.TurnState.Finished)
                break;

            // Wrap-around advancement that ALWAYS lands on a live unit (or breaks the loop
            // if everyone is dead). The previous version skipped dead units after the
            // increment but didn't re-skip after wrapping back to index 0, so a dead unit
            // sitting at the start of the order would still get its turn run.
            if (!AdvanceToNextLiveUnit())
            {
                GD.Print("TurnManager: no live units remaining — ending battle loop.");
                break;
            }

            GD.Print($"Current index after advancing to next live unit : {_currentIndex}");

            _currentTurnState = _unitsTurnOrder[_currentIndex].Value;
        }
    }

    /// <summary>
    ///     Move <see cref="_currentIndex" /> to the next live unit in turn order, wrapping
    ///     around the end and firing <see cref="OnCurrentTurnEnd" /> on wrap. Returns
    ///     <c>false</c> when no live units remain so the caller can exit the loop.
    /// </summary>
    /// <returns><c>true</c> if a live unit was found, <c>false</c> if everyone is dead.</returns>
    private bool AdvanceToNextLiveUnit()
    {
        int n = _unitsTurnOrder.Count;
        if (n == 0) return false;

        for (int step = 0; step < n; step++)
        {
            int next = _currentIndex + 1;
            if (next >= n)
            {
                next = 0;
                _turn++;
                OnCurrentTurnEnd?.Invoke();
            }
            _currentIndex = next;
            if (_unitsTurnOrder[_currentIndex].Key.IsAlive)
                return true;
        }

        return false; // walked the full ring, nobody alive
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
	public void InitializeTurnOrder(List<IUnitSystem> playerUnits, List<IUnitSystem> enemyUnits)
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
        // DO NOT use Task.Run here. ProcessTurn fires OnPlayerTurn/OnEnemyTurn events that
        // synchronously invoke GameManager handlers, which touch Godot scene/render APIs
        // (HUD widgets, indicator overlays, label text). Those APIs must run on the main
        // thread — calling them from a worker thread silently corrupts the renderer state
        // (CanvasLayer 2D content stops rendering even though 3D content keeps working).
        // `await` is enough — it yields to the engine on every await point so the main
        // loop keeps processing, no thread switch needed.
        await ProcessTurn();
    }

    /// <summary>
    /// Gets the unit currently taking its turn.
    /// </summary>
    /// <returns>The <see cref="IUnitSystem"/> that is currently active.</returns>
    public IUnitSystem GetCurrentUnit()
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
