using AshesOfVelsingrad.Data;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Systems;

/// <summary>
///     Coverage for <see cref="EntityProfile" /> — the designer-authored display metadata
///     resource attached to combatants. <see cref="EntityProfile" /> is a Godot
///     <c>Resource</c>, so each instance is wrapped in <c>using</c> to ensure the underlying
///     native handle is released before the NUnit host shuts down. Without that, the
///     <c>GodotObject</c> finalizer tries to free the handle outside a Godot runtime and
///     crashes the test host (the tests themselves all pass — see "Test host process
///     crashed" with exit code 1 on the previous CI run).
/// </summary>
[TestFixture]
public class EntityProfileTests
{
    [Test]
    public void Defaults_AreEmptyStringsAndLevelOne()
    {
        using var profile = new EntityProfile();
        Assert.That(profile.DisplayName, Is.EqualTo(string.Empty));
        Assert.That(profile.ClassName, Is.EqualTo(string.Empty));
        Assert.That(profile.Bio, Is.EqualTo(string.Empty));
        Assert.That(profile.Portrait, Is.Null);
        Assert.That(profile.Level, Is.EqualTo(1));
    }

    [Test]
    public void Setters_AssignValuesAndAreReadable()
    {
        using var profile = new EntityProfile
        {
            DisplayName = "Pikachu",
            ClassName = "Combattant",
            Level = 7,
            Bio = "An electric fighter with a fierce spirit.",
        };

        Assert.That(profile.DisplayName, Is.EqualTo("Pikachu"));
        Assert.That(profile.ClassName, Is.EqualTo("Combattant"));
        Assert.That(profile.Level, Is.EqualTo(7));
        Assert.That(profile.Bio, Does.StartWith("An electric"));
    }

    [Test]
    public void Portrait_IsNullableAndAssignable()
    {
        using var profile = new EntityProfile();
        Assert.That(profile.Portrait, Is.Null);
        // We can't easily construct a Texture2D in a non-Godot-runtime unit test, so the
        // null round-trip is the meaningful coverage we get here. The setter being
        // exercised at all is enough for the line-coverage tool.
        profile.Portrait = null;
        Assert.That(profile.Portrait, Is.Null);
    }
}
