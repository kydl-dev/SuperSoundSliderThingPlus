using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Tests.Unit
{
    /// Found in game 2026-11-11: instantiated sound objects are named "<prefab>(Clone)" and must classify like the prefab.
    public class CloneNameTests
    {
        [TestCase("sfx_thunder(Clone)", "sfx_thunder(Clone)", "Weather")]
        [TestCase("sfx_mistlands_thunder(Clone)", "", "Weather")]
        [TestCase("sfx_OpenPortal(Clone)", "sfx_OpenPortal(Clone)", "Portal")]
        [TestCase("sfx_footstep(Clone)", "Player(Clone)", "Effects")]
        [TestCase("sfx_wolf_alerted(Clone)", "sfx_wolf_alerted(Clone) Wolf_cub(Clone)", "WolfPup")]
        public void CloneSuffix_DoesNotChangeTheCategory(string sound, string owner, string expected)
            => Assert.That(SoundClassifier.Classify(sound, owner).ToString(), Is.EqualTo(expected));
    }
}
