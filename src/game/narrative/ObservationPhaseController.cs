using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Orchestrates the observation phase of narration.
/// 
/// Overall observation structure (7 sentences max):
///   [0]   General description of the node (no outcome, no keywords)
///   [1-2] Transition + focus for outcome 1
///   [3-4] Transition + focus for outcome 2
///   [5-6] Transition + focus for outcome 3
/// Up to 3 keywords per outcome group are extracted from its two sentences and
/// mapped back to that outcome in KeywordOutcomeMap.
///
/// Focus observation structure (3 sentences max):
///   [0]   Focus description of the clicked outcome
///   [1-2] Transition + focus for one other outcome from the node
/// </summary>
public class ObservationPhaseController
{
    private readonly ObservationExecutor _observationExecutor;
    private readonly ObservationPromptConstructor _promptConstructor;
    private readonly KeywordRenderer _keywordRenderer;
    private readonly WorldContext _worldContext;
    private readonly Random _random = new();

    public ObservationPhaseController(LlamaServerManager llamaServer, ModusMentisSlotManager slotManager, WorldContext? worldContext = null)
    {
        _observationExecutor = new ObservationExecutor(llamaServer, slotManager);
        _promptConstructor = new ObservationPromptConstructor();
        _keywordRenderer = new KeywordRenderer();
        _worldContext = worldContext ?? new ForestBiomeContext();
    }

    /// <summary>
    /// Executes the overall observation phase.
    /// Generates 1 general sentence then repeats (transition + focus) for up to 3 sampled outcomes.
    /// Each outcome group yields up to 3 clickable keywords linked back to that outcome.
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

        // Sample up to 3 direct outcomes
        var allOutcomes = currentNode.GetAllDirectConcreteOutcomes();
        var sampledOutcomes = allOutcomes.OrderBy(_ => _random.Next()).Take(3).ToList();

        if (sampledOutcomes.Count == 0)
        {
            Console.WriteLine("ObservationPhaseController: No concrete outcomes found at node.");
            return new List<NarrationBlock>();
        }

        // Acquire slot once, reset history
        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(modusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();
        int locationId = protagonist.CurrentLocationId;

        // Start with the node's own neutral description as the initial previous context
        string previousDescription = currentNode.GenerateNeutralDescription(locationId);

        // 1. General description sentence (no outcome or keyword hints)
        try
        {
            var generalPrompt = _promptConstructor.BuildGeneralDescriptionPrompt(currentNode, locationId, modusMentis.PersonaTone, _worldContext, modusMentis.PersonaReminder);
            var generalText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, generalPrompt, isFirstInBatch: true, ct: ct);
            sentences.Add(new NarrationSentence(generalText, new List<string>()));
            Console.WriteLine($"ObservationPhaseController: General sentence generated");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: General sentence failed: {ex.Message}");
        }

        // 2-6. For each sampled outcome: transition sentence + focus sentence + keyword extraction
        foreach (var outcome in sampledOutcomes)
        {
            try
            {
                var outcomeKeywords = outcome is NarrationNode nn ? nn.NodeKeywords : outcome.OutcomeKeywords;

                // Transition sentence — tell the LLM what was previously described
                var transPrompt = _promptConstructor.BuildTransitionSentencePrompt(outcome, previousDescription, modusMentis.PersonaReminder);
                var transText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, transPrompt, isTransition: true, ct: ct);

                // Focus sentence with outcome keywords
                var focusPrompt = _promptConstructor.BuildOutcomeDescriptionSentencePrompt(outcome, modusMentis.PersonaReminder);
                var focusText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, focusPrompt, ct: ct);

                // Extract up to 3 keywords from the two sentences combined
                var sampledKws = _observationExecutor.ExtractKeywordsFromSentences(transText, focusText, outcomeKeywords, _random, 3);
                var (transKws, focKws) = _observationExecutor.AssignKeywordsToSentences(sampledKws, transText, focusText);

                sentences.Add(new NarrationSentence(transText, transKws));
                sentences.Add(new NarrationSentence(focusText, focKws));

                foreach (var kw in sampledKws)
                {
                    allKeywords.Add(kw);
                    keywordOutcomeMap.TryAdd(kw, outcome);
                }

                // Update previous description for the next transition
                previousDescription = outcome is NarrationNode nn2 ? nn2.GenerateNeutralDescription(locationId) : outcome.DisplayName;

