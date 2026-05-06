using AshesOfVelsingrad.Systems.Battle;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Scrolling log subscribed to <see cref="BattleNotifications" />.
/// </summary>
/// <remarks>
///     Producers (<c>GameManager</c>, skills, status effects) call
///     <c>BattleNotifications.Post(message, severity)</c> and the log appends a colour-coded
///     line. Caps at <see cref="MaxLines" /> entries.
/// </remarks>
public sealed partial class BattleLog : Control
{
    /// <summary>Maximum number of log lines retained on screen.</summary>
    public const int MaxLines = 60;

    private RichTextLabel? _text;
    private bool _built;

    /// <inheritdoc />
    public override void _Ready()
    {
        EnsureBuilt();
    }

    /// <summary>Idempotent build — safe to call before <c>_Ready</c> fires.</summary>
    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        BuildLayout();
        BattleNotifications.Posted += OnNotification;
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        BattleNotifications.Posted -= OnNotification;
    }

    private void BuildLayout()
    {
        // 1152×648 design viewport. Width 260 (was 480), wedged between the
        // EnemyRoster (top-right, ends at y=332) and the SkillSelector strip
        // (starts at y=514). Resulting rect at the design size is x[880,1140]
        // y[360,500] — clear of every other widget in every direction.
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        OffsetLeft = -260;
        OffsetTop = -288;
        OffsetRight = -12;
        OffsetBottom = -148;
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer inner = new() { MouseFilter = MouseFilterEnum.Ignore };
        inner.AddThemeConstantOverride("separation", 4);
        inner.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(inner));

        Label header = new() { Text = "Log" };
        HudStyle.StyleLabel(header);
        header.AddThemeColorOverride("font_color", HudStyle.DimText);
        inner.AddChild(header);

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
        HudStyle.ApplyScaledFontSize(_text, "normal_font_size", HudStyle.FontSizeSmall);
        inner.AddChild(_text);
    }

    private void OnNotification(string message, BattleNotifications.Severity severity)
    {
        Append(ColorFor(severity), message);
    }

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

    private static string ColorFor(BattleNotifications.Severity severity) => severity switch
    {
        BattleNotifications.Severity.Positive => "#33cc77",
        BattleNotifications.Severity.Negative => "#cc7733",
        BattleNotifications.Severity.Critical => "#cc3333",
        _ => "#cccccc",
    };
}
