using KydlCore.Core;

namespace SuperSoundSliderThingPlus.Features.Sound.Infrastructure
{
    /// <summary>The sliders are local player settings (client-side mod, D-006): this client always owns them.</summary>
    internal sealed class LocalSoundOwnership : IOwnership
    {
        private const bool Local = true;
        public bool IsOwner { get { return Local; } }
        public long OwnerId { get { return 0L; } }
    }
}
