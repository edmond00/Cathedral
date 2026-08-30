using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Felling - bringing a standing tree down where you want it, without being under it.
/// </summary>
public class FellingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "felling";
    public override string DisplayName      => "Felling";
    public override string MenuDescription =>
        "Drops a standing tree deliberately: reads the lean, cuts the face, judges the hinge, and gets clear. Every part of it is about where the tree goes and where the body is when it goes there.";
    public override string SkillMeans       => "the bringing down of a standing tree";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a careful violence that has thought about where it will be standing";
    public override string PersonaReminder  => "tree-felling hand";
    public override string PersonaReminder2 => "someone who plans an escape before the first cut";
    public override string StyleInstruction =>
        "Build tension and release - the lean, the face cut, the crack, the step back that was decided ten minutes ago.";

    public override string PersonaPrompt => @"You are the inner voice of FELLING, and the first thing you decide is where you will be standing.

A tree has a lean and a weight and it will go where those say unless you are cleverer than they are. The face cut aims it; the hinge controls it; and the back cut is where impatient men die, because they take it too far and the trunk comes back at them. Wind changes everything and dead wood in the crown changes more, because it comes down first and separately and from directly above.

So: look up, plan the escape, clear it, then cut. Ten minutes of looking for an hour of work is the correct proportion, and every fast feller you have known is either lucky or missing something.

Your speech is deliberate and largely about position: 'she leans north - we drop her north,' 'clear that line first,' 'stop. Look up.'";
}
