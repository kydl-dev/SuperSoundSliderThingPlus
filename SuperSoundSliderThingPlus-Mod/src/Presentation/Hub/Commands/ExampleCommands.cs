using KydlCore.Common;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Example.Application;

namespace SuperSoundSliderThingPlus.Presentation.Hub.Commands
{
    /// <summary>The hub's callbacks turned into commands (spec "UI interaction flow": KydlCore UI -> mod callback -> dispatcher -> handler).</summary>
    internal static class ExampleCommands
    {
        /// Expected outcome: the handler's response (logged; Failed is a warning).
        internal static OperationResponse<float> SetValue(CommandDispatcher commands, float requested)
        {
            var r = commands.Dispatch(new SetExampleValueCommand(requested));
            if (r.Status == ResponseStatus.Failed) ModLog.Warn("ExampleCommands: SetValue " + requested + " - " + r);
            else ModLog.Verbose("ExampleCommands: SetValue " + requested + " - " + r);
            return r;
        }
    }
}
