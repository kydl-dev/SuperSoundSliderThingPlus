using System.Collections.Generic;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Sound.Application;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Execution;

namespace SuperSoundSliderThingPlus.Tests.Integration
{
    /// Spec 9 "integration": SetMute command -> dispatcher -> handler -> pipeline -> rules -> executor, with a fake ownership and a recording
    /// action instead of the config. One test per branch of the flow, the owner gate included.
    public class SoundMuteFlowTests
    {
        sealed class Owner : IOwnership { public bool IsOwner { get; set; } public long OwnerId { get { return 1; } } }

        static CommandDispatcher Wire(Owner owner, List<string> applied)
        {
            var d = new CommandDispatcher();
            d.Register(new SetMuteHandler(new MuteExecutor(owner, (slider, level) => applied.Add(slider + "=" + level)), null));
            return d;
        }

        [Test] public void Owner_TheClampedLevelIsAppliedToTheRightSlider()
        {
            var applied = new List<string>();
            var r = Wire(new Owner { IsOwner = true }, applied).Dispatch(new SetMuteCommand(SoundSlider.Weather, 140f));
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Success));
            Assert.That(applied, Is.EqualTo(new[] { "Weather=100" }));
        }

        [Test] public void EverySlider_ReachesTheExecutorWithItsOwnName()
        {
            var applied = new List<string>();
            var d = Wire(new Owner { IsOwner = true }, applied);
            foreach (SoundSlider s in System.Enum.GetValues(typeof(SoundSlider))) d.Dispatch(new SetMuteCommand(s, 10f));
            Assert.That(applied, Has.Count.EqualTo(7));
            Assert.That(applied, Does.Contain("Master=10").And.Contain("ChickenLove=10").And.Contain("Portal=10"));
        }

        [Test] public void NotOwner_NothingIsApplied()
        {
            var applied = new List<string>();
            var r = Wire(new Owner { IsOwner = false }, applied).Dispatch(new SetMuteCommand(SoundSlider.Master, 30f));
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(r.Reason, Is.EqualTo(MuteExecutor.NotOwner));
            Assert.That(applied, Is.Empty);
        }

        [Test] public void RejectedByTheRules_NeverReachesTheExecutor()
        {
            var applied = new List<string>();
            var r = Wire(new Owner { IsOwner = true }, applied).Dispatch(new SetMuteCommand(SoundSlider.Master, float.NaN));
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(applied, Is.Empty);
        }
    }
}
