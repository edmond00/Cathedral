using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cathedral;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Orchestrates the observation phase of narration.
///
/// Each sentence is built as neutral meaning text by <see cref="NeutralNarration"/> and then
/// re-expressed in the active Modus Mentis's voice by <see cref="PersonaRewriter"/> (or shown
/// verbatim in playground mode). Observation sentences also return the single noun the persona
/// chose, which becomes the clickable keyword mapped back to its observation object.
///
/// Which observation objects are described is an LLM decision: the Modus Mentis picks the object
/// matching its persona interest (by NeutralName). There is no overall-area opener.
///
/// Overall / focus observation structure:
///   [0]   Observation of the first object (no transition)
///   [1]   Transition to a second object (only if a second object exists)
///   [2]   Observation of the second object
/// For the focus phase (clicking "observe" on a keyword) the first object is the clicked one;
/// the second is LLM-chosen from the remaining objects.
/// </summary>
public class ObservationPhaseController
{
    private readonly ObservationExecutor _observationExecutor;
    private readonly PersonaRewriter _rewriter;
    private readonly KeywordRenderer _keywordRenderer;
    private readonly WorldContext? _worldContext;
    private readonly Random _random = new();

    public ObservationPhaseController(
        LlamaServerManager llamaServer,
        ModusMentisSlotManager slotManager,
        WorldContext? worldContext = null)
    {
        _observationExecutor = new ObservationExecutor(llamaServer, slotManager);
        _rewriter            = new PersonaRewriter(llamaServer);
        _keywordRenderer     = new KeywordRenderer();
        _worldContext        = worldContext;
    }

