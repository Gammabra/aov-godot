using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Exploration;

public sealed partial class ExplorationSlotUI : PanelContainer
{
    private Label? _label;
    private int _index;

    public void Setup(int index)
    {
        _index = index;
        EnsureBuilt();
    }

    public override void _Ready() => EnsureBuilt();

    private void EnsureBuilt()
    {
        if (_label != null) return;
        _label = new Label { Text = string.Empty, Visible = false };
        AddChild(_label);
    }

    public void Refresh(IInventorySlot slot)
    {
        EnsureBuilt();
        if (_label == null) return;

        if (slot.IsEmpty)
        {
            _label.Visible = false;
            return;
        }

        var item = ItemCatalog.Get(slot.ItemId);
        _label.Text = $"{item.Name} x{slot.Quantity}";
        _label.Visible = true;
    }
}
