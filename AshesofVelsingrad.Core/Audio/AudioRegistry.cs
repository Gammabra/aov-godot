using System;
using System.Collections.Generic;

namespace AshesOfVelsingrad.Audio;

/// <summary>
///     In-memory <see cref="IAudioRegistry" /> backed by an ordered dictionary.
/// </summary>
/// <remarks>
///     Lives in Core so the registry is exercised by NUnit without spinning up
///     Godot. The adapter constructs one instance per <c>AudioManager</c>; the
///     same registry instance is shared with whatever code (UI, scenes) wants to
///     enumerate available tracks.
/// </remarks>
public sealed class AudioRegistry : IAudioRegistry
{
    private readonly Dictionary<string, AudioTrack> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<AudioTrack> _ordered = new();

    /// <inheritdoc />
    public int Count => _ordered.Count;

    /// <inheritdoc />
    public IReadOnlyCollection<AudioTrack> Tracks => _ordered;

    /// <inheritdoc />
    public void Register(AudioTrack track)
    {
        if (track == null) throw new ArgumentNullException(nameof(track));
        if (string.IsNullOrWhiteSpace(track.Id))
        {
            throw new ArgumentException("AudioTrack.Id must be non-empty.", nameof(track));
        }

        if (_byId.ContainsKey(track.Id))
        {
            throw new ArgumentException(
                $"An AudioTrack with id '{track.Id}' is already registered.",
                nameof(track));
        }

        _byId[track.Id] = track;
        _ordered.Add(track);
    }

    /// <inheritdoc />
    public AudioTrack? Find(string trackId)
    {
        if (string.IsNullOrEmpty(trackId)) return null;
        return _byId.TryGetValue(trackId, out var track) ? track : null;
    }

    /// <inheritdoc />
    public bool TryGet(string trackId, out AudioTrack track)
    {
        if (!string.IsNullOrEmpty(trackId) && _byId.TryGetValue(trackId, out var found))
        {
            track = found;
            return true;
        }

        track = null!;
        return false;
    }

    /// <inheritdoc />
    public bool Contains(string trackId)
    {
        return !string.IsNullOrEmpty(trackId) && _byId.ContainsKey(trackId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _byId.Clear();
        _ordered.Clear();
    }
}
