using Godot;

namespace AshesOfVelsingrad.player;

/// <summary>
/// Camera controller for exploration mode.
/// Handles mouse input to rotate the camera using a SpringArm3D.
/// </summary>
public partial class ExplorationCamera : SpringArm3D
{
    private float _mouseSensitivity = 0.1f;

    public override void _Ready()
    {
        Input.SetMouseMode(Input.MouseModeEnum.Captured);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            Vector3 rotation = RotationDegrees;
            rotation.X -= mouseMotion.Relative.Y * _mouseSensitivity;
            rotation.X = Mathf.Clamp(rotation.X, -40, 40);

            rotation.Y -= mouseMotion.Relative.X * _mouseSensitivity;
            rotation.Y = Mathf.Wrap(rotation.Y, 0, 360);
            RotationDegrees = rotation;
        }
    }
}
