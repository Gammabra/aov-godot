using Godot;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.items;

public sealed partial class PotionItem : ItemSystem
{
    /// <summary>HP restored per use.</summary>
    private const float _healAmount = 50f;

    [Export]
    private NodePath? _interactTextPath;

    private Label3D? _interactText;

    public override void _Ready()
    {
        Id = 1;
        Name = "Potion";
        Description = $"Restores {_healAmount} HP.";
        Category = ItemCategory.Consumable;
        IsStackable = true;
        MaxStack = 10;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
        _interactText = GetNode<Label3D>(_interactTextPath);
        ItemCatalog.Register(this);
    }

    public override void Use(IUnitSystem user, IUnitSystem? target, IMapSystem? map)
    {
        // Heal the target if provided, otherwise heal the user (self-use)
        var actualTarget = target ?? user;
        actualTarget.OnEffectHeal(_healAmount);
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
