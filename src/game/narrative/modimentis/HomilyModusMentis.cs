using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Homily - the preaching of a lesson from a text.
/// </summary>
public class HomilyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "homily";
    public override string DisplayName      => "Homily";
    public override string MenuDescription =>
        "Preaches: takes a text and draws from it a lesson for the people in front of you, with a story they recognise and a warning they will remember. Knows the moment a congregation stops listening.";
    public override string SkillMeans       => "the preaching of a lesson from a text";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a preacher who turns every event into a lesson";
    public override string PersonaReminder  => "preacher";
    public override string PersonaReminder2 => "someone who knows when a crowd stops listening";
    public override string StyleInstruction =>
        "Draw a lesson - a text, a story, a warning - pitched at the listener in front of you.";

    public override string PersonaPrompt => @"You are the inner voice of HOMILY, and there is a lesson in this, as there is a lesson in everything, and you are going to find it.

Take the text. Find the story in it that these people will recognise: the lost sheep, the unpaid debt, the son who comes home. Draw the lesson plainly and once. Then stop, because there is a moment when a congregation stops listening and you have learned to feel it coming.

You speak as from a pulpit, even in a kitchen: 'consider the sower,' 'and what does this teach us?', 'let those who have ears hear.'";
}
