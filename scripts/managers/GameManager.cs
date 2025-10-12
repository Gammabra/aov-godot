using System.Collections.Generic;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.status_effects;
using Godot;

namespace AshesOfVelsingrad.Managers;

public enum GameState
{
    Waiting,
    PlayerTurn,
    EnemyTurn
}

public enum GameOutcome
{
    Ongoing,
    Victory,
    Defeat
}

/// <summary>
///     The conductor of a level. The <c>GameManager</c> handles everything that a level needs to work correctly.
/// </summary>
public partial class GameManager : BaseManager
{
    #region Private Fields

    private GameState _gameState = GameState.Waiting;
    private GameOutcome _gameOutcome = GameOutcome.Ongoing;
    private List<UnitSystem> _playerUnits = [];
    private List<UnitSystem> _enemyUnits = [];
    private StatusEffectSystem _statusEffectSystem;

    #endregion

    #region Godot Private Fields

    [Export]
    private NodePath _playerUnitsPath;

    [Export]
    private NodePath _enemyUnitsPath;

    [Export]
    private NodePath _mapSystemPath;

    [Export]
    private NodePath _turnManagerPath;

    private Node _playerUnitsContainer;
    private Node _enemyUnitsContainer;
    private MapSystem _mapSystemContainer;
    private TurnManager _turnManagerContainer;

    #endregion

    #region Public Properties

    public new static GameManager? Instance { get; private set; }

    #endregion

    #region Class initialization

    /// <summary>
    ///     Initializes the GameManager singleton instance.
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
        InitializeGameManager();
        GD.Print("GameManager initialized successfully");
    }

    #endregion

    #region Private Methods

    private void InitializeGameManager()
    {
        _playerUnitsContainer = GetNode<Node>(_playerUnitsPath);
        _enemyUnitsContainer = GetNode<Node>(_enemyUnitsPath);
        _mapSystemContainer = GetNode<MapSystem>(_mapSystemPath);
        _mapSystemContainer.PlaceUnits(_playerUnits, _enemyUnits);
        _turnManagerContainer = GetNode<TurnManager>(_turnManagerPath);
        _turnManagerContainer.InitializeTurnOrder(_playerUnits, _enemyUnits);
        LoadUnits();
    }

    private void LoadUnits()
    {
        _playerUnits.Clear();
        _enemyUnits.Clear();

        foreach (Node child in _playerUnitsContainer.GetChildren())
            if (child is UnitSystem unit)
                _playerUnits.Add(unit);

        foreach (Node child in _enemyUnitsContainer.GetChildren())
            if (child is UnitSystem unit)
                _enemyUnits.Add(unit);

        GD.Print($"Players count : {_playerUnits.Count} | Enemies count : {_enemyUnits.Count}");
    }

    #endregion
}
