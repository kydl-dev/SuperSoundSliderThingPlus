using System;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Features.Sound.Execution
{
    /// <summary>
    /// Checks ownership FIRST, then runs the side effect through a delegate bound once in the constructor. Game-free; the delegate is a
    /// method of the Infrastructure adapter (it writes the config entry).
    /// </summary>
    internal sealed class MuteExecutor
    {
        internal const string NotOwner = "not the owner", Applied = "applied";
        private readonly IOwnership _owner;
        private readonly Action<SoundSlider, float> _apply;

        internal MuteExecutor(IOwnership owner, Action<SoundSlider, float> apply)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            if (apply == null) throw new ArgumentNullException("apply");
            _owner = owner;
            _apply = apply;
        }

        /// <summary>Expected outcome: Success with the applied level; Rejected (nothing applied) when this client is not the owner.</summary>
        internal OperationResponse<float> Apply(SoundSlider slider, float level)
        {
            if (!_owner.IsOwner) return OperationResponse<float>.Rejected(NotOwner);
            _apply(slider, level);
            return OperationResponse<float>.Success(level, Applied);
        }
    }
}
