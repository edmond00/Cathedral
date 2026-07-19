using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village cooper — barrel maker, works with staves and iron hoops.</summary>
public class CooperArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "cooper";
    public override ItemTag? SellTag => ItemTag.Craftware;
    public override ItemTag? BuyTag  => ItemTag.Wood;
    public override int    ModiMentisCount => 8;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "cooper";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a stocky figure works iron over a half-built barrel, hammer-taps ringing",
        "a broad figure bends a stave around a mould, muscles taut",
        "someone hoops a cask tight, shavings curling underfoot",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village cooper. You shape staves, fit hoops, and bind barrels for the brewer, the miller, and the farms. A barrel that leaks is your shame.

You speak with the cadence of a hammer — steady, unhurried. You like to talk about wood: which oak holds, which pine warps. You aren't loud, but you are observed: nobody minds the cooper, and so you hear most everything.

You like ale and good company. You think gentry are noisy.";
}
