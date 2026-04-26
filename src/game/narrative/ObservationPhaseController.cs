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
/// Up to 1 keyword per outcome sentence-pair is extracted by KeywordFallbackService
/// from the generated text and mapped back to that outcome in KeywordOutcomeMap.
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
    private readonly QuestionFillerService _questionFillerService;
    private readonly KeywordFallbackService? _keywordFallback;
    private readonly Random _random = new();

    public ObservationPhaseController(
        LlamaServerManager llamaServer,
        ModusMentisSlotManager slotManager,
        WorldContext? worldContext = null,
        KeywordFallbackService? keywordFallback = null,
        ObservationPromptConstructor? promptConstructor = null)
    {
        _observationExecutor = new ObservationExecutor(llamaServer, slotManager);
        _promptConstructor   = promptConstructor ?? new ObservationPromptConstructor();
        _keywordRenderer     = new KeywordRenderer();
        _worldContext        = worldContext ?? new PlainBiomeContext();
        _questionFillerService = QuestionFillerService.Instance;
        _keywordFallback     = keywordFallback;
    }

    /// <summary>
    /// Executes the overall observation phase.
    /// Generates 1 general sentence then repeats (transition + focus) for up to 3 sampled outcomes.
    /// Each outcome group yields up to 1 clickable keyword linked back to that outcome.
    /// Keywords are always found dynamically from the generated text by KeywordFallbackService.
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
            throw new InvalidOperationException(
                "ObservationPhaseController: No observation modus mentis available for the active party member.");
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

        string previousDescription = currentNode.GenerateNeutralDescription(locationId);

        // 1. General description sentence (no outcome or keyword hints)
        try
        {
            var generalQ = _questionFillerService.GetNext(modusMentis, QuestionReference.ObserveFirst);
            var generalPrompt = _promptConstructor.BuildGeneralDescriptionPrompt(currentNode, locationId, modusMentis.PersonaTone, _worldContext, generalQ.PromptText, modusMentis.PersonaReminder, modusMentis.PersonaReminder2);
            var generalText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, generalPrompt, generalQ, isFirstInBatch: true, ct: ct);
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
                var transQ = _questionFillerService.GetNext(modusMentis, QuestionReference.ObserveTransition);
                var transPrompt = _promptConstructor.BuildTransitionSentencePrompt(outcome, previousDescription, transQ.PromptText, modusMentis.PersonaReminder, modusMentis.PersonaReminder2);
                var transText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, transPrompt, transQ, isTransition: true, ct: ct);

                var focusQ = _questionFillerService.GetNext(modusMentis, QuestionReference.ObserveContinuation);
                var focusPrompt = _promptConstructor.BuildOutcomeDescriptionSentencePrompt(outcome, focusQ.PromptText, modusMentis.PersonaReminder, modusMentis.PersonaReminder2);
                var focusText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, focusPrompt, focusQ, ct: ct);

                // Always use dynamic keyword extraction from the generated text
                var sampledKws = new List<string>();
                if (_keywordFallback != null)
                {
                    var combined = (transText + " " + focusText).Trim();
                    var kw = await _keywordFallback.FindBestKeywordAsync(combined, GetNeutralDescription(outcome, locationId));
                    if (kw != null)
                    {
                        sampledKws.Add(kw);
                        Console.WriteLine($"ObservationPhaseController: Keyword '{kw}' for '{outcome.DisplayName}'");
                    }
                }

                var (transKws, focKws) = _observationExecutor.AssignKeywordsToSentences(sampledKws, transText, focusText);

                sentences.Add(new NarrationSentence(transText, transKws));
                sentences.Add(new NarrationSentence(focusText, focKws));

                foreach (var kw in sampledKws)
                {
                    allKeywords.Add(kw);
                    keywordOutcomeMap.TryAdd(kw, outcome);
                }

                previousDescription = outcome is NarrationNode nn2 ? nn2.GenerateNeutralDescription(locationId) : outcome.DisplayName;

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
            Sentences: sentences
        );

        Console.WriteLine($"ObservationPhaseController: Overall observation complete ({sentences.Count} sentences, {allKeywords.Count} keywords)");
        return new List<NarrationBlock> { block };
    }

    /// <summary>
    /// Generates a focus observation for a specific outcome (right-click on a keyword).
    ///
    /// Structure:
    ///   [0]   Focus description of the clicked outcome
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

        // 1. Focus description of the clicked outcome (first sentence -- full context prompt)
        try
        {
            var firstQ = _questionFillerService.GetNext(observationModusMentis, QuestionReference.ObserveFirst);
            var firstPrompt = _promptConstructor.BuildFirstSentencePrompt(currentNode, locationId, focusOutcome, observationModusMentis.PersonaTone, _worldContext, firstQ.PromptText, observationModusMentis.PersonaReminder, observationModusMentis.PersonaReminder2);
            var firstText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, firstPrompt, firstQ, isFirstInBatch: true, ct: ct);

            var firstKws = new List<string>();
            if (_keywordFallback != null)
            {
                var kw = await _keywordFallback.FindBestKeywordAsync(firstText, GetNeutralDescription(focusOutcome, locationId));
                if (kw != null)
                {
                    firstKws.Add(kw);
                    Console.WriteLine($"ObservationPhaseController: Focus keyword '{kw}' for '{focusOutcome.DisplayName}'");
                }
            }

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

        string previousDescription = focusOutcome is NarrationNode fn2 ? fn2.GenerateNeutralDescription(locationId) : focusOutcome.DisplayName;

        var otherOutcome = currentNode.GetAllDirectConcreteOutcomes()
            .Where(o => o != focusOutcome)
            .OrderBy(_ => _random.Next())
            .FirstOrDefault();

        if (otherOutcome != null)
        {
            try
            {
                var transQ2 = _questionFillerService.GetNext(observationModusMentis, QuestionReference.ObserveTransition);
                var transPrompt = _promptConstructor.BuildTransitionSentencePrompt(otherOutcome, previousDescription, transQ2.PromptText, observationModusMentis.PersonaReminder, observationModusMentis.PersonaReminder2);
                var transText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, transPrompt, transQ2, isTransition: true, ct: ct);

                var focusQ2 = _questionFillerService.GetNext(observationModusMentis, QuestionReference.ObserveContinuation);
                var focusPrompt = _promptConstructor.BuildOutcomeDescriptionSentencePrompt(otherOutcome, focusQ2.PromptText, observationModusMentis.PersonaReminder, observationModusMentis.PersonaReminder2);
                var focusText = await _observationExecutor.GenerateSentenceFromPromptAsync(slotId, focusPrompt, focusQ2, ct: ct);

                var sampledKws = new List<string>();
                if (_keywordFallback != null)
                {
                    var combined = (transText + " " + focusText).Trim();
                    var kw = await _keywordFallback.FindBestKeywordAsync(combined, GetNeutralDescription(otherOutcome, locationId));
                    if (kw != null)
                    {
                        sampledKws.Add(kw);
                        Console.WriteLine($"ObservationPhaseController: Focus second keyword '{kw}' for '{otherOutcome.DisplayName}'");
                    }
                }

                var (transKws, focKws) = _observationExecutor.AssignKeywordsToSentences(sampledKws, transText, focusText);

                sentences.Add(new NarrationSentence(transText, transKws));
                sentences.Add(new NarrationSentence(focusText, focKws));

                foreach (var kw in sampledKws)
                {
                    allKeywords.Add(kw);
                    keywordOutcomeMap.TryAdd(kw, otherOutcome);
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
            Sentences: sentences
        );

        Console.WriteLine($"ObservationPhaseController: Focus observation complete ({sentences.Count} sentences, {allKeywords.Count} keywords)");
        return new List<NarrationBlock> { block };
    }

    /// <summary>
    /// Returns a concise noun-phrase description of an outcome for use in LLM prompts.
    /// </summary>
    private static string GetNeutralDescription(ConcreteOutcome outcome, int locationId)
        => outcome is NarrationNode nn   ? nn.GenerateNeutralDescription(locationId)
         : outcome is ObservationObject obs ? obs.GenerateNeutralDescription(0)
         : outcome.DisplayName;

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
    /// </summary>
    public async Task<NarrationBlock?> GenerateSpeakingTextAsync(
        string keyword,
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
            // --- Request 1: Call companion's attention ---
            var prompt1 = _promptConstructor.BuildSpeakingAttentionPrompt(
                currentNode, locationId, linkedOutcome,
                keyword, companionName,
                speakingModusMentis.PersonaTone, worldContext,
                speakingModusMentis.PersonaReminder, speakingModusMentis.PersonaReminder2);

            var raw1 = await _observationExecutor.GenerateSpeakingTextAsync(slotId, prompt1, ct);
            var sentence1 = raw1.Trim().Trim('"');
            Console.WriteLine($"ObservationPhaseController: Speaking sentence 1: {sentence1}");

            // --- Request 2: Describe observation (continuation — no slot reset) ---
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

            // Extract keywords dynamically from each sentence using fallback service
            var allExtractedKeywords = new List<string>();
            var speakingKeywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);

            if (_keywordFallback != null)
            {
                var fullText = (sentence1 + " " + sentence2 + " " + sentence3).Trim();
                var descr = GetNeutralDescription(linkedOutcome, locationId);
                var kw = await _keywordFallback.FindBestKeywordAsync(fullText, descr);
                if (kw != null)
                {
                    allExtractedKeywords.Add(kw);
                    speakingKeywordOutcomeMap[kw] = linkedOutcome;
                }
            }

            // Per-sentence keyword assignment for scroll buffer highlighting
            var speakingKws = allExtractedKeywords;
            var speakingSentences = new List<NarrationSentence>();
            if (!string.IsNullOrWhiteSpace(sentence1))
                speakingSentences.Add(new NarrationSentence(sentence1, speakingKws));
            if (!string.IsNullOrWhiteSpace(sentence2))
                speakingSentences.Add(new NarrationSentence(sentence2, new List<string>()));
            if (!string.IsNullOrWhiteSpace(sentence3))
                speakingSentences.Add(new NarrationSentence(sentence3, new List<string>()));

            var block = new NarrationBlock(
                Type: NarrationBlockType.Speaking,
                ModusMentis: speakingModusMentis,
                Text: spokenText,
                Keywords: allExtractedKeywords.Count > 0 ? allExtractedKeywords : null,
                Actions: null,
                ChainOrigin: null,
                LinkedOutcome: linkedOutcome,
                KeywordOutcomeMap: speakingKeywordOutcomeMap.Count > 0 ? speakingKeywordOutcomeMap : null,
                Sentences: speakingSentences,
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


