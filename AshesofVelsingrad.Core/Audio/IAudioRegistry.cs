using System.Collections.Generic;

namespace AshesOfVelsingrad.Audio;

/// <summary>
///     Lookup table for every <see cref="AudioTrack" /> known to the game.
/// </summary>
/// <remarks>
///     <para>
///         The registry is populated at start-up by <see cref="AudioCatalog" /> and
///         queried by <see cref="IAudioService" /> when call sites use a track id
///         instead of a raw resource path. Treat ids as the public contract — they
///         live as <c>const</c> fields on <see cref="AudioCatalog" /> and survive
///         asset-path renames.
///     </para>
///     <para>
///         Implementations are expected to be case-insensitive on ids and to reject
///         duplicate registrations explicitly so a typo in two catalog entries
///         doesn't silently overwrite the first one.
///     </para>
/// </remarks>
public interface IAudioRegistry
{
    /// <summary>Number of tracks currently registered.</summary>
    int Count { get; }

    /// <summary>All registered tracks, in insertion order.</summary>
    IReadOnlyCollection<AudioTrack> Tracks { get; }

    /// <summary>
    ///     Adds <paramref name="track" /> to the registry.
    /// </summary>
    /// <param name="track">Track metadata to register.</param>
    /// <exception cref="System.ArgumentNullException">When <paramref name="track" /> is null.</exception>
    /// <exception cref="System.ArgumentException">
    ///     When the track id is empty or already registered.
    /// </exception>
    void Register(AudioTrack track);

    /// <summary>Returns the track with id <paramref name="trackId" />, or <c>null</c> if missing.</summary>
    AudioTrack? Find(string trackId);

    /// <summary>
    ///     Looks up a track by id and returns it via <paramref name="track" />.
    /// </summary>
    /// <param name="trackId">Track identifier.</param>
    /// <param name="track">Resolved track when the method returns <c>true</c>.</param>
    /// <returns><c>true</c> when the track exists, <c>false</c> otherwise.</returns>
    bool TryGet(string trackId, out AudioTrack track);

    /// <summary>Returns <c>true</c> if a track with this id has been registered.</summary>
    bool Contains(string trackId);

    /// <summary>Removes every registered track.</summary>
    void Clear();
}
