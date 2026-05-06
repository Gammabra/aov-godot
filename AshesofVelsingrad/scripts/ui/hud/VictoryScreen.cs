using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Full-screen victory overlay shown when the player wins a battle.
/// </summary>
/// <remarks>
///     <para>
///         Sits at <see cref="VictoryLayer" />, above the in-battle HUD (layer 100). The
///         layout shows a card per party member (player party + allied guests) with their
///         portrait, name, HP / MP bars, alive-or-dead state, plus a placeholder "XP gained"
///         line and "Loot gained" list. A <em>Continue</em> button raises
///         <see cref="OnContinuePressed" />; <c>GameManager</c> can wire it to scene-change
///         logic.
///     </para>
///     <para>
///         Spawned programmatically and built once via <see cref="EnsureBuilt" /> exactly
///         like every other HUD widget.
///     </para>
/// </remarks>
public sealed partial class VictoryScreen : CanvasLayer
{
    /// <summary>CanvasLayer index above the regular HUD (which uses 100).</summary>
    public const int VictoryLayer = 110;

    /// <summary>Fired when the player presses Continue.</summary>
    public event Action? OnContinuePressed;

    private bool _built;
    private VBoxContainer? _root;
    private HBoxContainer? _cardRow;
    private Label? _xpLabel;
    private VBoxContainer? _lootList;

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
        Layer = VictoryLayer;
        Visible = true;
        BuildLayout();
    }

    /// <summary>
    ///     Populate the screen with the actual battle outcome data.
    /// </summary>
    /// <param name="partyMembers">Player + allied units to render as cards.</param>
    /// <param name="xpGained">Total XP awarded.</param>
    /// <param name="loot">Loot drop entries (each formatted for display, e.g. "Gold ×120").</param>
    public void Bind(IReadOnlyList<IUnitSystem> partyMembers, int xpGained, IReadOnlyList<string> loot)
    {
        if (!_built) EnsureBuilt();

        if (_cardRow is not null)
        {
            foreach (Node existing in _cardRow.GetChildren()) existing.QueueFree();
            foreach (IUnitSystem unit in partyMembers)
                _cardRow.AddChild(BuildCard(unit));
        }

        if (_xpLabel is not null)
            _xpLabel.Text = $"XP gained: +{xpGained}";

        if (_lootList is not null)
        {
            foreach (Node existing in _lootList.GetChildren()) existing.QueueFree();
            if (loot.Count == 0)
            {
                Label none = new() { Text = "No loot dropped." };
                HudStyle.StyleLabel(none);
                none.AddThemeColorOverride("font_color", HudStyle.DimText);
                _lootList.AddChild(none);
            }
            else
            {
                foreach (string item in loot)
                {
                    Label entry = new() { Text = "• " + item };
                    HudStyle.StyleLabel(entry);
                    _lootList.AddChild(entry);
                }
            }
        }
    }

    private void BuildLayout()
    {
        // Translucent dark backdrop covering the whole viewport.
        ColorRect dim = new()
        {
            Color = new Color(0, 0, 0, 0.65f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(dim);

        Control container = new() { MouseFilter = Control.MouseFilterEnum.Pass };
        container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(container);

        _root = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Pass,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        _root.AddThemeConstantOverride("separation", 16);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _root.OffsetLeft = -480;
        _root.OffsetRight = 480;
        _root.OffsetTop = -260;
        _root.OffsetBottom = 260;
        container.AddChild(HudStyle.MakePanel(_root));

        Label title = new() { Text = "Victory!" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.40f));
        HudStyle.ApplyScaledFontSize(title, "font_size", HudStyle.FontSizeTitle);
        _root.AddChild(title);

        Label subtitle = new() { Text = "The battle is won. Your party survives the day." };
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        HudStyle.StyleLabel(subtitle);
        subtitle.AddThemeColorOverride("font_color", HudStyle.DimText);
        _root.AddChild(subtitle);

        _cardRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
        _cardRow.AddThemeConstantOverride("separation", 12);
        _cardRow.Alignment = BoxContainer.AlignmentMode.Center;
        _root.AddChild(_cardRow);

        _xpLabel = new Label { Text = "XP gained: +0" };
        _xpLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _xpLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.95f, 0.55f));
        HudStyle.ApplyScaledFontSize(_xpLabel, "font_size", 20);
        _root.AddChild(_xpLabel);

        Label lootHeader = new() { Text = "Loot:" };
        lootHeader.HorizontalAlignment = HorizontalAlignment.Center;
        HudStyle.StyleLabel(lootHeader);
        _root.AddChild(lootHeader);

        _lootList = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
        _lootList.AddThemeConstantOverride("separation", 2);
        _lootList.Alignment = BoxContainer.AlignmentMode.Center;
        _root.AddChild(_lootList);

        Button continueButton = new() { Text = "Continue" };
        HudStyle.StyleButton(continueButton);
        continueButton.CustomMinimumSize = new Vector2(180, 44);
        continueButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        continueButton.Pressed += () => OnContinuePressed?.Invoke();
        _root.AddChild(continueButton);
    }

    /// <summary>Build a single party-member card for the row.</summary>
    private static Control BuildCard(IUnitSystem unit)
    {
        const int cardW = 200;
        const int cardH = 220;
        bool dead = !unit.IsAlive;
        Color tint = dead ? new Color(0.5f, 0.5f, 0.5f, 1f) : new Color(1f, 1f, 1f, 1f);

        Control wrapper = new()
        {
            CustomMinimumSize = new Vector2(cardW, cardH),
            MouseFilter = Control.MouseFilterEnum.Pass,
            Modulate = tint,
        };

        VBoxContainer inner = new() { MouseFilter = Control.MouseFilterEnum.Pass };
        inner.AddThemeConstantOverride("separation", 4);
        inner.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        wrapper.AddChild(HudStyle.MakePanel(inner));

        // Portrait (96×96) — EntityProfile carries a res:// path (Core has no Godot deps);
        // load lazily here. Falls back to a coloured rect when no path is set or the
        // resource is missing.
        Texture2D? portrait = LoadPortrait(unit.EntityProfile?.PortraitPath);
        if (portrait is not null)
        {
            TextureRect portraitView = new()
            {
                Texture = portrait,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(96, 96),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            inner.AddChild(portraitView);
        }
        else
        {
            ColorRect placeholder = new()
            {
                Color = ColorForFaction(unit.Faction) with { A = 0.4f },
                CustomMinimumSize = new Vector2(96, 96),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            inner.AddChild(placeholder);
        }

        string display = unit.EntityProfile?.DisplayName is { Length: > 0 } n ? n : unit.UnitName;
        string suffix = dead ? "  [color=#ff5555](KO)[/color]" : string.Empty;
        RichTextLabel name = new()
        {
            BbcodeEnabled = true,
            Text = display + suffix,
            FitContent = true,
            ScrollActive = false,
            CustomMinimumSize = new Vector2(0, 22),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        HudStyle.ApplyScaledFontSize(name, "normal_font_size", HudStyle.FontSizeBody);
        inner.AddChild(name);

        Label hpLabel = new() { Text = $"HP {unit.Hp:F0}/{unit.MaxHp:F0}" };
        HudStyle.StyleLabel(hpLabel);
        inner.AddChild(hpLabel);

        ProgressBar hp = new()
        {
            MinValue = 0,
            MaxValue = unit.MaxHp <= 0 ? 1 : unit.MaxHp,
            Value = unit.Hp,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 10),
        };
        HudStyle.ApplyBarStyle(hp, HudStyle.HpFill);
        inner.AddChild(hp);

        Label mpLabel = new() { Text = $"MP {unit.Mana:F0}/{unit.ManaMax:F0}" };
        HudStyle.StyleLabel(mpLabel);
        inner.AddChild(mpLabel);

        ProgressBar mp = new()
        {
            MinValue = 0,
            MaxValue = unit.ManaMax <= 0 ? 1 : unit.ManaMax,
            Value = unit.Mana,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 8),
        };
        HudStyle.ApplyBarStyle(mp, HudStyle.ManaFill);
        inner.AddChild(mp);

        return wrapper;
    }

    private static Color ColorForFaction(Faction f) => f switch
    {
        Faction.Player => HudStyle.PlayerColor,
        Faction.Ally => new Color(0.30f, 0.90f, 0.30f, 1f),
        Faction.Enemy => HudStyle.EnemyColor,
        _ => Colors.Gray,
    };

    /// <summary>
    ///     Materialise a portrait <see cref="Texture2D" /> from a <c>res://</c> path.
    ///     Returns null when the path is empty, missing, or fails to load — the card's
    ///     placeholder branch handles that case.
    /// </summary>
    private static Texture2D? LoadPortrait(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (!ResourceLoader.Exists(path)) return null;
        return ResourceLoader.Load<Texture2D>(path);
    }
}
