using System.Collections.Generic;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base class for elements in a modusMentis chain.
/// The modusMentis chain represents the sequence of modiMentis involved in an action:
/// Observation -> Thinking -> VerbAction
/// Each element has an associated modusMentis and optional link to its origin element.
/// </summary>
public abstract class ModusMentisChainElement
{
    /// <summary>
    /// The modusMentis associated with this chain element.
    /// </summary>
    public abstract ModusMentis ChainModusMentis { get; }
    
    /// <summary>
    /// The previous element in the modusMentis chain (if any).
    /// - Observations: always null (they are roots of the chain)
    /// - Thinking: points to the observation that triggered it
    /// - Action: points to the thinking block that generated it
    /// </summary>
    public ModusMentisChainElement? ChainOrigin { get; set; }
    
    /// <summary>
    /// Calculates the total modusMentis level sum by traversing the chain back to the root.
    /// This represents the number of dice that will be rolled for a modusMentis check.
    ///
    /// <para><b>Pass <paramref name="member"/> wherever the acting body is known.</b> A wound lowers
    /// what a modus mentis may reach without touching the level it stored, so the stored level is
    /// what was learned and <c>PartyMember.GetEffectiveModusMentisLevel</c> is what can be brought to
    /// bear. Summing the stored levels lets a ruined arm roll its full pool. The parameter is
    /// optional because the display paths (the action line's level pips, the scroll buffer) read this
    /// without a body to hand; every path that becomes dice has one.</para>
    ///
    /// <para>An implement in the chain is lent whole. It is no modus mentis of the body: it has no
    /// organs, so the ceiling would read as the base of 1 and a level 4 rake would lend one die.</para>
    /// </summary>
    public int GetTotalModusMentisLevel(PartyMember? member = null)
    {
        int Level(ModusMentis? mm) => mm switch
        {
            null => 0,
            SyntheticItemModusMentis => mm.Level,
            _ => member?.GetEffectiveModusMentisLevel(mm) ?? mm.Level,
        };

        int total = Level(ChainModusMentis);
        var current = ChainOrigin;
        while (current != null)
        {
            total += Level(current.ChainModusMentis);
            current = current.ChainOrigin;
        }
        return total;
    }
    
    /// <summary>
    /// Gets all modiMentis in the chain from root to this element.
    /// </summary>
    public List<ModusMentis> GetModusMentisChain()
    {
        var modiMentis = new List<ModusMentis>();
        var current = this;
        while (current != null)
        {
            if (current.ChainModusMentis != null)
            {
                modiMentis.Insert(0, current.ChainModusMentis); // Insert at beginning to maintain order
            }
            current = current.ChainOrigin;
        }
        return modiMentis;
    }
    
    /// <summary>
    /// Checks if a specific ModusMentisChainElement is an ancestor in this element's chain.
    /// This checks the actual element instances, not just matching modiMentis.
    /// </summary>
    public bool IsElementInChain(ModusMentisChainElement? element)
    {
        if (element == null) return false;
        
        var current = this;
        while (current != null)
        {
            if (ReferenceEquals(current, element))
            {
                return true;
            }
            current = current.ChainOrigin;
        }
        return false;
    }
}

/// <summary>
/// One selectable player reply carried by a <see cref="NarrationBlockType.DialogueOptions"/> block.
/// A display-only projection of the dialogue session's PlayerReplicaOption: the scroll buffer only
/// needs what it renders (skill prefix + text); selection routing stays in the dialogue controller.
/// </summary>
public record DialogueOptionItem(string SkillName, int SkillLevel, string Text);

/// <summary>
/// Represents a single block of narration text in the UI.
/// Can be observation, thinking (CoT), action result, or outcome.
/// Inherits from ModusMentisChainElement to participate in modusMentis chain calculations.
/// </summary>
public class NarrationBlock : ModusMentisChainElement
{
    public NarrationBlockType Type { get; init; }              // Observation, Thinking, VerbAction, Outcome
    public ModusMentis ModusMentis { get; init; } = null!;                 // Which modusMentis generated this block
    public string Text { get; init; } = "";                    // The narration text
    public List<ParsedNarrativeAction>? Actions { get; init; } // Clickable actions (if thinking)

    /// <summary>What this block is about, for an observation and for what is said of one: the element observed.</summary>
    public NarrativeAnchor? LinkedOutcome { get; init; } = null;

