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
    /// </summary>
    public int GetTotalModusMentisLevel()
    {
        int total = ChainModusMentis?.Level ?? 0;
        var current = ChainOrigin;
        while (current != null)
        {
            total += current.ChainModusMentis?.Level ?? 0;
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
/// Tracks the current state of narration flow.
/// Manages history of narration blocks, current node, thinking attempts, etc.
/// </summary>
public class NarrationState
{
    public string CurrentNodeId { get; set; } = "";
    public int ThinkingAttemptsRemaining { get; set; } = 3;
    public string? SelectedKeyword { get; set; }
    public ModusMentis? SelectedThinkingModusMentis { get; set; }
    public List<NarrationBlock> NarrationHistory { get; } = new();
    
    public void AddBlock(NarrationBlock block)
    {
        NarrationHistory.Add(block);
    }
    
    public void ClearHistory()
    {
        NarrationHistory.Clear();
    }
    
    public List<string> GetAllKeywords()
    {
        return NarrationHistory
            .Where(b => b.Keywords != null)
            .SelectMany(b => b.Keywords!)
            .Distinct()
            .ToList();
    }
}

/// <summary>
/// A single observation sentence, its keyword(s), and the thing they act on.
/// Stored on an observation NarrationBlock so the scroll buffer can assign each keyword
/// only to the wrapped lines of its own sentence, preventing cross-sentence highlighting.
///
/// <para><b>The anchor lives here, not in a per-block table keyed by the word.</b> It used to be
/// resolved at click time through a <c>Dictionary&lt;string, NarrativeAnchor&gt;</c>, which can hold
/// one object per word — so two sentences about two men could not both say "man", and the second
/// was pushed onto whatever its associated words offered ("thing"). Every object needed a supply of
/// second-best words purely to keep that table's keys distinct. Carrying the anchor on the sentence
/// that produced it removes the constraint: the keyword is resolved by <i>which occurrence was
/// clicked</i>, which the renderer has always known, so identical words in different sentences are
/// simply different links.</para>
/// </summary>
public record NarrationSentence(string Text, List<string> Keywords, NarrativeAnchor? Anchor = null);

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
    public List<string>? Keywords { get; init; }               // Highlighted keywords (if observation, max 1 per sentence)
    public List<ParsedNarrativeAction>? Actions { get; init; } // Clickable actions (if thinking)

    /// <summary>
    /// Per-sentence breakdown for observation blocks. When set, each sentence's keyword is
    /// highlighted only within that sentence's wrapped lines — not across the whole block text.
    /// Parallel to <see cref="Keywords"/>: Sentences[i].Keyword == Keywords[i].
    /// </summary>
    public List<NarrationSentence>? Sentences { get; init; } = null;
    
    /// <summary>
    /// For observation blocks, indicates if this is an overall or focus observation.
    /// </summary>
    public ObservationType? SourceObservationType { get; init; } = null;

    /// <summary>
    /// The concrete outcome this whole block is about, for blocks that are about exactly one thing
    /// and carry no <see cref="Sentences"/>. A multi-sentence observation puts the anchor on each
    /// <see cref="NarrationSentence"/> instead, and a click resolves through the region it landed
    /// on; this is the fallback for everything that has no per-sentence breakdown.
    /// </summary>
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
        List<string>? Keywords,
        List<ParsedNarrativeAction>? Actions,
        ModusMentisChainElement? ChainOrigin = null,
        ObservationType? SourceObservationType = null,
        NarrativeAnchor? LinkedOutcome = null,
        List<NarrationSentence>? Sentences = null,
        string? SpeakerName = null,
        IReadOnlyList<Outcome>? OutcomeReports = null,
        IReadOnlyList<DialogueOptionItem>? DialogueOptions = null)
    {
        this.Type = Type;
        this.ModusMentis = ModusMentis;
        this.Text = Text;
        this.Keywords = Keywords;
        this.Actions = Actions;
        this.ChainOrigin = ChainOrigin;
        this.SourceObservationType = SourceObservationType;
        this.LinkedOutcome = LinkedOutcome;
        this.Sentences = Sentences;
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
    DialogueOptions // Group of selectable player replies, rendered inline in the scroll buffer
}

/// <summary>
/// Types of observation blocks in the narration system.
/// Used to determine if a keyword came from an overall or focus observation.
/// </summary>
public enum ObservationType
{
    /// <summary>The first observation generated when entering a node.</summary>
    Overall,
    /// <summary>A detailed observation generated by right-clicking a keyword.</summary>
    Focus
}

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
    /// ("It is done! I succeeded to …" / "Alas, I failed to …") templates cleanly instead of
    /// re-embedding an already-styled phrase that does not fit it. Empty for actions with no
    /// neutral source.
    /// </summary>
    public string NeutralActionText { get; set; } = "";
    public string ActionModusMentisId { get; set; } = "";           // Which action modusMentis to use for check
    public ModusMentis? ActionModusMentis { get; set; }                   // Resolved modusMentis reference
    public ModusMentis ThinkingModusMentis { get; set; } = null!;         // Which thinking modusMentis generated this
    public VerbAction PreselectedOutcome { get; set; } = null!;  // Success outcome chosen by thinking modusMentis
    public Verb Verb => PreselectedOutcome.Verb;
    public string Keyword { get; set; } = "";                 // Keyword this action relates to

    /// <summary>
    /// Item combined with this action via the action popup's "Use Tool" row.
    /// Null when no item is combined. When set, the action text has been reformulated
    /// by the action modusMentis to incorporate the item, and dice rolls receive a bonus
    /// equal to the item's whole UsageLevel. The hands bear on <i>whether</i> a combination is
    /// permitted (<see cref="ToolUsageProficiencyStat"/>) rather than on how much it lends.
    /// </summary>
    public Item? CombinedItem { get; set; } = null;

    /// <summary>
    /// Difficulty level pre-computed after the thinking phase (1–10 scale).
    /// 0 means not yet evaluated.
    /// </summary>
    public int DifficultyLevel { get; set; } = 0;

    /// <summary>
    /// Set to true when this action has been judged IMPOSSIBLE by the critic LLM or a coded rule.
    /// Causes the action header and text to render greyed-out in the narration UI.
    /// </summary>
    public bool IsImpossible { get; set; } = false;

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
    /// The glyph opening this action's line: the verb's override when it has one (REMEMBER uses '○',
    /// having no normal difficulty), else the difficulty glyph, else '>' before evaluation.
    /// </summary>
    public char DifficultyGlyph
        => PreselectedOutcome?.Verb?.DifficultyGlyphOverride
           ?? (DifficultyLevel > 0
               ? Config.Symbols.DifficultyGlyphs[Math.Clamp(DifficultyLevel, 1, 10) - 1]
               : '>');

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
