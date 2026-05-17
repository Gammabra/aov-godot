using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.progression;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Three-segment gauge showing the active player unit's corruption level.
/// </summary>
/// <remarks>
///     Top-left, tight bounds. Each segment fills as <see cref="CharacterProfile.CorruptionPoints" />
///     grows; the next lights up when the level advances.
/// </remarks>
public sealed partial class CorruptionGauge : Control
{
    private readonly ColorRect[] _segments = new ColorRect[CharacterProfile.MaxCorruptionLevel];
    private Label? _label;

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();

        _ = HudBusHelper.WhenReadyAsync(this, bus =>
        {
            bus.Subscribe<BattleEvents.CorruptionChanged>(_ => RefreshFromCurrent());
            bus.Subscribe<BattleEvents.TurnStarted>(_ => RefreshFromCurrent());
            RefreshFromCurrent();
        });
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        // Subscriptions captured above are anonymous lambdas — unsubscribing precisely is
        // skipped here; the bus instance is process-lifetime, no leak in practice.
    }

    private void BuildLayout()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        OffsetLeft = 12;
        OffsetTop = 12;
        OffsetRight = 232;
        OffsetBottom = 72;
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer box = new() { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 4);
        box.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(box));

        _label = new Label { Text = "Corruption: 0" };
        HudStyle.StyleLabel(_label);
        box.AddChild(_label);

        HBoxContainer row = new() { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 3);
        box.AddChild(row);

        for (int i = 0; i < CharacterProfile.MaxCorruptionLevel; i++)
        {
            ColorRect seg = new()
            {
                CustomMinimumSize = new Vector2(64, 12),
                Color = new Color(0.18f, 0.18f, 0.18f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _segments[i] = seg;
            row.AddChild(seg);
        }
    }

    private void RefreshFromCurrent()
    {
        UnitSystem? unit = TurnManager.Active?.CurrentUnit;
        if (unit is null)
        {
            for (int i = 0; i < _segments.Length; i++)
                _segments[i].Color = new Color(0.18f, 0.18f, 0.18f);
            if (_label is not null) _label.Text = "Corruption: —";
            return;
        }

        int level = unit.CorruptionLevel;
        if (_label is not null)
            _label.Text = $"Corruption: {level} ({unit.CorruptionPoints}/{UnitSystem.CorruptionPointsPerLevel})";

        for (int i = 0; i < _segments.Length; i++)
        {
            _segments[i].Color = i < level ? HudStyle.HpFill : new Color(0.18f, 0.18f, 0.18f);
        }
    }
}
