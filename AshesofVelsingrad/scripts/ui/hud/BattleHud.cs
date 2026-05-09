using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.UI.Inventory;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Root <see cref="CanvasLayer" /> that hosts every battle HUD widget.
/// </summary>
/// <remarks>
///     Spawned (or found) by <c>GameManager</c> at battle start. Children are created
///     programmatically in <see cref="_Ready" /> if a <c>.tscn</c> didn't pre-instantiate
///     them. Each child widget self-positions; the HUD itself doesn't subscribe to events —
///     it just exposes the children so <c>GameManager</c> can wire them.
/// </remarks>
public sealed partial class BattleHud : CanvasLayer
{
    /// <summary>
    ///     CanvasLayer index. Pinned high so the HUD always renders above the 3D viewport
    ///     content and any other CanvasLayers in the scene.
    /// </summary>
    public const int HudLayer = 100;

    /// <summary>Action menu (Move / Attack / Skill / Pass / Cancel).</summary>
    public ActionMenu? ActionMenu { get; private set; }

    /// <summary>Five-slot active-skill bar.</summary>
    public SkillSelector? SkillSelector { get; private set; }

    /// <summary>Bottom-left status panel for the active controlled unit.</summary>
    public PlayerStatusPanel? PlayerStatus { get; private set; }

    /// <summary>Top-right vertical roster of enemies.</summary>
    public EnemyRoster? EnemyRoster { get; private set; }

    /// <summary>Top-centre upcoming-turn-order strip.</summary>
    public TurnOrderQueue? TurnQueue { get; private set; }

    /// <summary>Top-left context panel (movement budget vs queued skill details).</summary>
    public ContextInfoPanel? ContextInfo { get; private set; }

    /// <summary>Right-side scrolling log of warnings + events.</summary>
    public BattleLog? Log { get; private set; }

    public BattleInventoryUI? InventoryPanel { get; private set; }

    /// <inheritdoc />
    public override void _Ready()
    {
        GD.Print($"BattleHud._Ready running. Layer={Layer}");
        Build();

        // Live-bind to the user's Interface Size slider. Each styled control
        // stamps its design-time font size into a meta entry during Build();
        // when the player drags the slider mid-battle (or after returning from
        // settings), HudStyle.RefreshScaledFonts walks the tree and re-applies
        // the new scale without rebuilding any widget.
        if (SettingsManager.Instance is { } settings)
        {
            settings.UiScaleChanged += OnUiScaleChanged;
        }
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (SettingsManager.Instance is { } settings)
        {
            settings.UiScaleChanged -= OnUiScaleChanged;
        }
        base._ExitTree();
    }

    private void OnUiScaleChanged(float newScale)
    {
        // Re-apply font sizes everywhere under this CanvasLayer. Cheap — only
        // controls that opted in via HudStyle.ApplyScaledFontSize / StyleLabel /
        // StyleButton get touched.
        GD.Print($"[BattleHud] UiScale changed to {newScale:F2}× — refreshing fonts.");
        HudStyle.RefreshScaledFonts(this);
    }

    /// <summary>
    ///     Build (or re-resolve) the child widget references, pin the layer's render order,
    ///     and force every child widget's own layout to run synchronously. Idempotent — safe
    ///     to call from <c>GameManager.EnsureHud</c> right after <c>AddChild</c> so the HUD
    ///     is fully usable without waiting for <c>_Ready</c> to fire.
    /// </summary>
    public void Build()
    {
        Layer = HudLayer;
        Visible = true;

        ActionMenu ??= GetOrCreate<ActionMenu>("ActionMenu");
        SkillSelector ??= GetOrCreate<SkillSelector>("SkillSelector");
        PlayerStatus ??= GetOrCreate<PlayerStatusPanel>("PlayerStatus");
        EnemyRoster ??= GetOrCreate<EnemyRoster>("EnemyRoster");
        TurnQueue ??= GetOrCreate<TurnOrderQueue>("TurnQueue");
        ContextInfo ??= GetOrCreate<ContextInfoPanel>("ContextInfo");
        Log ??= GetOrCreate<BattleLog>("BattleLog");
        InventoryPanel ??= GetOrCreate<BattleInventoryUI>("InventoryPanel");

        // Force each widget to build its own layout NOW. This is what makes the HUD
        // independent of Godot's _Ready timing — if AddChild-during-_Ready quirks delay
        // their _Ready, EnsureBuilt() runs the layout immediately. Each widget's own
        // EnsureBuilt is idempotent (guarded by a _built flag), so calling _Ready later
        // is harmless.
        ActionMenu.EnsureBuilt();
        SkillSelector.EnsureBuilt();
        PlayerStatus.EnsureBuilt();
        EnemyRoster.EnsureBuilt();
        TurnQueue.EnsureBuilt();
        ContextInfo.EnsureBuilt();
        Log.EnsureBuilt();
        InventoryPanel.EnsureBuilt();
    }

    private T GetOrCreate<T>(string name) where T : Control, new()
    {
        if (HasNode(name) && GetNode(name) is T existing)
            return existing;
        T created = new() { Name = name };
        AddChild(created);
        return created;
    }
}
