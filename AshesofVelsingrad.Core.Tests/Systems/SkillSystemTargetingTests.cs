using System.Collections.Generic;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Systems;

/// <summary>
///     Verifies <see cref="SkillSystem.IsTargetCellValid" /> — the optional cell-level
///     targeting predicate added to support skills with extra rules beyond plain range
///     (cardinal-only Charge, line-of-sight, etc.). Default behaviour: any cell is valid.
///     Subclass behaviour: cardinal-alignment example mirroring how Charge filters its
///     red target tiles.
/// </summary>
[TestFixture]
public class SkillSystemTargetingTests
{
    private sealed class PlainSkill : SkillSystem
    {
        public PlainSkill()
        {
            Name = "Plain";
            Range = 3;
            TargetType = AovDataStructures.TargetTypes.SingleEnemy;
        }
        public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { }
    }

    /// <summary>
    ///     Sample subclass that mirrors <c>Charge</c>'s cardinal-alignment rule. Independent
    ///     of the actual Charge implementation so the test stays in Core (no Godot deps).
    /// </summary>
    private sealed class CardinalSkill : SkillSystem
    {
        public CardinalSkill()
        {
            Name = "Cardinal";
            Range = 4;
            TargetType = AovDataStructures.TargetTypes.SingleEnemy;
        }

        public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { }

        public override bool IsTargetCellValid(IUnitSystem caster, int x, int y, int z, IMapSystem map)
        {
            (int, int, int)? cp = map.GetUnitPosition(caster);
            if (cp is null) return true;
            (int cx, int cy, int cz) = cp.Value;
            if (cy != y) return false;
            return (cx == x) ^ (cz == z);
        }
    }

    [Test]
    public void Default_IsTargetCellValid_ReturnsTrueForAnyCell()
    {
        var skill = new PlainSkill();
        Assert.That(
            skill.IsTargetCellValid(caster: null!, x: 5, y: 0, z: 7, map: null!),
            Is.True,
            "Default skills should accept any cell — range filtering is applied separately.");
    }

    [Test]
    public void CardinalSkill_AcceptsSameRow()
    {
        var skill = new CardinalSkill();
        var map = new StubMap((1, 0, 4));
        // Same Z, different X → on the same row → legal
        Assert.That(skill.IsTargetCellValid(caster: null!, x: 5, y: 0, z: 4, map), Is.True);
    }

    [Test]
    public void CardinalSkill_AcceptsSameColumn()
    {
        var skill = new CardinalSkill();
        var map = new StubMap((1, 0, 4));
        // Same X, different Z → on the same column → legal
        Assert.That(skill.IsTargetCellValid(caster: null!, x: 1, y: 0, z: 7, map), Is.True);
    }

    [Test]
    public void CardinalSkill_RejectsDiagonal()
    {
        var skill = new CardinalSkill();
        var map = new StubMap((1, 0, 4));
        // Diagonal — different X AND different Z → illegal
        Assert.That(skill.IsTargetCellValid(caster: null!, x: 5, y: 0, z: 7, map), Is.False);
    }

    [Test]
    public void CardinalSkill_RejectsDifferentHeight()
    {
        var skill = new CardinalSkill();
        var map = new StubMap((1, 0, 4));
        // Same row but different Y → illegal
        Assert.That(skill.IsTargetCellValid(caster: null!, x: 5, y: 1, z: 4, map), Is.False);
    }

    [Test]
    public void CardinalSkill_RejectsCasterOwnTile()
    {
        var skill = new CardinalSkill();
        var map = new StubMap((1, 0, 4));
        // Same X AND Z (caster's own cell) → both axes match → XOR is false
        Assert.That(skill.IsTargetCellValid(caster: null!, x: 1, y: 0, z: 4, map), Is.False);
    }

    [Test]
    public void CardinalSkill_NoCasterPosition_FallsBackToTrue()
    {
        var skill = new CardinalSkill();
        var map = new StubMap(null);
        // Map can't locate the caster (off-grid / detached) → predicate falls back to true
        // so the runtime checks downstream of IsTargetCellValid still get a chance to run.
        Assert.That(skill.IsTargetCellValid(caster: null!, x: 5, y: 0, z: 7, map), Is.True);
    }

    /// <summary>
    ///     Minimal <see cref="IMapSystem" /> double — only <see cref="GetUnitPosition" /> is
    ///     ever exercised by these tests. Every other method is a no-op stub. We deliberately
    ///     ignore the <c>unit</c> parameter so callers can pass <c>null!</c> for the caster.
    /// </summary>
    private sealed class StubMap : IMapSystem
    {
        private readonly (int, int, int)? _casterPos;
        public StubMap((int, int, int)? casterPos) { _casterPos = casterPos; }

        public int Width => 16;
        public int Height => 1;
        public int Depth => 16;

        public (int, int, int)? GetUnitPosition(IUnitSystem unit) => _casterPos;

        public IUnitSystem? GetUnitAt(int x, int y, int z) => null;
        public bool IsEmpty(int x, int y, int z) => true;
        public bool IsWalkable(int x, int y, int z) => true;
        public void SetWalkable(int x, int y, int z) { }
        public void MoveUnit(IUnitSystem unit, int x, int y, int z) { }
        public void RemoveUnit(int x, int y, int z) { }
        public AovDataStructures.CellType GetCellType((int, int, int) position) => AovDataStructures.CellType.Grass;
        public void SetStatusEffectOnCells(List<(int, int, int)> cells, StatusEffect<CellInformation> effect) { }
        public void InjectDependencies(StatusEffectSystem statusEffectSystem) { }
        public void PlaceUnits(List<IUnitSystem> playerUnits, List<IUnitSystem> enemyUnits) { }
    }
}
