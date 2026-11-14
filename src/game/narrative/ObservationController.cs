using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.LLM;
using Cathedral.Game.Npc;
using Cathedral.Game.Narrative.Preview;
using Cathedral.Game.Narrative.Rules.Choice;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Narrative;

/// <summary>
/// What an observation came to: the element the mind attended to and the text it wrote of it, or —
/// <paramref name="Observed"/> null — the text of its declining.
/// </summary>
public sealed record ObservationResult(NarrativeAnchor? Observed, NarrationBlock Block);

/// <summary>
/// Observation: one mind, one element, one text.
///
/// <para>The player chooses <i>what kind</i> of thing to look at by the symbol clicked on the map; the
/// observing modus mentis chooses <i>which</i> — the <see cref="ChoiceJudge"/> picks, among the
/// elements of that kind not yet observed, the one that draws this mind — and the text is the neutral
/// account of that one element (<see cref="NeutralNarration.Observation"/>) re-expressed in the mind's
/// voice by the <see cref="PersonaRewriter"/>. A click on an element already known (remembered from an
/// earlier visit, or disclosed by the container that holds it) names the element itself, and the mind
/// is asked only whether it holds anything for it.</para>
///
/// <para><b>A mind may decline once.</b> Offered a kind, it may find nothing of that kind worth
/// attending to; handed a named element, it may find nothing in it. Either is a text of its own and
/// observes nothing. Whether it may is the caller's to say (<c>mayDecline</c>): the caller remembers
/// who declined what, and the same mind asked again must attend. A speaking mind never declines a
/// person (<see cref="SpeakingPriority"/>), and nothing declines in the two opening phases, whose
/// fragments and facets are the only way onward.</para>
///
/// <para><b>A will goes its own way</b> (<see cref="WillPriority"/>): it attends to the element that
/// begins its course, says why, and otherwise says what it wants instead. That is a rule of its
/// nature, not the declining above, and is never spent.</para>
/// </summary>
public sealed class ObservationController
{
    private readonly ObservationExecutor _slots;
    private readonly PersonaRewriter _rewriter;
    private readonly PersonaChoiceSelector _selector;
    private readonly WorldContext? _worldContext;
    private readonly Random _random = GameRng.Stream("observation-phase");

    /// <summary>
    /// True while every choice must commit to something — the childhood reminescence and the get-up
    /// phase. Read from the narrative controller's scene, which this controller does not hold.
    /// </summary>
    public Func<bool> MustCommit { get; set; } = () => false;

    public ObservationController(LlamaServerManager llamaServer, ModusMentisSlotManager slotManager,
                                 WorldContext? worldContext = null)
    {
        _slots        = new ObservationExecutor(llamaServer, slotManager);
        _rewriter     = new PersonaRewriter(llamaServer);
        _selector     = new PersonaChoiceSelector(llamaServer);
        _worldContext = worldContext;
    }

