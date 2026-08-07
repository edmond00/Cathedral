using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// Bridges the new Scene system with the existing LLM narrative pipeline.
///
/// The existing pipeline consumes NarrationNode/ConcreteOutcome/ObservationObject types
/// to produce NarrationBlocks. This adapter converts a <see cref="SceneView"/> into
/// the equivalent structures, allowing Scene-based locations to feed the pipeline
/// without rewriting the controllers.
///
/// Key mappings:
///   Area            → SyntheticAreaObservationObject  (ObservationObject with MoveToAreaVerb)
///   PointOfInterest → SyntheticObservationObject      (ObservationObject with PoI verbs + folded item verbs)
///   SceneNpc        → SyntheticNpcObservationObject   (ObservationObject with NPC verbs)
///   ItemElement     → folded into parent PoI as VerbOutcome SubOutcomes (not a standalone observation)
///
/// Observation text is generated from these Descriptions / DisplayName; the persona rewrite
/// selects the clickable keyword from its own styled sentence.
/// </summary>
public static class SceneViewAdapter
{
    /// <summary>
    /// Creates a synthetic NarrationNode that represents the current area in the SceneView.
    /// The node's PossibleOutcomes contain synthetic ObservationObjects derived from SceneView entries.
    /// </summary>
    public static SyntheticNarrationNode ToNarrationNode(SceneView view)
    {
        var node = new SyntheticNarrationNode(
            view.CurrentArea.DisplayName.ToLowerInvariant().Replace(' ', '_'),
            view.CurrentArea.ContextDescription,
            view.CurrentArea.TransitionDescription,
            view.CurrentArea);

        // Index item entries by the element reference so PoI construction can look them up.
        var itemEntryByElement = new Dictionary<Element, SceneViewEntry>();
        foreach (var entry in view.Entries)
        {
            if (entry.Source is ItemElement)
                itemEntryByElement[entry.Source] = entry;
        }

        // Convert each entry to an ObservationObject
        foreach (var entry in view.Entries)
        {
            if (entry.Source is Area area && area.Id == view.CurrentArea.Id)
                continue; // Skip current area (it IS the node)

            if (entry.Source is Area reachableArea)
            {
                node.PossibleOutcomes.Add(new SyntheticAreaObservationObject(reachableArea, entry));
            }
            else if (entry.Source is PointOfInterest poi)
            {
                var itemSubEntries = poi.Items
                    .Select(ie => itemEntryByElement.GetValueOrDefault(ie))
                    .Where(e => e != null)
                    .Select(e => e!)
                    .ToList();
                node.PossibleOutcomes.Add(new SyntheticObservationObject(
                    poi, entry, itemSubEntries, view.CurrentArea, view.CurrentPeriod));
            }
            else if (entry.Source is ItemElement)
            {
                // Items are handled as verb targets inside their parent PoI — skip standalone.
            }
            else if (entry.Source is SceneNpc npc)
            {
                node.PossibleOutcomes.Add(new SyntheticNpcObservationObject(npc, entry));
            }
        }

        return node;
    }

    /// <summary>
    /// Creates a VerbOutcome wrapping IgnoreVerb for the given element.
    /// Injected as the last SubOutcome of every synthetic ObservationObject.
    /// </summary>
    public static VerbOutcome MakeIgnoreSubOutcome(Element target)
        => new VerbOutcome(new VerbView(IgnoreVerb.Instance, IgnoreVerb.VerbatimText, target), target);

    /// <summary>
    /// The RNG that picks which of an element's descriptions and moods are used when it is observed.
    ///
    /// <para>Seeded per element, not per location. Seeding on <paramref name="locationId"/> alone —
    /// which every one of these call sites used to do, and with a hard-coded 0 at that — made every
    /// object in a scene draw the same index, so a location's whole multi-description and mood
    /// vocabulary collapsed to one entry. <see cref="Element.StableKey"/> is assigned in deterministic
    /// build order, so the pick varies between objects while staying identical across rebuilds of the
    /// same location.</para>
    /// </summary>
    public static Random DescriptionRng(Element element, int locationId)
        => element.StableKey.Length > 0
            ? new Random(GameRng.DerivedSeed($"obs|{locationId}|{element.StableKey}"))
            : new Random(locationId);

