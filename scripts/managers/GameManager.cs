using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.status_effects;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
/// Represents the overall game state during a level.
/// </summary>
public enum GameState
{
    /// <summary>Waiting for the battle to start or for a process to complete.</summary>
    Waiting,

    /// <summary>The player's turn is currently active.</summary>
    PlayerTurn,

    /// <summary>The enemy's turn is currently active.</summary>
    EnemyTurn
}

/// <summary>
/// Represents the outcome of a battle.
/// </summary>
public enum GameOutcome
{
    /// <summary>The battle is ongoing; no winner yet.</summary>
    Ongoing,

    /// <summary>The player has won the battle.</summary>
    Victory,

    /// <summary>The player has lost the battle.</summary>
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
    private readonly List<UnitSystem> _playerUnits = [];
    private readonly List<UnitSystem> _enemyUnits = [];
    private List<(int, int, int)> _currentUnitPossibleMoves = [];
    private SkillSystem? _selectedSkill;
    private UnitSystem? _selectedUnitForPlayedSkill;
    private StatusEffectSystem _statusEffectSystem = new();

    #endregion

    #region Godot Private Fields

    [Export]
    private NodePath? _playerUnitsPath;

    [Export]
    private NodePath? _enemyUnitsPath;

    [Export]
    private NodePath? _mapSystemPath;

    [Export]
    private NodePath? _turnManagerPath;

    [Export]
    private NodePath? _battleInputSystemPath;

    private Node? _playerUnitsContainer;
    private Node? _enemyUnitsContainer;
    private MapSystem? _mapSystemContainer;
    private TurnManager? _turnManagerContainer;
    private BattleInputSystem? _battleInputSystemContainer;

    #endregion

    #region Private Properties

    /// <summary>
    /// Singleton instance of the <see cref="GameManager"/>.
    /// Ensures that only one active instance exists at any given time.
    /// </summary>
    private new static GameManager? Instance { get; set; }

    #endregion

    #region Class initialization

    /// <inheritdoc/>
    public override void _Ready()
    {
        base._Ready();
        _ = StartBattleWhenReady();
    }

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

    /// <summary>
    /// Waits for the scene tree to be fully ready, then starts the battle loop.
    /// </summary>
    /// <returns>A task that completes once the battle has started.</returns>
    private async Task StartBattleWhenReady()
    {
        if (!IsInsideTree())
            await ToSignal(this, "ready");

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set");
            return;
        }

