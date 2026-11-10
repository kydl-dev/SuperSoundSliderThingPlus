using KydlCore.Common;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Bootstrap;
using SuperSoundSliderThingPlus.Configuration;
using SuperSoundSliderThingPlus.Features.Sound.Application;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Execution;
using SuperSoundSliderThingPlus.Integrations.Valheim;
using SuperSoundSliderThingPlus.Presentation.Hub.Commands;
using SuperSoundSliderThingPlus.Presentation.Hub.Registration;
using SuperSoundSliderThingPlus.Presentation.Hub.ViewModels;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Features.Sound.Infrastructure
{
    /// <summary>
    /// The sound feature adapter (MIGRATION_DECISIONS A4): wires the SetMute handler once, and on Start / Stop adds / removes what the feature
    /// contributes (its KY hub tab, the master sync). The Harmony patches in Integrations/Harmony read the factors through
    /// <see cref="SoundLevels"/> while <see cref="Running"/>. Gate: EnableAll only (one functionality).
    /// The master slider IS the game's master volume (D-017): a change of the slider is pushed to the game, the game's volume (also at game
    /// start) is pulled into the slider; the first time the game starts with the mod installed the category sliders start at 50 (D-018).
    /// </summary>
    internal sealed class SoundFeature : IFeature
    {
        internal const string FeatureId = "sound";
        internal const float FirstRunMute = 50f, TickSeconds = 0.5f, SaveDelaySeconds = 1.5f;
        private static readonly SoundSlider[] Categories =
            { SoundSlider.Effects, SoundSlider.Weather, SoundSlider.WolfPup, SoundSlider.ShieldGenerator, SoundSlider.ChickenLove, SoundSlider.Portal };

        private readonly DependencyRegistry _deps;
        private bool _running, _pulling;
        private float _nextTick, _saveAt = -1f;

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
            ModSettings.MasterMute.SettingChanged += OnMasterChanged;
            bool tab = HubTabs.AddSoundTab(() => { Pull(); return SoundPageModel.Build(_deps.Commands); });
            Pull();
            ApplyFirstRunDefaults();
            ModLog.State("sound feature", "stopped", "started", "patches act, master follows the game volume, hub tab added " + tab);
            return _running && tab;
        }

        public bool Stop()
        {
            _running = false;
            ModSettings.MasterMute.SettingChanged -= OnMasterChanged;
            bool gone = HubTabs.RemoveSoundTab();
            ModLog.State("sound feature", "started", "stopped", "patches leave the sounds alone, hub tab removed " + gone);
            return !_running && gone;
        }

        /// <summary>Game -> slider: the slider takes the game's master volume. Expected outcome: true when the slider already matches or was set.</summary>
        internal bool Pull()
        {
            if (!_running) return false;
            var mute = MasterVolumeRules.MuteFromVolume(GameVolume.Master());
            if (!mute.IsSuccess) return false;
            if (MasterVolumeRules.SameMute(SoundLevels.MuteOf(SoundSlider.Master), mute.Value)) return true;
            _pulling = true;   // the change event must not echo the value back into the game
            try
            {
                var r = SoundCommands.SetMute(_deps.Commands, SoundSlider.Master, mute.Value);
                ModLog.State("master mute", "slider", "game volume", "pulled " + mute.Value.ToString("0.#") + " from the game: " + r);
                return r.IsSuccess;
            }
            finally { _pulling = false; }
        }

        /// <summary>A few times a second: follows the game's master volume, applies the first-run defaults, saves a pushed volume. Cheap and throttled.</summary>
        internal void Tick()
        {
            if (!_running || Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + TickSeconds;
            Pull();
            ApplyFirstRunDefaults();
            if (_saveAt >= 0f && Time.unscaledTime >= _saveAt)
            {
                _saveAt = -1f;
                GameVolume.Save();
            }
            ModLog.Every("sound.tick", 30f, "SoundFeature: master mute " + SoundLevels.MuteOf(SoundSlider.Master).ToString("0.#") + ", game volume " + GameVolume.Master().ToString("F2")
                + ", first-run defaults " + (ModSettings.DefaultsApplied.Value ? "done" : "pending"));
        }

        /// <summary>Slider -> game (the hub, the settings tab or the config file changed the master mute).</summary>
        private void OnMasterChanged(object sender, System.EventArgs e)
        {
            if (!_running || _pulling) return;
            var volume = MasterVolumeRules.VolumeFromMute(SoundLevels.MuteOf(SoundSlider.Master));
            if (!volume.IsSuccess) return;
            if (MasterVolumeRules.SameMute(MasterVolumeRules.MuteFromVolume(GameVolume.Master()).Value, SoundLevels.MuteOf(SoundSlider.Master))) return;
            GameVolume.Write(volume.Value);
            _saveAt = Time.unscaledTime + SaveDelaySeconds;
            ModLog.State("game master volume", "game", "slider", "pushed " + volume.Value.ToString("F2") + " into the game's master volume");
        }

        // D-018: the first time the mod runs after install (no DefaultsApplied yet) the category sliders start at 50; after that the player's values stand.
        private void ApplyFirstRunDefaults()
        {
            if (ModSettings.DefaultsApplied.Value) return;
            foreach (SoundSlider slider in Categories) SoundCommands.SetMute(_deps.Commands, slider, FirstRunMute);
            ModSettings.DefaultsApplied.Value = true;
            ModLog.State("first-run defaults", "pending", "applied", "category sliders set to " + FirstRunMute.ToString("0") + "%");
        }

        private static void SetMute(SoundSlider slider, float level)
        {
            var entry = SoundLevels.EntryOf(slider);
            ModLog.State("mute " + slider, entry.Value.ToString("0.##"), level.ToString("0.##"), "SetMute command, owner checked");
            entry.Value = level;
        }
    }
}
