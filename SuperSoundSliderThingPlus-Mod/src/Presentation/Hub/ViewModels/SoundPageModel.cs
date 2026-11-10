using System.Collections.Generic;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Infrastructure;
using SuperSoundSliderThingPlus.Presentation.Hub.Commands;
using SuperSoundSliderThingPlus.Ui;

namespace SuperSoundSliderThingPlus.Presentation.Hub.ViewModels
{
    /// <summary>
    /// WHAT the sound tab shows (MIGRATION_DECISIONS A6): built from the config every time the tab opens - master first, then one row per
    /// category. The data contract (<see cref="SoundPage"/>) lives in the Unity project's Runtime folder so the preview can fill it too.
    /// A UI change goes through the command dispatcher, never straight into the config.
    /// </summary>
    internal static class SoundPageModel
    {
        private static readonly SoundSlider[] Order =
        {
            SoundSlider.Master, SoundSlider.Effects, SoundSlider.Weather, SoundSlider.WolfPup, SoundSlider.ShieldGenerator, SoundSlider.ChickenLove
        };

        internal static SoundPage Build(CommandDispatcher commands)
        {
            var rows = new List<SoundSliderRow>(Order.Length);
            foreach (SoundSlider slider in Order)
            {
                SoundSlider captured = slider;
                rows.Add(new SoundSliderRow(LabelKey(slider), SoundLevels.MuteOf(slider), v => SoundCommands.SetMute(commands, captured, v)));
            }
            return new SoundPage(rows);
        }

        /// <summary>The text key of a slider's label (UiText).</summary>
        internal static string LabelKey(SoundSlider slider) => "supersoundsliderthingplus_slider_" + slider.ToString().ToLowerInvariant();
    }
}
