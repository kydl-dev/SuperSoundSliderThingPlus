using System.Collections.Generic;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Configuration;
using UnityEngine;

// kydl:read-only-observer - this file only reads where characters are; it never moves, commands or spawns one (guard exemption, user 2026-11-10)
namespace SuperSoundSliderThingPlus.Integrations.Valheim
{
    /// <summary>
    /// Finds who a sound belongs to. The game spawns most creature sounds as their own root objects (e.g. "sfx_wolf_alerted(Clone)") at the
    /// creature's position, and a wolf pup reuses the adult wolf's sound prefabs - so the only hint is the creature standing at the sound.
    /// The answer is "&lt;root name&gt; &lt;nearest character within the radius, if any&gt;"; with a pup next to an adult it can pick the wrong one
    /// (the in-game recorder shows how often). Reads the scene only, never changes it.
    /// </summary>
    internal static class SoundOwner
    {
        /// <summary>Expected outcome: a non-empty text with the root object's name, followed by the nearest character's name when one is within the radius.</summary>
        internal static string NameFor(ZSFX sfx)
        {
            Transform root = sfx.transform.root;
            string rootName = root != null ? root.gameObject.name : sfx.gameObject.name;
            Character nearest = Nearest(sfx.transform.position, out float distance);
            string owner = nearest != null ? rootName + " " + nearest.gameObject.name : rootName;
            ModLog.Verbose("SoundOwner: " + sfx.gameObject.name + " root '" + rootName + "' nearest character '"
                + (nearest != null ? nearest.gameObject.name : "none") + "' at " + (nearest != null ? distance.ToString("F2") : "-") + " m (radius "
                + ModSettings.OwnerRadius.Value.ToString("F2") + " m)");
            return owner;
        }

        private static Character Nearest(Vector3 position, out float distance)
        {
            float radius = ModSettings.OwnerRadius.Value;
            float best = radius * radius;
            Character nearest = null;
            List<Character> all = Character.GetAllCharacters();
            for (int i = 0; i < all.Count; i++)
            {
                Character c = all[i];
                if (c == null) continue;
                float squared = (c.transform.position - position).sqrMagnitude;
                if (squared >= best) continue;
                best = squared;
                nearest = c;
            }
            distance = nearest != null ? Mathf.Sqrt(best) : -1f;
            return nearest;
        }
    }
}