    /// <summary>
    /// Executes the overall observation phase: the Modus Mentis chooses a first object to observe,
    /// then (if another object exists) chooses a second and bridges to it with one transition.
    /// Each observation sentence yields one clickable keyword linked to its object.
    /// </summary>
    public async Task<List<NarrationBlock>> ExecuteObservationPhaseAsync(
        NarrationNode currentNode,
        PartyMember actingMember,
        int locationId,
        bool isReminescence = false,
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
        if (allOutcomes.Count == 0)
        {
            Console.WriteLine("ObservationPhaseController: No concrete outcomes found at node.");
            return new List<NarrationBlock>();
        }

        // Collapse identical objects (e.g. several "Birch Tree") to one random representative each,
        // so the choice list has no duplicates and a chosen object's twins are not re-proposed.
        var candidates = DeduplicateByName(allOutcomes);

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(modusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();

        // First object: chosen by the Modus Mentis, observed without a transition.
        var first = await ChooseObservationObjectAsync(slotId, candidates, modusMentis, ct, isReminescence,
            _worldContext?.GenerateContextDescription(locationId), currentNode.GenerateNeutralDescription(locationId));
        await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, modusMentis, first, withTransition: false, locationId, ct, isPhaseOpener: true, isReminescence: isReminescence);

        // Second object (if any): chosen from the remaining objects, reached via one transition.
        var remaining = candidates.Where(o => o != first).ToList();
        if (remaining.Count > 0)
        {
            var second = await ChooseObservationObjectAsync(slotId, remaining, modusMentis, ct, isReminescence,
                _worldContext?.GenerateContextDescription(locationId), currentNode.GenerateNeutralDescription(locationId));
            await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, modusMentis, second, withTransition: true, locationId, ct, isReminescence: isReminescence);
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
    /// Generates a focus observation for a specific outcome (clicking "observe" on a keyword):
    /// observe the clicked object, then the Modus Mentis chooses one other object and bridges to it
    /// with a transition before observing it.
    /// </summary>
    public async Task<List<NarrationBlock>> GenerateFocusObservationAsync(
        ConcreteOutcome focusOutcome,
        ModusMentis observationModusMentis,
        NarrationNode currentNode,
        int locationId,
        bool isReminescence = false,
        CancellationToken ct = default)
    {
        Console.WriteLine($"ObservationPhaseController: Starting focus observation on '{focusOutcome.DisplayName}'");

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(observationModusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();

        // 1. Observe the clicked object (no transition).
        await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, observationModusMentis, focusOutcome, withTransition: false, locationId, ct, isPhaseOpener: true, isReminescence: isReminescence);

        // 2. A second object chosen by the Modus Mentis from the remaining objects, reached via a
        //    transition. Exclude the clicked object's name-twins (not just the clicked instance),
        //    then collapse remaining duplicates to one random representative each.
        var focusName = GetNeutralName(focusOutcome);
        var remaining = DeduplicateByName(
            currentNode.GetAllDirectConcreteOutcomes()
                .Where(o => !GetNeutralName(o).Equals(focusName, StringComparison.OrdinalIgnoreCase)));
        if (remaining.Count > 0)
        {
            var second = await ChooseObservationObjectAsync(slotId, remaining, observationModusMentis, ct, isReminescence,
                _worldContext?.GenerateContextDescription(locationId), currentNode.GenerateNeutralDescription(locationId));
            await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, observationModusMentis, second, withTransition: true, locationId, ct, isReminescence: isReminescence);
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

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends one object's observation to <paramref name="sentences"/> as a single rewritten entry
    /// built from one merged neutral text: an attention line naming the object by its simple phrase
    /// ("drawn to" for the first object, "shifts to" for a later one) plus a detail line giving its
    /// richer description. The persona rewrites both at once into two or three short sentences, from
    /// which the clickable keyword (mapped to <paramref name="outcome"/>) is extracted by rule.
    /// When <paramref name="isPhaseOpener"/> is set (the very first observation of the phase), the
    /// rewrite is GBNF-constrained to start with "I " so the whole block opens in first person.
    /// </summary>
    private async Task AppendObservationAsync(
        List<NarrationSentence> sentences,
        List<string> allKeywords,
        Dictionary<string, ConcreteOutcome> keywordOutcomeMap,
        int slotId,
        ModusMentis modusMentis,
        ConcreteOutcome outcome,
        bool withTransition,
        int locationId,
        CancellationToken ct,
        bool isPhaseOpener = false,
        bool isReminescence = false)
    {
        try
        {
            // Attention + detail merged into one neutral text and rewritten in a single request
            // (two or three short styled sentences) rather than two separate calls: the attention
            // line names the object ("drawn to" / "shifts to"), the detail line gives its richer
            // description. The phase opener is forced into first person ("I ...") to anchor the PoV.
            var neutral = NeutralNarration.Observation(
                isFirst: !withTransition,
                GetNeutralPhrase(outcome, locationId),
                GetNeutralDescription(outcome, locationId),
                isReminescence: isReminescence);
            var text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, forcedPrefix: isPhaseOpener ? "I " : null, styleInstruction: modusMentis.StyleInstruction, ct: ct);

            // Keyword is chosen by rule from the final (sanitized) text — the noun most related to the object.
            var kw = KeywordExtractor.ExtractKeyword(text, GetReferenceLemma(outcome));
            var kws = kw != null ? new List<string> { kw } : new List<string>();
            sentences.Add(new NarrationSentence(text, kws));
            if (kw != null)
            {
                allKeywords.Add(kw);
                keywordOutcomeMap.TryAdd(kw, outcome);
            }
            Console.WriteLine($"ObservationPhaseController: Observed '{outcome.DisplayName}' (keyword '{kw}')");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: Observation of '{outcome.DisplayName}' failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Collapses outcomes that share the same display name (e.g. several identical "Birch Tree"
    /// objects) down to a single, randomly-chosen representative per name. This keeps the
    /// observation-choice list free of duplicates, and — because both the first and second choice
    /// draw from the same deduplicated set — guarantees a chosen object's duplicates are never
    /// re-proposed for the second observation. Group order follows first appearance.
    /// </summary>
    private List<ConcreteOutcome> DeduplicateByName(IEnumerable<ConcreteOutcome> outcomes)
        => outcomes
            .GroupBy(GetNeutralName, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var members = g.ToList();
                return members[_random.Next(members.Count)];
            })
            .ToList();

    /// <summary>
    /// Asks the Modus Mentis to choose which object (by NeutralName) to observe — the one that best
    /// matches its persona interest. Returns the single candidate when there is only one, and a
    /// random candidate in playground mode (no LLM).
    ///
    /// Exception: during the childhood reminescence phase the <c>childhood_reminescence</c> MM picks
    /// at random (no LLM), so that across playthroughs every childhood memory fragment is reachable
    /// rather than the model always gravitating to the same few. Deliberately narrow — it does NOT
    /// apply to the post-childhood <c>childhood_memory</c> MM, to any other MM used during the phase,
    /// or to any observation outside the reminescence phase.
    /// </summary>
    private async Task<ConcreteOutcome> ChooseObservationObjectAsync(
        int slotId,
        List<ConcreteOutcome> candidates,
        ModusMentis modusMentis,
        CancellationToken ct,
        bool isReminescence = false,
        string? overallLocation = null,
        string? areaLocation = null)
    {
        if (candidates.Count == 1) return candidates[0];
        if (PlaygroundMode.IsActive) return candidates[_random.Next(candidates.Count)];
        if (isReminescence && modusMentis.ModusMentisId == "childhood_reminescence")
            return candidates[_random.Next(candidates.Count)];

        var names = candidates.Select(GetNeutralName).ToList();
        var prompt = BuildObservationChoicePrompt(names, modusMentis, overallLocation, areaLocation);
        var chosen = await _rewriter.ChooseAsync(slotId, prompt, names, "observation", keepHistory: true, ct);

        var idx = names.FindIndex(n => n.Equals(chosen, StringComparison.OrdinalIgnoreCase));
        return idx >= 0 ? candidates[idx] : candidates[0];
    }

    private static string BuildObservationChoicePrompt(List<string> names, ModusMentis modusMentis, string? overallLocation = null, string? areaLocation = null)
    {
        string reminderClause = modusMentis.PersonaReminder != null ? $"As a {modusMentis.PersonaReminder}, " : "";
        string list = string.Join("\n", names.Select(n => $"- {n}"));
        // Nothing is observed yet at this step, so only the location context is prepended.
        // Only the JSON-format clause is appended — the styling/grounding/"one short sentence" tail
        // belongs to the observation *text* generation, not to this constrained object choice.
        return $@"{ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, null)}Around you, you notice:
{list}

{reminderClause}which one draws your attention first?
{Config.Narrative.JsonFormatClause("{\"observation\": \"...\"}")}";
    }

    /// <summary>Short name of an outcome, used for the observation-choice enum.</summary>
    private static string GetNeutralName(ConcreteOutcome outcome)
        => outcome is ObservationObject obs ? obs.NeutralName
         : outcome.DisplayName;

    /// <summary>The outcome's core noun, used as the keyword-similarity anchor.</summary>
    private static string GetReferenceLemma(ConcreteOutcome outcome)
        => outcome is ObservationObject obs ? obs.ReferenceLemma
         : outcome.DisplayName;

    /// <summary>Articled noun phrase of an outcome, used to fill the transition sentence template.</summary>
    private static string GetNeutralPhrase(ConcreteOutcome outcome, int locationId)
        => outcome is ObservationObject obs ? obs.NeutralPhrase
         : GetNeutralDescription(outcome, locationId);

    /// <summary>
    /// Returns a rich noun-phrase description of an outcome for the observation sentence.
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
            var style = speakingModusMentis.StyleInstruction;
            var descr = GetNeutralDescription(linkedOutcome, locationId);

            var sentence1 = (await _rewriter.RewriteAsync(slotId, NeutralNarration.Attention(companionName), NarrationKind.Speaking, r2, companionName, keepHistory: true, styleInstruction: style, ct: ct)).Trim().Trim('"');
            var sentence2 = (await _rewriter.RewriteAsync(slotId, NeutralNarration.Description(descr), NarrationKind.Speaking, r2, companionName, keepHistory: true, styleInstruction: style, ct: ct)).Trim().Trim('"');
            var sentence3 = (await _rewriter.RewriteAsync(slotId, NeutralNarration.Question(), NarrationKind.Speaking, r2, companionName, keepHistory: true, styleInstruction: style, ct: ct)).Trim().Trim('"');

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
