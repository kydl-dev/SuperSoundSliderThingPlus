using System;
using System.Collections.Generic;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Bootstrap;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Infrastructure;
using SuperSoundSliderThingPlus.Presentation.Hub.Commands;
using SuperSoundSliderThingPlus.Presentation.Hub.Registration;
using UnityEngine;

namespace SuperSoundSliderThingPlus.Testing.TestHooks
{
    /// <summary>
    /// DrakesTestLab hooks for the sound feature (same rules as ModTestHooks: real code paths, short stable answers; `min` / `max` are NOT
    /// available on a hook step, so numbers a test must bound are exposed as bool reads like ProbeAudible / ProbeSilent). The probe is ONE sound
    /// the test plays itself (an effect prefab found among the loaded assets), so a test never depends on what the world happens to play.
    /// </summary>
    internal static class SoundTestHooks
    {
        private const float AudibleAbove = 0.001f, SilentBelow = 0.0001f;
        private static GameObject _probe;
        private static AudioSource _probeSource;

        /// <summary>Is the sound tab registered in the KY hub right now (no stale tab after the feature stops)?</summary>
        [TestHook("SuperSoundSliderThingPlus.sound.tab")]
        static bool SoundTab() { return global::KydlCore.Features.Hub.Contracts.KyHub.HasTab(Plugin.ModGuid, HubTabs.SoundId); }

        /// <summary>The hub slider's path (dispatcher -> handler -> rules -> executor). Returns the response, e.g. "Success: clamped into range".</summary>
        [TestHook("SuperSoundSliderThingPlus.mute.set")]
        static string MuteSet(string slider, double value)
        {
            if (ModInstaller.Current == null) return "Failed: mod not installed";
            SoundSlider parsed;
            if (!TryParse(slider, out parsed)) return "Failed: unknown slider '" + slider + "'";
            return SoundCommands.SetMute(ModInstaller.Current.Deps.Commands, parsed, (float)value).ToString();
        }

        /// <summary>The stored mute level (percent) of a slider; NaN for an unknown slider.</summary>
        [TestHook("SuperSoundSliderThingPlus.mute.value")]
        static double MuteValue(string slider)
        {
            SoundSlider parsed;
            return TryParse(slider, out parsed) ? SoundLevels.MuteOf(parsed) : double.NaN;
        }

        /// <summary>The live loudness factor of a category (D-011: (1 - master) x (1 - category)); NaN for an unknown category.</summary>
        [TestHook("SuperSoundSliderThingPlus.sound.factor")]
        static double Factor(string category)
        {
            SoundCategory parsed;
            return Enum.TryParse(category, true, out parsed) ? SoundLevels.Factor(parsed) : double.NaN;
        }

        /// <summary>Has the patch classified a sound whose name contains `sound` into `category` (the recorder saw it)?</summary>
        [TestHook("SuperSoundSliderThingPlus.sound.classified")]
        static bool Classified(string sound, string category)
        {
            SoundCategory parsed;
            bool known = Enum.TryParse(category, true, out parsed);
            bool seen = known && SoundRecorder.Has(sound, parsed);
            ModLog.Verbose("SoundTestHooks: classified '" + sound + "' as " + category + " -> " + seen + " (recorded: " + SoundRecorder.Summary() + ")");
            return seen;
        }

        /// <summary>Plays one effect prefab next to the player and keeps it as THE probe. Returns what happened, e.g. "playing sfx_thunder(Clone)".</summary>
        [TestHook("SuperSoundSliderThingPlus.sound.play")]
        static string Play(string prefab)
        {
            Clear();
            GameObject source = FindPrefab(prefab);
            if (source == null) return "Failed: no loaded prefab named '" + prefab + "'";
            Player player = Player.m_localPlayer;
            if (player == null) return "Failed: no local player";
            _probe = UnityEngine.Object.Instantiate(source, player.transform.position + Vector3.up, Quaternion.identity);
            _probeSource = _probe.GetComponentInChildren<AudioSource>();
            StartIfIdle(_probe);
            ModLog.Verbose("SoundTestHooks: playing " + _probe.name + ", audio source " + (_probeSource != null));
            return _probeSource != null ? "playing " + _probe.name : "Failed: '" + prefab + "' has no AudioSource";
        }

        private static GameObject _cub;
        private static float _baseline = float.NaN;

