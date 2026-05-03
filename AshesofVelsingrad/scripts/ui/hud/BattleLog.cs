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

    /// <inheritdoc />
    public override void _Ready()
    {
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
