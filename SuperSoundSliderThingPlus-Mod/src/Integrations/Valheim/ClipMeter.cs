using System.Collections.Generic;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Integrations.Valheim
{
    /// <summary>
    /// Measures how loud an AudioClip is: the RMS of a short window from the middle of the clip (so a 60 s ambient loop costs the same as a
    /// click), once per clip, remembered by the clip's instance id. A clip that cannot be read (streamed, not loaded yet, silent) is
    /// remembered as "unknown" and the caller leaves it alone. Reads the clip only, never changes it. Main thread only.
    /// </summary>
    internal static class ClipMeter
    {
        internal const float Unknown = -1f;
        internal const int MaxClips = 4000;
        private static readonly Dictionary<int, float> Measured = new Dictionary<int, float>();

        /// <summary>How many clips are remembered.</summary>
        internal static int Count => Measured.Count;

        /// <summary>Expected outcome: the clip's RMS, or <see cref="Unknown"/> (null clip, not readable, full cache). Never throws.</summary>
        internal static float RmsOf(AudioClip clip, float windowSeconds)
        {
            if (clip == null) return Unknown;
            int id = clip.GetInstanceID();
            float rms;
            if (Measured.TryGetValue(id, out rms)) return rms;
            if (Measured.Count >= MaxClips) return Unknown;
            rms = Measure(clip, windowSeconds);
            Measured[id] = rms;
            ModLog.Verbose("ClipMeter: '" + clip.name + "' " + (rms == Unknown ? "not measurable (load state " + clip.loadState + ", type " + clip.loadType + ")" : "rms " + rms.ToString("F4")));
            return rms;
        }

        /// <summary>Forgets every measurement (tests).</summary>
        internal static bool Clear()
        {
            Measured.Clear();
            return Measured.Count == 0;
        }

        // AudioClip.GetData(float[], int) is called through reflection: the game assembly also exposes a Span<float> overload whose Span type the net48 compiler cannot resolve (CS7069)
        private static readonly System.Reflection.MethodInfo GetData = typeof(AudioClip).GetMethod("GetData", new[] { typeof(float[]), typeof(int) });

        private static bool ReadData(AudioClip clip, float[] buffer, int offset)
        {
            if (GetData == null) return false;
            object result = GetData.Invoke(clip, new object[] { buffer, offset });
            return result is bool ok && ok;
        }

        private static float Measure(AudioClip clip, float windowSeconds)
        {
            if (clip.loadState != AudioDataLoadState.Loaded || clip.loadType == AudioClipLoadType.Streaming) return Unknown;
            int frames = clip.samples, channels = clip.channels;
            if (frames <= 0 || channels <= 0) return Unknown;
            int window = Mathf.Min(frames, Mathf.Max(1, Mathf.RoundToInt(clip.frequency * windowSeconds)));
            int offset = (frames - window) / 2;
            var buffer = new float[window * channels];
            try
            {
                if (!ReadData(clip, buffer, offset)) return Unknown;
            }
            catch (UnityException e)
            {
                ModLog.Warn("ClipMeter: '" + clip.name + "' could not be read - " + e.Message);
                return Unknown;
            }
            var r = LoudnessRules.Rms(buffer, buffer.Length);
            return r.IsSuccess ? r.Value : Unknown;
        }
    }
}
