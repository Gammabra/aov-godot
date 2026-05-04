using AshesOfVelsingrad.Systems;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Systems;

/// <summary>
///     Coverage for the <see cref="Faction" /> enum + <see cref="FactionExtensions" />
///     helpers. Drives the three-faction turn flow (Player / Ally / Enemy) and the
///     target-tile filter, so any regression in the relationship table breaks the HUD's
///     red-tile filtering and the AI target picker.
/// </summary>
[TestFixture]
public class FactionTests
{
    [Test]
    public void Player_AndAlly_AreFriendly()
    {
        Assert.That(Faction.Player.IsFriendlyTo(Faction.Ally), Is.True);
        Assert.That(Faction.Ally.IsFriendlyTo(Faction.Player), Is.True);
    }

    [Test]
    public void Player_AndEnemy_AreHostile()
    {
        Assert.That(Faction.Player.IsHostileTo(Faction.Enemy), Is.True);
        Assert.That(Faction.Enemy.IsHostileTo(Faction.Player), Is.True);
    }

    [Test]
    public void Ally_AndEnemy_AreHostile()
    {
        Assert.That(Faction.Ally.IsHostileTo(Faction.Enemy), Is.True);
        Assert.That(Faction.Enemy.IsHostileTo(Faction.Ally), Is.True);
    }

    [Test]
    public void SameFaction_IsAlwaysFriendly()
    {
        foreach (Faction f in System.Enum.GetValues<Faction>())
        {
            Assert.That(f.IsFriendlyTo(f), Is.True, $"{f} should be friendly to itself");
            Assert.That(f.IsHostileTo(f), Is.False, $"{f} should not be hostile to itself");
        }
    }

    [Test]
    public void IsHostileTo_IsTheNegationOfIsFriendlyTo()
    {
        foreach (Faction a in System.Enum.GetValues<Faction>())
        foreach (Faction b in System.Enum.GetValues<Faction>())
            Assert.That(a.IsHostileTo(b), Is.EqualTo(!a.IsFriendlyTo(b)),
                $"hostile/friendly should be exact opposites for ({a}, {b})");
    }

    [Test]
    public void OnlyPlayer_IsUserControlled()
    {
        Assert.That(Faction.Player.IsAiControlled(), Is.False);
        Assert.That(Faction.Ally.IsAiControlled(), Is.True);
        Assert.That(Faction.Enemy.IsAiControlled(), Is.True);
    }
}
