using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Abstract action that can modify the active party member (inventory),
/// the <see cref="PoV"/> (changing area/focus), or the <see cref="Scene"/> state (unlocking, etc.).
/// Verbs are registered in the global <see cref="VerbRegistry"/> and filtered per scene.
/// </summary>
public abstract class Verb
{
    /// <summary>Unique identifier for this verb type (e.g. "move", "grab").</summary>
    public abstract string VerbId { get; }

    /// <summary>Human-readable display name (e.g. "Move", "Grab").</summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Base difficulty of this verb (1–10). Combined with a situational modifier
    /// from the LLM critic to produce the final difficulty level.
    /// </summary>
    public abstract int BaseDifficulty { get; }

    /// <summary>
    /// Optional override for the action menu's difficulty glyph. When non-null, takes
    /// precedence over the difficulty-level derived glyph in <c>NarrativeUI</c>.
    /// </summary>
    public virtual char? DifficultyGlyphOverride => null;

    /// <summary>
    /// Whether performing this verb, here, on this target, is a crime — the one question the witness
    /// rules, the morality rules and the caught-red-handed path all ask.
    ///
    /// <para>Sealed on purpose, for the same reason <see cref="IsPossible"/> is: the setting test —
    /// standing in somebody's private area makes <i>anything</i> done there trespass — holds for every
    /// verb at once, and a gate that each of the three call sites could forget is a gate that does not
    /// hold. The verb's own, conditional half lives in <see cref="IsIllegalFor"/>.</para>
    ///
    /// <para>Legality is contextual, not a constant: a blow struck at somebody who is already your
    /// enemy is self-defence, a lock picked on a public storehouse is nobody's business, and a bowl
    /// smashed in a public hall is only bad manners. A null actor means "no particular body" (content
    /// audits, tooling) and is treated as having no enemies — the strictest reading, so an audit never
    /// under-reports what is a crime.</para>
    /// </summary>
    public bool IsIllegal(Scene scene, PoV pov, Element? target, PartyMember? actor = null)
        => pov.Where.IsPrivate || IsIllegalFor(scene, pov, target, actor);

    /// <summary>
    /// The verb's own condition for being a crime, asked outside anybody's private space.
    /// Defaults to false — most verbs are nobody's business. Override for the crimes, and consult
    /// <see cref="PrivacyModel.ReachesPrivateArea"/> rather than <c>pov.Where</c> for anything whose
    /// wrongness comes from where the <i>target</i> leads.
    /// </summary>
    protected virtual bool IsIllegalFor(Scene scene, PoV pov, Element? target, PartyMember? actor) => false;

    /// <summary>
    /// Whether <paramref name="target"/> is somebody who already counts <paramref name="actor"/> an
    /// enemy. Striking first at someone who has already declared for violence is not a crime — the
    /// quarrel exists before the blow, and a witness to it is watching a fight, not a murder.
    ///
    /// <para>Shared by attack, slay and murder, which agree on this and on very little else. Only
    /// <see cref="NpcEntity"/> keeps an affinity table; shallow wildlife has no opinion of anybody,
    /// and the three verbs exclude the tiny ones outright.</para>
    /// </summary>
    protected static bool TargetIsAlreadyHostile(Element? target, PartyMember? actor)
        => actor != null
           && target is SceneNpc { Entity: NpcEntity npc }
           && npc.AffinityTable.IsEnemy(actor.AffinityKey);

    /// <summary>
    /// Whether this verb is valid to use when an enemy is nearby (same area).
    /// When false, the LLM critic asks whether the enemy gets an opportunity attack.
    /// Override to true for combat verbs (attack, slay, reconcile, appease).
    /// </summary>
    public virtual bool CanBeUsedUnderThreat => false;

    /// <summary>
    /// What the acting body must be able to do for this verb to be offered at all — speech for
    /// anything that opens a conversation, handcraft for tools, locks and carrying. Default
    /// <see cref="AnatomyCapability.None"/>: available to every anatomy.
    ///
    /// <para>Checked in <see cref="IsPossible"/> before any scene state, so it holds for every caller
    /// at once. Without it a wolf narrating after a Speak-About hand-off is offered "introduce
    /// myself" and "pick the lock" like anyone else.</para>
    /// </summary>
    public virtual AnatomyCapability RequiredCapabilities => AnatomyCapability.None;