        /// <summary>Spawns a wolf cub and plays the effect prefab at the cub (the sound of a wolf PUP), keeping it as THE probe.</summary>
        [TestHook("SuperSoundSliderThingPlus.sound.playAtCub")]
        static string PlayAtCub(string prefab)
        {
            Clear();
            GameObject cubPrefab = FindNetPrefab("Wolf_cub");
            GameObject source = FindPrefab(prefab);
            Player player = Player.m_localPlayer;
            if (cubPrefab == null || source == null || player == null) return "Failed: cub " + (cubPrefab != null) + " sound " + (source != null) + " player " + (player != null);
            _cub = UnityEngine.Object.Instantiate(cubPrefab, player.transform.position + player.transform.forward * 4f, Quaternion.identity);
            _probe = UnityEngine.Object.Instantiate(source, _cub.transform.position + Vector3.up * 0.5f, Quaternion.identity);
            _probeSource = _probe.GetComponentInChildren<AudioSource>();
            StartIfIdle(_probe);
            return _probeSource != null ? "playing " + _probe.name + " at a cub" : "Failed: '" + prefab + "' has no AudioSource";
        }

        /// <summary>Remembers the probe's volume right now as the reference for <see cref="ProbeRatioNear"/>.</summary>
        [TestHook("SuperSoundSliderThingPlus.probe.mark")]
        static double ProbeMark()
        {
            _baseline = Alive() ? _probeSource.volume : float.NaN;
            return Math.Round(_baseline, 4);
        }

        /// <summary>Is the probe's volume now `expected` x the marked volume (within `tolerance`)?</summary>
        [TestHook("SuperSoundSliderThingPlus.probe.ratioNear")]
        static bool ProbeRatioNear(double expected, double tolerance)
        {
            if (!Alive() || float.IsNaN(_baseline) || _baseline <= 0.0005f) return false;
            double ratio = _probeSource.volume / _baseline;
            bool near = Math.Abs(ratio - expected) <= tolerance;
            ModLog.Info("SoundTestHooks: AUDIT " + _probeSource.gameObject.name + " baseline " + _baseline.ToString("F4") + " now " + _probeSource.volume.ToString("F4") + " ratio " + ratio.ToString("F3") + " expected " + expected + " -> " + (near ? "ok" : "WRONG"));
            return near;
        }

        /// <summary>Is the probe sound playing right now?</summary>
        [TestHook("SuperSoundSliderThingPlus.sound.probePlaying")]
        static bool ProbePlaying() { return Alive() && _probeSource.isPlaying; }

        /// <summary>Is the probe playing AND audible (the volume the game and the patches left on its AudioSource)?</summary>
        [TestHook("SuperSoundSliderThingPlus.sound.probeAudible")]
        static bool ProbeAudible()
        {
            bool audible = Alive() && _probeSource.isPlaying && _probeSource.volume > AudibleAbove;
            ModLog.Verbose("SoundTestHooks: probeAudible " + audible + " (" + Describe() + ")");
            return audible;
        }

        /// <summary>Is the probe playing but silenced (volume at zero)? A sound that is not playing is NOT silent here: the test must prove it plays.</summary>
        [TestHook("SuperSoundSliderThingPlus.sound.probeSilent")]
        static bool ProbeSilent()
        {
            bool silent = Alive() && _probeSource.isPlaying && _probeSource.volume < SilentBelow;
            ModLog.Verbose("SoundTestHooks: probeSilent " + silent + " (" + Describe() + ")");
            return silent;
        }

        /// <summary>The loudness-matching gain the equalizer gives the probe's clip right now, e.g. "gain 1.42 rms 0.0705" ("gain 1.00" = left alone / off).</summary>
        [TestHook("SuperSoundSliderThingPlus.loudness.gain")]
        static string LoudnessGain()
        {
            if (!Alive()) return "Failed: no probe";
            float gain = SuperSoundSliderThingPlus.Features.Sound.Infrastructure.LoudnessEqualizer.GainFor(_probeSource.clip);
            float rms = SuperSoundSliderThingPlus.Integrations.Valheim.ClipMeter.RmsOf(_probeSource.clip);
            return "gain " + gain.ToString("F2") + " rms " + rms.ToString("F4");
        }

        /// <summary>The game's own master volume (the saved preference, 0..1).</summary>
        [TestHook("SuperSoundSliderThingPlus.volume.game")]
        static double VolumeGame() { return Math.Round(SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Master(), 3); }

