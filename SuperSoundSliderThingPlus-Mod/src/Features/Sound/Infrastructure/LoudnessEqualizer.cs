using KydlCore.Common;
using SuperSoundSliderThingPlus.Configuration;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Integrations.Valheim;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Features.Sound.Infrastructure
{
    /// <summary>
    /// The loudness-matching switch board (D-005 step 2): while the feature runs and "Equalize" is on, a sound's clip is measured once and
    /// scaled toward the target loudness. Read at call time (PROJECT_RULES 17), so the toggle and the dev tunables apply at once.
    /// Only ZSFX sounds are matched; the ambient / wind / shield-hum sources are single looping sources the game balances itself.
    /// </summary>
    internal static class LoudnessEqualizer
    {
        /// <summary>Expected outcome: a gain (1 = leave alone). Never throws; anything unusable (off, no clip, unreadable clip, bad limits) is 1.</summary>
        internal static float GainFor(AudioClip clip)
        {
            SoundFeature feature = SoundFeature.Instance;
            if (feature == null || !feature.Running || ModSettings.EqualizeLoudness == null || !ModSettings.EqualizeLoudness.Value) return 1f;
            float rms = ClipMeter.RmsOf(clip, ModSettings.MeasureSeconds.Value);
            if (rms == ClipMeter.Unknown) return 1f;
            var r = LoudnessRules.Gain(rms, ModSettings.TargetRms.Value, ModSettings.MinGain.Value, ModSettings.MaxGain.Value);
            if (!r.IsSuccess)
            {
                ModLog.Warn("LoudnessEqualizer: gain rejected - " + r.Reason + " (the clip is left alone)");
                return 1f;
            }
            return r.Value;
        }
    }
}
