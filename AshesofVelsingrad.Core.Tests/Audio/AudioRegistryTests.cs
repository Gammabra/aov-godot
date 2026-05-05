using System;
using AshesOfVelsingrad.Audio;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Audio;

/// <summary>
///     Coverage for <see cref="AudioRegistry" />. The registry sits between the
///     catalog (registration) and the audio service (lookup), so the contract here
///     drives every Play(trackId) call site.
/// </summary>
[TestFixture]
public class AudioRegistryTests
{
    private static AudioTrack MakeTrack(string id = "music.test", float multiplier = 1f) =>
        new(
            Id: id,
            ResourcePath: "res://fake/path.ogg",
            Bus: AudioBus.Music,
            BaseVolumeMultiplier: multiplier,
            Loop: true);

    [Test]
    public void Register_AddsTrackToTheRegistry()
    {
        var registry = new AudioRegistry();
        var track = MakeTrack();

        registry.Register(track);

        Assert.That(registry.Count, Is.EqualTo(1));
        Assert.That(registry.Contains(track.Id), Is.True);
        Assert.That(registry.Find(track.Id), Is.SameAs(track));
    }

    [Test]
    public void Register_NullTrack_Throws()
    {
        var registry = new AudioRegistry();
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Test]
    public void Register_EmptyId_Throws()
    {
        var registry = new AudioRegistry();
        var track = MakeTrack(id: "");
        Assert.Throws<ArgumentException>(() => registry.Register(track));
    }

    [Test]
    public void Register_DuplicateId_Throws()
    {
        var registry = new AudioRegistry();
        registry.Register(MakeTrack(id: "music.dup"));
        Assert.Throws<ArgumentException>(() => registry.Register(MakeTrack(id: "music.dup")));
    }

    [Test]
    public void Find_UnknownId_ReturnsNull()
    {
        var registry = new AudioRegistry();
        Assert.That(registry.Find("does.not.exist"), Is.Null);
    }

    [Test]
    public void Find_IsCaseInsensitive()
    {
        var registry = new AudioRegistry();
        var track = MakeTrack(id: "Music.MainMenu");
        registry.Register(track);

        Assert.That(registry.Find("music.mainmenu"), Is.SameAs(track));
        Assert.That(registry.Contains("MUSIC.MAINMENU"), Is.True);
    }

    [Test]
    public void TryGet_UnknownId_ReturnsFalse()
    {
        var registry = new AudioRegistry();
        var found = registry.TryGet("missing", out var track);

        Assert.That(found, Is.False);
        Assert.That(track, Is.Null);
    }

    [Test]
    public void TryGet_KnownId_ReturnsTrueAndTrack()
    {
        var registry = new AudioRegistry();
        var registered = MakeTrack();
        registry.Register(registered);

        var found = registry.TryGet(registered.Id, out var track);

        Assert.That(found, Is.True);
        Assert.That(track, Is.SameAs(registered));
    }

    [Test]
    public void Tracks_PreservesInsertionOrder()
    {
        var registry = new AudioRegistry();
        var first = MakeTrack(id: "music.a");
        var second = MakeTrack(id: "music.b");
        var third = MakeTrack(id: "music.c");

        registry.Register(first);
        registry.Register(second);
        registry.Register(third);

        Assert.That(registry.Tracks, Is.EqualTo(new[] { first, second, third }));
    }

    [Test]
    public void Clear_RemovesEverything()
    {
        var registry = new AudioRegistry();
        registry.Register(MakeTrack(id: "music.a"));
        registry.Register(MakeTrack(id: "music.b"));

        registry.Clear();

        Assert.That(registry.Count, Is.EqualTo(0));
        Assert.That(registry.Find("music.a"), Is.Null);
    }

    [Test]
    public void Find_NullId_ReturnsNull()
    {
        var registry = new AudioRegistry();
        registry.Register(MakeTrack(id: "music.a"));

        Assert.That(registry.Find(null!), Is.Null);
    }

    [Test]
    public void Find_EmptyId_ReturnsNull()
    {
        var registry = new AudioRegistry();
        registry.Register(MakeTrack(id: "music.a"));

        Assert.That(registry.Find(string.Empty), Is.Null);
    }

    [Test]
    public void TryGet_NullId_ReturnsFalse()
    {
        var registry = new AudioRegistry();
        registry.Register(MakeTrack(id: "music.a"));

        var found = registry.TryGet(null!, out var track);

        Assert.That(found, Is.False);
        Assert.That(track, Is.Null);
    }

    [Test]
    public void TryGet_EmptyId_ReturnsFalse()
    {
        var registry = new AudioRegistry();
        registry.Register(MakeTrack(id: "music.a"));

        var found = registry.TryGet(string.Empty, out var track);

        Assert.That(found, Is.False);
        Assert.That(track, Is.Null);
    }

    [Test]
    public void Contains_NullId_ReturnsFalse()
    {
        var registry = new AudioRegistry();
        registry.Register(MakeTrack(id: "music.a"));

        Assert.That(registry.Contains(null!), Is.False);
    }

    [Test]
    public void Contains_EmptyId_ReturnsFalse()
    {
        var registry = new AudioRegistry();
        registry.Register(MakeTrack(id: "music.a"));

        Assert.That(registry.Contains(string.Empty), Is.False);
    }

    [Test]
    public void Clear_AllowsReRegistrationOfSameIds()
    {
        var registry = new AudioRegistry();
        registry.Register(MakeTrack(id: "music.a"));
        registry.Clear();

        Assert.DoesNotThrow(() => registry.Register(MakeTrack(id: "music.a")));
    }
}
