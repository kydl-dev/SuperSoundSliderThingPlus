using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Tests.Unit
{
    public class LoudnessEstimateTests
    {
        [Test] public void NotReady_UntilEnoughBlocks()
        {
            var e = new LoudnessEstimate();
            e.Add(0.1f); e.Add(0.1f);
            Assert.That(e.IsReady, Is.False);
            Assert.That(e.Rms, Is.EqualTo(0f));
            e.Add(0.1f);
            Assert.That(e.IsReady, Is.True);
            Assert.That(e.Rms, Is.EqualTo(0.1f).Within(1e-6f));
        }

        [Test] public void Rms_IsThePowerMean()
        {
            var e = new LoudnessEstimate();
            e.Add(0.3f); e.Add(0.4f); e.Add(0.5f);
            Assert.That(e.Rms, Is.EqualTo(System.Math.Sqrt((0.09 + 0.16 + 0.25) / 3)).Within(1e-5));
        }

        [TestCase(0f)]
        [TestCase(-0.2f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Silent_OrInvalidBlocks_AreIgnored(float block)
        {
            var e = new LoudnessEstimate();
            Assert.That(e.Add(block), Is.False);
            Assert.That(e.Blocks, Is.EqualTo(0));
        }

        [Test] public void StopsLearning_WhenFull()
        {
            var e = new LoudnessEstimate();
            for (int i = 0; i < LoudnessEstimate.MaxBlocks; i++) Assert.That(e.Add(0.1f), Is.True);
            Assert.That(e.WantsMore, Is.False);
            Assert.That(e.Add(0.9f), Is.False);
            Assert.That(e.Rms, Is.EqualTo(0.1f).Within(1e-6f));
        }
    }
}
