using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
/// Base class for all manager singletons in the game.
/// Implements the Manager Pattern from your architecture documentation.
/// </summary>
/// <remarks>
/// This class serves as a base for all manager classes, ensuring that only one instance exists.
/// It provides a common initialization and cleanup mechanism for all managers.
/// Derived classes should implement the Initialize method to set up their specific functionality.
/// </remarks>
public abstract partial class BaseManager : Node
{
	public static BaseManager? Instance { get; protected set; }

	/// <summary>
	/// Called when the node is added to the scene tree.
	/// Initializes the manager instance and checks for duplicates.
	/// </summary>
	/// <remarks>
	/// This method is called automatically by Godot when the node is ready.
	/// It ensures that only one instance of the manager exists in the scene tree.
	/// If a duplicate instance is found, it removes the duplicate.
	/// </remarks>
	public override void _Ready()
	{
		// Pour AutoLoad, l'initialisation se fait immédiatement
		if (IsInsideTree() && GetParent() == GetTree().Root)
		{
			Initialize();
		}
		// Pour les instances manuelles, vérifier les doublons
		else if (Instance == null)
		{
			Initialize();
		}
		else
		{
			GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
			QueueFree();
		}
	}

	/// <summary>
	/// Initializes the manager instance.
	/// This method should be overridden in derived classes to set up specific functionality.
	/// </summary>
	/// <remarks>
	/// This method is called by the _Ready method to initialize the manager.
	/// It should contain the logic necessary to set up the manager's state and functionality.
	/// Derived classes must implement this method to provide their specific initialization logic.
	/// </remarks>
	protected abstract void Initialize();

	/// <summary>
	/// Called when the node is removed from the scene tree.
	/// Cleans up the manager instance and sets it to null.
	/// </summary>
	/// <remarks>
	/// This method is called automatically by Godot when the node is removed from the scene tree.
	/// It ensures that the manager instance is properly cleaned up and set to null.
	/// This is important for preventing memory leaks and ensuring that the manager can be re-initialized later if needed.
	/// </remarks>
	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Cleanup();
			Instance = null;
		}
	}

	/// <summary>
	/// Cleans up the manager instance.
	/// This method can be overridden in derived classes to implement specific cleanup logic.
	/// </summary>
	/// <remarks>
	/// This method is called when the manager is removed from the scene tree.
	/// It provides a place for derived classes to implement any necessary cleanup logic,
	/// such as disconnecting signals or releasing resources.
	/// By default, it does nothing, but derived classes can override it to perform specific cleanup tasks.
	/// </remarks>
	protected virtual void Cleanup()
	{
		// Override in derived classes for cleanup logic
	}
}
