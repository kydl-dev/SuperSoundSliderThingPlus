using KydlCore.Common;
using KydlCore.Core;
using SuperSoundSliderThingPlus.Features.Sound.Application;
using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Presentation.Hub.Commands
{
    /// <summary>The hub's callbacks turned into commands (spec "UI interaction flow": KydlCore UI -> mod callback -> dispatcher -> handler).</summary>
    internal static class SoundCommands
    {
        /// <summary>Expected outcome: the handler's response (logged; Failed is a warning).</summary>
        internal static OperationResponse<float> SetMute(CommandDispatcher commands, SoundSlider slider, float requested)
        {
            var r = commands.Dispatch(new SetMuteCommand(slider, requested));
            if (r.Status == ResponseStatus.Failed) ModLog.Warn("SoundCommands: SetMute " + slider + " " + requested + " - " + r);
            else ModLog.Verbose("SoundCommands: SetMute " + slider + " " + requested + " - " + r);
            return r;
        }
    }
}
