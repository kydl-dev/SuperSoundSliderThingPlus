using HarmonyLib;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Infrastructure;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Integrations.Harmony
{
    /// <summary>
    /// The shield dome hum (the "sfx_shieldgenerator_wall_loop" sound) is NOT a ZSFX: AudioMan instantiates it once from m_shieldHumPrefab as a plain
    /// AudioSource whose volume the game never sets (UpdateShieldHum only plays / stops it and moves it to the closest shield point), so the volume the
    /// source had when first seen is remembered and the scaled value is written from it each call (also restores it at factor 1). It follows the
    /// shield generator slider.
    /// </summary>
    [HarmonyPatch(typeof(AudioMan), "UpdateShieldHum")]
    internal static class ShieldHumPatch
    {
        private static AudioSource _tracked;
        private static float _baseVolume, _applied = -1f;
        private static string _clip = "none";

        /// <summary>Tests: what the patch tracks. Expected outcome: false until the patch has seen the hum source once.</summary>
        internal static bool TryGetLast(out float baseVolume, out float applied, out string clip)
        {
            baseVolume = _baseVolume;
            applied = _applied;
            clip = _clip;
            return _tracked != null;
        }

        [HarmonyPostfix]
        private static void Postfix(AudioSource ___m_shieldHumSource)
        {
            if (___m_shieldHumSource == null) return;
            if (!ReferenceEquals(___m_shieldHumSource, _tracked))
            {
                _tracked = ___m_shieldHumSource;
                _baseVolume = ___m_shieldHumSource.volume;
                _clip = ___m_shieldHumSource.clip != null ? ___m_shieldHumSource.clip.name : "no clip";
                ModLog.Verbose("ShieldHumPatch: tracking the shield hum source '" + ___m_shieldHumSource.gameObject.name + "' clip '" + _clip + "', base volume " + _baseVolume.ToString("F3"));
            }
            float factor = SoundLevels.Factor(SoundCategory.ShieldGenerator);
            _applied = _baseVolume * factor;
            ___m_shieldHumSource.volume = _applied;
        }
    }
}
