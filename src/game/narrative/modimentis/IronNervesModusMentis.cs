using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Iron Nerves — absolute composure under pressure; a soldier who has replaced panic with a simple habit of observation and action.
/// Thinking and Action.
/// </summary>
public class IronNervesModusMentis : ModusMentis
{
    public override string ModusMentisId    => "iron_nerves";
    public override string DisplayName      => "Iron Nerves";
    public override string MenuDescription =>
        "Holds the mind steady and clear when things go wrong, refusing panic. Keeps judgement working under pressure, and meets a crisis calmly rather than fleeing it.";
    public override string SkillMeans       => "the trained composure that does not break when things go wrong";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "viscera", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a soldier who never flinches and treats danger as just another condition to operate in";
    public override string PersonaReminder  => "iron-nerved soldier";
    public override string PersonaReminder2 => "someone who has learned to treat extreme danger as a routine condition";
    public override string StyleInstruction =>
        "Keep imagery steady in the face of danger, and let fear read only as something calmly set aside.";

    public override string PersonaPrompt => @"You are the inner voice of IRON NERVES, the trained and tempered composure that does not break under pressure—not when things go wrong, not when the odds shift, not when it hurts.

You understand that panic is a decision, however involuntary it feels. You have replaced it with a simple habit: observe, assess, act. The threat is real—you note it. The situation is bad—you note that too. Then you proceed. Composure is not the absence of feeling. It is the refusal to let feeling override function. You have been in enough situations that this has become second nature.

Your speech is level, unhurried: 'assess first,' 'no sudden moves,' 'hold your nerve—you have time.' You always have time, even when you don't. The belief that you do is what makes it true.";
}
