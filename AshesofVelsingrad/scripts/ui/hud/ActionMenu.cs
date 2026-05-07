using System;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Bottom-centre action bar for the active player unit.
/// </summary>
/// <remarks>
///     <para>
///         Iron + bronze frame, four icon-and-label buttons (Move / Attack / Skill / Pass)
///         plus a contextual Cancel that only appears while skill-targeting. Each button
///         carries a procedural icon from <c>res://assets/ui/hud/icons/</c>; if the icon
///         hasn't been imported yet the button still works as text-only.
///     </para>
///     <para>
///         Layout uses <see cref="HudStyle.ScaledPx"/> for the bar's anchor offsets so the
///         strip grows / shrinks with the user's UI scale slider in lock-step with the fonts.
///     </para>
/// </remarks>
public sealed partial class ActionMenu : Control, IHudWidget
{
    /// <summary>Player chose the basic-attack action.</summary>
    public event Action? OnAttackPressed;
    /// <summary>Player asked to open / focus the skill submenu.</summary>
    public event Action? OnSkillPressed;
    /// <summary>Player chose the move action.</summary>
    public event Action? OnMovePressed;
    /// <summary>Player chose to pass the turn.</summary>
    public event Action? OnPassPressed;
    /// <summary>Player cancelled skill targeting.</summary>
    public event Action? OnCancelPressed;

    private Button? _cancelButton;
    private bool _built;

    /// <inheritdoc />
    public override void _Ready() => EnsureBuilt();

    /// <summary>Idempotent build — safe to call before <c>_Ready</c> fires.</summary>
    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        BuildLayout();
    }

    /// <inheritdoc />
    public void Relayout() => ApplyAnchorOffsets();

    private void ApplyAnchorOffsets()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        int halfW = HudStyle.ScaledPx(HudStyle.ActionBarWidth) / 2;
        int height = HudStyle.ScaledPx(HudStyle.ActionBarHeight);
        OffsetLeft = -halfW;
        OffsetRight = halfW;
        OffsetTop = -height - HudStyle.PadLg;
        OffsetBottom = -HudStyle.PadLg;
        CustomMinimumSize = new Vector2(2 * halfW, height);
    }

    private void BuildLayout()
    {
        ApplyAnchorOffsets();
        // The widget itself MUST pass clicks through outside of buttons — players need to
        // click the battlefield through the gap between buttons.
        MouseFilter = MouseFilterEnum.Ignore;

        Control panelContent = new() { Name = "Content", MouseFilter = MouseFilterEnum.Ignore };
        panelContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(panelContent, HudStyle.PanelTier.Heavy));

        HBoxContainer row = new()
        {
            Name = "ButtonRow",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride("separation", HudStyle.PadXs);
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelContent.AddChild(row);

        AddIconButton(row, "Move", "move", () => OnMovePressed?.Invoke());
        AddIconButton(row, "Attack", "attack", () => OnAttackPressed?.Invoke());
        AddIconButton(row, "Skill", "skill", () => OnSkillPressed?.Invoke());
        AddIconButton(row, "Pass", "pass", () => OnPassPressed?.Invoke());

        _cancelButton = BuildButton("Cancel", "cancel", () => OnCancelPressed?.Invoke());
        _cancelButton.Visible = false;
        // Cancel uses an orange accent so it stands out from the four normal actions.
        _cancelButton.AddThemeColorOverride("font_color", new Color(1f, 0.72f, 0.45f, 1f));
        row.AddChild(_cancelButton);
    }

    /// <summary>Show or hide the Cancel button (entered/left skill-targeting mode).</summary>
    public void ShowCancel(bool show)
    {
        if (_cancelButton is not null) _cancelButton.Visible = show;
    }

    private static void AddIconButton(Container parent, string label, string iconName, Action onPressed)
        => parent.AddChild(BuildButton(label, iconName, onPressed));

    private static Button BuildButton(string label, string iconName, Action onPressed)
    {
        // Min width is ~70 design px so 5 buttons (4 + Cancel) fit in the 440-wide bar
        // even with the bronze content margins. The HBox shares any extra width via
        // SizeFlags.ExpandFill.
        Button b = new()
        {
            Text = label,
            CustomMinimumSize = new Vector2(
                HudStyle.ScaledPx(70),
                HudStyle.ScaledPx(HudStyle.ButtonHeight)),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.Fill,
            ClipText = true,
            // Icon on the LEFT, text follows. ExpandIcon=false keeps the icon at its
            // natural size instead of stretching it to fill the button.
            IconAlignment = HorizontalAlignment.Left,
            VerticalIconAlignment = VerticalAlignment.Center,
            ExpandIcon = false,
        };
        HudStyle.StyleButton(b, HudStyle.FontSizeSub);
        HudStyle.SetButtonIcon(b, iconName);
        b.Pressed += () => onPressed();
        return b;
    }
}
