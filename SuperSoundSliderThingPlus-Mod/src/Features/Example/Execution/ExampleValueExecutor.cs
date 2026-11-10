using System;
using KydlCore.Core;

namespace SuperSoundSliderThingPlus.Features.Example.Execution
{
    /// <summary>
    /// EXAMPLE executor (Architecture V2): checks ownership FIRST, then runs the side effect through a delegate bound ONCE in the constructor
    /// (no closure per call). Returns what was issued. Game-free; the delegate is a method of the Infrastructure adapter.
    /// </summary>
    internal sealed class ExampleValueExecutor
    {
        internal const string NotOwner = "not the owner", Applied = "applied";
        readonly IOwnership _owner;
        readonly Action<float> _apply;

        internal ExampleValueExecutor(IOwnership owner, Action<float> apply)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            if (apply == null) throw new ArgumentNullException("apply");
            _owner = owner;
            _apply = apply;
        }

        /// Expected outcome: Success with the applied value; Rejected (nothing applied) when this client is not the owner.
        internal OperationResponse<float> Apply(float value)
        {
            if (!_owner.IsOwner) return OperationResponse<float>.Rejected(NotOwner);
            _apply(value);
            return OperationResponse<float>.Success(value, Applied);
        }
    }
}
