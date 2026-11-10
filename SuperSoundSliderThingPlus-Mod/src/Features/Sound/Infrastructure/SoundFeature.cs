using KydlCore.Common;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Bootstrap;
using SuperSoundSliderThingPlus.Configuration;
using SuperSoundSliderThingPlus.Features.Sound.Application;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Execution;
using SuperSoundSliderThingPlus.Presentation.Hub.Registration;
using SuperSoundSliderThingPlus.Presentation.Hub.ViewModels;

namespace SuperSoundSliderThingPlus.Features.Sound.Infrastructure
{
    /// <summary>
    /// The sound feature adapter (MIGRATION_DECISIONS A4): wires the SetMute handler once, and on Start / Stop adds / removes what the feature
    /// contributes (its KY hub tab). The Harmony patches in Integrations/Harmony read the factors through <see cref="SoundLevels"/> while
    /// <see cref="Running"/>. Gate: EnableAll only (one functionality).
    /// </summary>
    internal sealed class SoundFeature : IFeature
    {
        internal const string FeatureId = "sound";
        private readonly DependencyRegistry _deps;
        private bool _running;

        internal static SoundFeature Instance { get; private set; }

        /// <summary>True while the feature is started (the patches only act then).</summary>
        internal bool Running { get { return _running; } }

        internal SoundFeature(DependencyRegistry deps)
        {
            _deps = deps;
            var executor = new MuteExecutor(new LocalSoundOwnership(), SetMute);   // method group bound once
            deps.Commands.Register(new SetMuteHandler(executor, deps.Trace));
            Instance = this;
        }

        public string Id { get { return FeatureId; } }

        public bool IsEnabled() { return ModSettings.FeatureOn(null); }

        public bool Start()
        {
            _running = true;
            bool tab = HubTabs.AddSoundTab(() => SoundPageModel.Build(_deps.Commands));
            ModLog.State("sound feature", "stopped", "started", "patches act, hub tab added " + tab);
            return _running && tab;
        }

        public bool Stop()
        {
            _running = false;
            bool gone = HubTabs.RemoveSoundTab();
            ModLog.State("sound feature", "started", "stopped", "patches leave the sounds alone, hub tab removed " + gone);
            return !_running && gone;
        }

        private static void SetMute(SoundSlider slider, float level)
        {
            var entry = SoundLevels.EntryOf(slider);
            ModLog.State("mute " + slider, entry.Value.ToString("0.##"), level.ToString("0.##"), "SetMute command, owner checked");
            entry.Value = level;
        }
    }
}
