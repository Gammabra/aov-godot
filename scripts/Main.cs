using Godot;

namespace AshesofVelsingrad;

/// <summary>
/// Main class of the Godot project. Manages the main scene and global interactions.
/// </summary>
public partial class Main : Node
{
    /// <summary>
    /// Method called when the node is ready.
    /// Initializes the main scene and adds a label to indicate that the script is loaded.
    /// </summary>
    public override void _Ready()
    {
        var label = new Label();
        label.Text = "Main.cs loaded!";
        AddChild(label);
    }

    /// <summary>
    /// Method called every frame to process input.
    /// Checks if the space key is pressed and prints a message to the console.
    /// </summary>
    /// <param name="delta">The time elapsed since the last frame.</param>
    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("space_input"))
        {
            GD.Print("Space key pressed!");
        }
    }

    /// <summary>
    /// Increments the value passed as a parameter by 1.
    /// </summary>
    /// <param name="value">The value to increment.</param>
    /// <returns>The value incremented by 1.</returns>
    public int TempCounter(int value) => value + 1;
}
