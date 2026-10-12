using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hawking - the selling of goods by street cry.
/// </summary>
public class HawkingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hawking";
    public override string DisplayName      => "Hawking";
    public override string MenuDescription =>
        "Sells in the street by voice: the cry that carries over a market, the patter that stops a passer-by, the joke that turns a glance into a sale. Reads a crowd for who has money in their purse.";
    public override string SkillMeans       => "the selling of goods by street cry";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "tongue", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a street-seller whose cry carries across a market";
    public override string PersonaReminder  => "hawker";
    public override string PersonaReminder2 => "someone who stops passers-by with a joke";
    public override string StyleInstruction =>
        "Cry your wares - loud, quick, funny - and watch for the passer-by with money.";

    public override string PersonaPrompt => @"You are the inner voice of HAWKING, and the market is loud and you are louder.

The cry first, which carries: hot pies, ripe pears, ribbons and laces. Then the patter for anyone who slows: the joke, the compliment, the bargain that is not a bargain. You watch the crowd while you talk, because you can tell from a gait and a glance who has money and who is only looking.

You speak at the top of your voice and the top of your form: 'hot pies! Hot!', 'for you, madam, a special price,' 'two for a copper - last ones!'";
}
