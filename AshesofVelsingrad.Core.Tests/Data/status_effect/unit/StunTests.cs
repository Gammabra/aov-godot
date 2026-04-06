using NUnit.Framework;
using Moq;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.Core.Tests.Data;

[TestFixture]
public class StunTests
{
    private Mock<IUnitSystem>? _mockUnit;
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

        // Act
        stun.OnApply(_mockUnit.Object);

        // Assert
        _mockUnit.Verify(u => u.OnEffectControlApplied(), Times.Once);
    }

    [Test]
    public void OnRemove_CallsUnitOnEffectControlRemoved()
    {
        // Arrange
        var stun = new Stun(_duration);

        // Act
        stun.OnRemove(_mockUnit.Object);

        // Assert
        _mockUnit.Verify(u => u.OnEffectControlRemoved(), Times.Once);
    }

    [Test]
    public void ShouldApplyTwice_ReturnsTrue()
    {
        // Arrange
        var stun = new Stun(_duration);

        // Assert
        Assert.That(stun.ShouldApplyTwice, Is.True);
    }

    [Test]
    public void Constructor_SetsInheritedPropertiesCorrectly()
    {
        // Act
        var stun = new Stun(_duration);

        // Assert
        Assert.That(stun.Name, Is.EqualTo("Stun"));
        Assert.That(stun.Duration, Is.EqualTo(_duration));
        // Note: Amount and ModifierType aren't used here, but they exist in the base
    }

    [Test]
    public void OnApply_WhenTargetIsNull_DoesNotCallUnitMethods()
    {
        // Arrange
        var buffer = new Stun(_duration);

        // Act & Assert
        // We pass null. The 'is IUnitSystem' check will fail.
        // We just want to ensure it doesn't throw an exception.
        Assert.DoesNotThrow(() => buffer.OnApply(null!));
    }

    [Test]
    public void OnRemove_WhenTargetIsNull_DoesNotCallUnitMethods()
    {
        // Arrange
        var buffer = new Stun(_duration);

        // Act & Assert
        // We pass null. The 'is IUnitSystem' check will fail.
        // We just want to ensure it doesn't throw an exception.
        Assert.DoesNotThrow(() => buffer.OnRemove(null!));
    }
}