    /// <summary>
    /// Picks a mood adjective to prefix <paramref name="description"/> with, skipping any that the
    /// description already uses. Returns null when every mood is already in there, in which case the
    /// description stands alone.
    ///
    /// <para>Without the check the prefix stutters — the well whose mood is "stone-rimmed" and whose
    /// description opens "A stone-rimmed well…" came out as "a stone-rimmed stone-rimmed well". The
    /// mood pools were written to echo the descriptions, so this is the common case rather than a
    /// rare collision.</para>
    /// </summary>
    public static string? PickMood(string[] moods, string description, Random rng)
    {
        if (moods.Length == 0) return null;

        var words = new HashSet<string>(
            description.Split(' ', ',', '.', ';', ':', '—', '(', ')'),
            StringComparer.OrdinalIgnoreCase);

        // Start at a random offset and take the first mood the description does not already use, so
        // the choice stays varied rather than always falling to the first free entry.
        int start = rng.Next(moods.Length);
        for (int i = 0; i < moods.Length; i++)
        {
            var mood = moods[(start + i) % moods.Length];
            if (!words.Contains(mood)) return mood;
        }
        return null;
    }
}

/// <summary>
/// A synthetic NarrationNode backed by Scene data.
/// </summary>
public class SyntheticNarrationNode : NarrationNode
{
    private readonly string _nodeId;
    private readonly string _contextDescription;
    private readonly string _transitionDescription;

    /// <summary>The Area this node represents, if any.</summary>
    public Area? Area { get; }

    public SyntheticNarrationNode(
        string nodeId,
        string contextDescription,
        string transitionDescription,
        Area? area = null)
    {
        _nodeId                = nodeId;
        _contextDescription    = contextDescription;
        _transitionDescription = transitionDescription;
        Area                   = area;
    }

    public override string NodeId => _nodeId;
    public override string ContextDescription => _contextDescription;
    public override string TransitionDescription => _transitionDescription;
    public override bool IsEntryNode => false;

    public override string GenerateNeutralDescription(int locationId = 0)
        => Area?.DisplayName.ToLowerInvariant() ?? _contextDescription;

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        if (Area?.Descriptions.Count > 0)
        {
            var rng  = SceneViewAdapter.DescriptionRng(Area, locationId);
            var desc = Area.Descriptions[rng.Next(Area.Descriptions.Count)];
            var lower = char.ToLowerInvariant(desc[0]) + desc[1..];
            return $"{_contextDescription} — {lower}";
        }
        return _contextDescription;
    }
}

/// <summary>
/// A scene-backed observation object whose verb SubOutcomes can be re-expanded against the CURRENT
/// scene state. The synthetic objects bake their verb lists at graph-build time, but verb
/// applicability is state-dependent (affinity, item presence, depletion …), so the controller
/// refreshes them at each narration-segment start and before each thinking request. Implementations
/// mutate <see cref="ObservationObject.SubOutcomes"/> in place, preserving object identity in the
/// node (keyword→outcome maps hold references to these instances).
/// </summary>
public interface IVerbRefreshable
{
    void RefreshVerbs(Scene scene, PoV pov, PartyMember? actor = null);
}

/// <summary>
/// A synthetic ObservationObject backed by a <see cref="PointOfInterest"/>.
/// Items inside the PoI are NOT separate observations — their applicable verbs are folded
/// in as SubOutcomes (e.g. "grab the apple", "grab the leaf").
/// </summary>
public class SyntheticObservationObject : ObservationObject, IVerbRefreshable, IPeriodStampable
{
    private readonly PointOfInterest _poi;

