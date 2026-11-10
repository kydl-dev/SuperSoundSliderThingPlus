using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Tests.Unit
{
    public class MasterVolumeRulesTests
    {
        [TestCase(1f, 0f)]
        [TestCase(0f, 100f)]
        [TestCase(0.4f, 60f)]
        [TestCase(0.37f, 63f)]
        public void MuteFromVolume_IsTheMirror(float volume, float mute)
            => Assert.That(MasterVolumeRules.MuteFromVolume(volume).Value, Is.EqualTo(mute).Within(0.001f));

        [TestCase(2f, 0f)]
        [TestCase(-1f, 100f)]
        public void MuteFromVolume_ClampsTheVolume(float volume, float mute)
        {
            var r = MasterVolumeRules.MuteFromVolume(volume);
            Assert.That(r.Value, Is.EqualTo(mute));
            Assert.That(r.Reason, Is.EqualTo(MasterVolumeRules.Clamped));
        }

        [TestCase(0f, 1f)]
        [TestCase(100f, 0f)]
        [TestCase(60f, 0.4f)]
        [TestCase(150f, 0f)]
        [TestCase(-5f, 1f)]
        public void VolumeFromMute_IsTheMirror(float mute, float volume)
            => Assert.That(MasterVolumeRules.VolumeFromMute(mute).Value, Is.EqualTo(volume).Within(0.0001f));

        [Test] public void NaNAndInfinity_AreRejected()
        {
            Assert.That(MasterVolumeRules.MuteFromVolume(float.NaN).IsSuccess, Is.False);
            Assert.That(MasterVolumeRules.MuteFromVolume(float.PositiveInfinity).IsSuccess, Is.False);
            Assert.That(MasterVolumeRules.VolumeFromMute(float.NaN).IsSuccess, Is.False);
            Assert.That(MasterVolumeRules.VolumeFromMute(float.NegativeInfinity).IsSuccess, Is.False);
        }

        [Test] public void RoundTrip_IsTheSameSliderPosition()
        {
            for (int mute = 0; mute <= 100; mute++)
            {
                float volume = MasterVolumeRules.VolumeFromMute(mute).Value;
                Assert.That(MasterVolumeRules.SameMute(MasterVolumeRules.MuteFromVolume(volume).Value, mute), Is.True, "mute " + mute);
            }
        }

        [Test] public void SameMute_TellsDifferentPositionsApart()
        {
            Assert.That(MasterVolumeRules.SameMute(40f, 40.01f), Is.True);
            Assert.That(MasterVolumeRules.SameMute(40f, 41f), Is.False);
        }
    }
}
