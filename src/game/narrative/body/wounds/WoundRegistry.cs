using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Holds all defined wound types and parses wound glyph positions from wounds.txt.
/// </summary>
public static class WoundRegistry
{
    /// <summary>Every defined wound type, keyed by their single-char id.</summary>
    public static readonly Dictionary<char, Wound> All = BuildRegistry();

    /// <summary>All wildcard (Low handicap) wound templates.</summary>
    public static readonly IReadOnlyList<WildcardWound> WildcardTemplates =
        All.Values.OfType<WildcardWound>().ToList();

    private static Dictionary<char, Wound> BuildRegistry()
    {
        var dict = new Dictionary<char, Wound>();
        void Add(Wound w) => dict[w.WoundId] = w;

        Add(new BlackEyeLeftWound());         Add(new BlackEyeRightWound());
        Add(new PiercedEyeLeftWound());       Add(new PiercedEyeRightWound());
        Add(new PerforatedEardrumLeftWound()); Add(new PerforatedEardrumRightWound());
        Add(new SkullFractureWound());        Add(new ConcussionsWound());
        Add(new BrokenNoseWound());           Add(new BrokenTeethsWound());
        Add(new TornedOutTongueWound());
        Add(new BrokenBackboneWound());       Add(new BrokenRibsWound());
        Add(new EviscerationWound());         Add(new PiercedPaunchWound());
        Add(new GenitalMutilationWound());    Add(new DisfiguredWound());
        Add(new BrokenArmLeftWound());        Add(new BrokenArmRightWound());
        Add(new ShoulderDislocationLeftWound()); Add(new ShoulderDislocationRightWound());
        Add(new FingersAmputeeLeftWound());   Add(new FingersAmputeeRightWound());
        Add(new WristFractureLeftWound());    Add(new WristFractureRightWound());
        Add(new BrokenHandLeftWound());       Add(new BrokenHandRightWound());
        Add(new KneeFractureLeftWound());     Add(new KneeFractureRightWound());
        Add(new TibiaFractureLeftWound());    Add(new TibiaFractureRightWound());
        Add(new FootAmputeeLeftWound());      Add(new FootAmputeeRightWound());
        Add(new AnkleFractureLeftWound());    Add(new AnkleFractureRightWound());
        Add(new BrokenFootLeftWound());       Add(new BrokenFootRightWound());
        // Wildcard wounds (Low handicap: -1 HP only)
        Add(new ContusionWound());
        Add(new CutWound());
        Add(new PunctureWound());
        Add(new ScarWound());
        return dict;
    }

    /// <summary>
    /// Parse wounds.txt: for each wound id char found in the file, record its art (x, y) position.
    /// Returns a mapping from wound id char → list of art coordinates.
    /// </summary>
    public static Dictionary<char, List<(int x, int y)>> LoadWoundPositions(string folderPath)
    {
        var result = new Dictionary<char, List<(int x, int y)>>();
        string filePath = Path.Combine(folderPath, "wounds.txt");
        if (!File.Exists(filePath)) return result;

        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        for (int y = 0; y < lines.Length; y++)
        {
            string line = lines[y];
            // Enumerate Unicode code points (each logical char = one cell in the renderer)
            int x = 0;
            for (int ci = 0; ci < line.Length; )
            {
                int cp = char.ConvertToUtf32(line, ci);
                char ch;
                if (cp > 0xFFFF)
                {
                    // Surrogate pair — skip (not a wound id)
                    ch = '\0';
                    ci += 2;
                }
                else
                {
                    ch = (char)cp;
                    ci++;
                }

                if (ch != '\0' && All.ContainsKey(ch))
                {
                    if (!result.TryGetValue(ch, out var list))
                        result[ch] = list = new List<(int x, int y)>();
                    list.Add((x, y));
                }
                x++;
            }
        }
        return result;
    }
}