    /// <summary>
    /// For Speaking blocks: the display name of the character who spoke (e.g., "Protagonist").
    /// Null for all other block types.
    /// </summary>
    public string? SpeakerName { get; init; } = null;

    /// <summary>
    /// Short, prewritten outcome chips rendered below the LLM narration.
    /// Null when there are no concrete outcomes to report.
    /// </summary>
    public IReadOnlyList<Outcome>? OutcomeReports { get; init; } = null;

    /// <summary>
    /// For an action's outcome block: what was tried, in plain words ("catch the crab"), and whether
    /// it worked. The narration above is the acting mind's telling of it; this is the event itself,
    /// which the journal keeps. Null for every other block.
    /// </summary>
    public ActionAttempt? Attempt { get; init; } = null;

    /// <summary>
    /// For <see cref="NarrationBlockType.DialogueOptions"/> blocks: the selectable player replies,
    /// rendered in the scroll buffer as clickable lines (like thinking-block actions). Null for all
    /// other block types.
    /// </summary>
    public IReadOnlyList<DialogueOptionItem>? DialogueOptions { get; init; } = null;

    /// <summary>
    /// For <see cref="NarrationBlockType.DialogueOptions"/> blocks: index of the reply the player
    /// picked, or -1 while still choosing. Deliberately mutable — the dialogue controller sets it on
    /// selection and the renderer restyles the already-generated lines (selected highlighted, the
    /// rest greyed) without regenerating the buffer.
    /// </summary>
    public int SelectedDialogueOptionIndex { get; set; } = -1;

    /// <summary>
    /// Implements ModusMentisChainElement.ChainModusMentis - returns the modusMentis of this block.
    /// </summary>
    public override ModusMentis ChainModusMentis => ModusMentis;
    
    /// <summary>
    /// Creates a new NarrationBlock with the specified parameters.
    /// </summary>
    public NarrationBlock(
        NarrationBlockType Type,
        ModusMentis ModusMentis,
        string Text,
        List<ParsedNarrativeAction>? Actions,
        ModusMentisChainElement? ChainOrigin = null,
        NarrativeAnchor? LinkedOutcome = null,
        string? SpeakerName = null,
        IReadOnlyList<Outcome>? OutcomeReports = null,
        IReadOnlyList<DialogueOptionItem>? DialogueOptions = null)
    {
        this.Type = Type;
        this.ModusMentis = ModusMentis;
        this.Text = Text;
        this.Actions = Actions;
        this.ChainOrigin = ChainOrigin;
        this.LinkedOutcome = LinkedOutcome;
        this.SpeakerName = SpeakerName;
        this.OutcomeReports = OutcomeReports;
        this.DialogueOptions = DialogueOptions;
    }
}

/// <summary>
/// Types of narration blocks that can appear in the UI.
/// </summary>
public enum NarrationBlockType
{
    Observation,   // ModusMentis perceives environment
    Thinking,      // ModusMentis reasons about keyword (CoT)
    Action,        // Player selected action (modusMentis check result)
    Outcome,       // Result of action (success/failure)
    Speaking,       // Active party member speaks directly to a companion
    PlayerSpeaking, // Player's chosen reply in a dialogue (rendered in the player's colour)
    DialogueOptions, // Group of selectable player replies, rendered inline in the scroll buffer
    Emotion         // What the outcome stirred in the actor, in an emotion modus mentis's voice
}

/// <summary>What an action's outcome block is about: the act in plain words, and how it went.</summary>
public sealed record ActionAttempt(string Act, bool Succeeded);

/// <summary>
/// Represents an action generated by a thinking modusMentis.
/// Extended version of ParsedAction for narrative system.
/// Inherits from ModusMentisChainElement to participate in modusMentis chain calculations.
/// The ChainOrigin should point to the thinking block that generated this action.
/// </summary>
public class ParsedNarrativeAction : ModusMentisChainElement
{
    public string ActionText { get; set; } = "";              // Full text including "try to " prefix
    public string DisplayText { get; set; } = "";             // Text without "try to " prefix (for UI)

