using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.skills;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Top-left panel that swaps content based on the active player input mode.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Idle / movement mode → shows movement budget and a hint
///         "Click a green tile to move, or pick a skill / item / pass."</description></item>
///         <item><description>Skill targeting mode → shows the selected skill's name,
///         description, mana / cooldown / range, and target rule.</description></item>
///     </list>
///     <para>
///         Driven directly by <see cref="Managers.GameManager" /> via <see cref="ShowMovement" />
///         and <see cref="ShowSkill" />. Doesn't subscribe to the bus — keeps the
///         coupling explicit since it's tied to the input state machine, not battle events.
///     </para>
/// </remarks>
public sealed partial class ContextInfoPanel : Control
{
    private Label? _title;
    private Label? _detail;

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();
        ShowMovement(0, 0, false);
    }

    private void BuildLayout()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        OffsetLeft = 12;
        OffsetTop = 12;
        OffsetRight = 320;
        OffsetBottom = 110;
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer box = new() { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 4);
        box.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(box));

        _title = new Label { Text = "—" };
        _title.AddThemeFontSizeOverride("font_size", 16);
        HudStyle.StyleLabel(_title);
        box.AddChild(_title);

        _detail = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        HudStyle.StyleLabel(_detail);
        _detail.AddThemeColorOverride("font_color", HudStyle.DimText);
        _detail.AddThemeFontSizeOverride("font_size", 13);
        box.AddChild(_detail);
    }

    /// <summary>
    ///     Display the movement summary for the active turn.
    /// </summary>
    /// <param name="rangeAvailable">Tiles currently reachable. 0 means already moved.</param>
    /// <param name="rangeBudget">Maximum tiles the unit can move per turn.</param>
    /// <param name="canMove">Whether movement is still allowed this turn.</param>
    public void ShowMovement(int rangeAvailable, int rangeBudget, bool canMove)
    {
        if (_title is null || _detail is null) return;
        _title.Text = canMove ? "Movement" : "Action phase";
        _detail.Text = canMove
            ? $"Reachable tiles: {rangeAvailable}/{rangeBudget}\nClick a green tile to move, or use a skill, item, or pass."
            : "Move spent for this turn.\nUse a skill, item, basic attack, or pass.";
    }

    /// <summary>
    ///     Display details for a queued skill while the player picks a target.
    /// </summary>
    /// <param name="skill">The skill being targeted.</param>
    public void ShowSkill(SkillSystem skill)
    {
        if (_title is null || _detail is null) return;
        _title.Text = skill.Name;
        string targetLabel = skill.TargetType switch
        {
            TargetTypes.SingleAlly => "Single ally",
            TargetTypes.AllAllies => "All allies",
            TargetTypes.SingleEnemy => "Single enemy",
            TargetTypes.AllEnemies => "All enemies",
            _ => skill.TargetType.ToString(),
        };
        string desc = string.IsNullOrEmpty(skill.Description) ? "(no description)" : skill.Description;
        _detail.Text = $"{desc}\n\nMP {skill.ManaCost:F0}  •  CD {skill.TotalCooldown}  •  Range {skill.Range}\nTarget: {targetLabel}\nRight-click or Esc to cancel.";
    }

    /// <summary>Reset to a placeholder state when no battle is active.</summary>
    public void Clear()
    {
        if (_title is null || _detail is null) return;
        _title.Text = "—";
        _detail.Text = "";
    }
}
