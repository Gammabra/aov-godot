using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.ui.hud;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     The conductor of a level. Bootstraps every combat singleton, resolves the scene-tree
///     containers, hands ownership of the player input flow to <see cref="PlayerTurnController" />,
///     and starts the turn loop.
/// </summary>
/// <remarks>
///     <para>
///         The previous monolithic version of this class was split across:
///         <list type="bullet">
///             <item><see cref="BattleBootstrap" /> — spawns singletons + HUD.</item>
///             <item><see cref="CombatLoadoutSeeder" /> — auto-equips skills + items.</item>
///             <item><see cref="IndicatorOverlay" /> — owns the move/target/hover overlays.</item>
///             <item><see cref="PlayerTurnController" /> — runs the player input state machine.</item>
///             <item><see cref="GameManager" /> — wires the pieces together (this file).</item>
///         </list>
///     </para>
///     <para>
///         The split keeps each file focused and short. <c>GameManager</c> here is mostly
///         scene-tree plumbing: read <c>[Export]</c> paths, load units, hand them to the
///         controller, await the turn loop.
///     </para>
/// </remarks>
public partial class GameManager : BaseManager
{
    #region Exported scene paths

    [Export] private NodePath? _playerUnitsPath;
    [Export] private NodePath? _enemyUnitsPath;

    /// <summary>
    ///     Optional. Container of AI-controlled friendly guests. Empty when unused.
    /// </summary>
    [Export] private NodePath? _alliedUnitsPath;

    [Export] private NodePath? _mapSystemPath;
    [Export] private NodePath? _turnManagerPath;
    [Export] private NodePath? _battleInputSystemPath;

    /// <summary>Optional path to a designer-authored <c>BattleHud.tscn</c>.</summary>
    [Export(PropertyHint.File, "*.tscn")] private string _battleHudScenePath = string.Empty;

    #endregion

    #region Resolved containers

    private readonly List<UnitSystem> _playerUnits = [];
    private readonly List<UnitSystem> _allyUnits = [];
    private readonly List<UnitSystem> _enemyUnits = [];

    private MapSystem? _map;
    private TurnManager? _turn;
    private BattleInputSystem? _input;
    private BattleHud? _hud;
    private IndicatorOverlay? _indicators;
    private PlayerTurnController? _playerCtrl;

    #endregion

    #region Singleton

    /// <summary>Strongly-typed singleton accessor (shadows <see cref="BaseManager.Instance" />).</summary>
    public new static GameManager? Instance { get; private set; }

    #endregion

    #region Class initialization

    /// <inheritdoc />
    public override void _Ready()
    {
        base._Ready();
        _ = StartBattleWhenReady();
    }

    /// <inheritdoc />
    public override void _UnhandledInput(InputEvent @event)
    {
        if (_playerCtrl is null) return;
        if (_playerCtrl.HandleInput(@event))
            GetViewport().SetInputAsHandled();
    }

    /// <inheritdoc />
    protected override void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Instance = this;
        // Note: we deliberately do NOT touch BaseManager.Instance — that static is shared by
        // every manager subclass. Setting it here would break BaseManager's duplicate-detection
        // for sibling managers (TurnManager etc.) that aren't direct children of the scene root.

        BattleBootstrap.EnsureSingletons(this);

        ResolveContainers();
        LoadUnits();
        PlaceUnitsOnMap();
        SpawnIndicators();
        StartTurnOrder();
        BuildPlayerController();

