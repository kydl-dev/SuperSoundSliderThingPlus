using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Tests.Unit
{
    /// The clamp the hub sliders go through before a level is written (D-011: mute levels 0..100 percent).
    public class SoundMuteLevelTests
    {
        [TestCase(0f, 0f, SoundMixRules.InRange)]
        [TestCase(37.5f, 37.5f, SoundMixRules.InRange)]
        [TestCase(100f, 100f, SoundMixRules.InRange)]
        [TestCase(-1f, 0f, SoundMixRules.Clamped)]
        [TestCase(250f, 100f, SoundMixRules.Clamped)]
        public void Level_ClampsIntoZeroToHundred(float requested, float expected, string reason)
        {
            var r = SoundMixRules.Level(requested);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Success));
            Assert.That(r.Value, Is.EqualTo(expected));
            Assert.That(r.Reason, Is.EqualTo(reason));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Level_RejectsNonNumbers(float requested)
        {
            var r = SoundMixRules.Level(requested);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(r.Reason, Is.EqualTo(SoundMixRules.NotANumber));
        }
    }
}
