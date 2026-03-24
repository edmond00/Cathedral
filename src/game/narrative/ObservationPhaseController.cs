using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Orchestrates the observation phase of narration.
/// Generates one sentence per outcome using chained LLM calls on a single slot.
/// Each NarrationBlock carries a LinkedOutcome so click handlers have direct outcome access.
/// </summary>
public class ObservationPhaseController
{
    private readonly ObservationExecutor _observationExecutor;
    private readonly KeywordRenderer _keywordRenderer;
    private readonly Random _random = new();

    public ObservationPhaseController(LlamaServerManager llamaServer, ModusMentisSlotManager slotManager)
    {
        _observationExecutor = new ObservationExecutor(llamaServer, slotManager);
        _keywordRenderer = new KeywordRenderer();
    }

    /// <summary>
    /// Executes the overall observation phase: selects one observation modusMentis and generates
    /// one sentence per sampled outcome (3–5 sentences total), each in a separate LLM call
    /// on the same slot so context chains naturally. Each NarrationBlock has LinkedOutcome set.
    /// </summary>
    public async Task<List<NarrationBlock>> ExecuteObservationPhaseAsync(
        NarrationNode currentNode,
        Protagonist protagonist,
        CancellationToken ct = default)
    {
        Console.WriteLine($"ObservationPhaseController: Starting overall observation for {currentNode.NodeId}");

        var modusMentis = protagonist.GetObservationModiMentis()
            .OrderBy(_ => _random.Next())
            .FirstOrDefault();

        if (modusMentis == null)
        {
            Console.WriteLine("ObservationPhaseController: No observation modiMentis available!");
            return new List<NarrationBlock>();
        }

        Console.WriteLine($"ObservationPhaseController: Selected {modusMentis.DisplayName}");

        // Sample 3..5 outcomes to describe
        var allOutcomes = currentNode.GetAllDirectConcreteOutcomes();
        int targetCount = Math.Min(_random.Next(3, 6), allOutcomes.Count);
        var sampledOutcomes = allOutcomes.OrderBy(_ => _random.Next()).Take(targetCount).ToList();

        if (sampledOutcomes.Count == 0)
        {
            Console.WriteLine("ObservationPhaseController: No concrete outcomes found at node.");
            return new List<NarrationBlock>();
        }

        Console.WriteLine($"ObservationPhaseController: Generating {sampledOutcomes.Count} observation sentences");

        // Acquire slot once; reset its history so each observation batch starts fresh
        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(modusMentis);
        _observationExecutor.ResetSlot(slotId);

        var sentenceParts = new List<string>();
        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        int locationId = protagonist.CurrentLocationId;

        for (int i = 0; i < sampledOutcomes.Count; i++)
        {
            var outcome = sampledOutcomes[i];
            bool isFirst = i == 0;

            try
            {
                var sentence = await _observationExecutor.GenerateSentenceAsync(
                    slotId, currentNode, locationId, outcome, isFirst, modusMentis.PersonaTone, ct);

                var outcomeKeywords = outcome is NarrationNode nn ? nn.NodeKeywords : outcome.OutcomeKeywords;
                var keyword = _observationExecutor.ExtractKeywordFromSentence(sentence, outcomeKeywords);

                sentenceParts.Add(sentence);
                allKeywords.Add(keyword);
                keywordOutcomeMap.TryAdd(keyword, outcome);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ObservationPhaseController: Sentence {i} failed: {ex.Message}");
            }
        }

        if (sentenceParts.Count == 0)
        {
            Console.WriteLine("ObservationPhaseController: All sentences failed.");
            return new List<NarrationBlock>();
        }

        var block = new NarrationBlock(
            Type: NarrationBlockType.Observation,
            ModusMentis: modusMentis,
            Text: string.Join(" ", sentenceParts),
            Keywords: allKeywords,
            Actions: null,
            SourceObservationType: ObservationType.Overall,
            KeywordOutcomeMap: keywordOutcomeMap
        );

        Console.WriteLine($"ObservationPhaseController: Overall observation complete ({sentenceParts.Count} sentences, {allKeywords.Count} keywords)");
        return new List<NarrationBlock> { block };
    }

