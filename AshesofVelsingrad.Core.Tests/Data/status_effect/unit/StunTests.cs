using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using Moq;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Data;

[TestFixture]
public class StunTests
{
    private Mock<IUnitSystem> _mockUnit = null!;
    private const int _duration = 1;

    [SetUp]
    public void SetUp()
    {
        _mockUnit = new Mock<IUnitSystem>();
    }

    [Test]
    public void OnApply_CallsUnitOnEffectControlApplied()
    {
        // Arrange
        var stun = new Stun(_duration);
        bool called = false;
        _mockUnit.Setup(u => u.OnEffectControlApplied()).Callback(() => called = true);

        // Act
        stun.OnApply(_mockUnit.Object);

        // Assert
        Assert.That(called, Is.True);
    }

    [Test]
    public void OnRemove_CallsUnitOnEffectControlRemoved()
    {
        // Arrange
        var stun = new Stun(_duration);
        bool called = false;
        _mockUnit.Setup(u => u.OnEffectControlRemoved()).Callback(() => called = true);

        // Act
        stun.OnRemove(_mockUnit.Object);

        // Assert
        Assert.That(called, Is.True);
    }

    [Test]
    public void ShouldApplyTwice_ReturnsTrue()
    {
        var stun = new Stun(_duration);
        Assert.That(stun.ShouldApplyTwice, Is.True);
    }

    [Test]
    public void Constructor_SetsInheritedPropertiesCorrectly()
    {
        var stun = new Stun(_duration);
        Assert.That(stun.Name, Is.EqualTo("Stun"));
        Assert.That(stun.Duration, Is.EqualTo(_duration));
    }

    [Test]
    public void OnApply_WhenTargetIsNull_DoesNotCallUnitMethods()
    {
        var stun = new Stun(_duration);
        Assert.DoesNotThrow(() => stun.OnApply(null!));
    }

    [Test]
    public void OnRemove_WhenTargetIsNull_DoesNotCallUnitMethods()
    {
        var stun = new Stun(_duration);
        Assert.DoesNotThrow(() => stun.OnRemove(null!));
    }
}
