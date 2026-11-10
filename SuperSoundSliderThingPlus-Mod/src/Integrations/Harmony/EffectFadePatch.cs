using System.Runtime.CompilerServices;
using HarmonyLib;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Infrastructure;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Integrations.Harmony
{
    /// <summary>
    /// A portal's "target found" hum is not a ZSFX: an EffectFade sets its AudioSource volume to intensity x base volume on every Update
    /// (verified in the IL, 2026-11-10), so multiplying afterwards cannot compound. Only fades that belong to a portal are touched (found once
    /// through the parent chain, then remembered); every other EffectFade is left alone.
    /// </summary>
    [HarmonyPatch(typeof(EffectFade), "Update")]
    internal static class EffectFadePatch
    {
        private sealed class Owner
        {
            internal bool IsPortal;
        }

        private static readonly ConditionalWeakTable<EffectFade, Owner> Owners = new ConditionalWeakTable<EffectFade, Owner>();
        private static readonly ConditionalWeakTable<EffectFade, Owner>.CreateValueCallback Find = FindOwner;

        [HarmonyPostfix]
        private static void Postfix(EffectFade __instance, AudioSource ___m_audioSource)
        {
            if (___m_audioSource == null) return;
            if (!Owners.GetValue(__instance, Find).IsPortal) return;
            float factor = SoundLevels.Factor(SoundCategory.Portal);
            if (factor < 1f) ___m_audioSource.volume *= factor;
        }

        private static Owner FindOwner(EffectFade fade)
        {
            bool portal = fade.GetComponentInParent<TeleportWorld>() != null;
            if (portal) ModLog.Verbose("EffectFadePatch: '" + fade.gameObject.name + "' belongs to a portal -> " + SoundCategory.Portal);
            return new Owner { IsPortal = portal };
        }
    }
}
