using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene;

/// <summary>
/// Turns an area's observation objects into what an observing modus mentis is offered: things that
/// are parts of one whole come as that whole, and three or more things of one kind come as their kind.
/// Nothing in the scene changes — the members stay in their node; only the choice list is shorter.
///
/// <para>A city square used to offer two dozen choices: a door and a wall per house, a path and the
/// street it reaches per way out, three people among them. (A path no longer doubles a way out — paths
/// now only join areas two or three steps apart — so the pass that paired the two is gone.) The persona's second look then jumped to
/// whatever unrelated thing the judge preferred. Grouped, the square offers "the buildings around",
/// "the ways leading on", the fountain, the people — and a look at the buildings is followed by a look
/// at one of them (the zoom, in <c>ObservationPhaseController</c>).</para>
///
/// Two passes, in order:
/// <list type="number">
/// <item><b>Wholes</b> — points of interest sharing a <see cref="PoiCluster"/> (a building's entry
///       door and outside wall), when two or more are in view.</item>
/// <item><b>Families</b> — <see cref="FamilyMinimum"/> or more units of one kind: the same point of
///       interest type, the same kind of whole, or a way on (a neighbouring area or a path). Never
///       people, never the door back out of a building, and never a set whose members name themselves
///       with different nouns.</item>
/// </list>
/// </summary>
public static class ObservationGrouping
{
    /// <summary>How many things of one kind make a family. Below it the list is short enough.</summary>
    public const int FamilyMinimum = 3;

    /// <summary>How many members a family's description names before "and others".</summary>
    private const int FamilyListMax = 5;

    /// <summary>
    /// The units an observation choice is made over: every object of <paramref name="outcomes"/>,
    /// with groups in place of their members. Groups come from <paramref name="ledger"/>, so asking
    /// twice in a phase yields the same instances.
    /// </summary>
    public static List<NarrativeAnchor> Build(IReadOnlyList<NarrativeAnchor> outcomes, ObservationLedger ledger)
    {
        var units = new List<NarrativeAnchor>();
        var used  = new HashSet<NarrativeAnchor>(ReferenceEqualityComparer.Instance);

        // 1. Wholes.
        foreach (var cluster in outcomes.OfType<SyntheticObservationObject>()
                                        .Where(o => o.PointOfInterest.Cluster != null)
                                        .GroupBy(o => o.PointOfInterest.Cluster!))
        {
            var parts = cluster.Cast<NarrativeAnchor>().ToList();
            if (parts.Count < 2) continue;
            var c = cluster.Key;
            var group = ledger.GroupFor(Key("whole", parts), () => new ObservationGroup(
                c.Name, NeutralNarration.NounPhrase(c.Name.ToLowerInvariant()), c.Lemma,
                _ => c.Description, parts));
            used.UnionWith(parts);
            units.Add(group);
        }

        foreach (var o in outcomes)
            if (!used.Contains(o)) units.Add(o);

        // 2. Families.
        var result = new List<NarrativeAnchor>();
        foreach (var kind in units.GroupBy(FamilyKey))
        {
            var members = kind.ToList();
            var family = kind.Key != null && members.Count >= FamilyMinimum
                ? MakeFamily(kind.Key, members, ledger)
                : null;
            if (family == null) result.AddRange(members);
            else result.Add(family);
        }
        return result;
    }

    /// <summary>What kind a unit is for the family pass; null for what is never grouped.</summary>
    private static string? FamilyKey(NarrativeAnchor unit) => unit switch
    {
        SyntheticNpcObservationObject => null,
        // The way back out of a building stays one step away.
        SyntheticObservationObject { PointOfInterest: DoorPointOfInterest { Role: DoorRole.Entry } } => null,
        SyntheticObservationObject { PointOfInterest: PathPointOfInterest } => "way",
        SyntheticObservationObject o => "poi:" + o.PointOfInterest.GetType().Name,
        SyntheticAreaObservationObject => "way",
        ObservationGroup g => "whole:" + g.ReferenceLemma,
        _ => null,
    };

    private static ObservationGroup? MakeFamily(string key, List<NarrativeAnchor> members, ObservationLedger ledger)
    {
        string name, phrase, lemma;
        Func<string, string> clause;
        if (key == "way")
            (name, phrase, lemma, clause) = ("Ways Onward", "the ways leading on", "way",
                                             l => $"Ways lead on from here: {l}.");
        else if (key == "poi:" + nameof(LandscapePointOfInterest))
            (name, phrase, lemma, clause) = ("View", "the view below", "view",
                                             l => $"From here the eye reaches {l}.");
        else if (key == "poi:" + nameof(DoorPointOfInterest))
            (name, phrase, lemma, clause) = ("Doors", "the doors leading on", "door",
                                             l => $"Doors lead on from here: {l}.");
        else if (key.StartsWith("whole:", StringComparison.Ordinal))
            (name, phrase, lemma, clause) = ("Buildings", "the surrounding buildings", "building",
                                             l => $"Buildings stand around: {l}.");
        else
        {
            // Any other kind is named by the noun its members share; members that name themselves
            // differently (a bench, a mounting block, a cart tail) are not one kind to a reader.
            var lemmas = members.OfType<ObservationObject>().Select(m => m.ReferenceLemma)
                                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (lemmas.Count != 1 || lemmas[0].Length == 0) return null;
            var plural = Plural(lemmas[0].ToLowerInvariant());
            (name, phrase, lemma, clause) = (char.ToUpperInvariant(plural[0]) + plural[1..], $"the {plural} here",
                                             lemmas[0], l => $"There are several: {l}.");
        }

        return ledger.GroupFor(Key(key, members), () => new ObservationGroup(
            name, phrase, lemma, _ => clause(ListOf(members)), members, descriptionIsClause: true));
    }

    /// <summary>"a forge, an alehouse and a sunken house" — the first few members, then "and others".</summary>
    private static string ListOf(IReadOnlyList<NarrativeAnchor> members)
    {
        var phrases = members.OfType<ObservationObject>().Select(m => m.NeutralPhrase).ToList();
        var shown = phrases.Take(FamilyListMax).ToList();
        if (phrases.Count > FamilyListMax) shown.Add("others");
        return shown.Count == 1 ? shown[0] : $"{string.Join(", ", shown.Take(shown.Count - 1))} and {shown[^1]}";
    }

    /// <summary>English plural of a reference noun — enough for the nouns points of interest use.</summary>
    public static string Plural(string noun)
    {
        if (noun is "shelf" or "leaf" or "loaf" or "sheaf" or "calf" or "wolf" or "half")
            return noun[..^1] + "ves";
        if (noun.EndsWith('s') || noun.EndsWith('x') || noun.EndsWith('z') || noun.EndsWith("ch") || noun.EndsWith("sh"))
            return noun + "es";
        if (noun.Length > 1 && noun.EndsWith('y') && !"aeiou".Contains(noun[^2]))
            return noun[..^1] + "ies";
        return noun + "s";
    }

    private static string Key(string kind, IEnumerable<NarrativeAnchor> members)
        => kind + "|" + string.Join(",", members.Select(RuntimeHelpers.GetHashCode).OrderBy(h => h));
}
