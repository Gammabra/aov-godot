using Godot;

namespace AshesOfVelsingrad.tutorial;

public sealed partial class MovableDoor : Node3D
{
    [Export]
    private NodePath _doorPath = null!;

    [Export]
    private Vector3 _openTarget = new(0, -111, 0);

    [Export]
    private Vector3 _closeTarget = new(0, 0, 0);

    [Export]
    private float _speed = 1f;

    private Node3D _door = null!;
    private bool _isOpened;
    private bool _mustMove;

    [Signal]
    public delegate void TogglingDoorFinishedEventHandler();

    public override void _Ready()
    {
        _door = GetNode<Node3D>(_doorPath);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_mustMove)
            return;
        if (_isOpened)
        {
            if (_door.RotationDegrees.DistanceTo(_closeTarget) > 0.25f)
            {
                _door.RotationDegrees = _door.RotationDegrees.Lerp(_closeTarget, _speed * (float)delta);
            }
            else
            {
                _door.RotationDegrees = _closeTarget;
                _isOpened = false;
                _mustMove = false;
                EmitSignalTogglingDoorFinished();
            }
        }
        else
        {
            if (_door.RotationDegrees.DistanceTo(_openTarget) > 0.25f)
            {
                _door.RotationDegrees = _door.RotationDegrees.Lerp(_openTarget, _speed * (float)delta);
            }
            else
            {
                GD.Print("Door completely opened");
                _door.RotationDegrees = _openTarget;
                _isOpened = true;
                _mustMove = false;
                EmitSignalTogglingDoorFinished();
            }
        }
    }

    public void ToggleDoor()
    {
        if (!_mustMove)
        {
            _mustMove = !_mustMove;
        }
    }
}
