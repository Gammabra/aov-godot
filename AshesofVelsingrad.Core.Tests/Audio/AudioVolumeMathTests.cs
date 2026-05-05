using AshesOfVelsingrad.Audio;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Audio;

/// <summary>
///     Coverage for <see cref="AudioVolumeMath" />. Slider values come from the user
///     unmodified, so any regression in the linear↔dB conversion lands directly on
///     the player's ears.
/// </summary>
[TestFixture]
public class AudioVolumeMathTests
{
    private const float _tolerance = 0.001f;

    [Test]
    public void ClampLinear_BelowZero_ReturnsZero()
    {
        Assert.That(AudioVolumeMath.ClampLinear(-0.5f), Is.EqualTo(AudioVolumeMath.MinLinear));
    }

    [Test]
    public void ClampLinear_AboveOne_ReturnsOne()
    {
        Assert.That(AudioVolumeMath.ClampLinear(1.7f), Is.EqualTo(AudioVolumeMath.MaxLinear));
    }

    [Test]
    public void ClampLinear_InRange_ReturnsValueUnchanged()
    {
        Assert.That(AudioVolumeMath.ClampLinear(0.42f), Is.EqualTo(0.42f).Within(_tolerance));
    }

    [Test]
    public void ClampLinear_NaN_ReturnsZero()
    {
        Assert.That(AudioVolumeMath.ClampLinear(float.NaN), Is.EqualTo(AudioVolumeMath.MinLinear));
    }

    [Test]
    public void LinearToDb_Unity_ReturnsZeroDb()
    {
        Assert.That(AudioVolumeMath.LinearToDb(1f), Is.EqualTo(0f).Within(_tolerance));
    }

    [Test]
    public void LinearToDb_Half_ReturnsApproxNegativeSixDb()
    {
        // 20 * log10(0.5) ≈ -6.0206
        Assert.That(AudioVolumeMath.LinearToDb(0.5f), Is.EqualTo(-6.0206f).Within(0.01f));
    }

    [Test]
    public void LinearToDb_Zero_ReturnsSilenceDb()
    {
        Assert.That(AudioVolumeMath.LinearToDb(0f), Is.EqualTo(AudioVolumeMath.SilenceDb));
    }

    [Test]
    public void LinearToDb_BelowAudibleFloor_ReturnsSilenceDb()
    {
        Assert.That(AudioVolumeMath.LinearToDb(AudioVolumeMath.MinAudibleLinear * 0.5f),
            Is.EqualTo(AudioVolumeMath.SilenceDb));
    }

    [Test]
    public void LinearToDb_AboveOne_IsClampedFirst()
    {
        Assert.That(AudioVolumeMath.LinearToDb(5f), Is.EqualTo(0f).Within(_tolerance));
    }

    [Test]
    public void DbToLinear_ZeroDb_ReturnsUnity()
    {
        Assert.That(AudioVolumeMath.DbToLinear(0f), Is.EqualTo(1f).Within(_tolerance));
    }

    [Test]
    public void DbToLinear_SilenceFloor_ReturnsZero()
    {
        Assert.That(AudioVolumeMath.DbToLinear(AudioVolumeMath.SilenceDb), Is.EqualTo(0f));
    }

    [Test]
    public void DbToLinear_NaN_ReturnsZero()
    {
        Assert.That(AudioVolumeMath.DbToLinear(float.NaN), Is.EqualTo(0f));
    }

    [Test]
    public void RoundTrip_LinearDbLinear_PreservesValue()
    {
        foreach (var input in new[] { 0.1f, 0.25f, 0.5f, 0.8f, 1.0f })
        {
            var roundTripped = AudioVolumeMath.DbToLinear(AudioVolumeMath.LinearToDb(input));
            Assert.That(roundTripped, Is.EqualTo(input).Within(_tolerance),
                $"round-trip drift for input={input}");
        }
    }
}
