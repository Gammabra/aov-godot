using NUnit.Framework;
using Moq;
using AshesOfVelsingrad.Systems;
using System.Reflection;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Core.Tests.Systems;

[TestFixture]
public class CellInformationTests
{
    private const int _x = 1;
    private const int _y = 2;
    private const int _z = 3;
    private const AovDataStructures.CellType _defaultTerrain = AovDataStructures.CellType.Grass;

    public class TestStatusEffect<T> : StatusEffect<T>
    {
        public TestStatusEffect(
            string name, 
            int duration, 
            AovDataStructures.ModifierType modType, 
            float amount
        ) : base(name, "desc", duration, false, modType, amount) { }
    }

    [Test]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Act
        var cell = new CellInformation(_x, _y, _z, _defaultTerrain, true);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(cell.X, Is.EqualTo(_x));
            Assert.That(cell.Y, Is.EqualTo(_y));
            Assert.That(cell.Z, Is.EqualTo(_z));
            Assert.That(cell.CellType, Is.EqualTo(_defaultTerrain));
            Assert.That(cell.IsWalkable, Is.True);
            Assert.That(cell.Unit, Is.Null);
        });
    }

    [Test]
    public void SetWalkable_TogglesCurrentState()
    {
        // Arrange
        var cell = new CellInformation(_x, _y, _z, _defaultTerrain, true);

        // Act & Assert
        cell.SetWalkable();
        Assert.That(cell.IsWalkable, Is.False, "Should have toggled to False");

        cell.SetWalkable();
        Assert.That(cell.IsWalkable, Is.True, "Should have toggled back to True");
    }

    [Test]
    public void SetUnit_UpdatesUnitReference()
    {
        // Arrange
        var cell = new CellInformation(_x, _y, _z, _defaultTerrain, true);
        var mockUnit = new Mock<IUnitSystem>();

        // Act
        cell.SetUnit(mockUnit.Object);

        // Assert
        Assert.That(cell.Unit, Is.SameAs(mockUnit.Object));

        // Clear unit
        cell.SetUnit(null);
        Assert.That(cell.Unit, Is.Null);
    }

    [Test]
    public void OnUnitEntered_WhenCellHasSpreadableEffect_AppliesToUnit()
    {
        // Arrange
        var cell = new CellInformation(_x, _y, _z, _defaultTerrain, true);
        var mockUnit = new Mock<IUnitSystem>();
        
        // 1. Create a concrete instance of our stub
        var unitEffect = new TestStatusEffect<IUnitSystem>("Burning", 3, AovDataStructures.ModifierType.Flat, 5f);
        var cellEffect = new TestStatusEffect<CellInformation>("FireTile", 3, AovDataStructures.ModifierType.Flat, 0f);

        // 2. REFLECTION: Force the value into the init-only property
        // We look for the backing field (usually named '<EffectToSpread>k__BackingField')
        var field = typeof(StatusEffect<CellInformation>)
            .GetField("<EffectToSpread>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        field.SetValue(cellEffect, unitEffect);

        cell.ApplyEffect(cellEffect);

        // Act
        cell.OnUnitEntered(mockUnit.Object);

        // Assert
        // If reflection worked, Line 42 in CellInformation will now see the effect!
        mockUnit.Verify(u => u.SetStatusEffectOnUnit(unitEffect), Times.Once);
    }

    [Test]
    public void OnUnitEntered_WhenCellHasNoSpreadableEffect_DoesNothing()
    {
        // Arrange
        var cell = new CellInformation(_x, _y, _z, _defaultTerrain, true);
        var mockUnit = new Mock<IUnitSystem>();
        
        var nonSpreadingEffect = new TestStatusEffect<CellInformation>(
            "Static", 3,
            AovDataStructures.ModifierType.Flat, 0
        );
        // EffectToSpread is null by default
        
        cell.ApplyEffect(nonSpreadingEffect);

        // Act
        cell.OnUnitEntered(mockUnit.Object);

        // Assert
        mockUnit.Verify(u => u.SetStatusEffectOnUnit(It.IsAny<StatusEffect<IUnitSystem>>()), Times.Never);
    }
}