using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// An observation that has parts of its own to look at more closely: a group of things presented as
/// one (<see cref="ObservationGroup"/>), or a container whose contents can each be attended to.
///
/// <para>Observing it renders the whole; the next observation of the same block then turns to one of
/// its parts rather than to something unrelated (the zoom), and directing attention to it later
/// renders its parts one by one. Thinking about it offers everything its parts afford.</para>
/// </summary>
public interface IObservationGroup
{
    /// <summary>What closer attention can turn to. Empty when there is nothing to zoom into.</summary>
    IReadOnlyList<NarrativeAnchor> Parts { get; }
}

/// <summary>
/// Several observation objects presented as one — a building's door and wall as the building, a path
/// and the street it leads to as the street, the views from a roof as the view, a hall's inner doors
/// as the doors leading on. Built per narration phase by <c>ObservationGrouping</c> and held by the
/// phase's <see cref="ObservationLedger"/>, so the same group is the same object for the whole phase
/// (the ledger retires instances, and a keyword holds on to the object it names).
///
/// <para>Nothing about the world changes: the members stay in their node, are refreshed there, and are
/// acted on there. The group's goal list is read live from its members, so it is never stale.</para>
/// </summary>
public sealed class ObservationGroup : ObservationObject, IObservationGroup
{
    private readonly string _name;
    private readonly string _phrase;
    private readonly string _lemma;
    private readonly Func<int, string> _describe;
    private readonly bool _clause;

    /// <summary>The objects grouped, in order. May themselves be groups.</summary>
    public IReadOnlyList<NarrativeAnchor> Members { get; }

    IReadOnlyList<NarrativeAnchor> IObservationGroup.Parts => Members;

    public ObservationGroup(string name, string phrase, string lemma, Func<int, string> describe,
                            IReadOnlyList<NarrativeAnchor> members, bool descriptionIsClause = false)
    {
        _name     = name;
        _phrase   = phrase;
        _lemma    = lemma;
        _describe = describe;
        _clause   = descriptionIsClause;
        Members   = members;
    }

    public override string ObservationId => "group:" + _name.ToLowerInvariant().Replace(' ', '_');
    public override string NeutralName   => _name;
    public override string NeutralPhrase => _phrase;
    public override string ReferenceLemma => _lemma;
    public override bool DescriptionIsClause => _clause;
    public override string GenerateNeutralDescription(int locationId = 0) => _describe(locationId);

    /// <summary>
    /// Everything the members afford, read live, with one "walk away" at the end instead of one per
    /// member. A member that is itself a group contributes its own union.
    /// </summary>
    public override List<NarrativeAnchor> SubOutcomes
    {
        get
        {
            var all = new List<NarrativeAnchor>();
            NarrativeAnchor? ignore = null;
            var seen = new HashSet<NarrativeAnchor>(ReferenceEqualityComparer.Instance);
            foreach (var m in Members.OfType<ObservationObject>())
                foreach (var sub in m.SubOutcomes)
                {
                    if (IsIgnore(sub)) { ignore ??= sub; continue; }
                    if (seen.Add(sub)) all.Add(sub);
                }
            if (ignore != null) all.Add(ignore);
            return all;
        }
        protected set { }
    }

    private static bool IsIgnore(NarrativeAnchor a)
        => a is Cathedral.Game.Scene.VerbAction { Verb: Cathedral.Game.Scene.Verbs.IgnoreVerb };
}
