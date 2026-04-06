using NUnit.Framework;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Core.Tests.Systems;

[TestFixture]
public class EffectTargetTests
{
    // A simple concrete class to test the base logic
    private class TestTarget : EffectTarget<TestTarget> { }

    // Mocking StatusEffect is tricky because it's a class, 
    // so we create a simple concrete version for testing.
    private class MockStatusEffect : StatusEffect<TestTarget>
    {
        public MockStatusEffect(string name) : base(name, "Desc", 1, true) { } // Make stackable for tests
        public void SetDuration(int duration) => Duration = duration;    }

    private class DifferentStatusEffect : StatusEffect<TestTarget>
    {
        public DifferentStatusEffect() : base("Different", "Desc", 1, false) { }
    }

    private TestTarget? _target;

    [SetUp]
    public void SetUp()
    {
        _target = new TestTarget();
    }

    [Test]
    public void ApplyEffect_AddsEffectToList()
    {
        // Arrange
        var effect = new MockStatusEffect("Burn");

        // Act
        _target.ApplyEffect(effect);

        // Assert
        Assert.That(_target.GetActiveEffects(), Contains.Item(effect));
        Assert.That(_target.GetActiveEffects().Count, Is.EqualTo(1));
    }

    [Test]
    public void RemoveEffect_RemovesSpecificEffectFromList()
    {
        // Arrange
        var effect = new MockStatusEffect("Burn");
        _target.ApplyEffect(effect);

        // Act
        _target.RemoveEffect(effect);

        // Assert
        Assert.That(_target.GetActiveEffects(), Does.Not.Contain(effect));
        Assert.That(_target.GetActiveEffects().Count, Is.EqualTo(0));
    }

    [Test]
    public void HasEffect_ReturnsTrue_WhenEffectTypeExists()
    {
        // Arrange
        _target.ApplyEffect(new MockStatusEffect("Burn"));
        _target.ApplyEffect(new DifferentStatusEffect());

        // Act & Assert
        Assert.That(_target.HasEffect<MockStatusEffect>(), Is.True, "Should find MockStatusEffect");
        Assert.That(_target.HasEffect<DifferentStatusEffect>(), Is.True, "Should find DifferentStatusEffect");
    }

    [Test]
    public void HasEffect_ReturnsFalse_WhenEffectTypeDoesNotExist()
    {
        // Arrange
        _target.ApplyEffect(new DifferentStatusEffect());

        // Act & Assert
        Assert.That(_target.HasEffect<MockStatusEffect>(), Is.False, "Should not find MockStatusEffect if it wasn't added");
    }

    [Test]
    public void GetActiveEffects_ReturnsInternalListReference()
    {
        // Act
        var effects = _target.GetActiveEffects();

        // Assert
        Assert.That(effects, Is.Not.Null);
        Assert.That(effects, Is.InstanceOf<List<StatusEffect<TestTarget>>>());
    }

    [Test]
    public void StatusEffect_OnTurnPassed_DoesNotLogForUnknownTargetType()
    {
        // Arrange
        var effect = new MockStatusEffect("Test");
        _target.ApplyEffect(effect);

        // Act
        effect.OnTurnPassed(_target);

        // Assert
        // Since TestTarget is not IUnitSystem or CellInformation, no logging occurs
        // This covers the else branch in StatusEffect.OnTurnPassed
        Assert.That(effect.Duration, Is.EqualTo(0)); // Duration decremented
    }

    [Test]
    public void StatusEffect_AddStack_IncreasesStackCount_WhenStackable()
    {
        // Arrange
        var effect = new MockStatusEffect("Stackable");

        // Act
        effect.AddStack();

        // Assert
        Assert.That(effect.StackCount, Is.EqualTo(2));
    }

    [Test]
    public void StatusEffect_ResetDuration_UpdatesDuration_WhenNotPermanent()
    {
        // Arrange
        var effect = new MockStatusEffect("Test");
        effect.SetDuration(2);

        // Act
        effect.ResetDuration(5);

        // Assert
        Assert.That(effect.Duration, Is.EqualTo(5));
    }

    [Test]
    public void StatusEffect_ResetDuration_DoesNotUpdate_WhenPermanent()
    {
        // Arrange
        var effect = new MockStatusEffect("Test");
        effect.SetDuration(Constants.PermanentStatusEffect);

        // Act
        effect.ResetDuration(5);

        // Assert
        Assert.That(effect.Duration, Is.EqualTo(Constants.PermanentStatusEffect));
    }
}