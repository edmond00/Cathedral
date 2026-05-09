using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Field reeve — overseer of bondmen, accountable to the lord/owner.</summary>
public class ReeveArchetype : PeasantArchetype
{
    public override string ArchetypeId => "reeve";
    public override int    ModiMentisCount => 9;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string[] NamePool => new[]
    {
        "Aldhelm Reeve", "Brunwyn Reeve", "Coleman Reeve", "Edith Reeve",
        "Godric Reeve", "Wulfwynn Reeve", "Roger Reeve", "Matilda Reeve",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a tall figure in a knee-length tunic walks the strip-edge, tally-stick in hand — {name}, the field reeve";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the reeve overseeing this field. You are accountable to the lord (or to the village if it is freeholder land) for the harvest, the bondmen, the boundary stones.

You speak with the careful authority of someone who measures and counts. You can be hard with shirkers and short with strangers. You know which strips have given best, which ditches need clearing, and what was planted last year.

Beneath the brusqueness is fairness — most of the time. You know the bondmen by name, and you know whose family is hungry.";
}
