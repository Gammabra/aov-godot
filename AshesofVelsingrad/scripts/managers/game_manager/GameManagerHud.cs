using System;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Systems.Battle;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     <see cref="GameManager" /> partial — HUD + IndicatorOverlay spawn and wiring.
/// </summary>
/// <remarks>
///     Call <see cref="EnsureHud" /> + <see cref="EnsureIndicators" /> from
///     <c>InitializeGameManager</c> after the map and turn manager are resolved. Then
///     <see cref="WireHudEvents" /> connects ActionMenu / SkillSelector buttons to the
///     gameplay handlers.
/// </remarks>
public partial class GameManager
{
    /// <summary>HUD root, spawned (or found) in <see cref="EnsureHud" />.</summary>
    protected BattleHud? _battleHud;

    protected InventoryUI? _inventoryUI;

    /// <summary>World-space tile overlays (move/target/hover).</summary>
    protected IndicatorOverlay? _indicators;

    /// <summary>End-of-battle Victory overlay; spawned lazily.</summary>
    protected VictoryScreen? _victoryScreen;

    /// <summary>End-of-battle Defeat overlay; spawned lazily.</summary>
    protected GameOverScreen? _gameOverScreen;

    /// <summary>Optional path to a designer-authored HUD scene.</summary>
    [Export(PropertyHint.File, "*.tscn")]
    private string _battleHudScenePath = string.Empty;

