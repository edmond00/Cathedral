using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base class for all modiMentis.
/// ModiMentis define the protagonist's capabilities and narrative perspectives.
/// </summary>
public abstract class ModusMentis
{
    public abstract string ModusMentisId { get; }           // "observation", "algebraic_analysis"
    public abstract string DisplayName { get; }       // "Observation", "Algebraic Analysis"

    /// <summary>
    /// How this skill operates, shown in action lists as "with [SkillMeans]".
    /// A short flavored phrase used inside LLM prompts (e.g. "the breaking and turning of soil").
    /// It is also the whole of a means option the <see cref="ChoiceJudge"/> reads
    /// ("Natation: swimming across deep water…"): name the act in the verb a goal would use, and
    /// beware echoing a goal's wording by accident — the judge matches words before temperaments.
    /// Rules and the tools that measure them: the <c>choice-judge</c> skill.
    /// </summary>
    public abstract string SkillMeans { get; }

    /// <summary>
    /// Player-facing manual entry shown in the memory menu's detail box — a factual,
    /// third-person explanation of what this modusMentis governs and when it helps.
    /// Unlike <see cref="SkillMeans"/> / <see cref="PersonaTone"/> it is neutral game documentation
    /// rather than in-character flavour — which is exactly why <see cref="ChoiceJudge"/> reads it: the
    /// neutral judge of what this modus mentis would choose is told what it governs in these words.
    /// So it must say what the modus mentis is drawn to and good at, not merely how it scores:
    /// name its acts in the plain words options use ("swims", not only "the steady stroke"), say what
    /// it inclines to do, never soften a violent or criminal nature, and use no word that names a
    /// different act ("waits" made Spite choose "sit and wait"). The <c>choice-judge</c> skill has the
    /// rules and <c>tools/judge/</c> the benchmark and audit that check them.
    /// The detail box word-wraps to roughly 34 columns by ~11 lines, so keep it under ~60 words
    /// and use continuous prose (no manual line breaks — the renderer wraps on spaces).
    /// </summary>
    public abstract string MenuDescription { get; }
    public abstract ModusMentisFunction[] Functions { get; } // Can have multiple functions (1-3)

    /// <summary>
    /// Ids of the anatomy sources this modusMentis draws on. Each entry may name an <b>organ</b>
    /// (e.g. "eyes", "tongue") or a <b>body region</b> (e.g. "visage", "trunk"); both contribute to
    /// the max level via their <see cref="IMaxLevelContributionStat"/> (organ +0..+3, region +0..+6).
    /// <para>
    /// Exactly 1 region XOR exactly 2 distinct organs, enforced at startup by rule R5 in
    /// <see cref="ModusMentisRuleValidator"/>. <b>No entry is "primary"</b> — every one contributes
    /// equally, so anything reading only <c>Organs[0]</c> is a bug.
    /// </para>
    /// <para>
    /// This is also the <i>only</i> way anatomy reaches an outcome. Organs and regions set the level
    /// ceiling and nothing else: an action succeeds on the dice alone — one d6 per point of summed
    /// modus-mentis level, needing as many sixes as the difficulty — so a better organ helps only by
    /// letting a modus mentis be raised further, never by nudging a roll.
    /// </para>
    /// </summary>
    public abstract string[] Organs { get; }

    /// <summary>
    /// What a body must be <i>able</i> to do to hold this modus mentis, over and above owning the
    /// organs in <see cref="Organs"/>. Default <see cref="AnatomyCapability.None"/>: anything with the
    /// right organs can learn it.
    ///
    /// <para>This is the authored half of the anatomy gate. The structural half is free — a modus
    /// mentis related to <c>fangs</c> is already out of a human's reach — but a wolf owns a tongue and
    /// a cerebrum, so only a declaration keeps rhetoric away from it. Every modus mentis carrying the
    /// Speaking function requires <see cref="AnatomyCapability.Speech"/>; the lettered and
    /// institutional ones require <see cref="AnatomyCapability.Abstraction"/>.</para>
    ///
    /// <para>Rule R11 guarantees the declarations here can never strand an anatomy: every organ and
    /// region of every anatomy keeps at least 3 modi mentis its owner can actually learn.</para>
    /// </summary>
    public virtual AnatomyCapability RequiredCapabilities => AnatomyCapability.None;

    /// <summary>
    /// What this modus mentis <i>feels</i> about what an action produced — the declaration behind the
    /// <see cref="ModusMentisFunction.Emotion"/> function. Empty by default: a modus mentis with no
    /// triggers is a competence, not a disposition, and that is what most of the catalogue is.
    ///
    /// <para>Read only for a modus mentis the acting body actually <b>holds</b>, and read for every
    /// one it holds — not only the one that carried the action out. Avarice rejoices at coin whether
    /// or not avarice was the modus mentis that earned it; keyed to the acting one, the emotion would
    /// fire in the rare case and stay silent in the common one, which is backwards.</para>
    ///
    /// <para>Rules R14 and R15 police this: a modus mentis may not be both Action and Emotion, and
    /// the function and the list imply each other (declaring triggers without the function makes them
    /// dead, and the function without triggers makes the modus mentis dead as an emotion).</para>
    /// </summary>
    public virtual EmotionTrigger[] EmotionTriggers => System.Array.Empty<EmotionTrigger>();

