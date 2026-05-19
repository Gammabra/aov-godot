using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Systems.Battle;
using AshesOfVelsingrad.UI.Hud;
using AshesOfVelsingrad.UI.Inventory;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     <see cref="GameManager" /> partial — HUD + IndicatorOverlay spawn and wiring.
/// </summary>
public partial class GameManager
{
    /// <summary>HUD root, resolved via export path, scene search, or runtime generation.</summary>
    protected BattleHud? _battleHud;

    /// <summary>World-space tile overlays (move/target/hover).</summary>
    protected IndicatorOverlay? _indicators;

    /// <summary>End-of-battle Victory overlay; spawned lazily.</summary>
    protected VictoryScreen? _victoryScreen;

    /// <summary>End-of-battle Defeat overlay; spawned lazily.</summary>
    protected GameOverScreen? _gameOverScreen;
    
    /// <summary>Direct path to a pre-placed BattleHud node inside the designer-authored scene tree.</summary>
    [Export] 
    private NodePath? _battleHudPath;

    /// <summary>Optional path to a dedicated local UI Layer/Container. If unset, falls back to the scene root.</summary>
    [Export] 
    private NodePath? _uiContainerPath;

    /// <summary>Fallback path to a designer-authored HUD scene asset if no local instance is found.</summary>
    [Export(PropertyHint.File, "*.tscn")]
    private string _battleHudScenePath = string.Empty;

