using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Tests.Unit
{
    public class LoudnessRulesTests
    {
        [Test] public void Rms_OfConstantSamples_IsTheirMagnitude()
        {
            var r = LoudnessRules.Rms(new[] { 0.5f, -0.5f, 0.5f, -0.5f }, 4);
            Assert.That(r.IsSuccess, Is.True);
            Assert.That(r.Value, Is.EqualTo(0.5f).Within(1e-6f));
        }

        [Test] public void Rms_UsesOnlyTheFirstCountSamples()
        {
            var r = LoudnessRules.Rms(new[] { 1f, 1f, 99f, 99f }, 2);
            Assert.That(r.Value, Is.EqualTo(1f).Within(1e-6f));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(5)]
        public void Rms_BadCount_IsRejected(int count)
            => Assert.That(LoudnessRules.Rms(new[] { 0.1f, 0.2f }, count).IsSuccess, Is.False);

        [Test] public void Rms_NullSamples_IsRejected()
            => Assert.That(LoudnessRules.Rms(null, 1).IsSuccess, Is.False);

        [Test] public void Rms_NaNSample_IsRejected()
            => Assert.That(LoudnessRules.Rms(new[] { float.NaN }, 1).IsSuccess, Is.False);

        [Test] public void Gain_QuietClip_IsBoostedTowardTheTarget()
        {
            var r = LoudnessRules.Gain(0.05f, 0.1f, 0.25f, 4f);
            Assert.That(r.Value, Is.EqualTo(2f).Within(1e-5f));
            Assert.That(r.Reason, Is.EqualTo(LoudnessRules.Exact));
        }

        [Test] public void Gain_LoudClip_IsCutTowardTheTarget()
            => Assert.That(LoudnessRules.Gain(0.4f, 0.1f, 0.25f, 4f).Value, Is.EqualTo(0.25f).Within(1e-5f));

        [Test] public void Gain_IsLimitedToTheRange()
        {
            var up = LoudnessRules.Gain(0.001f, 0.1f, 0.25f, 4f);
            Assert.That(up.Value, Is.EqualTo(4f));
            Assert.That(up.Reason, Is.EqualTo(LoudnessRules.Limited));
            Assert.That(LoudnessRules.Gain(0.9f, 0.1f, 0.5f, 4f).Value, Is.EqualTo(0.5f));
        }

        [Test] public void Gain_SilentClip_IsLeftAlone()
        {
            var r = LoudnessRules.Gain(0f, 0.1f, 0.25f, 4f);
            Assert.That(r.IsSuccess, Is.True);
            Assert.That(r.Value, Is.EqualTo(1f));
        }

        [Test] public void Gain_NaNMeasurement_IsLeftAlone()
            => Assert.That(LoudnessRules.Gain(float.NaN, 0.1f, 0.25f, 4f).Value, Is.EqualTo(1f));

        [TestCase(0f, 0.25f, 4f)]
        [TestCase(0.1f, 0f, 4f)]
        [TestCase(0.1f, 4f, 0.25f)]
        [TestCase(float.NaN, 0.25f, 4f)]
        [TestCase(0.1f, 0.25f, float.PositiveInfinity)]
        public void Gain_InvalidTargetOrLimits_IsRejected(float target, float min, float max)
            => Assert.That(LoudnessRules.Gain(0.05f, target, min, max).IsSuccess, Is.False);
    }
}
