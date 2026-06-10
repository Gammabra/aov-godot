using AshesOfVelsingrad.items;
using Godot;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Autoload that registers every concrete <see cref="ItemSystem" /> into
///     <see cref="ItemCatalog" /> at game start.
///     Add new items here as they are created.
/// </summary>
public sealed partial class ItemCatalogSeeder : Node
{
    public override void _Ready()
    {
        ItemCatalog.Clear(); // safety: prevent duplicate registration on scene reload

        ItemCatalog.Register(new PotionItem());
        ItemCatalog.Register(new IronSwordItem());
        ItemCatalog.Register(new LeatherArmorItem());
        ItemCatalog.Register(new EtherItem());

        GD.Print($"[ItemCatalogSeeder] Registered {4} items.");
    }
}
