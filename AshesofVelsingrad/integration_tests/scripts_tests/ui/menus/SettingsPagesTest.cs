using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using GdUnit4;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Audio;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.UI;

[TestSuite]
[RequireGodotRuntime]
public class SettingsPagesTest
{
    private Control? _root;
    private SettingsPages? _pages;

    [BeforeTest]
    public void Setup()
    {
        ResetSingletons();

        // Reconstruct full expected UI layout hierarchy for SettingsPages
        _root = new Control { Name = "MockParent" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);

        _pages = new SettingsPages();
        _root.AddChild(_pages);

        // --- PageCommand Hierarchy ---
        var pageCommand = new Control { Name = "PageCommand" };
        _pages.AddChild(pageCommand);
        
        foreach (string action in _pages.actions.Keys)
        {
            var label = new Label { Name = action };
            pageCommand.AddChild(label);
            label.AddChild(new Button { Name = "Button" });
            label.AddChild(new Button { Name = "Button2" });
        }
        pageCommand.AddChild(new Button { Name = "ButtonReset" });

        // --- PageSubtitle Hierarchy ---
        var pageSubtitle = new Control { Name = "PageSubtitle" };
        _pages.AddChild(pageSubtitle);
        pageSubtitle.AddChild(new Label { Name = "ExampleLabel" });
        
        var fontControl = new Control { Name = "Font" };
        pageSubtitle.AddChild(fontControl);
        fontControl.AddChild(new OptionButton { Name = "OptionButton" });

        var subToggleControl = new Control { Name = "Subtitles" };
        pageSubtitle.AddChild(subToggleControl);
        subToggleControl.AddChild(new CheckBox { Name = "CheckBox" });

        var sizeControl = new Control { Name = "Size" };
        pageSubtitle.AddChild(sizeControl);
        sizeControl.AddChild(new HSlider { Name = "HSlider" });

        var textColorControl = new Control { Name = "TextColor" };
        pageSubtitle.AddChild(textColorControl);
        textColorControl.AddChild(new OptionButton { Name = "OptionButton" });

        var bgColorControl = new Control { Name = "BgColor" };
        pageSubtitle.AddChild(bgColorControl);
        bgColorControl.AddChild(new OptionButton { Name = "OptionButton" });

        var opacityControl = new Control { Name = "Opacity" };
        pageSubtitle.AddChild(opacityControl);
        opacityControl.AddChild(new HSlider { Name = "HSlider" });

        var langControl = new Control { Name = "Langage" };
        pageSubtitle.AddChild(langControl);
        langControl.AddChild(new OptionButton { Name = "OptionButton" });

        // --- PageVideo Hierarchy ---
        var pageVideo = new Control { Name = "PageVideo" };
        _pages.AddChild(pageVideo);
        
        string[][] videoControls = {
            new[] { "Contrast", "HSlider" },
            new[] { "Brightness", "HSlider" },
            new[] { "Animations", "CheckBox" },
            new[] { "Resolution", "OptionButton" },
            new[] { "WindowMode", "OptionButton" },
            new[] { "Texture", "OptionButton" }
        };
        BuildSimpleTree(pageVideo, videoControls);

        // --- PageVisual Hierarchy ---
        var pageVisual = new Control { Name = "PageVisual" };
        _pages.AddChild(pageVisual);
        
        var interfaceSizeControl = new Control { Name = "InterfaceSize" };
        pageVisual.AddChild(interfaceSizeControl);
        interfaceSizeControl.AddChild(new HSlider { Name = "HSlider" });
        interfaceSizeControl.AddChild(new Label { Name = "Preview" });

        string[][] visualControls = {
            new[] { "Blurry", "CheckBox" },
            new[] { "CameraShake", "CheckBox" },
            new[] { "VisualIndicators", "CheckBox" },
            new[] { "ColorBlindness", "OptionButton" }
        };
        BuildSimpleTree(pageVisual, visualControls);

        // --- PageAudio Hierarchy ---
        var pageAudio = new Control { Name = "PageAudio" };
        _pages.AddChild(pageAudio);
        
        string[][] audioControls = {
            new[] { "Master", "HSlider" },
            new[] { "Music", "HSlider" },
            new[] { "Voices", "HSlider" },
            new[] { "SFX", "HSlider" }
        };
        BuildSimpleTree(pageAudio, audioControls);

        _pages._Ready();
    }

    [AfterTest]
    public void TearDown()
    {
        if (GodotObject.IsInstanceValid(_root))
            _root!.QueueFree();

        ResetSingletons();
        InputMap.LoadFromProjectSettings(); // Restore project configurations
    }

