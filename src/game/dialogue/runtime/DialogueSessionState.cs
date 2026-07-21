using System.Collections.Generic;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Runtime;

// ── Player option ─────────────────────────────────────────────────────────────

/// <summary>One generated player reply option shown during option-selection phase.</summary>
public class PlayerReplicaOption
{
    /// <summary>The Modus Mentis that voiced this option; its level feeds the branch dice pool.</summary>
    public ModusMentis  Skill       { get; }

    /// <summary>The authored option chosen; <see cref="PlayerOption.Next"/> is where selecting it leads.</summary>
    public PlayerOption Option      { get; }

    /// <summary>The persona-rewritten line shown to the player (real names restored).</summary>
    public string       ReplicaText { get; }

    public PlayerReplicaOption(ModusMentis skill, PlayerOption option, string replicaText)
    {
        Skill       = skill;
        Option      = option;
        ReplicaText = replicaText;
    }
}

// ── Session state ─────────────────────────────────────────────────────────────

/// <summary>
/// Mutable live state for a single dialogue session.
/// Owned by <see cref="DialogueTreeController"/>.
/// </summary>
public class DialogueSessionState
{
    // ── Loading flags ─────────────────────────────────────────────────────────
    public bool IsLoadingNpcReplica  { get; set; }
    public bool IsLoadingOptions     { get; set; }
    public bool IsLoadingReaction    { get; set; }
    public int  OptionsLoaded        { get; set; }
    public int  OptionsTotal         { get; set; }

    // ── Dice roll ─────────────────────────────────────────────────────────────
    public bool   IsDiceRollActive    { get; set; }
    public int    DiceCount           { get; set; }
    public int    DiceDifficulty      { get; set; }
    public bool   IsDiceRolling       { get; set; }
    public int[]? DiceFinalValues     { get; set; }
    public bool   DiceSucceeded       { get; set; }
    public bool   IsContinueHovered   { get; set; }

    public void StartDiceRoll(int count, int difficulty)
    {
        IsDiceRollActive  = true;
        DiceCount         = count;
        DiceDifficulty    = Math.Clamp(difficulty, 1, Math.Max(1, count));
        IsDiceRolling     = true;
        DiceFinalValues   = null;
        DiceSucceeded     = false;
        IsContinueHovered = false;
    }

    public void CompleteDiceRoll(int[] finalValues)
    {
        IsDiceRolling   = false;
        DiceFinalValues = finalValues;
        int sixes = System.Linq.Enumerable.Count(finalValues, v => v == 6);
        DiceSucceeded = sixes >= DiceDifficulty;
    }

    public void ClearDiceRoll()
    {
        IsDiceRollActive  = false;
        DiceCount         = 0;
        DiceDifficulty    = 1;
        IsDiceRolling     = false;
        DiceFinalValues   = null;
        DiceSucceeded     = false;
        IsContinueHovered = false;
    }

    // ── Content ───────────────────────────────────────────────────────────────
    // Spoken lines live in the narration session's shared NarrationScrollBuffer, which also owns the
    // scroll position — only the selectable replies are session-local.
    public List<PlayerReplicaOption>  Options { get; set; } = new();

    // ── Selection ─────────────────────────────────────────────────────────────
    public int HoveredOptionIndex { get; set; } = -1;

    // ── Footer exit button (LEAVE / INTERRUPT) ────────────────────────────────
    /// <summary>Click region of the footer button; default (Width 0) while hidden.</summary>
    public (int X, int Y, int Width) ExitButtonRegion { get; set; }
    public bool IsExitButtonHovered { get; set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    public bool ConversationEnded { get; set; }
    public bool RequestedExit     { get; set; }
    public string? ErrorMessage   { get; set; }

    // ── Pending result (computed in parallel with dice animation) ─────────────
    public bool?   PendingSucceeded     { get; set; }
    public string? PendingNpcReaction   { get; set; }

    public void Clear()
    {
        IsLoadingNpcReplica = false;
        IsLoadingOptions    = false;
        IsLoadingReaction   = false;
        OptionsLoaded       = 0;
        OptionsTotal        = 0;
        ClearDiceRoll();
        Options.Clear();
        HoveredOptionIndex  = -1;
        ExitButtonRegion    = default;
        IsExitButtonHovered = false;
        ConversationEnded   = false;
        RequestedExit       = false;
        ErrorMessage        = null;
        PendingSucceeded    = null;
        PendingNpcReaction  = null;
    }
}
