using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cathedral;
using Cathedral.LLM;
using Cathedral.Game.Npc;
using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Narrative.Preview;

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
/// Every choice list is narrowed by the phase's <see cref="ObservationLedger"/>: an object observed
/// earlier in this narration phase is not offered again, so successive observations reach for new
/// things instead of circling the same one. As the pool shrinks, the persona is also given a refusal
/// option — "let your focus go" — which produces a short in-voice line saying there is nothing more
/// worth attending to. That option is withheld while the ledger is empty, so the request that opens a
/// phase always yields at least one keyword to click.
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
    private readonly Random _random = GameRng.Stream("observation-phase");

    /// <summary>
    /// How many keyword candidates a spoken address asks for before the party's own names are filtered
    /// out. One is not enough: the address opens by naming the companion, so the best-ranked noun can
    /// be that name, and the block would end up with no clickable word at all.
    /// </summary>
    private const int KeywordCandidatesForSpeaking = 4;

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
    ///
    /// This is the request that opens a narration phase, so <paramref name="ledger"/> is empty on
    /// entry (the controller clears it at every phase boundary) and the first choice is therefore made
    /// over the whole scene, with no refusal option.
    /// </summary>
    public async Task<List<NarrationBlock>> ExecuteObservationPhaseAsync(
        NarrationNode currentNode,
        PartyMember actingMember,
        int locationId,
        bool isReminescence = false,
        ObservationLedger? ledger = null,
        LlmPreviewSession? preview = null,
        Action<List<NarrationBlock>>? commit = null,
        CancellationToken ct = default)
    {
        ledger ??= new ObservationLedger();

        Console.WriteLine($"ObservationPhaseController: Starting overall observation for {currentNode.NodeId}");

        var modusMentis = PickFirstObservationModusMentis(actingMember);

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

        // Retire what this phase already looked at, THEN collapse identical objects (e.g. several
        // "Birch Tree") to one random representative each. Order matters: deduplicating first would
        // elect one birch to stand for the group, and retiring it would take its twins with it —
        // the ledger works on instances precisely so the second birch stays observable.
        var candidates = DeduplicateByName(ledger.Remaining(allOutcomes));

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
        // Both observation sentences (same modus mentis) stream into ONE preview part and are shown
        // stacked; CONTINUE unlocks only once the second finishes. The phase still commits as one
        // joined Observation block, deferred to that CONTINUE (see FinalizePreview).
        var part = preview?.BeginAccumulatingPart(PreviewTitles.For(modusMentis));

        var (first, firstThought) = await SelectObservationObjectAsync(slotId, candidates, modusMentis, ledger, locationId, ct, isReminescence, overall, area, part: part);

        if (first == null)
        {
            await AppendNothingObservationAsync(sentences, slotId, modusMentis, isReminescence, ct, innerThought: firstThought, part: part);
        }
        else
        {
            var firstText = await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, modusMentis, first, withTransition: false, locationId, ledger, ct, isReminescence: isReminescence, innerThought: firstThought, part: part);

            // Second (and possibly third) observation, gated on how long the first one(s) ran — see the
            // length rules in AppendFollowUpObservationsAsync. The candidate pool is the deduplicated
            // set; objects already observed are excluded from every follow-up choice.
            await AppendFollowUpObservationsAsync(sentences, allKeywords, keywordOutcomeMap, slotId, modusMentis,
                candidates, new List<ConcreteOutcome> { first }, firstText, locationId, ledger, ct, isReminescence, overall, area, part);
        }

        if (sentences.Count == 0)
        {
            preview?.Reset();
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
        var resultBlocks = new List<NarrationBlock> { block };
        FinalizePreview(preview, commit, resultBlocks, part);
        return resultBlocks;
    }

    /// <summary>
    /// Wires the deferred commit for a preview flow: the last part carries the buffer-append closure
    /// and is marked complete only after that closure is attached (so a fast CONTINUE can never fire a
    /// commit-less part). Non-preview callers commit immediately. Ordering matters — see the race note.
    /// </summary>
    private static void FinalizePreview(
        LlmPreviewSession? preview,
        Action<List<NarrationBlock>>? commit,
        List<NarrationBlock> blocks,
        PreviewPart? lastPart)
    {
        if (preview != null && lastPart != null)
        {
            if (commit != null) lastPart.AttachCommit(() => commit(blocks));
            lastPart.MarkComplete();
            preview.EndProduction();
        }
        else
        {
            commit?.Invoke(blocks);
            preview?.Reset();
        }
    }

    /// <summary>
    /// Generates a focus observation for a specific outcome (clicking "observe" on a keyword). The
    /// clicked focus is offered, not imposed: the (newly chosen) Modus Mentis first decides whether
    /// to keep its focus on the object or lose interest. Losing interest interrupts the chain of
    /// thought — the block is a single "I am not interested" sentence in its voice, with no detail
    /// and no keywords. Otherwise it observes the clicked object, then may choose one other object
    /// and bridge to it with a transition before observing it.
    ///
    /// <paramref name="focusOutcome"/> is the player's own choice and is observed whether or not this
    /// phase has already looked at it — the keyword that was clicked necessarily came from an earlier
    /// block, so it always has. Only the LLM-chosen follow-up draws from what is left unobserved.
    /// </summary>
    public async Task<List<NarrationBlock>> GenerateFocusObservationAsync(
        ConcreteOutcome focusOutcome,
        ModusMentis observationModusMentis,
        NarrationNode currentNode,
        int locationId,
        PartyMember actingMember,
        bool isReminescence = false,
        ObservationLedger? ledger = null,
        LlmPreviewSession? preview = null,
        Action<List<NarrationBlock>>? commit = null,
        CancellationToken ct = default)
    {
        ledger ??= new ObservationLedger();
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
        // Both focus sentences (same modus mentis) stream into one stacked preview part. The keep-focus
        // free reasoning opens the box before the first observation rewrite.
        var part = preview?.BeginAccumulatingPart(PreviewTitles.For(observationModusMentis));

        var (keeps, focusThought) = await AskKeepFocusAsync(slotId, observationModusMentis, focusOutcome, focusPhrase, ct, isReminescence, overall, area, part: part);
        if (!keeps)
            return await BuildNotInterestedBlockAsync(slotId, observationModusMentis, focusPhrase, isReminescence, ct, innerThought: focusThought, preview: preview, commit: commit, part: part);

        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();

        // 1. Observe the clicked object (no transition) — it is always the first observation.
        var firstText = await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, observationModusMentis, focusOutcome, withTransition: false, locationId, ledger, ct, isReminescence: isReminescence, innerThought: focusThought, part: part);

        // 2. A second (and possibly third) object, reached via a transition and gated on the same length
        //    rules as the overall phase (see AppendFollowUpObservationsAsync). The candidate pool drops
        //    everything this phase has already observed, then the clicked object's name-twins (so the
        //    persona cannot immediately shift to an identical one), then collapses duplicates.
        var focusName = GetNeutralName(focusOutcome);
        var followUpCandidates = DeduplicateByName(
            ledger.Remaining(currentNode.GetAllDirectConcreteOutcomes())
                .Where(o => !GetNeutralName(o).Equals(focusName, StringComparison.OrdinalIgnoreCase)));
        await AppendFollowUpObservationsAsync(sentences, allKeywords, keywordOutcomeMap, slotId, observationModusMentis,
            followUpCandidates, new List<ConcreteOutcome> { focusOutcome }, firstText, locationId, ledger, ct, isReminescence, overall, area, part);

        if (sentences.Count == 0)
        {
            preview?.Reset();
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
        var resultBlocks = new List<NarrationBlock> { block };
        FinalizePreview(preview, commit, resultBlocks, part);
        return resultBlocks;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends one object's observation to <paramref name="sentences"/> as a single rewritten entry
    /// built from one merged neutral text: an attention line naming the object by its simple phrase
    /// ("drawn to" for the first object, "shifts to" for a later one) plus a detail line giving its
    /// richer description. The persona rewrites both at once into two or three short sentences, from
    /// which the clickable keyword (mapped to <paramref name="outcome"/>) is extracted by rule.
    /// Every observation is GBNF-constrained to open in first person ("I ...") by the rewriter's
    /// per-kind default, so the whole block reads from the persona's point of view.
    ///
    /// This is the single place an object is recorded in <paramref name="ledger"/>, and it records
    /// only once text actually exists — a rewrite that threw leaves the object available. Every
    /// observation the game produces passes through here, including the ones the player or the scene
    /// imposes (a clicked keyword, the post-dialogue NPC, a threat), so those retire from later choice
    /// lists too.
    /// </summary>
    private async Task<string?> AppendObservationAsync(
        List<NarrationSentence> sentences,
        List<string> allKeywords,
        Dictionary<string, ConcreteOutcome> keywordOutcomeMap,
        int slotId,
        ModusMentis modusMentis,
        ConcreteOutcome outcome,
        bool withTransition,
        int locationId,
        ObservationLedger ledger,
        CancellationToken ct,
        bool isReminescence = false,
        string? innerThought = null,
        PreviewPart? part = null,
        string? trailingNeutral = null)
    {
        var sink = part?.NextSegment();
        try
        {
            // Attention + detail merged into one neutral text and rewritten in a single request
            // (two or three short styled sentences) rather than two separate calls: the attention
            // line names the object ("drawn to" / "shifts to"), the detail line gives its richer
            // description. The rewrite is forced into first person ("I ...") to anchor the PoV.
            // The focus-choice reasoning (when given) is the inner thought behind attending to this.
            var neutral = NeutralNarration.Observation(
                isFirst: !withTransition,
                GetNeutralPhrase(outcome, locationId),
                GetNeutralDescription(outcome, locationId),
                isReminescence: isReminescence);
            // An optional trailing sentence (e.g. a threat caution) is folded into the same neutral
            // text so the persona rewrite styles it into the block rather than tacking on plain prose.
            if (!string.IsNullOrWhiteSpace(trailingNeutral))
                neutral = $"{neutral} {trailingNeutral.Trim()}";
            var text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, styleInstruction: modusMentis.StyleInstruction, innerThought: innerThought, preview: sink, ct: ct);

            // Keywords are chosen by rule from the final (sanitized) text — the noun(s) most related to
            // the object. A long observation gets two distinct keywords, both mapped to this same object
            // so either click does the same thing; a normal-length one keeps a single keyword.
            int wanted = text.Length > Config.Narrative.ObservationTwoKeywordsThreshold ? 2 : 1;
            var kws = KeywordExtractor.ExtractKeywords(text, GetReferenceLemma(outcome), wanted);
            sentences.Add(new NarrationSentence(text, kws));
            ledger.Observe(outcome);
            foreach (var kw in kws)
            {
                allKeywords.Add(kw);
                keywordOutcomeMap.TryAdd(kw, outcome);
            }
            Console.WriteLine($"ObservationPhaseController: Observed '{outcome.DisplayName}' (keywords: {string.Join(", ", kws)})");
            return text;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: Observation of '{outcome.DisplayName}' failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// After the first object of a phase has been observed, appends a second and (conditionally) a third
    /// observation, gated on the length of what has been produced so far (thresholds in
    /// <see cref="Config.Narrative"/>):
    ///   • if the first observation's text alone exceeds <c>ObservationSkipSecondThreshold</c>, stop —
    ///     the first stands on its own and no second/third is added;
    ///   • otherwise ask for a second object from the still-unobserved candidates and observe it;
    ///   • then, if the first + second texts together stay below <c>ObservationTriggerThirdThreshold</c>
    ///     and more than <c>ObservationThirdMinRemaining</c> objects remain, observe a third.
    /// Shared by the overall and focus phases. <paramref name="candidates"/> is the deduplicated pool a
    /// follow-up may draw from (already stripped of what this phase observed); <paramref name="observed"/>
    /// holds the objects already narrated (the first, plus each follow-up as it is added) so they are
    /// never re-proposed. <paramref name="firstText"/> is the first observation's text (already appended;
    /// null if it failed).
    ///
    /// <para>A follow-up choice always carries the refusal option (something has been observed by now,
    /// so the ledger is not empty). Taking it ends the block with one in-voice line saying there is
    /// nothing more worth attending to, and no third observation is attempted — the persona said it was
    /// done looking. A pool that is merely empty stays silent, as before: that is bookkeeping, not a
    /// thought the character had.</para>
    /// </summary>
    private async Task AppendFollowUpObservationsAsync(
        List<NarrationSentence> sentences,
        List<string> allKeywords,
        Dictionary<string, ConcreteOutcome> keywordOutcomeMap,
        int slotId,
        ModusMentis modusMentis,
        List<ConcreteOutcome> candidates,
        List<ConcreteOutcome> observed,
        string? firstText,
        int locationId,
        ObservationLedger ledger,
        CancellationToken ct,
        bool isReminescence,
        string? overall,
        string? area,
        PreviewPart? part)
    {
        // A first observation long enough on its own stops the phase here.
        if ((firstText?.Length ?? 0) > Config.Narrative.ObservationSkipSecondThreshold)
            return;

        // Second observation over the still-unobserved objects, reached via a transition.
        string? secondText = null;
        var remaining = candidates.Where(c => !observed.Contains(c)).ToList();
        if (remaining.Count > 0)
        {
            bool declineOffered = !ledger.IsEmpty;
            var (second, secondThought) = await SelectObservationObjectAsync(slotId, remaining, modusMentis, ledger, locationId, ct, isReminescence, overall, area, part: part);
            if (second == null)
            {
                // Refused: close the block in voice and stop — nothing more here is worth attending to.
                if (declineOffered)
                    await AppendNothingObservationAsync(sentences, slotId, modusMentis, isReminescence, ct, innerThought: secondThought, part: part, nothingMore: true);
                return;
            }

            secondText = await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, modusMentis, second, withTransition: true, locationId, ledger, ct, isReminescence: isReminescence, innerThought: secondThought, part: part);
            observed.Add(second);
        }

        // Third observation when the first two stayed short and enough objects remain to be observed.
        int combinedLength = (firstText?.Length ?? 0) + (secondText?.Length ?? 0);
        var stillRemaining = candidates.Where(c => !observed.Contains(c)).ToList();
        if (combinedLength < Config.Narrative.ObservationTriggerThirdThreshold
            && stillRemaining.Count > Config.Narrative.ObservationThirdMinRemaining)
        {
            bool declineOffered = !ledger.IsEmpty;
            var (third, thirdThought) = await SelectObservationObjectAsync(slotId, stillRemaining, modusMentis, ledger, locationId, ct, isReminescence, overall, area, part: part);
            if (third == null)
            {
                if (declineOffered)
                    await AppendNothingObservationAsync(sentences, slotId, modusMentis, isReminescence, ct, innerThought: thirdThought, part: part, nothingMore: true);
                return;
            }

            await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, modusMentis, third, withTransition: true, locationId, ledger, ct, isReminescence: isReminescence, innerThought: thirdThought, part: part);
        }
    }

    /// <summary>
    /// Picks the modus mentis that narrates the first observation of a phase: a random one drawn — by
    /// priority — from the observation modi mentis currently held in the active member's sensory memory,
    /// falling back to any observation modus mentis when none of them sits in sensory memory. Returns
    /// null only when the member has no observation modus mentis at all.
    /// </summary>
    private ModusMentis? PickFirstObservationModusMentis(PartyMember member)
    {
        var observation = member.GetObservationModiMentis();
        if (observation.Count == 0) return null;

        var inSensory = member.GetMemoryModule(MemoryModuleType.Sensory)?.FilledModiMentis.ToHashSet()
                        ?? new HashSet<ModusMentis>();
        var preferred = observation.Where(mm => inSensory.Contains(mm)).ToList();
        var pool = preferred.Count > 0 ? preferred : observation;
        return pool[_random.Next(pool.Count)];
    }

    /// <summary>
    /// First observation of a narration phase opened right after a dialogue ended, kept continuous with
    /// that conversation: the narrator is <paramref name="originObservationModusMentis"/> — the observation
    /// MM that originated the dialogue's chain of thought — when it is still learned, otherwise a resampled
    /// one (sensory memory first, like any first observation). The observed object is
    /// <paramref name="npcOutcome"/> — the NPC that was talked to — with no "what draws you?" selection, and
    /// it is a single observation regardless of length (no second or third). Follows the same
    /// rewrite/keyword path as any observation, so a long text still gets two keywords. Returns an empty
    /// list (so the caller can fall back to the normal phase) when no observation MM is available.
    /// </summary>
    public async Task<List<NarrationBlock>> GeneratePostDialogueObservationAsync(
        ConcreteOutcome npcOutcome,
        ModusMentis? originObservationModusMentis,
        int locationId,
        PartyMember actingMember,
        ObservationLedger? ledger = null,
        LlmPreviewSession? preview = null,
        Action<List<NarrationBlock>>? commit = null,
        CancellationToken ct = default)
    {
        ledger ??= new ObservationLedger();
        // Reuse the originating observation MM if it is still learned; otherwise resample one.
        var observationModusMentis =
            originObservationModusMentis != null && actingMember.LearnedModiMentis.Contains(originObservationModusMentis)
                ? originObservationModusMentis
                : PickFirstObservationModusMentis(actingMember);
        if (observationModusMentis == null)
        {
            Console.WriteLine("ObservationPhaseController: No observation modus mentis for post-dialogue observation.");
            return new List<NarrationBlock>();
        }

        Console.WriteLine($"ObservationPhaseController: Post-dialogue observation of '{npcOutcome.DisplayName}' with {observationModusMentis.DisplayName}");

        (npcOutcome as INpcContextLabelStampable)?.StampContextLabel(actingMember, _worldContext, locationId);

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(observationModusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();

        var part = preview?.BeginAccumulatingPart(PreviewTitles.For(observationModusMentis));

        // Exactly one observation, of the NPC, whatever its length — no follow-up choice. It still goes
        // on the ledger, so a later request this phase reaches for something other than the person just
        // spoken to.
        await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, observationModusMentis, npcOutcome, withTransition: false, locationId, ledger, ct, isReminescence: false, innerThought: null, part: part);

        if (sentences.Count == 0)
        {
            preview?.Reset();
            Console.WriteLine("ObservationPhaseController: Post-dialogue observation produced nothing.");
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
            Sentences: sentences);

        Console.WriteLine($"ObservationPhaseController: Post-dialogue observation complete ({allKeywords.Count} keywords)");
        var resultBlocks = new List<NarrationBlock> { block };
        FinalizePreview(preview, commit, resultBlocks, part);
        return resultBlocks;
    }

    /// <summary>
    /// Opens a narration phase with a single, forced observation of a same-area enemy — the "under
    /// threat" lead-in used when a visual threat is present (the same condition that turns the exit
    /// button into RUNAWAY). Like the post-dialogue opener, the observed object is imposed
    /// (<paramref name="threatOutcome"/>, no "what draws you?" selection) and it is one observation
    /// regardless of length (no second or third). Unlike it, the narrator is freshly sampled (sensory
    /// memory first, like any first observation) and a threat-caution sentence is folded into the
    /// neutral text so the persona rewrites a note of danger into the block. Returns an empty list
    /// (so the caller can fall back to the normal phase) when no observation MM is available or the
    /// rewrite produced nothing.
    /// </summary>
    public async Task<List<NarrationBlock>> GenerateThreatObservationAsync(
        ConcreteOutcome threatOutcome,
        int locationId,
        PartyMember actingMember,
        ObservationLedger? ledger = null,
        LlmPreviewSession? preview = null,
        Action<List<NarrationBlock>>? commit = null,
        CancellationToken ct = default)
    {
        ledger ??= new ObservationLedger();
        var observationModusMentis = PickFirstObservationModusMentis(actingMember);
        if (observationModusMentis == null)
        {
            Console.WriteLine("ObservationPhaseController: No observation modus mentis for threat observation.");
            return new List<NarrationBlock>();
        }

        Console.WriteLine($"ObservationPhaseController: Threat observation of '{threatOutcome.DisplayName}' with {observationModusMentis.DisplayName}");

        (threatOutcome as INpcContextLabelStampable)?.StampContextLabel(actingMember, _worldContext, locationId);

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(observationModusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var keywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<NarrationSentence>();

        var part = preview?.BeginAccumulatingPart(PreviewTitles.For(observationModusMentis));

        // Exactly one observation, of the enemy, whatever its length — no follow-up choice. The
        // threat-caution sentence rides into the same rewrite as the observation itself.
        await AppendObservationAsync(sentences, allKeywords, keywordOutcomeMap, slotId, observationModusMentis,
            threatOutcome, withTransition: false, locationId, ledger, ct, isReminescence: false, innerThought: null,
            part: part, trailingNeutral: NeutralNarration.ThreatCaution());

        if (sentences.Count == 0)
        {
            preview?.Reset();
            Console.WriteLine("ObservationPhaseController: Threat observation produced nothing.");
            return new List<NarrationBlock>();
        }

        var block = new NarrationBlock(
            Type: NarrationBlockType.Observation,
            ModusMentis: observationModusMentis,
            Text: string.Join(" ", sentences.Select(s => s.Text)),
            Keywords: allKeywords,
            Actions: null,
            SourceObservationType: ObservationType.Overall,
            KeywordOutcomeMap: keywordOutcomeMap,
            Sentences: sentences);

        Console.WriteLine($"ObservationPhaseController: Threat observation complete ({allKeywords.Count} keywords)");
        var resultBlocks = new List<NarrationBlock> { block };
        FinalizePreview(preview, commit, resultBlocks, part);
        return resultBlocks;
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
    /// refusal line. Returns a random object in playground mode (no LLM).
    ///
    /// <para><paramref name="candidates"/> arrives already stripped of everything this narration phase
    /// has observed, so the list shrinks request by request. The refusal option — "let your focus go" —
    /// is offered exactly when <paramref name="ledger"/> is non-empty: with the pool narrowing, a
    /// persona forced to pick from two leftovers it cares nothing for would attend to them out of
    /// character, and it needs a way to say so. Withholding it while the ledger is empty is what keeps
    /// the request that opens a phase from producing a block with no keyword to click.</para>
    /// </summary>
    private async Task<PersonaChoice<ConcreteOutcome>> SelectObservationObjectAsync(
        int slotId,
        List<ConcreteOutcome> candidates,
        ModusMentis modusMentis,
        ObservationLedger ledger,
        int locationId,
        CancellationToken ct,
        bool isReminescence = false,
        string? overallLocation = null,
        string? areaLocation = null,
        PreviewPart? part = null)
    {
        if (candidates.Count == 0) return new PersonaChoice<ConcreteOutcome>(null, null);

        // Each object is offered as the act of attending to it — "focus on the plowman of the field
        // (a woman)" — via GetNeutralPhrase (proper names stay verbatim; common-noun objects gain
        // "a"/"an").
        var prompt = new PersonaChoicePrompt(
            ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, null),
            "What do you want to focus on?", "what they want to focus on");
        // The free reasoning streams into the box as a dimmer inner thought before the observation
        // rewrite. Playground mode never declines (the selector short-circuits to a real option).
        string? decline = ledger.IsEmpty
            ? null
            : isReminescence ? "let the recollection go, nothing more is surfacing"
                             : "let your focus go, nothing else here is worth attending to";
        return await _selector.SelectAsync(
            slotId, modusMentis, candidates,
            c => $"focus on {GetNeutralPhrase(c, locationId)}",
            prompt, declineOption: decline, preview: part?.NextSegment(isFree: true), ct: ct);
    }

    /// <summary>
    /// Asks the (new) focus Modus Mentis whether it keeps its focus on the handed object or loses
    /// interest, via the same persona-reasoning → neutral-critic pass as every other choice: the
    /// options are "keep your focus on X" and, as the decline option, "lose interest in X" — a
    /// <c>null</c> pick means the persona lost interest. Playground mode never declines (the
    /// selector picks the only real option), and the childhood-reminescence MM always keeps the
    /// handed memory: a fragment the player reached for is the phase's only way forward, since
    /// REMEMBER is the sole action there.
    /// </summary>
    private async Task<(bool Keeps, string? Reasoning)> AskKeepFocusAsync(
        int slotId,
        ModusMentis modusMentis,
        ConcreteOutcome focusOutcome,
        string focusPhrase,
        CancellationToken ct,
        bool isReminescence,
        string? overallLocation,
        string? areaLocation,
        PreviewPart? part = null)
    {
        if (isReminescence && modusMentis.ModusMentisId == "childhood_reminescence") return (true, null);

        var prompt = new PersonaChoicePrompt(
            ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, focusPhrase),
            "What do you want to do?", "what they want to do");
        var kept = await _selector.SelectAsync(
            slotId, modusMentis, new[] { focusOutcome },
            _ => $"keep your focus on {focusPhrase}",
            prompt, declineOption: "turn my attention elsewhere instead", preview: part?.NextSegment(isFree: true), ct: ct);
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
        string? innerThought = null,
        LlmPreviewSession? preview = null,
        Action<List<NarrationBlock>>? commit = null,
        PreviewPart? part = null)
    {
        var neutral = NeutralNarration.ObservationNotInterested(focusPhrase, isReminescence);
        string text;
        try
        {
            text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, styleInstruction: modusMentis.StyleInstruction, innerThought: innerThought, preview: part?.NextSegment(), ct: ct);
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
        var resultBlocks = new List<NarrationBlock> { block };
        FinalizePreview(preview, commit, resultBlocks, part);
        return resultBlocks;
    }

    /// <summary>
    /// Appends the "nothing here draws my attention" observation (the decline result of the object
    /// evaluation), re-expressed in the Modus Mentis's voice with no clickable keyword.
    ///
    /// With <paramref name="nothingMore"/> it is instead the "nothing further" line that closes a block
    /// which already observed something: the persona was offered what this phase has not looked at yet
    /// and let its focus go. Same rewrite path, same absence of a keyword — only the neutral meaning
    /// differs (see <see cref="NeutralNarration.ObservationNothingMore"/>).
    /// </summary>
    private async Task AppendNothingObservationAsync(
        List<NarrationSentence> sentences,
        int slotId,
        ModusMentis modusMentis,
        bool isReminescence,
        CancellationToken ct,
        string? innerThought = null,
        PreviewPart? part = null,
        bool nothingMore = false)
    {
        var sink = part?.NextSegment();
        try
        {
            var neutral = nothingMore
                ? NeutralNarration.ObservationNothingMore(isReminescence)
                : NeutralNarration.ObservationNothing(isReminescence);
            var text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, styleInstruction: modusMentis.StyleInstruction, innerThought: innerThought, preview: sink, ct: ct);
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
         : outcome is ObservationObject obs ? obs.GenerateNeutralDescription(locationId)
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
    /// The three neutral lines (call attention → describe → ask) are merged into one address and
    /// rewritten as direct speech in a single request — see <see cref="NeutralNarration.SpokenReport"/>.
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
        LlmPreviewSession? preview = null,
        Action<NarrationBlock>? commit = null,
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

            // One request, one spoken turn, one preview segment; the part carries the deferred commit
            // (finalize below). The rewriter returns the words between the quotes — the address is
            // generated behind the same `I say to X : "…"` frame a dialogue reply is.
            var part = preview?.BeginAccumulatingPart(PreviewTitles.For(speakingModusMentis));
            var spoken = (await _rewriter.RewriteAsync(
                slotId, NeutralNarration.SpokenReport(companionName, descr), NarrationKind.Speaking,
                r2, companionName, keepHistory: true, styleInstruction: style,
                preview: part?.NextSegment(), ct: ct)).Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(spoken))
            {
                preview?.Reset();
                Console.WriteLine("ObservationPhaseController: Speaking line empty.");
                return null;
            }

            var spokenText = $"\"{spoken}\"";

            // Keyword: extracted from the spoken line by the same rule every observation uses — a noun
            // the persona actually wrote, ranked by relatedness to the object's reference lemma. A word
            // the line does not contain cannot be clicked, and the hand-off depends on this click: the
            // companion becomes the active member with this block as their observation root, so a
            // speaking block with no keyword leaves them nothing to look at or think about.
            //
            // Several candidates are asked for rather than one because the address names the companion
            // ("Ahoy James, cast an eye…") and a proper name is a noun like any other — the party's own
            // names are skipped so the click lands on the thing spoken about, not the person spoken to.
            var partyWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in new[] { companionName, actingMember.DisplayName })
                foreach (var w in (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    partyWords.Add(w.Trim('.', ',', '\'', '"'));

            var allExtractedKeywords = new List<string>();
            var speakingKeywordOutcomeMap = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
            string? kw = KeywordExtractor
                .ExtractKeywords(spoken, GetReferenceLemma(linkedOutcome), KeywordCandidatesForSpeaking)
                .FirstOrDefault(k => !partyWords.Contains(k));
            if (kw != null)
            {
                allExtractedKeywords.Add(kw);
                speakingKeywordOutcomeMap[kw] = linkedOutcome;
            }
            else
            {
                Console.Error.WriteLine("ObservationPhaseController: Speaking line yielded no keyword — the companion will have nothing to click.");
            }

            // One sentence covering the whole address; the renderer highlights only where the keyword
            // appears within it.
            var speakingSentences = new List<NarrationSentence> { new NarrationSentence(spoken, allExtractedKeywords) };

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

            // Deferred commit on CONTINUE (race-free: complete only after the commit attaches).
            if (preview != null && part != null)
            {
                if (commit != null) part.AttachCommit(() => commit(block));
                part.MarkComplete();
                preview.EndProduction();
            }
            else
            {
                commit?.Invoke(block);
                preview?.Reset();
            }
            return block;
        }
        catch (Exception ex)
        {
            preview?.Reset();
            Console.Error.WriteLine($"ObservationPhaseController: Speaking generation failed: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return null;
        }
    }
}
