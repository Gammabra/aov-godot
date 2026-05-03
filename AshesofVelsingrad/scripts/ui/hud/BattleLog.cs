using AshesOfVelsingrad.systems.battle;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Scrolling battle log that records significant combat events.
/// </summary>
/// <remarks>
///     Bottom-right column. Subscribes to bus events for free-form lines, deaths, skill casts,
///     and item uses. Keeps the most recent <see cref="MaxLines" /> entries.
/// </remarks>
public sealed partial class BattleLog : Control
{
    /// <summary>Maximum number of log lines retained on screen.</summary>
    public const int MaxLines = 60;

    private RichTextLabel? _text;

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();

        _ = HudBusHelper.WhenReadyAsync(this, bus =>
        {
            bus.Subscribe<BattleEvents.LogMessage>(OnLog);
            bus.Subscribe<BattleEvents.UnitDied>(OnUnitDied);
            bus.Subscribe<BattleEvents.SkillUsed>(OnSkillUsed);
            bus.Subscribe<BattleEvents.ItemUsed>(OnItemUsed);
        });
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        BattleEventBus? bus = BattleEventBus.Instance;
        if (bus is null) return;
        bus.Unsubscribe<BattleEvents.LogMessage>(OnLog);
        bus.Unsubscribe<BattleEvents.UnitDied>(OnUnitDied);
        bus.Unsubscribe<BattleEvents.SkillUsed>(OnSkillUsed);
        bus.Unsubscribe<BattleEvents.ItemUsed>(OnItemUsed);
    }

    private void BuildLayout()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        OffsetLeft = -480;
        OffsetTop = -260;
        OffsetRight = -12;
        OffsetBottom = -100;
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer inner = new() { MouseFilter = MouseFilterEnum.Ignore };
        inner.AddThemeConstantOverride("separation", 4);
        inner.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(inner));

        Label header = new() { Text = "Log" };
        HudStyle.StyleLabel(header);
        header.AddThemeColorOverride("font_color", HudStyle.DimText);
        inner.AddChild(header);

        // RichTextLabel handles its own scrolling when ScrollActive is true and FitContent
        // is false — the scroll bar appears automatically once the text exceeds the visible
        // area. Wrapping it in an extra ScrollContainer would interfere with that.
        _text = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollActive = true,
            ScrollFollowing = true,
            FitContent = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Stop,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _text.AddThemeFontSizeOverride("normal_font_size", 13);
        inner.AddChild(_text);
    }

    private void OnLog(BattleEvents.LogMessage e) => Append(ColorFor(e.Severity), e.Message);
    private void OnUnitDied(BattleEvents.UnitDied e) => Append("#cc3333", $"{e.Unit.UnitName} fell.");
    private void OnSkillUsed(BattleEvents.SkillUsed e) => Append("#cccccc", $"{e.Caster.UnitName} casts {e.SkillId}.");
    private void OnItemUsed(BattleEvents.ItemUsed e) => Append("#aaccff", $"{e.User.UnitName} uses {e.ItemId}.");

    private void Append(string colorHex, string message)
    {
        if (_text is null) return;
        _text.PushColor(new Color(colorHex));
        _text.AppendText(message + "\n");
        _text.Pop();

        // Trim oldest lines if we exceed MaxLines.
        string[] lines = _text.Text.Split('\n');
        if (lines.Length > MaxLines)
        {
            int drop = lines.Length - MaxLines;
            string trimmed = string.Join('\n', lines, drop, lines.Length - drop);
            _text.Text = trimmed;
        }
    }

    private static string ColorFor(LogSeverity sev) => sev switch
    {
        LogSeverity.Positive => "#33cc77",
        LogSeverity.Negative => "#cc7733",
        LogSeverity.Critical => "#cc3333",
        _ => "#cccccc",
    };
}
