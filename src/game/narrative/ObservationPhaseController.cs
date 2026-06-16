using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Orchestrates the observation phase of narration.
///
/// Each sentence is built as neutral meaning text by <see cref="NeutralNarration"/> and then
/// re-expressed in the active Modus Mentis's voice by <see cref="PersonaRewriter"/> (or shown
/// verbatim in playground mode). The persona-styled focus sentences also return the single noun
/// the persona chose, which becomes the clickable keyword mapped back to its outcome.
///
/// Overall observation structure (7 sentences max):
///   [0]   General description of the node (no keyword)
///   [1-2] Transition + focus for outcome 1   (focus yields a keyword)
///   [3-4] Transition + focus for outcome 2
///   [5-6] Transition + focus for outcome 3
///
/// Focus observation structure (3 sentences max):
///   [0]   Focus description of the clicked outcome
///   [1-2] Transition + focus for one other outcome from the node
/// </summary>
public class ObservationPhaseController
{
    private readonly ObservationExecutor _observationExecutor;
    private readonly PersonaRewriter _rewriter;
    private readonly KeywordRenderer _keywordRenderer;
    private readonly Random _random = new();

    public ObservationPhaseController(
        LlamaServerManager llamaServer,
        ModusMentisSlotManager slotManager,
        WorldContext? worldContext = null)
    {
        _observationExecutor = new ObservationExecutor(llamaServer, slotManager);
        _rewriter            = new PersonaRewriter(llamaServer);
        _keywordRenderer     = new KeywordRenderer();
    }

