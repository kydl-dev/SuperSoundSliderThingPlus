using KydlCore.Common;
using SuperSoundSliderThingPlus.Configuration;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Integrations.Valheim;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Features.Sound.Infrastructure
{
    /// <summary>
    /// The loudness-matching switch board (D-005 step 2, D-015): while the feature runs and "EqualizeLoudness" is on, each clip's level is
    /// learned while it plays and the clip is scaled toward the target. Read at call time (PROJECT_RULES 17), so the toggle and the dev tunables apply at once.
    /// A clip that has not been heard long enough yet plays at the game's own level. Only ZSFX sounds are matched; the ambient / wind / shield-hum
    /// sources are single looping sources the game balances itself.
    /// </summary>
    internal static class LoudnessEqualizer
    {
        /// <summary>True while matching is switched on and the feature runs.</summary>
        internal static bool Active()
        {
            SoundFeature feature = SoundFeature.Instance;
            return feature != null && feature.Running && ModSettings.EqualizeLoudness != null && ModSettings.EqualizeLoudness.Value;
        }

        /// <summary>Expected outcome: a gain (1 = leave alone). Never throws; anything unusable (off, no clip, not learned yet, bad limits) is 1.</summary>
        internal static float GainFor(AudioClip clip)
        {
            if (!Active()) return 1f;
            float rms = ClipMeter.RmsOf(clip);
            if (rms <= ClipMeter.Unknown) return 1f;
            var r = LoudnessRules.Gain(rms, ModSettings.TargetRms.Value, ModSettings.MinGain.Value, ModSettings.MaxGain.Value);
            if (!r.IsSuccess)
            {
                ModLog.Warn("LoudnessEqualizer: gain rejected - " + r.Reason + " (the clip is left alone)");
                return 1f;
            }
            return r.Value;
        }

        /// <summary>Lets the meter hear one block of a playing source. `playedVolume` is the volume the source had when the heard block was made.</summary>
        internal static bool Observe(AudioSource source, float playedVolume) => Active() && ClipMeter.Observe(source, playedVolume);
    }
}
