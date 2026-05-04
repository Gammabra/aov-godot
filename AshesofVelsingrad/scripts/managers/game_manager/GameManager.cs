using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Systems.Battle;
using AshesOfVelsingrad.UI.Hud;
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
    private readonly List<IUnitSystem> _playerUnits = new List<IUnitSystem>();
    private readonly List<IUnitSystem> _allyUnits = new List<IUnitSystem>();
    private readonly List<IUnitSystem> _enemyUnits = new List<IUnitSystem>();
    private List<(int, int, int)> _currentUnitPossibleMoves = new List<(int, int, int)>();
    private List<Vector3I> _currentUnitReachableCellsForCurrentSelectedSkill = new List<Vector3I>();
    private ISkillSystem? _selectedSkill;
    private readonly StatusEffectSystem _statusEffectSystem = new();

    #endregion

    #region Godot Private Fields

    [Export]
    private NodePath? _playerUnitsPath;

    [Export]
    private NodePath? _enemyUnitsPath;

    /// <summary>
    ///     Optional sibling container for AI-controlled friendly guest units (recruited mercs,
    ///     summoned creatures, scripted helpers). Leave empty for battles with no allies.
    /// </summary>
    [Export]
    private NodePath? _alliedUnitsPath;

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
    private Node? _alliedUnitsContainer;
    private IMapSystem? _mapSystemContainer;
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

    /// <inheritdoc />
    /// <remarks>
    ///     Clear the static singleton when this node leaves the tree (scene unload, scene
    ///     reload). Without this, the new <see cref="GameManager" /> spawned by the next
    ///     scene sees a stale <c>Instance</c> and QueueFrees itself as a duplicate.
    /// </remarks>
    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
        // Also free the end-screens we may have spawned so they don't outlive the scene.
        if (_victoryScreen is not null && IsInstanceValid(_victoryScreen)) _victoryScreen.QueueFree();
        if (_gameOverScreen is not null && IsInstanceValid(_gameOverScreen)) _gameOverScreen.QueueFree();
        if (_battleHud is not null && IsInstanceValid(_battleHud) && _battleHud.GetParent() == GetTree().Root)
            _battleHud.QueueFree();
        base._ExitTree();
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
        if (_alliedUnitsPath is not null && !_alliedUnitsPath.IsEmpty)
            _alliedUnitsContainer = GetNodeOrNull<Node>(_alliedUnitsPath);

        LoadUnits();

        _mapSystemContainer = GetNode<MapSystem>(_mapSystemPath);
        _mapSystemContainer.InjectDependencies(_statusEffectSystem);
        // PlaceUnits only distinguishes friendlies vs hostiles — merge allies into the
        // friendly list so they get placed alongside the player party.
        List<IUnitSystem> friendlies = new(_playerUnits.Count + _allyUnits.Count);
        friendlies.AddRange(_playerUnits);
        friendlies.AddRange(_allyUnits);
        _mapSystemContainer.PlaceUnits(friendlies, _enemyUnits);

        // HUD + indicator overlays must exist BEFORE the first ActivatePlayerUnit call,
        // otherwise the initial move tiles never get drawn and the status / context panels
        // never get bound. The BattleHud's child widgets still need a deferred
        // refresh once their _Ready has fired (see RefreshHudOnReady below).
        EnsureHud();
        EnsureIndicators();

        _turnManagerContainer = GetNode<TurnManager>(_turnManagerPath);
        _turnManagerContainer.OnPlayerTurn += ActivatePlayerUnit;
        _turnManagerContainer.OnPlayerTurnEnd += DeactivatePlayerUnit;
        _turnManagerContainer.OnEnemyTurn += EnemyTurnStarted;
        _turnManagerContainer.OnEnemyTurnEnd += EnemyTurnEnded;
        _turnManagerContainer.OnAllyTurn += AllyTurnStarted;
        _turnManagerContainer.OnAllyTurnEnd += AllyTurnEnded;
        _turnManagerContainer.OnCurrentTurnEnd += CurrentTurnEnded;
        _turnManagerContainer.InitializeTurnOrder(_playerUnits, _allyUnits, _enemyUnits);

        // Debug: show unit counts to help diagnose empty turn order
        GD.Print($"[DEBUG] Players: {_playerUnits.Count}, Allies: {_allyUnits.Count}, Enemies: {_enemyUnits.Count}");

        bool hasUnits = (_playerUnits.Count + _allyUnits.Count + _enemyUnits.Count) > 0;

        if (hasUnits)
        {
            try
            {
                IUnitSystem first = _turnManagerContainer.GetCurrentUnit();
                if (_playerUnits.Contains(first))
                    ActivatePlayerUnit();
                else
                    DeactivatePlayerUnit();
            }
            catch (Exception ex)
            {
                // The whole try-block is wrapped, so this catch can fire from anywhere
                // inside ActivatePlayerUnit / DeactivatePlayerUnit too — log the type and
                // a stack trace so the next failure is debuggable instead of misleading.
                GD.PrintErr($"Initial-turn activation failed [{ex.GetType().Name}]: {ex.Message}\n{ex.StackTrace}");
                // Fallback: ensure player unit is deactivated to avoid undefined state
                DeactivatePlayerUnit();
            }
        }
        else
        {
            GD.PrintErr("No units found; skipping turn activation.");
        }

        // Create EnemyAIManager as a battle-scoped instance (NOT a global singleton)
        // Allies are valid targets for hostile AI, so they go into the "friendly" list here.
        AIManager = new EnemyAIManager(this);
        AIManager.SetUnitReferences(friendlies, _enemyUnits);

        if (_mapSystemContainer != null)
            AIManager.SetMapSystem(_mapSystemContainer);
        else
            GD.PrintErr("EnemyAIManager: MapSystem not available when setting up AI manager");

        _turnManagerContainer.SetAIManager(AIManager);

        // BattleHud._Ready (and therefore its child widget references) only fires next frame
        // after AddChild. Defer everything that touches those children.
        CallDeferred(nameof(WireHudEvents));
        CallDeferred(nameof(BindHudRosters));
        CallDeferred(nameof(RefreshHudOnReady));
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

        IUnitSystem activeUnit = _turnManagerContainer.GetCurrentUnit();
        _statusEffectSystem.ProcessUnitStatusEffects(activeUnit);
        _isPlayerTurn = true;
        if (_currentUnitPossibleMoves.Count == 0)
            _currentUnitPossibleMoves = activeUnit.GetPossibleMoves(_mapSystemContainer);
        GD.Print("Current Unit Possible Moves: " + string.Join(", ", _currentUnitPossibleMoves));
        BattleNotifications.Post(
            $"{activeUnit.UnitName}'s turn — HP {activeUnit.Hp:F0}/{activeUnit.MaxHp:F0}, MP {activeUnit.Mana:F0}/{activeUnit.ManaMax:F0}",
            BattleNotifications.Severity.Info);
        _battleInputSystemContainer.SetInputEnabled(true);
        GD.Print("Activate input");
        RefreshHudForActiveUnit(_turnManagerContainer.GetCurrentUnit());
        ShowMoveIndicators(_currentUnitPossibleMoves);
        _battleHud?.ContextInfo?.ShowMovement(_currentUnitPossibleMoves.Count,
            _turnManagerContainer.GetCurrentUnit().PossibleMovesRange, canMove: !_unitMoved);
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
        HideAllIndicators();
        _battleHud?.ActionMenu?.ShowCancel(false);
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

        GD.Print($"Selected Skill {skillId + 1}");
        _clickOnMapContext = AovDataStructures.ClickOnMapContext.SelectUnitTarget;
        IUnitSystem caster = _turnManagerContainer.GetCurrentUnit();
        _selectedSkill = caster.ActiveSkills[skillId];
        var reachableTuples = caster.GetReachableCellsForSkills(_mapSystemContainer, _selectedSkill);

        // Only show red tiles where the skill can legally land — i.e. cells whose occupant
        // matches the skill's TargetType vs caster faction. Damage skills won't show on
        // self/allies; buff/heal skills won't show on enemies. Empty cells are kept for
        // AOE skills that can target empty ground.
        _currentUnitReachableCellsForCurrentSelectedSkill = new List<Vector3I>();
        foreach ((int x, int y, int z) in reachableTuples)
        {
            if (CellMatchesSkillTarget(caster, _selectedSkill, x, y, z))
                _currentUnitReachableCellsForCurrentSelectedSkill.Add(new Vector3I(x, y, z));
        }

        GD.Print(
            "Current Unit Reachable cells: " + string.Join(", ", _currentUnitReachableCellsForCurrentSelectedSkill)
        );
        ShowTargetIndicators(_currentUnitReachableCellsForCurrentSelectedSkill);
        _battleHud?.ContextInfo?.ShowSkill(_selectedSkill);
        _battleHud?.ActionMenu?.ShowCancel(true);
    }

    /// <summary>
    ///     Returns true when the cell at (<paramref name="x" />, <paramref name="y" />,
    ///     <paramref name="z" />) is a legal landing spot for <paramref name="skill" /> from
    ///     <paramref name="caster" />'s point of view.
    /// </summary>
    /// <remarks>
    ///     Damage / control / debuff skills only highlight cells with hostile occupants;
    ///     heal / buff / revive skills only highlight friendly occupants (including the
    ///     caster for self-castable skills with <c>TargetTypes.SingleAlly</c> at range 0).
    ///     Empty cells are allowed for both groups because some skills (terrain, AoE)
    ///     legitimately target ground.
    /// </remarks>
    private bool CellMatchesSkillTarget(IUnitSystem caster, ISkillSystem skill, int x, int y, int z)
    {
        if (_mapSystemContainer is null) return false;

        // Per-skill cell-level rule (cardinal-only Charge, line-of-sight, cone, …).
        if (!skill.IsTargetCellValid(caster, x, y, z, _mapSystemContainer)) return false;

        IUnitSystem? occupant;
        try { occupant = _mapSystemContainer.GetUnitAt(x, y, z); }
        catch (ArgumentOutOfRangeException) { return false; }
        if (occupant is null) return true; // empty ground is fine for AOE / terrain skills

        return skill.TargetType switch
        {
            AovDataStructures.TargetTypes.SingleEnemy or AovDataStructures.TargetTypes.AllEnemies
                => caster.Faction.IsHostileTo(occupant.Faction),
            AovDataStructures.TargetTypes.SingleAlly or AovDataStructures.TargetTypes.AllAllies
                => caster.Faction.IsFriendlyTo(occupant.Faction),
            _ => true,
        };
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
                HandlePlayerUnitMove((cell.X, cell.Y, cell.Z));
                break;
            case AovDataStructures.ClickOnMapContext.SelectUnitTarget:
                HandlePlayerSelectTarget((cell.X, cell.Y, cell.Z));
                break;
        }
    }

    private void EnemyTurnStarted()
    {
        if (_turnManagerContainer is null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            return;
        }

        IUnitSystem activeUnit = _turnManagerContainer.GetCurrentUnit();
        _statusEffectSystem.ProcessUnitStatusEffects(activeUnit);
        bool isAlly = _allyUnits.Contains(activeUnit);
        BattleNotifications.Post(
            $"{activeUnit.UnitName}'s turn ({(isAlly ? "Ally" : "Enemy")})",
            isAlly ? BattleNotifications.Severity.Positive : BattleNotifications.Severity.Negative);
        RefreshHudForActiveUnit(activeUnit);
    }

    private void AllyTurnStarted() => EnemyTurnStarted();

    private void AllyTurnEnded() => EnemyTurnEnded();

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
        CheckUnitTurnEnd();
    }

    /// <summary>
    ///     Handles all logic that should occur at the end of the current turn.
    /// </summary>
    private void CurrentTurnEnded()
    {
        foreach (IUnitSystem unit in _playerUnits)
        {
            foreach (ISkillSystem skill in unit.ActiveSkills)
                skill.ReduceCooldown();
        }

        foreach (IUnitSystem unit in _enemyUnits)
        {
            foreach (ISkillSystem skill in unit.ActiveSkills)
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
    public virtual void MoveUnit((int, int, int) cell)
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

        // Animate via A* + tween instead of teleporting. Logical state updates immediately
        // (MoveTo) so subsequent decisions don't see a stale grid position; visuals trail.
        _ = AnimateUnitMove(cell);
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
    public virtual void UseSkill(IUnitSystem sourceUnit, IUnitSystem targetUnit, ISkillSystem skill)
    {
        BattleNotifications.Post(
            $"{sourceUnit.UnitName} uses [b]{skill.Name}[/b] on {targetUnit.UnitName}",
            BattleNotifications.Severity.Info);
        List<IUnitSystem> allyUnits = new List<IUnitSystem>();
        List<IUnitSystem> enemyUnits = new List<IUnitSystem>();
        List<IUnitSystem> targetUnits = new List<IUnitSystem>();

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
            }

            // Press F2 to show action scores for current AI unit
            if (keyEvent.Keycode == Key.F2)
            {
                ShowCurrentAIActionScores();
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
            GD.PrintErr($"No AI behavior found for {currentUnit.UnitName}");
            return;
        }

        // This would require making some methods public in AIDecisionGenerator
        // For now, just enable the visualizer and let it show on next turn
        GD.Print("Action scores will be shown automatically if debug visualization is enabled");
    }

    /// <summary>
    /// Finds the EnemyAIBehavior component attached to a unit.
    /// </summary>
    private EnemyAIBehavior? FindAIBehavior(IUnitSystem unit)
    {
        foreach (Node child in ((CharacterBody3D)unit).GetChildren())
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