    /// <summary>Find an existing <see cref="BattleHud" /> in the scene or spawn one.</summary>
    protected void EnsureHud()
    {
        if (_battleHud is not null && IsInstanceValid(_battleHud)) return;

        // Step 1: Attempt to resolve via direct NodePath (Highly Recommended)
        if (_battleHudPath is not null && !_battleHudPath.IsEmpty)
        {
            _battleHud = GetNodeOrNull<BattleHud>(_battleHudPath);
            if (_battleHud is not null)
            {
                GD.Print($"GameManager: BattleHud resolved via explicit NodePath '{_battleHudPath}'");
                _battleHud.Build();
                return;
            }
        }

        // Step 2: Fallback to searching the active scene hierarchy
        SceneTree tree = GetTree();
        BattleHud? found = tree.CurrentScene is not null ? FindHudIn(tree.CurrentScene) : null;
        found ??= FindHudIn(tree.Root);
        
        if (found is not null) 
        { 
            _battleHud = found;
            GD.Print($"GameManager: BattleHud located contextually inside scene hierarchy: '{_battleHud.Name}'");
            _battleHud.Build();
            return; 
        }

        // Step 3: Instantiate dynamically from a packed scene asset path
        if (!string.IsNullOrEmpty(_battleHudScenePath))
        {
            PackedScene? scene = ResourceLoader.Load<PackedScene>(_battleHudScenePath);
            if (scene is not null) _battleHud = scene.Instantiate<BattleHud>();
        }

        // Step 4: Absolute structural fallback
        _battleHud ??= new BattleHud { Name = "BattleHud" };
        _battleHud.Layer = BattleHud.HudLayer;
        _battleHud.Visible = true;

        // Parent the dynamically instantiated HUD to your local UI container layer if specified
        Node host = ResolveUiHostNode();
        host.CallDeferred(Node.MethodName.AddChild, _battleHud);
        
        GD.Print($"GameManager: BattleHud spawned procedurally under '{host.Name}' (Type: {host.GetType().Name})");

        try
        {
            _battleHud.Build();
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

        if (_battleHud.InventoryPanel is not null && _battleInputSystemContainer is not null)
            _battleHud.InventoryPanel.SetBattleInputSystem(_battleInputSystemContainer);

        if (_battleHud.ActionMenu is { } actionMenu
            && _battleHud.InventoryPanel is not null
            && _battleHud.SkillSelector is { } skillSelector)
        {
            actionMenu.SetInventoryUI(
                _battleHud.InventoryPanel,
                skillSelector,
                () => _turnManagerContainer?.GetCurrentUnit().Inventory as InventorySystem
            );
            if (_battleInputSystemContainer is not null)
                _battleHud.InventoryPanel.SetBattleInputSystem(_battleInputSystemContainer);
        }
    }

    /// <summary>Populate widgets that only need a one-shot bind at battle start.</summary>
    protected void BindHudRosters()
    {
        if (_battleHud is null) return;
        _battleHud.EnemyRoster?.Bind(_enemyUnits);
    }

    /// <summary>One-shot first-turn HUD refresh — runs after _Ready chains finish processing.</summary>
    protected void RefreshHudOnReady()
    {
        if (_turnManagerContainer is null) return;

        IUnitSystem? active;
        try { active = _turnManagerContainer.GetCurrentUnit(); }
        catch { return; }

        RefreshHudForActiveUnit(active);

        if (!_playerUnits.Contains(active)) return;

        ShowMoveIndicators(_currentUnitPossibleMoves);
        _battleHud?.ContextInfo?.ShowMovement(
            _currentUnitPossibleMoves.Count,
            active.PossibleMovesRange,
            canMove: !_unitMoved);
    }

    /// <summary>Refresh the player-side HUD bindings for the active turn.</summary>
    protected void RefreshHudForActiveUnit(IUnitSystem? active)
    {
        if (_battleHud is null) return;
        _battleHud.PlayerStatus?.Bind(active);
        _battleHud.SkillSelector?.Bind(active);
        if (_turnManagerContainer is not null)
            _battleHud.TurnQueue?.UpdateOrder(_turnManagerContainer.GetUpcomingUnits());

        bool isPlayerTurn = active is not null && active.Faction == Faction.Player;
        if (_battleHud.ActionMenu is { } menu) menu.Visible = isPlayerTurn;

        UpdateActiveMarkers(active);
    }

    private void UpdateActiveMarkers(IUnitSystem? active)
    {
        UpdateMarkersIn(_playerUnits, active);
        UpdateMarkersIn(_allyUnits, active);
        UpdateMarkersIn(_enemyUnits, active);

        static void UpdateMarkersIn(List<IUnitSystem> units, IUnitSystem? active)
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

    /// <summary>Spawn (or refresh) the Victory Screen overlay, bound to a clean local UI container layer.</summary>
    protected void ShowVictoryScreen()
    {
        if (_victoryScreen is null || !IsInstanceValid(_victoryScreen))
        {
            _victoryScreen = new VictoryScreen { Name = "VictoryScreen" };
            
            Node host = ResolveUiHostNode();
            host.CallDeferred(Node.MethodName.AddChild, _victoryScreen);
            _victoryScreen.OnContinuePressed += OnVictoryContinue;
        }

        var party = new List<IUnitSystem>();
        party.AddRange(_playerUnits);
        party.AddRange(_allyUnits);

        int xpGained = 100 * Math.Max(1, _enemyUnits.Count);
        var loot = new List<string> { "Gold ×120", "Health Potion ×1" };

        _victoryScreen.EnsureBuilt();
        _victoryScreen.Bind(party, xpGained, loot);
    }

    /// <summary>Spawn the GameOverScreen overlay, bound to a clean local UI container layer.</summary>
    protected void ShowGameOverScreen()
    {
        if (_gameOverScreen is null || !IsInstanceValid(_gameOverScreen))
        {
            _gameOverScreen = new GameOverScreen { Name = "GameOverScreen" };
            
            Node host = ResolveUiHostNode();
            host.CallDeferred(Node.MethodName.AddChild, _gameOverScreen);
            _gameOverScreen.OnTryAgainPressed += OnTryAgain;
            _gameOverScreen.OnForfeitPressed += OnForfeit;
        }

        _gameOverScreen.EnsureBuilt();
    }

    /// <summary>Helper logic to determine where runtime overlay scenes should reside.</summary>
    private Node ResolveUiHostNode()
    {
        if (_uiContainerPath is not null && !_uiContainerPath.IsEmpty)
        {
            Node? container = GetNodeOrNull(_uiContainerPath);
            if (container is not null) return container;
        }
        
        return GetTree().CurrentScene ?? GetTree().Root;
    }

    private void OnTryAgain()
    {
        GD.Print("GameManager: TryAgain pressed — reloading current scene.");
        Error err = GetTree().ReloadCurrentScene();
        if (err != Error.Ok)
            GD.PrintErr($"GameManager: ReloadCurrentScene failed with {err}");
    }

    private void OnForfeit()
    {
        GD.Print("GameManager: Forfeit pressed — handing off to BattleLauncher.");
        BattleNotifications.Post("Battle forfeited.", BattleNotifications.Severity.Negative);
        if (BattleLauncher.Instance is null)
        {
            GD.PrintErr("GameManager: no BattleLauncher autoload — cannot return to exploration.");
            return;
        }
        BattleLauncher.Instance.Forfeit();
    }

    private void OnVictoryContinue()
    {
        GD.Print("GameManager: Victory Continue pressed — handing off to BattleLauncher.");
        if (BattleLauncher.Instance is null)
        {
            GD.PrintErr("GameManager: no BattleLauncher autoload — cannot return to exploration.");
            return;
        }
        BattleLauncher.Instance.VictoryReturn();
    }
}
