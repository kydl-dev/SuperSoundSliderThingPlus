using BenchmarkDotNet.Attributes;
using SuperSoundSliderThingPlus.Features.Example.Domain;

namespace SuperSoundSliderThingPlus.Benchmarks
{
    /// EXAMPLE (template): coverage-suite benchmark of a game-free rule (skill microbenchmarking, use case 1). Replace with the mod's own hot-path
    /// rules (per tick / per entity). Rules: inputs in fields or [Params] (never literals), return the result, no manual loops, mark a baseline when
    /// comparing alternatives, a seeded Random for generated data.
    [MemoryDiagnoser]
    public class ExampleRulesBenchmarks
    {
        private float _requested;

        [Params(-1f, 5f, 12f)]
        public float Requested { get; set; }

        [GlobalSetup]
        public void Setup() => _requested = Requested;

        [Benchmark]
        public float Decide() => ExampleRules.Decide(_requested).Value;
    }
}