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
        var keywordContextMap = new Dictionary<string, KeywordInContext>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();
        int locationId = protagonist.CurrentLocationId;

        string previousDescription = currentNode.GenerateNeutralDescription(locationId);
        KeywordInContext? previousKeywordInContext = null;

        // 1. General description sentence (no outcome or keyword hints)
        try
        {
            var generalPrompt = _promptConstructor.BuildGeneralDescriptionPrompt(currentNode, locationId, modusMentis.PersonaTone, _worldContext, modusMentis.PersonaReminder, modusMentis.PersonaReminder2);
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
                var outcomeKics = outcome is NarrationNode nn ? nn.NodeKeywordsInContext
                    : outcome is ObservationObject obs2 ? obs2.ObservationKeywordsInContext
                    : outcome.OutcomeKeywordsInContext;
                var outcomeKeywords = outcomeKics.Select(k => k.Keyword).ToList();

                var directKeywords = outcome is ObservationObject obsD ? obsD.DirectObservationKeywords : new List<string>();

                var transPrompt = _promptConstructor.BuildTransitionSentencePrompt(outcome, previousDescription, modusMentis.PersonaReminder, previousKeywordInContext, modusMentis.PersonaReminder2);
                var transText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, transPrompt, isTransition: true, ct: ct);

                var focusPrompt = _promptConstructor.BuildOutcomeDescriptionSentencePrompt(outcome, modusMentis.PersonaReminder, modusMentis.PersonaReminder2);
                var focusText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, focusPrompt, ct: ct);

                var sampledKws = _observationExecutor.ExtractKeywordsFromSentences(transText, focusText, outcomeKeywords, directKeywords, _random, 3);
                var (transKws, focKws) = _observationExecutor.AssignKeywordsToSentences(sampledKws, transText, focusText);

                sentences.Add(new NarrationSentence(transText, transKws));
                sentences.Add(new NarrationSentence(focusText, focKws));

                foreach (var kw in sampledKws)
                {
                    allKeywords.Add(kw);
                    keywordOutcomeMap.TryAdd(kw, outcome);
                    var matchedKic = outcomeKics.FirstOrDefault(k => k.Keyword.Equals(kw, StringComparison.OrdinalIgnoreCase));
                    if (matchedKic != null) keywordContextMap.TryAdd(kw, matchedKic);
                }

                previousDescription = outcome is NarrationNode nn2 ? nn2.GenerateNeutralDescription(locationId) : outcome.DisplayName;
                previousKeywordInContext = sampledKws.Count > 0
                    ? outcomeKics.FirstOrDefault(k => k.Keyword.Equals(sampledKws[0], StringComparison.OrdinalIgnoreCase))
                    : outcomeKics.FirstOrDefault();

                Console.WriteLine($"ObservationPhaseController: Outcome '{outcome.DisplayName}' → {sampledKws.Count} keywords");
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
            Sentences: sentences,
            KeywordContextMap: keywordContextMap
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
        var keywordContextMap = new Dictionary<string, KeywordInContext>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();
        int locationId = protagonist.CurrentLocationId;

        // 1. Focus description of the clicked outcome (first sentence -- full context prompt)
        KeywordInContext? previousKeywordInContext = null;
        try
        {
            var focusOutcomeKics = focusOutcome is NarrationNode fn ? fn.NodeKeywordsInContext
                    : focusOutcome is ObservationObject fobs ? fobs.ObservationKeywordsInContext
                    : focusOutcome.OutcomeKeywordsInContext;
            var focusOutcomeKeywords = focusOutcomeKics.Select(k => k.Keyword).ToList();
            var focusDirectKeywords = focusOutcome is ObservationObject fobsD ? fobsD.DirectObservationKeywords : new List<string>();

            var firstPrompt = _promptConstructor.BuildFirstSentencePrompt(currentNode, locationId, focusOutcome, observationModusMentis.PersonaTone, _worldContext, observationModusMentis.PersonaReminder, observationModusMentis.PersonaReminder2);
            var firstText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, firstPrompt, isFirstInBatch: true, ct: ct);

            var firstKws = _observationExecutor.ExtractKeywordsFromSentences("", firstText, focusOutcomeKeywords, focusDirectKeywords, _random, 3);
            sentences.Add(new NarrationSentence(firstText, firstKws));
            foreach (var kw in firstKws)
            {
                allKeywords.Add(kw);
                keywordOutcomeMap.TryAdd(kw, focusOutcome);
                var matchedKic = focusOutcomeKics.FirstOrDefault(k => k.Keyword.Equals(kw, StringComparison.OrdinalIgnoreCase));
                if (matchedKic != null) keywordContextMap.TryAdd(kw, matchedKic);
            }
            previousKeywordInContext = firstKws.Count > 0
                ? focusOutcomeKics.FirstOrDefault(k => k.Keyword.Equals(firstKws[0], StringComparison.OrdinalIgnoreCase))
                : focusOutcomeKics.FirstOrDefault();
            Console.WriteLine($"ObservationPhaseController: Focus first sentence generated, {firstKws.Count} keywords");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: Focus first sentence failed: {ex.Message}");
        }

        string previousDescription = focusOutcome is NarrationNode fn2 ? fn2.GenerateNeutralDescription(locationId) : focusOutcome.DisplayName;

        var otherOutcome = currentNode.GetAllDirectConcreteOutcomes()
            .Where(o => o != focusOutcome)
            .OrderBy(_ => _random.Next())
            .FirstOrDefault();

        if (otherOutcome != null)
        {
            try
            {
                var otherKics = otherOutcome is NarrationNode on2 ? on2.NodeKeywordsInContext
                    : otherOutcome is ObservationObject oobs ? oobs.ObservationKeywordsInContext
                    : otherOutcome.OutcomeKeywordsInContext;
                var otherKeywords = otherKics.Select(k => k.Keyword).ToList();
                var otherDirectKeywords = otherOutcome is ObservationObject oobsD ? oobsD.DirectObservationKeywords : new List<string>();

                var transPrompt = _promptConstructor.BuildTransitionSentencePrompt(otherOutcome, previousDescription, observationModusMentis.PersonaReminder, previousKeywordInContext, observationModusMentis.PersonaReminder2);
                var transText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, transPrompt, isTransition: true, ct: ct);

                var focusPrompt = _promptConstructor.BuildOutcomeDescriptionSentencePrompt(otherOutcome, observationModusMentis.PersonaReminder, observationModusMentis.PersonaReminder2);
                var focusText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, focusPrompt, ct: ct);

                var sampledKws = _observationExecutor.ExtractKeywordsFromSentences(transText, focusText, otherKeywords, otherDirectKeywords, _random, 3);
                var (transKws, focKws) = _observationExecutor.AssignKeywordsToSentences(sampledKws, transText, focusText);

                sentences.Add(new NarrationSentence(transText, transKws));
                sentences.Add(new NarrationSentence(focusText, focKws));

                foreach (var kw in sampledKws)
                {
                    allKeywords.Add(kw);
                    keywordOutcomeMap.TryAdd(kw, otherOutcome);
                    var matchedKic = otherKics.FirstOrDefault(k => k.Keyword.Equals(kw, StringComparison.OrdinalIgnoreCase));
                    if (matchedKic != null) keywordContextMap.TryAdd(kw, matchedKic);
                }

                Console.WriteLine($"ObservationPhaseController: Focus second outcome '{otherOutcome.DisplayName}' -> {sampledKws.Count} keywords");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ObservationPhaseController: Focus second outcome '{otherOutcome.DisplayName}' sentences failed: {ex.Message}");
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
            Sentences: sentences,
            KeywordContextMap: keywordContextMap
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

    /// <summary>
    /// Generates a Speaking block: the active party member addresses a companion about a keyword.
    /// The spoken text is wrapped in quotes by the GBNF schema, contains highlighted keywords,
    /// and acts as the chain root for the companion's subsequent thinking/action blocks.
    /// </summary>
    public async Task<NarrationBlock?> GenerateSpeakingTextAsync(
        string keyword,
        KeywordInContext? keywordInContext,
        ModusMentis speakingModusMentis,
        string companionName,
        ConcreteOutcome linkedOutcome,
        NarrationNode currentNode,
        Protagonist protagonist,
        WorldContext worldContext,
        CancellationToken ct = default)
    {
        Console.WriteLine($"ObservationPhaseController: Speaking to '{companionName}' about '{keyword}' with {speakingModusMentis.DisplayName}");

        // Acquire slot once and reset — all 3 requests run on this slot without further resets.
        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(speakingModusMentis);
        _observationExecutor.ResetSlot(slotId);

        int locationId = protagonist.CurrentLocationId;

        try
        {
            // --- Request 1: Call companion's attention (no keyword hints) ---
            var prompt1 = _promptConstructor.BuildSpeakingAttentionPrompt(
                currentNode, locationId, linkedOutcome,
                keyword, keywordInContext, companionName,
                speakingModusMentis.PersonaTone, worldContext,
                speakingModusMentis.PersonaReminder, speakingModusMentis.PersonaReminder2);

            var raw1 = await _observationExecutor.GenerateSpeakingTextAsync(slotId, prompt1, ct);
            var sentence1 = raw1.Trim().Trim('"');
            Console.WriteLine($"ObservationPhaseController: Speaking sentence 1: {sentence1}");

            // --- Request 2: Describe observation with keyword hints (continuation — no slot reset) ---
            var prompt2 = _promptConstructor.BuildSpeakingDescriptionPrompt(
                linkedOutcome, companionName,
                speakingModusMentis.PersonaReminder, speakingModusMentis.PersonaReminder2);

            var raw2 = await _observationExecutor.GenerateSpeakingTextAsync(slotId, prompt2, ct);
            var sentence2 = raw2.Trim().Trim('"');
            Console.WriteLine($"ObservationPhaseController: Speaking sentence 2: {sentence2}");

            // --- Request 3: Open question to companion (continuation — no slot reset) ---
            var prompt3 = _promptConstructor.BuildSpeakingQuestionPrompt(
                companionName,
                speakingModusMentis.PersonaReminder, speakingModusMentis.PersonaReminder2);

            var raw3 = await _observationExecutor.GenerateSpeakingTextAsync(slotId, prompt3, ct);
            var sentence3 = raw3.Trim().Trim('"');
            Console.WriteLine($"ObservationPhaseController: Speaking sentence 3: {sentence3}");

            // Combine non-empty sentences
            var parts = new[] { sentence1, sentence2, sentence3 }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (parts.Count == 0)
            {
                Console.WriteLine("ObservationPhaseController: All speaking sentences empty.");
                return null;
            }

            var rawCombined = string.Join(" ", parts);
            var spokenText = $"\"{rawCombined}\"";
            Console.WriteLine($"ObservationPhaseController: Speaking full text: {spokenText}");

            // Extract keywords per sentence — sentence 2 is specifically prompted with keyword hints,
            // but any sentence may use them if the LLM happens to.
            var outcomeKics = linkedOutcome is NarrationNode nn ? nn.NodeKeywordsInContext
                : linkedOutcome is ObservationObject obs ? obs.ObservationKeywordsInContext
                : linkedOutcome.OutcomeKeywordsInContext;
            var outcomeKeywords = outcomeKics.Select(k => k.Keyword).ToList();

            var kws1 = string.IsNullOrWhiteSpace(sentence1)
                ? new List<string>()
                : _observationExecutor.ExtractKeywordsFromSentences("", sentence1, outcomeKeywords, _random, 3);
            var kws2 = string.IsNullOrWhiteSpace(sentence2)
                ? new List<string>()
                : _observationExecutor.ExtractKeywordsFromSentences("", sentence2, outcomeKeywords, _random, 3);
            var kws3 = string.IsNullOrWhiteSpace(sentence3)
                ? new List<string>()
                : _observationExecutor.ExtractKeywordsFromSentences("", sentence3, outcomeKeywords, _random, 3);

            var allExtractedKeywords = kws1.Concat(kws2).Concat(kws3)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var keywordContextMap = new Dictionary<string, KeywordInContext>(StringComparer.OrdinalIgnoreCase);
            foreach (var kw in allExtractedKeywords)
            {
                var match = outcomeKics.FirstOrDefault(k => k.Keyword.Equals(kw, StringComparison.OrdinalIgnoreCase));
                if (match != null) keywordContextMap.TryAdd(kw, match);
            }

            // Per-sentence keyword assignment feeds the scroll buffer's per-line highlighting.
            var sentences = new List<NarrationSentence>();
            if (!string.IsNullOrWhiteSpace(sentence1))
                sentences.Add(new NarrationSentence(sentence1, kws1));
            if (!string.IsNullOrWhiteSpace(sentence2))
                sentences.Add(new NarrationSentence(sentence2, kws2));
            if (!string.IsNullOrWhiteSpace(sentence3))
                sentences.Add(new NarrationSentence(sentence3, kws3));

            var block = new NarrationBlock(
                Type: NarrationBlockType.Speaking,
                ModusMentis: speakingModusMentis,
                Text: spokenText,
                Keywords: allExtractedKeywords.Count > 0 ? allExtractedKeywords : null,
                Actions: null,
                ChainOrigin: null,            // speaking block is a chain root
                LinkedOutcome: linkedOutcome, // preserved so keyword clicks resolve correctly
                KeywordOutcomeMap: null,
                Sentences: sentences,
                KeywordContextMap: keywordContextMap.Count > 0 ? keywordContextMap : null,
                SpeakerName: protagonist.DisplayName
            );

            Console.WriteLine($"ObservationPhaseController: Speaking generation complete ({allExtractedKeywords.Count} keywords)");
            return block;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: Speaking generation failed: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return null;
        }
    }
}