                Console.WriteLine($"ObservationPhaseController: Outcome '{outcome.DisplayName}' �� {sampledKws.Count} keywords");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ObservationPhaseController: Outcome '{outcome.DisplayName}' sentences failed: {ex.Message}");
            }
        }

        if (sentences.Count == 0)
        {
            Console.WriteLine("ObservationPhaseController: All sentences failed.");
            return new List<NarrationBlock>();
        }

        var block = new NarrationBlock(
            Type: NarrationBlockType.Observation,
            ModusMentis: modusMentis,
            Text: string.Join(" ", sentences.Select(s => s.Text)),
            Keywords: allKeywords,
            Actions: null,
            SourceObservationType: ObservationType.Overall,
            KeywordOutcomeMap: keywordOutcomeMap,
            Sentences: sentences
        );

        Console.WriteLine($"ObservationPhaseController: Overall observation complete ({sentences.Count} sentences, {allKeywords.Count} keywords)");
        return new List<NarrationBlock> { block };
    }

    /// <summary>
    /// Generates a focus observation for a specific outcome (right-click on a keyword).
    ///
    /// Structure:
    ///   [0]   Focus description of the clicked outcome with its keywords
    ///   [1-2] Transition + focus for one other outcome at the current node
    /// </summary>
    public async Task<List<NarrationBlock>> GenerateFocusObservationAsync(
        ConcreteOutcome focusOutcome,
        ModusMentis observationModusMentis,
        NarrationNode currentNode,
        Protagonist protagonist,
        CancellationToken ct = default)
    {
        Console.WriteLine($"ObservationPhaseController: Starting focus observation on '{focusOutcome.DisplayName}'");

        // Acquire slot and reset history
        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(observationModusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();
        int locationId = protagonist.CurrentLocationId;

        // 1. Focus description of the clicked outcome (first sentence ? full context prompt)
        try
        {
            var focusOutcomeKeywords = focusOutcome is NarrationNode fn ? fn.NodeKeywords : focusOutcome.OutcomeKeywords;
            var firstPrompt = _promptConstructor.BuildFirstSentencePrompt(currentNode, locationId, focusOutcome, observationModusMentis.PersonaTone, _worldContext, observationModusMentis.PersonaReminder);
            var firstText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, firstPrompt, isFirstInBatch: true, ct: ct);

            var firstKws = _observationExecutor.ExtractKeywordsFromSentences("", firstText, focusOutcomeKeywords, _random, 3);
            sentences.Add(new NarrationSentence(firstText, firstKws));
            foreach (var kw in firstKws)
            {
                allKeywords.Add(kw);
                keywordOutcomeMap.TryAdd(kw, focusOutcome);
            }
            Console.WriteLine($"ObservationPhaseController: Focus first sentence generated, {firstKws.Count} keywords");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: Focus first sentence failed: {ex.Message}");
        }

        // Previous description for the transition is the focus outcome we just described
        string previousDescription = focusOutcome is NarrationNode fn2 ? fn2.GenerateNeutralDescription(locationId) : focusOutcome.DisplayName;

        // 2-3. One new outcome group: transition + focus for an other outcome at this node
        var otherOutcome = currentNode.GetAllDirectConcreteOutcomes()
            .Where(o => o != focusOutcome)
            .OrderBy(_ => _random.Next())
            .FirstOrDefault();

        if (otherOutcome != null)
        {
            try
            {
                var otherOutcomeKeywords = otherOutcome is NarrationNode on2 ? on2.NodeKeywords : otherOutcome.OutcomeKeywords;

                var transPrompt = _promptConstructor.BuildTransitionSentencePrompt(otherOutcome, previousDescription, observationModusMentis.PersonaReminder);
                var transText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, transPrompt, isTransition: true, ct: ct);

                var focusPrompt = _promptConstructor.BuildOutcomeDescriptionSentencePrompt(otherOutcome, observationModusMentis.PersonaReminder);
                var focusText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, focusPrompt, ct: ct);

                var sampledKws = _observationExecutor.ExtractKeywordsFromSentences(transText, focusText, otherOutcomeKeywords, _random, 3);
                var (transKws, focKws) = _observationExecutor.AssignKeywordsToSentences(sampledKws, transText, focusText);

                sentences.Add(new NarrationSentence(transText, transKws));
                sentences.Add(new NarrationSentence(focusText, focKws));

                foreach (var kw in sampledKws)
                {
                    allKeywords.Add(kw);
                    keywordOutcomeMap.TryAdd(kw, otherOutcome);
                }

                Console.WriteLine($"ObservationPhaseController: Focus second outcome '{otherOutcome.DisplayName}' �� {sampledKws.Count} keywords");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ObservationPhaseController: Focus second outcome sentences failed: {ex.Message}");
            }
        }

        if (sentences.Count == 0)
        {
            Console.WriteLine("ObservationPhaseController: All focus sentences failed.");
            return new List<NarrationBlock>();
        }

        var block = new NarrationBlock(
            Type: NarrationBlockType.Observation,
            ModusMentis: observationModusMentis,
            Text: string.Join(" ", sentences.Select(s => s.Text)),
            Keywords: allKeywords,
            Actions: null,
            SourceObservationType: ObservationType.Focus,
            KeywordOutcomeMap: keywordOutcomeMap,
            Sentences: sentences
        );

        Console.WriteLine($"ObservationPhaseController: Focus observation complete ({sentences.Count} sentences, {allKeywords.Count} keywords)");
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


