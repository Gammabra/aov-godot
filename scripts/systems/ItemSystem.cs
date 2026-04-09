namespace AshesOfVelsingrad.systems;

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

public abstract class ItemSystem
{
	public int Id { get; protected set; }
	public string? Name { get; protected set; }
	public string? Description { get; protected set; }

	public ItemCategory Category { get; protected set; } = ItemCategory.Misc;

	public TargetType TargetType { get; protected set; } = TargetType.None;
	public string Icon { get; protected set; } = string.Empty;

	public bool IsStackable { get; protected set; } = false;
	public int MaxStack { get; protected set; } = 0;
}
