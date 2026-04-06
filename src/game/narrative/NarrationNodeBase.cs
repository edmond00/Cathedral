using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents a discrete narrative context within a location that can be reached as an outcome.
/// Nodes have their own keywords (for being discovered as transitions) plus keywords from immediate outcomes.
/// Neutral descriptions are generated with random qualifiers for variety.
/// Implements IObservation: a node IS its own observation whose outcomes are its items + child NarrationNodes.
/// </summary>
public abstract class NarrationNode : ConcreteOutcome, IObservation
{
    /// <summary>
    /// Unique identifier for this node (e.g., "clearing", "stream").
    /// </summary>
    public abstract string NodeId { get; }
    
    /// <summary>
    /// Short context description used in critic prompts (e.g., "exploring a clearing", "examining a stream").
    /// This provides context to the LLM about what the player is currently doing at this node.
    /// </summary>
    public abstract string ContextDescription { get; }
    
    /// <summary>
    /// Natural language description for transitioning to this node (e.g., "approach the stream").
    /// Used in LLM prompts to describe possible outcomes.
    /// </summary>
    public abstract string TransitionDescription { get; }
    
    /// <summary>
    /// All possible outcomes available from this node.
    /// Populated at runtime by NarrationGraphFactory.
    /// </summary>
    public List<OutcomeBase> PossibleOutcomes { get; set; } = new();
    
    /// <summary>
    /// Can this node be used as an entry point when entering the location?
    /// </summary>
    public abstract bool IsEntryNode { get; }
    
    /// <summary>
    /// Keywords with surrounding context that describe what can be noticed at this node.
    /// Each entry is a contextual phrase like "a rough bark of the beech" with the keyword
    /// word marked in the raw source. Used for LLM observation hints and UI keyword display.
    /// </summary>
    public abstract List<KeywordInContext> NodeKeywordsInContext { get; }
    
    /// <summary>
    /// NPC encounter slots for this node.  Used by <see cref="NarrationGraphFactory.BuildNpcs"/>
    /// at graph-construction time to decide whether to include an NPC in this location
    /// (SpawnChance = graph inclusion probability).  Override in subclasses to declare
    /// which archetypes can appear here.
    /// Empty by default (no encounters).
    /// </summary>
    public virtual List<NpcEncounterSlot> PossibleEncounters => new();

    /// <summary>
    /// Returns the items available at this node. Override in subclasses to list items explicitly.
    /// Replaces the old reflection-based discovery — items are declared here, not auto-discovered.
    /// </summary>
    public virtual List<Item> GetItems() => new();

    /// <summary>
    /// Gets all items available at this node. Delegates to <see cref="GetItems"/>.
    /// </summary>
    public List<Item> GetAvailableItems()
    {
        var items = GetItems();
        
        return items;
    }
    
