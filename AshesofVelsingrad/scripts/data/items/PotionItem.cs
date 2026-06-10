using Godot;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.items;

public sealed partial class PotionItem : ItemSystem
{
    [Export]
    private NodePath? _interactTextPath;

    private Label3D? _interactText;

    public override void _Ready()
    {
        Id = 1;
        Name = "Potion";
        Description = "Restores a small amount of HP.";
        Category = ItemCategory.Consumable;
        IsStackable = true;
        MaxStack = 10;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
        _interactText = GetNode<Label3D>(_interactTextPath);
        ItemCatalog.Register(this);
    }

    public override void Use(IUnitSystem user, IUnitSystem? target, IMapSystem? map)
    {
        if (target == null)
            return;
    }

    public override void ShowPrompt()
    {
        if (_interactText is not null)
            _interactText.Visible = true;
    }

    public override void HidePrompt()
    {
        if (_interactText is not null)
            _interactText.Visible = false;
    }
}
