using NUnit.Framework;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Core.Tests.Systems;

[TestFixture]
public class StatusEffectTests
{
    // Concrete implementation for testing abstract base logic
    private class TestEffect<T>(string name, int duration, bool stackable) 
        : StatusEffect<T>(name, "Desc", duration, stackable) { }

    private class DummyTarget { }

    [Test]
    public void Constructor_SetsInitialValues()
    {
        var effect = new TestEffect<DummyTarget>("Haste", 3, true);

        Assert.Multiple(() =>
        {
            Assert.That(effect.Name, Is.EqualTo("Haste"));
            Assert.That(effect.Duration, Is.EqualTo(3));
            Assert.That(effect.IsStackable, Is.True);
            Assert.That(effect.StackCount, Is.EqualTo(1));
            Assert.That(effect.ShouldApplyTwice, Is.False);
        });
    }

    [Test]
    public void OnTurnPassed_DecreasesDuration_WhenNotPermanent()
    {
        // Arrange
        var effect = new TestEffect<DummyTarget>("Poison", 5, false);
        var target = new DummyTarget();

        // Act
        effect.OnTurnPassed(target);

        // Assert
        Assert.That(effect.Duration, Is.EqualTo(4));
    }

    [Test]
    public void OnTurnPassed_DoesNotDecreaseDuration_IfPermanent()
    {
        // Arrange - Assuming -1 is the constant for permanent
        var effect = new TestEffect<DummyTarget>("Aura", Constants.PermanentStatusEffect, false);
        var target = new DummyTarget();

        // Act
        effect.OnTurnPassed(target);

        // Assert
        Assert.That(effect.Duration, Is.EqualTo(Constants.PermanentStatusEffect));
    }

    [Test]
    public void AddStack_IncreasesCount_OnlyIfStackable()
    {
        // Arrange
        var stackable = new TestEffect<DummyTarget>("Might", 3, true);
        var nonStackable = new TestEffect<DummyTarget>("Stun", 1, false);

        // Act
        stackable.AddStack();
        nonStackable.AddStack();

        // Assert
        Assert.That(stackable.StackCount, Is.EqualTo(2));
        Assert.That(nonStackable.StackCount, Is.EqualTo(1));
    }

    [Test]
    public void ResetDuration_UpdatesToHigherValue_ButNeverLower()
    {
        // Arrange
        var effect = new TestEffect<DummyTarget>("Regen", 2, false);

        // Act & Assert
        effect.ResetDuration(5);
        Assert.That(effect.Duration, Is.EqualTo(5), "Should update to higher duration");

        effect.ResetDuration(3);
        Assert.That(effect.Duration, Is.EqualTo(5), "Should NOT overwrite with a shorter duration");
    }

    [Test]
    public void ResetDuration_DoesNotAffectPermanentEffects()
    {
        // Arrange
        var effect = new TestEffect<DummyTarget>("GodMode", Constants.PermanentStatusEffect, false);

        // Act
        effect.ResetDuration(10);

        // Assert
        Assert.That(effect.Duration, Is.EqualTo(Constants.PermanentStatusEffect));
    }
}
