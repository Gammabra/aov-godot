using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///     Horizontal strip: unit name label + 5 drop-target slots.
///     Binds to a persistent <see cref="InventorySystem" /> party loadout,
///     so no live unit node is required during exploration.
/// </summary>
public sealed partial class UnitTransferPanel : Control
{
    private Label? _nameLabel;
    private HBoxContainer? _slotRow;
    private readonly List<UnitSlotUI> _slots = new();
    private InventorySystem? _loadout;
    private bool _built;

    public override void _Ready() => EnsureBuilt();

    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>Bind to a persistent party loadout slot.</summary>
    public void Bind(string unitName, InventorySystem loadout)
    {
        EnsureBuilt();

        // Detach old subscription
        if (_loadout != null) _loadout.SlotChanged -= OnLoadoutSlotChanged;

        // Clear old children
        foreach (Node child in GetChildren()) child.QueueFree();
        _slots.Clear();
        _nameLabel = null;
        _slotRow = null;

        _loadout = loadout;
        _loadout.SlotChanged += OnLoadoutSlotChanged;

        // ── Layout: one row per unit ────────────────────────────────────
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);
        hbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(hbox));

        // Unit name label (fixed width so all rows align)
        _nameLabel = new Label
        {
            Text = unitName,
            CustomMinimumSize = new Vector2(90f, 0f),
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        HudStyle.StyleLabel(_nameLabel);
        _nameLabel.AddThemeFontSizeOverride("font_size", 13);
        hbox.AddChild(_nameLabel);

        var divider = new VSeparator();
        divider.AddThemeColorOverride("color", HudStyle.PanelBorder);
        hbox.AddChild(divider);

        // 5 drop-target slots in a row
        _slotRow = new HBoxContainer();
        _slotRow.AddThemeConstantOverride("separation", 4);
        hbox.AddChild(_slotRow);

        for (int i = 0; i < InventoryConstants.BattleCapacity; i++)
        {
            var slot = new UnitSlotUI();
            slot.EnsureBuilt();
            slot.Setup(i, loadout);
            slot.Refresh(loadout.GetSlot(i));
            _slotRow.AddChild(slot);
            _slots.Add(slot);
        }
    }

    public override void _ExitTree()
    {
        if (_loadout != null)
        {
            _loadout.SlotChanged -= OnLoadoutSlotChanged;
        }
        base._ExitTree();
    }

    private void OnLoadoutSlotChanged(int index)
    {
        // 2. UPDATE THIS LINE: Add IsInstanceValid(this) for lifecycle safety
        if (!GodotObject.IsInstanceValid(this) || _loadout == null || index < 0 || index >= _slots.Count) return;
        
        _slots[index].Refresh(_loadout.GetSlot(index));
    }
}
