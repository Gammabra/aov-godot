namespace AshesOfVelsingrad.Systems;

/// <summary>
/// Defines a contract for objects that can be interacted with by the player.
/// Implementing classes handle interaction logic, availability checks,
/// and optional UI feedback such as prompts.
/// </summary>
/// <remarks>IMPORTANT: The interactable object must be in the Collision Layer 2 because the interaction area of the player sees at Layer 2</remarks>
public interface IInteractable
{
    /// <summary>
    /// Executes the interaction logic.
    /// </summary>
    /// <param name="interactor">
    /// The entity that initiates the interaction.
    /// </param>
    public void Interact(IInteractor interactor);

    /// <summary>
    /// Determines whether the object can currently be interacted with.
    /// </summary>
    /// <returns>
    /// <c>true</c> if interaction is allowed; otherwise, <c>false</c>.
    /// </returns>
    public bool CanInteract();

    /// <summary>
    /// Displays a visual or UI prompt indicating that the object
    /// can be interacted with.
    /// </summary>
    public void ShowPrompt();

    /// <summary>
    /// Hides the interaction prompt.
    /// </summary>
    public void HidePrompt();
}
