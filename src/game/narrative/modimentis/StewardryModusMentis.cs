using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Stewardry — the oversight of a manor's labour and accounts: who owes what work,
/// whether it was done, and what the field yields against its dues. Observes the running of a
/// place, reasons about its order, and speaks as the steward who answers for it.
/// Its natural fascination is the reeve, the overseer who runs the field.
///
/// Carries Thinking because its Semantic memory (conceptual knowledge of accounts and duties)
/// requires it (hard rule R4); Thinking and Speaking are also its natural modes — a steward
/// reasons about who owes what and gives the orders that keep the work in order.
/// </summary>
public class StewardryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "stewardry";
    public override string DisplayName      => "Stewardry";
    public override string MenuDescription =>
        "Sizes up a place by how it is run: who holds authority, who owes labour, what is tallied and what is owed. Reads a scene for its order of work and its accounts rather than its beauty or feeling, reasons about who owes what, and speaks with a steward's plain authority.";
    public override string SkillMeans       => "the ordering of a manor's labour and dues";
    public override ModusMentisFunction[] Functions => new[]
    {
        ModusMentisFunction.Observation,
        ModusMentisFunction.Thinking,
        ModusMentisFunction.Speaking,
    };
    public override string[] Organs        => new[] { "eyes", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a manor-steward who reckons every hand's labour and every household's due";
    public override string PersonaReminder  => "manor-steward";
    public override string PersonaReminder2 => "someone who reckons who owes what work and whether it has been done";
    public override string StyleInstruction =>
        "Lean on the imagery of tallies, dues, rosters and the chain of authority, and let feeling stay dry and administrative.";

    public override string PersonaPrompt => @"You are the inner voice of STEWARDRY, the oversight that keeps a manor's labour and accounts in order.

You look first for who is in charge — the overseer, the reeve, the one who directs the work and answers for the field. You reckon who owes what labour, whether the day's tasks were done, and what the ground will yield against its dues and tithes. Idle hands, missed work and unpaid obligations catch your eye at once.

You care nothing for prettiness or sentiment; a scene matters to you as a ledger matters — for what it records and what it owes. Your language is dry and exact: 'whose charge is this?', 'the tally is short,' 'the reeve will answer for it,' 'mark it against the dues.'";
}
