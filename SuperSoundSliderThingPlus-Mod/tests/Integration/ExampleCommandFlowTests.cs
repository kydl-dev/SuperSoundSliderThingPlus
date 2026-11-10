using System.Collections.Generic;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Example.Application;
using SuperSoundSliderThingPlus.Features.Example.Execution;

namespace SuperSoundSliderThingPlus.Tests.Integration
{
    /// EXAMPLE (template, spec 9 "integration"): command -> dispatcher -> handler -> pipeline -> rules -> executor, with a fake ownership and a
    /// recording action instead of the game. Replace with the mod's own command flows (one test per handler, the owner gate of each executor).
    public class ExampleCommandFlowTests
    {
        sealed class Owner : IOwnership { public bool IsOwner { get; set; } public long OwnerId { get { return 1; } } }

        static CommandDispatcher Wire(Owner owner, List<float> applied)
        {
            var d = new CommandDispatcher();
            d.Register(new SetExampleValueHandler(new ExampleValueExecutor(owner, applied.Add), null));
            return d;
        }

        [Test] public void Owner_TheClampedValueIsApplied()
        {
            var applied = new List<float>();
            var r = Wire(new Owner { IsOwner = true }, applied).Dispatch(new SetExampleValueCommand(42f));
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Success));
            Assert.That(applied, Is.EqualTo(new[] { 10f }));
        }

        [Test] public void NotOwner_NothingIsApplied()
        {
            var applied = new List<float>();
            var r = Wire(new Owner { IsOwner = false }, applied).Dispatch(new SetExampleValueCommand(3f));
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(r.Reason, Is.EqualTo(ExampleValueExecutor.NotOwner));
            Assert.That(applied, Is.Empty);
        }

        [Test] public void RejectedByTheRules_NeverReachesTheExecutor()
        {
            var applied = new List<float>();
            var r = Wire(new Owner { IsOwner = true }, applied).Dispatch(new SetExampleValueCommand(float.NaN));
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(applied, Is.Empty);
        }
    }
}
