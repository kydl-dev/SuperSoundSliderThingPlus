using KydlCore.Common;
using SuperSoundSliderThingPlus.Bootstrap;

namespace SuperSoundSliderThingPlus.Testing.TestHooks
{
    /// <summary>
    /// DrakesTestLab hooks (spec 8.7) for the suite in tests/TestLab/suites/SuperSoundSliderThingPlus. A hook is a static method with [TestHook("name")];
    /// DrakesTestLab finds it by the attribute's NAME (the mod has no reference to DrakesTestLab; TestHookAttribute comes from KydlCore.dll:
    /// KydlCore.Common.TestHookAttribute, KydlCore\KydlCore-Mod\src\Shared\Common\TestLabSupport.cs). Names start with "SuperSoundSliderThingPlus." so two mods never clash.
    /// Hooks call the REAL code paths (the command dispatcher, the feature registry), never a copy of a rule. Only add a hook where a normal
    /// player action or a built-in check cannot reach the behaviour. Reads return short values a suite compares with expect / contains / notContains / regex (NOT min / max: a `do: hook` step silently ignores them, PROJECT_RULES 21 - numeric limits go into `wait until`, a `check` step or `asserts`). A hook that returns false for a reason logs that reason (throttled, ModLog.IsDue): lessons\DIAGNOSTIC_LOGGING.md section 3.
    /// </summary>
    internal static class ModTestHooks
    {
        /// Every test starts here (MIGRATION_DECISIONS C26, lessons TESTLAB_PLAYBOOK): the player on the open arena spot 70 m from the lab start, facing +z, god mode on.
        [TestHook("SuperSoundSliderThingPlus.arena")]
        static string Arena() { return TestArena.MovePlayer(); }

        /// All Startup_* / World_* checks of InGameTests (the ones Kydl.GameProbe runs): "PASS: ..." or "FAIL: ...".
        [TestHook("SuperSoundSliderThingPlus.selftest")]
        static string SelfTest() { return TestLabSupport.RunInGameTests(typeof(InGameTests)); }

        /// Is the feature running (FeatureRegistry state after its Enable switches)?
        [TestHook("SuperSoundSliderThingPlus.featureRunning")]
        static bool FeatureRunning(string id) { return ModInstaller.Current != null && ModInstaller.Current.Features.IsRunning(id); }
    }
}
