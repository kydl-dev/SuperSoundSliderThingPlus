using HarmonyLib;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Infrastructure;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Integrations.Harmony
{
    /// <summary>
    /// The shield dome hum is a plain AudioSource whose volume the game never sets (UpdateShieldHum only plays / stops it), so the volume the
    /// source had when first seen is remembered and the scaled value is written from it each call (also restores it at factor 1).
    /// </summary>
    [HarmonyPatch(typeof(AudioMan), "UpdateShieldHum")]
    internal static class ShieldHumPatch
    {
        private static AudioSource _tracked;
        private static float _baseVolume;

        [HarmonyPostfix]
        private static void Postfix(AudioSource ___m_shieldHumSource)
        {
            if (___m_shieldHumSource == null) return;
            if (!ReferenceEquals(___m_shieldHumSource, _tracked))
            {
                _tracked = ___m_shieldHumSource;
                _baseVolume = ___m_shieldHumSource.volume;
                ModLog.Verbose("ShieldHumPatch: tracking the shield hum source, base volume " + _baseVolume.ToString("F3"));
            }
            float factor = SoundLevels.Factor(SoundCategory.ShieldGenerator);
            ___m_shieldHumSource.volume = _baseVolume * factor;
        }
    }
}
