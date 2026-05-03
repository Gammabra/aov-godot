using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.items;
using AshesOfVelsingrad.systems.skills;
using AshesOfVelsingrad.systems.status_effects;
using AshesOfVelsingrad.ui.hud;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     Represents the overall game state during a level.
/// </summary>
public enum GameState
{
    /// <summary>Waiting for the battle to start or for a process to complete.</summary>
    Waiting,

    /// <summary>The player's turn is currently active.</summary>
    PlayerTurn,

    /// <summary>The enemy's turn is currently active.</summary>
    EnemyTurn,

    /// <summary>The active player unit is selecting a target for a previously-chosen skill.</summary>
    TargetingSkill
}

/// <summary>
///     Represents the outcome of a battle.
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
///     The conductor of a level. The <see cref="GameManager" /> handles everything that a level
///     needs to work correctly.
/// </summary>
/// <remarks>
///     <para>
///         Beyond the legacy responsibilities (loading units, wiring input, driving the turn
///         loop), the GameManager is now responsible for <b>bootstrapping the combat layer</b>:
///         it lazily creates a <see cref="BattleEventBus" />, the skill / item registries, the
///         <see cref="DefaultDatabaseSeeder" />, the <see cref="BattleLauncher" />, and the
///         <see cref="BattleHud" /> if they are not already in the scene tree. This means a
///         level scene only needs to contain the <c>GameManager</c> + units + map and combat
///         "just works" — no manual AutoLoad setup required.
///     </para>
///     <para>
///         For projects that prefer the AutoLoad pattern, simply promote any of the bootstrapped
///         systems to AutoLoads; this manager only creates what's missing.
///     </para>
/// </remarks>
public partial class GameManager : BaseManager
{
    #region Private Fields

    private GameState _gameState = GameState.Waiting;
    private GameOutcome _gameOutcome = GameOutcome.Ongoing;
    private readonly List<UnitSystem> _playerUnits = [];
    private readonly List<UnitSystem> _allyUnits = [];
    private readonly List<UnitSystem> _enemyUnits = [];
    private List<(int, int, int)> _currentUnitPossibleMoves = [];
    private SkillSystem? _selectedSkill;
    private readonly StatusEffectSystem _statusEffectSystem = new();

    /// <summary>HUD root, lazily spawned in <see cref="EnsureBattleHud" />.</summary>
    private BattleHud? _battleHud;

    #endregion

    #region Godot Private Fields

    [Export] private NodePath? _playerUnitsPath;
    [Export] private NodePath? _enemyUnitsPath;

    /// <summary>
    ///     <b>Optional.</b> Container of AI-controlled friendly units (recruited mercs that join the
    ///     fight as guests, summoned creatures, etc.). Leave empty when the encounter has none.
    /// </summary>
    [Export] private NodePath? _alliedUnitsPath;

    [Export] private NodePath? _mapSystemPath;
    [Export] private NodePath? _turnManagerPath;
    [Export] private NodePath? _battleInputSystemPath;

    /// <summary>
    ///     <b>Optional.</b> Path to a pre-built <see cref="BattleHud" /> scene. If unset, the
    ///     manager creates one programmatically (with placeholder layouts).
    /// </summary>
    [Export(PropertyHint.File, "*.tscn")] private string _battleHudScenePath = string.Empty;

    private Node? _playerUnitsContainer;
    private Node? _enemyUnitsContainer;
    private Node? _alliedUnitsContainer;
    private MapSystem? _mapSystemContainer;
    private TurnManager? _turnManagerContainer;
    private BattleInputSystem? _battleInputSystemContainer;

    #endregion

    #region Singleton

    /// <summary>
    ///     Singleton instance of the <see cref="GameManager" />. Hides
    ///     <see cref="BaseManager.Instance" /> with a strongly-typed accessor.
    /// </summary>
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
    protected override void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Instance = this;
        // Note: we deliberately do NOT set BaseManager.Instance. That property is shared
        // across every BaseManager subclass, and any assignment here would cause the
        // duplicate-detection check in BaseManager._Ready to falsely trip for sibling
        // managers (TurnManager etc.) that are not parented directly to the scene root.

        // Make sure the new combat infrastructure is available before anything else runs.
        BootstrapCombatInfrastructure();

