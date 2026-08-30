using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cathedral.Game.Scene;

/// <summary>
/// The reachability half of <see cref="VerbAudit"/>: whether a lesson keyed to a content type can
/// ever actually fire.
/// </summary>
public static partial class VerbAudit
{
    /// <summary>
    /// Kinds a verb meets in play but that no factory places, so the sweep never sees one and a
    /// lesson keyed to it would be reported unreachable when it is not.
    /// </summary>
    private static readonly HashSet<string> SpawnedInPlay = new(StringComparer.Ordinal)
    {
        "CorpsePointOfInterest",       // spawned by a kill, never placed at build
        "SleepingNpcPointOfInterest",  // merged per night from a sleeper and their bed
    };

    /// <summary>
    /// The question a lesson keyed to a type must answer: <b>is this verb ever offered on that type
    /// at all?</b> A verb's own gate decides what it accepts, and a lesson naming something outside
    /// that gate can never fire however much of the content the world contains.
    ///
    /// <para>This is a whole class of silent fault, and it was worth twenty-one dead lessons the
    /// first time it ran. Four shapes of it, none visible without the check:</para>
    ///
    /// <list type="bullet">
    ///   <item>the verb accepts a different type entirely — BREAK only takes breakables, so a lesson
    ///     about forcing chests and doors never fired;</item>
    ///   <item>the target is the <i>item</i> and not its holder — GATHER takes an
    ///     <c>ItemElement</c>, so a lesson naming the bush could not match (hence
    ///     <c>LessonContext.Holder</c>);</item>
    ///   <item>the object does not reward the sense — nine objects declared what contemplating them
    ///     teaches while their <c>SensoryProfile</c> rewarded only examine and listen, so
    ///     CONTEMPLATE was never offered on them;</item>
    ///   <item>the object exists but the verb refuses it for another reason — DIG refuses ground
    ///     with nothing left in it, so a churchyard placed without spoil could not be dug.</item>
    /// </list>
    ///
    /// <para>It reads the verb <b>sources</b> rather than the types, because a lesson is free C# and
    /// cannot be enumerated by running it. A condition naming several types passes if <i>any</i> of
    /// them is reachable, and a build with no sources beside it (a shipped one) skips the check
    /// rather than failing it.</para>
    /// </summary>
    private static void CheckLessonsCanBeReached(List<string> warnings)
    {
        string dir = Path.Combine("src", "game", "scene", "verbs");
        if (!Directory.Exists(dir)) return;

        var verbId = new Regex("VerbId\\s*=>\\s*\"([a-z_]+)\"");
        var branch = new Regex(@"if \((.*?)\) yield return Mm<(\w+)ModusMentis>\(\);");
        var named  = new Regex(@"(?:is|or)\s+(?:Building\.)?(\w+(?:PointOfInterest|Area))");

        foreach (string file in Directory.GetFiles(dir, "*.cs"))
        {
            string src;
            try { src = File.ReadAllText(file); } catch { continue; }

            // One segment per class, so a multi-class file cannot pair a branch with the wrong verb.
            foreach (string segment in src.Split("\npublic ").Skip(1))
            {
                var id = verbId.Match(segment);
                if (!id.Success || !ReachableTypes.TryGetValue(id.Groups[1].Value, out var seen)) continue;

                foreach (Match b in branch.Matches(segment))
                {
                    var types = named.Matches(b.Groups[1].Value).Select(m => m.Groups[1].Value).ToList();
                    if (types.Count == 0) continue;

                    bool anyReachable = types.Any(
                        t => SpawnedInPlay.Contains(t)
                          || seen.Any(s => s.Name == t || Inherits(s, t)));

                    if (!anyReachable)
                        warnings.Add($"verb '{id.Groups[1].Value}' teaches '{b.Groups[2].Value}' when the "
                                   + $"target is [{string.Join(", ", types)}], but it is never offered on "
                                   + "any of those — the lesson cannot fire");
                }
            }
        }
    }

    /// <summary>Whether a seen type is, or descends from, the named one.</summary>
    private static bool Inherits(Type seen, string name)
    {
        for (var t = seen; t != null; t = t.BaseType)
            if (t.Name == name) return true;
        return false;
    }
}