        GD.Print("All nodes should be ready. Start battle now.");
        _ = _turnManagerContainer.StartBattle();
    }

    /// <summary>
    /// Initializes references to all major systems and sets up initial bindings.
    /// </summary>
    /// <remarks>
    /// This method is called during <see cref="Initialize"/> to connect signals,
    /// set up turn events, load units, and prepare the map and input systems.
    /// </remarks>
    private void InitializeGameManager()
    {
        _battleInputSystemContainer = GetNode<BattleInputSystem>(_battleInputSystemPath);
        _battleInputSystemContainer.OnPassTurnPressed += PlayerUnitPassedTurn;
        _battleInputSystemContainer.OnMoveUnitToPressed += PlayerUnitMoved;
        _battleInputSystemContainer.OnSelectedSkillPressed += SelectSkill;
        _playerUnitsContainer = GetNode<Node>(_playerUnitsPath);
        _enemyUnitsContainer = GetNode<Node>(_enemyUnitsPath);
        LoadUnits();
        _mapSystemContainer = GetNode<MapSystem>(_mapSystemPath);
        _mapSystemContainer.PlaceUnits(_playerUnits, _enemyUnits);
        _turnManagerContainer = GetNode<TurnManager>(_turnManagerPath);
        _turnManagerContainer.OnPlayerTurn += ActivatePlayerUnit;
        _turnManagerContainer.OnPlayerEndTurn += DeactivatePlayerUnit;
        _turnManagerContainer.InitializeTurnOrder(_playerUnits, _enemyUnits);
        if (_enemyUnits.Contains(_turnManagerContainer.GetCurrentUnit()))
            DeactivatePlayerUnit();
        else
            ActivatePlayerUnit();
    }

    /// <summary>
    /// Loads all player and enemy units from their respective container nodes.
    /// </summary>
    /// <remarks>
    /// This function scans the Godot scene tree for <see cref="UnitSystem"/> nodes
    /// and stores references to them for battle management.
    /// </remarks>
    private void LoadUnits()
    {
        if (_playerUnitsContainer == null)
        {
            GD.PrintErr("PlayerUnitsContainer not set");
            return;
        }

        if (_enemyUnitsContainer == null)
        {
            GD.PrintErr("EnemyUnitsContainer not set");
            return;
        }

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

    /// <summary>
    /// Activate player unit (actions and inputs) at the start of their turn.
    /// </summary>
    private void ActivatePlayerUnit()
    {
        if (_battleInputSystemContainer == null)
        {
            GD.PrintErr("BattleInputSystemContainer not set in GameManager.");
            return;
        }

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            return;
        }

        if (_mapSystemContainer == null)
        {
            GD.PrintErr("MapSystemContainer not set in GameManager.");
            return;
        }

        if (_currentUnitPossibleMoves.Count == 0)
            _currentUnitPossibleMoves = _turnManagerContainer.GetCurrentUnit().GetPossibleMoves(_mapSystemContainer);
        GD.Print("Current Unit Possible Moves: " + string.Join(", ", _currentUnitPossibleMoves));
        _battleInputSystemContainer.SetInputEnabled(true);
        GD.Print("Activate input");
    }

    /// <summary>
    /// Disables player unit (actions and inputs) when their turn ends.
    /// </summary>
    private void DeactivatePlayerUnit()
    {
        if (_battleInputSystemContainer == null)
        {
            GD.PrintErr("BattleInputSystemContainer not set in GameManager.");
            return;
        }

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            return;
        }

        if (_mapSystemContainer == null)
        {
            GD.PrintErr("MapSystemContainer not set in GameManager.");
            return;
        }

        _currentUnitPossibleMoves.Clear();
        GD.Print("Deactivate input");
        _battleInputSystemContainer.SetInputEnabled(false);
    }

    private void SelectSkill(int skillId)
    {
        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            return;
        }

        if (skillId >= _turnManagerContainer.GetCurrentUnit().ActiveSkills.Count)
        {
            GD.PrintErr("Skill Id is out of Skill List");
            return;
        }

        _selectedSkill = _turnManagerContainer.GetCurrentUnit().ActiveSkills[skillId];
    }

    private void SelectTarget(UnitSystem unit)
    {
        List<UnitSystem> targetUnits = [];

        if (_selectedSkill == null)
        {
            GD.PrintErr("Skill not selected");
            return;
        }

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set");
            return;
        }

        switch (_selectedSkill.TargetType)
        {
            case TargetTypes.SingleAlly:
                if (!_playerUnits.Contains(unit))
                {
                    GD.PrintErr($"Player unit {unit.UnitName} not found.");
                    return;
                }

                targetUnits.Add(unit);
                _turnManagerContainer.GetCurrentUnit().Play(targetUnits, _mapSystemContainer, _selectedSkill);
                break;

            case TargetTypes.SingleEnemy:
                if (!_enemyUnits.Contains(unit))
                {
                    GD.PrintErr($"Enemy unit {unit.UnitName} not found.");
                    return;
                }

                targetUnits.Add(unit);
                _turnManagerContainer.GetCurrentUnit().Play(targetUnits, _mapSystemContainer, _selectedSkill);
                break;

            case TargetTypes.AllAllies:
                if (!_playerUnits.Contains(unit))
                {
                    GD.PrintErr($"Player unit {unit.UnitName} not found.");
                    return;
                }

                _turnManagerContainer.GetCurrentUnit().Play(_playerUnits, _mapSystemContainer, _selectedSkill);
                break;

            case TargetTypes.AllEnemies:
                if (!_enemyUnits.Contains(unit))
                {
                    GD.PrintErr($"Enemy unit {unit.UnitName} not found.");
                    return;
                }

                _turnManagerContainer.GetCurrentUnit().Play(_enemyUnits, _mapSystemContainer, _selectedSkill);
                break;
        }
    }

    /// <summary>
    /// Handles player unit pass turn input.
    /// </summary>
    /// <remarks>
    /// Called when <see cref="BattleInputSystem.OnPassTurnPressed"/> is triggered.
    /// </remarks>
    private void PlayerUnitPassedTurn()
    {
        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set");
            return;
        }

        _turnManagerContainer.GetCurrentUnit().PassTurn();
    }

    /// <summary>
    /// Handles player "move to" input.
    /// </summary>
    /// <remarks>
    /// Called when <see cref="BattleInputSystem.OnMoveUnitToPressed"/> is triggered.
    /// </remarks>
    private void PlayerUnitMoved(Vector3I cell)
    {
        if (_mapSystemContainer == null)
        {
            GD.PrintErr("MapSystemContainer not set in GameManager.");
            return;
        }

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            return;
        }

        if (!_currentUnitPossibleMoves.Contains((cell.X, cell.Y, cell.Z)))
        {
            ActivatePlayerUnit();
            return;
        }

        try
        {
            if (!_turnManagerContainer.GetCurrentUnit().CanMoveTo(cell.X, cell.Y, cell.Z, _mapSystemContainer))
            {
                ActivatePlayerUnit();
                return;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            ActivatePlayerUnit();
            return;
        }

        // TODO: Handle the unit moves in the ui and not just teleport the unit
        Vector3I pos = new(cell.X, cell.Y, cell.Z);
        Vector3 worldPos = _mapSystemContainer.MapToLocal(pos);
        worldPos.Y += _mapSystemContainer.CellSize.Y * 0.5f;
        _turnManagerContainer.GetCurrentUnit().GlobalPosition = worldPos;

        GD.Print("Player unit moved");
        _turnManagerContainer.GetCurrentUnit().MoveTo(cell.X, cell.Y, cell.Z, _mapSystemContainer);
    }

    #endregion
}
