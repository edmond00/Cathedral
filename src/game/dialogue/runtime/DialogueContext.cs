using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// The live context of a single conversation, supplying values for <c>{scope:field}</c> template
/// tokens (see <see cref="DialogueTemplate"/>) and the placeholder ↔ real name mapping
/// (<see cref="DialogueNameTable"/>). Built once per session and reused for every replica rewrite.
/// </summary>
public class DialogueContext
{
    /// <summary>The speaking party member (the "you" role).</summary>
    public PartyMember Pc { get; }

    /// <summary>The interlocutor (the "npc" role).</summary>
    public NpcEntity Npc { get; }

    /// <summary>Where the conversation happens (may be null when unknown); drives <c>{npc:location}</c>.</summary>
    public WorldContext? World { get; }

    /// <summary>Location vertex id, for world-context flavouring.</summary>
    public int LocationId { get; }

    /// <summary>The party member's current affinity level with the NPC; drives <c>{npc:relation}</c>.</summary>
    public AffinityLevel Relation { get; }

    /// <summary>Placeholder ↔ real name mapping for this conversation.</summary>
    public DialogueNameTable Names { get; }

    /// <summary>
    /// A third person the conversation is <i>about</i> rather than <i>with</i> — the master an
    /// apprentice can introduce you to, the reeve a farmhand can put you in front of. Null for every
    /// conversation that concerns only the two people in it, which is most of them.
    ///
    /// <para>Drives the <c>{third:*}</c> token family. Kept separate from <see cref="Npc"/> because
    /// the two are simultaneously live: the apprentice is speaking and the blacksmith is the subject,
    /// and a tree that conflated them would have the apprentice offering to introduce you to
    /// himself.</para>
    /// </summary>
    public NpcEntity? ThirdParty { get; set; }

    public DialogueContext(PartyMember pc, NpcEntity npc, WorldContext? world, int locationId, DialogueNameTable names)
    {
        Pc         = pc;
        Npc        = npc;
        World      = world;
        LocationId = locationId;
        Names      = names;
        Relation   = npc.AffinityTable.GetLevel(pc.AffinityKey);
    }
}
