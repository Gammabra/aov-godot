using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;

namespace AshesofVelsingrad.scripts.systems;

/// <summary>
/// Handles interactions with pickable items.
/// </summary>
public static class PickableSystem
{
    /// <summary>
    /// Adds the specified item to the player's inventory and removes it from the world if successful.
    /// </summary>
    /// <param name="itemSystem">The item to pick up.</param>
    public static void AddToInventory(ItemSystem itemSystem)
    {
        if (PlayerInventoryManager.Instance is { } inv)
        {
            int amount = inv.GlobalInventory.AddItem(itemSystem.Id, 1);

            if (amount == 0)
                itemSystem.QueueFree();
        }
    }
}