    /// <summary>
    /// <paramref name="mind"/> observes one element of <paramref name="pool"/>, or declines.
    /// </summary>
    /// <param name="pool">The elements on offer: those of one kind, in one area, not yet observed —
    /// or, when <paramref name="named"/>, the single element the click named.</param>
    /// <param name="at">The scene and the point of view the element is seen from.</param>
    /// <param name="mayDecline">Whether this mind may still decline this (see the class remarks).</param>
    /// <param name="commit">Runs when the player presses CONTINUE on the text.</param>
    /// <returns>Null when no text could be written at all; nothing is then committed.</returns>
    public async Task<ObservationResult?> ObserveAsync(
        IReadOnlyList<NarrativeAnchor> pool,
        bool named,
        ModusMentis mind,
        PartyMember member,
        NarrationNode node,
        (Scene.Scene Scene, PoV PoV) at,
        int locationId,
        bool mayDecline,
        LlmPreviewSession? preview = null,
        Action<ObservationResult>? commit = null,
        CancellationToken ct = default)
    {
        if (pool.Count == 0) throw new ArgumentException("nothing to observe", nameof(pool));
        bool reminescence = at.Scene.Phase == NarrationPhase.ChildhoodReminescence;

        // Stamp the contextual NPC label (relation + role + location) for this narrator's point of
        // view before any prompt text is built. Non-NPC and shallow elements no-op.
        foreach (var o in pool)
            (o as INpcContextLabelStampable)?.StampContextLabel(member, _worldContext, locationId);

        int slot = await _slots.GetOrCreateSlotForModusMentisPublicAsync(mind);
        _slots.ResetSlot(slot);
        var part = preview?.BeginAccumulatingPart(PreviewTitles.For(mind));

        var (chosen, trailing, refusal) = await ChooseAsync(pool, named, mind, member, node, at, locationId, mayDecline, reminescence, ct);
        // A choice may have been asked on this slot; the rewrite that follows starts from a clear one.
        _slots.ResetSlot(slot);

        string? text = chosen != null
            ? await DescribeAsync(slot, mind, chosen, at, locationId, reminescence, trailing, part, ct)
            : await RewriteAsync(slot, mind, refusal!, part, ct, observedOther: null);
        if (text == null)
        {
            preview?.Reset();
            return null;
        }

        var result = new ObservationResult(chosen, new NarrationBlock(
            Type: NarrationBlockType.Observation,
            ModusMentis: mind,
            Text: text,
            Actions: null,
            LinkedOutcome: chosen));
        Console.WriteLine(chosen != null
            ? $"ObservationController: [{mind.DisplayName}] observed '{chosen.DisplayName}'"
            : $"ObservationController: [{mind.DisplayName}] declined");

        // Complete only after the commit is attached, so a fast CONTINUE never fires a commit-less part.
        if (preview != null && part != null)
        {
            if (commit != null) part.AttachCommit(() => commit(result));
            part.MarkComplete();
            preview.EndProduction();
        }
        else
        {
            commit?.Invoke(result);
            preview?.Reset();
        }
        return result;
    }

    /// <summary>
    /// Which element <paramref name="mind"/> attends to — with what is said after its description — or
    /// the neutral words of its declining.
    /// </summary>
    private async Task<(NarrativeAnchor? Chosen, string? Trailing, string? Refusal)> ChooseAsync(
        IReadOnlyList<NarrativeAnchor> pool, bool named, ModusMentis mind, PartyMember member, NarrationNode node,
        (Scene.Scene Scene, PoV PoV) at, int locationId, bool mayDecline, bool reminescence, CancellationToken ct)
    {
        // A will attends only to what begins its course.
        if (!reminescence && WillPriority.Steers(mind, at.Scene))
        {
            var path = WillPriority.PathOf(mind, at.Scene, at.PoV, member);
            string purpose = mind.Will!.Purpose;
            if (named)
            {
                string phrase = Phrase(pool[0], locationId);
                if (path == null) return (null, null, NeutralNarration.WillObservationNotHere(phrase, purpose));
                return WillPriority.PathOn(pool[0], mind, at.Scene, at.PoV, member, path) is { } through
                    ? (pool[0], NeutralNarration.WillObservationReason(through, member), null)
                    : (null, null, NeutralNarration.WillObservationNotThisWay(phrase, path, member));
            }
            if (path != null)
                foreach (var anchor in pool)
                    if (WillPriority.PathOn(anchor, mind, at.Scene, at.PoV, member, path) is { } way)
                        return (anchor, NeutralNarration.WillObservationReason(way, member), null);
            return (null, null, NeutralNarration.WillNothing(path, purpose, member));
        }

        // A speaking mind never turns from a person; nothing turns away where every choice must commit.
        bool people = !reminescence && SpeakingPriority.Speaks(mind) && pool.All(SpeakingPriority.IsPerson);
        bool declines = mayDecline && !MustCommit() && !people;

        string? overall = _worldContext?.GenerateContextDescription(locationId);
        string? area    = node.GenerateNeutralDescription(locationId);

        if (named)
        {
            string phrase = Phrase(pool[0], locationId);
            bool keeps = !declines || (!Config.Debug.RefuseFocus && await _selector.SelectAsync(
                ChoiceKind.KeepFocus, mind, new[] { pool[0] },
                _ => "yes - it touches what this faculty knows, does or cares about",
                ThinkingPromptConstructor.SituationLine(overall, area, phrase),
                declineOption: "no - it is nothing to this faculty", ct: ct,
                question: $"Does {phrase} hold anything for this faculty?") != null);
            return keeps ? (pool[0], null, null)
                         : (null, null, NeutralNarration.ObservationNotInterested(phrase, reminescence));
        }

        // Several things of one name are one choice: the judge is shown "a birch tree" once, and one
        // of them stands for it.
        var candidates = DeduplicateByName(ObserveOnlyFilter(pool));
        if (declines && Config.Debug.RefuseFocus)
            return (null, null, NeutralNarration.ObservationNothing(reminescence));
        string? decline = !declines ? null
            : reminescence ? "let the recollection go, nothing of this is surfacing"
                           : "let your focus go, none of these is worth attending to";
        var picked = await _selector.SelectAsync(
            ChoiceKind.Focus, mind, candidates,
            c => $"focus on {Phrase(c, locationId)}",
            ThinkingPromptConstructor.SituationLine(overall, area, null),
            declineOption: decline, ct: ct);
        return picked != null ? (picked, null, null)
                              : (null, null, NeutralNarration.ObservationNothing(reminescence));
    }

