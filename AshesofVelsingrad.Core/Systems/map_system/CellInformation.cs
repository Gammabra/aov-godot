using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Represents the information for a single grid cell in the map,
///     including its coordinates, ground type, walkability, and the unit occupying it.
/// </summary>
/// <param name="x">The x-coordinate of the grid cell.</param>
/// <param name="y">The y-coordinate of the grid cell.</param>
/// <param name="z">The z-coordinate of the grid cell.</param>
/// <param name="cellType">The <see cref="CellType" /> defining the terrain of the cell.</param>
/// <param name="isWalkable">Indicates whether the cell can be walked on.</param>
public sealed class CellInformation(
    int x,
    int y,
    int z,
    AovDataStructures.CellType cellType,
    bool isWalkable
) : EffectTarget<CellInformation>
{
    #region Public Properties

    public int X { get; } = x;
    public int Y { get; } = y;
    public int Z { get; } = z;
    public AovDataStructures.CellType CellType { get; } = cellType;
    public bool IsWalkable { get; private set; } = isWalkable;
    public IUnitSystem? Unit { get; private set; }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Set the logic when a unit has entered the cell
    /// </summary>
    /// <param name="unit">The unit that entered the cell</param>
    public void OnUnitEntered(IUnitSystem unit)
    {
        foreach (StatusEffect<CellInformation> effect in GetActiveEffects())
            if (effect.EffectToSpread != null)
                unit.SetStatusEffectOnUnit((StatusEffect<IUnitSystem>)(object)effect.EffectToSpread);
    }

    /// <summary>
    ///     Set <see cref="IsWalkable" /> variable to the opposite one
    /// </summary>
    public void SetWalkable()
    {
        IsWalkable = !IsWalkable;
    }

    /// <summary>
    ///     Set <see cref="Unit" />
    /// </summary>
    /// <param name="unit">The unit that is on the cell</param>
    public void SetUnit(IUnitSystem? unit = null)
    {
        Unit = unit;
    }

    #endregion
}
