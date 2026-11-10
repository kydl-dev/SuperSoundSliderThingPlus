using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Tests.Unit
{
    /// Every branch of the mute-level maths (D-004): master 0 = full loudness, 100 = muted, the category is scaled by the master.
    public class SoundMixRulesTests
    {
        [TestCase(0f, 0f, 1f)]
        [TestCase(100f, 0f, 0f)]
        [TestCase(0f, 100f, 0f)]
        [TestCase(50f, 0f, 0.5f)]
        [TestCase(0f, 50f, 0.5f)]
        [TestCase(50f, 50f, 0.25f)]
        [TestCase(100f, 100f, 0f)]
        public void Factor_MultipliesTheTwoLoudnessFractions(float master, float category, float expected)
        {
            var r = SoundMixRules.Factor(master, category);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Success));
            Assert.That(r.Value, Is.EqualTo(expected).Within(0.0001f));
            Assert.That(r.Reason, Is.EqualTo(SoundMixRules.InRange));
        }

        [TestCase(-10f, 0f, 1f)]
        [TestCase(0f, -10f, 1f)]
        [TestCase(150f, 0f, 0f)]
        [TestCase(0f, 150f, 0f)]
        public void Factor_ClampsLevelsOutsideTheRange(float master, float category, float expected)
        {
            var r = SoundMixRules.Factor(master, category);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Success));
            Assert.That(r.Value, Is.EqualTo(expected).Within(0.0001f));
            Assert.That(r.Reason, Is.EqualTo(SoundMixRules.Clamped));
        }

        [Test]
        public void Factor_RaisingTheMasterNeverRaisesTheFactor()
        {
            float previous = float.MaxValue;
            for (float master = SoundMixRules.MinMute; master <= SoundMixRules.MaxMute; master += 5f)
            {
                float factor = SoundMixRules.Factor(master, 30f).Value;
                Assert.That(factor, Is.LessThanOrEqualTo(previous));
                previous = factor;
            }
        }

        [TestCase(float.NaN, 0f)]
        [TestCase(0f, float.NaN)]
        [TestCase(float.PositiveInfinity, 0f)]
        [TestCase(0f, float.NegativeInfinity)]
        public void Factor_RejectsNonNumbers(float master, float category)
        {
            var r = SoundMixRules.Factor(master, category);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(r.Reason, Is.EqualTo(SoundMixRules.NotANumber));
        }
    }
}