    /// <summary>
    /// Generates a focus observation for a specific outcome (right-click on a keyword).
    /// Produces up to 5 sentences: [0] the focus outcome, [1-2] circuitous outcomes inside it (if any),
    /// [3-4] other outcomes at the current node.
    /// Circuitous-sentence blocks have IsCircuitousSentence=true and FocusOriginNode set so that
    /// right-clicking them again refocuses on the origin rather than drilling deeper.
    /// </summary>
    public async Task<List<NarrationBlock>> GenerateFocusObservationAsync(
        ConcreteOutcome focusOutcome,
        ModusMentis observationModusMentis,
        NarrationNode currentNode,
        Protagonist protagonist,
        CancellationToken ct = default)
    {
        Console.WriteLine($"ObservationPhaseController: Starting focus observation on '{focusOutcome.DisplayName}'");

        // Build ordered outcome list: focus → circuitous → other
        var orderedOutcomes = new List<(ConcreteOutcome Outcome, bool IsCircuitous, NarrationNode? CircuitousOrigin)>();
        orderedOutcomes.Add((focusOutcome, false, null));

        NarrationNode? focusNode = focusOutcome as NarrationNode;
        if (focusNode != null)
        {
            var circuitous = focusNode.GetCircuitousOutcomes().Take(2);
            foreach (var (co, origin) in circuitous)
                orderedOutcomes.Add((co, true, origin));
        }

        // Fill remaining slots (up to 5 total) with other outcomes from the current node
        var otherOutcomes = currentNode.GetAllDirectConcreteOutcomes()
            .Where(o => o != focusOutcome)
            .OrderBy(_ => _random.Next());

        foreach (var other in otherOutcomes)
        {
            if (orderedOutcomes.Count >= 5) break;
            orderedOutcomes.Add((other, false, null));
        }

        // Acquire slot and reset history for this focus batch
        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(observationModusMentis);
        _observationExecutor.ResetSlot(slotId);

        var sentenceParts = new List<string>();
        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        int locationId = protagonist.CurrentLocationId;

        for (int i = 0; i < orderedOutcomes.Count; i++)
        {
            var (outcome, isCircuitous, circuitousOrigin) = orderedOutcomes[i];
            bool isFirst = i == 0;

            try
            {
                var sentence = await _observationExecutor.GenerateSentenceAsync(
                    slotId, currentNode, locationId, outcome, isFirst, observationModusMentis.PersonaTone, ct);

                var outcomeKeywords = outcome is NarrationNode nn ? nn.NodeKeywords : outcome.OutcomeKeywords;
                var keyword = _observationExecutor.ExtractKeywordFromSentence(sentence, outcomeKeywords);

                sentenceParts.Add(sentence);
                allKeywords.Add(keyword);
                // For circuitous sentences, map keyword → the circuitous origin (re-focus goes there)
                keywordOutcomeMap.TryAdd(keyword, isCircuitous && circuitousOrigin != null ? circuitousOrigin : outcome);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ObservationPhaseController: Focus sentence {i} failed: {ex.Message}");
            }
        }

        if (sentenceParts.Count == 0)
        {
            Console.WriteLine("ObservationPhaseController: All focus sentences failed.");
            return new List<NarrationBlock>();
        }

        var block = new NarrationBlock(
            Type: NarrationBlockType.Observation,
            ModusMentis: observationModusMentis,
            Text: string.Join(" ", sentenceParts),
            Keywords: allKeywords,
            Actions: null,
            SourceObservationType: ObservationType.Focus,
            KeywordOutcomeMap: keywordOutcomeMap
        );

        Console.WriteLine($"ObservationPhaseController: Focus observation complete ({sentenceParts.Count} sentences, {allKeywords.Count} keywords)");
        return new List<NarrationBlock> { block };
    }

    /// <summary>
    /// Formats narration blocks for terminal display with keyword highlighting.
    /// </summary>
    public string FormatNarrationBlockForDisplay(NarrationBlock block, bool keywordsEnabled = true)
    {
        var formattedText = _keywordRenderer.FormatForTerminal(
            block.Text,
            block.Keywords ?? new List<string>(),
            keywordsEnabled
        );

        return $"[{block.ModusMentis.DisplayName}]\n{formattedText}\n";
    }

    /// <summary>
    /// Gets all unique keywords from a list of narration blocks.
    /// </summary>
    public List<string> GetAllKeywords(List<NarrationBlock> blocks)
    {
        return blocks
            .Where(b => b.Keywords != null)
            .SelectMany(b => b.Keywords!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