    /// <summary>
    /// Whether this verb can be executed given the current scene state <b>and</b> the acting body.
    ///
    /// <para>Sealed on purpose: the anatomy gate is applied here, once, and the per-verb condition
    /// lives in <see cref="IsPossibleFor"/>. Direct callers exist outside the scene view — the routine
    /// replay engine, the verb audit, the debug window — and a gate they could each forget is a gate
    /// that does not hold. A null actor means "no particular body" (content audits, tooling) and
    /// passes the capability test.</para>
    /// </summary>
    public bool IsPossible(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => (actor?.Can(EffectiveCapabilities) ?? true) && IsPossibleFor(scene, pov, target, actor);

    /// <summary>The verb's own condition: scene state, target shape, affinity, schedule, tools.</summary>
    protected abstract bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null);

    /// <summary>Natural-language string describing the intended action, sent to the LLM.</summary>
    public abstract string Verbatim(Scene scene, PoV pov, Element target);

    /// <summary>
    /// Expands this verb into the concrete action views it offers for <paramref name="target"/>.
    /// The default is one view when <see cref="IsPossible"/> holds — the same behaviour as the old
    /// per-verb enumeration. Verbs that turn a single target into several actions (like GRAB across
    /// items, or <c>RequestJobVerb</c> across offered jobs) override this to yield several views,
    /// carrying a per-view payload in <see cref="VerbAction.Variant"/>.
    /// </summary>
    public virtual IEnumerable<VerbAction> ExpandViews(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (IsPossible(scene, pov, target, actor))
            yield return new VerbAction(this, Verbatim(scene, pov, target), target);
    }

    /// <summary>
    /// The base difficulty for this verb against a specific target. Defaults to
    /// <see cref="BaseDifficulty"/>; override to make difficulty depend on the target
    /// (e.g. requesting work from a master is harder than from a reeve).
    /// </summary>
    public virtual int DifficultyFor(Element? target) => BaseDifficulty;

    /// <summary>
    /// Object pronoun for a named NPC target, used by NPC verb verbatims: the NPC is introduced
    /// once in the prompt's attention line, and the verbatim refers back by pronoun. Gendered
    /// ("him"/"her" from the NPC's gender stat, "it" for named beasts) rather than neutral "them",
    /// because the critic stage renders the acting character as "they" — a neutral "them" for the
    /// NPC would collide with it. Falls back to "them" for non-NPC targets.
    /// </summary>
    protected static string NpcPronoun(Element target)
    {
        if (target is not SceneNpc { Entity: NpcEntity npc }) return "them";
        if (npc.Combatant.AnatomyType != AnatomyType.Human) return "it";
        return NpcLabelResolver.GenderIsMale(npc.Combatant) ? "him" : "her";
    }

    /// <summary>
    /// <b>Subject</b> pronoun for a named NPC target — "he"/"she"/"it" — for the clause of a verbatim
    /// where the NPC is the one doing something ("see where he goes"). <see cref="NpcPronoun"/> is the
    /// object form and reads as broken English in that position ("see where him goes"), which the
    /// persona then faithfully copies into the action text.
    /// </summary>
    protected static string NpcSubjectPronoun(Element target)
    {
        if (target is not SceneNpc { Entity: NpcEntity npc }) return "they";
        if (npc.Combatant.AnatomyType != AnatomyType.Human) return "it";
        return NpcLabelResolver.GenderIsMale(npc.Combatant) ? "he" : "she";
    }

    /// <summary>
    /// <b>Possessive</b> determiner for a named NPC target — "his"/"her"/"its" — for a verbatim naming
    /// something the NPC owns ("go through his pockets"). Same reason as
    /// <see cref="NpcSubjectPronoun"/>: the object form gives "go through him pockets".
    /// </summary>
    protected static string NpcPossessive(Element target)
    {
        if (target is not SceneNpc { Entity: NpcEntity npc }) return "their";
        if (npc.Combatant.AnatomyType != AnatomyType.Human) return "its";
        return NpcLabelResolver.GenderIsMale(npc.Combatant) ? "his" : "her";
    }

    /// <summary>
    /// Builds the verbatim for an item-pickup verb (grab/gather/steal/cut), e.g. "gather some moss",
    /// "grab an apple", "steal a wool cloak". The item name is routed through
    /// <see cref="Item.WithArticle"/> so mass nouns ("moss", "bread") and plurals get "some" rather
    /// than an ungrammatical "a moss". Falls back to a plain a/an article for non-item targets.
    /// </summary>
    protected static string PickupVerbatim(string verb, Element target)
    {
        if (target is ItemElement itemEl)
            return $"{verb} {itemEl.Item.WithArticle()}";

        var name    = target.DisplayName.ToLowerInvariant();
        var article = name.Length > 0 && "aeiou".Contains(name[0]) ? "an" : "a";
        return $"{verb} {article} {name}";
    }

