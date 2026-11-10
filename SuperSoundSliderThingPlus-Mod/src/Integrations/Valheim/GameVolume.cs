using KydlCore.Common;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Integrations.Valheim
{
    /// <summary>
    /// The player's in-game master volume (Settings > Audio, saved as the float "MasterVolume" in the game's preferences; verified in the IL
    /// 2026-11-10). The game applies it as master x sfx to the mixer (AudioMan.SetSFXVolume) and master x music to MusicMan, which is repeated
    /// here for a live change. Reads and writes the game's own storage; nothing else.
    /// </summary>
    internal static class GameVolume
    {
        internal const string MasterKey = "MasterVolume", SfxKey = "SfxVolume", MusicKey = "MusicVolume";

        /// <summary>Expected outcome: the master volume 0..1 (1 when the player never changed it).</summary>
        internal static float Master() => Mathf.Clamp01(PlatformPrefs.GetFloat(MasterKey, 1f));

        /// <summary>Expected outcome: stores the master volume and applies it to the running game at once (the preferences file is saved by <see cref="Save"/>).</summary>
        internal static bool Write(float master)
        {
            float v = Mathf.Clamp01(master);
            PlatformPrefs.SetFloat(MasterKey, v);
            AudioMan.SetSFXVolume(v * PlatformPrefs.GetFloat(SfxKey, 1f));
            MusicMan.m_masterMusicVolume = v * PlatformPrefs.GetFloat(MusicKey, 1f);
            ModLog.Verbose("GameVolume: master volume set to " + v.ToString("F3") + " (stored and applied)");
            return true;
        }

        /// <summary>Writes the preferences to disk.</summary>
        internal static bool Save()
        {
            PlatformPrefs.Save();
            return true;
        }

        /// <summary>The volume the mixer plays SFX at right now (tests).</summary>
        internal static float Sfx() => AudioMan.GetSFXVolume();
    }
}
