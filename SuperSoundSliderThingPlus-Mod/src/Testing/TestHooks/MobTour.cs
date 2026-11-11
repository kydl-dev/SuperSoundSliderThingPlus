using System.Collections;
using System.Collections.Generic;
using System.Text;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Features.Sound.Infrastructure;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Testing.TestHooks
{
    /// <summary>
    /// TEST ONLY: spawns every creature prefab of the game one at a time in front of the (god mode) player, lets it act for a number of seconds so it
    /// can engage and attack, removes it, and logs which (sound | owner) pairs the sound patch saw because of it. Needs the lab's CreaturesHostile on.
    /// </summary>
    internal sealed class MobTour : MonoBehaviour
    {
        private static MobTour _current;
        internal string Summary { get; private set; } = "not started";
        internal bool Done { get; private set; }
        private float _seconds;
        private string _filter;

        /// <summary>Expected outcome: true when the tour started, false without a player or scene.</summary>
        internal static bool Begin(float seconds, string filter)
        {
            if (Player.m_localPlayer == null || ZNetScene.instance == null) return false;
            if (_current != null) Destroy(_current.gameObject);
            var host = new GameObject("SSST_MobTour");
            _current = host.AddComponent<MobTour>();
            _current._seconds = seconds;
            _current._filter = filter ?? "";
            _current.StartCoroutine(_current.Run());
            return true;
        }

        internal static string State() => _current == null ? "not started" : _current.Summary;
        internal static bool IsDone() => _current != null && _current.Done;

        private static HashSet<string> Pairs()
        {
            return new HashSet<string>(SoundRecorder.List().Split(new[] { "; " }, System.StringSplitOptions.RemoveEmptyEntries));
        }

        private const float ClearRadius = 150f, NatureRadius = 180f;

        /// <summary>Removes every creature (not the player) within 150 m of the player, so nothing else makes sounds or fights the tour creature. Returns how many were removed.</summary>
        private static int ClearArea()
        {
            Player player = Player.m_localPlayer;
            if (player == null || ZNetScene.instance == null) return 0;
            var victims = new List<GameObject>();
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c is Player) continue;
                if (Vector3.Distance(c.transform.position, player.transform.position) <= ClearRadius) victims.Add(c.gameObject);
            }
            foreach (GameObject go in victims)
            {
                if (go != null) ZNetScene.instance.Destroy(go);
            }
            return victims.Count;
        }

        /// <summary>Removes every tree, stump, log, bush, boulder and pickable within 180 m of the player so creatures have a clear line of sight. Returns how many were removed.</summary>
        private static int ClearNature()
        {
            Player player = Player.m_localPlayer;
            if (player == null || ZNetScene.instance == null) return 0;
            var victims = new List<GameObject>();
            foreach (ZNetView view in FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
            {
                if (view == null) continue;
                GameObject go = view.gameObject;
                if (Vector3.Distance(go.transform.position, player.transform.position) > NatureRadius) continue;
                if (go.GetComponent<TreeBase>() != null || go.GetComponent<TreeLog>() != null || go.GetComponent<MineRock>() != null || go.GetComponent<MineRock5>() != null || go.GetComponent<Pickable>() != null)
                    victims.Add(go);
            }
            foreach (GameObject go in victims)
            {
                if (go != null) ZNetScene.instance.Destroy(go);
            }
            return victims.Count;
        }

        private IEnumerator Run()
        {
            int nature = ClearNature();
            ModLog.Info("MobTour: removed " + nature + " trees, logs, rocks and pickables within " + NatureRadius + " m of the player");
            yield return null;
            int cleared = ClearArea();
            ModLog.Info("MobTour: cleared " + cleared + " creatures within " + ClearRadius + " m of the player");
            yield return null;
            // optional restart point: the environment variable SSST_TOUR_FROM (set by scripts\Run-MobTour.ps1 -From) skips every creature before that name
            string from = System.Environment.GetEnvironmentVariable("SSST_TOUR_FROM") ?? "";
            string until = System.Environment.GetEnvironmentVariable("SSST_TOUR_TO") ?? "";   // exclusive end point (scripts\Run-MobTour.ps1 -To)
            var names = new List<string>();
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null || prefab.GetComponent<Character>() == null || prefab.GetComponent<Player>() != null) continue;
                if (_filter.Length > 0 && !prefab.name.Contains(_filter)) continue;
                if (from.Length > 0 && string.CompareOrdinal(prefab.name, from) < 0) continue;
                if (until.Length > 0 && string.CompareOrdinal(prefab.name, until) >= 0) continue;
                names.Add(prefab.name);
            }
            names.Sort();
            ModLog.Info("MobTour: " + names.Count + " creatures: " + string.Join(" ", names.ToArray()));
            int withNew = 0, totalNew = 0;
            var quiet = new StringBuilder();
            Vector3 start = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : Vector3.zero;
            foreach (string name in names)
            {
                Player player = Player.m_localPlayer;
                GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
                if (player == null || prefab == null) break;
                int extra = ClearArea();
                if (extra > 0) ModLog.Info("MobTour: removed " + extra + " other creatures before " + name);
                yield return null;
                HashSet<string> before = Pairs();
                Vector3 position = player.transform.position + player.transform.forward * 5f;
                GameObject mob = Instantiate(prefab, position, Quaternion.identity);
                // the creature starts 5 m away (it must notice the player); then, without respawning it, the player steps back so that it is 20 m and then 45 m
                // away from the creature (ranged attacks, meteors, arrows, spells), 1/3 of the time each
                Vector3 back = -player.transform.forward;
                float waited = 0f;
                int phase = 0;
                while (waited < _seconds && mob != null)
                {
                    waited += Time.deltaTime;
                    int want = waited < _seconds / 3f ? 0 : (waited < _seconds * 2f / 3f ? 1 : 2);
                    if (want != phase && Player.m_localPlayer != null)
                    {
                        phase = want;
                        Vector3 to = mob.transform.position + back * (phase == 1 ? 20f : 45f);
                        to.y = ZoneSystem.instance.GetGroundHeight(to) + 0.3f;
                        Player p = Player.m_localPlayer;
                        Rigidbody body = p.GetComponent<Rigidbody>();
                        if (body != null) { body.linearVelocity = Vector3.zero; body.position = to; }
                        p.transform.position = to;
                        if (ZNet.instance != null) ZNet.instance.SetReferencePosition(to);
                    }
                    yield return null;
                }
                bool died = mob == null;
                if (Player.m_localPlayer != null)
                {
                    Player p0 = Player.m_localPlayer;
                    Rigidbody b0 = p0.GetComponent<Rigidbody>();
                    if (b0 != null) { b0.linearVelocity = Vector3.zero; b0.position = start; }
                    p0.transform.position = start;
                    if (ZNet.instance != null) ZNet.instance.SetReferencePosition(start);
                }
                if (mob != null && ZNetScene.instance != null) ZNetScene.instance.Destroy(mob);
                HashSet<string> after = Pairs();
                after.ExceptWith(before);
                if (after.Count > 0) { withNew++; totalNew += after.Count; } else quiet.Append(name).Append(' ');
                ModLog.Info("MobTour: " + name + (died ? " (died early)" : "") + " -> " + after.Count + " new sound pairs: " + string.Join("; ", new List<string>(after).ToArray()));
                yield return null;
            }
            Summary = "tour of " + names.Count + " creatures: " + withNew + " added sounds (" + totalNew + " new pairs), " + SoundRecorder.Distinct + " pairs in total (" + SoundRecorder.Summary() + "); added nothing new: " + quiet;
            ModLog.Info("MobTour: DONE " + Summary);
            Done = true;
        }
    }
}
