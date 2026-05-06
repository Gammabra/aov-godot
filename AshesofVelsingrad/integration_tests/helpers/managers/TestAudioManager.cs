using System.Reflection;
using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.Helpers.Managers;

/// <summary>
///     Test-time subclass that exposes <see cref="AudioManager" />'s singleton hooks for
///     reflection-driven test setup and bypasses the noisy startup logging.
/// </summary>
/// <remarks>
///     Mirrors the pattern used by <c>TestSettingsManager</c>. We do not override
///     <c>Initialize</c> behaviour beyond singleton wiring — the production manager's
///     bus / pool setup runs as-is so the integration tests cover real code paths.
/// </remarks>
public partial class TestAudioManager : AudioManager
{
    public TestAudioManager()
    {
        Name = "TestAudioManager";
    }

    /// <summary>
    ///     Reflection helper: clear the static <see cref="AudioManager.Instance" /> so
    ///     each test starts with a clean slate.
    /// </summary>
    public static void ResetSingleton()
    {
        var prop = typeof(AudioManager).GetProperty(
            "Instance",
            BindingFlags.Public | BindingFlags.Static);
        prop?.SetValue(null, null);
    }

    /// <summary>Invokes the protected <c>Initialize</c> method via reflection.</summary>
    public void TestInitialize()
    {
        var method = typeof(AudioManager).GetMethod(
            "Initialize",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(this, null);
    }

    /// <summary>Returns the live decibel value Godot has on a bus by name.</summary>
    public static float GetBusVolumeDb(string busName)
    {
        var index = AudioServer.GetBusIndex(busName);
        return index == -1 ? 0f : AudioServer.GetBusVolumeDb(index);
    }

    /// <summary>Returns whether the given bus exists in <c>AudioServer</c>.</summary>
    public static bool BusExists(string busName) => AudioServer.GetBusIndex(busName) != -1;
}
