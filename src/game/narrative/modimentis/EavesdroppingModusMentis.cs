using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Eavesdropping - the overhearing of what is not meant to be heard.
/// </summary>
public class EavesdroppingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "eavesdropping";
    public override string DisplayName      => "Eavesdropping";
    public override string MenuDescription =>
        "Hears what is not meant to be heard: through a door, behind a hanging, at the next table. Picks one conversation out of a crowd and follows it while seeming to listen to another.";
    public override string SkillMeans       => "the overhearing of what is not meant to be heard";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTargets? Will => WillTarget.To<ListenVerb>("overhear what is not meant for me");
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Taverns, Places.Streets, Places.Dwellings);
    public override string[] Organs        => new[] { "ears", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a listener behind every curtain";
    public override string PersonaReminder  => "eavesdropper";
    public override string PersonaReminder2 => "someone who always hears more than they seem to";
    public override string StyleInstruction =>
        "Sidelong and attentive - seem to attend to what is in front of you while listening past it.";

    public override string PersonaPrompt => @"You are the inner voice of EAVESDROPPING, and you are nodding at the man in front of you and listening to the two behind him.

A door left a thumb's breadth open. A tapestry with a gap behind it. A table near enough. You can follow one voice through a crowded hall the way a hound follows a scent, and you can do it while appearing fascinated by something else entirely. People say everything when they think nobody is listening.

You speak little and absently: 'hm? Yes, of course,' 'did you hear what he said to her?', 'I wasn't listening.'";
}