    /// <summary>
    /// The point of interest behind this observation. Exposed so placement can tell one observation
    /// from another by what it is <i>of</i> — which is how a bed's observation is found and taken out
    /// when the person whose bed it is climbs into it.
    /// </summary>
    public PointOfInterest PointOfInterest => _poi;

    /// <summary>
    /// The area this observation is made from. A connector PoI (door, stair, path) sits in two areas'
    /// PoI lists and therefore yields two distinct observation objects — this is what tells them
    /// apart, so a door can read one way from the street and another from the hall inside.
    /// </summary>
    private readonly Area? _viewingArea;

    /// <summary>Live time of day, stamped by the controller. See <see cref="IPeriodStampable"/>.</summary>
    private TimePeriod? _period;

    public SyntheticObservationObject(
        PointOfInterest poi,
        SceneViewEntry entry,
        List<SceneViewEntry> itemSubEntries,
        Area? viewingArea = null,
        TimePeriod? period = null)
    {
        _poi         = poi;
        _viewingArea = viewingArea;
        _period      = period;

        SubOutcomes = new List<ConcreteOutcome>();

        // Verbs that act on the PoI itself
        foreach (var vv in entry.ApplicableVerbs)
            SubOutcomes.Add(new VerbOutcome(vv, poi));

        // Verbs that act on items inside the PoI
        foreach (var itemEntry in itemSubEntries)
            foreach (var vv in itemEntry.ApplicableVerbs)
                SubOutcomes.Add(new VerbOutcome(vv, itemEntry.Source));

        SubOutcomes.Add(SceneViewAdapter.MakeIgnoreSubOutcome(poi));
    }

    /// <inheritdoc cref="IVerbRefreshable"/>
    public void RefreshVerbs(Scene scene, PoV pov, PartyMember? actor = null)
    {
        SubOutcomes.Clear();

        foreach (var verb in scene.Verbs)
            foreach (var vv in verb.ExpandViews(scene, pov, _poi, actor))
                SubOutcomes.Add(new VerbOutcome(vv, _poi));

        // Items are re-read from the PoI, so grabbed/expired items drop their verbs naturally.
        foreach (var item in _poi.Items)
            foreach (var verb in scene.Verbs)
                foreach (var vv in verb.ExpandViews(scene, pov, item, actor))
                    SubOutcomes.Add(new VerbOutcome(vv, item));

        SubOutcomes.Add(SceneViewAdapter.MakeIgnoreSubOutcome(_poi));
    }

    /// <inheritdoc cref="IPeriodStampable"/>
    public void StampPeriod(TimePeriod period) => _period = period;

    public override string ObservationId => _poi.DisplayName.ToLowerInvariant().Replace(' ', '_');

    public override string NeutralName => _poi.DisplayName;

    public override string ReferenceLemma => _poi.ReferenceLemma;

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        // A door describes itself differently from either side and says whether it looks shut, so it
        // supersedes the static Descriptions list whenever we know where we are standing.
        if (_poi is IContextualDescription contextual && _viewingArea != null)
            return contextual.DescribeFrom(_viewingArea, _period ?? TimePeriod.Morning);

        // Use a random description from the PoI's Descriptions list if available, then mood prefix
        var rng = SceneViewAdapter.DescriptionRng(_poi, locationId);
        if (_poi.Descriptions.Count > 0)
        {
            var desc = _poi.Descriptions[rng.Next(_poi.Descriptions.Count)];
            var mood = SceneViewAdapter.PickMood(_poi.Moods, desc, rng);
            return mood == null ? desc : $"{mood} {desc}";
        }

        var name      = _poi.DisplayName.ToLowerInvariant();
        var nameMood  = SceneViewAdapter.PickMood(_poi.Moods, name, rng);
        return nameMood == null ? name : $"{nameMood} {name}";
    }
}

