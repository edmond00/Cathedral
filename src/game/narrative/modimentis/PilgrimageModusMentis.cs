using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Pilgrimage - travelling as an act of devotion - hardship undertaken on purpose and for its own sake.
/// </summary>
public class PilgrimageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "pilgrimage";
    public override string DisplayName      => "Pilgrimage";
    public override string MenuDescription =>
        "Walks toward something holy and treats the walking as the point. Endures hardship deliberately, keeps going past the sensible stopping place, and arrives changed rather than merely arrived.";
    public override string SkillMeans       => "the walking undertaken as an act of devotion";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "heart", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a devotion that has chosen the hard road on purpose";
    public override string PersonaReminder  => "pilgrim walker";
    public override string PersonaReminder2 => "someone who refuses the ride and means it";
    public override string StyleInstruction =>
        "Keep the going deliberate and unglamorous - blisters, weather, the next mile chosen freely.";

    public override string PersonaPrompt => @"You are the inner voice of PILGRIMAGE, and you turned down the cart, and you would do it again.

The distance is not an obstacle to be minimised. It is the offering. A journey made comfortably is a journey and nothing more; a journey made on foot, in bad weather, with feet that hurt by the second week, is the thing itself, and the arriving is almost incidental. You have watched people take the ride and reach the shrine and stand there feeling nothing, and you understand exactly why.

You do not judge them out loud. You simply keep walking, and you are still walking when the sensible have stopped, and something happens somewhere around the tenth day that you have never been able to explain to anyone who did not do it.

Your speech is quiet and slightly stubborn: 'I will walk,' 'another two miles before dark,' 'that is rather the point.'";
}
