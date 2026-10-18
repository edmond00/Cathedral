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
/// matching its persona interest (by NeutralName).
///
/// The overall phase may open on an area introduction — where and when I am — when the caller asks
/// for one (the area or the time of day changed since the last; exploration only). It lists up to five
/// of the objects present, takes the first of the three slots, carries at most one keyword — only if it
/// names an object outright, which then counts as observed for the phase — and is always
/// followed by at least one real observation (see <see cref="AppendAreaIntroductionAsync"/>).
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
    private readonly WorldContext? _worldContext;
    private readonly Random _random = GameRng.Stream("observation-phase");

    /// <summary>
    /// How many keyword candidates a spoken address asks for before the party's own names are filtered
    /// out. One is not enough: the address opens by naming the companion, so the best-ranked noun can
    /// be that name, and the block would end up with no clickable word at all.
    /// </summary>
    private const int KeywordCandidatesForSpeaking = 4;

    /// <summary>
    /// True while every choice must commit to something — the childhood reminescence and the get-up
    /// phase. There the scene holds only the fragments or facets that lead onward, so no modus mentis
    /// is offered to let its focus go, nor asked whether a handed object holds anything for it: a
    /// "stop" there only left the player hunting for a keyword that reached the rest. Read from the
    /// narrative controller's scene, which this controller does not hold.
    /// </summary>
    public Func<bool> MustCommit { get; set; } = () => false;

    public ObservationPhaseController(
        LlamaServerManager llamaServer,
        ModusMentisSlotManager slotManager,
        WorldContext? worldContext = null)
    {
        _observationExecutor = new ObservationExecutor(llamaServer, slotManager);
        _rewriter            = new PersonaRewriter(llamaServer);
        _selector            = new PersonaChoiceSelector(llamaServer);
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
        TimePeriod? introPeriod = null,
        bool ensurePersonKeyword = false,
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

        // Every observation modus mentis is broken — the picker only ever returns one of those when
        // there is nothing else left. The phase opens on the refusal instead of an observation.
        if (actingMember.IsModusMentisBroken(modusMentis))
        {
            Console.WriteLine(
                "ObservationPhaseController: every observation modus mentis is broken — " +
                "opening the phase on the refusal.");
            var brokenSlot = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(modusMentis);
            _observationExecutor.ResetSlot(brokenSlot);
            return await BrokenObservationBlockAsync(
                modusMentis, actingMember, brokenSlot, preview, commit, ct);
        }

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
        var candidates = DeduplicateByName(ObserveOnlyFilter(ledger.Remaining(allOutcomes)));

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(modusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var sentences = new List<NarrationSentence>();

        string? overall = _worldContext?.GenerateContextDescription(locationId);
        string? area    = currentNode.GenerateNeutralDescription(locationId);

        // First observation: the choice judge picks one object for this Modus Mentis (or the decline
        // option). A null result means nothing here drew it at all — a single "nothing draws me" block.
        // Both observation sentences (same modus mentis) stream into ONE preview part and are shown
        // stacked; CONTINUE unlocks only once the second finishes. The phase still commits as one
        // joined Observation block, deferred to that CONTINUE (see FinalizePreview).
        var part = preview?.BeginAccumulatingPart(PreviewTitles.For(modusMentis));

        // Area introduction: when the caller asks for one (the area or the time of day changed since
        // the last one), the block opens on where and when I am, and what is around me, before any
        // object is singled out. Every object the introduction linked a keyword to is retired here —
        // from this block's pool and from the ledger — exactly as an observed object would be.
        string? introText = null;
        if (introPeriod is { } period && currentNode is Cathedral.Game.Scene.SyntheticNarrationNode { Area: { } introArea })
        {
            List<NarrativeAnchor> introAnchors;
            (introText, introAnchors) = await AppendAreaIntroductionAsync(sentences, allKeywords, slotId, modusMentis,
                introArea, period, candidates, ledger, locationId, ct, part);
            foreach (var a in introAnchors) candidates.Remove(a);
        }

        // The first real observation is mandatory: an introduction that retired an object has made the
        // ledger non-empty, but the persona still may not decline here.
        var first = await SelectObservationObjectAsync(candidates, modusMentis, ledger, locationId, ct, isReminescence, overall, area, offerDecline: false);
        // A choice used to be asked on this slot and wiped its history; the rewrite that follows
        // keeps history, so the slot is still cleared here to keep the block's context bounded.
        _observationExecutor.ResetSlot(slotId);

        if (first == null)
        {
            await AppendNothingObservationAsync(sentences, slotId, modusMentis, isReminescence, ct, part: part);
        }
        else
        {
            var firstText = await AppendObservationAsync(sentences, allKeywords, slotId, modusMentis, first, withTransition: false, locationId, ledger, ct, isReminescence: isReminescence, part: part);

            // Second (and possibly third) observation, gated on how long the first one(s) ran — see the
            // length rules in AppendFollowUpObservationsAsync. The candidate pool is the deduplicated
            // set; objects already observed are excluded from every follow-up choice.
            await AppendFollowUpObservationsAsync(sentences, allKeywords, slotId, modusMentis,
                candidates, new List<NarrativeAnchor> { first }, firstText, locationId, ledger, ct, isReminescence, overall, area, part,
                introText,
                // A person is about to be added: "nothing else here is worth attending to" followed
                // by somebody attended to reads as a contradiction, so a decline ends the block quietly.
                quietDecline: ensurePersonKeyword && !isReminescence
                              && candidates.Any(Rules.Choice.SpeakingPriority.IsPerson)
                              && !Rules.Choice.SpeakingPriority.IsPerson(first));
        }

        if (ensurePersonKeyword && !isReminescence)
            await AppendPersonObservationIfMissingAsync(sentences, allKeywords, slotId, modusMentis,
                allOutcomes, locationId, ledger, ct, overall, area, part);

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
    /// to keep its focus on the object or lose interest. Losing interest turns it to another object
    /// of its own choosing among those not yet observed: one text dismisses the clicked object and
    /// observes the new one, and carries the new one's keyword. Only when nothing is left unobserved
    /// is the block a single keyword-less "I am not interested" sentence. Either way it may then
    /// choose one other object and bridge to it with a transition before observing it.
    ///
    /// <paramref name="focusOutcome"/> is the player's own choice and is observed whether or not this
    /// phase has already looked at it — the keyword that was clicked necessarily came from an earlier
    /// block, so it always has. Only the LLM-chosen follow-up draws from what is left unobserved.
    /// </summary>
    public async Task<List<NarrationBlock>> GenerateFocusObservationAsync(
        NarrativeAnchor focusOutcome,
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

        // Both focus sentences (same modus mentis) stream into one stacked preview part.
        var part = preview?.BeginAccumulatingPart(PreviewTitles.For(observationModusMentis));

        // The pool a follow-up — or a replacement — may draw from: everything this phase has not yet
        // observed, minus the clicked object's name-twins (so the persona cannot shift to an identical
        // one), with duplicates collapsed.
        var focusName = GetNeutralName(focusOutcome);
        var followUpCandidates = DeduplicateByName(
            ObserveOnlyFilter(ledger.Remaining(currentNode.GetAllDirectConcreteOutcomes()))
                .Where(o => !GetNeutralName(o).Equals(focusName, StringComparison.OrdinalIgnoreCase)));

        var allKeywords = new List<string>();
        var sentences = new List<NarrationSentence>();
        string? firstText;
        NarrativeAnchor firstObserved;

        // 0. Keep focus or lose interest? The new Modus Mentis may turn away from the clicked object.
        var keeps = await AskKeepFocusAsync(observationModusMentis, focusOutcome, focusPhrase, ct, isReminescence, overall, area);
        if (keeps)
        {
            // 1. Observe the clicked object (no transition).
            firstText = await AppendObservationAsync(sentences, allKeywords, slotId, observationModusMentis, focusOutcome, withTransition: false, locationId, ledger, ct, isReminescence: isReminescence, part: part);
            firstObserved = focusOutcome;
        }
        else
        {
            // Turning away is not a dead end while something is left to look at: the Modus Mentis
            // picks what it would rather attend to (it may not decline — it has just turned to
            // something), and the dismissal and the new observation are one neutral text rewritten
            // in one request, so the turn reads as a single movement and carries the new keyword.
            // Only with nothing left does the keyword-less "not interested" block remain.
            if (followUpCandidates.Count == 0)
                return await BuildNotInterestedBlockAsync(slotId, observationModusMentis, focusPhrase, isReminescence, ct, preview: preview, commit: commit, part: part);

            var instead = await SelectObservationObjectAsync(followUpCandidates, observationModusMentis, ledger, locationId, ct, isReminescence, overall, area, offerDecline: false);
            _observationExecutor.ResetSlot(slotId);
            if (instead == null)
                return await BuildNotInterestedBlockAsync(slotId, observationModusMentis, focusPhrase, isReminescence, ct, preview: preview, commit: commit, part: part);

            Console.WriteLine($"ObservationPhaseController: focus refused — turned from '{focusPhrase}' to '{instead.DisplayName}'.");
            firstText = await AppendObservationAsync(sentences, allKeywords, slotId, observationModusMentis, instead, withTransition: true, locationId, ledger, ct, isReminescence: isReminescence, part: part,
                leadingNeutral: NeutralNarration.ObservationTurnedAway(focusPhrase, isReminescence), turnedAwayFrom: focusOutcome);
            firstObserved = instead;
        }

        // 2. A second (and possibly third) object, reached via a transition and gated on the same length
        //    rules as the overall phase (see AppendFollowUpObservationsAsync).
        await AppendFollowUpObservationsAsync(sentences, allKeywords, slotId, observationModusMentis,
            followUpCandidates, new List<NarrativeAnchor> { firstObserved }, firstText, locationId, ledger, ct, isReminescence, overall, area, part);

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
        int slotId,
        ModusMentis modusMentis,
        NarrativeAnchor outcome,
        bool withTransition,
        int locationId,
        ObservationLedger ledger,
        CancellationToken ct,
        bool isReminescence = false,
        PreviewPart? part = null,
        string? trailingNeutral = null,
        string? leadingNeutral = null,
        NarrativeAnchor? turnedAwayFrom = null)
    {
        var sink = part?.NextSegment();
        try
        {
            // Attention + detail merged into one neutral text and rewritten in a single request
            // (two or three short styled sentences) rather than two separate calls: the attention
            // line names the object ("drawn to" / "shifts to"), the detail line gives its richer
            // description. The rewrite is forced into first person ("I ...") to anchor the PoV.
            var neutral = NeutralNarration.Observation(
                isFirst: !withTransition,
                GetNeutralPhrase(outcome, locationId),
                GetNeutralDescription(outcome, locationId),
                isReminescence: isReminescence,
                // A named NPC's appearance is a clause ("a figure hauls a barrel …"); shallow
                // wildlife's is a noun phrase ("a wiry white goat, chewing a rope").
                descriptionIsClause: outcome is Cathedral.Game.Scene.SyntheticNpcObservationObject { NpcEntity: Cathedral.Game.Npc.NpcEntity });
            // An optional trailing sentence (e.g. a threat caution) is folded into the same neutral
            // text so the persona rewrite styles it into the block rather than tacking on plain prose.
            if (!string.IsNullOrWhiteSpace(trailingNeutral))
                neutral = $"{neutral} {trailingNeutral.Trim()}";
            // An optional leading sentence (a focus just turned away from) opens the same text, so the
            // rewrite can turn from one object to the other in a single movement.
            if (!string.IsNullOrWhiteSpace(leadingNeutral))
                neutral = $"{leadingNeutral.Trim()} {neutral}";
            var text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, styleInstruction: modusMentis.StyleInstruction, preview: sink, ct: ct);

            // A turn-away opens on the refusal ("I am not interested in the reaper …"), and the model,
            // told to write one to three sentences, tends to drop that half and write only about the new
            // object — the player then reads about a hedgebank with no word that the reaper was refused.
            // The refusal comes first in the neutral text, so the rewrite's first sentence should name
            // the object turned away from; when it does not, the plain refusal is put back in front.
            if (turnedAwayFrom != null && !string.IsNullOrWhiteSpace(leadingNeutral)
                && !FirstSentenceNames(text, turnedAwayFrom))
            {
                Console.WriteLine($"ObservationPhaseController: the rewrite dropped the refusal of '{turnedAwayFrom.DisplayName}' — restoring it.");
                text = $"{leadingNeutral.Trim()} {text}";
            }

            // Keywords are chosen by rule from the final (sanitized) text — the word(s) most related
            // to the object. A long observation gets two distinct keywords, both linked to this same
            // object so either click does the same thing; a normal-length one keeps a single keyword.
            //
            // Nothing is withheld because another object of this block already used it. The anchor
            // rides on the sentence, so two sentences about two men are both clickable as "man",
            // each acting on its own. Withholding was needed only while the click resolved through a
            // table keyed by the word, which could hold one object per word.
            int wanted = text.Length > Config.Narrative.ObservationTwoKeywordsThreshold ? 2 : 1;
            List<string> kws;
            if (turnedAwayFrom == null)
                kws = KeywordExtractor.ExtractKeywords(text, GetReferenceLemma(outcome), wanted,
                                                       ownName: GetNeutralName(outcome), isPerson: IsPerson(outcome));
            else
            {
                // The text also names the object turned away from, and a keyword is wired to this
                // object whatever word it is — so no word naming the other one may become it.
                var shunned = new HashSet<string>(
                    $"{GetNeutralName(turnedAwayFrom)} {GetReferenceLemma(turnedAwayFrom)}"
                        .Split(new[] { ' ', '-', '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries),
                    StringComparer.OrdinalIgnoreCase);
                kws = KeywordExtractor.ExtractKeywords(text, GetReferenceLemma(outcome), wanted + 3,
                                                       ownName: GetNeutralName(outcome), isPerson: IsPerson(outcome))
                    .Where(k => !k.Split(' ').Any(w => shunned.Contains(w) || shunned.Contains(w.TrimEnd('s'))))
                    .Take(wanted)
                    .ToList();
            }
            sentences.Add(new NarrationSentence(text, kws, outcome));
            ledger.Observe(outcome);
            foreach (var kw in kws)
                allKeywords.Add(kw);
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
    /// The arrival guarantee: when the block that opens a visit carries no keyword leading to a living
    /// person while one stands unobserved in view, one more observation is appended — same modus
    /// mentis, reached by a transition — whose choice is narrowed to the people and may not decline.
    /// Which person is still the modus mentis's choice.
    ///
    /// <para>A city square offers two dozen doors, walls and ways against three people, and a modus
    /// mentis with no interest in people never reached them: a whole visit could pass without the
    /// narration noticing that anybody was there. The persona's first choices are left alone — the
    /// person is added after them, not put in their place.</para>
    /// </summary>
    private async Task AppendPersonObservationIfMissingAsync(
        List<NarrationSentence> sentences,
        List<string> allKeywords,
        int slotId,
        ModusMentis modusMentis,
        List<NarrativeAnchor> allOutcomes,
        int locationId,
        ObservationLedger ledger,
        CancellationToken ct,
        string? overall,
        string? area,
        PreviewPart? part)
    {
        bool hasPerson = sentences.Any(s => Enumerable.Range(0, s.Keywords.Count)
            .Any(i => Rules.Choice.SpeakingPriority.IsPerson(s.AnchorOf(i))));
        if (hasPerson) return;

        var people = DeduplicateByName(ObserveOnlyFilter(ledger.Remaining(allOutcomes))
            .Where(Rules.Choice.SpeakingPriority.IsPerson));
        if (people.Count == 0) return;

        Console.WriteLine($"ObservationPhaseController: the arrival block names nobody — "
                        + $"adding an observation of one of the {people.Count} person(s) in view.");
        var person = await SelectObservationObjectAsync(people, modusMentis, ledger, locationId, ct,
            isReminescence: false, overall, area, offerDecline: false);
        _observationExecutor.ResetSlot(slotId);
        if (person == null) return;

        bool afterObservation = sentences.Any(s => s.Anchor != null);
        await AppendObservationAsync(sentences, allKeywords, slotId, modusMentis, person,
            withTransition: afterObservation, locationId, ledger, ct, part: part);
    }

    /// <summary>A named NPC — named by name, role or sex rather than by a head noun.</summary>
    private static bool IsPerson(NarrativeAnchor outcome)
        => outcome is Cathedral.Game.Scene.SyntheticNpcObservationObject { NpcEntity: Cathedral.Game.Npc.NpcEntity };

    /// <summary>
    /// Whether the first sentence of <paramref name="text"/> names <paramref name="anchor"/> — by its
    /// reference lemma or the head of its neutral name, singular or plural. Used to tell a
    /// rewrite that kept a turn-away's refusal from one that dropped it.
    /// </summary>
    private bool FirstSentenceNames(string text, NarrativeAnchor anchor)
    {
        var first = System.Text.RegularExpressions.Regex.Split(text.Trim(), @"(?<=[.!?])\s+")[0];
        // The head of the name only: "the reaper of the radish field (a man)" is named by "reaper",
        // and "field" in the first sentence may well be about something else.
        var name = GetNeutralName(anchor);
        int cut = name.IndexOfAny(new[] { '(' });
        if (cut >= 0) name = name[..cut];
        int of = name.IndexOf(" of ", StringComparison.OrdinalIgnoreCase);
        if (of >= 0) name = name[..of];
        var words = $"{name} {GetReferenceLemma(anchor)}"
            .Split(new[] { ' ', '-', '(', ')', ',', '–' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length > 2 && w is not ("the" or "and" or "man" or "woman" or "boy" or "girl"))
            .Distinct();
        return words.Any(w => System.Text.RegularExpressions.Regex.IsMatch(
            first, $@"\b{System.Text.RegularExpressions.Regex.Escape(w)}(s|es)?\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Appends the area introduction that opens a block when the area or the time of day has changed:
    /// where I am and when, what the place is, and what is around me
    /// (<see cref="NeutralNarration.AreaIntroduction"/>) — from the area's <c>ContextDescription</c>,
    /// one of its <c>Descriptions</c>, and the short phrases of up to
    /// <see cref="IntroductionSightsMax"/> objects drawn from <paramref name="pool"/> in random order —
    /// rewritten in the persona's voice.
    ///
    /// <para>Every object of <paramref name="pool"/> the rewritten text names gets a keyword of its own,
    /// linked to it (<see cref="IntroductionKeywordLinker"/>); the list of sights is what makes that
    /// likely. A linked object counts as attended to: it goes on <paramref name="ledger"/> and is
    /// returned so the caller drops it from the block's pool, exactly as an observed object would be.
    /// One object is always left unlinked, for the mandatory observation that follows.</para>
    ///
    /// Returns the text (null when the rewrite failed — the block then proceeds as if no introduction
    /// had been asked for) and the objects keywords were linked to.
    /// </summary>
    private async Task<(string? Text, List<NarrativeAnchor> Anchors)> AppendAreaIntroductionAsync(
        List<NarrationSentence> sentences,
        List<string> allKeywords,
        int slotId,
        ModusMentis modusMentis,
        Cathedral.Game.Scene.Area introArea,
        TimePeriod period,
        List<NarrativeAnchor> pool,
        ObservationLedger ledger,
        int locationId,
        CancellationToken ct,
        PreviewPart? part)
    {
        var sink = part?.NextSegment();
        try
        {
            string? description = null;
            if (introArea.Descriptions.Count > 0)
            {
                var rng = Cathedral.Game.Scene.SceneViewAdapter.DescriptionRng(introArea, locationId);
                description = introArea.Descriptions[rng.Next(introArea.Descriptions.Count)];
            }

            var sights = pool.OrderBy(_ => _random.Next())
                             .Take(IntroductionSightsMax)
                             .Select(o => GetNeutralPhrase(o, locationId))
                             .ToList();

            var neutral = NeutralNarration.AreaIntroduction(introArea.ContextDescription, period.Phrase(), description, sights);
            var text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, styleInstruction: modusMentis.StyleInstruction, preview: sink, ct: ct);

            var links = LinkIntroductionKeywords(text, introArea.ContextDescription, pool);
            var kws = links.Select(l => l.Keyword).ToList();
            var anchors = links.Select(l => l.Anchor).ToList();
            sentences.Add(new NarrationSentence(text, kws, anchors.FirstOrDefault(), anchors));
            allKeywords.AddRange(kws);
            foreach (var a in anchors) ledger.Observe(a);
            Console.WriteLine($"ObservationPhaseController: Area introduction of '{introArea.DisplayName}' " +
                              (links.Count > 0
                                  ? $"(keywords: {string.Join(", ", links.Select(l => $"'{l.Keyword}' → '{l.Anchor.DisplayName}'"))})"
                                  : "(no keyword)"));
            return (text, anchors);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: Area introduction failed: {ex.Message}");
            return (null, new List<NarrativeAnchor>());
        }
    }

    /// <summary>How many objects the area introduction lists as being around me.</summary>
    private const int IntroductionSightsMax = 5;

    /// <summary>
    /// The keywords of an area introduction, each linked to the object of <paramref name="pool"/> it
    /// names (<see cref="IntroductionKeywordLinker"/>).
    ///
    /// <para>One object is always left unlinked: a linked object is retired from the block, and the
    /// mandatory observation that follows needs something to look at. So no link is made when the pool
    /// holds a single object, and when every object was named the last link in reading order is
    /// dropped.</para>
    /// </summary>
    private static List<(string Keyword, NarrativeAnchor Anchor)> LinkIntroductionKeywords(
        string text, string whereClause, List<NarrativeAnchor> pool)
    {
        if (pool.Count < 2) return new();
        var links = IntroductionKeywordLinker.Link(text, whereClause, pool, NounExtractor.ExtractKeywordCandidates(text));
        if (links.Count >= pool.Count) links.RemoveAt(links.Count - 1);
        return links;
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
    ///
    /// <para>With <paramref name="introText"/> (an area introduction opened the block) the
    /// introduction takes the first slot: the skip rule and the second observation are dropped, and
    /// only the conditional last observation remains, gated on the introduction plus the first.</para>
    ///
    /// <para><paramref name="quietDecline"/> drops the in-voice refusal line: the caller is about to
    /// add an observation of its own (the arrival's person), which the line would contradict.</para>
    /// </summary>
    private async Task AppendFollowUpObservationsAsync(
        List<NarrationSentence> sentences,
        List<string> allKeywords,
        int slotId,
        ModusMentis modusMentis,
        List<NarrativeAnchor> candidates,
        List<NarrativeAnchor> observed,
        string? firstText,
        int locationId,
        ObservationLedger ledger,
        CancellationToken ct,
        bool isReminescence,
        string? overall,
        string? area,
        PreviewPart? part,
        string? introText = null,
        bool quietDecline = false)
    {
        // An area introduction counts as the first of the three slots: the observation already made
        // is the second, and the only follow-up left is the conditional last one, gated on the
        // introduction and that observation together.
        bool introduced = introText != null;

        // A first observation long enough on its own stops the phase here.
        if (!introduced && (firstText?.Length ?? 0) > Config.Narrative.ObservationSkipSecondThreshold)
            return;

        // Second observation over the still-unobserved objects, reached via a transition.
        string? secondText = introText;
        var remaining = candidates.Where(c => !observed.Contains(c)).ToList();
        if (!introduced && remaining.Count > 0)
        {
            bool declineOffered = !ledger.IsEmpty;
            var second = await SelectObservationObjectAsync(remaining, modusMentis, ledger, locationId, ct, isReminescence, overall, area);
            // A choice used to be asked on this slot and wiped its history; the rewrite that follows
            // keeps history, so the slot is still cleared here to keep the block's context bounded.
            _observationExecutor.ResetSlot(slotId);
            if (second == null)
            {
                // Refused: close the block in voice and stop — nothing more here is worth attending to.
                if (declineOffered && !quietDecline)
                    await AppendNothingObservationAsync(sentences, slotId, modusMentis, isReminescence, ct, part: part, nothingMore: true);
                return;
            }

            secondText = await AppendObservationAsync(sentences, allKeywords, slotId, modusMentis, second, withTransition: true, locationId, ledger, ct, isReminescence: isReminescence, part: part);
            observed.Add(second);
        }

        // Third observation when the first two stayed short and enough objects remain to be observed.
        int combinedLength = (firstText?.Length ?? 0) + (secondText?.Length ?? 0);
        var stillRemaining = candidates.Where(c => !observed.Contains(c)).ToList();
        if (combinedLength < Config.Narrative.ObservationTriggerThirdThreshold
            && stillRemaining.Count > Config.Narrative.ObservationThirdMinRemaining)
        {
            bool declineOffered = !ledger.IsEmpty;
            var third = await SelectObservationObjectAsync(stillRemaining, modusMentis, ledger, locationId, ct, isReminescence, overall, area);
            // A choice used to be asked on this slot and wiped its history; the rewrite that follows
            // keeps history, so the slot is still cleared here to keep the block's context bounded.
            _observationExecutor.ResetSlot(slotId);
            if (third == null)
            {
                if (declineOffered && !quietDecline)
                    await AppendNothingObservationAsync(sentences, slotId, modusMentis, isReminescence, ct, part: part, nothingMore: true);
                return;
            }

            await AppendObservationAsync(sentences, allKeywords, slotId, modusMentis, third, withTransition: true, locationId, ledger, ct, isReminescence: isReminescence, part: part);
        }
    }

    /// <summary>
    /// Picks the modus mentis that narrates the first observation of a phase: a random one drawn — by
    /// priority — from the observation modi mentis currently held in the active member's sensory memory,
    /// falling back to any observation modus mentis when none of them sits in sensory memory. Returns
    /// null only when the member has no observation modus mentis at all.
    ///
    /// <para><b>Modi mentis wounds have broken are passed over while any usable one remains</b>, and
    /// that is not the silent filtering <see cref="BrokenModusMentis"/> argues against. Nobody chose
    /// this one — the phase opens on its own, and the player is given no popup to be told anything
    /// through, so opening on a refusal would spend a noetic point and a whole phase on a choice they
    /// never made. Where the player <i>does</i> choose an observation modus mentis (the focus
    /// observation) the broken ones are still offered and still refuse, which is where the account of
    /// the injury belongs.</para>
    ///
    /// <para>When <em>every</em> observation modus mentis is broken one is returned anyway rather than
    /// null: the body genuinely cannot perceive, and <see cref="ExecuteObservationPhaseAsync"/> opens
    /// the phase on that refusal. Null is kept for its original meaning — no observation modus mentis
    /// at all — which the caller treats as a fault.</para>
    /// </summary>
    private ModusMentis? PickFirstObservationModusMentis(PartyMember member)
    {
        var observation = member.GetObservationModiMentis();
        if (observation.Count == 0) return null;

        var usable = observation.Where(mm => !member.IsModusMentisBroken(mm)).ToList();
        if (usable.Count > 0) observation = usable;

        var inSensory = member.GetMemoryModule(MemoryModuleType.Sensory)?.FilledModiMentis.ToHashSet()
                        ?? new HashSet<ModusMentis>();
        var preferred = observation.Where(mm => inSensory.Contains(mm)).ToList();
        var pool = preferred.Count > 0 ? preferred : observation;
        return pool[_random.Next(pool.Count)];
    }

    /// <summary>
    /// The one block a phase opens with when the body cannot perceive at all — every observation
    /// modus mentis broken by wounds. It names the organs and the wounds responsible, in the voice of
    /// the modus mentis that could not do its office, and carries no keyword: there is nothing to
    /// click, and LEAVE is the way out.
    /// </summary>
    private async Task<List<NarrationBlock>> BrokenObservationBlockAsync(
        ModusMentis modusMentis, PartyMember actingMember, int slotId,
        LlmPreviewSession? preview, Action<List<NarrationBlock>>? commit, CancellationToken ct)
    {
        var part = preview?.BeginPart(PreviewTitles.For(modusMentis));
        string neutral = BrokenModusMentis.NeutralFor(
            actingMember, modusMentis, NeutralNarration.BrokenFaculty.Observation);

        string text;
        try
        {
            text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation,
                modusMentis.PersonaReminder2, keepHistory: true,
                styleInstruction: modusMentis.StyleInstruction, preview: part?.NextSegment(), ct: ct);
            if (string.IsNullOrWhiteSpace(text)) text = neutral;
        }
        catch (Exception ex)
        {
            // A refusal must never be the thing that takes the phase down — the body being ruined is
            // already the news, and a failed rewrite would replace it with a lost visit.
            Console.Error.WriteLine(
                $"ObservationPhaseController: broken-observation rewrite failed: {ex.Message}");
            text = neutral;
        }

        var blocks = new List<NarrationBlock>
        {
            new(Type: NarrationBlockType.Observation,
                ModusMentis: modusMentis,
                Text: text,
                Keywords: new List<string>(),
                Actions: null)
        };

        if (part != null)
        {
            part.AttachCommit(() => commit?.Invoke(blocks));
            part.MarkComplete();
            preview?.EndProduction();
        }
        else commit?.Invoke(blocks);

        return blocks;
    }

    /// <summary>
    /// First observation of a narration phase whose subject was decided for the player: the person a
    /// dialogue was just held with (kept continuous with that conversation), the one an introduction
    /// presented, or the one a Meet routine is about. The narrator is
    /// <paramref name="originObservationModusMentis"/> — for a dialogue, the observation MM that
    /// originated its chain of thought — when given and still learned, otherwise a resampled one
    /// (sensory memory first, like any first observation). The observed object is
    /// <paramref name="npcOutcome"/>, with no "what draws you?" selection, and it is a single observation
    /// regardless of length (no second or third). Follows the same rewrite/keyword path as any
    /// observation, so a long text still gets two keywords. Returns an empty list (so the caller can
    /// fall back to the normal phase) when no observation MM is available.
    /// </summary>
    public async Task<List<NarrationBlock>> GenerateFocusedObservationAsync(
        NarrativeAnchor npcOutcome,
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
            Console.WriteLine("ObservationPhaseController: No observation modus mentis for focused observation.");
            return new List<NarrationBlock>();
        }

        Console.WriteLine($"ObservationPhaseController: Focused observation of '{npcOutcome.DisplayName}' with {observationModusMentis.DisplayName}");

        (npcOutcome as INpcContextLabelStampable)?.StampContextLabel(actingMember, _worldContext, locationId);

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(observationModusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var sentences = new List<NarrationSentence>();

        var part = preview?.BeginAccumulatingPart(PreviewTitles.For(observationModusMentis));

        // Exactly one observation, of the focus, whatever its length — no follow-up choice. It still goes
        // on the ledger, so a later request this phase reaches for something other than what the phase
        // opened on.
        await AppendObservationAsync(sentences, allKeywords, slotId, observationModusMentis, npcOutcome, withTransition: false, locationId, ledger, ct, isReminescence: false, part: part);

        if (sentences.Count == 0)
        {
            preview?.Reset();
            Console.WriteLine("ObservationPhaseController: Focused observation produced nothing.");
            return new List<NarrationBlock>();
        }

        var block = new NarrationBlock(
            Type: NarrationBlockType.Observation,
            ModusMentis: observationModusMentis,
            Text: string.Join(" ", sentences.Select(s => s.Text)),
            Keywords: allKeywords,
            Actions: null,
            SourceObservationType: ObservationType.Focus,
            Sentences: sentences);

        Console.WriteLine($"ObservationPhaseController: Focused observation complete ({allKeywords.Count} keywords)");
        var resultBlocks = new List<NarrationBlock> { block };
        FinalizePreview(preview, commit, resultBlocks, part);
        return resultBlocks;
    }

    /// <summary>
    /// Opens a narration phase on the bodies an action just made: every corpse in
    /// <paramref name="corpseOutcomes"/> is observed, in the order they fell, and nothing else is.
    /// Like the post-dialogue opener the objects are imposed — there is no "what draws you?" selection
    /// and no length gate — because looking away from something you have just killed to notice the
    /// weather is not a thing a person does. The first is observed plainly; each further one is
    /// reached by a transition, so a won fight against three reads as one sweep across three bodies.
    ///
    /// <para>The narrator is freshly sampled (sensory memory first, like any first observation): a kill
    /// through a verb has an originating chain, but a kill through a fight has none, and the two should
    /// not open differently. Every corpse goes on the ledger, so the rest of the phase looks elsewhere.
    /// Returns an empty list — so the caller falls back to the normal phase — when there is no
    /// observation MM or nothing was produced.</para>
    /// </summary>
    public async Task<List<NarrationBlock>> GenerateCorpseObservationAsync(
        IReadOnlyList<NarrativeAnchor> corpseOutcomes,
        int locationId,
        PartyMember actingMember,
        ObservationLedger? ledger = null,
        LlmPreviewSession? preview = null,
        Action<List<NarrationBlock>>? commit = null,
        CancellationToken ct = default)
    {
        if (corpseOutcomes.Count == 0) return new List<NarrationBlock>();

        ledger ??= new ObservationLedger();
        var observationModusMentis = PickFirstObservationModusMentis(actingMember);
        if (observationModusMentis == null)
        {
            Console.WriteLine("ObservationPhaseController: No observation modus mentis for corpse observation.");
            return new List<NarrationBlock>();
        }

        Console.WriteLine($"ObservationPhaseController: Corpse observation of {corpseOutcomes.Count} " +
                          $"body(ies) with {observationModusMentis.DisplayName}");

        var slotId = await _observationExecutor.GetOrCreateSlotForModusMentisPublicAsync(observationModusMentis);
        _observationExecutor.ResetSlot(slotId);

        var allKeywords = new List<string>();
        var sentences = new List<NarrationSentence>();

        var part = preview?.BeginAccumulatingPart(PreviewTitles.For(observationModusMentis));

        for (int i = 0; i < corpseOutcomes.Count; i++)
        {
            await AppendObservationAsync(sentences, allKeywords, slotId,
                observationModusMentis, corpseOutcomes[i], withTransition: i > 0, locationId, ledger, ct,
                isReminescence: false, part: part);
        }

        if (sentences.Count == 0)
        {
            preview?.Reset();
            Console.WriteLine("ObservationPhaseController: Corpse observation produced nothing.");
            return new List<NarrationBlock>();
        }

        var block = new NarrationBlock(
            Type: NarrationBlockType.Observation,
            ModusMentis: observationModusMentis,
            Text: string.Join(" ", sentences.Select(s => s.Text)),
            Keywords: allKeywords,
            Actions: null,
            SourceObservationType: ObservationType.Overall,
            Sentences: sentences);

        Console.WriteLine($"ObservationPhaseController: Corpse observation complete ({sentences.Count} sentences, {allKeywords.Count} keywords)");
        var resultBlocks = new List<NarrationBlock> { block };
        FinalizePreview(preview, commit, resultBlocks, part);
        return resultBlocks;
    }

    /// <summary>
    /// Opens a narration phase with a single, forced observation of a same-area enemy — the "under
    /// threat" lead-in used when a visual threat is present (the same condition that turns the exit
    /// button into RUNAWAY). Like the post-dialogue opener, the observed object is imposed
    /// (<paramref name="threatOutcome"/>, no "what draws you?" selection) and it is one observation
    /// (an earshot arrival passes its own <paramref name="trailingNeutral"/>: a witness is no threat)
    /// regardless of length (no second or third). Unlike it, the narrator is freshly sampled (sensory
    /// memory first, like any first observation) and a threat-caution sentence is folded into the
    /// neutral text so the persona rewrites a note of danger into the block. Returns an empty list
    /// (so the caller can fall back to the normal phase) when no observation MM is available or the
    /// rewrite produced nothing.
    /// </summary>
    public async Task<List<NarrationBlock>> GenerateThreatObservationAsync(
        NarrativeAnchor threatOutcome,
        int locationId,
        PartyMember actingMember,
        ObservationLedger? ledger = null,
        LlmPreviewSession? preview = null,
        Action<List<NarrationBlock>>? commit = null,
        CancellationToken ct = default,
        string? trailingNeutral = null)
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
        var sentences = new List<NarrationSentence>();

        var part = preview?.BeginAccumulatingPart(PreviewTitles.For(observationModusMentis));

        // Exactly one observation, of the enemy, whatever its length — no follow-up choice. The
        // threat-caution sentence rides into the same rewrite as the observation itself.
        await AppendObservationAsync(sentences, allKeywords, slotId, observationModusMentis,
            threatOutcome, withTransition: false, locationId, ledger, ct, isReminescence: false,
            part: part, trailingNeutral: trailingNeutral ?? NeutralNarration.ThreatCaution());

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
            Sentences: sentences);

        Console.WriteLine($"ObservationPhaseController: Threat observation complete ({allKeywords.Count} keywords)");
        var resultBlocks = new List<NarrationBlock> { block };
        FinalizePreview(preview, commit, resultBlocks, part);
        return resultBlocks;
    }

    /// <summary>
    /// Narrows a candidate pool to what <c>--observe-only</c> names. Whole words are matched first —
    /// "pig" means the pig, not the Courtyard–Pigsty Track that merely contains those letters — and a
    /// loose substring pass runs only when no word matched, so a partial name still works when it is
    /// unambiguous. Returns the pool untouched at the flag's default, and again when nothing in it
    /// matches at all: a phase where the named object is absent narrates normally rather than going
    /// blank.
    /// </summary>
    private static IEnumerable<NarrativeAnchor> ObserveOnlyFilter(IEnumerable<NarrativeAnchor> outcomes)
    {
        var wanted = Config.Debug.ObserveOnly;
        if (string.IsNullOrWhiteSpace(wanted)) return outcomes;

        var pool = outcomes.ToList();

        // Exact name first. Objects and the paths between them share words — a Boulder Field holds a
        // Boulder and is joined by a "Boulder Field–Scree Track" — so a whole-word match on "Boulder"
        // keeps both, and the persona is as likely to look at the track. Which is how a gather test
        // came to follow a path instead. An exact name is unambiguous, so it wins outright.
        var byExact = pool.Where(o => TargetableNames(o)
            .Any(n => n.Equals(wanted, StringComparison.OrdinalIgnoreCase))).ToList();
        if (byExact.Count > 0) return byExact;

        var byWord = pool.Where(o => TargetableNames(o).Any(n => Regex.IsMatch(
            n, $@"\b{Regex.Escape(wanted)}\b", RegexOptions.IgnoreCase))).ToList();
        if (byWord.Count > 0) return byWord;

        var bySubstring = pool
            .Where(o => TargetableNames(o).Any(n => n.Contains(wanted, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (bySubstring.Count > 0) return bySubstring;

        Cathedral.Game.DebugFlagAudit.Miss("--observe-only", wanted, "the whole scene");
        return pool;
    }

    /// <summary>
    /// Collapses outcomes that share the same display name (e.g. several identical "Birch Tree"
    /// objects) down to a single, randomly-chosen representative per name. This keeps the
    /// observation-choice list free of duplicates, and — because both the first and second choice
    /// draw from the same deduplicated set — guarantees a chosen object's duplicates are never
    /// re-proposed for the second observation. Group order follows first appearance.
    /// </summary>
    private List<NarrativeAnchor> DeduplicateByName(IEnumerable<NarrativeAnchor> outcomes)
        => outcomes
            .GroupBy(GetNeutralName, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var members = g.ToList();
                return members[_random.Next(members.Count)];
            })
            .ToList();

    /// <summary>
    /// The <see cref="ChoiceJudge"/> picks the object this Modus Mentis attends to — or the decline
    /// option, in which case this returns <c>null</c> and the caller renders the refusal line. Returns
    /// a random object in playground mode (no LLM).
    ///
    /// <para><paramref name="candidates"/> arrives already stripped of everything this narration phase
    /// has observed, so the list shrinks request by request. The refusal option — "let your focus go" —
    /// is offered exactly when <paramref name="ledger"/> is non-empty: with the pool narrowing, a
    /// persona forced to pick from two leftovers it cares nothing for would attend to them out of
    /// character, and it needs a way to say so. Withholding it while the ledger is empty is what keeps
    /// the request that opens a phase from producing a block with no keyword to click.</para>
    ///
    /// <para><paramref name="offerDecline"/> false withholds it regardless: the observation after an
    /// area introduction is mandatory even when the introduction retired an object.</para>
    ///
    /// <para>Never offered while <see cref="MustCommit"/> holds (the reminescence and the get-up
    /// phase): the judge took "nothing more is surfacing" as the natural answer in a memory, and a
    /// fragment stayed unreachable while it waited.</para>
    /// </summary>
    private async Task<NarrativeAnchor?> SelectObservationObjectAsync(
        List<NarrativeAnchor> candidates,
        ModusMentis modusMentis,
        ObservationLedger ledger,
        int locationId,
        CancellationToken ct,
        bool isReminescence = false,
        string? overallLocation = null,
        string? areaLocation = null,
        bool offerDecline = true)
    {
        if (candidates.Count == 0) return null;

        // A speaking modus mentis attends to a living person before anything else in view
        // (SpeakingPriority). Only the order changes: the ledger retires whoever it looked at, so the
        // rest of the scene comes back once the people are seen.
        if (!isReminescence && Rules.Choice.SpeakingPriority.Speaks(modusMentis))
        {
            var people = candidates.Where(Rules.Choice.SpeakingPriority.IsPerson).ToList();
            if (people.Count > 0 && people.Count < candidates.Count)
            {
                Console.WriteLine($"ObservationPhaseController: [{modusMentis.DisplayName}] speaks — "
                                + $"attending only to the {people.Count} person(s) in view.");
                candidates = people;
            }
        }

        // Each object is offered as the act of attending to it — "focus on the plowman of the field
        // (a woman)" — via GetNeutralPhrase (proper names stay verbatim; common-noun objects gain
        // "a"/"an"). Playground mode never declines (the selector short-circuits to a real option).
        string? decline = ledger.IsEmpty || !offerDecline || MustCommit()
            ? null
            : isReminescence ? "let the recollection go, nothing more is surfacing"
                             : "let your focus go, nothing else here is worth attending to";
        return await _selector.SelectAsync(
            ChoiceKind.Focus, modusMentis, candidates,
            c => $"focus on {GetNeutralPhrase(c, locationId)}",
            ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, null),
            declineOption: decline, ct: ct);
    }

    /// <summary>
    /// Asks the <see cref="ChoiceJudge"/> whether the (new) focus Modus Mentis keeps its focus on the
    /// handed object or loses interest — put as whether the object holds anything for it, yes or no,
    /// which the judge answers far more evenly than "keep it or turn elsewhere". "No" is the decline. Playground
    /// mode never declines (the selector picks the only real option), and no modus mentis declines
    /// while <see cref="MustCommit"/> holds: a fragment or facet the player reached for is the phase's
    /// only way forward, since REMEMBER or GET UP is the sole action there.
    /// </summary>
    private async Task<bool> AskKeepFocusAsync(
        ModusMentis modusMentis,
        NarrativeAnchor focusOutcome,
        string focusPhrase,
        CancellationToken ct,
        bool isReminescence,
        string? overallLocation,
        string? areaLocation)
    {
        if (MustCommit()) return true;
        if (Config.Debug.RefuseFocus) return false;
        // A speaking modus mentis never turns away from a person (SpeakingPriority).
        if (!isReminescence && Rules.Choice.SpeakingPriority.Speaks(modusMentis)
            && Rules.Choice.SpeakingPriority.IsPerson(focusOutcome))
        {
            Console.WriteLine($"ObservationPhaseController: [{modusMentis.DisplayName}] speaks — keeps its focus on a person.");
            return true;
        }

        var kept = await _selector.SelectAsync(
            ChoiceKind.KeepFocus, modusMentis, new[] { focusOutcome },
            _ => "yes - it touches what this faculty knows, does or cares about",
            ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, focusPhrase),
            declineOption: "no - it is nothing to this faculty", ct: ct,
            question: $"Does {focusPhrase} hold anything for this faculty?");
        return kept != null;
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
        LlmPreviewSession? preview = null,
        Action<List<NarrationBlock>>? commit = null,
        PreviewPart? part = null)
    {
        var neutral = NeutralNarration.ObservationNotInterested(focusPhrase, isReminescence);
        string text;
        try
        {
            text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, styleInstruction: modusMentis.StyleInstruction, preview: part?.NextSegment(), ct: ct);
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
        PreviewPart? part = null,
        bool nothingMore = false)
    {
        var sink = part?.NextSegment();
        try
        {
            var neutral = nothingMore
                ? NeutralNarration.ObservationNothingMore(isReminescence)
                : NeutralNarration.ObservationNothing(isReminescence);
            var text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Observation, modusMentis.PersonaReminder2, keepHistory: true, styleInstruction: modusMentis.StyleInstruction, preview: sink, ct: ct);
            sentences.Add(new NarrationSentence(text, new List<string>()));
            Console.WriteLine("ObservationPhaseController: nothing drew the persona's attention (declined).");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: 'nothing' observation failed: {ex.Message}");
        }
    }

    /// <summary>Short name of an outcome, used for the observation-choice enum.</summary>
    /// <summary>
    /// Every name <c>--observe-only</c> may be matched against: the neutral name the narration uses,
    /// and the observation id.
    ///
    /// <para>The id is what makes an NPC targetable at all. An NPC's <c>NeutralName</c> is its stamped
    /// <i>contextual label</i> — "a stranger", "my acquaintance Aldric" — because the real name is
    /// deliberately kept away from the LLM. So a script asking for a person by name matched nothing,
    /// fell back to the whole scene, and tested whatever the persona happened to look at instead:
    /// every NPC verb (stalk, provoke, pickpocket, meet_stranger, attack …) was untestable, and
    /// silently so. <c>ObservationId</c> is derived from the real display name, so matching it too
    /// costs nothing and makes them reachable.</para>
    /// </summary>
    private static IEnumerable<string> TargetableNames(NarrativeAnchor outcome)
    {
        yield return GetNeutralName(outcome);
        if (outcome is ObservationObject obs)
        {
            yield return obs.ObservationId.Replace('_', ' ');
            foreach (var alias in obs.TargetingAliases) yield return alias;
        }
    }

    private static string GetNeutralName(NarrativeAnchor outcome)
        => outcome is ObservationObject obs ? obs.NeutralName
         : outcome.DisplayName;

    /// <summary>The outcome's core noun, used as the keyword-similarity anchor.</summary>
    private static string GetReferenceLemma(NarrativeAnchor outcome)
        => outcome is ObservationObject obs ? obs.ReferenceLemma
         : outcome.DisplayName;

    /// <summary>Articled noun phrase of an outcome, used to fill the transition sentence template.</summary>
    private static string GetNeutralPhrase(NarrativeAnchor outcome, int locationId)
        => outcome is ObservationObject obs ? obs.NeutralPhrase
         : GetNeutralDescription(outcome, locationId);

    /// <summary>
    /// Returns a rich noun-phrase description of an outcome for the observation sentence.
    /// </summary>
    private static string GetNeutralDescription(NarrativeAnchor outcome, int locationId)
        => outcome is ObservationObject obs ? obs.GenerateNeutralDescription(locationId)
         : outcome.DisplayName;

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
        NarrativeAnchor linkedOutcome,
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
            string? kw = KeywordExtractor
                .ExtractKeywords(spoken, GetReferenceLemma(linkedOutcome), KeywordCandidatesForSpeaking)
                .FirstOrDefault(k => !partyWords.Contains(k));
            if (kw != null)
                allExtractedKeywords.Add(kw);
            else
                Console.Error.WriteLine("ObservationPhaseController: Speaking line yielded no keyword — the companion will have nothing to click.");

            // One sentence covering the whole address; the renderer highlights only where the keyword
            // appears within it.
            var speakingSentences = new List<NarrationSentence> { new NarrationSentence(spoken, allExtractedKeywords, linkedOutcome) };

            var block = new NarrationBlock(
                Type: NarrationBlockType.Speaking,
                ModusMentis: speakingModusMentis,
                Text: spokenText,
                Keywords: allExtractedKeywords.Count > 0 ? allExtractedKeywords : null,
                Actions: null,
                ChainOrigin: null,
                LinkedOutcome: linkedOutcome,
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
