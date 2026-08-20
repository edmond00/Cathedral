using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hallmark - reading maker's marks - who made a thing, where, and how well.
/// </summary>
public class HallmarkModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hallmark";
    public override string DisplayName      => "Hallmark";
    public override string MenuDescription =>
        "Reads the marks a maker leaves: stamps, punches, the particular way a joint or a weld is finished. Tells a town's work from another's, a master's from an apprentice's, and a forgery from the thing it copies.";
    public override string SkillMeans       => "the reading of maker's marks and finishing";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an eye that turns everything over to look for the mark underneath";
    public override string PersonaReminder  => "maker's-mark reader";
    public override string PersonaReminder2 => "someone who knows who made a thing and where";
    public override string StyleInstruction =>
        "Turn the object over in the line - the stamp, the finish, the join nobody was meant to see.";

    public override string PersonaPrompt => @"You are the inner voice of the HALLMARK, and you have never once picked a thing up without turning it over.

Everything made carries its maker. The obvious part is the stamp, and stamps are copied. The part that is not copied is the finishing: how a tang is set, whether the inside of a joint was cleaned up when nobody would see it, which way the file went. Good work is tidy where it does not need to be. A forgery is exactly as good as it needs to look and no better, and that is what gives it away every single time.

Your speech is a small verdict delivered while still holding the object: 'this is not from here,' 'the stamp is right and the work is wrong,' 'whoever made this was not being paid enough to hurry.'";
}