/// <summary>
/// A synthetic ObservationObject backed by a reachable <see cref="Area"/>.
/// Deliberately NOT <see cref="IVerbRefreshable"/>: its one verb is the hand-wired
/// <see cref="MoveToAreaVerb"/> transition (not registry-expanded — see the graph factory's
/// area wiring), which a registry re-expansion would silently drop.
/// </summary>
public class SyntheticAreaObservationObject : ObservationObject
{
    private readonly Area _area;

    public SyntheticAreaObservationObject(Area area, SceneViewEntry entry)
    {
        _area       = area;
        SubOutcomes = new List<ConcreteOutcome>();

        foreach (var vv in entry.ApplicableVerbs)
            SubOutcomes.Add(new VerbOutcome(vv, area));

        SubOutcomes.Add(SceneViewAdapter.MakeIgnoreSubOutcome(area));
    }

    public override string ObservationId => _area.DisplayName.ToLowerInvariant().Replace(' ', '_');

    public override string NeutralName => _area.DisplayName;

    public override string ReferenceLemma => _area.ReferenceLemma;

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        if (_area.Descriptions.Count > 0)
        {
            var rng = SceneViewAdapter.DescriptionRng(_area, locationId);
            return _area.Descriptions[rng.Next(_area.Descriptions.Count)];
        }
        return _area.DisplayName.ToLowerInvariant();
    }
}

/// <summary>
/// A synthetic ObservationObject backed by a <see cref="SceneNpc"/>.
/// </summary>
public class SyntheticNpcObservationObject : ObservationObject, INpcContextLabelStampable, IVerbRefreshable
{
    private readonly SceneNpc _npc;

    /// <summary>
    /// Contextual label (relation + role + location) substituted for the proper name in LLM
    /// prompts. Only set for named (<see cref="NpcEntity"/>) NPCs; stays null for shallow wildlife,
    /// which keeps its current type/appearance text. Reads fall back to the display name.
    /// </summary>
    public string? ContextLabel { get; private set; }

    /// <summary>
    /// The backing named NPC entity, or null for shallow (unnamed) wildlife. Lets a caller match this
    /// observation object to a specific <see cref="NpcEntity"/> — used to re-observe the NPC a dialogue
    /// was just held with when a new observation phase opens.
    /// </summary>
    public NpcEntity? NpcEntity => _npc.Entity as NpcEntity;

    public SyntheticNpcObservationObject(SceneNpc npc, SceneViewEntry entry)
    {
        _npc        = npc;
        SubOutcomes = new List<ConcreteOutcome>();

        foreach (var vv in entry.ApplicableVerbs)
            SubOutcomes.Add(new VerbOutcome(vv, npc));

        SubOutcomes.Add(SceneViewAdapter.MakeIgnoreSubOutcome(npc));
    }

    /// <summary>
    /// Placement constructor: builds the object without pre-expanded verbs. Used by
    /// <see cref="SceneNpcPlacement"/>, which owns one instance per NPC and moves it between area
    /// nodes each period; the verb SubOutcomes are then filled in by <see cref="RefreshVerbs"/>
    /// (which the controller runs against the current period before observation/thinking). Seeds
    /// with the IGNORE sub-outcome only so the object is never verb-less before its first refresh.
    /// </summary>
    public SyntheticNpcObservationObject(SceneNpc npc)
    {
        _npc        = npc;
        SubOutcomes = new List<ConcreteOutcome> { SceneViewAdapter.MakeIgnoreSubOutcome(npc) };
    }

