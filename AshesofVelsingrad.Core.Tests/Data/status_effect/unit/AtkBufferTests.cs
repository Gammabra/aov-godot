using NUnit.Framework;
using Moq;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Core.Tests.Data;

[TestFixture]
public class AtkBufferTests
{
    private Mock<IUnitSystem>? _mockUnit;
    private const int _duration = 3;
    private const float _buffAmount = 10f;
    private const AovDataStructures.ModifierType _modType = AovDataStructures.ModifierType.Flat;

    [SetUp]
    public void SetUp()
    {
        _mockUnit = new Mock<IUnitSystem>();
    }

    [Test]
    public void OnApply_CallsUnitEffectAppliedWithCorrectParameters()
    {
        // Arrange
        var buffer = new AtkBuffer(_duration, _modType, _buffAmount);

        // Act
        buffer.OnApply(_mockUnit.Object);

        // Assert
        _mockUnit.Verify(u => u.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Atk, 
            _modType, 
            _buffAmount
        ), Times.Once);
    }

    [Test]
    public void OnRemove_CallsUnitEffectRemovedWithCorrectParameters()
    {
        // Arrange
        var buffer = new AtkBuffer(_duration, _modType, _buffAmount);

        // Act
        buffer.OnRemove(_mockUnit.Object);

        // Assert
        _mockUnit.Verify(u => u.OnEffectModifierRemoved(
            AovDataStructures.StatTypeWithModifier.Atk, 
            _modType, 
            _buffAmount
        ), Times.Once);
    }

    [Test]
    public void Constructor_SetsInheritedPropertiesCorrectly()
    {
        // Act
        var buffer = new AtkBuffer(_duration, _modType, _buffAmount);

        // Assert
        Assert.That(buffer.Name, Is.EqualTo("AtkBuffer"));
        Assert.That(buffer.Duration, Is.EqualTo(_duration));
        Assert.That(buffer.Amount, Is.EqualTo(_buffAmount));
        Assert.That(buffer.ModifierType, Is.EqualTo(_modType));
    }

    [Test]
    public void OnApply_WhenTargetIsNull_DoesNotCallUnitMethods()
    {
        // Arrange
        var buffer = new AtkBuffer(_duration, _modType, _buffAmount);

        // Act & Assert
        // We pass null. The 'is IUnitSystem' check will fail.
        // We just want to ensure it doesn't throw an exception.
        Assert.DoesNotThrow(() => buffer.OnApply(null!));
    }

    [Test]
    public void OnRemove_WhenTargetIsNull_DoesNotCallUnitMethods()
    {
        // Arrange
        var buffer = new AtkBuffer(_duration, _modType, _buffAmount);

        // Act & Assert
        Assert.DoesNotThrow(() => buffer.OnRemove(null!));
    }
}
