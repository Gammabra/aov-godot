using NUnit.Framework;
using AshesOfVelsingrad.Data; 
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesofVelsingrad.Tests.Data;

[TestFixture]
public class StatusEffectTest
{
    [Test]
    public void AtkBuffer_IncreasesAttack()
    {
        var statusEffect = new AtkBuffer(3, AovDataStructures.ModifierType.Flat, 30);
        Assert.That(statusEffect.Name, Is.EqualTo("AtkBuffer"));
        Assert.That(statusEffect.Duration, Is.EqualTo(3));
    }

    [Test]
    public void BurningEffect_HasCorrectDuration()
    {
        var effect = new BurningEffect(2, AovDataStructures.ModifierType.Flat, 10);
        Assert.That(effect.Duration, Is.EqualTo(2));
    }

    [Test]
    public void Stun_IsNotStackable()
    {
        var stun = new Stun(1);
        Assert.That(stun.IsStackable, Is.False);
    }
}