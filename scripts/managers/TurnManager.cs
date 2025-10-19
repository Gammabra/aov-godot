using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems;
using Godot;

namespace AshesOfVelsingrad.Managers;

public enum TurnState
{
    PlayerTurn,
    EnemyTurn,
    Waiting
}

public partial class TurnManager : BaseManager
{
    #region Private Fields

    private TurnState _currentTurnState = TurnState.Waiting;
    private int _turn;
    private List<KeyValuePair<UnitSystem, TurnState>> _unitsTurnOrder = [];
    private int _currentIndex;

    #endregion

    #region Public Properties

    private new static TurnManager? Instance { get; set; }

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
        _ = StartBattle();
    }

    #endregion

    #region Private Methods

    private async Task StartBattle()
    {
        GD.Print("Starting Battle");
        _currentTurnState = _unitsTurnOrder[_currentIndex].Value;
        _turn++;
        await ProcessTurn();
    }

    private async Task ProcessTurn()
    {
        while (true)
        {
            GD.Print($"{_unitsTurnOrder[_currentIndex].Key.Name} turn");
            switch (_currentTurnState)
            {
                case TurnState.PlayerTurn:
                    await WaitForPlayerAction(_unitsTurnOrder[_currentIndex].Key);
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

    private static Task WaitForPlayerAction(UnitSystem unit)
    {
        TaskCompletionSource tcs = new();

        unit.ActionCompleted += () => tcs.SetResult();
        return tcs.Task;
    }

    private static async Task WaitForEnemyAction(UnitSystem unit)
    {
        GD.Print($"{unit.Name} start thinking...");
        await Task.Delay(1000); // TODO: Replace by the real ai method
        GD.Print($"{unit.Name} played.");
    }

    #endregion

    #region Public Methods

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

    #endregion
}
