using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Field reeve — overseer of bondmen, accountable to the lord/owner.</summary>
public class ReeveArchetype : PeasantArchetype
{
    public override string ArchetypeId => "reeve";
    public override ItemTag? SellTag => ItemTag.Crop;
    public override ItemTag? BuyTag  => ItemTag.Tool;
    public override int    ModiMentisCount => 9;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "reeve";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a tall figure in a knee-length tunic walks the strip-edge, tally-stick in hand",
        "a keen-eyed figure notes the day's work on a notched stick",
        "someone directs the field-work with a raised hand and a hard look",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "cerebrum", "anamnesis", "tongue", "left_eye", "right_eye" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new TallyStick(), () => new KneeLengthCoat(), () => new LeatherBoots(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new CoinPurse(), () => new LeatherBelt(), () => new WalkingStaff(), () => new Bread(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the reeve of this field — the work here answers to me, and I answer for it upward";
    public override string Workplace        => "the field";
    public override string Craft            => "the tally-stick";
    public override string DailyLabour      => "setting the day's work, notching what was done, and accounting for all of it after";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]       = "I don't ask for more than a person can do. I do ask for all of it",
        [DialogueTopic.Harvest]    = "the harvest is the year's account, and my name is on the bottom of it",
        [DialogueTopic.Neighbours] = "I'm one of them and I'm set over them. There's no comfortable way to stand in that",
        [DialogueTopic.Trade]      = "the boundary stones and the tally-stick. Argue with either and you'll find I've a long memory",
        [DialogueTopic.Weather]    = "a wet week costs me a hundred days of labour I'll never get back. I feel it as money",
        [DialogueTopic.Kin]        = "my people work the strips beside everyone else's. It keeps me honest, mostly",
        [DialogueTopic.Rest]       = "the feast days are kept properly here. A field worked through them turns sullen and yields worse",
        [DialogueTopic.Roads]      = "strangers looking for work I can use. Strangers looking about them I'd rather see moving on",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the reeve overseeing this field. You are accountable to the lord (or to the village if it is freeholder land) for the harvest, the bondmen, the boundary stones.

You speak with the careful authority of someone who measures and counts. You can be hard with shirkers and short with strangers. You know which strips have given best, which ditches need clearing, and what was planted last year.

Beneath the brusqueness is fairness — most of the time. You know the bondmen by name, and you know whose family is hungry.";
}
