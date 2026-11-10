using KydlCore.Common;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Configuration;

namespace SuperSoundSliderThingPlus.Features.Sound.Infrastructure
{
    /// <summary>
    /// The sound feature adapter (MIGRATION_DECISIONS A4): starts / stops what the sound feature contributes live by its Enable switch.
    /// The volume maths and the sound classifier live in Domain (unit-tested); the Harmony patches in Integrations/Harmony read the
    /// factors through SoundLevels while this feature runs. The KY hub page follows (the sliders are config entries until then). Gate: EnableAll only (one functionality).
    /// </summary>
    internal sealed class SoundFeature : IFeature
    {
        internal const string FeatureId = "sound";
        bool _running;

        internal static SoundFeature Instance { get; private set; }

        /// <summary>True while the feature is started (the patches only act then).</summary>
        internal bool Running { get { return _running; } }

        internal SoundFeature() => Instance = this;

        public string Id { get { return FeatureId; } }

        public bool IsEnabled() { return ModSettings.FeatureOn(null); }

        public bool Start()
        {
            _running = true;
            ModLog.State("sound feature", "stopped", "started", "patches act while started");
            return _running;
        }

        public bool Stop()
        {
            _running = false;
            ModLog.State("sound feature", "started", "stopped", "patches leave the sounds alone");
            return !_running;
        }
    }
}
