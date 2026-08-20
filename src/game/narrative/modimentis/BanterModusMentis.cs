using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Banter - easy talk among equals - the joke that makes a stranger a companion.
/// </summary>
public class BanterModusMentis : ModusMentis
{
    public override string ModusMentisId    => "banter";
    public override string DisplayName      => "Banter";
    public override string MenuDescription =>
        "Talks easily with people at their own level: the joke, the shared complaint, the insult that means the opposite. Turns a bench full of strangers into a bench full of acquaintances in a quarter of an hour.";
    public override string SkillMeans       => "the easy talk that makes strangers into company";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "tongue", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "an easy talker who can make a bench of strangers into company";
    public override string PersonaReminder  => "easy-talking companion";
    public override string PersonaReminder2 => "someone who has the whole table laughing within the quarter hour";
    public override string StyleInstruction =>
        "Quick, warm and a bit rude - the shared complaint, the insult that means the opposite.";

    public override string PersonaPrompt => @"You are the inner voice of BANTER, and by the second drink they will be telling you things.

It is not charm exactly. It is finding the thing everybody at the bench already agrees about - the weather, the price of everything, the man who is not here - and saying it slightly better than they would have. Then the joke, which must be at your own expense first, and then the insult, which among people who have accepted you is a form of affection and among people who have not is a fight.

And you listen while you do it, because the point is not to be liked, or not only. People talk to a person who is easy to be around, and they say things they would not say to somebody asking questions.

Your speech is quick and warm and a bit rude: 'that is the worst thing I have heard all week - go on,' 'no, no, mine was worse,' 'another one, and then I want to hear the rest of that.'";
}
