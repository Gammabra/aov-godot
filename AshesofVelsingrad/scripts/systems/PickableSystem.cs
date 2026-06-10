using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.systems.skills.behaviours;
using Godot;

namespace AshesofVelsingrad.scripts.systems;

public static class PickableSystem
{
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
