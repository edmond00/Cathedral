using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that records (or escalates) a witnessed crime in the NPC's affinity table.
/// Only upgrades the criminal record — never downgrades it.
/// </summary>
public class CriminalAffinityOutcome : IDialogueOutcome
{
    private readonly CriminalAffinityType _crime;

    public CriminalAffinityOutcome(CriminalAffinityType crime) => _crime = crime;

    public string Description => $"NPC records crime: {_crime}";

    public OutcomeReport? Apply(NpcEntity npc, string partyMemberId)
    {
        npc.AffinityTable.RecordCrime(partyMemberId, _crime);
        return DialogueOutcomeReports.Relation(
            $"{npc.DisplayName} holds you a {_crime.ToString().ToLower()}", OutcomeReportSeverity.Negative);
    }
}
