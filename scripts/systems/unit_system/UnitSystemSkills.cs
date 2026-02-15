using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace AshesOfVelsingrad.Systems;

public abstract partial class UnitSystem
{
    #region Public Properties

    /// <summary>List of active (usable) skills available to this unit.</summary>
    public List<SkillSystem> ActiveSkills { get; protected set; } = [];

    /// <summary>List of passive (always-on) skills applied to this unit.</summary>
    public List<SkillSystem> PassiveSkills { get; protected set; } = [];

    #endregion

    #region Public Methods

    /// <summary>
    ///     Calculates all reachable cells for this unit based on the skill range and the unit position.
    /// </summary>
    /// <param name="map">The map to evaluate reachable cells on.</param>
    /// <param name="skill">The selected skill</param>
    /// <returns>A list of reachable coordinates (x, y, z).</returns>
    /// <remarks>
    ///     This method uses a Breadth-First Search (BFS) algorithm to evaluate all valid cells
    ///     considering vertical traversal (e.g. stairs, cliffs).
    /// </remarks>
    public virtual List<Vector3I> GetReachableCellsForSkills(MapSystem map, SkillSystem skill)
    {
        List<Vector3I> possibleCells = [];
        Queue<(Vector3I pos, int dist)> toExplore = new();
        Vector3I? unitPosition = map.GetUnitPosition(this);
        List<Vector3I> visitedCells = [];
        Vector3I[] directions =
        [
            new Vector3I(-1, 0, 0), // Left
            new Vector3I(1, 0, 0), // Right
            new Vector3I(0, 1, 0), // Up
            new Vector3I(0, -1, 0), // Down
            new Vector3I(0, 0, 1), // Forward
            new Vector3I(0, 0, -1) // Backward
        ];

        if (unitPosition == null)
            return possibleCells;

        possibleCells.Add(unitPosition.Value);

        // Queue the unit position
        visitedCells.Add(unitPosition.Value);
        toExplore.Enqueue((unitPosition.Value, 0));
        while (toExplore.Count > 0)
        {
            (Vector3I pos, int dist) currentPos = toExplore.Dequeue();

            if (currentPos.dist > skill.Range)
                continue;

            // Set the current position to visited cells
            if (currentPos.pos != unitPosition.Value && !visitedCells.Contains(currentPos.pos))
            {
                possibleCells.Add(currentPos.pos);
                visitedCells.Add(currentPos.pos);
            }

            // Check the possible neighbor at the current position
            foreach (Vector3I dir in directions)
            {
                Vector3I pos = currentPos.pos;
                pos.X += dir.X;
                pos.Y += dir.Y;
                pos.Z += dir.Z;

                try
                {
                    map.IsEmpty(pos.X, pos.Y, pos.Z);
                    toExplore.Enqueue((pos, currentPos.dist + 1));
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Just continue the loop without enqueue the position.
                    // If the exception must be handled one day, it must be handled here.
                }
            }
        }

        possibleCells = possibleCells
            .Where(c => !map.IsEmpty(c.X, c.Y, c.Z))
            .ToList();

        return possibleCells;
    }

    /// <summary>
    ///     Performs an attack on the specified targets.
    /// </summary>
    /// <param name="targets">List of target units to attack.</param>
    /// <param name="map">Reference to the map system for positional logic.</param>
    /// <param name="skill">Tells which active skill to use</param>
    public virtual void Play(List<UnitSystem> targets, MapSystem? map, SkillSystem skill)
    {
        if (!ActiveSkills.Contains(skill))
        {
            GD.PrintErr($"The unit does not have the skill '{skill.Name}'");
            return;
        }

        if (Mana < skill.ManaCost)
        {
            GD.PrintErr($"The unit mana point is smaller than the skill '{skill.Name}'");
            return;
        }

        skill.Use(this, targets, map);
        Mana -= skill.ManaCost;
        ReportSystemUnitHasPlayed();
    }

    #endregion
}
