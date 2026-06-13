using System.Collections.Generic;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.UI;

[TestSuite]
[RequireGodotRuntime]
public class SettingsPageManagerTest
{
    private Node? _root;
    private Control? _mainContent;
    private SettingsPageManager? _manager;
    private readonly List<Button> _titleButtons = new();
    private readonly List<Control> _pageNodes = new();

    [BeforeTest]
    public void Setup()
    {
        // Reconstruct required scene structure:
        // MockParent (Control)
        //  ├── MainContent (Control)
        //  │    ├── PageSubtitle, PageVideo, PageVisual, PageCommand, PageAudio
        //  └── SettingsPageManager (Node)
        //       └── VBoxContainer/HBoxContainer
        //            └── [Title Buttons]

        _root = new Control { Name = "MockParent" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);

        _mainContent = new Control { Name = "MainContent" };
        _root.AddChild(_mainContent);

        string[] pageNames = { "PageSubtitle", "PageVideo", "PageVisual", "PageCommand", "PageAudio" };
        _pageNodes.Clear();
        foreach (string name in pageNames)
        {
            var page = new Control { Name = name };
            _mainContent.AddChild(page);
            _pageNodes.Add(page);
        }

        _manager = new SettingsPageManager();
        _root.AddChild(_manager);

        var vbox = new VBoxContainer { Name = "VBoxContainer" };
        var hbox = new HBoxContainer { Name = "HBoxContainer" };
        _manager.AddChild(vbox);
        vbox.AddChild(hbox);

        _titleButtons.Clear();
        for (int i = 0; i < 5; i++)
        {
            var btn = new Button { Name = $"Button{i}" };
            hbox.AddChild(btn);
            _titleButtons.Add(btn);
        }

        // Manually trigger _Ready because nodes were populated dynamically
        _manager._Ready();
    }

    [AfterTest]
    public void TearDown()
    {
        if (GodotObject.IsInstanceValid(_root))
        {
            _root!.QueueFree();
        }
    }

    [TestCase]
    public void Ready_InitializesToFirstPage_AndHighlightsFirstButton()
    {
        AssertThat(_manager!.currentPage).IsEqual(1);
        AssertThat(_titleButtons[0].Modulate).IsEqual(Colors.Red);
        AssertThat(_pageNodes[0].Visible).IsTrue();

        for (int i = 1; i < _pageNodes.Count; i++)
        {
            AssertThat(_titleButtons[i].Modulate).IsEqual(Colors.White);
            AssertThat(_pageNodes[i].Visible).IsFalse();
        }
    }

    [TestCase]
    public void ChangePage_UpdatesVisibilityAndModulation()
    {
        _manager!.ChangePage(3); // Go to Visual Page

        AssertThat(_manager.currentPage).IsEqual(3);
        AssertThat(_titleButtons[2].Modulate).IsEqual(Colors.Red);
        AssertThat(_pageNodes[2].Visible).IsTrue();

        // Old page should be hidden and unhighlighted
        AssertThat(_titleButtons[0].Modulate).IsEqual(Colors.White);
        AssertThat(_pageNodes[0].Visible).IsFalse();
    }

    [TestCase]
    public void HideAllPages_ClearsHighlightsAndHidesVBox()
    {
        _manager!.HideAllPages();

        foreach (var page in _pageNodes)
            AssertThat(page.Visible).IsFalse();

        foreach (var title in _titleButtons)
            AssertThat(title.Modulate).IsEqual(Colors.White);

        var vbox = _manager.GetNode<Control>("VBoxContainer");
        AssertThat(vbox.Visible).IsFalse();
        AssertThat(vbox.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
    }

    [TestCase]
    public void ShowCurrentPage_RestoresVBoxAndUpdatesState()
    {
        _manager!.HideAllPages();
        _manager.ShowCurrentPage();

        var vbox = _manager.GetNode<Control>("VBoxContainer");
        AssertThat(vbox.Visible).IsTrue();
        AssertThat(_pageNodes[0].Visible).IsTrue();
    }

    [TestCase]
    public void Shortcuts_RouteToCorrectPages()
    {
        _manager!.OnButtonAudioPressed();
        AssertThat(_manager.currentPage).IsEqual(5);

        _manager.OnButtonVideoPressed();
        AssertThat(_manager.currentPage).IsEqual(2);
    }
}
