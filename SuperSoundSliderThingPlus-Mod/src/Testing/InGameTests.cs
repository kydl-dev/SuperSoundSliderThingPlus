using KydlCore.Common;
using SuperSoundSliderThingPlus.Bootstrap;
using SuperSoundSliderThingPlus.Configuration;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Infrastructure;

namespace SuperSoundSliderThingPlus.Testing
{
    /// <summary>
    /// In-game tests run by the user's Kydl.GameProbe (dev tool, never a dependency: it finds this class by its NAME `InGameTests`).
    ///   Startup_*  run when the main menu is reached;   World_*  run 5 s after a world is loaded; both again on GameProbe's RetestKey (PageDown).
    /// Each method: static, no parameters, returns a text starting with PASS / FAIL / SKIP and the detail with numbers + units,
    /// e.g. "FAIL: coil radius 0.900 m, expected 0.180 m". null or an exception counts as FAIL. Results land in the session report.
    /// Keep them cheap and read-only: they also run in players' games when GameProbe is installed there (it never is for players).
    /// </summary>
    internal static class InGameTests
    {
        /// The player config is bound and the mod is switched on.
        static string Startup_ConfigBound()
        {
            if (ModSettings.EnableAll == null) return "FAIL: ModSettings.Bind did not run (EnableAll is null)";
            return ModSettings.On ? "PASS: EnableAll on" : "SKIP: EnableAll is off in the config";
        }

        /// The installer ran and the feature registry holds the mod's features (each started per its Enable switch).
        static string Startup_FeaturesInstalled()
        {
            var inst = ModInstaller.Current;
            if (inst == null) return "FAIL: ModInstaller.Install did not run";
            int running = 0;
            foreach (var f in inst.Features.All) if (inst.Features.IsRunning(f.Id)) running++;
            ModLog.Verbose("InGameTests: Startup_FeaturesInstalled " + inst.Features.Count + " feature(s), " + running + " running");
            return inst.Features.Count > 0 ? "PASS: " + inst.Features.Count + " feature(s), " + running + " running" : "FAIL: no feature registered";
        }

        /// KydlCore (hard dependency, T-070) is loaded: it owns the KY hub (F6) with this mod's tabs.
        static string Startup_CoreLoaded()
        {
            bool loaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(KydlCore.Bootstrap.Plugin.ModGuid);
            ModLog.Verbose("InGameTests: Startup_CoreLoaded " + loaded);
            return loaded ? "PASS: " + KydlCore.Bootstrap.Plugin.ModGuid + " is loaded" : "FAIL: " + KydlCore.Bootstrap.Plugin.ModGuid + " is not loaded (this mod requires it)";
        }

        /// Every category's live factor equals (1 - master) x (1 - category) from the config (D-011): the config, SoundLevels and the maths agree.
        static string Startup_SoundFactorsMatchTheConfig()
        {
            if (ModSettings.MasterMute == null) return "FAIL: the slider config is not bound";
            var feature = SoundFeature.Instance;
            if (feature == null || !feature.Running) return "SKIP: the sound feature is not running (mod off)";
            float master = ModSettings.MasterMute.Value;
            string worst = string.Empty;
            foreach (SoundCategory c in System.Enum.GetValues(typeof(SoundCategory)))
            {
                float expected = (1f - master / 100f) * (1f - SoundLevels.MuteOf(c) / 100f);
                float actual = SoundLevels.Factor(c);
                ModLog.Verbose("InGameTests: Startup_SoundFactorsMatchTheConfig " + c + " factor " + actual.ToString("F3") + ", expected " + expected.ToString("F3"));
                if (System.Math.Abs(actual - expected) > 0.001f) worst = c + " factor " + actual.ToString("F3") + ", expected " + expected.ToString("F3");
            }
            return worst.Length == 0 ? "PASS: master " + master.ToString("F0") + "% - every category factor matches" : "FAIL: " + worst;
        }

        /// 5 s into a world the patches have classified real sounds; the detail lists how many distinct sounds landed in each slider.
        static string World_SoundsAreClassified()
        {
            int distinct = SoundRecorder.Distinct;
            ModLog.Verbose("InGameTests: World_SoundsAreClassified " + distinct + " distinct sound(s): " + SoundRecorder.Summary());
            return distinct > 0 ? "PASS: " + distinct + " distinct sound(s) classified (" + SoundRecorder.Summary() + ")" : "FAIL: no sound was classified 5 s into the world (are the ZSFX patches applied?)";
        }

        /// The local player exists once a world is loaded (example of a World_ test; replace with the mod's own checks).
        static string World_PlayerPresent()
        {
            var p = Player.m_localPlayer;
            ModLog.Verbose("InGameTests: World_PlayerPresent player " + (p != null ? p.GetPlayerName() : "null"));
            return p != null ? "PASS: " + p.GetPlayerName() + " at " + p.transform.position.ToString("F1") : "FAIL: no local player 5 s after the world loaded";
        }
    }
}
