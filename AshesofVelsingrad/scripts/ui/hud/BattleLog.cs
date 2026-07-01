using AshesOfVelsingrad.Systems.Battle;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Scrolling log subscribed to <see cref="BattleNotifications" />.
/// </summary>
/// <remarks>
///     Light-iron panel sitting above the action bar on the right side. Producers
///     (<c>GameManager</c>, skills, status effects) call <c>BattleNotifications.Post(message,
///     severity)</c> and the log appends a colour-coded line. Caps at <see cref="MaxLines" />
///     entries.
/// </remarks>
public sealed partial class BattleLog : Control, IHudWidget
{
    /// <summary>Maximum number of log lines retained on screen.</summary>
    public const int MaxLines = 60;

    private RichTextLabel? _text;
    private bool _built;
    private int _lineCount;

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

    /// <inheritdoc />
    public void Relayout() => ApplyAnchorOffsets();

    private void ApplyAnchorOffsets()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        int width = HudStyle.ScaledPx(HudStyle.LogWidth);
        int height = HudStyle.ScaledPx(HudStyle.LogHeight);
        int actionH = HudStyle.ScaledPx(HudStyle.ActionBarHeight);
        int skillH = HudStyle.ScaledPx(HudStyle.SkillBarHeight);
        OffsetLeft = -width - HudStyle.PadLg;
        OffsetTop = -actionH - skillH - height - HudStyle.PadLg - HudStyle.PadSm * 2;
        OffsetRight = -HudStyle.PadLg;
        OffsetBottom = -actionH - skillH - HudStyle.PadLg - HudStyle.PadSm * 2;
        CustomMinimumSize = new Vector2(width, height);
    }

    private void BuildLayout()
    {
        ApplyAnchorOffsets();
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer inner = new() { MouseFilter = MouseFilterEnum.Ignore };
        inner.AddThemeConstantOverride("separation", HudStyle.PadXs);
        inner.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(inner, HudStyle.PanelTier.Light));

        Label header = new() { Text = "BATTLE  LOG" };
        HudStyle.StyleHeader(header, HudStyle.FontSizeSub);
        header.AddThemeColorOverride("font_color", HudStyle.Gold);
        inner.AddChild(header);

        ColorRect rule = new()
        {
            Color = HudStyle.BronzeDim,
            CustomMinimumSize = new Vector2(0, 1),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        inner.AddChild(rule);

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
        _text.AddThemeColorOverride("default_color", HudStyle.Parchment);
        HudStyle.ApplyScaledFontSize(_text, "normal_font_size", HudStyle.FontSizeSmall);
        // Pin the bold / italic variants to the SAME size as normal text. Without this,
        // [b]…[/b] (used for skill names) falls back to the theme's default font size,
        // which renders several times larger than the log's body text.
        int logFontSize = HudStyle.ScaledFontSize(HudStyle.FontSizeSmall);
        _text.AddThemeFontSizeOverride("bold_font_size", logFontSize);
        _text.AddThemeFontSizeOverride("italics_font_size", logFontSize);
        _text.AddThemeFontSizeOverride("bold_italics_font_size", logFontSize);
        _text.AddThemeFontSizeOverride("mono_font_size", logFontSize);
        inner.AddChild(_text);
    }

    private void OnNotification(string message, BattleNotifications.Severity severity)
    {
        Append(ColorFor(severity), message);
    }

    private void Append(string colorHex, string message)
    {
        if (_text is null) return;
        // House style: no em/en dashes in the log — swap them for a plain hyphen.
        message = message.Replace(" — ", " - ").Replace("—", "-").Replace("–", "-");
        _text.PushColor(new Color(colorHex));
        _text.AppendText(message + "\n");
        _text.Pop();
        _lineCount++;

        // Drop the oldest paragraphs in place. Removing paragraphs preserves the BBCode
        // colour of every retained line — reassigning _text.Text (which returns BBCode-
        // stripped text) used to wipe all colour history once the cap was hit.
        while (_lineCount > MaxLines)
        {
            _text.RemoveParagraph(0);
            _lineCount--;
        }
    }

    private static string ColorFor(BattleNotifications.Severity severity) => severity switch
    {
        BattleNotifications.Severity.Positive => "#7fdba5",
        BattleNotifications.Severity.Negative => "#e8a04c",
        BattleNotifications.Severity.Critical => "#e85a5a",
        _ => "#dcd2bb",
    };
}
