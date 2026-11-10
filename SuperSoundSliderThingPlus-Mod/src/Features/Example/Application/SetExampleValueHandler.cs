using System;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Example.Domain;
using SuperSoundSliderThingPlus.Features.Example.Execution;

namespace SuperSoundSliderThingPlus.Features.Example.Application
{
    /// <summary>
    /// EXAMPLE handler: pipeline (validation steps, then the domain decision) -> executor (the side effect). Built once; Handle allocates nothing
    /// beyond the trace line while debug is on. Game-free: tested with a fake ownership + a recording action (tests/Integration).
    /// </summary>
    internal sealed class SetExampleValueHandler : ICommandHandler<SetExampleValueCommand, float>
    {
        internal const string NoCommand = "no command";
        readonly OperationPipeline<SetExampleValueCommand, float> _pipeline;
        readonly ExampleValueExecutor _executor;

        internal SetExampleValueHandler(ExampleValueExecutor executor, Action<string> trace)
        {
            if (executor == null) throw new ArgumentNullException("executor");
            _executor = executor;
            _pipeline = new OperationPipeline<SetExampleValueCommand, float>("SetExampleValue", c => ExampleRules.Decide(c.Requested))
                .Validate("command given", c => ValidationResult.RejectUnless(c != null, NoCommand));
            _pipeline.Trace = trace;
        }

        /// Expected outcome: the executor's response when the rules accept the value, else the rules' Rejected / Ignored response.
        public OperationResponse<float> Handle(SetExampleValueCommand command)
        {
            var decision = _pipeline.Run(command);
            return decision.IsSuccess ? _executor.Apply(decision.Value) : decision;
        }
    }
}
