using Godot;
using AshesofVelsingrad.scripts.systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

// TODO: Remove it when the Tactical combat branch is merged because it is defined in the skill system
public enum TargetType
{
    None,
    Allies,
    Enemies
}

public enum ItemCategory
{
    Misc,
    Consumable,
    Weapon,
    Armor
}

public abstract partial class ItemSystem : Node3D, IItemSystem, IInteractable
{
    public int Id { get; protected set; }
    public string? Name { get; protected set; }
    public string? Description { get; protected set; }

    public ItemCategory Category { get; protected set; } = ItemCategory.Misc;

    public AovDataStructures.TargetTypes TargetType { get; protected set; }
    public string Icon { get; protected set; } = string.Empty;

    public bool IsStackable { get; protected set; } = false;
    public int MaxStack { get; protected set; } = 0;

    public virtual bool ConsumesTurn => Category == ItemCategory.Consumable;

    [Signal]
    public delegate void InteractedEventHandler();

    public abstract void Use(IUnitSystem user, IUnitSystem? target, IMapSystem? map);

    public virtual bool CanInteract()
    {
        return true;
    }

    public virtual void Interact(IInteractor interactor)
    {
        PickableSystem.AddToInventory(this);
        EmitSignalInteracted();
    }

    public virtual void HidePrompt()
    {
    }

    public virtual void ShowPrompt()
    {
    }
}
