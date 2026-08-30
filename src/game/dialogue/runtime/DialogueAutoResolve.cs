using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// <c>--auto-dialogue</c>: settle a conversation without holding one.
///
/// <para><b>What it is for.</b> A dozen verbs do nothing themselves — <c>meet_stranger</c>,
/// <c>introduce_me</c>, <c>request_job</c>, <c>propose_to_buy</c>, <c>provoke</c>, <c>reconcile</c>
/// and the rest only <i>open a conversation</i>, and what happens next belongs to the tree. A test
/// for the verb therefore has to walk somebody else's dialogue to reach its own assertion, and every
/// such test breaks whenever that tree is re-authored — a branch renamed in <c>request_job</c> should
/// not fail the <c>request_job</c> <i>verb</i> test.</para>
///
/// <para>So this applies what winning the conversation would have applied and skips the conversation.
/// The verb test then asserts about the verb, and the trees are covered separately by
/// <c>cli/_systems/dialogue_*.cli</c>, which drive them properly.</para>
///
/// <para><b>It is a shortcut, not a lie.</b> It performs exactly the writes
/// <c>DialogueTreeController</c> makes when a branch resolves in the player's favour — the tree's
/// success outcomes, the lesson it teaches, and the first-contact affinity stamp — in that order.
/// What it skips is the talking. Anything that reads the world afterwards cannot tell the
/// difference, which is the property that makes it usable in a test.</para>
///
/// <para>Inert unless the flag is passed.</para>
/// </summary>
public static class DialogueAutoResolve
{
    /// <summary>
    /// Applies the outcome of a won conversation with <paramref name="npc"/>, and reports what it did.
    /// Returns false when the tree cannot be resolved, so the caller can fall back to the real thing
    /// rather than silently skipping a conversation that was supposed to happen.
    /// </summary>
    public static bool TryResolve(
        NpcEntity      npc,
        Protagonist    protagonist,
        string?        treeId,
        DialogueTree?  prebuiltTree)
    {
        var tree = prebuiltTree ?? (treeId != null ? DialogueTreeRegistry.Instance.TryGet(treeId) : null);
        if (tree == null)
        {
            Console.Error.WriteLine(
                $"[debug] --auto-dialogue: no tree resolved for '{treeId ?? "(none)"}' — holding the conversation instead.");
            return false;
        }

        string partyMemberId = protagonist.AffinityKey;
        var applied = new List<string>();

        // The same three writes DialogueTreeController makes on a successful resolution, in the same
        // order: the tree's outcomes, then what it teaches, then the first-contact stamp (which only
        // fires if no outcome already set a level).
        foreach (var outcome in tree.SuccessOutcomes)
        {
            outcome.ApplyTo(OutcomeContext.ForDialogue(npc, partyMemberId, protagonist));
            applied.Add(outcome.DisplayName);
        }

        // Only the tree's own lesson. AdditionalGrantedModusMentisIds is keyed on WHICH branch was
        // taken, and no branch was taken here — inventing one would teach something the conversation
        // never reached.
        foreach (var id in new[] { tree.GrantedModusMentisId }.Distinct())
        {
            var lesson = ModusMentisGrantOutcome.For(protagonist, id);
            if (lesson == null) continue;
            lesson.ApplyTo(OutcomeContext.For(protagonist, null, null));
            applied.Add($"learned {id}");
        }

        npc.AffinityTable.MarkFirstContact(partyMemberId);

        Console.WriteLine($"[debug] --auto-dialogue: '{tree.TreeId}' with {npc.DisplayName} settled as a success"
                          + (applied.Count == 0 ? " (no outcomes)" : $" — {string.Join("; ", applied)}"));
        return true;
    }
}
