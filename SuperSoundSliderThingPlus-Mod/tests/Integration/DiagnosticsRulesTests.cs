using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SuperSoundSliderThingPlus.Tests.Integration;

/// <summary>
/// Diagnostics from the start (PROJECT_RULES 28, user 2026-11-06), checked on the sources:
///  * per-frame code in a feature or an integration has a throttled status line (ModLog.Every / IsDue), so a log - and KydlCore's flight
///    recorder - shows what it was doing when something went wrong;
///  * a mod whose features drive creatures (Character / MonsterAI / BaseAI / Humanoid) tracks them with KydlCore's FlightRecorder.
/// Origin: 2026-11-05 SteveTheTroll release runs, where a frozen far troll and a pinned guard target were found only after lines were added.
/// Keep these green; a deliberate exception is written in docs/Architecture.md AND here.
/// </summary>
[TestFixture]
public class DiagnosticsRulesTests
{
    static readonly Regex LineComment = new Regex(@"//.*$", RegexOptions.Multiline);
    static readonly Regex StringLiteral = new Regex(@"""([^""\\]|\\.)*""");
    static readonly Regex PerFrame = new Regex(@"\bvoid\s+(Update|FixedUpdate|LateUpdate|Tick)\s*\(");
    static readonly Regex Throttled = new Regex(@"\bModLog\.(Every|IsDue)\s*\(");
    static readonly Regex CreatureType = new Regex(@"\b(Character|MonsterAI|BaseAI|Humanoid)\b");
    static readonly Regex Tracks = new Regex(@"\bFlightRecorder\.Track\s*\(");

    static string Code(string file) { return StringLiteral.Replace(LineComment.Replace(File.ReadAllText(file), ""), "\"\""); }

    /// Game-side code of the features and the integrations (the pure layers have no frames; Bootstrap / Testing only dispatch).
    static IEnumerable<string> GameSideFiles()
    {
        var files = new List<string>();
        var features = Path.Combine(TestPaths.Src, "Features");
        if (Directory.Exists(features))
            foreach (var dir in Directory.GetDirectories(features, "Infrastructure", SearchOption.AllDirectories))
                files.AddRange(Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories));
        var integrations = Path.Combine(TestPaths.Src, "Integrations");
        if (Directory.Exists(integrations)) files.AddRange(Directory.GetFiles(integrations, "*.cs", SearchOption.AllDirectories));
        return files.Distinct();
    }

    [Test] public void PerFrameCode_HasAThrottledStatusLine()
    {
        var silent = GameSideFiles().Where(f => { var c = Code(f); return PerFrame.IsMatch(c) && !Throttled.IsMatch(c); }).Select(Path.GetFileName).ToList();
        Assert.That(silent, Is.Empty, "PROJECT_RULES 28: per-frame code (Update / FixedUpdate / LateUpdate / Tick) writes ModLog.Every(key, seconds, \"what it does now, with values and units\")");
    }

    [Test] public void CreatureCode_IsTrackedByTheFlightRecorder()
    {
        var files = GameSideFiles().ToList();
        var driving = files.Where(f => CreatureType.IsMatch(Code(f))).Select(Path.GetFileName).ToList();
        bool tracks = Directory.GetFiles(TestPaths.Src, "*.cs", SearchOption.AllDirectories).Any(f => Tracks.IsMatch(Code(f)));
        Assert.That(driving.Count == 0 || tracks, Is.True,
            "PROJECT_RULES 28: these files drive creatures, so the mod tracks them - FlightRecorder.Track(creature, label, () => \"the mod's state\") where it takes one over: " + string.Join(", ", driving));
    }
}
