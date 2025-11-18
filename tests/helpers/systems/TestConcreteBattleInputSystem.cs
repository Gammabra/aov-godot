using AshesOfVelsingrad.Systems;
using Godot;

namespace UnitTests;

/// <summary>
/// A fully test-friendly concrete implementation of BattleInputSystem.
/// All Godot-dependent logic (raycasts, camera, viewport) is mocked out.
/// </summary>
public partial class TestConcreteBattleInputSystem : BattleInputSystem
{
    public bool ReadyCalled { get; private set; }
    public bool InitializeCalled { get; private set; }

    public bool PassTurnSignalEmitted { get; private set; }
    public bool MoveOrSelectSignalEmitted { get; private set; }
    public Vector3I? LastMoveOrSelectCell { get; private set; }

    public bool SelectMoveSignalEmitted { get; private set; }
    public bool[] SkillSelectedSignals { get; private set; } = new bool[5];

    /// <summary>
    /// Determines whether _Input() should simulate "valid map click"
    /// instead of real camera & raycast logic.
    /// </summary>
    public bool ForceSimulateMapClick { get; set; } = false;

    /// <summary>
    /// Allows tests to specify a fake cell returned when simulating a click.
    /// </summary>
    public Vector3I FakeClickedCell { get; set; } = new(0, 0, 0);

    protected override void Initialize()
    {
        InitializeCalled = true;
        ReadyCalled = true;

        // Prevent base class from trying to access Godot scene nodes.
        GD.Print("TestConcreteBattleInputSystem Initialize()");
    }

    public override void _Ready()
    {
        ReadyCalled = true;
        base._Ready();
    }

    public override void _Input(InputEvent @event)
    {
        // Mocked input, avoid calling Godot logic
        if (@event.IsActionPressed("battle_pass_turn"))
        {
            PassTurnSignalEmitted = true;
            EmitSignalOnPassTurnPressed();
            return;
        }

        if (@event.IsActionPressed("battle_move_unit_and_select_target"))
        {
            if (ForceSimulateMapClick)
            {
                MoveOrSelectSignalEmitted = true;
                LastMoveOrSelectCell = FakeClickedCell;
                EmitSignalOnMoveUnitOrSelectTargetPressed(FakeClickedCell);
            }

            return;
        }

        if (@event.IsActionPressed("battle_select_move"))
        {
            SelectMoveSignalEmitted = true;
            EmitSignalOnSelectMovePressed();
            return;
        }

        // Skill inputs 1–5
        for (int i = 0; i < 5; i++)
            if (@event.IsActionPressed($"battle_select_skill{i + 1}"))
            {
                SkillSelectedSignals[i] = true;
                EmitSignalOnSelectedSkillPressed(i);
                return;
            }
    }

    // Convenience methods for tests to simulate input
    public void SimulatePress(string action)
    {
        InputEventAction evt = new()
        {
            Action = action,
            Pressed = true
        };
        _Input(evt);
    }
}
