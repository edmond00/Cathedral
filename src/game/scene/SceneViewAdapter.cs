using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
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
///   Spot            → SyntheticSpotObject             (ObservationObject with enter verb)
///   PointOfInterest → SyntheticObservationObject      (ObservationObject with PoI verbs + folded item verbs)
///   SceneNpc        → SyntheticNpcObservationObject   (ObservationObject with NPC verbs)
///   ItemElement     → folded into parent PoI as VerbOutcome SubOutcomes (not a standalone observation)
///
/// Every synthetic ObservationObject receives an IgnoreVerb SubOutcome as its last entry,
/// so the GOAL list always includes a "move on" option without ThinkingExecutor injecting it
/// manually.
/// </summary>
public static class SceneViewAdapter
{
    /// <summary>
    /// Creates a synthetic NarrationNode that represents the current area in the SceneView.
    /// The node's PossibleOutcomes contain synthetic ObservationObjects derived from SceneView entries.
    /// This node can be passed to ObservationPhaseController and ThinkingExecutor.
    /// </summary>
    public static SyntheticNarrationNode ToNarrationNode(SceneView view)
    {
        var node = new SyntheticNarrationNode(
            view.CurrentArea.DisplayName.ToLowerInvariant().Replace(' ', '_'),
            view.CurrentArea.ContextDescription,
            view.CurrentArea.TransitionDescription,
            view.CurrentArea.Keywords,
            view.CurrentArea);

        // Index item entries by the element reference so PoI construction can look them up.
        // Items are NOT observation objects — they are verb targets inside their parent PoI.
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
                // Reachable areas → ObservationObject with MoveToAreaVerb SubOutcome
                node.PossibleOutcomes.Add(new SyntheticAreaObservationObject(reachableArea, entry));
            }
            else if (entry.Source is Spot spot)
            {
                // Spots → synthetic ObservationObject (enterable sub-location)
                node.PossibleOutcomes.Add(new SyntheticSpotObject(spot, entry));
            }
            else if (entry.Source is PointOfInterest poi)
            {
                // PointsOfInterest → ObservationObject; item verb sub-entries are folded in.
                var itemSubEntries = poi.Items
                    .Select(ie => itemEntryByElement.GetValueOrDefault(ie))
                    .Where(e => e != null)
                    .Select(e => e!)
                    .ToList();
                node.PossibleOutcomes.Add(new SyntheticObservationObject(poi, entry, itemSubEntries));
            }
            else if (entry.Source is ItemElement)
            {
                // Items are handled as verb targets inside their parent PoI — skip standalone.
            }
            else if (entry.Source is SceneNpc npc)
            {
                // NPCs → ObservationObject with all NPC verbs (MeetStranger, Attack, etc.)
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
        => new VerbOutcome(new VerbView(IgnoreVerb.Instance, "move on", target), target);
}

/// <summary>
/// A synthetic NarrationNode backed by Scene data.
/// Satisfies the NarrationNode contract so existing pipeline controllers can consume it.
/// </summary>
public class SyntheticNarrationNode : NarrationNode
{
    private readonly string _nodeId;
    private readonly string _contextDescription;
    private readonly string _transitionDescription;
    private readonly List<KeywordInContext> _keywords;

    /// <summary>The Area this node represents, if any.</summary>
    public Area? Area { get; }

    public SyntheticNarrationNode(
        string nodeId,
        string contextDescription,
        string transitionDescription,
        List<KeywordInContext> keywords,
        Area? area = null)
    {
        _nodeId                = nodeId;
        _contextDescription    = contextDescription;
        _transitionDescription = transitionDescription;
        _keywords              = keywords;
        Area                   = area;
    }

    public override string NodeId => _nodeId;
    public override string ContextDescription => _contextDescription;
    public override string TransitionDescription => _transitionDescription;
    public override bool IsEntryNode => false;
    public override List<KeywordInContext> NodeKeywordsInContext => _keywords;

    public override string GenerateNeutralDescription(int locationId = 0) => _contextDescription;
    public override string GenerateEnrichedContextDescription(int locationId = 0) => _contextDescription;
}

/// <summary>
/// A synthetic ObservationObject backed by a <see cref="PointOfInterest"/>.
/// Items inside the PoI are NOT separate observations — their applicable verbs are folded
/// in as SubOutcomes (e.g. "grab the apple", "grab the leaf") so the player can choose
/// a specific verb+item combination during the thinking GOAL phase.
/// Keywords aggregate the PoI's own keywords plus all item keywords.
/// </summary>
public class SyntheticObservationObject : ObservationObject
{
    private readonly PointOfInterest _poi;
    private readonly List<SceneViewEntry> _itemSubEntries;

