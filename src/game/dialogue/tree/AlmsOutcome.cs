using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Records that an NPC gave the player a coin or two.
///
/// <para>Sets an amount on the NPC rather than crediting the wallet, because an
/// <see cref="IDialogueOutcome"/> is handed the NPC and the party-member id and nothing else — no
/// scene, no protagonist, no purse. The controller pays it out when the session closes, the same way
/// it opens the trade menu and the work menu.</para>
///
/// <para>Deliberately small. Begging is meant to keep somebody alive, not to be an income.</para>
/// </summary>
public class AlmsOutcome : IDialogueOutcome
{
    private readonly int _copper;

    public AlmsOutcome(int copper = 2) => _copper = copper;

    public string Description => $"the NPC gives {_copper} copper";

    public OutcomeReport? Apply(NpcEntity npc, string partyMemberId)
    {
        npc.AlmsGiven = _copper;
        return DialogueOutcomeReports.Relation(
            $"{npc.DisplayName} gives you {_copper} copper", OutcomeReportSeverity.Positive);
    }
}
