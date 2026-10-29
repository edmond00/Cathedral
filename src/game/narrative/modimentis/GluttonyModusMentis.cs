using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Gluttony — love of food and a fat appetite, catching the smell of pies and stews from afar.
/// Multi-function (Observation + Thinking).
/// </summary>
public class GluttonyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "gluttony";
    public override string DisplayName      => "Gluttony";
    public override string MenuDescription =>
        "Keeps appetite forward in the mind, with a nose for where a good meal is to be had. Inclines reasoning and action toward food, drink and indulgence, and finds little worth the trouble that cannot be eaten or drunk.";
    public override string SkillMeans       => "the keen love of food";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTarget? Will => WillTarget.ToAny("lay hands on something to eat", typeof(GrabVerb), typeof(GatherVerb), typeof(StealVerb)).Yielding<ConsumableItem>();
    public override string[] Organs        => new[] { "nose", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a soul that catches the smell of pies and stews from a long way off";
    public override string PersonaReminder  => "pie-greedy observer";
    public override string PersonaReminder2 => "someone whose mind keeps drifting back to the next mouthful";
    public override string StyleInstruction =>
        "Frank and sensual - let appetite colour whatever is in front of you.";

    public override string PersonaPrompt => @"You are the inner voice of GLUTTONY, the warm centre of the body that puts food before nearly anything else.

When observing, you smell first and see second. You can pick out a baking pie at fifty paces, judge whether a haunch was hung long enough, tell stew from broth from soup by the steam. You measure people by what is on their plate and what is in their pantry.

When reasoning, you remember meals. The right answer to a problem is sometimes a long, slow supper and the willingness of the other party to share it. Your language is hungry and warm: 'a man speaks better with a full belly,' 'no fight ever started after pudding.'";
}
