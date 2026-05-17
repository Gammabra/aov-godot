using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///     Single slot in the battle inventory. Styled with <see cref="HudStyle" />.
///     Shows item icon area, name label, and a "Use" button.
/// </summary>
public sealed partial class BattleInventorySlotUI : PanelContainer
{
    private Label? _nameLabel;
    private Label? _qtyLabel;
    private Button? _useButton;
    private int _slotIndex;
    private BattleInventoryUI? _owner;
    private bool _built;

    public override void _Ready() => EnsureBuilt();

    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        CustomMinimumSize = new Vector2(66f, 48f);
        AddThemeStyleboxOverride("panel", HudStyle.MakePanelStyle());
        MouseFilter = MouseFilterEnum.Stop;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(vbox);

        // Item name
        _nameLabel = new Label
        {
            Text = string.Empty,
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 28),
        };
        HudStyle.StyleLabel(_nameLabel);
        _nameLabel.AddThemeColorOverride("font_color", HudStyle.DimText);
        vbox.AddChild(_nameLabel);

        // Quantity
        _qtyLabel = new Label { Text = string.Empty, Visible = false };
        HudStyle.StyleLabel(_qtyLabel);
        vbox.AddChild(_qtyLabel);

        // Use button
        _useButton = new Button
        {
            Text = "Use",
            Visible = false,
            CustomMinimumSize = new Vector2(0, 26),
        };
        HudStyle.StyleButton(_useButton);
        _useButton.Pressed += OnUsePressed;
        vbox.AddChild(_useButton);
    }

    public void Setup(int slotIndex, BattleInventoryUI owner)
    {
        EnsureBuilt();
        _slotIndex = slotIndex;
        _owner = owner;
    }

    public void Refresh(IInventorySlot slot)
    {
        EnsureBuilt();

        bool empty = slot.IsEmpty;

        if (_nameLabel != null)
        {
            _nameLabel.Visible = !empty;
            if (!empty)
            {
                if (!ItemCatalog.TryGet(slot.ItemId, out var item))
                {
                    GD.PrintErr($"Unknown item id {slot.ItemId}");
                    return;
                }
                _nameLabel.Text = item.Name ?? string.Empty;
                _nameLabel.AddThemeColorOverride("font_color", HudStyle.TextColor);
            }
        }

        if (_qtyLabel != null)
        {
            _qtyLabel.Visible = !empty && slot.Quantity > 1;
            if (!empty) _qtyLabel.Text = $"x{slot.Quantity}";
        }

        if (_useButton != null)
            _useButton.Visible = !empty;
    }

    private void OnUsePressed() => _owner?.NotifyUseItemPressed(_slotIndex);
}
