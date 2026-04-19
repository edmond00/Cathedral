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
/// Keywords for observation text are found dynamically by KeywordFallbackService using
/// the descriptions from Descriptions and DisplayName.
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
            else if (entry.Source is Spot spot)
            {
                node.PossibleOutcomes.Add(new SyntheticSpotObject(spot, entry));
            }
            else if (entry.Source is PointOfInterest poi)
            {
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
            var rng  = new Random(locationId);
            var desc = Area.Descriptions[rng.Next(Area.Descriptions.Count)];
            var lower = char.ToLowerInvariant(desc[0]) + desc[1..];
            return $"{_contextDescription} — {lower}";
        }
        return _contextDescription;
    }
}

/// <summary>
/// A synthetic ObservationObject backed by a <see cref="PointOfInterest"/>.
/// Items inside the PoI are NOT separate observations — their applicable verbs are folded
/// in as SubOutcomes (e.g. "grab the apple", "grab the leaf").
/// </summary>
public class SyntheticObservationObject : ObservationObject
{
    private readonly PointOfInterest _poi;

    public SyntheticObservationObject(PointOfInterest poi, SceneViewEntry entry, List<SceneViewEntry> itemSubEntries)
    {
        _poi = poi;

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

    public override string ObservationId => _poi.DisplayName.ToLowerInvariant().Replace(' ', '_');

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        // Use a random description from the PoI's Descriptions list if available, then mood prefix
        if (_poi.Descriptions.Count > 0)
        {
            var rng = new Random(locationId);
            var desc = _poi.Descriptions[rng.Next(_poi.Descriptions.Count)];
            if (_poi.Moods.Length > 0)
                return $"{_poi.Moods[rng.Next(_poi.Moods.Length)]} {desc}";
            return desc;
        }
        var moods = _poi.Moods;
        if (moods.Length == 0) return _poi.DisplayName.ToLowerInvariant();
        var r = new Random(locationId);
        return $"{moods[r.Next(moods.Length)]} {_poi.DisplayName.ToLowerInvariant()}";
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

        foreach (var vv in entry.ApplicableVerbs)
            SubOutcomes.Add(new VerbOutcome(vv, spot));

        SubOutcomes.Add(SceneViewAdapter.MakeIgnoreSubOutcome(spot));
    }

    public override string ObservationId => _spot.DisplayName.ToLowerInvariant().Replace(' ', '_');

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        if (_spot.Descriptions.Count > 0)
        {
            var rng = new Random(locationId);
            return _spot.Descriptions[rng.Next(_spot.Descriptions.Count)];
        }
        return _spot.DisplayName.ToLowerInvariant();
    }
}

/// <summary>
/// A synthetic ObservationObject backed by a reachable <see cref="Area"/>.
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

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        if (_area.Descriptions.Count > 0)
        {
            var rng = new Random(locationId);
            return _area.Descriptions[rng.Next(_area.Descriptions.Count)];
        }
        return _area.DisplayName.ToLowerInvariant();
    }
}

/// <summary>
/// A synthetic ObservationObject backed by a <see cref="SceneNpc"/>.
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

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        if (_npc.Descriptions.Count > 0)
        {
            var rng = new Random(locationId);
            return _npc.Descriptions[rng.Next(_npc.Descriptions.Count)];
        }
        return _npc.DisplayName.ToLowerInvariant();
    }
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
}
