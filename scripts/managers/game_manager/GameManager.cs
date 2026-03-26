using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.Managers;

public partial class GameManager : BaseManager
{
    public EnemyAIManager? AIManager { get; private set; }

    #region Private Fields

    private AovDataStructures.GameOutcome _gameOutcome = AovDataStructures.GameOutcome.Ongoing;
    private bool _isPlayerTurn;
    private bool _unitMoved;
    private AovDataStructures.ClickOnMapContext _clickOnMapContext = AovDataStructures.ClickOnMapContext.MoveUnit;
    private readonly List<UnitSystem> _playerUnits = new List<UnitSystem>();
    private readonly List<UnitSystem> _enemyUnits = new List<UnitSystem>();
    private List<Vector3I> _currentUnitPossibleMoves = new List<Vector3I>();
    private List<Vector3I> _currentUnitReachableCellsForCurrentSelectedSkill = new List<Vector3I>();
    private SkillSystem? _selectedSkill;
    private UnitSystem? _selectedUnitForPlayedSkill;
    private readonly StatusEffectSystem _statusEffectSystem = new();

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

    /// <summary>
    /// Toggle threat map visualization for all enemy units.
    /// Call this from a debug input or console command.
    /// </summary>
    [Export]
    public bool EnableThreatMapDebug { get; set; } = false;

    private Node? _playerUnitsContainer;
    private Node? _enemyUnitsContainer;
    private MapSystem? _mapSystemContainer;
    private TurnManager? _turnManagerContainer;
    private BattleInputSystem? _battleInputSystemContainer;

    #endregion

    #region Private Properties

    /// <summary>
    ///     Singleton instance of the <see cref="GameManager" />.
    ///     Ensures that only one active instance exists at any given time.
    /// </summary>
    private new static GameManager? Instance { get; set; }

    #endregion

    #region Class initialization

    /// <inheritdoc />
    public override void _Ready()
    {
        base._Ready();
        _ = StartBattleWhenReady();
    }

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

    /// <summary>
    ///     Initializes references to all major systems and sets up initial bindings.
    /// </summary>
    /// <remarks>
    ///     This method is called during <see cref="Initialize" /> to connect signals,
    ///     set up turn events, load units, and prepare the map and input systems.
    /// </remarks>
    private void InitializeGameManager()
    {
        _battleInputSystemContainer = GetNode<BattleInputSystem>(_battleInputSystemPath);
        _battleInputSystemContainer.OnPassTurnPressed += PlayerPassedUnitTurn;
        _battleInputSystemContainer.OnMoveUnitOrSelectTargetPressed += PlayerMovedUnitOrSelectedTarget;
        _battleInputSystemContainer.OnSelectedSkillPressed += PlayerSelectedSkill;
        _battleInputSystemContainer.OnSelectMovePressed += PlayerSelectedMove;
        _playerUnitsContainer = GetNode<Node>(_playerUnitsPath);
        _enemyUnitsContainer = GetNode<Node>(_enemyUnitsPath);

        LoadUnits();

        _mapSystemContainer = GetNode<MapSystem>(_mapSystemPath);
        _mapSystemContainer.InjectDependencies(_statusEffectSystem);
        _mapSystemContainer.PlaceUnits(_playerUnits, _enemyUnits);
        _turnManagerContainer = GetNode<TurnManager>(_turnManagerPath);
        _turnManagerContainer.OnPlayerTurn += ActivatePlayerUnit;
        _turnManagerContainer.OnPlayerTurnEnd += DeactivatePlayerUnit;
        _turnManagerContainer.OnEnemyTurnEnd += EnemyTurnEnded;
        _turnManagerContainer.OnCurrentTurnEnd += CurrentTurnEnded;
        _turnManagerContainer.InitializeTurnOrder(_playerUnits, _enemyUnits);

        // Debug: show unit counts to help diagnose empty turn order
        GD.Print($"[DEBUG] Players: {_playerUnits.Count}, Enemies: {_enemyUnits.Count}");

        bool hasUnits = (_playerUnits.Count + _enemyUnits.Count) > 0;

        if (hasUnits)
        {
            try
            {
                if (_enemyUnits.Contains(_turnManagerContainer.GetCurrentUnit()))
                    DeactivatePlayerUnit();
                else
                    ActivatePlayerUnit();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"TurnManager.GetCurrentUnit() failed: {ex.Message}");
                // Fallback: ensure player unit is deactivated to avoid undefined state
                DeactivatePlayerUnit();
            }
        }
        else
        {
            GD.PrintErr("No units found; skipping turn activation.");
        }

