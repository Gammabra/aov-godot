using NUnit.Framework;
using Moq;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Core.Tests.Data;

[TestFixture]
public class AtkBufferTests
{
    // FIX 1: Use null! to remove nullable warnings and hidden compiler null-checks
    private Mock<IUnitSystem> _mockUnit = null!;
    private const int _duration = 3;
    private const float _buffAmount = 10f;
    private const AovDataStructures.ModifierType _modType = AovDataStructures.ModifierType.Flat;

    [SetUp]
    public void SetUp()
    {
        _mockUnit = new Mock<IUnitSystem>();
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

    [Test]
    public void OnApply_CallsUnitEffectAppliedWithCorrectParameters()
    {
        var buffer = new AtkBuffer(_duration, _modType, _buffAmount);
        bool called = false;
        _mockUnit.Setup(u => u.OnEffectModifierApplied(AovDataStructures.StatTypeWithModifier.Atk, AovDataStructures.ModifierType.Flat, 10f))
                 .Callback(() => called = true);

        buffer.OnApply(_mockUnit.Object);

        Assert.That(called, Is.True);
    }

    [Test]
    public void OnRemove_CallsUnitEffectRemovedWithCorrectParameters()
    {
        var buffer = new AtkBuffer(_duration, _modType, _buffAmount);
        bool called = false;
        _mockUnit.Setup(u => u.OnEffectModifierRemoved(AovDataStructures.StatTypeWithModifier.Atk, AovDataStructures.ModifierType.Flat, 10f))
                 .Callback(() => called = true);

        buffer.OnRemove(_mockUnit.Object);

        Assert.That(called, Is.True);
    }
}