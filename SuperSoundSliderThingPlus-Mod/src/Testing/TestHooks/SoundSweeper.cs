using System.Collections;
using System.Collections.Generic;
using System.Text;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Infrastructure;
using SuperSoundSliderThingPlus.Integrations.Harmony;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Testing.TestHooks
{
    /// <summary>
    /// TEST ONLY: plays every sound effect prefab of the game once (a few frames each), reads what the mod and the mixer do to it and logs one
    /// "SWEEP" line per sound. Answers: which sounds follow the master (their AudioSource goes through a mixer group, so the game's master volume reaches
    /// them) and which category the mod puts each one in. Reads and instantiates sound prefabs next to the player; changes no mod state.
    /// </summary>
    internal sealed class SoundSweeper : MonoBehaviour
    {
        private const int FramesPerSound = 3;
        private const float LateSeconds = 0.5f;
        private static SoundSweeper _current;

        internal bool Done { get; private set; }
        internal string Summary { get; private set; } = "not started";

        /// <summary>Expected outcome: a sweep is running (true), or the player is missing (false).</summary>
        internal static bool Begin()
        {
            if (Player.m_localPlayer == null) return false;
            if (_current != null) Destroy(_current.gameObject);
            var host = new GameObject("SSST_SoundSweep");
            _current = host.AddComponent<SoundSweeper>();
            _current.StartCoroutine(_current.Run());
            return true;
        }

        internal static string State() => _current == null ? "not started" : _current.Summary;

        internal static bool IsDone() => _current != null && _current.Done;

        private IEnumerator Run()
        {
            var prefabs = new List<GameObject>();
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!go.scene.IsValid() && go.GetComponent<ZSFX>() != null && go.GetComponent<AudioSource>() != null) prefabs.Add(go);
            }
            prefabs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var groups = new SortedDictionary<string, int>();
            var categories = new SortedDictionary<string, int>();
            int played = 0, notPlaying = 0, noGroup = 0, ratioWrong = 0, late = 0;
            Vector3 position = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 6f;
            ModLog.Info("SWEEP start: " + prefabs.Count + " sound prefabs, game master volume " + SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Master().ToString("F2"));
            foreach (GameObject prefab in prefabs)
            {
                GameObject instance = Instantiate(prefab, position, Quaternion.identity);
                var zsfx = instance.GetComponent<ZSFX>();
                var source = instance.GetComponent<AudioSource>();
                if (zsfx != null && !zsfx.m_playOnAwake) zsfx.Play();
                for (int i = 0; i < FramesPerSound; i++) yield return null;
                if (instance == null) continue;
                // sounds with a start delay or a condition need longer: wait up to half a second until the source plays and the patch has handled it
                float waited = 0f;
                SoundCategory seenCategory;
                float seenRaw, seenScaled;
                while (waited < LateSeconds && (!source.isPlaying || !ZsfxCustomUpdatePatch.TryGetLast(zsfx, out seenCategory, out seenRaw, out seenScaled)))
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                    if (instance == null) break;
                }
                if (instance == null) continue;
                if (waited > 0f) late++;
                SoundCategory category;
                float raw, scaled;
                bool known = ZsfxCustomUpdatePatch.TryGetLast(zsfx, out category, out raw, out scaled);
                string group = source.outputAudioMixerGroup != null ? source.outputAudioMixerGroup.name : "(none)";
                bool playing = source.isPlaying;
                float expected = known ? SoundLevels.Factor(category) : -1f;
                float ratio = known && raw > 0.0005f ? scaled / raw : -1f;
                bool wrong = known && ratio >= 0f && System.Math.Abs(ratio - expected) > 0.02f;
                played++;
                if (!playing) notPlaying++;
                if (group == "(none)") noGroup++;
                if (wrong) ratioWrong++;
                Increment(groups, group);
                Increment(categories, known ? category.ToString() : "unseen");
                ModLog.Info("SWEEP " + prefab.name + " (root " + prefab.transform.root.name + ") | group " + group + " | " + (playing ? "playing" : "not playing") + " | category " + (known ? category.ToString() : "unseen")
                    + " | raw " + raw.ToString("F3") + " scaled " + scaled.ToString("F3") + " ratio " + ratio.ToString("F3") + " expected " + expected.ToString("F3") + (wrong ? " | WRONG" : string.Empty)
                    + " | played volume " + (scaled * SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Sfx()).ToString("F3") + " (scaled x mixer " + SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Sfx().ToString("F2") + ")");
                Destroy(instance);
                yield return null;
            }
            var sb = new StringBuilder();
            sb.Append(played).Append(" sounds, ").Append(notPlaying).Append(" not playing even after waiting up to " + LateSeconds + " s (" + late + " needed the longer wait), ").Append(noGroup).Append(" without a mixer group (the master cannot reach them), ")
              .Append(ratioWrong).Append(" with a wrong category factor; groups ").Append(Join(groups)).Append("; categories ").Append(Join(categories));
            Summary = sb.ToString();
            ModLog.Info("SWEEP done: " + Summary);
            Done = true;
        }

        private static void Increment(SortedDictionary<string, int> map, string key)
        {
            int n;
            map.TryGetValue(key, out n);
            map[key] = n + 1;
        }

        private static string Join(SortedDictionary<string, int> map)
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in map)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(pair.Key).Append(' ').Append(pair.Value);
            }
            return sb.ToString();
        }
    }
}