    public SyntheticObservationObject(PointOfInterest poi, SceneViewEntry entry, List<SceneViewEntry> itemSubEntries)
    {
        _poi            = poi;
        _itemSubEntries = itemSubEntries;

        SubOutcomes = new List<ConcreteOutcome>();

        // Verbs that act on the PoI itself (steal, open door, etc.)
        foreach (var vv in entry.ApplicableVerbs)
            SubOutcomes.Add(new VerbOutcome(vv, poi));

        // Verbs that act on items inside the PoI (e.g. "grab the apple", "grab the leaf")
        foreach (var itemEntry in itemSubEntries)
            foreach (var vv in itemEntry.ApplicableVerbs)
                SubOutcomes.Add(new VerbOutcome(vv, itemEntry.Source));

        SubOutcomes.Add(SceneViewAdapter.MakeIgnoreSubOutcome(poi));
    }

    public override string ObservationId => _poi.DisplayName.ToLowerInvariant().Replace(' ', '_');

    /// Aggregates the PoI's own keywords with all contained items' keywords.
    public override List<KeywordInContext> ObservationKeywordsInContext
    {
        get
        {
            var result = new List<KeywordInContext>(_poi.Keywords);
            foreach (var itemEntry in _itemSubEntries)
                result.AddRange(itemEntry.ObservationKeywords);
            return result;
        }
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var moods = _poi.Moods;
        if (moods.Length == 0) return _poi.DisplayName.ToLowerInvariant();
        var rng = new Random(locationId);
        return $"{moods[rng.Next(moods.Length)]} {_poi.DisplayName.ToLowerInvariant()}";
    }
}

/// <summary>
/// A synthetic ObservationObject backed by a <see cref="Spot"/> (enterable sub-location).
/// </summary>
public class SyntheticSpotObject : ObservationObject
{
    private readonly Spot _spot;

    public SyntheticSpotObject(Spot spot, SceneViewEntry entry)
    {
        _spot       = spot;
        SubOutcomes = new List<ConcreteOutcome>();

        // Expose enter verb as sub-outcome
        foreach (var vv in entry.ApplicableVerbs)
            SubOutcomes.Add(new VerbOutcome(vv, spot));

        SubOutcomes.Add(SceneViewAdapter.MakeIgnoreSubOutcome(spot));
    }

    public override string ObservationId => _spot.DisplayName.ToLowerInvariant().Replace(' ', '_');
    public override List<KeywordInContext> ObservationKeywordsInContext => _spot.Keywords;

    public override string GenerateNeutralDescription(int locationId = 0)
        => _spot.DisplayName.ToLowerInvariant();
}

/// <summary>
/// A synthetic ObservationObject backed by a reachable <see cref="Area"/>.
/// SubOutcomes contain MoveToAreaVerb (and any other applicable verbs) plus IgnoreVerb.
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
    public override List<KeywordInContext> ObservationKeywordsInContext => _area.Keywords;

    public override string GenerateNeutralDescription(int locationId = 0)
        => _area.DisplayName.ToLowerInvariant();
}

/// <summary>
/// A synthetic ObservationObject backed by a <see cref="SceneNpc"/>.
/// SubOutcomes contain all applicable NPC verbs (MeetStranger, Attack, Appease, etc.) plus IgnoreVerb.
/// </summary>
public class SyntheticNpcObservationObject : ObservationObject
{
    private readonly SceneNpc _npc;

    public SyntheticNpcObservationObject(SceneNpc npc, SceneViewEntry entry)
    {
        _npc        = npc;
        SubOutcomes = new List<ConcreteOutcome>();

        foreach (var vv in entry.ApplicableVerbs)
            SubOutcomes.Add(new VerbOutcome(vv, npc));

        SubOutcomes.Add(SceneViewAdapter.MakeIgnoreSubOutcome(npc));
    }

    public override string ObservationId => _npc.DisplayName.ToLowerInvariant().Replace(' ', '_');
    public override List<KeywordInContext> ObservationKeywordsInContext => _npc.Keywords;

    public override string GenerateNeutralDescription(int locationId = 0)
        => _npc.DisplayName.ToLowerInvariant();
}

/// <summary>
/// A synthetic outcome representing a Verb action.
/// </summary>
public class VerbOutcome : ConcreteOutcome
{
    public VerbView VerbView { get; }
    public Element Target { get; }

    public VerbOutcome(VerbView verbView, Element target)
    {
        VerbView = verbView;
        Target   = target;
    }

    public override string DisplayName => VerbView.Verbatim;
    public override string ToNaturalLanguageString() => VerbView.Verbatim;

    public override List<KeywordInContext> OutcomeKeywordsInContext =>
        Target.Keywords.Take(2).ToList();
}