    /// <summary>
    /// A definite noun phrase for a specific scene feature (a point of interest, spot or area), e.g.
    /// "the hedge gap", "the wooden door", "the stairs". Such features are named with bare nouns by
    /// their builders, so this lower-cases the name and prepends "the" (unless it already opens with a
    /// determiner). Use it whenever a verb embeds a fixed scene target in its verbatim, so the neutral
    /// sentence reads grammatically ("follow the hedge gap", not "follow hedge gap").
    /// </summary>
    protected static string DefiniteTarget(Element target)
    {
        string lower = (target.DisplayName ?? string.Empty).Trim().ToLowerInvariant();
        if (lower.Length == 0) return "it";
        int sp = lower.IndexOf(' ');
        string first = sp < 0 ? lower : lower.Substring(0, sp);
        return System.Array.IndexOf(Determiners, first) >= 0 ? lower : $"the {lower}";
    }

    private static readonly string[] Determiners =
    {
        "the", "a", "an", "some", "this", "that", "these", "those",
        "his", "her", "its", "their", "your", "my", "our",
    };

    /// <summary>
    /// Returns the <see cref="Outcome"/> objects that result from a successful execution
    /// of this verb. Each report both describes itself for the UI and applies its own
    /// game-state change via <see cref="Outcome.Apply"/>.
    /// </summary>
    public virtual IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => System.Array.Empty<Outcome>();

