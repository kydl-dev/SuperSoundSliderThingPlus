using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Example.Domain;

namespace SuperSoundSliderThingPlus.Tests.Unit
{
    /// EXAMPLE (template): every branch of a domain rule, in priority order. Replace with the mod's own rules.
    public class ExampleRulesTests
    {
        [TestCase(5f, 5f, "in range")]
        [TestCase(-1f, 0f, "clamped into range")]
        [TestCase(12f, 10f, "clamped into range")]
        [TestCase(10f, 10f, "in range")]
        public void Decide_ClampsIntoRange(float requested, float expected, string reason)
        {
            var r = ExampleRules.Decide(requested);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Success));
            Assert.That(r.Value, Is.EqualTo(expected));
            Assert.That(r.Reason, Is.EqualTo(reason));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Decide_RejectsNonNumbers(float requested)
        {
            var r = ExampleRules.Decide(requested);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(r.Reason, Is.EqualTo(ExampleRules.NotANumber));
        }
    }
}