        GD.Print("GameManager initialized successfully");
    }

    #endregion

    #region Initialization steps

    /// <summary>Look up every <c>[Export]</c> NodePath, falling back to logged errors.</summary>
    private void ResolveContainers()
    {
        _input = GetNode<BattleInputSystem>(_battleInputSystemPath);
        _input.OnPassTurnPressed += () => _playerCtrl?.HandlePass();
        _input.OnMoveUnitToPressed += cell => _playerCtrl?.HandleMapClick(cell);
        _input.OnSelectedSkillPressed += slot => _playerCtrl?.HandleSkillHotkey(slot);

        _map = GetNode<MapSystem>(_mapSystemPath);
        _turn = GetNode<TurnManager>(_turnManagerPath);
    }

    /// <summary>
    ///     Pull every <see cref="UnitSystem" /> from the player / ally / enemy containers,
    ///     tagging each with the appropriate <see cref="Faction" />.
    /// </summary>
    private void LoadUnits()
    {
        Node? players = ResolveOptional(_playerUnitsPath);
        Node? allies = ResolveOptional(_alliedUnitsPath);
        Node? enemies = ResolveOptional(_enemyUnitsPath);

        _playerUnits.Clear();
        _allyUnits.Clear();
        _enemyUnits.Clear();

        if (players is not null)
            CollectUnits(players, Faction.Player, _playerUnits);
        if (allies is not null)
            CollectUnits(allies, Faction.Ally, _allyUnits);
        if (enemies is not null)
            CollectUnits(enemies, Faction.Enemy, _enemyUnits);

        GD.Print($"Players: {_playerUnits.Count} | Allies: {_allyUnits.Count} | Enemies: {_enemyUnits.Count}");
    }

    private static void CollectUnits(Node container, Faction faction, List<UnitSystem> sink)
    {
        foreach (Node child in container.GetChildren())
        {
            if (child is not UnitSystem unit) continue;
            unit.AssignFaction(faction);
            sink.Add(unit);
        }
    }

    private Node? ResolveOptional(NodePath? path)
    {
        if (path is null || path.IsEmpty) return null;
        return GetNodeOrNull<Node>(path);
    }

    private void PlaceUnitsOnMap()
    {
        if (_map is null) return;
        List<UnitSystem> friendlies = [.._playerUnits, .._allyUnits];
        _map.PlaceUnits(friendlies, _enemyUnits);
    }

    private void SpawnIndicators()
    {
        if (_map is null) return;
        _indicators = new IndicatorOverlay { Name = "IndicatorOverlay" };
        _map.AddChild(_indicators);
        _indicators.Initialize(_map);
    }

    private void StartTurnOrder()
    {
        if (_turn is null) return;
        _turn.OnPlayerTurn += () => _playerCtrl?.BeginTurn();
        _turn.OnPlayerEndTurn += () => _playerCtrl?.EndTurn();
        _turn.InitializeTurnOrder(_playerUnits, _allyUnits, _enemyUnits);
    }

    private void BuildPlayerController()
    {
        // The HUD is created later (during StartBattleWhenReady) so we hold off on building
        // the controller until then — see EnsurePlayerController().
    }

    #endregion

    #region Battle start

    private async Task StartBattleWhenReady()
    {
        if (!IsInsideTree())
            await ToSignal(this, "ready");

        if (_turn is null || _map is null || _input is null)
        {
            GD.PrintErr("GameManager: containers not resolved; aborting StartBattleWhenReady.");
            return;
        }

        // Yield once so the singletons created in BattleBootstrap have run their _Ready
        // (registries, seeder, bus) and AutoEquip can rely on them.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // Auto-equip skills + seed inventory.
        CombatLoadoutSeeder.AutoEquip(EnumerateAllCombatants());
        CombatLoadoutSeeder.SeedStarterItems();

        // Spawn / find the HUD now that the bus exists.
        _hud = BattleBootstrap.EnsureBattleHud(this, _battleHudScenePath);

        // Yield twice so BattleHud._Ready and each child widget's _Ready have all run.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        EnsurePlayerController();
        WireHudEvents();

        // Tell the world we're starting; the HUD shows itself, indicators repaint.
        BattleEventBus.Instance?.Publish(new BattleEvents.BattleStarted(_playerUnits, _allyUnits, _enemyUnits));
        BattleEventBus.Instance?.Publish(new BattleEvents.TurnOrderChanged(_turn.TurnOrder));

        GD.Print("All nodes ready. Starting battle.");
        _ = _turn.StartBattle();
    }

    /// <summary>Build the controller once the HUD + indicators are alive.</summary>
    private void EnsurePlayerController()
    {
        if (_playerCtrl is not null) return;
        if (_map is null || _hud is null || _indicators is null || _input is null) return;
        _playerCtrl = new PlayerTurnController(
            _map, _hud, _indicators, _input,
            _playerUnits, _allyUnits, _enemyUnits);
    }

    private void WireHudEvents()
    {
        if (_hud is null || _playerCtrl is null) return;

        if (_hud.ActionMenu is { } menu)
        {
            menu.OnAttackPressed += _playerCtrl.HandleAttackPressed;
            menu.OnSkillPressed += _playerCtrl.HandleSkillPressed;
            menu.OnItemPressed += _playerCtrl.HandleItemPressed;
            menu.OnFleePressed += _playerCtrl.HandleFleePressed;
            menu.OnCancelPressed += _playerCtrl.HandleCancelPressed;
            // "Pass" is wired internally in ActionMenu via UnitSystem.PassTurn.
        }

        if (_hud.SkillSelector is { } selector)
            selector.OnSkillSelected += _playerCtrl.HandleSkillSlotChosen;

        if (_hud.InventoryPanel is { } inv)
            inv.OnItemChosen += _playerCtrl.HandleItemChosen;
    }

    #endregion

    #region Helpers

    /// <summary>Iterate every player + ally + enemy unit currently in the encounter.</summary>
    private IEnumerable<UnitSystem> EnumerateAllCombatants()
    {
        foreach (UnitSystem u in _playerUnits) yield return u;
        foreach (UnitSystem u in _allyUnits) yield return u;
        foreach (UnitSystem u in _enemyUnits) yield return u;
    }

    #endregion
}
