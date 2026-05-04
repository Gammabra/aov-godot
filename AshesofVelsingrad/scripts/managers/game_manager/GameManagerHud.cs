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
    protected void EnsureHud()
    {
        if (_battleHud is not null && IsInstanceValid(_battleHud)) return;

        Node host = GetTree().CurrentScene ?? GetTree().Root;
        BattleHud? found = FindHudIn(host);
        if (found is not null) { _battleHud = found; return; }

        if (!string.IsNullOrEmpty(_battleHudScenePath))
        {
            PackedScene? scene = ResourceLoader.Load<PackedScene>(_battleHudScenePath);
            if (scene is not null) _battleHud = scene.Instantiate<BattleHud>();
        }
        _battleHud ??= new BattleHud { Name = "BattleHud" };
        host.AddChild(_battleHud);
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
