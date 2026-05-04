using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     Holds the player party's global karma value in <c>[-100, +100]</c>.
/// </summary>
/// <remarks>
///     <para>
///         Influenced by narrative beats outside combat (mercenary contract outcomes, dialogue
///         choices, mission completion methods). Feeds
///         <see cref="Data.Corruption.CorruptionSystem.RollBacklash" /> which modulates the
///         chance a dark-magic cast triggers a corruption side-effect on the caster.
///     </para>
///     <para>
///         <b>Sign convention:</b> negative karma = corrupt-leaning (less backlash because
///         the user has embraced morally-grey choices); positive karma = virtuous (more
///         backlash because dark magic is unfamiliar).
///     </para>
///     <para>Register as an AutoLoad singleton.</para>
/// </remarks>
public sealed partial class KarmaManager : BaseManager
{
    /// <summary>Inclusive minimum karma value.</summary>
    public const int MinKarma = -100;

    /// <summary>Inclusive maximum karma value.</summary>
    public const int MaxKarma = 100;

    /// <summary>Active singleton instance, set in <see cref="Initialize" />.</summary>
    public new static KarmaManager? Instance { get; private set; }

    /// <summary>Current karma value, clamped to <c>[-100, +100]</c>.</summary>
    public int Karma { get; private set; }

    /// <inheritdoc />
    protected override void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {nameof(KarmaManager)} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Instance = this;
        Karma = 0;
        GD.Print("KarmaManager initialized successfully");
    }

    /// <summary>Set the karma value directly (clamped to legal range).</summary>
    /// <param name="value">New value.</param>
    public void SetKarma(int value) => Karma = Mathf.Clamp(value, MinKarma, MaxKarma);

    /// <summary>Adjust karma by <paramref name="delta" /> (clamped after).</summary>
    /// <param name="delta">Signed change.</param>
    public void AdjustKarma(int delta) => SetKarma(Karma + delta);

    /// <inheritdoc />
    protected override void Cleanup()
    {
        if (Instance == this) Instance = null;
        base.Cleanup();
    }
}
