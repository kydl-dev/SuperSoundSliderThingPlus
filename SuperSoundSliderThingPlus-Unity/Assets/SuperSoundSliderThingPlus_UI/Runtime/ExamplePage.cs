using System;

namespace SuperSoundSliderThingPlus.Ui
{
    /// <summary>
    /// The data contract of the example tab (MIGRATION_DECISIONS A6): what ExampleTabView draws. Filled by the mod's
    /// src/Presentation/Hub/ViewModels/ExamplePageModel (game) or by MenuPreview (Unity). Plain data + callbacks, no game types.
    /// </summary>
    internal sealed class ExamplePage
    {
        internal float Value { get; }
        internal float Min { get; }
        internal float Max { get; }
        /// Called when the player moves the slider (the mod turns it into a command).
        internal Action<float> OnValue { get; }

        internal ExamplePage(float value, float min, float max, Action<float> onValue)
        {
            Value = value; Min = min; Max = max;
            OnValue = onValue ?? (v => { });
        }
    }
}
