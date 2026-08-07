using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Whether a given body can hold a given modus mentis — the one answer every grant path asks for.
///
/// <para>Two tests, in this order:</para>
/// <list type="number">
/// <item><b>Structural.</b> Every id in <see cref="ModusMentis.Organs"/> must resolve on the body.
/// This needs no authoring and is the reason a human cannot learn a fang skill. It also closes a
/// quieter hole: an absent organ contributes +0 to the level cap, so such a modus mentis used to be
/// <i>grantable and stuck at level 1</i> rather than refused — held, useless, and unexplained.</item>
/// <item><b>Capability.</b> The body must have everything in
/// <see cref="ModusMentis.RequiredCapabilities"/> (see <see cref="AnatomyCapability"/>).</item>
/// </list>
///
/// <para>Anatomy-typed overloads exist so content validation (rules R11/R12) can ask the same
/// question of an anatomy rather than of a particular character.</para>
/// </summary>
public static class ModusMentisAnatomy
{
    /// <summary>True when <paramref name="member"/> can learn and use <paramref name="mm"/>.</summary>
    public static bool IsLearnableBy(ModusMentis mm, PartyMember member)
        => mm.Organs.All(id => member.GetOrganById(id) != null || member.GetBodyPartById(id) != null)
           && member.Can(mm.RequiredCapabilities);

    /// <summary>True when any body of <paramref name="anatomy"/> can learn <paramref name="mm"/>.</summary>
    public static bool IsLearnableBy(ModusMentis mm, AnatomyType anatomy)
    {
        var factory = AnatomyFactoryRegistry.GetFactory(anatomy);
        var sources = SourcesOf(anatomy);
        return mm.Organs.All(sources.Contains)
               && (factory.Capabilities & mm.RequiredCapabilities) == mm.RequiredCapabilities;
    }

    /// <summary>Everything this modus mentis is filtered down to for one body.</summary>
    public static IEnumerable<ModusMentis> LearnableBy(IEnumerable<ModusMentis> pool, PartyMember member)
        => pool.Where(mm => IsLearnableBy(mm, member));

    /// <summary>
    /// Every organ id and body-region id an anatomy owns, as the ids modi mentis name them by.
    /// Built from the anatomy factory itself, so a new anatomy needs nothing added here.
    ///
    /// <para>Ids, not class names: <c>LegsOrgan</c> and <c>BeastLegsOrgan</c> both answer
    /// <c>"legs"</c>, and <c>BeastClawsOrgan</c> answers <c>"claws"</c>. Anything matching on the
    /// class would get both wrong.</para>
    /// </summary>
    public static HashSet<string> SourcesOf(AnatomyType anatomy)
    {
        if (_sourcesByAnatomy.TryGetValue(anatomy, out var cached)) return cached;

        var ids = new HashSet<string>();
        foreach (var part in AnatomyFactoryRegistry.GetFactory(anatomy).CreateBodyParts())
        {
            ids.Add(part.Id);
            foreach (var organ in part.Organs)
                ids.Add(organ.Id);
        }
        return _sourcesByAnatomy[anatomy] = ids;
    }

    /// <summary>Every anatomy the game defines, in enum order.</summary>
    public static IEnumerable<AnatomyType> AllAnatomies
        => System.Enum.GetValues<AnatomyType>();

    private static readonly Dictionary<AnatomyType, HashSet<string>> _sourcesByAnatomy = new();
}
