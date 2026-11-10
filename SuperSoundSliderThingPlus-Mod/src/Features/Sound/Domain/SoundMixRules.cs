using KydlCore.Core;

namespace SuperSoundSliderThingPlus.Features.Sound.Domain
{
    /// <summary>
    /// Pure volume maths (D-004): the master slider and every category slider are MUTE LEVELS in percent
    /// (0 = full loudness, 100 = muted); the master scales every category, so the factors multiply.
    /// No Unity / Valheim types and no ModLog (the guard test checks it): every branch is unit-tested.
    /// </summary>
    internal static class SoundMixRules
    {
        internal const float MinMute = 0f, MaxMute = 100f, DefaultMute = 0f;
        internal const string NotANumber = "not a number", Clamped = "clamped into range", InRange = "in range";

        /// <summary>Expected outcome: Success with the loudness factor 0..1 (1 = unchanged, 0 = silent); Rejected when a level is NaN / infinity.</summary>
        internal static OperationResponse<float> Factor(float masterMute, float categoryMute)
        {
            if (!IsNumber(masterMute) || !IsNumber(categoryMute)) return OperationResponse<float>.Rejected(NotANumber);
            float master = Clamp(masterMute);
            float category = Clamp(categoryMute);
            bool clamped = master != masterMute || category != categoryMute;
            float factor = (1f - master / MaxMute) * (1f - category / MaxMute);
            return OperationResponse<float>.Success(factor, clamped ? Clamped : InRange);
        }

        /// <summary>Expected outcome: Success with the level clamped into 0..100 (reason says whether it was clamped); Rejected for NaN / infinity.</summary>
        internal static OperationResponse<float> Level(float requested)
        {
            if (!IsNumber(requested)) return OperationResponse<float>.Rejected(NotANumber);
            float level = Clamp(requested);
            return OperationResponse<float>.Success(level, level == requested ? InRange : Clamped);
        }

        private static bool IsNumber(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static float Clamp(float mute) => mute < MinMute ? MinMute : mute > MaxMute ? MaxMute : mute;
    }
}
