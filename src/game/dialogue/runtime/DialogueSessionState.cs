using System.Collections.Generic;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Runtime;

// ── Log entry types ───────────────────────────────────────────────────────────

public enum DialogueLogEntryType { NpcSpeaking, PlayerReplica, SystemMessage, Separator }

public class DialogueLogEntry
{
    public DialogueLogEntryType Type    { get; }
    public string?              Speaker { get; }
    public string               Text    { get; }

    public DialogueLogEntry(DialogueLogEntryType type, string? speaker, string text)
    {
        Type    = type;
        Speaker = speaker;
        Text    = text;
    }
}

// ── Player option ─────────────────────────────────────────────────────────────

/// <summary>One generated player reply option shown during option-selection phase.</summary>
public class PlayerReplicaOption
{
    public ModusMentis      Skill        { get; }
    public DialogueTreeNode TargetNode   { get; }
    public string           ReplicaText  { get; }

    public PlayerReplicaOption(ModusMentis skill, DialogueTreeNode targetNode, string replicaText)
    {
        Skill       = skill;
        TargetNode  = targetNode;
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
    public List<DialogueLogEntry>     Log     { get; } = new();
    public List<PlayerReplicaOption>  Options { get; set; } = new();

    // ── Selection ─────────────────────────────────────────────────────────────
    public int HoveredOptionIndex { get; set; } = -1;

    // ── Scroll ────────────────────────────────────────────────────────────────
    public int ScrollOffset { get; set; }

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
        Log.Clear();
        Options.Clear();
        HoveredOptionIndex  = -1;
        ScrollOffset        = 0;
        ConversationEnded   = false;
        RequestedExit       = false;
        ErrorMessage        = null;
        PendingSucceeded    = null;
        PendingNpcReaction  = null;
    }
}
