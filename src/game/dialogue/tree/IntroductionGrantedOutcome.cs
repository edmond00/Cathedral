using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Records that the go-between agreed to present the player to the third party.
///
/// <para>Does two things, and the second is the point of the whole verb: it hands the third party to
/// the controller so the player can be walked to them, and it sets the player's standing with that
/// third party to <see cref="AffinityLevel.DistantAcquaintance"/> — the same level a successful
/// MEET STRANGER reaches. Being introduced <i>is</i> having met someone, so the player arrives with
/// that conversation already behind them, which is what makes finding a go-between worth the
/// trouble.</para>
/// </summary>
public class IntroductionGrantedOutcome : IDialogueOutcome
{
    public string Description => "the NPC agrees to present the player to a third party";

    public OutcomeReport? Apply(NpcEntity npc, string partyMemberId)
    {
        var third = npc.PendingIntroductionTarget;
        npc.PendingIntroductionTarget = null;
        if (third == null) return null;

        // No longer a stranger to them. Deliberately not higher: an introduction opens the door and
        // says nothing about what happens once you are through it.
        third.AffinityTable.SetLevel(partyMemberId, AffinityLevel.DistantAcquaintance);
        third.AffinityTable.MarkFirstContact(partyMemberId);

        npc.IntroductionGranted = third;   // the controller walks the player over

        return DialogueOutcomeReports.Relation(
            $"{npc.DisplayName} will present you to {third.DisplayName}", OutcomeReportSeverity.Positive);
    }
}
