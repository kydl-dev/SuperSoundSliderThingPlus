using KydlCore.Common;
using SuperSoundSliderThingPlus.Configuration;
using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Features.Sound.Infrastructure
{
    /// <summary>
    /// The one place where the player's slider values meet the pure maths: reads the config at call time (PROJECT_RULES 17: no caching, so a
    /// change applies at once) and asks <see cref="SoundMixRules"/> for the loudness factor of a category. Inert (factor 1) while the sound
    /// feature is not running (mod switched off).
    /// </summary>
    internal static class SoundLevels
    {
        /// <summary>Expected outcome: a factor 0..1 (1 = the sound is left alone). Never throws; a rejected level (NaN) leaves the sound alone.</summary>
        internal static float Factor(SoundCategory category)
        {
            SoundFeature feature = SoundFeature.Instance;
            if (feature == null || !feature.Running || ModSettings.MasterMute == null) return 1f;
            var r = SoundMixRules.Factor(ModSettings.MasterMute.Value, MuteOf(category));
            if (!r.IsSuccess) ModLog.Warn("SoundLevels: " + category + " factor rejected - " + r.Reason + " (the sound is left alone)");
            return r.IsSuccess ? r.Value : 1f;
        }

        /// <summary>The mute level (percent) of one category slider; 0 while the config is not bound yet.</summary>
        internal static float MuteOf(SoundCategory category)
        {
            switch (category)
            {
                case SoundCategory.Weather: return Read(ModSettings.WeatherMute);
                case SoundCategory.WolfPup: return Read(ModSettings.WolfPupMute);
                case SoundCategory.ShieldGenerator: return Read(ModSettings.ShieldGeneratorMute);
                case SoundCategory.ChickenLove: return Read(ModSettings.ChickenLoveMute);
                default: return Read(ModSettings.EffectsMute);
            }
        }

        private static float Read(BepInEx.Configuration.ConfigEntry<float> entry) => entry == null ? SoundMixRules.DefaultMute : entry.Value;
    }
}
