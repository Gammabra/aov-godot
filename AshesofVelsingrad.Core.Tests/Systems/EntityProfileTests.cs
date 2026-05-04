using AshesOfVelsingrad.Data;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Systems;

/// <summary>
///     Coverage for <see cref="EntityProfile" /> — pure-C# display metadata.
/// </summary>
/// <remarks>
///     <para>
///         Originally <see cref="EntityProfile" /> derived from <c>Godot.Resource</c>, which
///         crashed the test host with <c>0xC0000005</c> (access violation) inside
///         <c>godotsharp_string_new_with_utf16_chars</c> — Godot's native runtime isn't
///         initialised in unit tests, so the Resource static constructor blew up.
///     </para>
///     <para>
///         The fix: Core owns the <em>data shape</em> (this class), Godot owns the
///         <em>texture loading</em> (HUD widgets call <c>ResourceLoader.Load&lt;Texture2D&gt;</c>
///         against <see cref="EntityProfile.PortraitPath" /> at render time). The class is
///         now a plain <c>sealed</c> with auto-properties, fully testable from a non-Godot
///         process and serialisable for save/load with no special handling.
///     </para>
/// </remarks>
[TestFixture]
public class EntityProfileTests
{
    [Test]
    public void Defaults_AreEmptyStringsAndLevelOne()
    {
        var profile = new EntityProfile();
        Assert.That(profile.DisplayName, Is.EqualTo(string.Empty));
        Assert.That(profile.ClassName, Is.EqualTo(string.Empty));
        Assert.That(profile.Bio, Is.EqualTo(string.Empty));
        Assert.That(profile.PortraitPath, Is.EqualTo(string.Empty));
        Assert.That(profile.Level, Is.EqualTo(1));
    }

    [Test]
    public void Setters_AssignValuesAndAreReadable()
    {
        var profile = new EntityProfile
        {
            DisplayName = "Pikachu",
            ClassName = "Combattant",
            Level = 7,
            Bio = "An electric fighter with a fierce spirit.",
            PortraitPath = "res://assets/portraits/Pikachu.png",
        };

        Assert.That(profile.DisplayName, Is.EqualTo("Pikachu"));
        Assert.That(profile.ClassName, Is.EqualTo("Combattant"));
        Assert.That(profile.Level, Is.EqualTo(7));
        Assert.That(profile.Bio, Does.StartWith("An electric"));
        Assert.That(profile.PortraitPath, Does.StartWith("res://"));
    }

    [Test]
    public void PortraitPath_IsAStringThatRoundTripsViaSetter()
    {
        var profile = new EntityProfile { PortraitPath = "res://assets/portraits/test.png" };
        Assert.That(profile.PortraitPath, Is.EqualTo("res://assets/portraits/test.png"));

        // Empty path is also valid — HUD widgets fall back to a coloured placeholder
        // when the path is empty or the resource doesn't exist.
        profile.PortraitPath = string.Empty;
        Assert.That(profile.PortraitPath, Is.EqualTo(string.Empty));
    }
}
