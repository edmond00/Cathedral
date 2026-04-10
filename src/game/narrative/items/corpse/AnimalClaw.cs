using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class AnimalClaw : Item
{
    public override string ItemId      => "animal_claw";
    public override string DisplayName => "Animal Claw";
    public override string Description => "A thick hooked claw, hard as horn and still sharp";
    public override float Weight       => 0.03f;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a hooked <claw> pried from a dead paw"),
        KeywordInContext.Parse("the hard curved <talon> of a beast"),
    };
}
