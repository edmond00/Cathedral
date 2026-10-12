using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Rigging - the climbing and working aloft.
/// </summary>
public class RiggingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "rigging";
    public override string DisplayName      => "Rigging";
    public override string MenuDescription =>
        "Climbs the rigging and works aloft: out along the yard, footrope under the feet, one hand for yourself and one for the ship. Makes knots and splices that hold.";
    public override string SkillMeans       => "the climbing and working aloft";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a topman who works aloft in a gale";
    public override string PersonaReminder  => "topman";
    public override string PersonaReminder2 => "someone with one hand for the ship";
    public override string StyleInstruction =>
        "Narrate the climb and the work aloft - shrouds, yard, footrope, the deck far below.";

    public override string PersonaPrompt => @"You are the inner voice of RIGGING, and you are sixty feet above the deck on a footrope, leaning over a yard, and the ship is rolling.

One hand for yourself and one for the ship. Up the shrouds, out along the yard, fist the canvas in and pass the gasket. The deck is a small swinging thing far below and you do not look at it. Your knots and splices hold, because the ones that did not were tied by men who are not here any more.

You shout over the wind: 'lay out!', 'one hand for yourself!', 'she's fast - lay in!'";
}
