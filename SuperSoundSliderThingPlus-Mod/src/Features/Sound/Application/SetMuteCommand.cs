using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Features.Sound.Application
{
    /// <summary>The player (hub slider) or a test hook asks for a new mute level of one slider. Immutable data.</summary>
    internal sealed class SetMuteCommand : ICommand<float>
    {
        internal SoundSlider Slider { get; }
        internal float Requested { get; }
        internal SetMuteCommand(SoundSlider slider, float requested) { Slider = slider; Requested = requested; }
    }
}
