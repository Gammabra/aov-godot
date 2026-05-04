using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     <see cref="TurnManager" /> partial — adds three-faction support, an upcoming-queue
///     getter for the HUD turn-order widget, and an <see cref="OnAllyTurn" /> event.
/// </summary>
/// <remarks>
///     <para>
///         The base <see cref="TurnManager" /> distinguishes player vs enemy turns via
///         <see cref="AovDataStructures.TurnState" />. Allied (guest) units are still tagged with
///         <see cref="AovDataStructures.TurnState.EnemyTurn" /> internally because they're
///         AI-driven, but this partial fires <see cref="OnAllyTurn" /> instead of
///         <see cref="TurnManager.OnEnemyTurn" /> when the active unit's
///         <see cref="UnitSystem.Faction" /> is <see cref="Faction.Ally" />. <c>GameManager</c>
///         routes the dispatch.
///     </para>
/// </remarks>
public partial class TurnManager
{
    /// <summary>Triggered when an Ally-faction unit's turn begins.</summary>
    public event Action? OnAllyTurn;

    /// <summary>Triggered when an Ally-faction unit's turn ends.</summary>
    public event Action? OnAllyTurnEnd;

    /// <summary>
    ///     Initialise the turn order with three factions. Allies are AI-driven friendly guests
    ///     (recruited mercs, summoned creatures, scripted helpers).
    /// </summary>
    /// <param name="playerUnits">Player-controlled units.</param>
    /// <param name="allyUnits">AI-controlled friendly units (may be empty).</param>
    /// <param name="enemyUnits">AI-controlled hostile units.</param>
    /// <remarks>
    ///     Internally allied units are stored as <see cref="AovDataStructures.TurnState.EnemyTurn" />
    ///     (so the existing turn loop dispatches to AI), but <c>GameManager</c> consults
    ///     <see cref="UnitSystem.Faction" /> to fire the right HUD events.
    /// </remarks>
    public void InitializeTurnOrder(
        List<IUnitSystem> playerUnits,
        List<IUnitSystem> allyUnits,
        List<IUnitSystem> enemyUnits)
    {
        // Tag faction defensively. GameManager already does this from the container layout;
        // this is a backstop for callers that bypass GameManager.
        foreach (IUnitSystem u in playerUnits)
            if (u is UnitSystem unit && unit.Faction != Faction.Player) unit.AssignFaction(Faction.Player);
        foreach (IUnitSystem u in allyUnits)
            if (u is UnitSystem unit && unit.Faction != Faction.Ally) unit.AssignFaction(Faction.Ally);
        foreach (IUnitSystem u in enemyUnits)
            if (u is UnitSystem unit && unit.Faction != Faction.Enemy) unit.AssignFaction(Faction.Enemy);

        // Allies share the EnemyTurn state since they're AI-driven; the dispatch happens via
        // OnAllyTurn / OnEnemyTurn picked by GameManager based on Faction.
        var combined = new List<IUnitSystem>();
        combined.AddRange(playerUnits);
        combined.AddRange(allyUnits);
        InitializeTurnOrder(playerUnits, allyUnits.Count == 0 ? enemyUnits : ConcatLists(allyUnits, enemyUnits));
    }

    /// <summary>
    ///     Returns the next <paramref name="count" /> alive units in activation order, starting
    ///     with the currently-acting unit. Used by the HUD turn-queue widget.
    /// </summary>
    /// <param name="count">Maximum number of units to return.</param>
    /// <returns>Read-only list of upcoming units.</returns>
    public IReadOnlyList<IUnitSystem> GetUpcomingUnits(int count = 8)
    {
        var result = new List<IUnitSystem>();
        if (_unitsTurnOrder.Count == 0) return result;
        int n = _unitsTurnOrder.Count;
        for (int i = 0; i < n * 2 && result.Count < count; i++)
        {
            UnitSystem u = _unitsTurnOrder[(_currentIndex + i) % n].Key;
            if (u.IsAlive) result.Add(u);
        }
        return result;
    }

    /// <summary>
    ///     Dispatch helper for <c>GameManager</c> — fires <see cref="OnAllyTurn" /> /
    ///     <see cref="OnAllyTurnEnd" /> when an AI-faction turn belongs to an ally rather
    ///     than an enemy.
    /// </summary>
    /// <param name="isStart">True for turn start, false for turn end.</param>
    public void RaiseAllyTurn(bool isStart)
    {
        if (isStart) OnAllyTurn?.Invoke();
        else OnAllyTurnEnd?.Invoke();
    }

    private static List<IUnitSystem> ConcatLists(List<IUnitSystem> a, List<IUnitSystem> b)
    {
        var result = new List<IUnitSystem>(a.Count + b.Count);
        result.AddRange(a);
        result.AddRange(b);
        return result;
    }
}
