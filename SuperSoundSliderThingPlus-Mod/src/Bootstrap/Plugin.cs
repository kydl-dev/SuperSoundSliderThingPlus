using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using KydlCore.Common;
using UnityEngine;
using SuperSoundSliderThingPlus.Configuration;

namespace SuperSoundSliderThingPlus.Bootstrap
{
    /// <summary>
    /// SuperSoundSliderThingPlus: TBD - ask the user and update this line.
    /// Entry point. Order in Awake matters: log first, then the config (player settings), then text, then everything that uses them.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    // HARD dependency on KydlCore (T-070): BepInEx loads KydlCore first and refuses to load this mod without it (or with an older one).
    // The version here = the KydlCore version in manifest.json (a test checks it). Raise both together when the mod needs a newer KydlCore.
    [BepInDependency(KydlCore.Bootstrap.Plugin.ModGuid, "1.2.0")]
    // MIGRATION_DECISIONS "Network compatibility": exact versions (Patch). A mod that changes shared world state = EveryoneMustHaveMod;
    // a client-only mod (camera / view) = VersionCheckOnly (joins servers without it; same version when the server has it). Reason in docs\Dependencies.md.
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
    public class Plugin : BaseUnityPlugin
    {
        // ModGuid never changes after the first release. ModVersion = manifest.json version_number = newest CHANGELOG heading (tests check it).
        public const string ModGuid = "com.kydldev.SuperSoundSliderThingPlus";
        public const string ModName = "SuperSoundSliderThingPlus";
        public const string ModVersion = "0.1.0";
        public const string BundleName = "supersoundsliderthingplus";

        internal static Plugin Instance;
        /// This mod's AssetBundle (null while the mod has none yet: Unity has not built one).
        internal static AssetBundle Bundle;
        ConfigReloader _cfgWatcher;
        ModInstaller _installer;

        void Awake()
        {
            Instance = this;
            ModLog.Init(Logger, ModName, () => ModSettings.DebugLog != null && ModSettings.DebugLog.Value);
            ModLog.Info(ModName + " " + ModVersion + " loaded (dll built " + File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-dd HH:mm:ss") + ")");

            // the UI kit, its log, the input lock and the F6 hub belong to KydlCore: this mod sets no log sink / owner / lock and has no menu key
            ModSettings.Bind(Config);
            _cfgWatcher = new ConfigReloader(Config);
            Loc.Register(Ui.UiText.En);       // Ui. = SuperSoundSliderThingPlus.Ui (the Unity project's views and texts, linked into the mod)

            Bundle = LoadBundle();
            // Harmony patches live in src/Integrations/Harmony (MIGRATION_DECISIONS A5); each one asks the FeatureRegistry whether its feature runs
            new Harmony(ModGuid).PatchAll(Assembly.GetExecutingAssembly());
            _installer = new ModInstaller(Config);
            bool installed = _installer.Install();          // features + the mod's KY hub page (F6), started per their Enable switches
            Config.SettingChanged += OnSettingChanged;      // a player's config change switches features at once (PROJECT_RULES 17)
            ModLog.Verbose("Plugin: Awake finished (architecture " + Core.Architecture.Tag + ", Harmony patches applied, installer " + (installed ? "ok" : "had failures") + ")");
        }

        void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            ModLog.Verbose("Plugin: config [" + e.ChangedSetting.Definition.Section + "] " + e.ChangedSetting.Definition.Key + " changed - refreshing the features");
            if (_installer != null) _installer.Refresh();
        }

        // 1) embedded in the DLL (Kydl.Mod.targets embeds Assets\<bundle>; read fully into memory: LoadFromStream breaks once the stream
        //    is disposed), 2) the file next to the DLL (the Gale test profile gets both). No bundle at all is normal for a new mod.
        static AssetBundle LoadBundle()
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var res in asm.GetManifestResourceNames())
            {
                if (res != BundleName && !res.EndsWith("." + BundleName)) continue;
                using (var stream = asm.GetManifestResourceStream(res))
                using (var ms = new MemoryStream())
                {
                    if (stream != null) stream.CopyTo(ms);
                    var b = AssetBundle.LoadFromMemory(ms.ToArray());
                    if (b != null) { ModLog.Verbose("Plugin: bundle '" + BundleName + "' loaded from the DLL (" + b.GetAllAssetNames().Length + " asset(s))"); return b; }
                    ModLog.Warn("Plugin: the embedded bundle '" + res + "' could not be loaded");
                }
            }
            var path = Path.Combine(Path.GetDirectoryName(asm.Location) ?? "", BundleName);
            if (!File.Exists(path)) { ModLog.Verbose("Plugin: no bundle '" + BundleName + "' (not embedded, not at " + path + ") - fine until the mod has assets"); return null; }
            var fromFile = AssetBundle.LoadFromFile(path);
            if (fromFile == null) ModLog.Warn("Plugin: bundle file " + path + " could not be loaded");
            else ModLog.Verbose("Plugin: bundle loaded from " + path);
            return fromFile;
        }

        void Update()
        {
            if (_cfgWatcher != null) _cfgWatcher.Tick();
        }

        void OnDestroy()
        {
            Config.SettingChanged -= OnSettingChanged;
            if (_installer != null) { _installer.Uninstall(); _installer = null; }
            if (_cfgWatcher != null) { _cfgWatcher.Dispose(); _cfgWatcher = null; }
            ModLog.Verbose("Plugin: destroyed");
        }
    }
}