    /// <summary>
    /// All keywords available at this node: node's own keywords plus keywords from items and child nodes.
    /// Deduplicated by bare keyword (case-insensitive); first occurrence wins.
    /// </summary>
    public override List<KeywordInContext> OutcomeKeywordsInContext
    {
        get
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allKeywords = new List<KeywordInContext>();

            void Add(KeywordInContext kic)
            {
                if (seen.Add(kic.Keyword)) allKeywords.Add(kic);
            }

            foreach (var kic in NodeKeywordsInContext) Add(kic);

            // Add keywords from items discovered via reflection
            var items = GetAvailableItems();
            foreach (var item in items)
                foreach (var kic in item.OutcomeKeywordsInContext) Add(kic);

            // Add keywords from child NarrationNodes and ObservationObjects
            foreach (var outcome in PossibleOutcomes)
            {
                if (outcome is NarrationNode childNode)
                    foreach (var kic in childNode.NodeKeywordsInContext) Add(kic);
                else if (outcome is ObservationObject obs)
                    foreach (var kic in obs.ObservationKeywordsInContext) Add(kic);
                // Sub-outcome keywords are NOT bubbled up here — only accessed inside the observation
            }

            return allKeywords;
        }
    }
    
    /// <summary>
    /// Gets keywords grouped by their source outcome.
    /// Returns a dictionary mapping outcome display names to their associated keywords.
    /// </summary>
    public Dictionary<string, List<KeywordInContext>> GetKeywordsByOutcome()
    {
        var result = new Dictionary<string, List<KeywordInContext>>();

        if (NodeKeywordsInContext.Count > 0)
            result[NodeId] = new List<KeywordInContext>(NodeKeywordsInContext);

        var items = GetAvailableItems();
        foreach (var item in items)
            if (item.OutcomeKeywordsInContext.Count > 0)
                result[item.DisplayName] = new List<KeywordInContext>(item.OutcomeKeywordsInContext);

        foreach (var outcome in PossibleOutcomes)
            if (outcome is NarrationNode childNode && childNode.NodeKeywordsInContext.Count > 0)
                result[childNode.DisplayName] = new List<KeywordInContext>(childNode.NodeKeywordsInContext);

        return result;
    }
    
    /// <summary>
    /// Display name is just the node type without qualifiers (e.g., "clearing" not "sun-dappled clearing").
    /// </summary>
    public override string DisplayName => NodeId;
    
    /// <summary>
    /// Generates a neutral description with random qualifiers for variety.
    /// Override this to provide node-specific description generation.
    /// </summary>
    /// <param name="locationId">Location ID used as RNG seed for consistency</param>
    public abstract string GenerateNeutralDescription(int locationId = 0);

    /// <inheritdoc/>
    /// For narration nodes, the keyword is one of the NodeKeywordsInContext bare keywords.
    /// Example: keyword="stream" → "This stream is part of a mossy forest clearing."
    public override string GetKeywordToOutcomeTransition(string keyword, KeywordInContext? keywordInContext = null)
    {
        string desc = GenerateNeutralDescription(0);
        string withArticle = desc.Length > 0 && "aeiouAEIOU".Contains(desc[0]) ? $"an {desc}" : $"a {desc}";
        return keywordInContext != null
            ? $"It is part of {withArticle}."
            : $"This {keyword} is part of {withArticle}.";
    }

    /// <summary>
    /// Generates an enriched context description that includes a mood qualifier.
    /// Default: returns ContextDescription as-is.
    /// Override in nodes that have a Moods array to inject the mood into the verb phrase.
    /// Example override: $"wandering through a {mood} brightwood"
    /// </summary>
    public virtual string GenerateEnrichedContextDescription(int locationId = 0)
        => ContextDescription;

    /// <summary>
    /// Builds the two-line location context used at the start of every first LLM call.
    /// Line 1: world context with stable flavor ("You are in a gloomy forest.")
    /// Line 2: enriched node context ("You are currently wandering through a mossy brightwood.")
    /// </summary>
    public string BuildLocationContext(WorldContext worldContext, int locationId)
        => $"You are in a {worldContext.GenerateContextDescription(locationId)}. You are currently {GenerateEnrichedContextDescription(locationId)}.";
    
    /// <summary>
    /// Gets all outcomes that have a specific keyword.
    /// Includes both child nodes and items discovered via reflection.
    /// These are "straightforward" outcomes - directly linked to the keyword.
    /// </summary>
    public List<OutcomeBase> GetOutcomesForKeyword(string keyword)
    {
        var normalizedKeyword = keyword.ToLowerInvariant();
        var outcomes = new List<OutcomeBase>();

        outcomes.AddRange(PossibleOutcomes
            .Where(outcome => outcome is ConcreteOutcome co &&
                co.OutcomeKeywordsInContext.Any(k => k.Keyword.ToLowerInvariant() == normalizedKeyword)));

        var items = GetAvailableItems();
        outcomes.AddRange(items
            .Where(item => item.OutcomeKeywordsInContext.Any(k => k.Keyword.ToLowerInvariant() == normalizedKeyword)));

        return outcomes;
    }
    
    public override string ToNaturalLanguageString()
    {
        return TransitionDescription;
    }

    // ── IObservation ──────────────────────────────────────────────────────────
    string IObservation.ObservationId => NodeId;
    List<KeywordInContext> IObservation.ObservationKeywords => NodeKeywordsInContext;
    IReadOnlyList<ConcreteOutcome> IObservation.ObservationOutcomes
    {
        get
        {
            var result = new List<ConcreteOutcome>();
            result.AddRange(GetAvailableItems());
            result.AddRange(PossibleOutcomes.OfType<NarrationNode>());
            return result.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all concrete outcomes directly available at this node (child nodes + items + spawned NPCs).
    /// Used for sampling which outcomes to generate observation sentences for.
    /// </summary>
    public List<ConcreteOutcome> GetAllDirectConcreteOutcomes()
    {
        var outcomes = new List<ConcreteOutcome>();

        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is NarrationNode childNode && childNode.NodeKeywordsInContext.Count > 0)
                outcomes.Add(childNode);
            else if (outcome is ObservationObject obs && obs.ObservationKeywordsInContext.Count > 0)
                outcomes.Add(obs);
            else if (outcome is ConcreteOutcome co && !(outcome is NarrationNode) && !(outcome is ObservationObject) && co.OutcomeKeywordsInContext.Count > 0)
                outcomes.Add(co);
        }

        foreach (var item in GetAvailableItems())
        {
            if (item.OutcomeKeywordsInContext.Count > 0)
                outcomes.Add(item);
        }

        return outcomes;
    }

    /// <summary>
    /// Gets all observations at this node as IObservation instances:
    /// ObservationObjects, child NarrationNodes, and items (each self-referential).
    /// </summary>
    public List<IObservation> GetObservations()
    {
        var result = new List<IObservation>();
        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is ObservationObject obs) result.Add(obs);
            else if (outcome is NarrationNode nn) result.Add(nn);
        }
        foreach (var item in GetAvailableItems())
            result.Add(item);
        return result;
    }


    /// If outcomes >= targetCount, samples one keyword from targetCount random outcomes.
    /// If outcomes < targetCount, samples multiple keywords per outcome to reach target.
    /// </summary>
    /// <param name="random">Random instance for consistent sampling</param>
    /// <param name="targetKeywordCount">Target number of keywords to return</param>
    /// <returns>List of (keyword-in-context, outcome) tuples</returns>
    public List<(KeywordInContext Kic, ConcreteOutcome Outcome)> GetRepresentativeKeywordsPerOutcome(Random random, int targetKeywordCount = Cathedral.Config.Narrative.TargetKeywordCount)
    {
        var result = new List<(KeywordInContext, ConcreteOutcome)>();
        
        // Get all immediate concrete outcomes (direct child nodes + items at this node)
        var concreteOutcomes = new List<ConcreteOutcome>();
        
        // Add child nodes, observations, and other concrete outcomes
        // NOTE: We only add the node/observation itself, not its recursive keywords
        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is ConcreteOutcome co)
            {
                if (outcome is NarrationNode node && node.NodeKeywordsInContext.Count > 0)
                    concreteOutcomes.Add(co);
                else if (outcome is ObservationObject obs && obs.ObservationKeywordsInContext.Count > 0)
                    concreteOutcomes.Add(co);
                else if (!(outcome is NarrationNode) && !(outcome is ObservationObject) && co.OutcomeKeywordsInContext.Count > 0)
                    concreteOutcomes.Add(co);
            }
        }
        
        // Add items discovered via reflection at THIS node only
        var items = GetAvailableItems();
        foreach (var item in items)
        {
            if (item.OutcomeKeywordsInContext.Count > 0)
            {
                concreteOutcomes.Add(item);
            }
        }
        
        // Apply new sampling logic based on target keyword count
        if (concreteOutcomes.Count == 0)
        {
            return result;
        }
        
        if (concreteOutcomes.Count >= targetKeywordCount)
        {
            // More outcomes than target: sample targetKeywordCount outcomes, one keyword each
            var shuffledOutcomes = concreteOutcomes.OrderBy(_ => random.Next()).Take(targetKeywordCount);
            foreach (var outcome in shuffledOutcomes)
            {
                List<KeywordInContext> kicsToUse = outcome is NarrationNode node ? node.NodeKeywordsInContext
                    : outcome is ObservationObject obs ? obs.ObservationKeywordsInContext
                    : outcome.OutcomeKeywordsInContext;

                if (kicsToUse.Count > 0)
                {
                    var selectedKic = kicsToUse[random.Next(kicsToUse.Count)];
                    result.Add((selectedKic, outcome));
                }
            }
        }
        else
        {
            var keywordsPerOutcome = targetKeywordCount / concreteOutcomes.Count;
            var extraKeywords = targetKeywordCount % concreteOutcomes.Count;

            for (int i = 0; i < concreteOutcomes.Count; i++)
            {
                var outcome = concreteOutcomes[i];
                List<KeywordInContext> kicsToUse = outcome is NarrationNode node ? node.NodeKeywordsInContext
                    : outcome is ObservationObject obs ? obs.ObservationKeywordsInContext
                    : outcome.OutcomeKeywordsInContext;

                if (kicsToUse.Count == 0) continue;

                var keywordsToTake = Math.Min(keywordsPerOutcome + (i < extraKeywords ? 1 : 0), kicsToUse.Count);
                var shuffledKics = kicsToUse.OrderBy(_ => random.Next()).Take(keywordsToTake);
                foreach (var kic in shuffledKics)
                {
                    result.Add((kic, outcome));
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets the concrete outcome that owns a specific keyword.
    /// Returns null if keyword is not found or belongs to multiple outcomes.
    /// </summary>
    public ConcreteOutcome? GetOutcomeOwningKeyword(string keyword)
    {
        var normalizedKeyword = keyword.ToLowerInvariant();

        // Check ObservationObjects first: any keyword they expose (own + sub) routes to the obs
        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is ObservationObject obs &&
                obs.OutcomeKeywordsInContext.Any(k => k.Keyword.ToLowerInvariant() == normalizedKeyword))
                return obs;
        }

        // Then check plain NarrationNode children and other ConcreteOutcomes
        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is ObservationObject) continue; // already handled above
            if (outcome is ConcreteOutcome co &&
                co.OutcomeKeywordsInContext.Any(k => k.Keyword.ToLowerInvariant() == normalizedKeyword))
                return co;
        }

        var items = GetAvailableItems();
        foreach (var item in items)
        {
            if (item.OutcomeKeywordsInContext.Any(k => k.Keyword.ToLowerInvariant() == normalizedKeyword))
                return item;
        }

        return null;
    }
}