    /// <summary>
    /// <inheritdoc cref="IVerbRefreshable"/>
    /// For NPCs the load-bearing gate is affinity — "meet …, to introduce myself" is only for
    /// strangers, strengthen-relationship only for acquaintances — so without this a dialogue's
    /// affinity change would neither retire nor unlock verbs until the location was re-entered.
    /// The stamped <see cref="ContextLabel"/>, if any, is re-applied to the fresh VerbOutcomes.
    /// </summary>
    public void RefreshVerbs(Scene scene, PoV pov, PartyMember? actor = null)
    {
        SubOutcomes.Clear();
        foreach (var verb in scene.Verbs)
            foreach (var vv in verb.ExpandViews(scene, pov, _npc, actor))
                SubOutcomes.Add(new VerbOutcome(vv, _npc));
        SubOutcomes.Add(SceneViewAdapter.MakeIgnoreSubOutcome(_npc));

        if (ContextLabel != null)
            foreach (var sub in SubOutcomes)
                if (sub is VerbOutcome v) v.ContextLabel = ContextLabel;
    }

    /// <inheritdoc/>
    public void StampContextLabel(PartyMember? actingMember, WorldContext? world, int locationId)
    {
        // Named NPCs only — shallow wildlife has no affinity and keeps its current behavior.
        if (_npc.Entity is not NpcEntity named) return;

        ContextLabel = NpcLabelResolver.Resolve(named, world, locationId, actingMember);
        foreach (var sub in SubOutcomes)
            if (sub is VerbOutcome v) v.ContextLabel = ContextLabel;
    }

    public override string ObservationId => _npc.DisplayName.ToLowerInvariant().Replace(' ', '_');

    public override string NeutralName => Label;

    public override string NeutralPhrase => Label;

    public override string ReferenceLemma => "person";          // names aren't in the embedding vocab

    /// <summary>
    /// Named NPC: the stamped contextual label (else the proper name). Shallow NPC: the clean articled
    /// type noun ("a crab", "an owl") so it reads naturally mid-sentence rather than the bare "Crab".
    /// </summary>
    private string Label => ContextLabel
        ?? (_npc.Entity is ShallowNpcEntity
            ? Cathedral.Game.NeutralNarration.NounPhrase(_npc.DisplayName.ToLowerInvariant())
            : _npc.DisplayName);

    public override string GenerateNeutralDescription(int locationId = 0)
        // Both named and shallow entities carry an appearance-only ObservationHint (name-free; the
        // shallow one is composed with per-NPC-id variation). This supersedes the old SceneNpc.Descriptions
        // path, which only ever held the bare DisplayName ("Crab").
        => _npc.Entity.ObservationHint;
}

/// <summary>
/// A synthetic outcome representing a Verb action.
/// </summary>
public class VerbOutcome : ConcreteOutcome
{
    public VerbView  VerbView { get; }
    public Element?  Target   { get; }

    /// <summary>
    /// Contextual NPC label substituted for the target's proper name in the LLM-facing goal
    /// phrase. Null unless the parent observation object stamps it (named NPCs only); the
    /// human-facing <see cref="DisplayName"/> always keeps the verbatim verb text.
    /// </summary>
    public string? ContextLabel { get; set; }

    public VerbOutcome(VerbView verbView, Element? target)
    {
        VerbView = verbView;
        Target   = target;
    }

    public override string DisplayName => VerbView.Verbatim;

    public override string ToNaturalLanguageString()
    {
        if (ContextLabel == null || Target is not SceneNpc n)
            return VerbView.Verbatim;

        string name = n.DisplayName;

        // Some verbs prefix the bare name with a determiner and lower-case it (e.g. SlayVerb's
        // "slay the edmund sheaf"). Drop that determiner when swapping in the contextual label —
        // it already carries its own article ("the reaper of the field (a man)"), so we must not
        // produce "slay the the reaper …". Case-insensitive so a lower-cased name still matches.
        string withArticle = VerbView.Verbatim.Replace($"the {name}", ContextLabel, System.StringComparison.OrdinalIgnoreCase);
        if (withArticle != VerbView.Verbatim)
            return withArticle;

        return VerbView.Verbatim.Replace(name, ContextLabel, System.StringComparison.OrdinalIgnoreCase);
    }
}
