using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.Helpers.Managers;

public partial class TestConcreteTurnManager : TurnManager
{
    private IUnitSystem? _currentUnit;
    public bool TurnLoopEnded { get; private set; }

    public void SetCurrentUnit(IUnitSystem unit)
    {
        _currentUnit = unit;
    }

    public new IUnitSystem GetCurrentUnit()
    {
        return _currentUnit ?? base.GetCurrentUnit();
    }

    // If EndTurnManagerLoop is NOT virtual, use new:
    public override void EndTurnManagerLoop()
    {
        GD.Print("[TEST] EndTurnManagerLoop called");
        TurnLoopEnded = true;
    }
}
