# SuperSoundSliderThingPlus tests (MIGRATION_DECISIONS A1, C15-C24)

| Folder | What | Run |
|---|---|---|
| `Unit/` | pure rules (Domain / Application / Execution are LINKED, not the DLL), NUnit + FsCheck | `dotnet test tests\Unit` (or `dotnet msbuild SuperSoundSliderThingPlus.csproj -t:UnitTests`) |
| `Integration/` | the built DLL + package contracts (manifest, versions, Hexium, settings, patch targets, suite guards, architecture rules) AND command flows with fake capabilities | `dotnet test tests\Integration` (`-t:IntegrationTests`); release gates: `--filter TestCategory=ReleaseGate` |
| `TestLab/suites/SuperSoundSliderThingPlus/` | DrakesTestLab suite: the real game (hooks in `src/Testing/TestHooks`) | `-t:TestLab` (= `Claude\lessons\scripts\lab-guarded.ps1 -Mod SuperSoundSliderThingPlus`; the lab mod is switched on for the run and ALWAYS off afterwards) |
| `TestLab/suites/SuperSoundSliderThingPlus-Manual/` | checkpoints a person answers (looks, real keys) | F9 > Tests in game |
| `COVERAGE.md` | requirement -> Unit / Integration / TestLab / Manual / Gap | update with every change |

**Before writing, changing or running a TestLab test: read and follow `%APPDATA%\com.kesomannen.gale\valheim\profiles\test\BepInEx\config\DrakesTestLab\docs\TESTLAB-AUTHORING.md`** (PROJECT_RULES 21): repeated steps go into `TestLab/suites/SuperSoundSliderThingPlus/_functions/`, `do: hook` steps never use `min` / `max` (ignored), `lab-suite-lint.py` before every run, single-test runs for fixes.

Build the mod first (the tests read `assets\SuperSoundSliderThingPlus.dll`). Lab runs: never build while the game runs; read `Claude\lessons\TESTLAB_PLAYBOOK.md`
and the current lab docs (`%APPDATA%\com.kesomannen.gale\valheim\profiles\test\BepInEx\config\DrakesTestLab\docs`). Every run is archived in
`E:\Projects\Valheim\_testlab\` (results + DLL hashes) and gets a TESTLOG line. Release: 3 consecutive green FULL runs of this DLL (G16, no ecosystem run since 2026-11-08): `Claude\lessons\scripts\lab-chain.ps1`; a new or changed test must first pass 10/10 with `repeat-tests.ps1`; `check-tests.ps1` before every run; poll with `lab-progress.ps1`
(all in `Claude\lessons\scripts\`, overview in `Claude\lessons\TESTING_TOOLKIT.md`, rules in `LESSONS_LEARNED.md` section 7, logging in `DIAGNOSTIC_LOGGING.md`).
Everything in this folder stays out of the release zip.
