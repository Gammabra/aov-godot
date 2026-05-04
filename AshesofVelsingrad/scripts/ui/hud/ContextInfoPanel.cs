using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Top-left context panel that swaps content based on the player's input mode.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Movement mode: shows reachable-tile budget and a one-line hint.</description></item>
///         <item><description>Action phase (after a move): shows "move spent, pick an action".</description></item>
///         <item><description>Skill targeting: shows the selected skill's full details + cancel hint.</description></item>
///     </list>
///     <para>
///         Driven directly by <c>GameManager</c> through <see cref="ShowMovement" /> and
///         <see cref="ShowSkill" /> — no event subscription, since the panel is a pure view of
///         the input state machine.
///     </para>
/// </remarks>
public sealed partial class ContextInfoPanel : Control
{
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
    /// <param name="reachableCount">Number of reachable tiles shown on the map.</param>
    /// <param name="rangeBudget">The unit's <see cref="IUnitSystem.PossibleMovesRange" />.</param>
    /// <param name="canMove">Whether the unit can still move this turn.</param>
    public void ShowMovement(int reachableCount, int rangeBudget, bool canMove)
    {
        if (_title is null || _detail is null) return;
        _title.Text = canMove ? "Movement" : "Action phase";
        _detail.Text = canMove
            ? $"Reachable tiles: {reachableCount}/{rangeBudget}\nClick a green tile to move, or pick a skill / pass."
            : "Move spent for this turn.\nPick a skill or pass.";
    }

    /// <summary>
    ///     Display details for a queued skill while the player picks a target.
    /// </summary>
    /// <param name="skill">Skill currently being targeted.</param>
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
    }

    /// <summary>Reset to a placeholder state when no battle is active.</summary>
    public void Clear()
    {
        if (_title is null || _detail is null) return;
        _title.Text = "—";
        _detail.Text = "";
    }
}