        InitializeGameManager();
        GD.Print("GameManager initialized successfully");
    }

    #endregion

    #region Combat-layer bootstrap

    /// <summary>
    ///     Create or locate every singleton the new combat layer needs.
    /// </summary>
    /// <remarks>
    ///     Each system is a <see cref="Node" /> child of the scene root. If a system is already
    ///     present (e.g. registered as AutoLoad), this method is a no-op for that system.
    ///     Keeping the bootstrap centralised here means new combat scenes don't need any setup
    ///     beyond dropping in a <see cref="GameManager" />.
    /// </remarks>
    private void BootstrapCombatInfrastructure()
    {
        EnsureSingleton<BattleEventBus>("BattleEventBus");
        EnsureSingleton<KarmaManager>("KarmaManager");
        EnsureSingleton<SkillRegistry>("SkillRegistry");
        EnsureSingleton<ItemRegistry>("ItemRegistry");
        EnsureSingleton<PartyInventory>("PartyInventory");
        EnsureSingleton<BattleLauncher>("BattleLauncher");

        // Seeder must come AFTER the registries so it has somewhere to register into.
        EnsureSingleton<DefaultDatabaseSeeder>("DefaultDatabaseSeeder");
    }

    /// <summary>
    ///     Add a node of type <typeparamref name="T" /> to the scene root if none exists.
    /// </summary>
    /// <typeparam name="T">A <see cref="Node" /> subclass with a parameterless constructor.</typeparam>
    /// <param name="name">Name to give the node when freshly created.</param>
    /// <remarks>
    ///     Adds directly (not deferred) so that <c>Instance</c> singletons are populated
    ///     before the next bootstrap step runs. Godot fires <c>_Ready</c> on the new child
    ///     at the next idle frame, which we wait for explicitly in
    ///     <see cref="StartBattleWhenReady" />.
    /// </remarks>
    private void EnsureSingleton<T>(string name) where T : Node, new()
    {
        Node root = GetTree().Root;
        // Quick check: if any child is already of the right type, we're done.
        foreach (Node child in root.GetChildren())
            if (child is T) return;

        T node = new() { Name = name };
        root.AddChild(node);
        GD.Print($"GameManager: bootstrapped {typeof(T).Name}.");
    }

    /// <summary>
    ///     Add a <see cref="BattleHud" /> to the scene tree if not already present.
    /// </summary>
    /// <remarks>
    ///     Does NOT wire HUD buttons — call <see cref="WireHudButtons" /> after at least one
    ///     frame has elapsed so that <c>BattleHud._Ready</c> has populated its child widget
    ///     references (see <see cref="StartBattleWhenReady" />).
    /// </remarks>
    private void EnsureBattleHud()
    {
        if (_battleHud is not null && IsInstanceValid(_battleHud))
        {
            GD.Print("GameManager: BattleHud already present, reusing.");
            return;
        }

        Node host = GetTree().CurrentScene ?? GetTree().Root;

        // Look for an existing HUD anywhere under the host first.
        BattleHud? found = FindBattleHudIn(host);
        if (found is not null)
        {
            _battleHud = found;
            GD.Print("GameManager: found existing BattleHud in scene, reusing.");
            return;
        }

        // Try a designer-supplied .tscn first; fall back to programmatic widget assembly.
        if (!string.IsNullOrEmpty(_battleHudScenePath))
        {
            PackedScene? scene = ResourceLoader.Load<PackedScene>(_battleHudScenePath);
            if (scene is not null)
            {
                _battleHud = scene.Instantiate<BattleHud>();
                GD.Print($"GameManager: instantiated BattleHud from '{_battleHudScenePath}'.");
            }
            else
            {
                GD.PrintErr($"GameManager: could not load HUD scene '{_battleHudScenePath}'.");
            }
        }

        if (_battleHud is null)
        {
            _battleHud = new BattleHud { Name = "BattleHud" };
            GD.Print("GameManager: spawning programmatic BattleHud (no .tscn assigned).");
            // BattleHud._Ready spawns each widget child programmatically.
        }

        host.AddChild(_battleHud);
        GD.Print($"GameManager: added BattleHud under '{host.Name}'.");
    }

    /// <summary>
    ///     Recursively search a subtree for the first <see cref="BattleHud" />.
    /// </summary>
    /// <param name="root">Node to search from.</param>
    /// <returns>The HUD or null.</returns>
    private static BattleHud? FindBattleHudIn(Node root)
    {
        if (root is BattleHud hud) return hud;
        foreach (Node child in root.GetChildren())
        {
            BattleHud? found = FindBattleHudIn(child);
            if (found is not null) return found;
        }
        return null;
    }

    /// <summary>
    ///     Connect the HUD's action / skill / item buttons to GameManager handlers.
    /// </summary>
    private void WireHudButtons()
    {
        if (_battleHud is null) return;

        // ActionMenu: Attack / Skill / Item / Pass / Flee.
        if (_battleHud.ActionMenu is { } menu)
        {
            menu.OnAttackPressed += OnHudAttackPressed;
            menu.OnSkillPressed += OnHudSkillPressed;
            menu.OnItemPressed += OnHudItemPressed;
            menu.OnFleePressed += OnHudFleePressed;
            // Pass is already handled internally by the ActionMenu (calls unit.PassTurn()).
        }

        // SkillSelector: a skill was chosen for the active unit.
        if (_battleHud.SkillSelector is { } selector)
            selector.OnSkillSelected += OnHudSkillSelected;

        // InventoryPanel: an item was chosen.
        if (_battleHud.InventoryPanel is { } inv)
            inv.OnItemChosen += OnHudItemChosen;
    }

    #endregion

    #region Private Methods — turn / battle bootstrap

    private async Task StartBattleWhenReady()
    {
        if (!IsInsideTree())
            await ToSignal(this, "ready");

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set");
            return;
        }

        // Yield one frame so every bootstrapped node has run its _Ready
        // (BattleEventBus.Instance, registries, seeder, etc. populated).
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // Auto-equip a default skill loadout for any unit that came in with empty
        // ActiveSkills. Done here, not in unit Initialize, because the SkillRegistry
        // only finishes seeding after this first frame yield.
        AutoEquipDefaultSkills();

        // Seed a few starter items into the shared inventory so the Item button has content.
        SeedStarterItems();

        // Make sure the HUD is in the tree before the bus starts firing TurnStarted etc.
        EnsureBattleHud();

        // Yield again so the HUD's _Ready runs (creates child widgets and they subscribe to events).
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // Yield once more so each child widget's own _Ready has run; only then are
        // _battleHud.ActionMenu / SkillSelector / InventoryPanel safe to dereference.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        WireHudButtons();

        // Tell the world we're starting so the HUD can show itself.
        BattleEventBus.Instance?.Publish(new BattleEvents.BattleStarted(
            _playerUnits, _allyUnits, _enemyUnits));

        // Republish the initiative queue now that subscribers exist.
        BattleEventBus.Instance?.Publish(new BattleEvents.TurnOrderChanged(_turnManagerContainer.TurnOrder));

        GD.Print("All nodes should be ready. Start battle now.");
        _ = _turnManagerContainer.StartBattle();
    }

    /// <summary>
    ///     For every unit in the battle whose <see cref="UnitSystem.ActiveSkills" /> is empty,
    ///     equip a default loadout from the seeded skill database so the HUD has something
    ///     to display and the AI has something to use.
    /// </summary>
    /// <remarks>
    ///     Player units get a generalist 5-skill kit; AI-controlled factions get a single
    ///     basic attack. This is a pragmatic fallback — designers should override
    ///     <see cref="UnitSystem.Initialize" /> in their unit data scripts to set bespoke
    ///     <c>ActiveSkills</c> per unit.
    /// </remarks>
    private void AutoEquipDefaultSkills()
    {
        if (SkillRegistry.Instance is null)
        {
            GD.PrintErr("AutoEquipDefaultSkills: SkillRegistry not initialised yet.");
            return;
        }

        // Curated starter loadout per faction — uses skill ids from DefaultDatabaseSeeder.
        string[] playerDefaults = ["frappe_ecrasante", "boule_de_feu", "rayon_sacre", "fleche_perforante", "coup_critique"];
        string[] aiDefaults = ["frappe_ecrasante"];

        foreach (UnitSystem unit in EnumerateAllCombatants())
        {
            if (unit.ActiveSkills.Count > 0) continue;

            string[] ids = unit.Faction == Faction.Player ? playerDefaults : aiDefaults;
            int equipped = 0;
            foreach (string id in ids)
            {
                SkillDefinition? def = SkillRegistry.Instance.GetDefinition(id);
                if (def is null) continue;
                DataDrivenSkill? skill = DataDrivenSkill.From(def);
                if (skill is null) continue;
                unit.ActiveSkills.Add(skill);
                equipped++;
            }
            GD.Print($"AutoEquipDefaultSkills: equipped {equipped} skills on {unit.UnitName} ({unit.Faction}).");
        }
    }

    /// <summary>
    ///     Drop a starter item set into the shared <see cref="PartyInventory" /> if it's empty.
    ///     Saves the player from facing the inventory button with no contents on a fresh run.
    /// </summary>
    private void SeedStarterItems()
    {
        PartyInventory? inv = PartyInventory.Instance;
        if (inv is null || inv.Items.Count > 0) return;

        inv.Add("healing_potion", 3);
        inv.Add("mana_potion", 2);
        inv.Add("antidote", 1);
        inv.Add("purifying_elixir", 1);
        GD.Print("SeedStarterItems: party inventory primed (3× HP / 2× MP / 1× antidote / 1× elixir).");
    }

    /// <summary>Iterate every player + ally + enemy unit currently in the encounter.</summary>
    private System.Collections.Generic.IEnumerable<UnitSystem> EnumerateAllCombatants()
    {
        foreach (UnitSystem u in _playerUnits) yield return u;
        foreach (UnitSystem u in _allyUnits) yield return u;
        foreach (UnitSystem u in _enemyUnits) yield return u;
    }

    private void InitializeGameManager()
    {
        _battleInputSystemContainer = GetNode<BattleInputSystem>(_battleInputSystemPath);
        _battleInputSystemContainer.OnPassTurnPressed += PlayerUnitPassedTurn;
        _battleInputSystemContainer.OnMoveUnitToPressed += PlayerUnitMoved;
        _battleInputSystemContainer.OnSelectedSkillPressed += PlayerSkillSelected;

        _playerUnitsContainer = GetNode<Node>(_playerUnitsPath);
        _enemyUnitsContainer = GetNode<Node>(_enemyUnitsPath);

        // Allies are optional — only resolve the path if it was provided.
        if (_alliedUnitsPath is not null && !_alliedUnitsPath.IsEmpty)
            _alliedUnitsContainer = GetNodeOrNull<Node>(_alliedUnitsPath);

        LoadUnits();

        _mapSystemContainer = GetNode<MapSystem>(_mapSystemPath);
        GD.Print($"[GameManager] Before PlaceUnits - players: {_playerUnits.Count} | allies: {_allyUnits.Count} | enemies: {_enemyUnits.Count}");

        // MapSystem.PlaceUnits today takes (friendly, enemy). Treat the player + ally lists as friendly.
        List<UnitSystem> friendlies = [.._playerUnits, .._allyUnits];
        _mapSystemContainer.PlaceUnits(friendlies, _enemyUnits);

        for (int i = 0; i < _playerUnits.Count; i++)
            GD.Print($"[GameManager] Player {i}: {_playerUnits[i].UnitName} at {_playerUnits[i].GlobalPosition}");
        for (int i = 0; i < _allyUnits.Count; i++)
            GD.Print($"[GameManager] Ally   {i}: {_allyUnits[i].UnitName} at {_allyUnits[i].GlobalPosition}");
        for (int i = 0; i < _enemyUnits.Count; i++)
            GD.Print($"[GameManager] Enemy  {i}: {_enemyUnits[i].UnitName} at {_enemyUnits[i].GlobalPosition}");

        _turnManagerContainer = GetNode<TurnManager>(_turnManagerPath);
        _turnManagerContainer.OnPlayerTurn += ActivatePlayerUnit;
        _turnManagerContainer.OnPlayerEndTurn += DeactivatePlayerUnit;
        _turnManagerContainer.InitializeTurnOrder(_playerUnits, _allyUnits, _enemyUnits);

        // Initial activation: only enable input if the first unit in initiative is a player one.
        UnitSystem first = _turnManagerContainer.GetCurrentUnit();
        if (first.Faction == Faction.Player)
            ActivatePlayerUnit();
        else
            DeactivatePlayerUnit();
    }

    /// <summary>
    ///     Load every <see cref="UnitSystem" /> child of the player / ally / enemy containers
    ///     and tag each with the appropriate <see cref="Faction" />.
    /// </summary>
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
        _allyUnits.Clear();
        _enemyUnits.Clear();

        foreach (Node child in _playerUnitsContainer.GetChildren())
            if (child is UnitSystem unit)
            {
                unit.AssignFaction(Faction.Player);
                _playerUnits.Add(unit);
            }

        if (_alliedUnitsContainer is not null)
        {
            foreach (Node child in _alliedUnitsContainer.GetChildren())
                if (child is UnitSystem unit)
                {
                    unit.AssignFaction(Faction.Ally);
                    _allyUnits.Add(unit);
                }
        }

        foreach (Node child in _enemyUnitsContainer.GetChildren())
            if (child is UnitSystem unit)
            {
                unit.AssignFaction(Faction.Enemy);
                _enemyUnits.Add(unit);
            }

        GD.Print($"Players: {_playerUnits.Count} | Allies: {_allyUnits.Count} | Enemies: {_enemyUnits.Count}");
    }

    private void ActivatePlayerUnit()
    {
        if (!ValidateContainers()) return;

        _gameState = GameState.PlayerTurn;
        if (_currentUnitPossibleMoves.Count == 0)
            _currentUnitPossibleMoves = _turnManagerContainer!.GetCurrentUnit().GetPossibleMoves(_mapSystemContainer!);
        GD.Print("Current Unit Possible Moves: " + string.Join(", ", _currentUnitPossibleMoves));
        _battleInputSystemContainer!.SetInputEnabled(true);
        GD.Print("Activate input");
    }

    private void DeactivatePlayerUnit()
    {
        if (!ValidateContainers()) return;

        _gameState = GameState.EnemyTurn;
        _currentUnitPossibleMoves.Clear();
        _selectedSkill = null;
        GD.Print("Deactivate input");
        _battleInputSystemContainer!.SetInputEnabled(false);
    }

    private bool ValidateContainers()
    {
        if (_battleInputSystemContainer == null)
        {
            GD.PrintErr("BattleInputSystemContainer not set in GameManager.");
            return false;
        }

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            return false;
        }

        if (_mapSystemContainer == null)
        {
            GD.PrintErr("MapSystemContainer not set in GameManager.");
            return false;
        }

        return true;
    }

    #endregion

    #region Private Methods — input handlers

    /// <summary>Handle a skill being selected via hot-key (<see cref="BattleInputSystem" />).</summary>
    private void PlayerSkillSelected(int skillId)
    {
        if (_turnManagerContainer == null) return;
        UnitSystem current = _turnManagerContainer.GetCurrentUnit();
        if (skillId >= current.ActiveSkills.Count)
        {
            GD.PrintErr($"Skill id {skillId} out of range (active count {current.ActiveSkills.Count}).");
            return;
        }

        SetSelectedSkill(current.ActiveSkills[skillId]);
    }

    private void PlayerUnitPassedTurn()
    {
        if (_turnManagerContainer == null) return;
        _turnManagerContainer.GetCurrentUnit().PassTurn();
    }

    /// <summary>
    ///     Handle a click on the map. If the player is targeting a skill, the click is
    ///     interpreted as a target selection. Otherwise it's a move command.
    /// </summary>
    /// <param name="cell">Clicked grid cell.</param>
    private void PlayerUnitMoved(Vector3I cell)
    {
        if (_mapSystemContainer == null || _turnManagerContainer == null) return;

        // If a skill is queued, the click is interpreted as target selection.
        if (_gameState == GameState.TargetingSkill && _selectedSkill is not null)
        {
            ResolveSkillClick(cell);
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

        MoveUnit(cell);
    }

    /// <summary>
    ///     Resolve a click while the player is in skill-targeting mode.
    /// </summary>
    /// <param name="cell">Clicked grid cell.</param>
    private void ResolveSkillClick(Vector3I cell)
    {
        if (_mapSystemContainer == null || _turnManagerContainer == null || _selectedSkill is null)
            return;

        UnitSystem? clicked = _mapSystemContainer.GetUnitAt(cell.X, cell.Y, cell.Z);
        if (clicked is null)
        {
            GD.Print("No unit on that cell — skill cast cancelled.");
            _gameState = GameState.PlayerTurn;
            ActivatePlayerUnit();
            return;
        }

        UnitSystem source = _turnManagerContainer.GetCurrentUnit();
        UseSkill(source, clicked, _selectedSkill);
        _selectedSkill = null;
        _gameState = GameState.PlayerTurn;
    }

    #endregion

    #region Private Methods — HUD handlers

    private void OnHudAttackPressed()
    {
        // Treat "Attack" as "select skill 0" (the default basic skill in the loadout).
        if (_turnManagerContainer is null) return;
        UnitSystem unit = _turnManagerContainer.GetCurrentUnit();
        if (unit.ActiveSkills.Count == 0) return;
        SetSelectedSkill(unit.ActiveSkills[0]);
    }

    private void OnHudSkillPressed()
    {
        // Just bring up the skill selector. The player picks a slot which fires
        // OnHudSkillSelected, then a target click resolves the cast.
        // BattleHud.SkillSelector is always visible in the current minimal layout, so
        // this hook is wired for future "open submenu" behaviour.
    }

    private void OnHudItemPressed()
    {
        _battleHud?.InventoryPanel?.Toggle();
    }

    private void OnHudFleePressed()
    {
        if (_turnManagerContainer is null) return;
        _turnManagerContainer.RequestAbort(BattleOutcome.Retreat);
        // Unblock the WaitForActionAsync the loop is sitting on so it can observe
        // the abort flag on the next iteration.
        _turnManagerContainer.GetCurrentUnit().PassTurn();
    }

    private void OnHudSkillSelected(int slotIndex, DataDrivenSkill skill)
    {
        SetSelectedSkill(skill);
    }

    private void OnHudItemChosen(string itemId)
    {
        if (_turnManagerContainer is null) return;
        UnitSystem user = _turnManagerContainer.GetCurrentUnit();
        // Default behaviour: target the user (most starter items are Self / SingleAlly self-cast).
        user.UseItem(itemId, [user]);
    }

    /// <summary>
    ///     Queue a skill for the active unit and switch to targeting mode.
    /// </summary>
    /// <param name="skill">The skill to cast.</param>
    private void SetSelectedSkill(SkillSystem skill)
    {
        _selectedSkill = skill;
        _gameState = GameState.TargetingSkill;
        BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
            $"Choose a target for {skill.Name}.", LogSeverity.Info));
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Move the currently active unit to the specified cell on the map.
    /// </summary>
    /// <param name="cell">The target grid coordinates.</param>
    public void MoveUnit(Vector3I cell)
    {
        if (!ValidateContainers()) return;

        Vector3 worldPos = _mapSystemContainer!.MapToLocal(cell);
        worldPos.Y += _mapSystemContainer.CellSize.Y * 0.5f;

        UnitSystem current = _turnManagerContainer!.GetCurrentUnit();
        current.GlobalPosition = worldPos;
        current.MoveTo(cell.X, cell.Y, cell.Z, _mapSystemContainer);
        GD.Print("Unit moved");
    }

    /// <summary>
    ///     Apply <paramref name="skill" /> from <paramref name="sourceUnit" /> to
    ///     <paramref name="targetUnit" />, expanding the target list according to
    ///     the skill's <see cref="SkillSystem.TargetType" />.
    /// </summary>
    /// <param name="sourceUnit">Caster.</param>
    /// <param name="targetUnit">The unit the player clicked / aimed at.</param>
    /// <param name="skill">The skill to use.</param>
    /// <remarks>
    ///     Friendly factions for the source are <see cref="Faction.Player" /> + <see cref="Faction.Ally" />
    ///     when the source is a player unit, and the symmetric case for an ally caster. Enemies are
    ///     anything in the opposing faction list.
    /// </remarks>
    public void UseSkill(UnitSystem sourceUnit, UnitSystem targetUnit, SkillSystem skill)
    {
        if (_turnManagerContainer == null) return;

        (List<UnitSystem> friendly, List<UnitSystem> hostile) = GetFactionsFor(sourceUnit);
        List<UnitSystem> targets = [];

        switch (skill.TargetType)
        {
            case TargetTypes.SingleAlly:
                if (!friendly.Contains(targetUnit))
                {
                    GD.PrintErr($"Ally unit {targetUnit.UnitName} not found.");
                    return;
                }
                targets.Add(targetUnit);
                break;

            case TargetTypes.SingleEnemy:
                if (!hostile.Contains(targetUnit))
                {
                    GD.PrintErr($"Enemy unit {targetUnit.UnitName} not found.");
                    return;
                }
                targets.Add(targetUnit);
                break;

            case TargetTypes.AllAllies:
                targets.AddRange(friendly);
                break;

            case TargetTypes.AllEnemies:
                targets.AddRange(hostile);
                break;
        }

        sourceUnit.Play(targets, _mapSystemContainer, skill);
    }

    private (List<UnitSystem> friendly, List<UnitSystem> hostile) GetFactionsFor(UnitSystem unit)
    {
        return unit.Faction switch
        {
            Faction.Player => ([.._playerUnits, .._allyUnits], _enemyUnits),
            Faction.Ally => ([.._allyUnits, .._playerUnits], _enemyUnits),
            Faction.Enemy => (_enemyUnits, [.._playerUnits, .._allyUnits]),
            _ => (_playerUnits, _enemyUnits)
        };
    }

    #endregion
}
