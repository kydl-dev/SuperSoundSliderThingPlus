using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Tests.Unit
{
    /// Real names from the ripped game assets (2026-11-10), including the look-alikes that must NOT be weather.
    public class SoundClassifierTests
    {
        [TestCase("sfx_shieldgenerator_hit", "ShieldGenerator", "ShieldGenerator")]
        [TestCase("sfx_shieldgenerator_powered_loop", "", "ShieldGenerator")]
        [TestCase("sfx_wolf_alerted", "Wolf_cub(Clone)", "WolfPup")]
        [TestCase("sfx_wolf_alerted", "Wolf(Clone)", "Effects")]
        [TestCase("sfx_wolf_alerted(Clone)", "sfx_wolf_alerted(Clone) Wolf_cub(Clone)", "WolfPup")]
        [TestCase("sfx_wolf_alerted(Clone)", "sfx_wolf_alerted(Clone) Wolf(Clone)", "Effects")]
        [TestCase("sfx_thunder", "", "Weather")]
        [TestCase("sfx_mistlands_thunder", null, "Weather")]
        [TestCase("Amb_LightRain_S_Loop", "", "Weather")]
        [TestCase("Amb_MainWind_S_Loop", "", "Weather")]
        [TestCase("Amb_ThunderClap_S_03", "", "Weather")]
        [TestCase("Amb_WindBreeze_S_02", "", "Weather")]
        [TestCase("Amb_Blizzard_OneShots_Wind_07", "", "Weather")]
        [TestCase("sfx_windows_close", "", "Effects")]
        [TestCase("sfx_window_open", "", "Effects")]
        [TestCase("Amb_WindingTunnel_Main_01", "", "Effects")]
        [TestCase("sfx_frozenking_frozentwirl_wind", "", "Effects")]
        [TestCase("sfx_chicken_love", "Chicken(Clone)", "ChickenLove")]
        [TestCase("sfx_love", "fx_hen_love(Clone)", "ChickenLove")]
        [TestCase("sfx_love", "Hen(Clone)", "ChickenLove")]
        [TestCase("sfx_love", "fx_troll_love(Clone)", "Effects")]
        [TestCase("sfx_love", "", "Effects")]
        [TestCase("sfx_chicken_idle_vocal", "Chicken(Clone)", "Effects")]
        [TestCase("sfx_boar_love", "Boar(Clone)", "Effects")]
        [TestCase("sfx_sword_hit", "Player", "Effects")]
        [TestCase(null, null, "Effects")]
        [TestCase("", "", "Effects")]
        public void Classify_PutsEachSoundInItsSlider(string sound, string owner, string expected)
            => Assert.That(SoundClassifier.Classify(sound, owner).ToString(), Is.EqualTo(expected));

        [Test]
        public void Classify_IgnoresCaseAndSurroundingSpaces()
            => Assert.That(SoundClassifier.Classify("  SFX_SHIELDGENERATOR_HIT ", ""), Is.EqualTo(SoundCategory.ShieldGenerator));
    }
}