    private void ResetSingletons()
    {
        SetSingletonInstance<SettingsManager>(null);
        SetSingletonInstance<AudioManager>(null);
    }

    private void SetSingletonInstance<T>(T? instance) where T : class
    {
        PropertyInfo? instanceProperty = typeof(T).GetProperty(
            "Instance",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic
        );
        instanceProperty?.SetValue(null, instance);
    }

    private void BuildSimpleTree(Control parent, string[][] configurations)
    {
        foreach (var set in configurations)
        {
            var wrapper = new Control { Name = set[0] };
            parent.AddChild(wrapper);

            Node childNode = set[1] switch
            {
                "HSlider" => new HSlider(),
                "CheckBox" => new CheckBox(),
                "OptionButton" => new OptionButton(),
                _ => new Control()
            };

            childNode.Name = set[1];
            wrapper.AddChild(childNode);
        }
    }

    #region Value Modification Tracking

    [TestCase]
    public void SubtitleToggled_UpdatesInternalValueAndLabel()
    {
        _pages!.OnSubtitlesToggled(true);
        AssertThat(_pages.subtitles_enabled).IsTrue();

        var label = _pages.GetNode<Label>("PageSubtitle/ExampleLabel");
        AssertThat(label.Visible).IsTrue();
    }

    [TestCase]
    public void AudioSliders_ChangeServerVolume()
    {
        // Ensure standard bus exists or use fallback logic verification
        int masterIdx = AudioServer.GetBusIndex("Master");
        if (masterIdx != -1)
        {
            _pages!.OnMasterVolumeChanged(0.0f); // Mute
            AssertThat(AudioServer.GetBusVolumeDb(masterIdx)).IsLess(-60.0f);

            _pages.OnMasterVolumeChanged(100.0f); // Max
            AssertThat(AudioServer.GetBusVolumeDb(masterIdx)).IsGreaterEqual(-0.1f);
        }
    }

    [TestCase]
    public void Video_ResolutionSelection_AltersDisplayServer()
    {
        _pages!.OnResolutionItemSelected(1); // 1280x720
        AssertThat(_pages.resolution).IsEqual(new Vector2I(1280, 720));
        AssertThat(DisplayServer.WindowGetSize()).IsEqual(new Vector2I(1280, 720));
    }

    #endregion

    #region Input Map Rebinding Engine

    [TestCase]
    public void Rebind_InterceptsAndAssigns_NewKeyboardKey()
    {
        if (!InputMap.HasAction("move_up")) InputMap.AddAction("move_up");

        var label = _pages!.GetNode<Label>("PageCommand/move_up");
        var primaryBtn = label.GetNode<Button>("Button");

        // Force hook into rebinding state via reflection or internal method trigger
        MethodInfo? rebindMethod = typeof(SettingsPages).GetMethod("OnRebindPressed", BindingFlags.NonPublic | BindingFlags.Instance);
        rebindMethod!.Invoke(_pages, new object[] { "move_up", primaryBtn });

        AssertThat(primaryBtn.Text).IsEqual("Press key...");

        // Fire mock keyboard intercept event
        var mockEvent = new InputEventKey { PhysicalKeycode = Key.K, Pressed = true };
        _pages._Input(mockEvent);

        AssertThat(primaryBtn.Text).IsEqual("K");
        AssertThat(InputMap.ActionHasEvent("move_up", mockEvent)).IsTrue();
    }

    #endregion

    #region UI Sync Application Lifecycle

    [TestCase]
    public void ApplySettingsToUI_PushesStateToControlNodes()
    {
        _pages!.subtitles_enabled = true;
        _pages.subtitle_size = 45f;
        _pages.master_volume = 75f;

        _pages.ApplySettingsToUI();

        AssertThat(_pages.GetNode<CheckBox>("PageSubtitle/Subtitles/CheckBox").ButtonPressed).IsTrue();
        AssertThat(_pages.GetNode<HSlider>("PageSubtitle/Size/HSlider").Value).IsEqual(45.0);
        AssertThat(_pages.GetNode<HSlider>("PageAudio/Master/HSlider").Value).IsEqual(75.0);
    }

    [TestCase]
    public void ShowAndHideAll_TogglesParentControlState()
    {
        _pages!.HideAll();
        AssertThat(_root!.Visible).IsFalse();

        _pages.ShowAll();
        AssertThat(_root.Visible).IsTrue();
        AssertThat(_root.MouseFilter).IsEqual(Control.MouseFilterEnum.Pass);
    }

    #endregion
}
