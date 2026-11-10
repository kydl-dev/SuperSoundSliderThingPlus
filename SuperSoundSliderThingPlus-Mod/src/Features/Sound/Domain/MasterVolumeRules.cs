using KydlCore.Core;

namespace SuperSoundSliderThingPlus.Features.Sound.Domain
{
    /// <summary>
    /// Pure maths between the game's master volume (0 = silent, 1 = full, the player's in-game slider) and this mod's master MUTE level
    /// (0 = full loudness, 100 = muted): they are the same control seen from two sides, so mute = (1 - volume) x 100 (D-017).
    /// No Unity / Valheim types and no ModLog (guard test).
    /// </summary>
    internal static class MasterVolumeRules
    {
        internal const float SameWithin = 0.05f;
        internal const string NotANumber = "not a number", Clamped = "clamped into range", InRange = "in range";

        /// <summary>Expected outcome: Success with the mute level 0..100 (one decimal) for a game volume; the volume is clamped into 0..1; Rejected for NaN / infinity.</summary>
        internal static OperationResponse<float> MuteFromVolume(float volume)
        {
            if (!IsNumber(volume)) return OperationResponse<float>.Rejected(NotANumber);
            float v = volume < 0f ? 0f : volume > 1f ? 1f : volume;
            float mute = (float)System.Math.Round((1f - v) * 100f, 1);
            return OperationResponse<float>.Success(mute, v == volume ? InRange : Clamped);
        }

        /// <summary>Expected outcome: Success with the game volume 0..1 for a mute level; the level is clamped into 0..100; Rejected for NaN / infinity.</summary>
        internal static OperationResponse<float> VolumeFromMute(float mute)
        {
            if (!IsNumber(mute)) return OperationResponse<float>.Rejected(NotANumber);
            float m = mute < 0f ? 0f : mute > 100f ? 100f : mute;
            return OperationResponse<float>.Success(1f - m / 100f, m == mute ? InRange : Clamped);
        }

        /// <summary>True when two mute levels are the same slider position (rounding noise of the float round trip is ignored).</summary>
        internal static bool SameMute(float a, float b) => System.Math.Abs(a - b) < SameWithin;

        private static bool IsNumber(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
