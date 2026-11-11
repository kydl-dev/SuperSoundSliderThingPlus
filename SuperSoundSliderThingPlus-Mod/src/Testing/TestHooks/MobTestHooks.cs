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

        /// <summary>Puts the player `metres` from the lab start (the spawn house), on the same side as the arena spot, on flat dry ground (tries 16 directions, flattest wins). Zones need a few seconds to load afterwards.</summary>
        [TestHook("SuperSoundSliderThingPlus.mob.moveAway")]
        static string MoveAway(double metres)
        {
            Player player = Player.m_localPlayer;
            if (player == null || ZoneSystem.instance == null) return "Failed: no player or zones";
            Vector3 start = System.AppDomain.CurrentDomain.GetData("Kydl.TestArena.Start") is Vector3 s ? s : player.transform.position;
            Vector3 flat = player.transform.position - start;
            flat.y = 0f;
            float baseAngle = flat.sqrMagnitude > 1f ? Mathf.Atan2(flat.z, flat.x) : 0f;
            Vector3 best = Vector3.zero;
            float bestSpread = float.MaxValue;
            for (int i = 0; i < 16; i++)
            {
                float angle = baseAngle + i * Mathf.PI * 2f / 16f;
                Vector3 at = start + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (float)metres;
                float lo = float.MaxValue, hi = float.MinValue;
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        float h = ZoneSystem.instance.GetGroundHeight(at + new Vector3(dx * 10f, 0f, dz * 10f));
                        lo = Mathf.Min(lo, h);
                        hi = Mathf.Max(hi, h);
                    }
                }
                if (lo < 31f) continue;   // water (sea level is 30)
                float spread = hi - lo;
                if (spread < bestSpread) { bestSpread = spread; best = at; }
            }
            if (bestSpread == float.MaxValue) return "Failed: no dry ground " + metres + " m from the start";
            best.y = ZoneSystem.instance.GetGroundHeight(best) + 0.3f;
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null) { body.linearVelocity = Vector3.zero; body.position = best; }
            player.transform.position = best;
            player.transform.rotation = Quaternion.identity;
            if (ZNet.instance != null) ZNet.instance.SetReferencePosition(best);
            string text = "moved to " + best.ToString("F0") + ", " + Vector3.Distance(new Vector3(best.x, 0f, best.z), new Vector3(start.x, 0f, start.z)).ToString("F0") + " m from the lab start, height spread " + bestSpread.ToString("F1") + " m";
            ModLog.Info("MobTestHooks: " + text);
            return text;
        }

        /// <summary>Makes sure the player is NOT flying (creatures cannot reach a flying player, so they never attack). Reports the state before and after; god mode is left alone.</summary>
        [TestHook("SuperSoundSliderThingPlus.mob.noFly")]
        static string NoFly()
        {
            Player player = Player.m_localPlayer;
            if (player == null) return "Failed: no player";
            const System.Reflection.BindingFlags all = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            System.Reflection.FieldInfo field = typeof(Player).GetField("m_debugFly", all);
            if (field == null) return "Failed: Player has no m_debugFly field";
            bool before = (bool)field.GetValue(player);
            if (before)
            {
                System.Reflection.MethodInfo toggle = typeof(Player).GetMethod("ToggleDebugFly", all);
                if (toggle == null) return "Failed: Player has no ToggleDebugFly";
                toggle.Invoke(player, null);
            }
            bool after = (bool)field.GetValue(player);
            string text = "fly before " + before + " after " + after + ", god mode " + player.InGodMode() + ", ghost " + player.InGhostMode() + ", flying " + player.IsFlying();
            ModLog.Info("MobTestHooks: " + text);
            return text;
        }

        /// <summary>Starts the creature tour: every creature prefab, one at a time, `seconds` each (optional name filter).</summary>
        [TestHook("SuperSoundSliderThingPlus.mob.tourStart")]
        static bool TourStart(double seconds, string filter) { return MobTour.Begin((float)seconds, filter); }

        /// <summary>Has the creature tour finished?</summary>
        [TestHook("SuperSoundSliderThingPlus.mob.tourDone")]
        static bool TourDone() { return MobTour.IsDone(); }

        /// <summary>The tour's result line.</summary>
        [TestHook("SuperSoundSliderThingPlus.mob.tourSummary")]
        static string TourSummary() { return MobTour.State(); }

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
