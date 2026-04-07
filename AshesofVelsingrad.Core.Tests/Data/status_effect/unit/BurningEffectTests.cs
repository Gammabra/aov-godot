using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Moq;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Data;

[TestFixture]
public class BurningEffectTests
{
    private Mock<IUnitSystem> _mockUnit = null!;
    private const int _duration = 3;
    private const float _damage = 5f;
    private const AovDataStructures.ModifierType _modType = AovDataStructures.ModifierType.Flat;

    [SetUp]
    public void SetUp()
    {
        _mockUnit = new Mock<IUnitSystem>();
    }

    [Test]
    public void OnTurnPassed_CallsUnitOnEffectDamage()
    {
        // Arrange
        var burning = new BurningEffect(_duration, _modType, _damage);
        bool called = false;

        // Match the specific damage call
        _mockUnit.Setup(u => u.OnEffectDamage(AovDataStructures.ModifierType.Flat, _damage))
                 .Callback(() => called = true);

        // Act
        burning.OnTurnPassed(_mockUnit.Object);

        // Assert
        Assert.That(called, Is.True);
    }

    [Test]
    public void Constructor_SetsInheritedPropertiesCorrectly()
    {
        var burning = new BurningEffect(_duration, _modType, _damage);
        Assert.That(burning.Name, Is.EqualTo("Burning"));
        Assert.That(burning.Duration, Is.EqualTo(_duration));
        Assert.That(burning.Amount, Is.EqualTo(_damage));
    }

    [Test]
    public void OnTurnPassed_WhenTargetIsNull_DoesNotThrow()
    {
        var burning = new BurningEffect(_duration, _modType, _damage);
        Assert.DoesNotThrow(() => burning.OnTurnPassed(null!));
    }
}
