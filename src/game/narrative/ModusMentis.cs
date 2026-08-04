using System;
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
    /// </summary>
    public abstract string SkillMeans { get; }

    /// <summary>
    /// Player-facing manual entry shown in the memory menu's detail box — a factual,
    /// third-person explanation of what this modusMentis governs and when it helps.
    /// Unlike <see cref="SkillMeans"/> / <see cref="PersonaTone"/> this is NEVER fed to the LLM,
    /// so it is written as neutral game documentation rather than in-character flavour.
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
    /// A discrete modus mentis is one step harder to notice: it prepends "discretely" to its action
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
    /// Used as a quick reminder of the modusMentis's personality in individual LLM calls.
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
    /// The base value preserves the old generic behaviour for modiMentis without a persona.
    /// </summary>
    public virtual string StyleInstruction =>
        "Where it fits, a figure of speech (metaphor, comparison, imagery) or an inner feeling that suits your character is welcome.";
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
    Fighting       // Declarative: this modusMentis unlocks fighting skills (main or secondary)
}
