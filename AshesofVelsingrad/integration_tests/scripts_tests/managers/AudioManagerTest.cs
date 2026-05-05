using System.Collections.Generic;
using AshesOfVelsingrad.Audio;
using AshesOfVelsingrad.Helpers.Managers;
using AshesOfVelsingrad.Managers;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.Managers;

/// <summary>
///     Engine-level coverage for <see cref="AudioManager" />. These tests touch
///     <c>AudioServer</c> directly so they have to run inside the Godot runtime.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class AudioManagerTest
{
    private readonly List<Node> _testNodes = new();
    private Node? _root;
    private TestSettingsManager? _settings;
    private TestAudioManager? _audio;

    [BeforeTest]
    public void SetUp()
    {
        TestAudioManager.ResetSingleton();
        ResetSettingsSingletons();
        TestSettingsManager.ClearTempFiles();

        _root = new Node { Name = "AudioTestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);

        // SettingsManager has to come up first; AudioManager binds to it during Initialize.
        _settings = AddNode(new TestSettingsManager());
        InvokePrivate(_settings, "Initialize");

        _audio = AddNode(new TestAudioManager());
        _audio.TestInitialize();
    }

    [AfterTest]
    public void TearDown()
    {
        foreach (var node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.QueueFree();
            }
        }
        _testNodes.Clear();

        TestAudioManager.ResetSingleton();
        ResetSettingsSingletons();
        TestSettingsManager.ClearTempFiles();
    }

    [TestCase]
    public void Initialize_RegistersSingleton()
    {
        AssertThat(AudioManager.Instance).IsEqual(_audio);
    }

    [TestCase]
    public void Initialize_CreatesAllExpectedBuses()
    {
        foreach (var name in new[] { "Master", "Music", "SFX", "Ambient", "Ui", "Voices" })
        {
            AssertThat(TestAudioManager.BusExists(name)).IsTrue();
        }
    }

    [TestCase]
    public void Initialize_SecondInstance_RemovesDuplicate()
    {
        var duplicate = AddNode(new TestAudioManager());
        duplicate.TestInitialize();

        // The first instance must remain authoritative; the duplicate should be queued.
        AssertThat(AudioManager.Instance).IsEqual(_audio);
        AssertThat(duplicate.IsQueuedForDeletion()).IsTrue();
    }

    [TestCase]
    public void SetBusVolume_UpdatesAudioServerInDb()
    {
        _audio!.SetBusVolume(AudioBus.Sfx, 0.5f);

        var actualDb = TestAudioManager.GetBusVolumeDb("SFX");
        AssertThat(Mathf.IsEqualApprox(actualDb, AudioVolumeMath.LinearToDb(0.5f), 0.05f)).IsTrue();
    }

    [TestCase]
    public void SetBusVolume_ClampsOutOfRangeValues()
    {
        _audio!.SetBusVolume(AudioBus.Music, 5f);
        AssertThat(Mathf.IsEqualApprox(_audio.GetBusVolume(AudioBus.Music), 1f, 0.01f)).IsTrue();

        _audio.SetBusVolume(AudioBus.Music, -1f);
        AssertThat(Mathf.IsEqualApprox(_audio.GetBusVolume(AudioBus.Music), 0f, 0.01f)).IsTrue();
    }

    [TestCase]
    public void SetBusVolume_PersistsToSettings()
    {
        _audio!.SetBusVolume(AudioBus.Voice, 0.42f);

        var stored = _settings!.GetSetting<float>(AudioManager.GetSettingsKey(AudioBus.Voice));
        AssertThat(Mathf.IsEqualApprox(stored, 0.42f, 0.001f)).IsTrue();
    }

    [TestCase]
    public async System.Threading.Tasks.Task SettingsChange_UpdatesBusVolumeInstantly()
    {
        // Simulates the player dragging the music slider in the options menu.
        _settings!.SetSetting(AudioManager.GetSettingsKey(AudioBus.Music), 0.25f);

        var tree = (SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        var liveDb = TestAudioManager.GetBusVolumeDb("Music");
        AssertThat(Mathf.IsEqualApprox(liveDb, AudioVolumeMath.LinearToDb(0.25f), 0.05f)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(_audio!.GetBusVolume(AudioBus.Music), 0.25f, 0.01f)).IsTrue();
    }

    [TestCase]
    public void SetBusMuted_SilencesBusButPreservesPersistedVolume()
    {
        _audio!.SetBusVolume(AudioBus.Ambient, 0.7f);
        _audio.SetBusMuted(AudioBus.Ambient, true);

        AssertThat(_audio.IsBusMuted(AudioBus.Ambient)).IsTrue();
        var index = AudioServer.GetBusIndex("Ambient");
        AssertThat(AudioServer.IsBusMute(index)).IsTrue();

        _audio.SetBusMuted(AudioBus.Ambient, false);

        AssertThat(_audio.IsBusMuted(AudioBus.Ambient)).IsFalse();
        AssertThat(Mathf.IsEqualApprox(_audio.GetBusVolume(AudioBus.Ambient), 0.7f, 0.01f)).IsTrue();
    }

    [TestCase]
    public void GetSettingsKey_FollowsCanonicalFormat()
    {
        AssertThat(AudioManager.GetSettingsKey(AudioBus.Master)).IsEqual("audio.volume.master");
        AssertThat(AudioManager.GetSettingsKey(AudioBus.Music)).IsEqual("audio.volume.music");
        AssertThat(AudioManager.GetSettingsKey(AudioBus.Sfx)).IsEqual("audio.volume.sfx");
        AssertThat(AudioManager.GetSettingsKey(AudioBus.Ambient)).IsEqual("audio.volume.ambient");
        AssertThat(AudioManager.GetSettingsKey(AudioBus.Ui)).IsEqual("audio.volume.ui");
        AssertThat(AudioManager.GetSettingsKey(AudioBus.Voice)).IsEqual("audio.volume.voice");
    }

    [TestCase]
    public void ExitTree_ClearsSingleton()
    {
        _audio!._ExitTree();
        AssertThat(AudioManager.Instance).IsNull();
    }

    // === Registry / catalog integration ===

    [TestCase]
    public void Initialize_PopulatesRegistryFromCatalog()
    {
        AssertThat(_audio!.Registry.Count).IsGreater(0);
        AssertThat(_audio.Registry.Contains(AudioCatalog.MainMenuTheme)).IsTrue();
    }

    [TestCase]
    public void Play_UnknownTrackId_DoesNotThrow()
    {
        // Should log an error and noop; never blow up the caller.
        _audio!.Play("does.not.exist");
    }

    [TestCase]
    public void Play_MainMenuTheme_DoesNotThrow()
    {
        // Whether the player actually starts depends on Godot's .import for the
        // asset being present (the editor generates it on first open). The
        // contract we care about here is that the dispatch path is wired up and
        // a missing .import surfaces as a printed error, not an exception.
        _audio!.Play(AudioCatalog.MainMenuTheme);
    }

    [TestCase]
    public void Registry_ExposesTrackMetadataForUiUse()
    {
        var track = _audio!.Registry.Find(AudioCatalog.MainMenuTheme);

        AssertThat(track).IsNotNull();
        AssertThat(track!.Bus).IsEqual(AudioBus.Music);
        AssertThat(track.ResourcePath).IsEqual("res://assets/audio_assets/musics/TA_A.wav");
    }

    // === Helpers ===

    private T AddNode<T>(T node) where T : Node
    {
        if (_root == null)
        {
            throw new System.InvalidOperationException("Test root not initialized");
        }

        _root.AddChild(node);
        _testNodes.Add(node);
        return node;
    }

    private static void InvokePrivate(object target, string method)
    {
        var info = target.GetType().GetMethod(
            method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        info?.Invoke(target, null);
    }

    private static void ResetSettingsSingletons()
    {
        var settingsProp = typeof(SettingsManager).GetProperty(
            "Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        settingsProp?.SetValue(null, null);

        var testProp = typeof(TestSettingsManager).GetProperty(
            "Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        testProp?.SetValue(null, null);
    }
}
