using System;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Full-screen Game-Over overlay shown when the entire player party is defeated.
/// </summary>
/// <remarks>
///     <para>
///         Shows two actions: <em>Try Again</em> reloads the current scene to restart the
///         encounter, <em>Forfeit</em> raises an event the rest of the project will wire up
///         later (return-to-menu, save the failure flag, etc.). Sits at
///         <see cref="GameOverLayer" />, above both the in-battle HUD (100) and the
///         <see cref="VictoryScreen" /> (110), since these two screens are mutually exclusive
///         but the higher layer guarantees nothing peeks through.
///     </para>
/// </remarks>
public sealed partial class GameOverScreen : CanvasLayer
{
    /// <summary>CanvasLayer index above HUD and VictoryScreen.</summary>
    public const int GameOverLayer = 120;

    /// <summary>Fired when the player presses Try Again — reload the current scene.</summary>
    public event Action? OnTryAgainPressed;

    /// <summary>
    ///     Fired when the player presses Forfeit. Wired to nothing real yet — the project
    ///     will hook this up to a "return to menu" or "fail-state save" handler later.
    /// </summary>
    public event Action? OnForfeitPressed;

    private bool _built;
    private bool _actionTaken;
    private Button? _tryAgainButton;
    private Button? _forfeitButton;

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
        Layer = GameOverLayer;
        Visible = true;
        BuildLayout();
    }

    private void BuildLayout()
    {
        // Heavily-darkened backdrop covering everything underneath.
        ColorRect dim = new()
        {
            Color = new Color(0, 0, 0, 0.78f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(dim);

        Control container = new() { MouseFilter = Control.MouseFilterEnum.Pass };
        container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(container);

        VBoxContainer panel = new()
        {
            MouseFilter = Control.MouseFilterEnum.Pass,
        };
        panel.AddThemeConstantOverride("separation", 18);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        panel.OffsetLeft = -260;
        panel.OffsetRight = 260;
        panel.OffsetTop = -180;
        panel.OffsetBottom = 180;
        container.AddChild(HudStyle.MakePanel(panel));

        Label title = new() { Text = "Defeat" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", new Color(0.95f, 0.30f, 0.30f));
        title.AddThemeFontSizeOverride("font_size", 42);
        panel.AddChild(title);

        Label subtitle = new() { Text = "Your party has fallen." };
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        HudStyle.StyleLabel(subtitle);
        subtitle.AddThemeColorOverride("font_color", HudStyle.DimText);
        panel.AddChild(subtitle);

        Label spacer = new() { Text = " " };
        panel.AddChild(spacer);

        _tryAgainButton = new Button { Text = "Try Again" };
        HudStyle.StyleButton(_tryAgainButton);
        _tryAgainButton.CustomMinimumSize = new Vector2(220, 50);
        _tryAgainButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        _tryAgainButton.AddThemeFontSizeOverride("font_size", 18);
        _tryAgainButton.Pressed += FireTryAgain;
        panel.AddChild(_tryAgainButton);

        _forfeitButton = new Button { Text = "Forfeit" };
        HudStyle.StyleButton(_forfeitButton);
        _forfeitButton.CustomMinimumSize = new Vector2(220, 50);
        _forfeitButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        _forfeitButton.AddThemeFontSizeOverride("font_size", 18);
        _forfeitButton.AddThemeColorOverride("font_color", new Color(0.85f, 0.65f, 0.55f));
        _forfeitButton.Pressed += FireForfeit;
        panel.AddChild(_forfeitButton);

        Label forfeitNote = new() { Text = "(Forfeit is wired to nothing yet — placeholder.)" };
        forfeitNote.HorizontalAlignment = HorizontalAlignment.Center;
        HudStyle.StyleLabel(forfeitNote);
        forfeitNote.AddThemeColorOverride("font_color", HudStyle.DimText);
        forfeitNote.AddThemeFontSizeOverride("font_size", 11);
        panel.AddChild(forfeitNote);
    }

    /// <summary>
    ///     Fire <see cref="OnTryAgainPressed" /> exactly once, then disable both buttons so
    ///     a quick double-click doesn't queue a second scene reload.
    /// </summary>
    private void FireTryAgain()
    {
        if (_actionTaken) return;
        _actionTaken = true;
        DisableButtons();
        OnTryAgainPressed?.Invoke();
    }

    /// <summary>Fire <see cref="OnForfeitPressed" /> exactly once.</summary>
    private void FireForfeit()
    {
        if (_actionTaken) return;
        _actionTaken = true;
        DisableButtons();
        OnForfeitPressed?.Invoke();
    }

    private void DisableButtons()
    {
        if (_tryAgainButton is not null) _tryAgainButton.Disabled = true;
        if (_forfeitButton is not null) _forfeitButton.Disabled = true;
    }
}
