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
    public virtual List<(int, int, int)> GetReachableCellsForSkills(MapSystem map, SkillSystem skill)
    {
        List<(int, int, int)> possibleCells = [];
        Queue<((int, int, int) pos, int dist)> toExplore = new();
        (int, int, int)? unitPosition = map.GetUnitPosition(this);
        List<(int, int, int)> visitedCells = [];
        (int, int, int)[] directions =
        [
            (-1, 0, 0), // Left
            (1, 0, 0), // Right
            (0, 1, 0), // Up
            (0, -1, 0), // Down
            (0, 0, 1), // Forward
            (0, 0, -1) // Backward
        ];

        if (unitPosition == null)
            return possibleCells;

        possibleCells.Add(unitPosition.Value);

        // Queue the unit position
        visitedCells.Add(unitPosition.Value);
        toExplore.Enqueue((unitPosition.Value, 0));
        while (toExplore.Count > 0)
        {
            ((int, int, int), int) currentPos = toExplore.Dequeue();

            if (currentPos.Item2 > skill.Range)
                continue;

            // Set the current position to visited cells
            if (currentPos.Item1 != unitPosition.Value && !visitedCells.Contains(currentPos.Item1))
            {
                possibleCells.Add(currentPos.Item1);
                visitedCells.Add(currentPos.Item1);
            }

            // Check the possible neighbor at the current position
            foreach ((int, int, int) dir in directions)
            {
                (int, int, int) pos = currentPos.Item1;
                pos.Item1 += dir.Item1;
                pos.Item2 += dir.Item2;
                pos.Item3 += dir.Item3;

                try
                {
                    map.IsEmpty(pos.Item1, pos.Item2, pos.Item3);
                    toExplore.Enqueue((pos, currentPos.Item2 + 1));
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Just continue the loop without enqueue the position.
                    // If the exception must be handled one day, it must be handled here.
                }
            }
        }

        possibleCells = possibleCells
            .Where(c => !map.IsEmpty(c.Item1, c.Item2, c.Item3))
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

        if (ManaPoint < skill.ManaCost)
        {
            GD.PrintErr($"The unit mana point is smaller than the skill '{skill.Name}'");
            return;
        }

        skill.Use(this, targets, map);
        ManaPoint -= skill.ManaCost;
        ReportSystemUnitHasPlayed();
    }

    #endregion
}
