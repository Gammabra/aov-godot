using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Top-left context panel that swaps content based on the player's input mode.
/// </summary>
/// <remarks>
///     <para>
///         Three modes:
///         <list type="bullet">
///             <item><description>Movement: reachable-tile budget and a one-line hint.</description></item>
///             <item><description>Action phase (after a move): "move spent, pick an action".</description></item>
///             <item><description>Skill targeting: selected skill's full details + cancel hint.</description></item>
///         </list>
///     </para>
///     <para>
///         Heavy iron frame, gold title, parchment body. Driven directly by <c>GameManager</c>
///         through <see cref="ShowMovement" /> and <see cref="ShowSkill" />.
///     </para>
/// </remarks>
public sealed partial class ContextInfoPanel : Control, IHudWidget
{
    private TextureRect? _icon;
    private Label? _title;
    private Label? _detail;
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
        ShowMovement(0, 0, false);
    }

    /// <inheritdoc />
    public void Relayout() => ApplyAnchorOffsets();

    private void ApplyAnchorOffsets()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        OffsetLeft = HudStyle.PadLg;
        OffsetTop = HudStyle.PadLg;
        OffsetRight = HudStyle.PadLg + HudStyle.ScaledPx(HudStyle.ContextWidth);
        OffsetBottom = HudStyle.PadLg + HudStyle.ScaledPx(HudStyle.ContextHeight);
        CustomMinimumSize = new Vector2(
            HudStyle.ScaledPx(HudStyle.ContextWidth),
            HudStyle.ScaledPx(HudStyle.ContextHeight));
    }

    private void BuildLayout()
    {
        ApplyAnchorOffsets();
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer box = new() { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", HudStyle.PadXs);
        box.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(box, HudStyle.PanelTier.Heavy));

        HBoxContainer titleRow = new() { MouseFilter = MouseFilterEnum.Ignore };
        titleRow.AddThemeConstantOverride("separation", HudStyle.PadSm);
        box.AddChild(titleRow);

        _icon = new TextureRect
        {
            Texture = HudStyle.LoadIcon("move"),
            CustomMinimumSize = new Vector2(
                HudStyle.ScaledPx(HudStyle.FontSizeHeader),
                HudStyle.ScaledPx(HudStyle.FontSizeHeader)),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        titleRow.AddChild(_icon);

        _title = new Label
        {
            Text = "—",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        HudStyle.StyleHeader(_title, HudStyle.FontSizeHeader);
        titleRow.AddChild(_title);

        ColorRect rule = new()
        {
            Color = HudStyle.BronzeDim,
            CustomMinimumSize = new Vector2(0, 1),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        box.AddChild(rule);

        _detail = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        HudStyle.StyleLabel(_detail, HudStyle.FontSizeBody);
        _detail.AddThemeColorOverride("font_color", HudStyle.Parchment);
        box.AddChild(_detail);
    }

    /// <summary>Display the movement summary for the active turn.</summary>
    public void ShowMovement(int reachableCount, int rangeBudget, bool canMove)
    {
        if (_title is null || _detail is null) return;
        _title.Text = canMove ? "Movement" : "Action Phase";
        _detail.Text = canMove
            ? $"Reachable tiles: {reachableCount}/{rangeBudget}\nClick a green tile to move, or pick a skill / pass."
            : "Move spent for this turn.\nPick a skill or pass.";
        if (_icon is not null) _icon.Texture = HudStyle.LoadIcon(canMove ? "move" : "attack");
    }

    /// <summary>Display details for a queued skill while the player picks a target.</summary>
    public void ShowSkill(ISkillSystem skill)
    {
        if (_title is null || _detail is null) return;

        string targetLabel = skill.TargetType switch
        {
            AovDataStructures.TargetTypes.SingleAlly => "Single ally",
            AovDataStructures.TargetTypes.AllAllies => "All allies",
            AovDataStructures.TargetTypes.SingleEnemy => "Single enemy",
            AovDataStructures.TargetTypes.AllEnemies => "All enemies",
            _ => skill.TargetType.ToString(),
        };
        string desc = string.IsNullOrEmpty(skill.Description) ? "(no description)" : skill.Description;

        _title.Text = string.IsNullOrEmpty(skill.Name) ? "Skill" : skill.Name;
        _detail.Text =
            $"{desc}\n\nMP {skill.ManaCost:F0}  •  CD {skill.TotalCooldown}  •  Range {skill.Range}\n" +
            $"Target: {targetLabel}\nRight-click or Esc to cancel.";
        if (_icon is not null) _icon.Texture = HudStyle.LoadIcon("skill");
    }

    /// <summary>Reset to a placeholder state when no battle is active.</summary>
    public void Clear()
    {
        if (_title is null || _detail is null) return;
        _title.Text = "—";
        _detail.Text = "";
    }
}
