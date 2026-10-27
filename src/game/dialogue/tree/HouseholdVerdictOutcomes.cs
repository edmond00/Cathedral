using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// A householder who found the player waiting in their house at night lets them stay. Raises
/// <see cref="Npc.NpcEntity.ToleratesTrespass"/>; the controller makes the house tolerated for the
/// rest of the visit once the conversation closes — an outcome can reach the NPC and nothing else.
/// </summary>
public class TrespassToleratedOutcome : Outcome
{
    public TrespassToleratedOutcome()
        : base("the householder lets the player stay", OutcomeSeverity.Positive, "was allowed to stay in the house") { }

    public override bool ShowInUI => Reported;

    protected override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        npc.ToleratesTrespass = true;
        Report($"{Cathedral.Game.Npc.NpcLabelResolver.ShortLabelCapitalized(npc)} lets you stay");
    }
}

/// <summary>
/// A householder who found the player waiting in their house at night will not have them stay.
/// Raises <see cref="Npc.NpcEntity.ShowsTheDoor"/>; the controller ends the visit on it.
/// </summary>
public class ShownTheDoorOutcome : Outcome
{
    public ShownTheDoorOutcome()
        : base("the householder shows the player the door", OutcomeSeverity.Negative, "was shown the door") { }

    public override bool ShowInUI => Reported;

    protected override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        npc.ShowsTheDoor = true;
        Report($"{Cathedral.Game.Npc.NpcLabelResolver.ShortLabelCapitalized(npc)} shows you the door");
    }
}