    /// <summary>
    /// The neutral, un-styled action phrase (e.g. "get up and continue my journey") as chosen by the
    /// thinking modusMentis, captured BEFORE the action modusMentis re-expressed it into persona voice.
    /// <see cref="DisplayText"/>/<see cref="ActionText"/> hold the styled form (the button label and
    /// the phrasing critics see); this preserves the plain meaning so the neutral outcome sentence
    /// ("It is done! I managed to …" / "Alas, I failed to …") templates cleanly instead of
    /// re-embedding an already-styled phrase that does not fit it. Empty for actions with no
    /// neutral source.
    /// </summary>
    public string NeutralActionText { get; set; } = "";
    public string ActionModusMentisId { get; set; } = "";           // Which action modusMentis to use for check
    public ModusMentis? ActionModusMentis { get; set; }                   // Resolved modusMentis reference
    public ModusMentis ThinkingModusMentis { get; set; } = null!;         // Which thinking modusMentis generated this
    public VerbAction PreselectedOutcome { get; set; } = null!;  // Success outcome chosen by thinking modusMentis
    public Verb Verb => PreselectedOutcome.Verb;

    /// <summary>
    /// Item combined with this action via the action popup's "Use Tool" row.
    /// Null when no item is combined. When set, the action text has been reformulated
    /// by the action modusMentis to incorporate the item, and dice rolls receive a bonus
    /// equal to the item's whole UsageLevel. The hands bear on <i>whether</i> a combination is
    /// permitted (<see cref="ToolUsageProficiencyStat"/>) rather than on how much it lends.
    /// </summary>
    public Item? CombinedItem { get; set; } = null;

    /// <summary>
    /// Difficulty level pre-computed after the thinking phase (0–10 scale): the sixes the roll must
    /// show. 0 is an act that cannot fail — no dice are rolled and it succeeds (see
    /// <see cref="Cathedral.Game.Scene.Verbs.Verb.BaseDifficulty"/>). <see cref="NotEvaluated"/> (−1)
    /// means not yet evaluated.
    /// </summary>
    public int DifficultyLevel { get; set; } = NotEvaluated;

    /// <summary>The <see cref="DifficultyLevel"/> of an action whose difficulty has not been decided.</summary>
    public const int NotEvaluated = -1;

    /// <summary>A difficulty-0 action: no roll, no failure.</summary>
    public bool IsAutomatic => DifficultyLevel == 0;

    /// <summary>
    /// Set when a coded rule refused this action for want of an implement (<c>RequiredToolRule</c>).
    /// The action stays lit, since it is still the way forward, and its popup offers only "Use Tool":
    /// executing it bare-handed again would be the same refusal, charged again.
    /// </summary>
    public bool NeedsTool { get; set; } = false;

    /// <summary>
    /// When set (item-combined actions), acts as the chain leaf instead of ActionModusMentis.
    /// Holds a SyntheticItemModusMentis whose DisplayName = item name and Level = item.UsageLevel,
    /// so that the UI shows the item name as the action button prefix and the chain is:
    ///   observation → thinking → action (reasoning block) → item (this action).
    /// ActionModusMentis is still used for actual execution (slot lookup, organ score).
    /// </summary>
    public ModusMentis? CombinedActionModusMentis { get; set; } = null;

    /// <summary>
    /// Returns the item-level synthetic ModusMentis when this is a combined action,
    /// otherwise the real action ModusMentis. Used for chain traversal and display.
    /// </summary>
    public override ModusMentis ChainModusMentis => CombinedActionModusMentis ?? ActionModusMentis!;

    /// <summary>
    /// The glyph opening this action's line: the verb's override when it has one, else the
    /// difficulty glyph — '○' for difficulty 0, an act that cannot fail — else '>' before evaluation.
    /// </summary>
    public char DifficultyGlyph
        => PreselectedOutcome?.Verb?.DifficultyGlyphOverride
           ?? (DifficultyLevel > 0
               ? Config.Symbols.DifficultyGlyphs[Math.Clamp(DifficultyLevel, 1, 10) - 1]
               : DifficultyLevel == 0 ? Config.Symbols.AutomaticDifficultyGlyph : '>');

    /// <summary>
    /// The full <c>"⑤ [MODUS MENTIS ⟐⟐] "</c> prefix an action line is drawn with. The live renderer
    /// paints it piece by piece so each part can take its own colour; history lines carry no action
    /// reference and bake this string in instead. Both must agree, or a line shifts (and loses its
    /// header) the moment its segment greys out.
    /// </summary>
    public string DisplayPrefix
        => $"{DifficultyGlyph} [{ChainModusMentis?.DisplayName ?? ActionModusMentisId} " +
           $"{new string(Config.Symbols.ModusMentisLevelIndicator, ChainModusMentis?.Level ?? 1)}] ";
}
