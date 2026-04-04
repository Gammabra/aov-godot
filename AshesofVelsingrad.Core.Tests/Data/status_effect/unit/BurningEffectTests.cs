using NUnit.Framework;
using Moq;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Core.Tests.Data;

[TestFixture]
public class BurningEffectTests
{
    private Mock<IUnitSystem>? _mockUnit;
    private const int _duration = 3;
    private const float _damageAmount = 15f;
    private const AovDataStructures.ModifierType _modType = AovDataStructures.ModifierType.Flat;

    [SetUp]
    public void SetUp()
    {
        _mockUnit = new Mock<IUnitSystem>();
    }

    [Test]
    public void OnTurnPassed_CallsUnitOnEffectDamageWithCorrectParameters()
    {
        if (_mockUnit == null)
        {
            Assert.Fail("MockUnit not initialized properly.");
            return;
        }

        // Arrange
        var effect = new BurningEffect(_duration, _modType, _damageAmount);

        // Act
        effect.OnTurnPassed(_mockUnit.Object);

        // Assert
        // We verify that the unit actually takes the damage
        _mockUnit.Verify(u => u.OnEffectDamage(_modType, _damageAmount), Times.Once);
    }

    [Test]
    public void Constructor_SetsInheritedPropertiesCorrectly()
    {
        // Act
        var effect = new BurningEffect(_duration, _modType, _damageAmount);

        // Assert
        Assert.That(effect.Name, Is.EqualTo("Burning"));
        Assert.That(effect.Duration, Is.EqualTo(_duration));
        Assert.That(effect.Amount, Is.EqualTo(_damageAmount));
    }

    [Test]
    public void OnTurnPassed_MultipleTurns_CallsDamageEachTime()
    {
        if (_mockUnit == null)
        {
            Assert.Fail("MockUnit not initialized properly.");
            return;
        }

        // Arrange
        var effect = new BurningEffect(_duration, _modType, _damageAmount);

        // Act
        effect.OnTurnPassed(_mockUnit.Object);
        effect.OnTurnPassed(_mockUnit.Object);

        // Assert
        _mockUnit.Verify(u => u.OnEffectDamage(_modType, _damageAmount), Times.Exactly(2));
    }
}
