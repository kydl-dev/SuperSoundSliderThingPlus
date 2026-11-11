using BenchmarkDotNet.Attributes;
using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Benchmarks
{
    /// <summary>
    /// Coverage suite for the sound rules on the hot path (skill microbenchmarking, use case 1). ZSFX.CustomUpdate runs for every playing sound
    /// every frame, so <see cref="SoundMixRules.Factor"/> and <see cref="LoudnessRules.Gain"/> are measured per call; the classifier runs once per
    /// new sound, with the name lengths the game really has (scale axis = sound name). Inputs come from fields set up from [Params].
    /// </summary>
    [MemoryDiagnoser]
    public class SoundRulesBenchmarks
    {
        private float _master, _category, _measured;
        private string _sound, _owner;

        [Params(0f, 50f, 100f)]
        public float MasterMute { get; set; }

        [Params("sfx_wolf_haul", "sfx_OpenPortal(Clone)", "fx_hen_love(Clone)")]
        public string Sound { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _master = MasterMute;
            _category = 100f - MasterMute;
            _measured = 0.05f + MasterMute / 500f;
            _sound = Sound;
            _owner = "Hen(Clone)";
        }

        /// <summary>The per-sound, per-frame factor.</summary>
        [Benchmark(Baseline = true)]
        public float Factor() => SoundMixRules.Factor(_master, _category).Value;

        /// <summary>The loudness gain of one clip (once per clip, then cached by the mod).</summary>
        [Benchmark]
        public float Gain() => LoudnessRules.Gain(_measured, 0.1f, 0.25f, 3f).Value;

        /// <summary>The game volume to mute level conversion (a few times per second).</summary>
        [Benchmark]
        public float MuteFromVolume() => MasterVolumeRules.MuteFromVolume(_measured).Value;

        /// <summary>Classifying a new sound (once per sound).</summary>
        [Benchmark]
        public int Classify() => (int)SoundClassifier.Classify(_sound, _owner);
    }
}
