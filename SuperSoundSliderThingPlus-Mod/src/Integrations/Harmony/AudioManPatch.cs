using HarmonyLib;
using SuperSoundSliderThingPlus.Features.Sound.Infrastructure;

namespace SuperSoundSliderThingPlus.Integrations.Harmony
{
    /// <summary>
    /// Keeps the master mute slider and the game's master volume the same control: at game start (AudioMan.Start has just applied the saved
    /// volume) and a few times a second afterwards, the slider follows the game. The other direction is the slider's own change event.
    /// AudioMan exists in the main menu too, so this also works before a world is loaded.
    /// </summary>
    [HarmonyPatch]   // class-level attribute: Harmony's PatchAll only looks at classes that carry one, the methods below name their own targets
    internal static class AudioManPatch
    {
        [HarmonyPatch(typeof(AudioMan), "Start")]
        [HarmonyPostfix]
        private static void Started()
        {
            SoundFeature feature = SoundFeature.Instance;
            if (feature != null) feature.Pull();
        }

        [HarmonyPatch(typeof(AudioMan), "Update")]
        [HarmonyPostfix]
        private static void Updated()
        {
            SoundFeature feature = SoundFeature.Instance;
            if (feature != null) feature.Tick();
        }
    }
}
