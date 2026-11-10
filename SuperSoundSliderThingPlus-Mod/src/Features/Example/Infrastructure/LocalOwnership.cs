using KydlCore.Core;

namespace SuperSoundSliderThingPlus.Features.Example.Infrastructure
{
    /// <summary>EXAMPLE: the example value is a local setting, so this client always owns it. A networked feature reads its ZNetView / ZDO here.</summary>
    internal sealed class LocalOwnership : IOwnership
    {
        public bool IsOwner { get { return IsLocal; } }
        public long OwnerId { get { return 0L; } }
        const bool IsLocal = true;
    }
}
