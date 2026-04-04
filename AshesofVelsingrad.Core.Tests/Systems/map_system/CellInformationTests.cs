using NUnit.Framework;
using Moq;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Core.Tests.Systems;

[TestFixture]
public class CellInformationTests
{
    private const int _x = 1;
    private const int _y = 2;
    private const int _z = 3;
    private const AovDataStructures.CellType _defaultTerrain = AovDataStructures.CellType.Grass;

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
}