using System.Collections.Generic;
using System.Text;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Features.Sound.Infrastructure;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Testing.TestHooks
{
    /// <summary>
    /// TEST ONLY: spawns real creatures around the player so the sounds the all-sounds sweep could not make (creature footsteps, attacks, hits) are
    /// played by the game itself and handled by the sound patch, then reports which (sound | owner) pairs the patch saw and in which category.
    /// </summary>
    internal static class MobTestHooks
    {
        private static readonly List<GameObject> Spawned = new List<GameObject>();

        /// <summary>Turns the player's ghost mode (monsters ignore a ghost) on or off.</summary>
        [TestHook("SuperSoundSliderThingPlus.mob.ghost")]
        static string Ghost(bool on)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return "Failed: no player";
            player.SetGhostMode(on);
            return "ghost " + on;
        }

        /// <summary>Spawns one of each listed creature prefab (comma separated) in a ring 6 to 12 m around the player. Returns what was spawned and what was not found.</summary>
        [TestHook("SuperSoundSliderThingPlus.mob.spawn")]
        static string Spawn(string prefabs)
        {
            Player player = Player.m_localPlayer;
            if (player == null || ZNetScene.instance == null) return "Failed: no player or scene";
            var done = new StringBuilder();
            var missing = new StringBuilder();
            string[] names = prefabs.Split(',');
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i].Trim();
                GameObject prefab = ZNetScene.instance.GetPrefab(name);
                if (prefab == null) { missing.Append(name).Append(' '); continue; }
                float angle = (i + 0.5f) / names.Length * Mathf.PI * 2f;
                Vector3 position = player.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (6f + (i % 3) * 3f);
                Spawned.Add(Object.Instantiate(prefab, position, Quaternion.identity));
                done.Append(name).Append(' ');
            }
            string text = "spawned: " + done + "| missing: " + missing;
            ModLog.Info("MobTestHooks: " + text);
            return text;
        }

        /// <summary>Logs every (sound | owner) pair the patch has seen in a category other than the default list and returns how many distinct pairs it has seen so far.</summary>
        [TestHook("SuperSoundSliderThingPlus.mob.report")]
        static int Report()
        {
            ModLog.Info("MobTestHooks: REPORT distinct pairs " + SoundRecorder.Distinct + " | " + SoundRecorder.Summary());
            ModLog.Info("MobTestHooks: PAIRS " + SoundRecorder.List());
            return SoundRecorder.Distinct;
        }

        /// <summary>Forgets the sounds seen so far (call before spawning so the report shows only what the creatures made).</summary>
        [TestHook("SuperSoundSliderThingPlus.mob.forget")]
        static bool Forget() { return SoundRecorder.Clear(); }

        /// <summary>Removes every spawned creature.</summary>
        [TestHook("SuperSoundSliderThingPlus.mob.clear")]
        static bool Clear()
        {
            foreach (GameObject go in Spawned)
            {
                if (go != null && ZNetScene.instance != null) ZNetScene.instance.Destroy(go);
            }
            Spawned.Clear();
            return Spawned.Count == 0;
        }
    }
}
