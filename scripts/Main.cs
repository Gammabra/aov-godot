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
    /// Increments the value passed as a parameter by 1.
    /// </summary>
    /// <param name="value">The value to increment.</param>
    /// <returns>The value incremented by 1.</returns>
    public int TempCounter(int value) => value + 1;
}
