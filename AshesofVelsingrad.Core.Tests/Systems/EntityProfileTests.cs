using AshesOfVelsingrad.Data;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Systems;

/// <summary>
///     Coverage for <see cref="EntityProfile" /> — the designer-authored display metadata
///     resource attached to combatants. The class is a simple property bag so the tests
///     just verify the defaults and that the setters round-trip, but the assertions hit
///     every line the coverage tool flagged as missing (<c>DisplayName</c>, <c>Portrait</c>,
///     <c>Level</c>, <c>ClassName</c>, <c>Bio</c>).
/// </summary>
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
        Assert.That(profile.Portrait, Is.Null);
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
        };

        Assert.That(profile.DisplayName, Is.EqualTo("Pikachu"));
        Assert.That(profile.ClassName, Is.EqualTo("Combattant"));
        Assert.That(profile.Level, Is.EqualTo(7));
        Assert.That(profile.Bio, Does.StartWith("An electric"));
    }

    [Test]
    public void Portrait_IsNullableAndAssignable()
    {
        var profile = new EntityProfile();
        Assert.That(profile.Portrait, Is.Null);
        // We can't easily construct a Texture2D in a non-Godot-runtime unit test, so the
        // null round-trip is the meaningful coverage we get here. The setter being
        // exercised at all is enough for the line-coverage tool.
        profile.Portrait = null;
        Assert.That(profile.Portrait, Is.Null);
    }
}