    /// <summary>
    /// The account of one element in <paramref name="mind"/>'s voice: an attention line naming it and
    /// its description, merged into one neutral text and rewritten in a single request.
    /// </summary>
    private Task<string?> DescribeAsync(int slot, ModusMentis mind, NarrativeAnchor element,
        (Scene.Scene Scene, PoV PoV) at, int locationId, bool reminescence, string? trailing,
        PreviewPart? part, CancellationToken ct)
    {
        var neutral = NeutralNarration.Observation(
            Phrase(element, locationId), Description(element, locationId), reminescence,
            // A named NPC's appearance is a clause ("a figure hauls a barrel …"); shallow wildlife's
            // is a noun phrase ("a wiry white goat, chewing a rope").
            descriptionIsClause: element is ObservationObject { DescriptionIsClause: true });
        // A way out already known is said to be one.
        if (KnownWayNote(element, at) is { } known) neutral = $"{neutral} {known}";
        if (!string.IsNullOrWhiteSpace(trailing)) neutral = $"{neutral} {trailing.Trim()}";

        // Looking at a person or a beast, the rewrite is told it is somebody else (and checked for
        // having become them): their label as the player reads it, the name once introduced.
        string? observedOther = element is SyntheticNpcObservationObject npc
            ? Npc.NpcLabelResolver.ShortLabel(npc.SceneNpc.Entity)
            : null;
        return RewriteAsync(slot, mind, neutral, part, ct, observedOther);
    }

