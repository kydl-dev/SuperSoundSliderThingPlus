using System;

namespace SuperSoundSliderThingPlus.Features.Sound.Domain
{
    /// <summary>
    /// Decides which slider a sound belongs to from two names the game gives us: the sound effect's own name (prefab or clip) and the name
    /// of the creature / object that owns it. Names come from the ripped game assets (2026-11-10); the chicken love sound is the "sfx_love" child of the
    /// "fx_hen_love" effect prefab (ripped assets, user 2026-11-10); the in-game recorder still has to confirm the runtime names. Pure: no Unity / Valheim types.
    /// </summary>
    internal static class SoundClassifier
    {
        private static readonly string[] WeatherPrefabs = { "sfx_thunder", "sfx_mistlands_thunder" };
        private static readonly string[] WeatherClipWords = { "rain", "thunder", "blizzard", "mainwind", "windbreeze", "oneshots_wind" };
        private static readonly char[] OwnerSeparators = { '_', '(', ')', ' ', '-', '.' };
        private const string AmbientClipPrefix = "amb_", ShieldGeneratorWord = "shieldgenerator", PortalWord = "portal", WolfPupOwner = "wolf_cub", LoveWord = "love";
        private const string HenToken = "hen", ChickPrefix = "chick";

        /// <summary>Expected outcome: always a category; <see cref="SoundCategory.Effects"/> when nothing special matches. Null / empty names match nothing.</summary>
        internal static SoundCategory Classify(string soundName, string ownerName)
        {
            string sound = Normalise(soundName);
            string owner = Normalise(ownerName);
            if (sound.Contains(ShieldGeneratorWord) || owner.Contains(ShieldGeneratorWord)) return SoundCategory.ShieldGenerator;
            if (sound.Contains(PortalWord) || owner.Contains(PortalWord)) return SoundCategory.Portal;
            if (owner.Contains(WolfPupOwner)) return SoundCategory.WolfPup;
            if (IsChickenLove(sound, owner)) return SoundCategory.ChickenLove;
            if (IsWeather(sound)) return SoundCategory.Weather;
            return SoundCategory.Effects;
        }

        private static bool IsChickenLove(string sound, string owner)
            => sound.Contains(LoveWord) && IsChickenOwner(owner);

        // owner names are whole-word checked: the hen's love effect is the root "fx_hen_love(Clone)", and "hen" must not match inside other words
        private static bool IsChickenOwner(string owner)
            => Array.Exists(owner.Split(OwnerSeparators, StringSplitOptions.RemoveEmptyEntries),
                token => token == HenToken || token.StartsWith(ChickPrefix, StringComparison.Ordinal));

        private static bool IsWeather(string sound)
            => Array.IndexOf(WeatherPrefabs, sound) >= 0
               || (sound.StartsWith(AmbientClipPrefix, StringComparison.Ordinal) && ContainsAny(sound, WeatherClipWords));

        private static bool ContainsAny(string text, string[] words)
            => Array.Exists(words, word => text.Contains(word));

        // instantiated objects are named "<prefab>(Clone)": the suffix is dropped so a prefab name matches exactly (found in game 2026-11-11: "sfx_thunder(Clone)" was not weather)
        private static string Normalise(string name) => string.IsNullOrEmpty(name) ? string.Empty : name.Replace("(Clone)", string.Empty).Replace("(clone)", string.Empty).Trim().ToLowerInvariant();
    }
}