    /// <summary>
    /// Executes the overall observation phase: 1 general sentence then (transition + focus) for up
    /// to 3 sampled outcomes. Each focus sentence yields one clickable keyword linked to its outcome.
    /// </summary>
    public async Task<List<NarrationBlock>> ExecuteObservationPhaseAsync(
        NarrationNode currentNode,
        PartyMember actingMember,
        int locationId,
        CancellationToken ct = default)
    {
        Console.WriteLine($"ObservationPhaseController: Starting overall observation for {currentNode.NodeId}");

        var modusMentis = actingMember.GetObservationModiMentis()
            .OrderBy(_ => _random.Next())
            .FirstOrDefault();

        if (modusMentis == null)
        {
            throw new InvalidOperationException(
                "ObservationPhaseController: No observation modus mentis available for the active party member.");
        }

        Console.WriteLine($"ObservationPhaseController: Selected {modusMentis.DisplayName}");

        var allOutcomes = currentNode.GetAllDirectConcreteOutcomes();
        var sampledOutcomes = allOutcomes.OrderBy(_ => _random.Next()).Take(3).ToList();

        if (sampledOutcomes.Count == 0)
        {
            Console.WriteLine("ObservationPhaseController: No concrete outcomes found at node.");
            return new List<NarrationBlock>();
        }

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(modusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();

        // 1. General description sentence (no keyword).
        try
        {
            var neutral = NeutralNarration.Observation(isFirst: true, isTransition: false, currentNode.GenerateNeutralDescription(locationId));
            var text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, ct: ct);
            sentences.Add(new NarrationSentence(text, new List<string>()));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: General sentence failed: {ex.Message}");
        }

        // 2-6. For each sampled outcome: transition (no keyword) + focus (keyword).
        foreach (var outcome in sampledOutcomes)
        {
            try
            {
                var desc = GetNeutralDescription(outcome, locationId);

                var transNeutral = NeutralNarration.Observation(isFirst: false, isTransition: true, desc);
                var transText = await _rewriter.RewriteAsync(slotId, transNeutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, ct: ct);
                sentences.Add(new NarrationSentence(transText, new List<string>()));

                var focusNeutral = NeutralNarration.Observation(isFirst: false, isTransition: false, desc);
                var (focusText, kw) = await _rewriter.RewriteObservationAsync(slotId, focusNeutral, modusMentis.PersonaReminder2, keepHistory: true, ct: ct);
                var focusKws = kw != null ? new List<string> { kw } : new List<string>();
                sentences.Add(new NarrationSentence(focusText, focusKws));

                if (kw != null)
                {
                    allKeywords.Add(kw);
                    keywordOutcomeMap.TryAdd(kw, outcome);
                }
                Console.WriteLine($"ObservationPhaseController: Sentences generated for '{outcome.DisplayName}' (keyword '{kw}')");
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
    /// Generates a focus observation for a specific outcome (right-click on a keyword):
    /// a focus sentence on the clicked outcome, then transition + focus for one other outcome.
    /// </summary>
    public async Task<List<NarrationBlock>> GenerateFocusObservationAsync(
        ConcreteOutcome focusOutcome,
        ModusMentis observationModusMentis,
        NarrationNode currentNode,
        int locationId,
        CancellationToken ct = default)
    {
        Console.WriteLine($"ObservationPhaseController: Starting focus observation on '{focusOutcome.DisplayName}'");

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(observationModusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();

        // 1. Focus on the clicked outcome (first sentence, yields a keyword).
        try
        {
            var neutral = NeutralNarration.Observation(isFirst: true, isTransition: false, GetNeutralDescription(focusOutcome, locationId));
            var (text, kw) = await _rewriter.RewriteObservationAsync(slotId, neutral, observationModusMentis.PersonaReminder2, keepHistory: true, ct: ct);
            var kws = kw != null ? new List<string> { kw } : new List<string>();
            sentences.Add(new NarrationSentence(text, kws));
            if (kw != null) { allKeywords.Add(kw); keywordOutcomeMap.TryAdd(kw, focusOutcome); }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: Focus first sentence failed: {ex.Message}");
        }

        // 2-3. Transition + focus for one other outcome at the node.
        var otherOutcome = currentNode.GetAllDirectConcreteOutcomes()
            .Where(o => o != focusOutcome)
            .OrderBy(_ => _random.Next())
            .FirstOrDefault();

        if (otherOutcome != null)
        {
            try
            {
                var desc = GetNeutralDescription(otherOutcome, locationId);

                var transNeutral = NeutralNarration.Observation(isFirst: false, isTransition: true, desc);
                var transText = await _rewriter.RewriteAsync(slotId, transNeutral, NarrationKind.Observation, observationModusMentis.PersonaReminder2, keepHistory: true, ct: ct);
                sentences.Add(new NarrationSentence(transText, new List<string>()));

                var focusNeutral = NeutralNarration.Observation(isFirst: false, isTransition: false, desc);
                var (focusText, kw) = await _rewriter.RewriteObservationAsync(slotId, focusNeutral, observationModusMentis.PersonaReminder2, keepHistory: true, ct: ct);
                var focusKws = kw != null ? new List<string> { kw } : new List<string>();
                sentences.Add(new NarrationSentence(focusText, focusKws));
                if (kw != null) { allKeywords.Add(kw); keywordOutcomeMap.TryAdd(kw, otherOutcome); }
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
    /// Returns a concise noun-phrase description of an outcome for neutral narration.
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
    /// Three neutral lines (call attention → describe → ask) are each rewritten as direct speech.
    /// </summary>
    public async Task<NarrationBlock?> GenerateSpeakingTextAsync(
        string keyword,
        ModusMentis speakingModusMentis,
        string companionName,
        ConcreteOutcome linkedOutcome,
        NarrationNode currentNode,
        PartyMember actingMember,
        int locationId,
        WorldContext worldContext,
        CancellationToken ct = default)
    {
        Console.WriteLine($"ObservationPhaseController: Speaking to '{companionName}' about '{keyword}' with {speakingModusMentis.DisplayName}");

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(speakingModusMentis);
        _observationExecutor.ResetSlot(slotId);

        try
        {
            var r2 = speakingModusMentis.PersonaReminder2;
            var descr = GetNeutralDescription(linkedOutcome, locationId);

            var sentence1 = (await _rewriter.RewriteAsync(slotId, NeutralNarration.Attention(companionName), NarrationKind.Speaking, r2, companionName, keepHistory: true, ct)).Trim().Trim('"');
            var sentence2 = (await _rewriter.RewriteAsync(slotId, NeutralNarration.Description(descr), NarrationKind.Speaking, r2, companionName, keepHistory: true, ct)).Trim().Trim('"');
            var sentence3 = (await _rewriter.RewriteAsync(slotId, NeutralNarration.Question(), NarrationKind.Speaking, r2, companionName, keepHistory: true, ct)).Trim().Trim('"');

            var parts = new[] { sentence1, sentence2, sentence3 }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (parts.Count == 0)
            {
                Console.WriteLine("ObservationPhaseController: All speaking sentences empty.");
                return null;
            }

            var spokenText = $"\"{string.Join(" ", parts)}\"";

            // Keyword: prefer the linked outcome's noun if the persona kept it; else the description line's last word.
            var allExtractedKeywords = new List<string>();
            var speakingKeywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
            var fullText = (sentence1 + " " + sentence2 + " " + sentence3).Trim();
            var candidate = NeutralNarration.KeywordFromPhrase(descr);
            string? kw = (candidate != null && Regex.IsMatch(fullText, $@"\b{Regex.Escape(candidate)}\b", RegexOptions.IgnoreCase))
                ? candidate
                : NeutralNarration.KeywordFromPhrase(sentence2);
            if (kw != null)
            {
                allExtractedKeywords.Add(kw);
                speakingKeywordOutcomeMap[kw] = linkedOutcome;
            }

            // Attach the keyword list to every sentence; the renderer highlights only where it appears.
            var speakingSentences = new List<NarrationSentence>();
            if (!string.IsNullOrWhiteSpace(sentence1)) speakingSentences.Add(new NarrationSentence(sentence1, allExtractedKeywords));
            if (!string.IsNullOrWhiteSpace(sentence2)) speakingSentences.Add(new NarrationSentence(sentence2, allExtractedKeywords));
            if (!string.IsNullOrWhiteSpace(sentence3)) speakingSentences.Add(new NarrationSentence(sentence3, allExtractedKeywords));

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
                SpeakerName: actingMember.DisplayName
            );

            Console.WriteLine($"ObservationPhaseController: Speaking generation complete (keyword '{kw}')");
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
