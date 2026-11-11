using System;
using KydlCore.Common;
using UnityEngine;
using UnityEngine.Audio;

namespace SuperSoundSliderThingPlus.Testing.TestHooks
{
    /// <summary>
    /// TEST ONLY: measures the FINAL MIX. A steady sine tone is played through one mixer group and the listener's output block
    /// (AudioListener.GetOutputData = what leaves the master mixer) is measured while the game's master volume changes, so the master's real effect on the
    /// audio that reaches the speakers is seen, not only the routing. The tone is loud for a fraction of a second at the test profile's level.
    /// </summary>
    internal static class MixTestHooks
    {
        private const int BlockSize = 1024, SampleRate = 44100;
        private static GameObject _tone;
        private static double _marked = double.NaN;
        private static long _phase;
        private static readonly float[] Left = new float[BlockSize], Right = new float[BlockSize];

        /// <summary>Starts a looping sine tone through the named mixer group ("SFX", "SFX_LARGE", "Ambient", "GUI", or "none" for no group). Returns what it did.</summary>
        [TestHook("SuperSoundSliderThingPlus.mix.tone")]
        static string Tone(string group)
        {
            Stop();
            AudioMixerGroup target = null;
            if (group != "none")
            {
                if (AudioMan.instance == null || AudioMan.instance.m_masterMixer == null) return "Failed: no AudioMan";
                foreach (AudioMixerGroup g in AudioMan.instance.m_masterMixer.FindMatchingGroups(group))
                {
                    if (g.name == group) target = g;
                }
                if (target == null) return "Failed: no mixer group '" + group + "'";
            }
            var clip = AudioClip.Create("ssst_tone", SampleRate, 1, SampleRate, true, delegate(float[] buffer) { for (int i = 0; i < buffer.Length; i++) { buffer[i] = 0.6f * Mathf.Sin(2f * Mathf.PI * 440f * (_phase % SampleRate) / SampleRate); _phase++; } });
            _tone = new GameObject("SSST_Tone");
            var source = _tone.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.outputAudioMixerGroup = target;
            source.Play();
            return "tone through " + (target != null ? target.name : "no group");
        }

        /// <summary>The final output level (RMS of the listener's output block, both channels) right now.</summary>
        [TestHook("SuperSoundSliderThingPlus.mix.level")]
        static double Level() { return Math.Round(Measure(), 5); }

        /// <summary>Remembers the current final output level as the reference.</summary>
        [TestHook("SuperSoundSliderThingPlus.mix.mark")]
        static double Mark()
        {
            _marked = Measure();
            ModLog.Info("SoundTestHooks: MIX mark " + _marked.ToString("F5"));
            return Math.Round(_marked, 5);
        }

        /// <summary>Is the final output now `expected` x the marked level (within `tolerance`)? The reference must be audible (a silent reference proves nothing).</summary>
        [TestHook("SuperSoundSliderThingPlus.mix.ratioNear")]
        static bool RatioNear(double expected, double tolerance)
        {
            double now = Measure();
            if (double.IsNaN(_marked) || _marked < 0.005)
            {
                ModLog.Info("SoundTestHooks: MIX reference too quiet to judge (" + _marked.ToString("F5") + ")");
                return false;
            }
            double ratio = now / _marked;
            bool ok = Math.Abs(ratio - expected) <= tolerance;
            ModLog.Info("SoundTestHooks: MIX marked " + _marked.ToString("F5") + " now " + now.ToString("F5") + " ratio " + ratio.ToString("F3") + " expected " + expected.ToString("F3") + " -> " + (ok ? "ok" : "WRONG"));
            return ok;
        }

        /// <summary>The game's master volume, as the mixer applies it to the effects (amplitude factor = sqrt of the linear volume: the game converts it with 10 x log10).</summary>
        [TestHook("SuperSoundSliderThingPlus.mix.stop")]
        static bool Stop()
        {
            if (_tone != null) UnityEngine.Object.Destroy(_tone);
            _tone = null;
            _marked = double.NaN;
            return _tone == null;
        }

        private static double Measure()
        {
            AudioListener.GetOutputData(Left, 0);
            AudioListener.GetOutputData(Right, 1);
            double sum = 0;
            for (int i = 0; i < BlockSize; i++) sum += Left[i] * Left[i] + Right[i] * Right[i];
            return Math.Sqrt(sum / (2 * BlockSize));
        }
    }
}
