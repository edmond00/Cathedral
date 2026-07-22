using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cathedral;
using Cathedral.LLM;
using Cathedral.Game.Npc;

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
    private readonly PersonaChoiceSelector _selector;
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
        _selector            = new PersonaChoiceSelector(llamaServer);
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

        // Stamp the contextual NPC label (relation + role + location) for this narrator's POV
        // before any prompt text is built. Non-NPC / shallow outcomes no-op.
        foreach (var o in allOutcomes)
            (o as INpcContextLabelStampable)?.StampContextLabel(actingMember, _worldContext, locationId);

        // Collapse identical objects (e.g. several "Birch Tree") to one random representative each,
        // so the choice list has no duplicates and a chosen object's twins are not re-proposed.
        var candidates = DeduplicateByName(allOutcomes);

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(modusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();

        string? overall = _worldContext?.GenerateContextDescription(locationId);
        string? area    = currentNode.GenerateNeutralDescription(locationId);

        // First observation: the Modus Mentis reasons over every candidate and the neutral critic maps
        // that to one object (or the decline option). A null result means nothing here drew it at all —
        // a single "nothing draws me" block. The focus reasoning rides into the observation rewrite as
        // the inner thought behind why this object drew the persona.
        var (first, firstThought) = await SelectObservationObjectAsync(slotId, candidates, modusMentis, locationId, ct, isReminescence, overall, area);

        if (first == null)
        {
            await AppendNothingObservationAsync(sentences, slotId, modusMentis, isReminescence, ct, innerThought: firstThought);
        }
        else
        {
            await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, modusMentis, first, withTransition: false, locationId, ct, isPhaseOpener: true, isReminescence: isReminescence, innerThought: firstThought);

            // Second observation: ask again over the remaining objects (the first excluded), reached
            // via a transition. Declining here simply omits the second — no failure block.
            var remaining = candidates.Where(c => !ReferenceEquals(c, first)).ToList();
            if (remaining.Count > 0)
            {
                var (second, secondThought) = await SelectObservationObjectAsync(slotId, remaining, modusMentis, locationId, ct, isReminescence, overall, area);
                if (second != null)
                    await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, modusMentis, second, withTransition: true, locationId, ct, isReminescence: isReminescence, innerThought: secondThought);
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
    /// Generates a focus observation for a specific outcome (clicking "observe" on a keyword). The
    /// clicked focus is offered, not imposed: the (newly chosen) Modus Mentis first decides whether
    /// to keep its focus on the object or lose interest. Losing interest interrupts the chain of
    /// thought — the block is a single "I am not interested" sentence in its voice, with no detail
    /// and no keywords. Otherwise it observes the clicked object, then may choose one other object
    /// and bridge to it with a transition before observing it.
    /// </summary>
    public async Task<List<NarrationBlock>> GenerateFocusObservationAsync(
        ConcreteOutcome focusOutcome,
        ModusMentis observationModusMentis,
        NarrationNode currentNode,
        int locationId,
        PartyMember actingMember,
        bool isReminescence = false,
        CancellationToken ct = default)
    {
        Console.WriteLine($"ObservationPhaseController: Starting focus observation on '{focusOutcome.DisplayName}'");

        // Stamp the contextual NPC label for this narrator's POV on the clicked outcome and every
        // other node outcome (the second, LLM-chosen object is drawn from these). Non-NPC no-op.
        (focusOutcome as INpcContextLabelStampable)?.StampContextLabel(actingMember, _worldContext, locationId);
        foreach (var o in currentNode.GetAllDirectConcreteOutcomes())
            (o as INpcContextLabelStampable)?.StampContextLabel(actingMember, _worldContext, locationId);

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(observationModusMentis);
        _observationExecutor.ResetSlot(slotId);

        string  focusPhrase = GetNeutralPhrase(focusOutcome, locationId);
        string? overall     = _worldContext?.GenerateContextDescription(locationId);
        string? area        = currentNode.GenerateNeutralDescription(locationId);

        // 0. Keep focus or lose interest? The new Modus Mentis may interrupt the chain of thought
        //    right here: losing interest yields a single "not interested" sentence — no observation,
        //    no keywords — and nothing else happens. Either way its reasoning rides into the next
        //    rewrite as the inner thought behind keeping (or dropping) the focus.
        var (keeps, focusThought) = await AskKeepFocusAsync(slotId, observationModusMentis, focusOutcome, focusPhrase, ct, isReminescence, overall, area);
        if (!keeps)
            return await BuildNotInterestedBlockAsync(slotId, observationModusMentis, focusPhrase, isReminescence, ct, innerThought: focusThought);

        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();

        // 1. Observe the clicked object (no transition) — it is always the first observation.
        await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, observationModusMentis, focusOutcome, withTransition: false, locationId, ct, isPhaseOpener: true, isReminescence: isReminescence, innerThought: focusThought);

        // 2. A second object chosen from the remaining objects, reached via a transition. Exclude the
        //    clicked object's name-twins (not just the clicked instance), then collapse duplicates.
        var focusName = GetNeutralName(focusOutcome);
        var remaining = DeduplicateByName(
            currentNode.GetAllDirectConcreteOutcomes()
                .Where(o => !GetNeutralName(o).Equals(focusName, StringComparison.OrdinalIgnoreCase)));
        if (remaining.Count > 0)
        {
            // The clicked object is already observed; the Modus Mentis may or may not want a second.
            // Declining here simply omits it — no failure block.
            var (second, secondThought) = await SelectObservationObjectAsync(slotId, remaining, observationModusMentis, locationId, ct, isReminescence,
                overall, area);
            if (second != null)
                await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, observationModusMentis, second, withTransition: true, locationId, ct, isReminescence: isReminescence, innerThought: secondThought);
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
        bool isReminescence = false,
        string? innerThought = null)
    {
        try
        {
            // Attention + detail merged into one neutral text and rewritten in a single request
            // (two or three short styled sentences) rather than two separate calls: the attention
            // line names the object ("drawn to" / "shifts to"), the detail line gives its richer
            // description. The phase opener is forced into first person ("I ...") to anchor the PoV.
            // The focus-choice reasoning (when given) is the inner thought behind attending to this.
            var neutral = NeutralNarration.Observation(
                isFirst: !withTransition,
                GetNeutralPhrase(outcome, locationId),
                GetNeutralDescription(outcome, locationId),
                isReminescence: isReminescence);
            var text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, forcedPrefix: isPhaseOpener ? "I " : null, styleInstruction: modusMentis.StyleInstruction, innerThought: innerThought, ct: ct);

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
    /// The Modus Mentis reasons over the candidate objects ("What do you want to focus on?") and the
    /// neutral <see cref="PersonaMatchCritic"/> maps that to one object — the one it wants to attend to
    /// — or to the decline option, in which case this returns <c>null</c> and the caller renders the
    /// "nothing draws me" failure. Returns a random object in playground mode (no LLM).
    ///
    /// Exception: during the childhood reminescence phase the <c>childhood_reminescence</c> MM picks
    /// at random (no LLM, never declines), so that across playthroughs every childhood memory fragment
    /// is reachable rather than the model always gravitating to the same few. Deliberately narrow — it
    /// does NOT apply to the post-childhood <c>childhood_memory</c> MM, to any other MM used during the
    /// phase, or to any observation outside the reminescence phase.
    /// </summary>
    private async Task<PersonaChoice<ConcreteOutcome>> SelectObservationObjectAsync(
        int slotId,
        List<ConcreteOutcome> candidates,
        ModusMentis modusMentis,
        int locationId,
        CancellationToken ct,
        bool isReminescence = false,
        string? overallLocation = null,
        string? areaLocation = null)
    {
        if (candidates.Count == 0) return new PersonaChoice<ConcreteOutcome>(null, null);

        // Childhood reminescence: random pick, never declines (keeps every fragment reachable).
        if (isReminescence && modusMentis.ModusMentisId == "childhood_reminescence")
            return new PersonaChoice<ConcreteOutcome>(candidates[_random.Next(candidates.Count)], null);

        // Each object is offered as the act of attending to it — "focus on the plowman of the field
        // (a woman)" — via GetNeutralPhrase (proper names stay verbatim; common-noun objects gain
        // "a"/"an").
        var prompt = new PersonaChoicePrompt(
            ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, null),
            "What do you want to focus on?", "what they want to focus on");
        // No decline option for now — the persona always settles on one object to attend to.
        return await _selector.SelectAsync(
            slotId, modusMentis, candidates,
            c => $"focus on {GetNeutralPhrase(c, locationId)}",
            prompt, ct: ct);
    }

    /// <summary>
    /// Asks the (new) focus Modus Mentis whether it keeps its focus on the handed object or loses
    /// interest, via the same persona-reasoning → neutral-critic pass as every other choice: the
    /// options are "keep your focus on X" and, as the decline option, "lose interest in X" — a
    /// <c>null</c> pick means the persona lost interest. Playground mode never declines (the
    /// selector picks the only real option), and the childhood-reminescence MM always keeps the
    /// handed memory (mirrors its random-pick exception in <see cref="SelectObservationObjectAsync"/>).
    /// </summary>
    private async Task<(bool Keeps, string? Reasoning)> AskKeepFocusAsync(
        int slotId,
        ModusMentis modusMentis,
        ConcreteOutcome focusOutcome,
        string focusPhrase,
        CancellationToken ct,
        bool isReminescence,
        string? overallLocation,
        string? areaLocation)
    {
        if (isReminescence && modusMentis.ModusMentisId == "childhood_reminescence") return (true, null);

        var prompt = new PersonaChoicePrompt(
            ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, focusPhrase),
            "What do you want to do?", "what they want to do");
        var kept = await _selector.SelectAsync(
            slotId, modusMentis, new[] { focusOutcome },
            _ => $"keep your focus on {focusPhrase}",
            prompt, declineOption: $"lose interest in {focusPhrase}", ct: ct);
        return (kept.Item != null, kept.Reasoning);
    }

    /// <summary>
    /// Builds the whole focus block for a refused focus: the "I am not interested in X" line
    /// re-expressed in the refusing Modus Mentis's voice — no detail, no clickable keyword, so the
    /// chain of thought ends here.
    /// </summary>
    private async Task<List<NarrationBlock>> BuildNotInterestedBlockAsync(
        int slotId,
        ModusMentis modusMentis,
        string focusPhrase,
        bool isReminescence,
        CancellationToken ct,
        string? innerThought = null)
    {
        var neutral = NeutralNarration.ObservationNotInterested(focusPhrase, isReminescence);
        string text;
        try
        {
            text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, styleInstruction: modusMentis.StyleInstruction, innerThought: innerThought, ct: ct);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: 'not interested' rewrite failed: {ex.Message}");
            text = neutral;
        }

        Console.WriteLine($"ObservationPhaseController: focus refused — lost interest in '{focusPhrase}'.");
        var block = new NarrationBlock(
            Type: NarrationBlockType.Observation,
            ModusMentis: modusMentis,
            Text: text,
            Keywords: new List<string>(),
            Actions: null,
            SourceObservationType: ObservationType.Focus,
            KeywordOutcomeMap: new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase),
            Sentences: new List<NarrationSentence> { new(text, new List<string>()) }
        );
        return new List<NarrationBlock> { block };
    }

    /// <summary>
    /// Appends the "nothing here draws my attention" observation (the decline result of the object
    /// evaluation), re-expressed in the Modus Mentis's voice with no clickable keyword.
    /// </summary>
    private async Task AppendNothingObservationAsync(
        List<NarrationSentence> sentences,
        int slotId,
        ModusMentis modusMentis,
        bool isReminescence,
        CancellationToken ct,
        string? innerThought = null)
    {
        try
        {
            var neutral = NeutralNarration.ObservationNothing(isReminescence);
            var text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, styleInstruction: modusMentis.StyleInstruction, innerThought: innerThought, ct: ct);
            sentences.Add(new NarrationSentence(text, new List<string>()));
            Console.WriteLine("ObservationPhaseController: nothing drew the persona's attention (declined).");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: 'nothing' observation failed: {ex.Message}");
        }
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
