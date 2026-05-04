using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Systems;

/// <summary>
///     Extra coverage for <see cref="StatusEffect{TTarget}" /> properties + branches that
///     the broader <c>StatusEffectSystemTest</c> doesn't reach: the default virtual
///     <c>IsPurifiable</c>, the optional <c>EffectToSpread</c> hook, the cell-target branch
///     of <c>OnTurnPassed</c>, and the permanent-effect short-circuit in
///     <c>ResetDuration</c>.
/// </summary>
[TestFixture]
public class StatusEffectExtraTests
{
    private sealed class GenericEffect<T> : StatusEffect<T>
    {
        public GenericEffect(int duration, bool stackable = false, IStatusEffect? spread = null)
            : base("Generic", "desc", duration, stackable)
        {
            EffectToSpread = spread;
        }
    }

    private sealed class CurseEffect : StatusEffect<IUnitSystem>
    {
        public CurseEffect() : base("Curse", "Cannot be cleansed.", 5, false) { }
        public override bool IsPurifiable => false;
    }

    [Test]
    public void IsPurifiable_DefaultIsTrue()
    {
        var effect = new GenericEffect<IUnitSystem>(duration: 3);
        Assert.That(effect.IsPurifiable, Is.True,
            "Per the design's status table, every status except Curse is purifiable by default.");
    }

    [Test]
    public void IsPurifiable_CanBeOverriddenToFalseForCurseStyleEffects()
    {
        var curse = new CurseEffect();
        Assert.That(curse.IsPurifiable, Is.False);
    }

    [Test]
    public void ShouldApplyTwice_DefaultIsFalse()
    {
        var effect = new GenericEffect<IUnitSystem>(duration: 3);
        Assert.That(effect.ShouldApplyTwice, Is.False);
    }

    [Test]
    public void EffectToSpread_DefaultsNullAndCanBeAssignedThroughInit()
    {
        var spread = new GenericEffect<IUnitSystem>(duration: 1);
        var carrier = new GenericEffect<IUnitSystem>(duration: 3, spread: spread);
        Assert.That(carrier.EffectToSpread, Is.SameAs(spread));

        var lone = new GenericEffect<IUnitSystem>(duration: 3);
        Assert.That(lone.EffectToSpread, Is.Null);
    }

    [Test]
    public void OnTurnPassed_OnCellTarget_DecrementsDuration()
    {
        // Covers the "target is CellInformation cell" branch in OnTurnPassed.
        var cell = new CellInformation(2, 0, 3, AovDataStructures.CellType.Grass, true);
        var effect = new GenericEffect<CellInformation>(duration: 4);

        effect.OnTurnPassed(cell);

        Assert.That(effect.Duration, Is.EqualTo(3));
    }

    [Test]
    public void OnTurnPassed_OnPermanentEffect_DoesNotDecrement()
    {
        // Constants.PermanentStatusEffect (-1) means the effect never expires — the OnTurnPassed
        // early-return branch needs explicit coverage.
        var effect = new GenericEffect<IUnitSystem>(duration: Constants.PermanentStatusEffect);
        // We can't easily build an IUnitSystem here; OnTurnPassed only logs against the target,
        // it doesn't call methods on it, so a null cast through `default` is enough for the
        // permanent-effect branch since it returns BEFORE the target type-check.
        effect.OnTurnPassed(default!);
        Assert.That(effect.Duration, Is.EqualTo(Constants.PermanentStatusEffect));
    }

    [Test]
    public void ResetDuration_OnPermanent_DoesNothing()
    {
        var effect = new GenericEffect<IUnitSystem>(duration: Constants.PermanentStatusEffect);
        effect.ResetDuration(10);
        Assert.That(effect.Duration, Is.EqualTo(Constants.PermanentStatusEffect));
    }

    [Test]
    public void ResetDuration_OnlyExtendsWhenNewDurationIsLonger()
    {
        var effect = new GenericEffect<IUnitSystem>(duration: 5);
        effect.ResetDuration(2);
        Assert.That(effect.Duration, Is.EqualTo(5), "Shorter resets must not shorten the active duration.");

        effect.ResetDuration(8);
        Assert.That(effect.Duration, Is.EqualTo(8), "Longer resets should extend it.");
    }

    [Test]
    public void AddStack_OnNonStackable_KeepsStackCountAtOne()
    {
        var effect = new GenericEffect<IUnitSystem>(duration: 3, stackable: false);
        effect.AddStack();
        effect.AddStack();
        Assert.That(effect.StackCount, Is.EqualTo(1));
    }

    [Test]
    public void AddStack_OnStackable_IncrementsStackCount()
    {
        var effect = new GenericEffect<IUnitSystem>(duration: 3, stackable: true);
        effect.AddStack();
        effect.AddStack();
        Assert.That(effect.StackCount, Is.EqualTo(3),
            "Initial stack count is 1; AddStack twice should bring it to 3.");
    }
}
