using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Tests.Unit
{
    /// Real portal / shield generator names from the ripped game assets (2026-11-10).
    public class PortalClassifierTests
    {
        [TestCase("sfx_OpenPortal", "", "Portal")]
        [TestCase("sfx_OpenPortal(Clone)", "sfx_OpenPortal(Clone) Player(Clone)", "Portal")]
        [TestCase("sfx_unknown_loop", "fx_portal_connected(Clone)", "Portal")]
        [TestCase("sfx_unknown_loop", "fx_portal_stone_connected(Clone)", "Portal")]
        [TestCase("sfx_unknown", "fx_shieldgenerator_domehit(Clone)", "ShieldGenerator")]
        [TestCase("sfx_shieldgenerator_startup", "", "ShieldGenerator")]
        [TestCase("sfx_shieldgenerator_wall_loop", "", "ShieldGenerator")]
        [TestCase("sfx_shieldgenerator_lowfuel_loop", "", "ShieldGenerator")]
        [TestCase("sfx_shieldgenerator_refuel", "", "ShieldGenerator")]
        [TestCase("sfx_shieldgenerator_shutdown", "", "ShieldGenerator")]
        [TestCase("sfx_footstep", "Player(Clone)", "Effects")]
        public void Classify_ByName(string sound, string owner, string expected)
            => Assert.That(SoundClassifier.Classify(sound, owner).ToString(), Is.EqualTo(expected));
    }
}
