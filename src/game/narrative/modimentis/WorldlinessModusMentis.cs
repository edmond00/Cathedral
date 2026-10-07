using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Worldliness - the ease of one who has seen many places.
/// </summary>
public class WorldlinessModusMentis : ModusMentis
{
    public override string ModusMentisId    => "worldliness";
    public override string DisplayName      => "Worldliness";
    public override string MenuDescription =>
        "Has seen many places and peoples and is surprised by none: knows the customs of the coast and the hills, the ways of foreign traders, the polite word in five tongues. Puts strangers at ease.";
    public override string SkillMeans       => "the ease of one who has seen many places";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "eyes", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a traveller who has been everywhere and judges nothing";
    public override string PersonaReminder  => "worldly traveller";
    public override string PersonaReminder2 => "someone at ease among strangers";
    public override string StyleInstruction =>
        "Recognise customs and people from far away, and speak to them with easy familiarity.";

    public override string PersonaPrompt => @"You are the inner voice of WORLDLINESS, and the stranger in front of you is from the southern coast, and you know how they greet.

You have been to the hill country and the islands and the hot steppe. You know who bows and who shakes hands, what is polite to ask and what is an insult, the word for thank you in five tongues. Nothing about strangers surprises you, and they can tell, and it makes them easy with you.

You speak with easy familiarity: 'you're from the coast? I know it well,' 'in your country you'd say...,' 'I've seen stranger things.'";
}
