using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Systems;

[TestFixture]
public class SkillSystemTests
{
    // Concrete implementation for testing the abstract base
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private class TestSkill : SkillSystem
    {
        public TestSkill(string name, int totalCooldown, int range)
        {
            Name = name;
            TotalCooldown = totalCooldown;
            Range = range;
            Cooldown = 0;
        }

        // Mock implementation of the abstract Use method
        public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
        {
            SetCooldown();
        }
    }

    [Test]
    public void Constructor_InitializesBasicProperties()
    {
        // Act
        var skill = new TestSkill("Fireball", 3, 5);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(skill.Name, Is.EqualTo("Fireball"));
            Assert.That(skill.TotalCooldown, Is.EqualTo(3));
            Assert.That(skill.Range, Is.EqualTo(5));
            Assert.That(skill.Cooldown, Is.EqualTo(0));
        });
    }

    [Test]
    public void SetCooldown_SetsCooldownToTotal_WhenCurrentCooldownIsZero()
    {
        // Arrange
        var skill = new TestSkill("Heal", 2, 1);

        // Act
        skill.SetCooldown();

        // Assert
        Assert.That(skill.Cooldown, Is.EqualTo(2));
    }

    [Test]
    public void SetCooldown_DoesNotOverwrite_WhenCooldownIsAlreadyActive()
    {
        // Arrange
        var skill = new TestSkill("Heal", 5, 1);
        skill.SetCooldown(); // Sets to 5

        // Act - Try to set it again while it's at 5
        skill.SetCooldown();

        // Assert - Should still be 5, not reset or added to
        Assert.That(skill.Cooldown, Is.EqualTo(5));
    }

    [Test]
    public void ReduceCooldown_DecrementsValue_ButNotBelowZero()
    {
        // Arrange
        var skill = new TestSkill("Slash", 2, 1);
        skill.SetCooldown(); // Cooldown is now 2

        // Act & Assert
        skill.ReduceCooldown();
        Assert.That(skill.Cooldown, Is.EqualTo(1));

        skill.ReduceCooldown();
        Assert.That(skill.Cooldown, Is.EqualTo(0));

        skill.ReduceCooldown();
        Assert.That(skill.Cooldown, Is.EqualTo(0), "Cooldown should never be negative.");
    }
}
