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

    /// <inheritdoc />
    public override void _Ready()
    {
        GD.Print($"BattleHud._Ready running. Layer={Layer}");

        ActionMenu = GetOrCreate<ActionMenu>("ActionMenu");
        SkillSelector = GetOrCreate<SkillSelector>("SkillSelector");
        PlayerStatus = GetOrCreate<PlayerStatusPanel>("PlayerStatus");
        EnemyRoster = GetOrCreate<EnemyRoster>("EnemyRoster");
        TurnQueue = GetOrCreate<TurnOrderQueue>("TurnQueue");
        ContextInfo = GetOrCreate<ContextInfoPanel>("ContextInfo");
        Log = GetOrCreate<BattleLog>("BattleLog");

        Visible = true;
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
