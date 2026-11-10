using System;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Sound.Domain;
using SuperSoundSliderThingPlus.Features.Sound.Execution;

namespace SuperSoundSliderThingPlus.Features.Sound.Application
{
    /// <summary>
    /// Pipeline (command given, then the domain decision: clamp the level into 0..100) -> executor (writes the slider). Built once.
    /// Game-free: tested with a fake ownership and a recording action (tests/Integration).
    /// </summary>
    internal sealed class SetMuteHandler : ICommandHandler<SetMuteCommand, float>
    {
        internal const string NoCommand = "no command";
        private readonly OperationPipeline<SetMuteCommand, float> _pipeline;
        private readonly MuteExecutor _executor;

        internal SetMuteHandler(MuteExecutor executor, Action<string> trace)
        {
            if (executor == null) throw new ArgumentNullException("executor");
            _executor = executor;
            _pipeline = new OperationPipeline<SetMuteCommand, float>("SetMute", c => SoundMixRules.Level(c.Requested))
                .Validate("command given", c => ValidationResult.RejectUnless(c != null, NoCommand));
            _pipeline.Trace = trace;
        }

        /// <summary>Expected outcome: the executor's response when the rules accept the level, else the rules' Rejected response.</summary>
        public OperationResponse<float> Handle(SetMuteCommand command)
        {
            var decision = _pipeline.Run(command);
            return decision.IsSuccess ? _executor.Apply(command.Slider, decision.Value) : decision;
        }
    }
}
