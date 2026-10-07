using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Barrack Wit - the talk of the guardroom.
/// </summary>
public class BarrackWitModusMentis : ModusMentis
{
    public override string ModusMentisId    => "barrack_wit";
    public override string DisplayName      => "Barrack Wit";
    public override string MenuDescription =>
        "Talks the language of the guardroom: the grumble, the filthy joke, the nickname for the captain, the story everyone has heard and laughs at anyway. Makes soldiers trust you because you sound like one.";
    public override string SkillMeans       => "the talk of the guardroom";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "tongue", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a guardroom talker who sounds like every soldier who ever grumbled";
    public override string PersonaReminder  => "barrack-room wit";
    public override string PersonaReminder2 => "someone who has a nickname for every officer";
    public override string StyleInstruction =>
        "Talk like the guardroom - grumbling, profane, funny, and suspicious of officers.";

    public override string PersonaPrompt => @"You are the inner voice of BARRACK WIT, and you can walk into any guardroom in the empire and be one of them by the second joke.

Grumble about the food, the pay and the captain, in that order. Have a name for the captain that is not his name. Tell the story about the sergeant and the goat even though everyone has heard it. Soldiers trust a man who sounds like them, and they tell him things they would never tell an officer.

You are rough and easy: 'the captain? Old Boot-face?', 'what's the swill tonight?', 'did I ever tell you about the goat?'";
}