    /// <summary>
    /// View-aware success reports. Called by the execution pipeline with the exact
    /// <see cref="VerbAction"/> the player chose, so verbs that expanded into several actions can
    /// read <see cref="VerbAction.Variant"/> (e.g. which job was requested). Defaults to the
    /// target-only overload.
    /// </summary>
    /// <param name="tool">
    /// The implement the player combined with the action, or null for bare hands. Almost no verb
    /// reads it — a combined tool's effect is normally the die it lends the chain, which is settled
    /// long before this — but <c>attack</c> does, because <i>which</i> weapon struck decides which
    /// blow can be thrown at all. Null everywhere the reports are re-derived outside a live action
    /// (the outcome audit, a routine replay), which reads correctly as bare hands.
    /// </param>
    public virtual IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target,
                                                        VerbAction view, Item? tool = null)
        => SuccessReports(scene, pov, actor, target);

    /// <summary>
    /// Returns the <see cref="Outcome"/> objects that result from a failed execution
    /// of this verb (verb-specific failure side-effects, excluding LLM-decided wounds).
    /// </summary>
    public virtual IReadOnlyList<Outcome> FailureReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => System.Array.Empty<Outcome>();

    /// <summary>
    /// Applies all success reports in sequence. Kept for compatibility — prefer calling
    /// <see cref="SuccessReports"/> and iterating the results directly.
    /// </summary>
    public void Execute(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        foreach (var report in SuccessReports(scene, pov, actor, target))
            report.ApplyTo(OutcomeContext.For(actor, scene, pov));
    }

    // ── Routine recording hooks ───────────────────────────────────────────────
    // A successful verb may be recorded as a step in a learned routine that is later replayed
    // without narration or skill checks. By default no verb is recordable; recordable verbs
    // override these. The contract is dynamic: a verb may inspect scene/pov/target and decline
    // to be recorded in special situations.

    /// <summary>
    /// Whether a successful execution of this verb on <paramref name="target"/> can be recorded as
    /// a routine step. Default: false (no verb is recordable until it opts in).
    /// </summary>
    public virtual bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => false;

    /// <summary>
    /// Builds the stable, rebuild-independent reference to this verb's target for routine recording.
    /// Only meaningful when <see cref="CanRecordAsRoutine"/> returns true.
    /// </summary>
    public virtual RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target) => null;

    /// <summary>
    /// The player-facing name of this verb as a recorded routine step — and, for the last step, the
    /// name of the routine itself. Evaluated once at record time, while scene/pov/target are still
    /// live, so whatever it reads from the context is baked into the saved routine.
    ///
    /// This exists because <see cref="Verbatim"/> is written for the LLM prompt, where the target has
    /// already been named in the attention line and is therefore referred back to by pronoun ("meet
    /// her to talk"). A routine is read cold, months of play later, out of any context — so a verb
    /// whose verbatim leans on the surrounding prompt overrides this to name the target outright
    /// ("meet Aldith to talk"). Defaults to the verbatim, which is already concrete for every verb
    /// that spells its target out ("gather some moss", "climb up the low wall").
    /// </summary>
    /// <param name="view">The chosen view when the verb expanded into several actions (e.g. which job
    /// was requested), or null when the caller has none.</param>
    public virtual string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => Verbatim(scene, pov, target);

    /// <summary>
    /// The display name of an NPC target, for <see cref="RoutineLabel"/> overrides that replace the
    /// verbatim's pronoun with the real name. Falls back to the target's own display name (and to
    /// "them" when there is none), so a label is never left with a dangling blank.
    /// </summary>
    protected static string NpcName(Element target)
    {
        string name = (target as SceneNpc)?.Entity.DisplayName?.Trim()
                      ?? target?.DisplayName?.Trim()
                      ?? "";
        return name.Length == 0 ? "them" : name;
    }

    /// <summary>
    /// Whether a successful, <i>unrecordable</i> execution of this verb ends the routine being
    /// recorded, or may simply be left out of it. Skipping is the norm: introducing yourself to a
    /// stranger, grabbing a one-off item or picking a fight are not routine steps, but the chain of
    /// steps around them stays perfectly replayable, so recording continues as if they had not
    /// happened. Only effects that a replayed chain cannot reproduce end the recording.
    ///
    /// The decision is read off the reports the execution is about to apply — see
    /// <see cref="RoutineChainEffect"/> — so it stays correct for verbs that do not exist yet.
    /// Override only for a verb whose reports do not tell the whole story.
    /// </summary>
    public virtual bool BreaksRoutineRecording(Scene scene, PoV pov, Element target,
                                               IReadOnlyList<Outcome> reports)
        => reports.Any(r => r.RoutineChainEffect != RoutineChainEffect.None);

    /// <summary>
    /// The phase this verb transitions into on success, used to decide where a recorded routine
    /// stops and what happens after replay. Default: <see cref="RoutinePhaseKind.None"/>.
    /// </summary>
    public virtual RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.None;

    /// <summary>
    /// A stable key identifying the chosen <see cref="VerbAction.Variant"/> for routine recording, so
    /// replay can rebuild the same view (e.g. which job was requested). Default: null (no variant).
    /// </summary>
    public virtual string? RoutineVariantKey(VerbAction view) => null;

    /// <summary>
    /// Rebuilds the <see cref="VerbAction.Variant"/> payload from a key produced by
    /// <see cref="RoutineVariantKey"/>, used when replaying a recorded step. Default: null.
    /// </summary>
    public virtual object? ResolveRoutineVariant(string variantKey) => null;

    /// <summary>
    /// The item this verb would add to the actor's inventory on success, or null for verbs that do not
    /// pick anything up. Pickup verbs (grab/gather/steal/cut) override this so the coded
    /// inventory-capacity rule can block the action when there is no room to carry it.
    /// </summary>
    public virtual Item? AcquiredItem(Element? target) => null;

    // ── Tool requirements ──────────────────────────────────────────────────────

    /// <summary>
    /// Whether an implement may be combined with this verb, and whether one is obligatory.
    /// Default <see cref="Scene.Verbs.ToolUsage.Optional"/>.
    ///
    /// <para>The question is mechanical rather than aesthetic: a combined item joins the die chain as
    /// its leaf, so the test is "can holding a thing change how well this goes?". Where the answer is
    /// structurally no — speech, the senses, thought, waiting, walking on the flat — the verb is
    /// <see cref="Scene.Verbs.ToolUsage.Excluded"/> and any combination attempted against it fails
    /// without troubling the critic.</para>
    ///
    /// <para>Excluded is <b>not</b> the same as unofferable. The "Use Tool" row is greyed only when
    /// nothing combinable is carried at all, so the attempt is always available and always costs a
    /// noetic point when it fails. Choosing sensibly is the player's part of the bargain — and an
    /// implement may carry an exception for a particular excluded verb (see
    /// <c>Item.MadeForVerbIds</c>), which is what lets a glass be raised to the eye.</para>
    /// </summary>
    public virtual ToolUsage ToolUse => ToolUsage.Optional;

    /// <summary>
    /// The <c>ItemId</c>s of the tools this verb is normally done with. Read only when
    /// <see cref="ToolUse"/> is <see cref="Scene.Verbs.ToolUsage.Required"/>, and mandatory there —
    /// <c>--verb-audit</c> fails a Required verb that names none.
    ///
    /// <para>A Required verb is <b>impossible without a combined item</b> — you cannot mine a seam by
    /// hand — and an item that is not one of these is put to the item-use critic, which decides
    /// whether it could stand in. Listing several ids means any of them serves outright (a rod or a
    /// net will both catch fish). An item named here needs no critic call at all: it is the tool.</para>
    ///
    /// <para>Ids are matched against <c>ItemRegistry</c>; <c>--verb-audit</c> resolves every one, since
    /// a typo here makes the verb permanently impossible rather than merely wrong.</para>
    /// </summary>
    public virtual IReadOnlyList<string> ReferenceToolIds => System.Array.Empty<string>();

    /// <summary>Whether this verb cannot be attempted with bare hands.</summary>
    public bool RequiresTool => ToolUse == ToolUsage.Required;

    /// <summary>
    /// Whether this verb is struck with a <b>fighting medium</b> — an organ or a weapon — rather than
    /// worked at with an implement. Only <c>attack</c> is, and what it changes is the whole tool
    /// question: for such a verb a weapon is accepted without a critic call (a sword is what one
    /// attacks with; asking an LLM to confirm it is a request spent to be told the obvious) and
    /// anything that is not a weapon is refused without one either, however clever the case for it.
    ///
    /// <para>Declared here rather than tested by <see cref="VerbId"/> in each place that cares,
    /// because three layers read it: <c>ToolCombinationRules.Resolve</c> for the two gates above,
    /// <c>FirstBlowMediumRule</c> for whether the body has anything to strike with at all, and the
    /// verb's own reports for the blow that lands.</para>
    /// </summary>
    public virtual bool UsesFightingMedium => false;

    /// <summary>
    /// What the body must be able to do for this verb, <b>including what its tool rule implies</b>.
    /// A Required verb cannot be performed without combining an implement, and combining one is
    /// handcraft — so the capability follows from the category rather than being restated on each
    /// verb, exactly as the anatomy gate itself is applied once in <see cref="IsPossible"/> rather
    /// than trusted to every call site.
    ///
    /// <para>Without this a beast would be <i>offered</i> <c>slay</c> and then refused by
    /// <c>RequiredToolRule</c> at the cost of a noetic point, every time — a withholding dressed up
    /// as a refusal, which is the distinction the two rule families exist to keep.</para>
    /// </summary>
    public AnatomyCapability EffectiveCapabilities
        => RequiredCapabilities | (RequiresTool ? AnatomyCapability.Handcraft : AnatomyCapability.None);

    // ── Learning ───────────────────────────────────────────────────────────────

    /// <summary>
    /// The modus mentis a successful execution of this verb teaches — the verb's <i>default</i>
    /// lesson, before the target gets a say. Doing a thing is how the thing is learned: succeed at a
    /// verb you have no modus mentis for and you acquire it at level 1; succeed at one you already
    /// have and it earns experience instead.
    ///
    /// <para>Override on every verb. The target may override the override by implementing
    /// <see cref="IVerbModusMentisSource"/> — see <see cref="ResolveGrantedModusMentisId"/>, which is
    /// what the execution pipeline actually calls. Ids are resolved against
    /// <c>ModusMentisRegistry</c> and a bad one grants nothing silently, so <c>--verb-audit</c>
    /// checks that every id declared here and in every target override resolves.</para>
    /// </summary>
    public virtual string? GrantedModusMentisId(Element? target) => null;

    /// <summary>
    /// Every lesson this verb could teach for <paramref name="target"/>, <b>most restrictive first</b>
    /// — the ordered candidate list <see cref="ResolveGrantedModusMentisId"/> walks until it finds one
    /// the acting body can hold. Defaults to the single <see cref="GrantedModusMentisId"/>.
    ///
    /// <para>This exists because climbing, digging, smelling, tracking and catching are done by both
    /// anatomies and are not the <i>same lesson</i> for both: a beast clambers with claws and a person
    /// scales with arms, and neither modus mentis names an organ the other owns. Declaring one id
    /// meant one of the two learned nothing at all — which is what the game shipped, for ten verbs,
    /// with only a log line to say so.</para>
    ///
    /// <para><b>Order beast before human.</b> A beast's lesson names a beast organ, so a human cannot
    /// hold it and falls through; a human's lesson often names organs a beast also owns (eyes,
    /// anamnesis), so putting it first would take the beast's own lesson away from it.</para>
    /// </summary>
    public virtual IReadOnlyList<string> GrantedModusMentisIds(Element? target)
        => GrantedModusMentisId(target) is { } id ? new[] { id } : System.Array.Empty<string>();

    /// <summary>
    /// Every lesson this act could teach here, <b>most specific first</b>. The base walks the list
    /// and grants the first one the acting body can hold.
    ///
    /// <para>This is the only place a verb says what it teaches. It replaced a pair of methods — a
    /// contextual one that could return null to "fall through" to a separate default — and the pair
    /// was the bug: nine verbs ended a chain with an unconditional answer, and their own defaults
    /// became unreachable while both audits went on reporting them as reachable. With one method the
    /// default is the last line you read, so a shadowed default is visible rather than inferable.</para>
    ///
    /// <para>Override to add cases, and <b>end with <c>base.Lessons(ctx)</c></b>, which yields what
    /// the target itself declares and then the verb's own default:</para>
    /// <code>
    /// public override IEnumerable&lt;ModusMentis&gt; Lessons(LessonContext ctx)
    /// {
    ///     if (ctx.Night) yield return Mm&lt;MoonwaterModusMentis&gt;();
    ///     foreach (var m in base.Lessons(ctx)) yield return m;   // natation
    /// }
    /// </code>
    ///
    /// <para>Two rules for what goes here. <b>A specific verb teaches its own act</b> — swimming
    /// teaches swimming, and the broad dispositions (resolve, recklessness, nerve) belong to the
    /// general verbs like MOVE, or to a conversation. And <b>the body's injuries are not consulted</b>:
    /// a lesson is what the act was, not what state the actor happened to be in.</para>
    /// </summary>
    public virtual IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        var declared = (ctx.Target as IVerbModusMentisSource)?.ModusMentisFor(VerbId);
        if (declared != null)
        {
            var mm = ModusMentisRegistry.Instance.GetModusMentis(declared);
            if (mm != null) yield return mm;
        }

        foreach (var id in GrantedModusMentisIds(ctx.Target))
        {
            var mm = ModusMentisRegistry.Instance.GetModusMentis(id);
            if (mm != null) yield return mm;
        }
    }

    /// <summary>The registered template for a modus mentis, by type — compile-checked, so a lesson
    /// cannot be misspelt into silence the way a string id could.</summary>
    protected static ModusMentis Mm<T>() where T : ModusMentis, new()
        => ModusMentisRegistry.Instance.GetModusMentis(typeof(T)) ?? new T();

    /// <summary>
    /// The one lesson this action teaches: the first candidate the acting body can hold.
    /// Sealed — the order is the design, and it lives in <see cref="Lessons"/> where it can be read.
    /// </summary>
    public ModusMentis? ResolveLesson(LessonContext ctx)
    {
        foreach (var mm in Lessons(ctx))
            if (ctx.Actor == null || ModusMentisAnatomy.IsLearnableBy(mm, ctx.Actor)) return mm;
        return null;
    }
    // ── Failure penalties ──────────────────────────────────────────────────────
    // Replaces the former LLM failure-outcome critic tree: instead of asking the critic which body
    // part is wounded, each verb declares the injuries a failure can cause and one is sampled.

    /// <summary>A single "no injury" penalty list — the default for verbs that never wound on failure.</summary>
    protected static readonly IReadOnlyList<Wound?> NoPenalty = new Wound?[] { null };

    /// <summary>
    /// Candidate physical penalties for a failed execution of this verb. Each entry is a
    /// <see cref="Wound"/> to inflict or <c>null</c> for "no injury". On failure one entry is sampled
    /// uniformly (see <see cref="SampleFailurePenalty"/>), so repeat an entry to weight it — e.g.
    /// several <c>null</c>s for a usually-harmless verb. Default: never injures. Return fresh wound
    /// instances when the wound carries placement state.
    /// </summary>
    public virtual IReadOnlyList<Wound?> FailurePenalties(Element? target) => NoPenalty;

    /// <summary>Samples one failure penalty uniformly (null ⇒ no injury).</summary>
    public Wound? SampleFailurePenalty(Element? target, System.Random rng)
    {
        var penalties = FailurePenalties(target);
        if (penalties == null || penalties.Count == 0) return null;
        return penalties[rng.Next(penalties.Count)];
    }
}
