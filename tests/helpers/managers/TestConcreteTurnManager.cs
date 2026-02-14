using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using Godot;

namespace UnitTests;

public partial class TestConcreteTurnManager : TurnManager
{
    private UnitSystem? _currentUnit;
    public bool TurnLoopEnded { get; private set; }

    public void SetCurrentUnit(UnitSystem unit)
    {
        _currentUnit = unit;
    }

    public new UnitSystem GetCurrentUnit()
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
