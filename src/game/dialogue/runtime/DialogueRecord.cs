using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>How a conversation ended.</summary>
public enum DialogueEnding
{
    /// <summary>Its branch was rolled and won.</summary>
    Success,
    /// <summary>Its branch was rolled and lost.</summary>
    Failure,
    /// <summary>Walked away from before anything was settled.</summary>
    Interrupted,
    /// <summary>It ended with no branch resolved: an error, or a tree with nothing to say.</summary>
    Unsettled,
}

/// <summary>
/// What a conversation was, once it is over: who it was with, what the protagonist said — the
/// replies chosen, in order, as they were worded — how it ended, and what it changed. The
/// conversation's own screen is gone the moment it closes, so this is what the narration keeps of
/// it: one node on the thought tree, under the act that opened the conversation, read back under
/// the pointer.
///
/// <para>The reports are the very <see cref="Outcome"/>s the conversation applied, so the chips
/// read here are the ones its screen showed.</para>
/// </summary>
public sealed class DialogueRecord
{
    public NpcEntity Npc { get; }
    public DialogueEnding Ending { get; set; } = DialogueEnding.Unsettled;

    private readonly List<string> _replies = new();
    private readonly List<Outcome> _reports = new();

    /// <summary>The protagonist's replies, in the order they were said.</summary>
    public IReadOnlyList<string> Replies => _replies;

    /// <summary>What the conversation changed, as the chips its screen showed.</summary>
    public IReadOnlyList<Outcome> Reports => _reports;

    public DialogueRecord(NpcEntity npc) => Npc = npc;

    public void Said(string reply) => _replies.Add(reply);

    public void Came(IEnumerable<Outcome> reports)
    {
        foreach (var report in reports)
            if (report.ShowInUI) _reports.Add(report);
    }

    /// <summary>The ending in a word, for the reading box and the CLI.</summary>
    public string EndingWord => Ending switch
    {
        DialogueEnding.Success     => "success",
        DialogueEnding.Failure     => "failure",
        DialogueEnding.Interrupted => "interrupted",
        _                          => "nothing settled",
    };
}
