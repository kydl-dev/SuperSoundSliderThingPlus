namespace SuperSoundSliderThingPlus.Features.Sound.Domain
{
    /// <summary>Float comparison with a tolerance (never == on floats). No Unity / Valheim types.</summary>
    internal static class FloatCompare
    {
        internal const float Tolerance = 1e-6f;

        /// <summary>Expected outcome: true when the two values differ by less than <see cref="Tolerance"/>.</summary>
        internal static bool Same(float a, float b) => System.Math.Abs(a - b) < Tolerance;
    }
}
