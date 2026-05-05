using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.UI.Exploration;

/// <summary>
///     Exploration-mode inventory panel. Reads directly from
///     <see cref="PlayerInventoryManager.Inventory" />.
///     Toggled by the "open_inventory" input action.
/// </summary>
public sealed partial class ExplorationInventoryUI : CanvasLayer
{
    public int localLayer = 10;

    private bool _built;
    private GridContainer? _slotContainer;
    private readonly List<ExplorationSlotUI> _slotUis = new();

    public override void _Ready()
    {
        EnsureBuilt();

        if (PlayerInventoryManager.Instance is { } mgr)
        {
            mgr.Inventory.SlotChanged += OnSlotChanged;
            BuildSlots(mgr.Inventory);
            RefreshAll(mgr.Inventory);
        }
        else
        {
            GD.PrintErr("ExplorationInventoryUI: PlayerInventoryManager not available.");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("open_inventory"))
            Toggle();
    }

    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        Layer = localLayer;
        Visible = false;

        var panel = new PanelContainer { Name = "Panel" };
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(480, 400);
        AddChild(panel);

        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        var title = new Label { Text = "Inventory" };
        title.AddThemeFontSizeOverride("font_size", 20);
        vbox.AddChild(title);

        var closeBtn = new Button { Text = "Close" };
        closeBtn.Pressed += Toggle;
        vbox.AddChild(closeBtn);

        _slotContainer = new GridContainer { Columns = 5 };
        vbox.AddChild(_slotContainer);
    }

    public void Toggle() => Visible = !Visible;

    private void BuildSlots(InventorySystem inventory)
    {
        if (_slotContainer == null) return;

        foreach (Node child in _slotContainer.GetChildren())
            child.QueueFree();
        _slotUis.Clear();

        for (int i = 0; i < inventory.Slots.Length; i++)
        {
            var slot = new ExplorationSlotUI();
            slot.Setup(i);
            _slotContainer.AddChild(slot);
            _slotUis.Add(slot);
        }
    }

    private void RefreshAll(InventorySystem inventory)
    {
        for (int i = 0; i < _slotUis.Count; i++)
            _slotUis[i].Refresh(inventory.Slots[i]);
    }

    private void OnSlotChanged(int index)
    {
        if (PlayerInventoryManager.Instance is not { } mgr) return;
        if (index < 0 || index >= _slotUis.Count) return;
        _slotUis[index].Refresh(mgr.Inventory.Slots[index]);
    }
}
