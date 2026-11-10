using System.Runtime.CompilerServices;
using HarmonyLib;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Infrastructure;
using SuperSoundSliderThingPlus.Integrations.Valheim;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Integrations.Harmony
{
    /// <summary>
    /// Scales every sound effect by its category. ZSFX.CustomUpdate sets the AudioSource volume to an absolute value on every call
    /// (verified in the IL, 2026-11-10), so multiplying afterwards cannot compound. A sound is classified once, the first time it is seen,
    /// and recorded for the in-game tests. Inert while the sound feature is off (SoundLevels returns 1).
    /// </summary>
    [HarmonyPatch(typeof(ZSFX), nameof(ZSFX.CustomUpdate))]
    internal static class ZsfxCustomUpdatePatch
    {
        private sealed class SoundEntry
        {
            internal SoundCategory Category;
        }

        private static readonly ConditionalWeakTable<ZSFX, SoundEntry> Entries = new ConditionalWeakTable<ZSFX, SoundEntry>();
        private static readonly ConditionalWeakTable<ZSFX, SoundEntry>.CreateValueCallback Classify = ClassifyNew;

        [HarmonyPostfix]
        private static void Postfix(ZSFX __instance, AudioSource ___m_audioSource)
        {
            if (___m_audioSource == null) return;
            SoundEntry entry = Entries.GetValue(__instance, Classify);
            float factor = SoundLevels.Factor(entry.Category) * LoudnessEqualizer.GainFor(___m_audioSource.clip);
            if (factor != 1f) ___m_audioSource.volume *= factor;
        }

        private static SoundEntry ClassifyNew(ZSFX sfx)
        {
            string sound = sfx.gameObject.name;
            string owner = SoundOwner.NameFor(sfx);
            SoundCategory category = SoundClassifier.Classify(sound, owner);
            bool first = SoundRecorder.Record(sound, owner, category);
            if (first) ModLog.Verbose("ZsfxCustomUpdatePatch: new sound '" + sound + "' owner '" + owner + "' -> " + category);
            return new SoundEntry { Category = category };
        }
    }
}
