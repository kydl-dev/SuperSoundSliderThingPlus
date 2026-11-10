using KydlCore.Common;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Bootstrap;
using SuperSoundSliderThingPlus.Configuration;
using SuperSoundSliderThingPlus.Features.Example.Application;
using SuperSoundSliderThingPlus.Features.Example.Domain;
using SuperSoundSliderThingPlus.Features.Example.Execution;
using SuperSoundSliderThingPlus.Presentation.Hub.Registration;
using SuperSoundSliderThingPlus.Presentation.Hub.ViewModels;

namespace SuperSoundSliderThingPlus.Features.Example.Infrastructure
{
    /// <summary>
    /// EXAMPLE feature (template; replace / rename). The adapter: wires the handler + executor once, owns the game-side state, and on
    /// Start / Stop adds / removes everything it contributes (here: its KY hub tab). Gate: EnableAll only (a mod with several features gives
    /// each one its own [<Feature>] Enable and passes it to ModSettings.FeatureOn).
    /// </summary>
    internal sealed class ExampleFeature : IFeature
    {
        internal const string FeatureId = "example";
        readonly DependencyRegistry _deps;

        /// The current example value (state of this feature). Test hooks read it.
        internal float Value { get; private set; } = ExampleRules.Default;
        internal static ExampleFeature Instance { get; private set; }

        internal ExampleFeature(DependencyRegistry deps)
        {
            _deps = deps;
            var executor = new ExampleValueExecutor(new LocalOwnership(), SetValue);   // method group bound once
            deps.Commands.Register(new SetExampleValueHandler(executor, deps.Trace));
            Instance = this;
        }

        public string Id { get { return FeatureId; } }

        public bool IsEnabled() { return ModSettings.FeatureOn(null); }

        public bool Start()
        {
            bool tab = HubTabs.AddExampleTab(() => ExamplePageModel.Build(this, _deps.Commands));
            ModLog.State("example feature", "stopped", "started", "value " + Value + ", hub tab added " + tab);
            return tab;
        }

        public bool Stop()
        {
            bool gone = HubTabs.RemoveExampleTab();
            ModLog.State("example feature", "started", "stopped", "hub tab removed " + gone);
            return gone;
        }

        // PROJECT_RULES 28: a state change is written with ModLog.State (old, new, why); it goes to the debug log AND to KydlCore's flight recorder,
        // whose dump after a failure shows it even with the debug log off. A choice is ModLog.Decision(what, outcome, inputs); per-frame code
        // writes ModLog.Every(key, seconds, status); a creature the mod drives is tracked with FlightRecorder.Track(creature, label, () => state).
        void SetValue(float value)
        {
            ModLog.State("example value", Value.ToString("0.##"), value.ToString("0.##"), "SetExampleValue command, owner checked");
            Value = value;
        }
    }
}
