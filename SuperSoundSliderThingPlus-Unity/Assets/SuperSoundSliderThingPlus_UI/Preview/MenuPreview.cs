using KydlCore.Ui;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SuperSoundSliderThingPlus.Ui
{
    /// <summary>
    /// Unity-only preview of a tab of the mod in the KY hub (NOT compiled into the mod: only Runtime/ is). Add this component to any GameObject of an empty
    /// scene and press Play: the menu opens in the Game view in the same look as in game (game fonts from Assets/KydlCore.Ui/Preview/GameAssets).
    /// Set the Game view to 1920x1080 (the panel scales from that like in game).
    /// </summary>
    public sealed class MenuPreview : MonoBehaviour
    {
        const string FontFolder = "Assets/KydlCore.Ui/Preview/GameAssets/";

        void OnEnable()
        {
            LoadGameFonts();
            UiKit.Reset();
            var root = UiKit.Root;
            if (root == null) { Debug.LogError("[MenuPreview] UI Toolkit panel could not be made"); return; }
            // in game the hub (KydlCore) draws the window around a tab page; the preview draws a plain window with the same kit
            var window = UiKit.Window(UiText.English("supersoundsliderthingplus_menu_title"), UiText.English("supersoundsliderthingplus_menu_footer"), () => Debug.Log("[MenuPreview] close pressed"), out UiScroll body);
            body.Content.Add(SoundTabView.Build(UiText.English, SamplePage()));
            root.Add(window);
            Debug.Log("[MenuPreview] SuperSoundSliderThingPlus menu shown");
        }

        void OnDisable() { UiKit.Reset(); }

        // sample values for the preview: master 20%, the categories spread out so every slider position is visible
        static SoundPage SamplePage()
        {
            var keys = new[] { "master", "effects", "weather", "wolfpup", "shieldgenerator", "chickenlove" };
            var values = new[] { 20f, 0f, 50f, 100f, 25f, 75f };
            var rows = new System.Collections.Generic.List<SoundSliderRow>();
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                rows.Add(new SoundSliderRow("supersoundsliderthingplus_slider_" + key, values[i], v => UiLog.Verbose("MenuPreview: " + key + " mute " + v)));
            }
            return new SoundPage(rows);
        }

        // UiKit finds the game fonts by name among the loaded fonts (Norse, Norsebold, NotoEmoji-Regular): load them first.
        static void LoadGameFonts()
        {
#if UNITY_EDITOR
            foreach (var f in new[] { "Norse.otf", "Norsebold.otf", "NotoEmoji-Regular.ttf" })
                if (AssetDatabase.LoadAssetAtPath<Font>(FontFolder + f) == null) Debug.LogWarning("[MenuPreview] font missing: " + FontFolder + f + " (Kydl > Sync shared UI kit)");
#endif
        }
    }
}
