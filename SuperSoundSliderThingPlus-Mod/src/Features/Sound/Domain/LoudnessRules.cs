using KydlCore.Core;

namespace SuperSoundSliderThingPlus.Features.Sound.Domain
{
    /// <summary>
    /// Pure loudness-matching maths (D-005 step 2): the loudness of a clip is the RMS of a window of its samples, and the gain that
    /// brings it to the target is target / measured, kept inside [min, max] so a very quiet clip is never boosted into noise and a loud
    /// one is never cut to nothing. No Unity / Valheim types and no ModLog (guard test): every branch is unit-tested.
    /// </summary>
    internal static class LoudnessRules
    {
        internal const string NotMeasurable = "no usable measurement", BadLimits = "gain limits are not valid", Limited = "limited to the gain range", Exact = "exact";
        internal const float SilenceBelow = 0.0001f;

        /// <summary>Expected outcome: Success with the RMS (0..1+) of the first `count` samples; Rejected for no samples or a null array.</summary>
        internal static OperationResponse<float> Rms(float[] samples, int count)
        {
            if (samples == null || count <= 0 || count > samples.Length) return OperationResponse<float>.Rejected(NotMeasurable);
            double sum = 0;
            for (int i = 0; i < count; i++)
            {
                double s = samples[i];
                sum += s * s;
            }
            double rms = System.Math.Sqrt(sum / count);
            if (double.IsNaN(rms) || double.IsInfinity(rms)) return OperationResponse<float>.Rejected(NotMeasurable);
            return OperationResponse<float>.Success((float)rms, Exact);
        }

        /// <summary>
        /// Expected outcome: Success with the gain (reason says whether the range limited it); a silent or invalid measurement gives Success 1
        /// (the clip is left alone) - never a huge boost; Rejected when the target or the limits are not valid numbers (min &gt; max, min &lt;= 0).
        /// </summary>
        internal static OperationResponse<float> Gain(float measuredRms, float targetRms, float minGain, float maxGain)
        {
            if (!IsNumber(targetRms) || targetRms <= 0f || !IsNumber(minGain) || !IsNumber(maxGain) || minGain <= 0f || minGain > maxGain)
                return OperationResponse<float>.Rejected(BadLimits);
            if (!IsNumber(measuredRms) || measuredRms < SilenceBelow) return OperationResponse<float>.Success(1f, NotMeasurable);
            float gain = targetRms / measuredRms;
            float limited = gain < minGain ? minGain : gain > maxGain ? maxGain : gain;
            return OperationResponse<float>.Success(limited, FloatCompare.Same(limited, gain) ? Exact : Limited);
        }

        private static bool IsNumber(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