        /// <summary>Simulates the player confirming a new master volume in the game's settings (stores the preference only; the mod must notice it).</summary>
        [TestHook("SuperSoundSliderThingPlus.volume.setGame")]
        static string VolumeSetGame(double value)
        {
            PlatformPrefs.SetFloat(SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.MasterKey, (float)value);
            return "stored " + value;
        }

        /// <summary>One stored game volume by preference key (MasterVolume, SfxVolume, MusicVolume), 0..1.</summary>
        [TestHook("SuperSoundSliderThingPlus.volume.getKey")]
        static double VolumeGetKey(string key) { return Math.Round(SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Get(key), 3); }

        /// <summary>Simulates the game's own audio settings: stores one volume and applies it the way the game does (SFX: master x sfx straight into the mixer).</summary>
        [TestHook("SuperSoundSliderThingPlus.volume.setKey")]
        static string VolumeSetKey(string key, double value)
        {
            PlatformPrefs.SetFloat(key, (float)value);
            var g = SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Get;
            AudioMan.SetSFXVolume(g(SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.MasterKey) * g(SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.SfxKey));
            MusicMan.m_masterMusicVolume = g(SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.MasterKey) * g(SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.MusicKey);
            return "stored " + key + " " + value;
        }

        /// <summary>The music volume the game plays at right now (master x music).</summary>
        [TestHook("SuperSoundSliderThingPlus.volume.music")]
        static double VolumeMusic() { return Math.Round(MusicMan.m_masterMusicVolume, 3); }

        /// <summary>The mixer's SFX volume right now.</summary>
        [TestHook("SuperSoundSliderThingPlus.volume.mixer")]
        static double VolumeMixer() { return Math.Round(SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Sfx(), 3); }

        private static float _savedGame = float.NaN;

        /// <summary>Remembers the player's real master volume before a test changes it.</summary>
        [TestHook("SuperSoundSliderThingPlus.volume.save")]
        static double VolumeSave()
        {
            _savedGame = SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Master();
            return _savedGame;
        }

        /// <summary>Puts the remembered master volume back (and the mod follows it).</summary>
        [TestHook("SuperSoundSliderThingPlus.volume.restore")]
        static string VolumeRestore()
        {
            if (float.IsNaN(_savedGame)) return "Failed: nothing saved";
            SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Write(SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.MasterKey, _savedGame);
            SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Save();
            return "restored " + _savedGame.ToString("F3");
        }

        /// <summary>Is the mixer's SFX volume (game master x sfx) at zero?</summary>
        [TestHook("SuperSoundSliderThingPlus.volume.sfxSilent")]
        static bool VolumeSfxSilent() { return SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Sfx() < 0.001f; }

        /// <summary>Is the mixer's SFX volume audible (above zero)?</summary>
        [TestHook("SuperSoundSliderThingPlus.volume.sfxAudible")]
        static bool VolumeSfxAudible() { return SuperSoundSliderThingPlus.Integrations.Valheim.GameVolume.Sfx() > 0.001f; }

        /// <summary>EXPERIMENT: the RMS of the probe source's output block and the volume it has - tells whether AudioSource.GetOutputData is post-volume.</summary>
        [TestHook("SuperSoundSliderThingPlus.loudness.output")]
        static string LoudnessOutput()
        {
            if (!Alive()) return "Failed: no probe";
            var block = new float[1024];
            _probeSource.GetOutputData(block, 0);
            double sum = 0;
            for (int i = 0; i < block.Length; i++) sum += block[i] * block[i];
            string text = "vol " + _probeSource.volume.ToString("F4") + " rms " + Math.Sqrt(sum / block.Length).ToString("F5") + " playing " + _probeSource.isPlaying;
            ModLog.Info("SoundTestHooks: output probe " + text);
            return text;
        }

        /// <summary>Removes the probe (also done by the next Play).</summary>
        [TestHook("SuperSoundSliderThingPlus.sound.clear")]
        static bool Clear()
        {
            if (_cub != null)
            {
                ZNetView view = _cub.GetComponent<ZNetView>();
                if (view != null && ZNetScene.instance != null) ZNetScene.instance.Destroy(_cub); else UnityEngine.Object.Destroy(_cub);
                _cub = null;
            }
            if (_probe != null) UnityEngine.Object.Destroy(_probe);
            _probe = null;
            _probeSource = null;
            return _probe == null;
        }

        // network prefabs (creatures) are listed by the scene; plain effect prefabs are found among the loaded assets
        private static GameObject FindNetPrefab(string name) => ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;

