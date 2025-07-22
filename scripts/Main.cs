using Godot;

namespace AshesofVelsingrad;

/// <summary>
/// Classe principale du projet Godot. Gère la scène principale et les interactions globales.
/// </summary>
public partial class Main : Node
{
    /// <summary>
    /// Incrémente la valeur passée en paramètre de 1.
    /// </summary>
    /// <param name="value">La valeur à incrémenter.</param>
    /// <returns>La valeur incrémentée de 1.</returns>
    public int TempCounter(int value) => value + 1;
}
