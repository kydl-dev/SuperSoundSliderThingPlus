using System.Collections.Generic;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Integrations.Valheim
{
    /// <summary>
    /// Learns how loud each AudioClip is while it plays. Most game clips are compressed in memory or streamed, so their samples cannot be read
    /// (AudioClip.GetData fails - found in game 2026-11-11); AudioSource.GetOutputData is the source's output AFTER its volume (verified in game: half
    /// the volume gives half the RMS), so each block is divided by the volume the source played at to get the clip's own level. One estimate per clip,
    /// remembered by instance id, bounded. Reads audio only, never changes it. Main thread only.
    /// </summary>
    internal static class ClipMeter
    {
        internal const float Unknown = -1f;
        internal const int MaxClips = 4000, BlockSize = 1024;
        private static readonly Dictionary<int, LoudnessEstimate> Estimates = new Dictionary<int, LoudnessEstimate>();
        private static readonly float[] Block = new float[BlockSize];

        /// <summary>How many clips have an estimate.</summary>
        internal static int Count => Estimates.Count;

        /// <summary>Expected outcome: adds one block of the source's output to its clip's estimate (true) or does nothing (false: no clip, not playing, volume too low to divide by, full).</summary>
        internal static bool Observe(AudioSource source, float playedVolume)
        {
            if (source == null || source.clip == null || !source.isPlaying || playedVolume < 0.01f) return false;
            LoudnessEstimate estimate = EstimateFor(source.clip, true);
            if (estimate == null || !estimate.WantsMore) return false;
            source.GetOutputData(Block, 0);
            var rms = LoudnessRules.Rms(Block, Block.Length);
            if (!rms.IsSuccess) return false;
            bool added = estimate.Add(rms.Value / playedVolume);
            if (added && estimate.Blocks == LoudnessEstimate.MaxBlocks)
                ModLog.Verbose("ClipMeter: '" + source.clip.name + "' learned, rms " + estimate.Rms.ToString("F4") + " from " + estimate.Blocks + " blocks");
            return added;
        }

        /// <summary>Expected outcome: the clip's level once enough was heard, otherwise <see cref="Unknown"/>.</summary>
        internal static float RmsOf(AudioClip clip)
        {
            LoudnessEstimate estimate = clip == null ? null : EstimateFor(clip, false);
            return estimate != null && estimate.IsReady ? estimate.Rms : Unknown;
        }

        /// <summary>Forgets every estimate (tests).</summary>
        internal static bool Clear()
        {
            Estimates.Clear();
            return Estimates.Count == 0;
        }

        private static LoudnessEstimate EstimateFor(AudioClip clip, bool create)
        {
            int id = clip.GetInstanceID();
            LoudnessEstimate estimate;
            if (Estimates.TryGetValue(id, out estimate)) return estimate;
            if (!create || Estimates.Count >= MaxClips) return null;
            estimate = new LoudnessEstimate();
            Estimates[id] = estimate;
            return estimate;
        }
    }
}
