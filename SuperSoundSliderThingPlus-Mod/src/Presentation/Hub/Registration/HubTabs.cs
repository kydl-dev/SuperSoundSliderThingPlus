using System;
using BepInEx.Configuration;
using KydlCore.Common;
using SuperSoundSliderThingPlus.Bootstrap;
using SuperSoundSliderThingPlus.Ui;

namespace SuperSoundSliderThingPlus.Presentation.Hub.Registration
{
    /// <summary>
    /// What this mod puts into the KY hub of KydlCore (F6) - the mod DECLARES its page here, KydlCore draws it (MIGRATION_DECISIONS B11).
    /// There is NO menu key, window or input lock in this mod: KydlCore owns them. Row 1 of the hub shows this mod (SetModTitle), row 2 its tabs.
    /// The settings tab is always there; a feature's own tab is added by its Start and removed by its Stop (FeatureRegistry), so a feature that is
    /// switched off leaves no stale tab behind. What a tab DRAWS is a view in the Unity project (SuperSoundSliderThingPlus-Unity/Assets/SuperSoundSliderThingPlus_UI/Runtime);
    /// what it SHOWS (values, callbacks) comes from src/Presentation/Hub/ViewModels.
    /// </summary>
    internal static class HubTabs
    {
        internal const string ExampleId = "example", SoundId = "sound", SettingsId = "settings";
        const int ExampleOrder = 1, SoundOrder = 0, SettingsOrder = 100;

        /// Once from ModInstaller.Install: the mod's title and its settings tab. Expected outcome: the settings tab is registered.
        internal static bool RegisterModPage(ConfigFile config)
        {
            global::KydlCore.Features.Hub.Contracts.KyHub.SetModTitle(Plugin.ModGuid, Plugin.ModName);
            var settings = global::KydlCore.Features.Hub.Contracts.KyHub.AddTab(Plugin.ModGuid, SettingsId, Loc.T("supersoundsliderthingplus_tab_settings"), () => global::KydlCore.Features.Hub.Contracts.KyHub.ConfigEntries(config), SettingsOrder);
            ModLog.Verbose("HubTabs: title + settings tab " + (settings != null ? "registered" : "FAILED"));
            return settings != null;
        }

        /// The sound feature's tab (its Start): the master and category mute sliders. `page` builds the view model each time the tab is opened. Expected outcome: the tab is registered.
        internal static bool AddSoundTab(Func<SoundPage> page)
        {
            var tab = global::KydlCore.Features.Hub.Contracts.KyHub.AddTab(Plugin.ModGuid, SoundId, Loc.T("supersoundsliderthingplus_tab_sound"), () => SoundTabView.Build(k => Loc.T(k), page()), SoundOrder);
            ModLog.Verbose("HubTabs: sound tab " + (tab != null ? "registered" : "FAILED"));
            return tab != null;
        }

        /// The sound feature's Stop. Expected outcome: the tab is gone (true also when it was never there).
        internal static bool RemoveSoundTab() { return RemoveTab(SoundId); }

        /// The example feature's tab (its Start). `page` builds the view model each time the tab is opened. Expected outcome: the tab is registered.
        internal static bool AddExampleTab(Func<ExamplePage> page)
        {
            var tab = global::KydlCore.Features.Hub.Contracts.KyHub.AddTab(Plugin.ModGuid, ExampleId, Loc.T("supersoundsliderthingplus_tab_example"), () => ExampleTabView.Build(k => Loc.T(k), page()), ExampleOrder);
            ModLog.Verbose("HubTabs: example tab " + (tab != null ? "registered" : "FAILED"));
            return tab != null;
        }

        /// The example feature's Stop. Expected outcome: the tab is gone (true also when it was never there).
        internal static bool RemoveExampleTab() { return RemoveTab(ExampleId); }

        /// Mod unload: every tab of this mod is removed. Expected outcome: the settings tab is gone.
        internal static bool UnregisterModPage() { return RemoveTab(SettingsId); }

        static bool RemoveTab(string id)
        {
            bool removed = global::KydlCore.Features.Hub.Contracts.KyHub.RemoveTab(Plugin.ModGuid, id);
            bool gone = !global::KydlCore.Features.Hub.Contracts.KyHub.HasTab(Plugin.ModGuid, id);
            ModLog.Verbose("HubTabs: tab '" + id + "' " + (removed ? "removed" : "was not registered") + (gone ? "" : " - STILL THERE"));
            return gone;
        }
    }
}
