using AshesOfVelsingrad.systems.battle;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     Holds the player party's global karma value.
/// </summary>
/// <remarks>
///     <para>
///         Karma is a single shared scalar in <c>[-100, +100]</c> influenced by narrative
///         decisions (mercenary contract outcomes, dialogue choices, mission completion
///         method). It feeds <see cref="systems.corruption.CorruptionSystem.RollBacklash" />
///         to modulate the chance of corruption side-effects when dark spells are cast.
///     </para>
///     <para>
///         <b>Sign convention:</b> per the project design, <i>negative</i> karma represents a
///         corrupt-leaning protagonist (the user has embraced morally-grey choices). Such a
///         character suffers <b>less</b> corruption backlash from dark spells. <i>Positive</i>
///         karma represents a virtuous path; dark spells backfire more often on a virtuous user.
///     </para>
///     <para>
///         Register as an AutoLoad singleton. Subscribe to <c>OnKarmaChanged</c> for narrative
///         hooks; <see cref="BattleEventBus" /> also publishes a typed event whenever the
///         value changes.
///     </para>
/// </remarks>
public sealed partial class KarmaManager : BaseManager
{
    #region Constants

    /// <summary>Inclusive minimum karma value.</summary>
    public const int MinKarma = -100;

    /// <summary>Inclusive maximum karma value.</summary>
    public const int MaxKarma = 100;

    #endregion

    #region Singleton

    /// <summary>The active singleton instance, set in <see cref="Initialize" />.</summary>
    public new static KarmaManager? Instance { get; private set; }

    #endregion

    #region Public Properties

    /// <summary>Current karma value, clamped to <c>[-100, +100]</c>.</summary>
    public int Karma { get; private set; }

    #endregion

    #region Class Initialization

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
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Set the karma value directly, clamped to the legal range.
    /// </summary>
    /// <param name="value">New karma value.</param>
    /// <param name="reason">Optional human-readable reason for the change.</param>
    public void SetKarma(int value, string reason = "")
    {
        int clamped = Mathf.Clamp(value, MinKarma, MaxKarma);
        if (clamped == Karma)
            return;

        int old = Karma;
        Karma = clamped;
        BattleEventBus.Instance?.Publish(new BattleEvents.KarmaChanged(old, Karma, reason));
    }

    /// <summary>
    ///     Increment / decrement the karma value by <paramref name="delta" />.
    /// </summary>
    /// <param name="delta">Signed change.</param>
    /// <param name="reason">Optional human-readable reason for the change.</param>
    public void AdjustKarma(int delta, string reason = "")
    {
        SetKarma(Karma + delta, reason);
    }

    #endregion

    #region Class Destroyer

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion
}
