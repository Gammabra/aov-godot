// Create: integration_tests/scripts_tests/player/MockExplorationInventoryUI.cs
using AshesOfVelsingrad.UI.Inventory;

namespace AshesOfVelsingrad.IntegrationTests.Player;

public partial class MockExplorationInventoryUI : ExplorationInventoryUI
{
    public bool EnsureBuiltCalled { get; private set; }
    public bool RefreshUnitPanelsCalled { get; private set; }

    public override void RefreshUnitPanels()
    {
        RefreshUnitPanelsCalled = true;
    }
}
