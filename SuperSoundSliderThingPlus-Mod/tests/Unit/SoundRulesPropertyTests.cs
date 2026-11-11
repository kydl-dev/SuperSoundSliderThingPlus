using System.Linq;
using FsCheck;
using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Tests.Unit
{
    /// <summary>
    /// Property-based tests (FsCheck, Props.Check) of the sound maths: rules that must hold for EVERY input, written from the documented
    /// behaviour (a round trip, a range that always holds, "more mute is never louder", "never throws"), not from the formulas.
    /// </summary>
    public class SoundRulesPropertyTests
    {
        private static readonly Gen<float> GameVolume = Gen.Choose(0, 1000).Select(i => i / 1000f);
        private static readonly Gen<float> MuteLevel = Gen.Choose(0, 1000).Select(i => i / 10f);
        private static readonly Gen<float> AnyFloat = Arb.Generate<float>();

        [Test] public void VolumeToMuteAndBack_ReturnsTheVolume()
            => Props.Check(Prop.ForAll(GameVolume.ToArbitrary(), v =>
            {
                float mute = MasterVolumeRules.MuteFromVolume(v).Value;
                float back = MasterVolumeRules.VolumeFromMute(mute).Value;
                return System.Math.Abs(back - v) <= 0.0006f;   // the mute level is rounded to one decimal (0.1 % = 0.001 volume)
            }));

        [Test] public void MuteFromVolume_IsAlwaysAPercentage()
            => Props.Check(Prop.ForAll(AnyFloat.ToArbitrary(), v =>
            {
                var r = MasterVolumeRules.MuteFromVolume(v);
                return !r.IsSuccess || (r.Value >= 0f && r.Value <= 100f);
            }));

        [Test] public void Factor_StaysBetweenSilenceAndFull()
            => Props.Check(Prop.ForAll(AnyFloat.ToArbitrary(), AnyFloat.ToArbitrary(), (m, c) =>
            {
                var r = SoundMixRules.Factor(m, c);
                return !r.IsSuccess || (r.Value >= 0f && r.Value <= 1f);
            }));

        [Test] public void Factor_MoreMuteIsNeverLouder()
            => Props.Check(Prop.ForAll(MuteLevel.ToArbitrary(), MuteLevel.ToArbitrary(), MuteLevel.ToArbitrary(), (a, b, other) =>
            {
                float lo = System.Math.Min(a, b), hi = System.Math.Max(a, b);
                bool category = SoundMixRules.Factor(0f, lo).Value >= SoundMixRules.Factor(0f, hi).Value;
                bool master = SoundMixRules.Factor(lo, other).Value >= SoundMixRules.Factor(hi, other).Value;
                return category && master;
            }));

        [Test] public void Factor_MasterAndCategoryMultiply()
            => Props.Check(Prop.ForAll(MuteLevel.ToArbitrary(), MuteLevel.ToArbitrary(), (m, c) =>
            {
                float both = SoundMixRules.Factor(m, c).Value;
                float apart = SoundMixRules.Factor(m, 0f).Value * SoundMixRules.Factor(0f, c).Value;
                return System.Math.Abs(both - apart) < 0.0001f;
            }));

        [Test] public void Gain_IsInsideTheLimitsOrLeftAlone()
            => Props.Check(Prop.ForAll(Gen.Choose(0, 20000).Select(i => i / 10000f).ToArbitrary(), measured =>
            {
                var r = LoudnessRules.Gain(measured, 0.1f, 0.25f, 3f);
                return r.IsSuccess && (System.Math.Abs(r.Value - 1f) < 1e-6f || (r.Value >= 0.25f && r.Value <= 3f));
            }));

        [Test] public void Gain_NeverThrowsOnAnyNumber()
            => Props.Check(Prop.ForAll(Gen.Four(AnyFloat).ToArbitrary(), t =>
            {
                LoudnessRules.Gain(t.Item1, t.Item2, t.Item3, t.Item4);
                return true;
            }));

        [Test] public void Rms_IsNeverAboveThePeak()
            => Props.Check(Prop.ForAll(Gen.Choose(-1000, 1000).Select(i => i / 1000f).ArrayOf().Where(a => a.Length > 0).ToArbitrary(), samples =>
            {
                var r = LoudnessRules.Rms(samples, samples.Length);
                float peak = samples.Max(s => System.Math.Abs(s));
                return r.IsSuccess && r.Value >= 0f && r.Value <= peak + 0.0001f;
            }));

        [Test] public void Classify_NeverThrowsAndAlwaysPicksACategory()
            => Props.Check(Prop.ForAll(Arb.Default.String(), Arb.Default.String(), (sound, owner) =>
                System.Enum.IsDefined(typeof(SoundCategory), SoundClassifier.Classify(sound, owner))));
    }
}
