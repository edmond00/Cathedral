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

    /// <summary>
    /// The wound catalogue for <paramref name="member"/>'s own anatomy.
    ///
    /// <para>
    /// <see cref="All"/> is the HUMAN catalogue. Reading it for every body meant a beast could only
    /// ever suffer the human wounds whose ids happened to coincide with beast anatomy — backbone,
    /// paunch, viscera — while every beast-specific wound (a broken foreleg, a torn-off fang) was
    /// unreachable. Each anatomy factory has always carried its own map; nothing asked it for one.
    /// </para>
    /// </summary>
    public static IEnumerable<Wound> ForAnatomy(PartyMember member)
        => ForAnatomy(member.AnatomyType);

    /// <inheritdoc cref="ForAnatomy(PartyMember)"/>
    public static IEnumerable<Wound> ForAnatomy(AnatomyType anatomy)
        => AnatomyFactoryRegistry.GetFactory(anatomy).GetWoundClassMap().Values;

    /// <summary>
    /// True when <paramref name="anatomy"/> owns <paramref name="wound"/> — the wound counterpart of
    /// <see cref="ModusMentisAnatomy.IsLearnableBy(ModusMentis, AnatomyType)"/>, and the question
    /// every path that puts a wound on a body must ask first.
    ///
    /// <para>
    /// A wound its anatomy does not own is <b>not a wound</b>. <c>KneeFractureRightWound</c> targets
    /// an organ part no beast has, so on a wolf every <c>Affects*</c> query misses and the injury
    /// costs one hit point and nothing else — a lame leg on an animal with no knees, invisible in
    /// play and impossible to explain. Worse, it is written into the save verbatim, and
    /// <c>PartyState.Rebuild</c> resolves wounds against the body's <i>own</i> catalogue and fails
    /// closed: one such wound makes the whole save unloadable, long after the run that produced it.
    /// </para>
    ///
    /// <para>Matched by type rather than by <see cref="Wound.WoundId"/>, which collides across
    /// anatomies by design — the same char is a human wound in one catalogue and a beast wound in
    /// the other, so an id comparison would call every mismatch a match.</para>
    /// </summary>
    public static bool CanBeSufferedBy(Wound wound, AnatomyType anatomy)
        => ForAnatomy(anatomy).Any(w => w.GetType() == wound.GetType());

    /// <inheritdoc cref="CanBeSufferedBy(Wound, AnatomyType)"/>
    public static bool CanBeSufferedBy(Wound wound, PartyMember member)
        => CanBeSufferedBy(wound, member.AnatomyType);

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
        Add(new PiercedHeartWound());
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
