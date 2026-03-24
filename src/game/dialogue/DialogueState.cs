using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Dialogue.Phases;

namespace Cathedral.Game.Dialogue;

/// <summary>
/// Tracks the live state of a dialogue session for rendering and input handling.
/// </summary>
public class DialogueState
{
    // ── Loading flags ─────────────────────────────────────────────────────────
    public bool IsLoadingGreeting  { get; set; }
    public bool IsLoadingReplicas  { get; set; }
    public bool IsLoadingResponse  { get; set; }
    public int  ReplicasLoaded     { get; set; }
    public int  ReplicasTotal      { get; set; }

    // ── Dice roll (N-dice system: roll DiceRollNumberOfDice d6, count 6s vs difficulty) ──
    public bool   IsDiceRollActive        { get; set; }
    public int    DiceRollNumberOfDice    { get; set; }
    public int    DiceRollDifficulty      { get; set; }
    public bool   IsDiceRolling           { get; set; }
    public int[]? DiceRollFinalValues     { get; set; }
    public bool   DiceRollSucceeded       { get; set; }
    public bool   IsDiceRollButtonHovered { get; set; }

    public void StartDiceRoll(int numberOfDice, int difficulty)
    {
        IsDiceRollActive        = true;
        DiceRollNumberOfDice    = numberOfDice;
        DiceRollDifficulty      = Math.Clamp(difficulty, 1, 10);
        IsDiceRolling           = true;
        DiceRollFinalValues     = null;
        DiceRollSucceeded       = false;
        IsDiceRollButtonHovered = false;
    }

    public void CompleteDiceRoll(int[] finalValues)
    {
        IsDiceRolling       = false;
        DiceRollFinalValues = finalValues;
        DiceRollSucceeded   = finalValues.Count(v => v == 6) >= DiceRollDifficulty;
    }

    public void ClearDiceRoll()
    {
        IsDiceRollActive        = false;
        DiceRollNumberOfDice    = 0;
        DiceRollDifficulty      = 1;
        IsDiceRolling           = true;
        DiceRollFinalValues     = null;
        DiceRollSucceeded       = false;
        IsDiceRollButtonHovered = false;
    }

    // ── Content ───────────────────────────────────────────────────────────────
    public string? NpcGreetingText   { get; set; }
    public string? NpcResponseText   { get; set; }
    public List<ReplicaOption> Replicas { get; set; } = new();

    // ── Selection ─────────────────────────────────────────────────────────────
    public int HoveredReplicaIndex   { get; set; } = -1;

    // ── Conversation lifecycle ────────────────────────────────────────────────
    public bool ConversationEnded    { get; set; }
    public bool RequestedExit        { get; set; }
    public string? ErrorMessage      { get; set; }

    public void Clear()
    {
        IsLoadingGreeting  = false;
        IsLoadingReplicas  = false;
        IsLoadingResponse  = false;
        ReplicasLoaded     = 0;
        ReplicasTotal      = 0;
        IsDiceRollActive        = false;
        DiceRollNumberOfDice    = 0;
        DiceRollDifficulty      = 1;
        IsDiceRolling           = false;
        DiceRollFinalValues     = null;
        DiceRollSucceeded       = false;
        IsDiceRollButtonHovered = false;
        NpcGreetingText    = null;
        NpcResponseText    = null;
        Replicas.Clear();
        HoveredReplicaIndex = -1;
        ConversationEnded  = false;
        RequestedExit      = false;
        ErrorMessage       = null;
    }
}