    private async Task<string?> RewriteAsync(int slot, ModusMentis mind, string neutral, PreviewPart? part,
                                             CancellationToken ct, string? observedOther)
    {
        try
        {
            return await _rewriter.RewriteAsync(slot, neutral, NarrationKind.Observation, mind.PersonaReminder2,
                keepHistory: true, styleInstruction: mind.StyleInstruction, preview: part?.NextSegment(), ct: ct,
                observedOther: observedOther, flavour: Sanitizer.PersonaFlavour.For(mind));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationController: rewrite failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates what the acting member says to a companion about <paramref name="about"/>: the three
    /// neutral lines (call attention, describe, ask) merged into one address and rewritten as direct
    /// speech in a single request — see <see cref="NeutralNarration.SpokenReport"/>.
    /// </summary>
    public async Task<NarrationBlock?> SpeakAsync(
        ModusMentis speakingMind,
        PartyMember speaker,
        string companionName,
        NarrativeAnchor about,
        int locationId,
        LlmPreviewSession? preview = null,
        Action<NarrationBlock>? commit = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"ObservationController: speaking to '{companionName}' about '{about.DisplayName}' with {speakingMind.DisplayName}");

        int slot = await _slots.GetOrCreateSlotForModusMentisPublicAsync(speakingMind);
        _slots.ResetSlot(slot);

        try
        {
            string description = Description(about, locationId);
            var part = preview?.BeginAccumulatingPart(PreviewTitles.For(speakingMind));
            // The rewriter returns the words between the quotes — the address is generated behind the
            // same `I say to X : "…"` frame a dialogue reply is.
            var spoken = (await _rewriter.RewriteAsync(
                slot,
                // A description written as a sentence (a person's appearance) is its own sentence
                // after the thing is named; "I noticed a figure hauls a barrel" is not.
                about is ObservationObject { DescriptionIsClause: true } clause
                    ? NeutralNarration.SpokenReport(companionName, clause.NeutralPhrase, description)
                    : NeutralNarration.SpokenReport(companionName, description),
                NarrationKind.Speaking,
                speakingMind.PersonaReminder2, companionName, keepHistory: true,
                styleInstruction: speakingMind.StyleInstruction,
                preview: part?.NextSegment(), ct: ct,
                flavour: Sanitizer.PersonaFlavour.For(speakingMind))).Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(spoken))
            {
                preview?.Reset();
                return null;
            }

            var block = new NarrationBlock(
                Type: NarrationBlockType.Speaking,
                ModusMentis: speakingMind,
                Text: $"\"{spoken}\"",
                Actions: null,
                LinkedOutcome: about,
                SpeakerName: speaker.DisplayName);

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
            Console.Error.WriteLine($"ObservationController: speaking failed: {ex.Message}");
            return null;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Narrows a pool to what <c>--observe-only</c> names. An exact name wins outright, then a whole
    /// word — "pig" means the pig, not the Courtyard–Pigsty Track — then a substring. The pool comes
    /// back untouched at the flag's default, and again when nothing in it matches: the flag names one
    /// thing, and every other kind of click must go on working.
    /// </summary>
    private static IReadOnlyList<NarrativeAnchor> ObserveOnlyFilter(IReadOnlyList<NarrativeAnchor> pool)
    {
        var wanted = Config.Debug.ObserveOnly;
        if (string.IsNullOrWhiteSpace(wanted)) return pool;

        var exact = pool.Where(o => TargetableNames(o).Any(n => n.Equals(wanted, StringComparison.OrdinalIgnoreCase))).ToList();
        if (exact.Count > 0) return exact;
        var word = pool.Where(o => TargetableNames(o).Any(n => Regex.IsMatch(
            n, $@"\b{Regex.Escape(wanted)}\b", RegexOptions.IgnoreCase))).ToList();
        if (word.Count > 0) return word;
        var substring = pool.Where(o => TargetableNames(o).Any(n => n.Contains(wanted, StringComparison.OrdinalIgnoreCase))).ToList();
        return substring.Count > 0 ? substring : pool;
    }

    /// <summary>
    /// Every name an element answers to in a script: the neutral name the narration uses, the
    /// observation id — derived from the real display name, where a person's neutral name is a
    /// contextual label such as "a stranger" — and its aliases (species, archetype id).
    /// </summary>
    public static IEnumerable<string> TargetableNames(NarrativeAnchor element)
    {
        if (element is not ObservationObject obs)
        {
            yield return element.DisplayName;
            yield break;
        }
        yield return obs.NeutralName;
        yield return obs.ObservationId.Replace('_', ' ');
        foreach (var alias in obs.TargetingAliases) yield return alias;
    }

    /// <summary>One representative, drawn at random, for each name in <paramref name="elements"/>.</summary>
    private List<NarrativeAnchor> DeduplicateByName(IEnumerable<NarrativeAnchor> elements)
        => elements
            .GroupBy(e => e is ObservationObject o ? o.NeutralName : e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var members = g.ToList();
                return members[_random.Next(members.Count)];
            })
            .ToList();

    /// <summary>
    /// The sentence that marks <paramref name="element"/> as a way already known from where it is
    /// seen (<see cref="AreaVisits.IsKnownWay"/>), or null. Exploration only.
    /// </summary>
    private static string? KnownWayNote(NarrativeAnchor element, (Scene.Scene Scene, PoV PoV) at)
    {
        if (at.Scene.Phase != NarrationPhase.Exploration) return null;
        Element? way = element switch
        {
            SyntheticAreaObservationObject a => a.Area,
            SyntheticObservationObject { PointOfInterest: ConnectorPointOfInterest c } => c,
            _ => null,
        };
        if (way == null || !AreaVisits.IsKnownWay(at.Scene, at.PoV.Where, way)) return null;
        return NeutralNarration.KnownWay(place: way is Area);
    }

    /// <summary>The element as an articled noun phrase.</summary>
    private static string Phrase(NarrativeAnchor element, int locationId)
        => element is ObservationObject obs ? obs.NeutralPhrase : Description(element, locationId);

    /// <summary>The element's richer description.</summary>
    private static string Description(NarrativeAnchor element, int locationId)
        => element is ObservationObject obs ? obs.GenerateNeutralDescription(locationId) : element.DisplayName;
}