    /// <summary>
    /// What this modus mentis wants to do — the declaration behind the <see cref="ModusMentisFunction.Will"/>
    /// function. Null by default. Rule R17 makes the two imply each other.
    ///
    /// <para>A will observes and thinks, but only toward its target: <see cref="ActionPathPlanner"/>
    /// finds the shortest path of actions from where the body stands to an action the target matches,
    /// and the will attends only to the object of the path's first step and proposes only that step.
    /// See <c>WillPriority</c> for every place this narrows a choice.</para>
    /// </summary>
    public virtual WillTargets? Will => null;

    /// <summary>
    /// The kinds of area a will belongs in — where a player would expect it to find something to do
    /// (herblore: a forest, a meadow). Area types, never names. Read by <c>--will-audit</c>, which plans
    /// the will from each and reports the share that has a way to a target: the related coverage.
    /// Empty for a modus mentis that is not a will. <see cref="WillPlaces"/> holds the usual groups.
    /// </summary>
    public virtual IReadOnlyList<Type> WillPlaces => Array.Empty<Type>();

    /// <summary>Whether this modus mentis bears the <see cref="ModusMentisFunction.Will"/> function.</summary>
    public bool IsWill => Functions.Contains(ModusMentisFunction.Will);

    /// <summary>
    /// Whether this modus mentis may be chosen to observe: the Observation function, or a will, which
    /// includes the observing office (narrowed to its path). Read wherever a body's observers are listed.
    /// </summary>
    public bool CanObserve => Functions.Contains(ModusMentisFunction.Observation) || IsWill;

    /// <summary>Whether this modus mentis may be chosen to think: the Thinking function, or a will.</summary>
    public bool CanThink => Functions.Contains(ModusMentisFunction.Thinking) || IsWill;


    public int Level { get; set; }                    // current level; capped by GetMaxLevelForModusMentis (random initial)
    public int CurrentXp { get; set; }                // progress toward next level; reset to 0 on level-up

    /// <summary>
    /// Which long-term memory module this modusMentis belongs to.
    /// Working and Residual modules accept any modusMentis regardless of this value.
    /// Every subclass must declare its memory type explicitly.
    /// </summary>
    public abstract ModusMentisMemoryType MemoryType { get; }

    /// <summary>
    /// Ethical alignment of this modusMentis — used during illegal-action plausibility checks.
    /// Low modiMentis support deception/violence; High ones resist it.
    /// Defaults to <see cref="MoralLevel.Medium"/>; override in subclasses.
    /// </summary>
    public virtual MoralLevel MoralLevel => MoralLevel.Medium;

    /// <summary>
    /// Whether this modus mentis carries out its actions discreetly (quietly, out of sight).
    /// A discrete modus mentis is one step harder to notice: it prepends "discreetly" to its action
    /// text and, in the effective-proximity model, downgrades a nearby witness/threat by one level
    /// (Visual→Audio, Audio→None). Defaults to false; override to true on stealthy modiMentis.
    /// </summary>
    public virtual bool ActsDiscretely => false;

    /// <summary>
    /// Persona prompt for LLM (only for Observation and Thinking modiMentis).
    /// This is cached in the LLM slot and defines the modusMentis's narrative voice.
    /// </summary>
    public virtual string? PersonaPrompt => null;
    
    /// <summary>
    /// Short persona description for user prompts (e.g., "write like [PersonaTone]").
    /// Used as a quick reminder of the modusMentis's personality in individual LLM calls, and read by
    /// <see cref="ChoiceJudge"/> as "Its manner: …". Lead with the manner — how it goes about
    /// things; an origin anecdote may trail it but must not be it ("a dreamer who once dreamt a golden
    /// arch" reads as a dreamer). See the <c>choice-judge</c> skill.
    /// </summary>
    public virtual string? PersonaTone => null;

    /// <summary>
    /// Very short phrase (3-5 words) used as "As a [PersonaReminder], what/why/..." in prompts.
    /// Example: "theatrical performance analyst", "relentless clinical investigator".
    /// </summary>
    public virtual string? PersonaReminder => null;

