using System;
using AshesOfVelsingrad.Audio;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Audio;

/// <summary>
///     Coverage for <see cref="AudioCatalog" />. The catalog is hand-edited each
///     time a song is added; these tests guard the wiring so a copy-paste mistake
///     (wrong path, wrong bus, missing entry) gets caught before it ships.
/// </summary>
[TestFixture]
public class AudioCatalogTests
{
    [Test]
    public void RegisterDefaults_NullRegistry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AudioCatalog.RegisterDefaults(null!));
    }

    [Test]
    public void RegisterDefaults_RegistersTheMainMenuTheme()
    {
        var registry = new AudioRegistry();

        AudioCatalog.RegisterDefaults(registry);

        Assert.That(registry.Contains(AudioCatalog.MainMenuTheme), Is.True,
            "main menu theme must be in the catalog so the menu can call Play(...) on it");
    }

    [Test]
    public void MainMenuTheme_IsConfiguredForMusicBusAndLoops()
    {
        var registry = new AudioRegistry();
        AudioCatalog.RegisterDefaults(registry);

        var track = registry.Find(AudioCatalog.MainMenuTheme);

        Assert.That(track, Is.Not.Null);
        Assert.That(track!.Bus, Is.EqualTo(AudioBus.Music));
        Assert.That(track.Loop, Is.True);
        Assert.That(track.ResourcePath, Does.StartWith("res://"));
        Assert.That(track.ResourcePath, Does.EndWith("TA_A.ogg"));
    }

    [Test]
    public void MainMenuTheme_BaseVolumeMultiplier_IsInValidRange()
    {
        var registry = new AudioRegistry();
        AudioCatalog.RegisterDefaults(registry);

        var track = registry.Find(AudioCatalog.MainMenuTheme)!;

        Assert.That(track.BaseVolumeMultiplier, Is.InRange(0f, 1f));
    }

    [Test]
    public void RegisterDefaults_TwiceOnTheSameRegistry_Throws()
    {
        // The registry is single-shot; double-registering surfaces a copy-paste
        // mistake instead of silently overwriting an entry. AudioManager calls
        // Clear() before re-running this in test scenarios.
        var registry = new AudioRegistry();
        AudioCatalog.RegisterDefaults(registry);

        Assert.Throws<ArgumentException>(() => AudioCatalog.RegisterDefaults(registry));
    }
}
