namespace SuperSoundSliderThingPlus.Features.Sound.Domain
{
    /// <summary>
    /// Running estimate of how loud one clip is (the mean power of the blocks heard while it played, each already divided by the volume it
    /// played at, so it is the clip's own level - found in game 2026-11-11: most clips are compressed or streamed and cannot be read, but
    /// AudioSource.GetOutputData is post-volume, so the level is learned while the clip plays). Bounded: after <see cref="MaxBlocks"/> blocks it stops
    /// learning. Silent / invalid blocks are ignored. Pure: no Unity / Valheim types.
    /// </summary>
    internal sealed class LoudnessEstimate
    {
        internal const int MaxBlocks = 40, MinBlocks = 3;
        private double _sumPower;
        private int _blocks;

        /// <summary>How many usable blocks were added.</summary>
        internal int Blocks => _blocks;

        /// <summary>True when enough blocks were heard for <see cref="Rms"/> to be trusted.</summary>
        internal bool IsReady => _blocks >= MinBlocks;

        /// <summary>True while more blocks are wanted.</summary>
        internal bool WantsMore => _blocks < MaxBlocks;

        /// <summary>The clip's RMS level (0 until ready).</summary>
        internal float Rms => IsReady ? (float)System.Math.Sqrt(_sumPower / _blocks) : 0f;

        /// <summary>Expected outcome: true when the block was used; false for a silent, negative, NaN / infinite block or when full.</summary>
        internal bool Add(float blockRms)
        {
            if (!WantsMore || float.IsNaN(blockRms) || float.IsInfinity(blockRms) || blockRms < LoudnessRules.SilenceBelow) return false;
            _sumPower += (double)blockRms * blockRms;
            _blocks++;
            return true;
        }
    }
}