        // some sounds are started by game code, not on awake (e.g. the shield wall loop): start them the way the game does
        private static void StartIfIdle(GameObject root)
        {
            foreach (ZSFX z in root.GetComponentsInChildren<ZSFX>())
            {
                if (!z.m_playOnAwake) z.Play();
            }
        }

        /// <summary>Does what the mod did to the probe match its factor (volume after the mod / volume the game set = the category factor)?</summary>
        [TestHook("SuperSoundSliderThingPlus.probe.scaledRatio")]
        static bool ProbeScaledRatio(double expected, double tolerance)
        {
            if (!Alive()) { ModLog.Info("SoundTestHooks: AUDIT no probe"); return false; }
            ZSFX z = _probe.GetComponentInChildren<ZSFX>();
            SoundCategory category;
            float raw, scaled;
            if (z == null || !SuperSoundSliderThingPlus.Integrations.Harmony.ZsfxCustomUpdatePatch.TryGetLast(z, out category, out raw, out scaled)) { ModLog.Info("SoundTestHooks: AUDIT " + _probe.name + " not seen by the patch yet"); return false; }
            if (raw <= 0.0005f) { ModLog.Info("SoundTestHooks: AUDIT " + _probe.name + " raw volume " + raw.ToString("F4") + " too low to judge"); return false; }
            double ratio = scaled / raw;
            bool ok = Math.Abs(ratio - expected) <= tolerance;
            ModLog.Info("SoundTestHooks: AUDIT " + _probe.name + " category " + category + " raw " + raw.ToString("F4") + " scaled " + scaled.ToString("F4") + " ratio " + ratio.ToString("F3") + " expected " + expected + " -> " + (ok ? "ok" : "WRONG"));
            return ok;
        }

        /// <summary>The shield dome hum source the shield-hum patch tracks (clip, base volume the game gave it, volume after the mod).</summary>
        [TestHook("SuperSoundSliderThingPlus.hum.info")]
        static string HumInfo()
        {
            float baseVolume, applied;
            string clip;
            if (!SuperSoundSliderThingPlus.Integrations.Harmony.ShieldHumPatch.TryGetLast(out baseVolume, out applied, out clip)) return "Failed: the patch has not seen the hum source";
            string text = "hum clip " + clip + " base " + baseVolume.ToString("F3") + " applied " + applied.ToString("F3");
            ModLog.Info("SoundTestHooks: HUM " + text);
            return text;
        }

        /// <summary>Is the hum's volume after the mod `expected` x the volume the game gave it?</summary>
        [TestHook("SuperSoundSliderThingPlus.hum.ratioNear")]
        static bool HumRatioNear(double expected, double tolerance)
        {
            float baseVolume, applied;
            string clip;
            if (!SuperSoundSliderThingPlus.Integrations.Harmony.ShieldHumPatch.TryGetLast(out baseVolume, out applied, out clip) || baseVolume <= 0.0005f) return false;
            double ratio = applied / baseVolume;
            bool ok = Math.Abs(ratio - expected) <= tolerance;
            ModLog.Info("SoundTestHooks: HUM " + clip + " base " + baseVolume.ToString("F3") + " applied " + applied.ToString("F3") + " ratio " + ratio.ToString("F3") + " expected " + expected + " -> " + (ok ? "ok" : "WRONG"));
            return ok;
        }

        /// <summary>Starts the all-sounds sweep (every sound prefab is played for a few frames; one SWEEP line per sound in the log).</summary>
        [TestHook("SuperSoundSliderThingPlus.sweep.start")]
        static bool SweepStart() { return SoundSweeper.Begin(); }

        /// <summary>Has the sweep finished?</summary>
        [TestHook("SuperSoundSliderThingPlus.sweep.done")]
        static bool SweepDone() { return SoundSweeper.IsDone(); }

        /// <summary>The sweep's result line.</summary>
        [TestHook("SuperSoundSliderThingPlus.sweep.summary")]
        static string SweepSummary() { return SoundSweeper.State(); }

        private static bool Alive() => _probe != null && _probeSource != null;

        private static string Describe() => Alive() ? _probe.name + " playing " + _probeSource.isPlaying + " volume " + _probeSource.volume.ToString("F4") : "no probe";

        private static bool TryParse(string name, out SoundSlider slider) => Enum.TryParse(name, true, out slider);

        // effect prefabs are plain assets (no ZNetView), so ZNetScene does not list them: take one of the loaded GameObject assets by name
        private static GameObject FindPrefab(string name)
        {
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == name && !go.scene.IsValid()) return go;
            }
            return null;
        }
    }
}
