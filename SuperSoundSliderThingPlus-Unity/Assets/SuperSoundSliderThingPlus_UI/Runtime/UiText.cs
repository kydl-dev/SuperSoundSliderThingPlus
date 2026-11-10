using System.Collections.Generic;

namespace SuperSoundSliderThingPlus.Ui
{
    /// <summary>
    /// Player-facing text of the mod, English only (user rule: no other languages). Shared by the game (registered with Jotunn in Plugin.Awake through KydlCore.Common.Loc)
    /// and the Unity preview (English). Every key starts with "supersoundsliderthingplus_" so it never clashes with another mod.
    /// </summary>
    internal static class UiText
    {
        internal static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "supersoundsliderthingplus_menu_title", "SuperSoundSliderThingPlus" },
            { "supersoundsliderthingplus_menu_footer", "Esc to close" },
            { "supersoundsliderthingplus_tab_example", "Example" },
            { "supersoundsliderthingplus_tab_settings", "Settings" },
            { "supersoundsliderthingplus_tab_example_section", "Example" },
            { "supersoundsliderthingplus_tab_example_note", "This is the starting tab of the mod in the KY hub (F6). Replace its content in ExampleTabView." },
            { "supersoundsliderthingplus_tab_example_slider", "Example value" },
        };


        /// English text (the Unity preview; the game reads the same table through Jotunn).
        internal static string English(string key)
        {
            string s;
            return En.TryGetValue(key, out s) ? s : key;
        }
    }
}
