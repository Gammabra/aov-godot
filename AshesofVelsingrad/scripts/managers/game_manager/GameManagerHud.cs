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

    /// <summary>World-space tile overlays (move/target/hover).</summary>
    protected IndicatorOverlay? _indicators;

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
        host.AddChild(_battleHud);
        Viewport? viewport = _battleHud.GetViewport();
        Vector2 viewportSize = viewport?.GetVisibleRect().Size ?? Vector2.Zero;
        GD.Print($"BattleHud spawned under '{host.Name}' (type={host.GetType().Name}), layer={_battleHud.Layer}, visible={_battleHud.Visible}, viewport={viewportSize}");
        // Build all widgets synchronously. Wrapped in try/catch so that any layout-construction
        // exception is reported here rather than silently bubbling out of GameManager._Ready
        // and leaving _turnManagerContainer null. EnsureBuilt() is idempotent, so a later
        // _Ready firing is harmless.
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
}
