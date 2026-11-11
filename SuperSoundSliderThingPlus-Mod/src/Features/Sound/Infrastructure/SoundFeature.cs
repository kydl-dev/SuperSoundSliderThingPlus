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
        // the first-run 50% applies to the category sliders; Effects, Music and Master follow the game's own volumes instead
        private static readonly SoundSlider[] Categories =
            { SoundSlider.Weather, SoundSlider.WolfPup, SoundSlider.ShieldGenerator, SoundSlider.ChickenLove, SoundSlider.Portal };
        // the sliders that ARE a game volume (D-017, D-023): mute = (1 - volume) x 100, synced both ways
        private static readonly SoundSlider[] Synced = { SoundSlider.Master, SoundSlider.Music, SoundSlider.Effects };

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
            ModSettings.MusicMute.SettingChanged += OnMusicChanged;
            ModSettings.EffectsMute.SettingChanged += OnEffectsChanged;
            bool tab = HubTabs.AddSoundTab(() => { Pull(); return SoundPageModel.Build(_deps.Commands); });
            Pull();
            GameVolume.Apply(true);
            ApplyFirstRunDefaults();
            ModLog.State("sound feature", "stopped", "started", "patches act, master follows the game volume, hub tab added " + tab);
            return _running && tab;
        }

        public bool Stop()
        {
            _running = false;
            ModSettings.MasterMute.SettingChanged -= OnMasterChanged;
            ModSettings.MusicMute.SettingChanged -= OnMusicChanged;
            ModSettings.EffectsMute.SettingChanged -= OnEffectsChanged;
            GameVolume.Apply(false);   // the game's own sound effects volume applies again
            bool gone = HubTabs.RemoveSoundTab();
            ModLog.State("sound feature", "started", "stopped", "patches leave the sounds alone, hub tab removed " + gone);
            return !_running && gone;
        }

        /// <summary>Game -> slider: the master, music and sound effects sliders take the game's volumes. Expected outcome: true when every slider matches or was set.</summary>
        internal bool Pull()
        {
            if (!_running) return false;
            bool ok = true;
            foreach (SoundSlider slider in Synced) ok &= PullOne(slider);
            return ok;
        }

        private bool PullOne(SoundSlider slider)
        {
            var mute = MasterVolumeRules.MuteFromVolume(GameVolume.Get(KeyOf(slider)));
            if (!mute.IsSuccess) return false;
            if (MasterVolumeRules.SameMute(SoundLevels.MuteOf(slider), mute.Value)) return true;
            _pulling = true;   // the change event must not echo the value back into the game
            try
            {
                var r = SoundCommands.SetMute(_deps.Commands, slider, mute.Value);
                ModLog.State(slider + " mute", "slider", "game volume", "pulled " + mute.Value.ToString("0.#") + " from the game: " + r);
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
            if (GameVolume.Reassert()) ModLog.Verbose("SoundFeature: the game re-applied its sound effects volume, set back to master only (the slider does that per sound)");
            ApplyFirstRunDefaults();
            if (_saveAt >= 0f && Time.unscaledTime >= _saveAt)
            {
                _saveAt = -1f;
                GameVolume.Save();
            }
            ModLog.Every("sound.tick", 30f, "SoundFeature: master mute " + SoundLevels.MuteOf(SoundSlider.Master).ToString("0.#") + ", game volume " + GameVolume.Master().ToString("F2")
                + ", first-run defaults " + (ModSettings.DefaultsApplied.Value ? "done" : "pending"));
        }

        /// <summary>Slider -> game (the hub, the settings tab or the config file changed a synced slider).</summary>
        private void OnMasterChanged(object sender, System.EventArgs e) => Push(SoundSlider.Master);
        private void OnMusicChanged(object sender, System.EventArgs e) => Push(SoundSlider.Music);
        private void OnEffectsChanged(object sender, System.EventArgs e) => Push(SoundSlider.Effects);

        private void Push(SoundSlider slider)
        {
            if (!_running || _pulling) return;
            var volume = MasterVolumeRules.VolumeFromMute(SoundLevels.MuteOf(slider));
            if (!volume.IsSuccess) return;
            string key = KeyOf(slider);
            if (MasterVolumeRules.SameMute(MasterVolumeRules.MuteFromVolume(GameVolume.Get(key)).Value, SoundLevels.MuteOf(slider))) return;
            GameVolume.Write(key, volume.Value);
            _saveAt = Time.unscaledTime + SaveDelaySeconds;
            ModLog.State("game " + key, "game", "slider", "pushed " + volume.Value.ToString("F2") + " into the game's " + key);
        }

        private static string KeyOf(SoundSlider slider)
        {
            switch (slider)
            {
                case SoundSlider.Music: return GameVolume.MusicKey;
                case SoundSlider.Effects: return GameVolume.SfxKey;
                default: return GameVolume.MasterKey;
            }
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
