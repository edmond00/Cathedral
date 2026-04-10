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
///   Area → synthetic NarrationNode (with SceneView-sourced keywords and outcomes)
///   Spot → synthetic ObservationObject (as an enterable sub-location)
///   PointOfInterest → synthetic ObservationObject (with items as sub-outcomes)
///   ItemElement → existing Item (direct pass-through)
///   VerbView → synthetic ConcreteOutcome (with verb verbatim as transition description)
/// </summary>
public static class SceneViewAdapter
{
    /// <summary>
    /// Creates a synthetic NarrationNode that represents the current area in the SceneView.
    /// The node's PossibleOutcomes contain synthetic outcomes derived from SceneView entries.
    /// This node can be passed to ObservationPhaseController and ThinkingExecutor.
    /// </summary>
    public static SyntheticNarrationNode ToNarrationNode(SceneView view)
    {
        var node = new SyntheticNarrationNode(
            view.CurrentArea.DisplayName.ToLowerInvariant().Replace(' ', '_'),
            view.CurrentArea.ContextDescription,
            view.CurrentArea.TransitionDescription,
            view.CurrentArea.Keywords);

        // Convert each entry to an outcome
        foreach (var entry in view.Entries)
        {
            if (entry.Source is Area area && area.Id == view.CurrentArea.Id)
                continue; // Skip current area (it IS the node)

            if (entry.Source is Area reachableArea)
            {
                // Reachable areas → VerbOutcome wrapping MoveToAreaVerb
                foreach (var vv in entry.ApplicableVerbs)
                    node.PossibleOutcomes.Add(new VerbOutcome(vv, reachableArea));
            }
            else if (entry.Source is Spot spot)
            {
                // Spots → synthetic ObservationObject (enterable sub-location)
                var obs = new SyntheticSpotObject(spot, entry);
                node.PossibleOutcomes.Add(obs);
            }
            else if (entry.Source is PointOfInterest poi)
            {
                // PointsOfInterest → synthetic ObservationObject
                var obs = new SyntheticObservationObject(poi, entry);
                node.PossibleOutcomes.Add(obs);
            }
            else if (entry.Source is ItemElement itemElement)
            {
                // Items → pass through (Item extends ConcreteOutcome)
                node.PossibleOutcomes.Add(itemElement.Item);
            }
            else if (entry.Source is SceneNpc npc)
            {
                // NPCs → synthetic outcome
                node.PossibleOutcomes.Add(new NpcElementOutcome(npc, entry));
            }
        }

        return node;
    }

    /// <summary>
    /// Given a keyword click on a SceneView, resolves which VerbView(s) are applicable
    /// and returns them paired with their target elements.
    /// </summary>
    public static List<VerbView> GetVerbsForKeyword(SceneView view, string keyword)
    {
        var result = new List<VerbView>();
        foreach (var entry in view.Entries)
        {
            if (entry.ObservationKeywords.Any(k =>
                    string.Equals(k.Keyword, keyword, StringComparison.OrdinalIgnoreCase)))
            {
                result.AddRange(entry.ApplicableVerbs);
            }
        }
        return result;
    }
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

    public SyntheticNarrationNode(
        string nodeId,
        string contextDescription,
        string transitionDescription,
        List<KeywordInContext> keywords)
    {
        _nodeId                = nodeId;
        _contextDescription    = contextDescription;
        _transitionDescription = transitionDescription;
        _keywords              = keywords;
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
/// </summary>
public class SyntheticObservationObject : ObservationObject
{
    private readonly PointOfInterest _poi;
    private readonly SceneViewEntry _entry;

    public SyntheticObservationObject(PointOfInterest poi, SceneViewEntry entry)
    {
        _poi   = poi;
        _entry = entry;

        SubOutcomes = new List<ConcreteOutcome>();
        foreach (var itemElement in poi.Items)
            SubOutcomes.Add(itemElement.Item);

        foreach (var vv in entry.ApplicableVerbs)
            SubOutcomes.Add(new VerbOutcome(vv, poi));
    }

    public override string ObservationId => _poi.DisplayName.ToLowerInvariant().Replace(' ', '_');
    public override List<KeywordInContext> ObservationKeywordsInContext => _poi.Keywords;

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
    }

    public override string ObservationId => _spot.DisplayName.ToLowerInvariant().Replace(' ', '_');
    public override List<KeywordInContext> ObservationKeywordsInContext => _spot.Keywords;

    public override string GenerateNeutralDescription(int locationId = 0)
        => _spot.DisplayName.ToLowerInvariant();
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

/// <summary>
/// A synthetic outcome representing an NPC in the scene.
/// </summary>
public class NpcElementOutcome : ConcreteOutcome
{
    public SceneNpc Npc { get; }
    private readonly SceneViewEntry _entry;

    public NpcElementOutcome(SceneNpc npc, SceneViewEntry entry)
    {
        Npc    = npc;
        _entry = entry;
    }

    public override string DisplayName => Npc.DisplayName;
    public override string ToNaturalLanguageString() => $"interact with {Npc.DisplayName}";
    public override List<KeywordInContext> OutcomeKeywordsInContext => Npc.Keywords;
}
