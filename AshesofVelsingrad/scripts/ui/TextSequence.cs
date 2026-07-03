using System;
using Godot;
using System.Threading.Tasks;

namespace AshesOfVelsingrad.ui;

/// <summary>
/// Represents a UI sequence system that displays a series of text pages
/// with fade-in and fade-out effects, typically used for cutscenes,
/// introductions, or narrative transitions.
/// </summary>
public partial class TextSequence : CanvasLayer
{
    /// <summary>
    /// NodePath to the background used for fading the entire sequence (background overlay).
    /// </summary>
    [Export]
    private NodePath _fadeBackgroundPath = null!;

    /// <summary>
    /// NodePath to the Label node used to display the text content.
    /// </summary>
    [Export]
    private NodePath _labelNodePath = null!;

    private ColorRect _fadeBackground = null!;
    private Label _label = null!;

    /// <summary>
    /// Event triggered when the entire text sequence has finished playing
    /// and the node is about to be freed.
    /// </summary>
    public event Action? OnSequenceEnded;

    /// <summary>
    /// Displays a single text page with a fade-in, hold, and fade-out animation.
    /// </summary>
    /// <param name="text">Text to display on the label.</param>
    /// <param name="fontSize">Font size applied to the label.</param>
    /// <param name="duration">Time in seconds the text remains visible after fade-in.</param>
    /// <returns>A task that completes when the full animation for the text is finished.</returns>
    private async Task ShowText(string text, int fontSize, float duration)
    {
        // TODO: Add the font in the parameter of this function when we will have one
        _label.Text = text;
        _label.AddThemeFontSizeOverride("font_size", fontSize);

        SetLabelAlpha(0);

        Tween tween = CreateTween();
        tween.TweenProperty(_label, "modulate:a", 1f, 0.7f);
        await ToSignal(tween, Tween.SignalName.Finished);

        await ToSignal(GetTree().CreateTimer(duration),
            SceneTreeTimer.SignalName.Timeout);

        tween = CreateTween();
        tween.TweenProperty(_label, "modulate:a", 0f, 0.7f);

        await ToSignal(GetTree().CreateTimer(0.3f),
            SceneTreeTimer.SignalName.Timeout);

        await ToSignal(tween, Tween.SignalName.Finished);
    }

    /// <summary>
    /// Fades the background overlay to a target alpha over a given duration.
    /// </summary>
    /// <param name="targetAlpha">Target transparency value (0 = transparent, 1 = opaque).</param>
    /// <param name="time">Duration of the fade animation in seconds.</param>
    /// <returns>A task that completes when the fade animation is finished.</returns>
    private async Task FadeBackground(float targetAlpha, float time)
    {
        Tween tween = CreateTween();
        tween.TweenProperty(_fadeBackground, "modulate:a", targetAlpha, time);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    /// <summary>
    /// Sets the alpha transparency of the text label.
    /// </summary>
    /// <param name="a">Alpha value (0 = invisible, 1 = fully visible).</param>
    private void SetLabelAlpha(float a)
    {
        Color c = _label.Modulate;
        c.A = a;
        _label.Modulate = c;
    }

    /// <summary>
    /// Sets the alpha transparency of the fade background overlay.
    /// </summary>
    /// <param name="a">Alpha value (0 = invisible, 1 = fully visible).</param>
    private void SetBackgroundAlpha(float a)
    {
        Color c = _fadeBackground.Modulate;
        c.A = a;
        _fadeBackground.Modulate = c;
    }

    /// <summary>
    /// Initializes the node by retrieving required child nodes.
    /// </summary>
    public override void _Ready()
    {
        _fadeBackground = GetNode<ColorRect>(_fadeBackgroundPath);
        _label = GetNode<Label>(_labelNodePath);
        Hide();
    }

    /// <summary>
    /// Plays a sequence of text pages with fade-in, display delay, and fade-out effects.
    /// Each page contains text, font size, and display duration.
    /// </summary>
    /// <param name="pages">
    /// Array of tuples containing:
    /// - text: The text to display.
    /// - size: The font size for the label.
    /// - duration: How long the text remains visible after fading in.
    /// </param>
    /// <returns>A task that completes when the sequence has finished playing.</returns>
    public async Task PlaySequence((string text, int size, float duration)[] pages)
    {
        Show();

        foreach ((string text, int size, float duration) page in pages)
        {
            await ShowText(page.text, page.size, page.duration);
        }

        await FadeBackground(0f, 1.5f);

        OnSequenceEnded?.Invoke();
        QueueFree();
    }
}