        // Create EnemyAIManager as a battle-scoped instance (NOT a global singleton)
        AIManager = new EnemyAIManager(this);
        AIManager.SetUnitReferences(_playerUnits, _enemyUnits);

        if (_mapSystemContainer != null)
            AIManager.SetMapSystem(_mapSystemContainer);
        else
            GD.PrintErr("EnemyAIManager: MapSystem not available when setting up AI manager");

        _turnManagerContainer.SetAIManager(AIManager);
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Waits for the scene tree to be fully ready, then starts the battle loop.
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
    ///     Activate player unit (actions and inputs) at the start of their turn.
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

        _isPlayerTurn = true;
        if (_currentUnitPossibleMoves.Count == 0)
            _currentUnitPossibleMoves = _turnManagerContainer.GetCurrentUnit().GetPossibleMoves(_mapSystemContainer);
        GD.Print("Current Unit Possible Moves: " + string.Join(", ", _currentUnitPossibleMoves));
        _battleInputSystemContainer.SetInputEnabled(true);
        GD.Print("Activate input");
    }

    /// <summary>
    ///     Disables player unit (actions and inputs) when their turn ends.
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

        _clickOnMapContext = AovDataStructures.ClickOnMapContext.MoveUnit;
        _isPlayerTurn = false;
        _selectedSkill = null;
        _unitMoved = false;
        _currentUnitPossibleMoves.Clear();
        _currentUnitReachableCellsForCurrentSelectedSkill.Clear();
        _battleInputSystemContainer.SetInputEnabled(false);
        GD.Print("Deactivate input and player units");
        _statusEffectSystem.ProcessUnitTurnEnd(_turnManagerContainer.GetCurrentUnit());
        CheckUnitTurnEnd();
    }

    /// <summary>
    ///     Handles player unit select skill input.
    /// </summary>
    /// <remarks>
    ///     Called when <see cref="BattleInputSystem.OnSelectedSkillPressed" /> is triggered.
    /// </remarks>
    private void PlayerSelectedSkill(int skillId)
    {
        _currentUnitReachableCellsForCurrentSelectedSkill.Clear();
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

        if (skillId >= _turnManagerContainer.GetCurrentUnit().ActiveSkills.Count)
        {
            GD.PrintErr("Skill Id is out of Skill List");
            return;
        }

        if (_turnManagerContainer.GetCurrentUnit().ActiveSkills[skillId].Cooldown != 0)
        {
            GD.PrintErr("The Skill cannot be used yet.");
            return;
        }

        GD.Print($"Selected Skill {skillId}");
        _clickOnMapContext = AovDataStructures.ClickOnMapContext.SelectUnitTarget;
        _selectedSkill = _turnManagerContainer.GetCurrentUnit().ActiveSkills[skillId];
        var reachableTuples = _turnManagerContainer
            .GetCurrentUnit()
            .GetReachableCellsForSkills(_mapSystemContainer, _selectedSkill);
        _currentUnitReachableCellsForCurrentSelectedSkill = reachableTuples.ConvertAll(t => new Vector3I(t.X, t.Y, t.Z));
        GD.Print(
            "Current Unit Reachable cells: " + string.Join(", ", _currentUnitReachableCellsForCurrentSelectedSkill)
        );
    }

    /// <summary>
    ///     Handles player unit pass turn input.
    /// </summary>
    /// <remarks>
    ///     Called when <see cref="BattleInputSystem.OnPassTurnPressed" /> is triggered.
    /// </remarks>
    private void PlayerPassedUnitTurn()
    {
        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set");
            return;
        }

        _statusEffectSystem.ProcessUnitTurnEnd(_turnManagerContainer.GetCurrentUnit());
        _turnManagerContainer.GetCurrentUnit().PassTurn();
        CheckUnitTurnEnd();
    }

    /// <summary>
    ///     Handles input when the player selects the “Move” action.
    /// </summary>
    private void PlayerSelectedMove()
    {
        if (_unitMoved)
            return;
        GD.Print("Selected move action.");
        _clickOnMapContext = AovDataStructures.ClickOnMapContext.MoveUnit;
    }

    /// <summary>
    ///     Determines what to do when the player clicks on the map —
    ///     either move a unit or select a skill target depending on the current context.
    /// </summary>
    /// <param name="cell">The grid cell clicked by the player.</param>
    private void PlayerMovedUnitOrSelectedTarget(Vector3I cell)
    {
        if (_battleInputSystemContainer == null)
        {
            GD.PrintErr("BattleInputSystemContainer not set in GameManager.");
            return;
        }

        if (_mapSystemContainer == null)
        {
            GD.PrintErr("MapSystemContainer not set in GameManager.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        switch (_clickOnMapContext)
        {
            case AovDataStructures.ClickOnMapContext.MoveUnit:
                HandlePlayerUnitMove(cell);
                break;
            case AovDataStructures.ClickOnMapContext.SelectUnitTarget:
                HandlePlayerSelectTarget(cell);
                break;
        }
    }

    /// <summary>
    ///     Resets movement flags when the enemy turn ends.
    /// </summary>
    private void EnemyTurnEnded()
    {
        if (_turnManagerContainer is null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            return;
        }

        _unitMoved = false;
        _statusEffectSystem.ProcessUnitTurnEnd(_turnManagerContainer.GetCurrentUnit());
        CheckUnitTurnEnd();
    }

    /// <summary>
    ///     Handles all logic that should occur at the end of the current turn.
    /// </summary>
    private void CurrentTurnEnded()
    {
        foreach (UnitSystem unit in _playerUnits)
        {
            foreach (SkillSystem skill in unit.ActiveSkills)
                skill.ReduceCooldown();
        }

        foreach (UnitSystem unit in _enemyUnits)
        {
            foreach (SkillSystem skill in unit.ActiveSkills)
                skill.ReduceCooldown();
        }

        _statusEffectSystem.ProcessTurnEnd();
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Moves the currently active unit to the specified cell on the map.
    /// </summary>
    /// <param name="cell">
    ///     The target cell position as a <see cref="Vector3I" /> (grid coordinates X, Y, Z).
    /// </param>
    /// <remarks>
    ///     Checks that both <c>_mapSystemContainer</c> and <c>_turnManagerContainer</c>
    ///     are properly set before performing the move.
    ///     If valid, the active unit is repositioned in the world based on the given
    ///     cell coordinates and the map’s cell size.
    /// </remarks>
    /// <example>
    ///     <code>
    /// Vector3I targetCell = new Vector3I(2, 0, 3);
    /// gameManager.MoveUnit(targetCell);
    /// </code>
    /// </example>
    public virtual void MoveUnit((int, int,int) cell)
    {
        if (_mapSystemContainer == null)
        {
            GD.PrintErr("MapSystemContainer not set in GameManager.");
            if (_isPlayerTurn)
                _battleInputSystemContainer?.SetInputEnabled(true);
            return;
        }

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            if (_isPlayerTurn)
                _battleInputSystemContainer?.SetInputEnabled(true);
            return;
        }

        if (_unitMoved)
        {
            GD.Print("Unit Already Moved");
            if (_isPlayerTurn)
                _battleInputSystemContainer?.SetInputEnabled(true);
            return;
        }

        // TODO: Replace by the unit move animation instead of teleport him
        Vector3I pos = new(cell.Item1, cell.Item2, cell.Item3);
        Vector3 worldPos = _mapSystemContainer.MapToLocal(pos);
        worldPos.Y += _mapSystemContainer.CellSize.Y * 0.5f;
        _turnManagerContainer.GetCurrentUnit().GlobalPosition = worldPos;

        _turnManagerContainer.GetCurrentUnit().MoveTo(cell.Item1, cell.Item2, cell.Item3, _mapSystemContainer);
        _unitMoved = true;
        GD.Print("Unit moved");
    }

    /// <summary>
    ///     Uses a skill from a source unit on a target unit, depending on the skill’s target type.
    /// </summary>
    /// <param name="sourceUnit">
    ///     The unit that performs the skill.
    /// </param>
    /// <param name="targetUnit">
    ///     The main target unit of the skill.
    /// </param>
    /// <param name="skill">
    ///     The skill being used, containing information about its target type and effects.
    /// </param>
    /// <remarks>
    ///     Determines which units are allies or enemies based on the source unit,
    ///     then applies the skill accordingly depending on its <c>TargetType</c>.
    ///     If required containers are not initialized, the method prints an error
    ///     and exits early.
    /// </remarks>
    /// <example>
    ///     <code>
    /// SkillSystem healSkill = new SkillSystem("Heal", TargetTypes.SingleAlly);
    /// gameManager.UseSkill(playerUnit, allyUnit, healSkill);
    /// </code>
    /// </example>
    public virtual void UseSkill(UnitSystem sourceUnit, UnitSystem targetUnit, SkillSystem skill)
    {
        List<UnitSystem> allyUnits = new List<UnitSystem>();
        List<UnitSystem> enemyUnits = new List<UnitSystem>();
        List<UnitSystem> targetUnits = new List<UnitSystem>();

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            if (_isPlayerTurn)
                _battleInputSystemContainer?.SetInputEnabled(true);
            return;
        }

        if (_playerUnits.Contains(sourceUnit))
        {
            allyUnits = _playerUnits;
            enemyUnits = _enemyUnits;
        }
        else
        {
            allyUnits = _enemyUnits;
            enemyUnits = _playerUnits;
        }

        switch (skill.TargetType)
        {
            case AovDataStructures.TargetTypes.SingleAlly:
                if (!allyUnits.Contains(targetUnit))
                {
                    GD.PrintErr($"Ally unit {targetUnit.UnitName} not found.");
                    if (_isPlayerTurn)
                        _battleInputSystemContainer?.SetInputEnabled(true);
                    return;
                }

                targetUnits.Add(targetUnit);
                _selectedSkill?.SetCooldown();
                _turnManagerContainer.GetCurrentUnit().Play(targetUnits, _mapSystemContainer, skill);
                break;

            case AovDataStructures.TargetTypes.SingleEnemy:
                if (!enemyUnits.Contains(targetUnit))
                {
                    GD.PrintErr($"Enemy unit {targetUnit.UnitName} not found.");
                    if (_isPlayerTurn)
                        _battleInputSystemContainer?.SetInputEnabled(true);
                    return;
                }

                targetUnits.Add(targetUnit);
                _selectedSkill?.SetCooldown();
                _turnManagerContainer.GetCurrentUnit().Play(targetUnits, _mapSystemContainer, skill);
                break;

            case AovDataStructures.TargetTypes.AllAllies:
                if (!allyUnits.Contains(targetUnit))
                {
                    GD.PrintErr($"Ally unit {targetUnit.UnitName} not found.");
                    if (_isPlayerTurn)
                        _battleInputSystemContainer?.SetInputEnabled(true);
                    return;
                }

                _selectedSkill?.SetCooldown();
                _turnManagerContainer.GetCurrentUnit().Play(allyUnits, _mapSystemContainer, skill);
                break;

            case AovDataStructures.TargetTypes.AllEnemies:
                if (!enemyUnits.Contains(targetUnit))
                {
                    GD.PrintErr($"Enemy unit {targetUnit.UnitName} not found.");
                    if (_isPlayerTurn)
                        _battleInputSystemContainer?.SetInputEnabled(true);
                    return;
                }

                _selectedSkill?.SetCooldown();
                _turnManagerContainer.GetCurrentUnit().Play(enemyUnits, _mapSystemContainer, skill);
                break;
        }
    }

    #endregion

    #region Debug Methods

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        // Press F1 to toggle threat map visualization
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.F1)
            {
                EnableThreatMapDebug = !EnableThreatMapDebug;
                GD.Print($"Threat Map Debug: {(EnableThreatMapDebug ? "ON" : "OFF")}");

                if (EnableThreatMapDebug)
                {
                    ShowAllThreatMaps();
                }
            }

            // Press F2 to show action scores for current AI unit
            if (keyEvent.Keycode == Key.F2)
            {
                ShowCurrentAIActionScores();
            }
        }
    }

    /// <summary>
    /// Visualizes threat maps for all enemy units.
    /// </summary>
    private void ShowAllThreatMaps()
    {
        if (_mapSystemContainer == null || AIManager == null)
        {
            GD.PrintErr("Cannot show threat maps - systems not initialized");
            return;
        }

        foreach (var enemy in _enemyUnits)
        {
            if (!enemy.IsAlive)
                continue;

            // Find the AIDebugVisualizer for this enemy
            var aiBehavior = FindAIBehavior(enemy);
            if (aiBehavior != null)
            {
                var visualizer = aiBehavior.GetNodeOrNull<AIDebugVisualizer>("AIDebugVisualizer");
                if (visualizer != null)
                {
                    var battleState = new BattleState
                    {
                        ActingUnit = enemy,
                        MapSystem = _mapSystemContainer,
                        PlayerUnits = AIManager.GetAlivePlayerUnits(),
                        EnemyUnits = AIManager.GetAliveEnemyUnits(),
                        GameManager = this
                    };

                    visualizer.VisualizeThreatMap(enemy, battleState, 5);
                    GD.Print($"Showing threat map for {enemy.Name}");
                }
            }
        }
    }

    /// <summary>
    /// Shows action scores for the current unit if it's an AI unit.
    /// </summary>
    private void ShowCurrentAIActionScores()
    {
        if (_turnManagerContainer == null || _mapSystemContainer == null || AIManager == null)
        {
            GD.PrintErr("Cannot show action scores - systems not initialized");
            return;
        }

        var currentUnit = _turnManagerContainer.GetCurrentUnit();

        // Only works for enemy units
        if (!_enemyUnits.Contains(currentUnit))
        {
            GD.Print("Current unit is not an enemy - no AI to visualize");
            return;
        }

        var aiBehavior = FindAIBehavior(currentUnit);
        if (aiBehavior == null)
        {
            GD.PrintErr($"No AI behavior found for {currentUnit.Name}");
            return;
        }

        // This would require making some methods public in AIDecisionGenerator
        // For now, just enable the visualizer and let it show on next turn
        GD.Print("Action scores will be shown automatically if debug visualization is enabled");
    }

    /// <summary>
    /// Finds the EnemyAIBehavior component attached to a unit.
    /// </summary>
    private EnemyAIBehavior? FindAIBehavior(UnitSystem unit)
    {
        foreach (Node child in unit.GetChildren())
        {
            if (child is EnemyAIBehavior behavior)
                return behavior;

            // Also check children of children (in case it's under a Node)
            foreach (Node grandchild in child.GetChildren())
            {
                if (grandchild is EnemyAIBehavior behaviorNested)
                    return behaviorNested;
            }
        }
        return null;
    }

    #endregion
}
