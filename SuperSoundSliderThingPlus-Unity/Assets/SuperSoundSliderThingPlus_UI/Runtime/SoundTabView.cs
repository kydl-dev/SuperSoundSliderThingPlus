using System;
using KydlCore.Ui;
using UnityEngine.UIElements;

namespace SuperSoundSliderThingPlus.Ui
{
    /// <summary>
    /// WHAT the sound tab shows in the KY hub (UI Toolkit, shared KydlCore.Ui look): the master mute slider on top, then one slider per
    /// category. Compiled into the mod AND shown by the Unity preview (MenuPreview). Returns only the page content: the hub draws the window.
    /// </summary>
    internal static class SoundTabView
    {
        /// <summary>Builds the page content. `text` turns a text key into the shown text; `page` = the rows (values + callbacks).</summary>
        internal static VisualElement Build(Func<string, string> text, SoundPage page)
        {
            var c = new VisualElement();
            c.Add(UiControls.Heading(text("supersoundsliderthingplus_tab_sound_section")));
            c.Add(UiControls.Note(text("supersoundsliderthingplus_tab_sound_note")));
            foreach (SoundSliderRow row in page.Rows)
            {
                SoundSliderRow captured = row;
                c.Add(UiControls.Row(text(captured.LabelKey), UiControls.Slider(SoundPage.Min, SoundPage.Max, captured.Value, "0", v => captured.OnValue(v))));
            }
            UiLog.Verbose("SoundTabView: built " + page.Rows.Count + " slider row(s)");
            return c;
        }
    }
}
