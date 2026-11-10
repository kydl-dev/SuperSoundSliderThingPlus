using System;
using System.Collections.Generic;

namespace SuperSoundSliderThingPlus.Ui
{
    /// <summary>One slider row of the sound tab: the text key of its label, its mute level (percent) and the callback for a change.</summary>
    internal sealed class SoundSliderRow
    {
        internal string LabelKey { get; }
        internal float Value { get; }
        /// <summary>Called when the player moves the slider (the mod turns it into a command).</summary>
        internal Action<float> OnValue { get; }

        internal SoundSliderRow(string labelKey, float value, Action<float> onValue)
        {
            LabelKey = labelKey;
            Value = value;
            OnValue = onValue ?? (v => { });
        }
    }

    /// <summary>
    /// The data contract of the sound tab (MIGRATION_DECISIONS A6): what SoundTabView draws. Filled by the mod's
    /// src/Presentation/Hub/ViewModels/SoundPageModel (game) or by MenuPreview (Unity). Plain data + callbacks, no game types.
    /// The first row is the master; every value is a mute level in percent (0 = full loudness, 100 = muted).
    /// </summary>
    internal sealed class SoundPage
    {
        internal const float Min = 0f, Max = 100f;
        internal IReadOnlyList<SoundSliderRow> Rows { get; }

        internal SoundPage(IReadOnlyList<SoundSliderRow> rows)
        {
            Rows = rows ?? new List<SoundSliderRow>();
        }
    }
}
