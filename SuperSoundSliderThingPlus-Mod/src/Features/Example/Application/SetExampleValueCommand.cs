using KydlCore.Core;

namespace SuperSoundSliderThingPlus.Features.Example.Application
{
    /// <summary>EXAMPLE command: the player (hub slider) or a test hook asks for a new example value. Immutable data.</summary>
    internal sealed class SetExampleValueCommand : ICommand<float>
    {
        internal float Requested { get; }
        internal SetExampleValueCommand(float requested) { Requested = requested; }
    }
}
