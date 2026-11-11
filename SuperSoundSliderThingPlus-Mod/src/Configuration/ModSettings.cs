using BepInEx.Configuration;
using KydlCore.Common;

namespace SuperSoundSliderThingPlus.Configuration
{
    /// <summary>
    /// All settings in one place (user rule, every mod):
    ///  * PLAYER CONFIG (BepInEx entries below, bound once from Plugin.Awake): only what a player should change. Clear descriptions, ranges,
    ///    drop-downs (enums). Descriptions never mention older versions, old names or old values.
    ///  * DEV-ONLY values (Tunable fields below): fixed values chosen by the user, NOT in the player's file. The user's KydlTools mod (F7) finds
    ///    every static Tunable here by itself and lets the user change it in game - nothing to register.
    /// When the user settles a value in game, it becomes the new fixed value / default here.
    /// </summary>
    internal static class ModSettings
    {
        // [General]
        public static ConfigEntry<bool> EnableAll;
        // no MenuKey: the mod's menu is a set of tabs in the KY hub (F6, KydlCore), see src/Presentation/Hub/Registration/HubTabs.cs
        // One functionality = one config section with its own switch (PROJECT_RULES 11), only when the mod has several of them, e.g.:
        //   public static ConfigEntry<bool> ColoursEnable;   // c.Bind("Colours", "Enable", true, "...");  on only when On && ColoursEnable.Value
        // [General] the sliders (D-004, D-011): every one is a MUTE level in percent, 0 = full loudness, 100 = muted; the master scales all others
        public static ConfigEntry<float> MasterMute, EffectsMute, WeatherMute, WolfPupMute, ShieldGeneratorMute, ChickenLoveMute, PortalMute, MusicMute;
        // [General] matching (D-005 step 2): off by default, the player switches it on
        public static ConfigEntry<bool> EqualizeLoudness, DefaultsApplied;
        // [DebugLog]
        public static ConfigEntry<bool> DebugLog;

        /// Is the mod switched on ([General] EnableAll)? Off = inert: no effects; nothing built is removed.
        public static bool On { get { return EnableAll == null || EnableAll.Value; } }

        /// The gate of one feature (IFeature.IsEnabled): EnableAll AND its own [<Feature>] Enable (null = a mod with one feature: EnableAll only).
        /// Read at call time, never cached, so a config change applies at once.
        public static bool FeatureOn(ConfigEntry<bool> ownEnable) { return On && (ownEnable == null || ownEnable.Value); }

        // --- dev-only fixed values (Tunables): not in the config file, KydlTools changes them for testing -----------------------------------
        public static readonly Tunable<float> ExampleSpeed = new Tunable<float>("Example", "ExampleSpeed", 1f, 0f, 10f,
            "Example dev-only value (0-10, with a range as PROJECT_RULES 11 asks): replace or remove. Read it with ModSettings.ExampleSpeed.Value.");

        public static readonly Tunable<float> OwnerRadius = new Tunable<float>("Sound", "OwnerRadius", 1.5f, 0.1f, 5f,
            "Dev-only (metres, 0.1-5): a sound belongs to the creature standing within this distance of it (tells a wolf pup's sounds from an adult's).");

        public static readonly Tunable<float> TargetRms = new Tunable<float>("Loudness", "TargetRms", 0.1f, 0.01f, 0.5f,
            "Dev-only (RMS 0.01-0.5): the loudness every clip is scaled toward while Equalize is on.");
        public static readonly Tunable<float> MinGain = new Tunable<float>("Loudness", "MinGain", 0.25f, 0.05f, 1f,
            "Dev-only (0.05-1): equalizing never cuts a clip below this factor.");
        public static readonly Tunable<float> MaxGain = new Tunable<float>("Loudness", "MaxGain", 3f, 1f, 8f,
            "Dev-only (1-8): equalizing never boosts a clip above this factor (a very quiet clip stays quiet instead of turning into noise).");
        public static readonly Tunable<float> MeasureSeconds = new Tunable<float>("Loudness", "MeasureSeconds", 1f, 0.1f, 5f,
            "Dev-only (seconds 0.1-5): the length of the window from the middle of a clip that is measured.");

        /// Expected outcome: every player entry is bound (returns that check).
        public static bool Bind(ConfigFile c)
        {
            var savedLines = ConfigTools.Capture(c);     // before the first Bind: every Bind saves the file with what BepInEx parsed

            EnableAll = c.Bind("General", "EnableAll", true,
                "Switches the whole mod on / off while playing. Off: no effects from this mod; things already built stay in the world.");
            DebugLog = c.Bind("DebugLog", "Enable", false,
                "Writes detailed lines about everything the mod does to the BepInEx log (for bug reports). Leave off while playing normally.");

            MasterMute = BindMute(c, "MasterMute", "Master mute level in percent: 0 = every sound at full loudness, 100 = everything muted. Scales all the sliders below.");
            EffectsMute = BindMute(c, "SoundEffectsMute", "Mute level in percent for all sound effects that have no slider of their own (the game's own sound effects slider follows it): 0 = full loudness, 100 = muted.");
            WeatherMute = BindMute(c, "WeatherMute", "Mute level in percent for world weather sounds (rain, wind, thunder): 0 = full loudness, 100 = muted.");
            WolfPupMute = BindMute(c, "WolfPupMute", "Mute level in percent for wolf pup sounds: 0 = full loudness, 100 = muted.");
            ShieldGeneratorMute = BindMute(c, "ShieldGeneratorMute", "Mute level in percent for shield generator sounds: 0 = full loudness, 100 = muted.");
            PortalMute = BindMute(c, "PortalMute", "Mute level in percent for portal sounds: 0 = full loudness, 100 = muted.");
            EqualizeLoudness = c.Bind("General", "EqualizeLoudness", false, "Match the loudness of sounds: every sound clip is measured once and scaled toward one target loudness, so loud sounds come down and quiet ones come up. Off = the game's own balance.");
            DefaultsApplied = c.Bind("General", "DefaultsApplied", false, "Set to true once the first world entry has set the category sliders to 50%. Set it to false to apply those starting values once more.");
            MusicMute = BindMute(c, "MusicMute", "Mute level in percent for the music (the game's own music slider): 0 = full loudness, 100 = muted.");
            ChickenLoveMute = BindMute(c, "ChickenLoveMute", "Mute level in percent for the chicken love sound: 0 = full loudness, 100 = muted.");

            ConfigTools.ApplyCommaFix(c, savedLines);    // after every Bind: "3,5" typed by hand means 3.5, not 35
            ConfigTools.DropRemoved(c);

            EnableAll.SettingChanged += (s, e) => ModLog.Info("Config: EnableAll = " + EnableAll.Value + (EnableAll.Value ? ", the mod is on" : ", the mod is inert (no effects)"));
            ModLog.Verbose("Config: bound (EnableAll " + EnableAll.Value + ", debug log " + DebugLog.Value + ")");
            return EnableAll != null && DebugLog != null && MasterMute != null && ChickenLoveMute != null && DefaultsApplied != null;
        }

        private static ConfigEntry<float> BindMute(ConfigFile c, string key, string description)
            => c.Bind("General", key, 0f, new ConfigDescription(description, new AcceptableValueRange<float>(0f, 100f)));
    }
}