    /// <summary>Find an existing <see cref="BattleHud" /> in the scene or spawn one.</summary>
    /// <remarks>
    ///     Widget internals are only built when the BattleHud's <c>_Ready</c> fires next frame,
    ///     so any code that touches <c>_battleHud.PlayerStatus.Bind(...)</c>, etc. must run via
    ///     <c>CallDeferred</c> (see <see cref="RefreshHudOnReady" />) rather than synchronously.
    /// </remarks>
    protected void EnsureHud()
    {
        if (_battleHud is not null && IsInstanceValid(_battleHud)) return;

        // Prefer the active scene branch so the HUD shares the same live viewport as
        // the battle scene. Fall back to the tree root only if no current scene exists.
        SceneTree tree = GetTree();
        Node host = tree.CurrentScene ?? tree.Root;
        BattleHud? found = tree.CurrentScene is not null ? FindHudIn(tree.CurrentScene) : null;
        found ??= FindHudIn(tree.Root);
        if (found is not null) { _battleHud = found; return; }

        if (!string.IsNullOrEmpty(_battleHudScenePath))
        {
            PackedScene? scene = ResourceLoader.Load<PackedScene>(_battleHudScenePath);
            if (scene is not null) _battleHud = scene.Instantiate<BattleHud>();
        }
        _battleHud ??= new BattleHud { Name = "BattleHud" };
        // Pin layer + visibility BEFORE adding to the tree so the very first paint already
        // shows the HUD on top of the 3D viewport.
        _battleHud.Layer = BattleHud.HudLayer;
        _battleHud.Visible = true;
        // DEFER the AddChild — calling AddChild while we're still inside another node's
        // _Ready can leave the new node in a state where Godot never dispatches its own
        // _Ready, and CanvasLayer 2D rendering doesn't initialise. Queueing it via
        // CallDeferred means the node enters the tree at the start of the next idle frame,
        // when no _Ready chain is in flight, and Godot processes it normally.
        host.CallDeferred("add_child", _battleHud);
        GD.Print($"BattleHud queued for deferred AddChild under '{host.Name}' (type={host.GetType().Name}), layer={_battleHud.Layer}, visible={_battleHud.Visible}");
        // Build the children synchronously NOW. Build() does AddChild on widgets parented
        // to the BattleHud — those children get added to the in-memory BattleHud right
        // away. When Godot processes the deferred AddChild, the entire subtree enters the
        // tree at once, _EnterTree and _Ready fire for everything in the right order.
        try
        {
            _battleHud.Build();
            GD.Print($"BattleHud.Build OK — child count: {_battleHud.GetChildCount()}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"BattleHud.Build threw [{ex.GetType().Name}]: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    ///     Spawn (or find) the <see cref="InventoryUI" /> overlay, following the
    ///     exact same pattern as <see cref="EnsureHud" />.
    /// </summary>
    protected void EnsureInventoryUI()
    {
        if (_inventoryUI is not null && IsInstanceValid(_inventoryUI)) return;

        SceneTree tree = GetTree();
        Node host = tree.CurrentScene ?? tree.Root;

        // Try to find one already in the tree (designer-placed or previous call)
        foreach (Node child in host.GetChildren())
        {
            if (child is InventoryUI existing) { _inventoryUI = existing; return; }
        }

        _inventoryUI = new InventoryUI { Name = "InventoryUI" };
        host.CallDeferred("add_child", _inventoryUI);
        _inventoryUI.EnsureBuilt();
    }

    /// <summary>Spawn the move/target/hover indicator overlay parented to the map.</summary>
    protected void EnsureIndicators()
    {
        if (_mapSystemContainer is not MapSystem mapNode) return;
        if (_indicators is not null && IsInstanceValid(_indicators)) return;

        _indicators = new IndicatorOverlay { Name = "IndicatorOverlay" };
        mapNode.AddChild(_indicators);
        _indicators.Initialize(mapNode);
    }

    /// <summary>Connect HUD buttons to the gameplay handlers.</summary>
    /// <remarks>Call AFTER <see cref="EnsureHud" /> and ONE ProcessFrame yield so the HUD's
    /// child widgets have run their <c>_Ready</c>.</remarks>
    protected void WireHudEvents()
    {
        if (_battleHud is null) return;

        if (_battleHud.ActionMenu is { } menu)
        {
            menu.OnAttackPressed += OnHudAttackPressed;
            menu.OnSkillPressed += OnHudSkillPressed;
            menu.OnMovePressed += OnHudMovePressed;
            menu.OnPassPressed += OnHudPassPressed;
            menu.OnCancelPressed += OnHudCancelPressed;
        }

        if (_battleHud.SkillSelector is { } selector)
            selector.OnSkillSelected += OnHudSkillSlotChosen;

        if (_inventoryUI is not null && _battleInputSystemContainer is not null)
        _inventoryUI.SetBattleInputSystem(_battleInputSystemContainer);
    }

    /// <summary>
    ///     Populate widgets that only need a one-shot bind at battle start (enemy roster).
    /// </summary>
    /// <remarks>
    ///     Called via <c>CallDeferred</c> after <see cref="EnsureHud" /> so the HUD's child
    ///     widgets have run their <c>_Ready</c> and the <see cref="BattleHud.EnemyRoster" />
    ///     reference is populated.
    /// </remarks>
    protected void BindHudRosters()
    {
        if (_battleHud is null) return;
        _battleHud.EnemyRoster?.Bind(_enemyUnits);
    }

    /// <summary>
    ///     One-shot first-turn HUD refresh — runs AFTER <see cref="EnsureHud" /> finishes
    ///     spawning the <see cref="BattleHud" /> and its <c>_Ready</c> populates the child
    ///     widget references. Re-binds the active unit, redraws move tiles and updates the
    ///     context info panel — all of which silently no-op'd on the very first
    ///     <see cref="ActivatePlayerUnit" /> call because <c>_battleHud</c>'s children
    ///     hadn't been built yet.
    /// </summary>
    /// <remarks>Scheduled via <c>CallDeferred</c> at the end of <c>InitializeGameManager</c>.</remarks>
    protected void RefreshHudOnReady()
    {
        if (_turnManagerContainer is null) return;

        IUnitSystem? active;
        try { active = _turnManagerContainer.GetCurrentUnit(); }
        catch { return; }

        RefreshHudForActiveUnit(active);

        // Only redraw move-tile indicators + context-info movement summary when it's
        // actually a player turn — AI turns don't get tile previews.
        if (!_playerUnits.Contains(active)) return;

        ShowMoveIndicators(_currentUnitPossibleMoves);
        _battleHud?.ContextInfo?.ShowMovement(
            _currentUnitPossibleMoves.Count,
            active.PossibleMovesRange,
            canMove: !_unitMoved);
    }

    /// <summary>
    ///     Refresh the player-side HUD bindings (status panel + skill bar) for the active turn.
    /// </summary>
    /// <param name="active">The unit whose turn it is.</param>
    protected void RefreshHudForActiveUnit(IUnitSystem? active)
    {
        if (_battleHud is null) return;
        _battleHud.PlayerStatus?.Bind(active);
        _battleHud.SkillSelector?.Bind(active);
        if (_turnManagerContainer is not null)
            _battleHud.TurnQueue?.UpdateOrder(_turnManagerContainer.GetUpcomingUnits());

        // Hide the action bar on non-player turns — the player can't act anyway, and a
        // visible Move/Attack/Skill/Pass row would imply they can.
        bool isPlayerTurn = active is not null && active.Faction == Faction.Player;
        if (_battleHud.ActionMenu is { } menu) menu.Visible = isPlayerTurn;

        // Pulse the FactionMarker arrow on the active unit, dim the others.
        UpdateActiveMarkers(active);
    }

    /// <summary>
    ///     Walk every loaded unit's <see cref="FactionMarker" /> and only the active unit's
    ///     marker pulses. Called from every turn-event handler via
    ///     <see cref="RefreshHudForActiveUnit" />.
    /// </summary>
    private void UpdateActiveMarkers(IUnitSystem? active)
    {
        UpdateMarkersIn(_playerUnits, active);
        UpdateMarkersIn(_allyUnits, active);
        UpdateMarkersIn(_enemyUnits, active);

        static void UpdateMarkersIn(System.Collections.Generic.List<IUnitSystem> units, IUnitSystem? active)
        {
            foreach (IUnitSystem u in units)
            {
                if (u is not Node node) continue;
                if (node.GetNodeOrNull<FactionMarker>("FactionMarker") is { } marker)
                    marker.SetActive(ReferenceEquals(u, active));
            }
        }
    }

    private static BattleHud? FindHudIn(Node root)
    {
        if (root is BattleHud hud) return hud;
        foreach (Node child in root.GetChildren())
        {
            BattleHud? found = FindHudIn(child);
            if (found is not null) return found;
        }
        return null;
    }

    /// <summary>
    ///     Spawn (or refresh) the <see cref="VictoryScreen" /> overlay and bind it to the
    ///     surviving party. Called from <c>CheckWinLoseCondition</c> on victory.
    /// </summary>
    /// <remarks>
    ///     XP and loot values are placeholders for now — the rest of the project will wire
    ///     them up to the progression and loot-drop systems later.
    /// </remarks>
    protected void ShowVictoryScreen()
    {
        if (_victoryScreen is null || !IsInstanceValid(_victoryScreen))
        {
            _victoryScreen = new VictoryScreen { Name = "VictoryScreen" };
            // Defer the AddChild for the same reason EnsureHud does — see the long
            // explanation there. CanvasLayers added during another node's _Ready
            // sometimes never get their own _Ready dispatched.
            Node host = GetTree().Root;
            host.CallDeferred("add_child", _victoryScreen);
            _victoryScreen.OnContinuePressed += OnVictoryContinue;
        }

        var party = new System.Collections.Generic.List<IUnitSystem>();
        party.AddRange(_playerUnits);
        party.AddRange(_allyUnits);

        // Placeholder rewards — real numbers come from the progression system later.
        int xpGained = 100 * System.Math.Max(1, _enemyUnits.Count);
        var loot = new System.Collections.Generic.List<string>
        {
            "Gold ×120",
            "Health Potion ×1",
        };

        // Build child widgets synchronously (same pattern as BattleHud.Build) so that
        // by the time the deferred AddChild lands, the entire subtree enters the scene
        // tree at once and Godot dispatches _Ready for everything in the right order.
        _victoryScreen.EnsureBuilt();
        _victoryScreen.Bind(party, xpGained, loot);
    }

    /// <summary>
    ///     Spawn the <see cref="GameOverScreen" /> overlay. Called from
    ///     <c>CheckWinLoseCondition</c> on defeat.
    /// </summary>
    protected void ShowGameOverScreen()
    {
        if (_gameOverScreen is null || !IsInstanceValid(_gameOverScreen))
        {
            _gameOverScreen = new GameOverScreen { Name = "GameOverScreen" };
            Node host = GetTree().Root;
            host.CallDeferred("add_child", _gameOverScreen);
            _gameOverScreen.OnTryAgainPressed += OnTryAgain;
            _gameOverScreen.OnForfeitPressed += OnForfeit;
        }

        _gameOverScreen.EnsureBuilt();
    }

    /// <summary>Reload the current scene to retry the battle from the start.</summary>
    private void OnTryAgain()
    {
        GD.Print("GameManager: TryAgain pressed — reloading current scene.");
        // Cleanup of the old GameManager / HUD happens automatically when the scene
        // unloads; we don't need to QueueFree manually here.
        Error err = GetTree().ReloadCurrentScene();
        if (err != Error.Ok)
            GD.PrintErr($"GameManager: ReloadCurrentScene failed with {err}");
    }

    /// <summary>Placeholder for the Forfeit flow — wired to nothing real yet.</summary>
    private void OnForfeit()
    {
        GD.Print("GameManager: Forfeit pressed — placeholder, no action wired.");
        BattleNotifications.Post("Forfeit registered (placeholder).", BattleNotifications.Severity.Negative);
        // TODO: wire to scene-change / save-fail handling once that subsystem lands.
    }

    /// <summary>Continue button on the victory screen — for now just log.</summary>
    private void OnVictoryContinue()
    {
        GD.Print("GameManager: Victory Continue pressed — placeholder, no action wired.");
        // TODO: route to the next scene / overworld map.
    }

}
