using AshesOfVelsingrad.systems.battle;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Root HUD for the battle layer.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="BattleHud" /> is a single <see cref="CanvasLayer" /> that hosts every
///         combat widget: action menu, skill selector, inventory, enemy roster, turn queue,
///         corruption gauge, and the battle log. Each widget is responsible for its own
///         <see cref="BattleEventBus" /> subscription via <see cref="HudBusHelper" />.
///     </para>
///     <para>
///         The HUD is visible by default. <see cref="BattleEvents.BattleEnded" /> hides it so
///         a future exploration → battle → exploration loop returns to a clean state.
///     </para>
/// </remarks>
public sealed partial class BattleHud : CanvasLayer
{
    /// <summary>Action menu (Attack / Skill / Item / Pass / Flee).</summary>
    public ActionMenu? ActionMenu { get; private set; }

    /// <summary>Five-slot active-skill selector.</summary>
    public SkillSelector? SkillSelector { get; private set; }

    /// <summary>Inventory panel for the shared bag.</summary>
    public InventoryPanel? InventoryPanel { get; private set; }

    /// <summary>Enemy roster (HP and status icons).</summary>
    public EnemyRoster? EnemyRoster { get; private set; }

    /// <summary>Turn-order queue showing the next units to act.</summary>
    public TurnOrderQueue? TurnQueue { get; private set; }

    /// <summary>Corruption gauge for the active player unit.</summary>
    public CorruptionGauge? CorruptionGauge { get; private set; }

    /// <summary>Battle log widget.</summary>
    public BattleLog? Log { get; private set; }

    /// <inheritdoc />
    public override void _Ready()
    {
        GD.Print($"BattleHud._Ready running. Layer={Layer}");

        ActionMenu = GetOrCreate<ActionMenu>("ActionMenu");
        SkillSelector = GetOrCreate<SkillSelector>("SkillSelector");
        InventoryPanel = GetOrCreate<InventoryPanel>("InventoryPanel");
        EnemyRoster = GetOrCreate<EnemyRoster>("EnemyRoster");
        TurnQueue = GetOrCreate<TurnOrderQueue>("TurnQueue");
        CorruptionGauge = GetOrCreate<CorruptionGauge>("CorruptionGauge");
        Log = GetOrCreate<BattleLog>("BattleLog");

        // Defer the bus subscription — by the time the helper resolves, every singleton's
        // _Ready has run and Instance is populated.
        _ = HudBusHelper.WhenReadyAsync(this, bus =>
        {
            bus.Subscribe<BattleEvents.BattleEnded>(OnBattleEnded);
            GD.Print("BattleHud subscribed to BattleEnded.");
        });

        Visible = true;
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        BattleEventBus? bus = BattleEventBus.Instance;
        if (bus is null) return;
        bus.Unsubscribe<BattleEvents.BattleEnded>(OnBattleEnded);
    }

    private T GetOrCreate<T>(string name) where T : Control, new()
    {
        if (HasNode(name) && GetNode(name) is T existing)
            return existing;
        T created = new() { Name = name };
        AddChild(created);
        return created;
    }

    private void OnBattleEnded(BattleEvents.BattleEnded _) => Visible = false;
}
