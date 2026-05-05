using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///     Vertical panel showing a unit portrait label + its
///     <see cref="InventoryConstants.BattleCapacity" /> drop-target slots.
///     One of these is created per player unit in <see cref="ExplorationInventoryUI" />.
/// </summary>
public sealed partial class UnitTransferPanel : Control
{
    private readonly List<UnitSlotUI> _slots = new();
    private InventorySystem? _unitInventory;
    private bool _built;

    public override void _Ready() => EnsureBuilt();

    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        CustomMinimumSize = new Vector2(InventoryConstants.SlotSize + 24f, 0);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>Bind to a unit and rebuild slot visuals.</summary>
    public void Bind(IUnitSystem unit)
    {
        EnsureBuilt();

        // Clear old children
        foreach (Node child in GetChildren()) child.QueueFree();
        _slots.Clear();

        _unitInventory = (InventorySystem)unit.Inventory;
        _unitInventory.SlotChanged += OnUnitSlotChanged;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(vbox));

        // Unit name header
        var nameLabel = new Label
        {
            Text = unit.UnitName,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        HudStyle.StyleLabel(nameLabel);
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(nameLabel);

        var sep = new HSeparator();
        sep.AddThemeColorOverride("color", HudStyle.PanelBorder);
        vbox.AddChild(sep);

        // Drop-target slots
        for (int i = 0; i < InventoryConstants.BattleCapacity; i++)
        {
            var slot = new UnitSlotUI();
            slot.EnsureBuilt();
            slot.Setup(i, _unitInventory);
            slot.Refresh(_unitInventory.GetSlot(i));
            vbox.AddChild(slot);
            _slots.Add(slot);
        }
    }

    private void OnUnitSlotChanged(int index)
    {
        if (_unitInventory == null || index < 0 || index >= _slots.Count) return;
        _slots[index].Refresh(_unitInventory.GetSlot(index));
    }
}