using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.progression;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Three-segment gauge showing the active player unit's corruption level.
/// </summary>
/// <remarks>
///     Top-left, sits directly under the <see cref="ContextInfoPanel"/>. Each segment lights as
///     <see cref="CharacterProfile.CorruptionPoints" /> grows; the next ignites when the level
///     advances. Iron-with-bronze frame matches the rest of the HUD.
/// </remarks>
public sealed partial class CorruptionGauge : Control, IHudWidget
{
    private readonly ColorRect[] _segments = new ColorRect[CharacterProfile.MaxCorruptionLevel];
    private Label? _label;
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

    /// <inheritdoc />
    public void Relayout() => ApplyAnchorOffsets();

    private void ApplyAnchorOffsets()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        OffsetLeft = HudStyle.PadLg;
        OffsetTop = HudStyle.PadLg + HudStyle.ScaledPx(HudStyle.ContextHeight) + HudStyle.PadSm;
        OffsetRight = HudStyle.PadLg + HudStyle.ScaledPx(HudStyle.CorruptionWidth);
        OffsetBottom = OffsetTop + HudStyle.ScaledPx(HudStyle.CorruptionHeight);
        CustomMinimumSize = new Vector2(
            HudStyle.ScaledPx(HudStyle.CorruptionWidth),
            HudStyle.ScaledPx(HudStyle.CorruptionHeight));
    }

    private void BuildLayout()
    {
        ApplyAnchorOffsets();
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer box = new() { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", HudStyle.PadXs);
        box.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(box, HudStyle.PanelTier.Light));

        HBoxContainer head = new() { MouseFilter = MouseFilterEnum.Ignore };
        head.AddThemeConstantOverride("separation", HudStyle.PadXs);
        box.AddChild(head);

        TextureRect skull = new()
        {
            Texture = HudStyle.LoadIcon("skull"),
            CustomMinimumSize = new Vector2(
                HudStyle.ScaledPx(HudStyle.FontSizeSub),
                HudStyle.ScaledPx(HudStyle.FontSizeSub)),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        head.AddChild(skull);

        _label = new Label
        {
            Text = "Corruption: 0",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        HudStyle.StyleLabel(_label, HudStyle.FontSizeSub);
        _label.AddThemeColorOverride("font_color", HudStyle.Gold);
        head.AddChild(_label);

        HBoxContainer row = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", HudStyle.PadXs);
        box.AddChild(row);

        for (int i = 0; i < CharacterProfile.MaxCorruptionLevel; i++)
        {
            PanelContainer frame = new()
            {
                CustomMinimumSize = new Vector2(0, HudStyle.ScaledPx(20)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(0.04f, 0.04f, 0.04f, 0.95f),
                BorderColor = HudStyle.BronzeDim,
                BorderWidthLeft = 1, BorderWidthRight = 1,
                BorderWidthTop = 1, BorderWidthBottom = 1,
                CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
                CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
                ContentMarginLeft = 2, ContentMarginRight = 2,
                ContentMarginTop = 2, ContentMarginBottom = 2,
            });
            row.AddChild(frame);

            ColorRect seg = new()
            {
                Color = new Color(0.10f, 0.09f, 0.08f, 1f),
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            _segments[i] = seg;
            frame.AddChild(seg);
        }
    }

    private void RefreshFromCurrent()
    {
        UnitSystem? unit = TurnManager.Active?.CurrentUnit;
        if (unit is null)
        {
            for (int i = 0; i < _segments.Length; i++)
                _segments[i].Color = new Color(0.10f, 0.09f, 0.08f, 1f);
            if (_label is not null) _label.Text = "Corruption: —";
            return;
        }

        int level = unit.CorruptionLevel;
        if (_label is not null)
            _label.Text = $"Corruption: {level} ({unit.CorruptionPoints}/{UnitSystem.CorruptionPointsPerLevel})";

        for (int i = 0; i < _segments.Length; i++)
        {
            _segments[i].Color = i < level ? HudStyle.Crimson : new Color(0.10f, 0.09f, 0.08f, 1f);
        }
    }
}
