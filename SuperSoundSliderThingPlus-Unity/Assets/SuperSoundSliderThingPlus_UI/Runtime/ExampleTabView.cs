using System;
using KydlCore.Ui;
using UnityEngine.UIElements;

namespace SuperSoundSliderThingPlus.Ui
{
    /// <summary>
    /// WHAT a tab of the mod shows in the KY hub (UI Toolkit, shared KydlCore.Ui look). Compiled into the mod AND shown by the Unity preview
    /// (Assets/SuperSoundSliderThingPlus_UI/Preview/MenuPreview.cs: enter Play mode in the Preview scene / any scene with that component).
    /// It returns only the PAGE CONTENT: the hub (KydlCore) draws the window, title, mod and tab rows and the close button, so no window here.
    /// Keep game types (Player, ZNet, ...) OUT of this folder: the Unity project does not have them. The game side (which tab, game data)
    /// is src/Presentation/Hub (Registration/HubTabs.cs, ViewModels) in the mod; data comes in through ExamplePage.
    /// Ready-made pieces in this folder (from WhipItUp): TabBar (tabs inside a page), ModelViewportView + ModelViewerRig (3D view of a
    /// model you can turn / move / zoom; game side: src/Integrations/Unity/ViewerScene.Make), DetailsView + DetailGroup / DetailRow (stat blocks with quality columns).
    /// </summary>
    internal static class ExampleTabView
    {
        /// Builds the page content. `text` turns a text key into the shown text (game: the mod's localization, Unity: English);
        /// `page` = what to show (values + callbacks, from the mod's view model).
        internal static VisualElement Build(Func<string, string> text, ExamplePage page)
        {
            var c = new VisualElement();
            c.Add(UiControls.Heading(text("supersoundsliderthingplus_tab_example_section")));
            c.Add(UiControls.Note(text("supersoundsliderthingplus_tab_example_note")));
            c.Add(UiControls.Row(text("supersoundsliderthingplus_tab_example_slider"), UiControls.Slider(page.Min, page.Max, page.Value, "0.0", v => page.OnValue(v))));
            UiLog.Verbose("ExampleTabView: built");
            return c;
        }
    }
}
