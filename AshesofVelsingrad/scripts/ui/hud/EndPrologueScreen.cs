using System;
using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     End-of-prologue chapter card, shown when the player wins the prologue fight and presses
///     Continue. It fills the screen with the <c>prologue_end.png</c> art and offers a single
///     "Back to main menu" button anchored bottom-right.
/// </summary>
/// <remarks>
///     <para>
///         The card image carries its own title text. The one interactive element, the button,
///         stays in step with the accessibility interface-scale setting: it scales with
///         <see cref="HudStyle.UiScale" />, shows the gold focus ring, and grabs keyboard focus
///         on open so the screen is usable without a mouse. The screen re-applies the scale live
///         when the player changes it via <see cref="SettingsManager.UiScaleChanged" />.
///     </para>
///     <para>
///         Spawned by <c>GameManager</c> above the in-battle HUD and the victory screen. Raises
///         <see cref="OnBackToMenuPressed" />; <c>GameManager</c> wires it to the main-menu load.
///     </para>
/// </remarks>
public sealed partial class EndPrologueScreen : CanvasLayer
{
    /// <summary>CanvasLayer index above the HUD (100), victory (110) and game-over (120).</summary>
    public const int EndPrologueLayer = 130;

    private const string CardPath = "res://assets/images/prologue_end.png";

    /// <summary>Raised when the player presses "Back to main menu".</summary>
    public event Action? OnBackToMenuPressed;

    private bool _built;
    private bool _actionTaken;
    private Button? _backButton;

    /// <inheritdoc />
    public override void _Ready() => EnsureBuilt();

    /// <summary>Idempotent build, safe to call before <c>_Ready</c> fires.</summary>
    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        Layer = EndPrologueLayer;
        Visible = true;
        BuildLayout();
        if (SettingsManager.Instance is { } settings)
            settings.UiScaleChanged += OnUiScaleChanged;
        // Land keyboard focus on the only action so the screen is usable without a mouse.
        _backButton?.CallDeferred(Control.MethodName.GrabFocus);
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (SettingsManager.Instance is { } settings)
            settings.UiScaleChanged -= OnUiScaleChanged;
        base._ExitTree();
    }

    private void OnUiScaleChanged(float newScale)
    {
        // Re-apply the scaled fonts across the card and re-size/re-place the button.
        HudStyle.RefreshScaledFonts(this);
        ApplyButtonLayout();
    }

    private static void SetAnchors(Control c, float l, float t, float r, float b)
    {
        c.AnchorLeft = l; c.AnchorTop = t; c.AnchorRight = r; c.AnchorBottom = b;
        c.OffsetLeft = 0; c.OffsetTop = 0; c.OffsetRight = 0; c.OffsetBottom = 0;
    }

    private void BuildLayout()
    {
        Control root = new() { MouseFilter = Control.MouseFilterEnum.Stop };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        // Near-black backdrop behind the card so any letterboxing blends into the scene and
        // it covers the battlefield and the victory screen underneath.
        ColorRect bg = new()
        {
            Color = new Color(0f, 0f, 0f, 1f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(bg);

        // The prologue card art (angel plus its baked title) fills the screen.
        if (ResourceLoader.Exists(CardPath))
        {
            TextureRect card = new()
            {
                Texture = ResourceLoader.Load<Texture2D>(CardPath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            card.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.AddChild(card);
        }

        // Back-to-menu button, bottom-right.
        _backButton = new Button { Text = "Back to main menu" };
        HudStyle.StyleButton(_backButton, HudStyle.FontSizeBody);
        _backButton.Pressed += FireBack;
        root.AddChild(_backButton);
        ApplyButtonLayout();
    }

    private void ApplyButtonLayout()
    {
        if (_backButton is null) return;
        int w = HudStyle.ScaledPx(240);
        int h = HudStyle.ScaledPx(50);
        _backButton.CustomMinimumSize = new Vector2(w, h);
        SetAnchors(_backButton, 1f, 1f, 1f, 1f);
        _backButton.GrowHorizontal = Control.GrowDirection.Begin;
        _backButton.GrowVertical = Control.GrowDirection.Begin;
        _backButton.OffsetLeft = -w - HudStyle.PadXl;
        _backButton.OffsetTop = -h - HudStyle.PadXl;
        _backButton.OffsetRight = -HudStyle.PadXl;
        _backButton.OffsetBottom = -HudStyle.PadXl;
    }

    private void FireBack()
    {
        if (_actionTaken) return;
        _actionTaken = true;
        if (_backButton is not null) _backButton.Disabled = true;
        OnBackToMenuPressed?.Invoke();
    }
}