    /// <summary>
    /// A paraphrase of <see cref="PersonaReminder"/> used at the end of prompts as
    /// "Stay in the character of [PersonaReminder2]." — a different angle on the same persona.
    /// Example: "someone who never misses a detail", "a mind that measures everything it touches".
    ///
    /// <para><b>A disposition, not a picture.</b> Whatever it names, the model puts in the scene:
    /// "someone who finds the hidden nest by listening" put a nest in nine answers of sixteen,
    /// "someone who stops at lit windows" a window in a forge. Say how the persona is — "someone who
    /// listens before looking and is rarely surprised" — and name none of its things.
    /// <see cref="PersonaLint"/> fails <c>--mm-audit</c> on a reminder naming two concrete nouns.</para>
    /// </summary>
    public virtual string? PersonaReminder2 => null;

    /// <summary>
    /// Persona-specific guidance, dropped into rewrite prompts, on HOW this modusMentis may colour
    /// its narration with figures of speech and optional inner feelings — e.g. "Use metaphor to make
    /// the sentence poetic." for Poetry, or "Compare what you sense to load, span and structure..."
    /// for Architecture. This replaces the generic "figures of speech are welcome" clause so each
    /// modusMentis flavours its prose in its own way.
    ///
    /// IMPORTANT: describe ONLY figurative/stylistic licence or optional inner feelings here.
    /// NEVER instruct the model to add literal facts, names, objects or events — that would break the
    /// "keep every literal fact, invent nothing" contract the rewrite prompt also enforces.
    ///
    /// <para><b>A manner, then a lens on whatever is in the scene</b> — "Slow, patient and exact -
    /// judge things by the patience in them, well made or rank." Never the persona's own objects or
    /// acts, and never "Use images of…" or "Narrate the…", which open a list of them: the model treats
    /// what a style names as present. "Use images of rendering fat, the dip and the cooling coat,
    /// wicks, wax and the steady flame" turned a greeting into a candle dipped twenty times; across the
    /// catalogue, the more things a style named the more of them reached scenes that held none
    /// (tools/prompts, NOTES.md, "Persona fields"). The manner is what a one-line action can still
    /// carry without inventing an act for it. <see cref="PersonaLint"/> fails <c>--mm-audit</c> on a
    /// style naming two concrete nouns or opening such a list.</para>
    ///
    /// The base value preserves the old generic behaviour for modiMentis without a persona.
    /// </summary>
    public virtual string StyleInstruction =>
        "Where it fits, a figure of speech (metaphor, comparison, imagery) or an inner feeling that suits your character is welcome.";
}

/// <summary>
/// What one call to <see cref="PartyMember.AwardModusMentisXp"/> came to, and the single place the
/// player-facing wording for it is decided.
///
/// <para>Every site that grants experience — an action's modus-mentis chain, a landed blow, a
/// conversation branch, a verb's own lesson — reports it through <see cref="Describe"/>, so the
/// narration chips and the fight log say the same thing in the same words. Nothing here is "skill":
/// a <b>fighting skill</b> is a separate concept (see <c>FightingSkill</c>) and keeps that name; what
/// gains experience is always a <b>modus mentis</b>.</para>
/// </summary>
/// <param name="ModusMentis">The instance that was awarded (or would have been).</param>
/// <param name="Landed">False when the modus mentis sits at its organ-derived ceiling and earned nothing.</param>
/// <param name="Levelled">True when this point of experience filled the bar and raised the level.</param>
public readonly record struct ModusMentisXpAward(
    ModusMentis ModusMentis, bool Landed, bool Levelled, int Level, int CurrentXp, int Threshold)
{
    /// <summary>An award that did nothing — the capped case.</summary>
    public static ModusMentisXpAward None(ModusMentis mm) => new(mm, false, false, mm.Level, mm.CurrentXp, 0);

    /// <summary>
    /// The one line describing this award, or the empty string when nothing was gained (a capped
    /// modus mentis must show no chip and no log line — otherwise every action would end with a
    /// note about how you cannot get any better at walking).
    /// </summary>
    public string Describe() =>
        !Landed  ? string.Empty
        : Levelled ? $"Modus mentis improved: {ModusMentis.DisplayName} — level {Level}"
                   : $"Modus mentis practised: {ModusMentis.DisplayName} ({CurrentXp}/{Threshold} xp)";

    /// <summary>The same line attributed to a fighter, for the fight log.</summary>
    public string DescribeFor(string who) => Landed ? $"{who} — {Describe()}" : string.Empty;
}

/// <summary>
/// ModusMentis functions determine when and how a modusMentis is used.
/// ModiMentis can have multiple functions.
/// </summary>
public enum ModusMentisFunction
{
    Observation,   // Generates perceptions of environment
    Thinking,      // Generates reasoning and actions (CoT)
    Action,        // Used for modusMentis checks when executing actions
    Speaking,      // Generates player dialogue replicas in conversation
    Fighting,      // Declarative: this modusMentis unlocks fighting skills (main or secondary)
    Emotion,       // Reacts to what an action produced: declares outcome types that move the spleen
    Will           // Observes and thinks toward one target action only (ModusMentis.Will, ActionPathPlanner)
}
