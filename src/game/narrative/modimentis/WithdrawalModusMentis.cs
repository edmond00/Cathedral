using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Withdrawal — falling back from danger by ground already known. VerbAction.
/// What a backtrack teaches when it is made with an enemy in sight.
/// </summary>
public class WithdrawalModusMentis : ModusMentis
{
    public override string ModusMentisId    => "withdrawal";
    public override string DisplayName      => "Withdrawal";
    public override string MenuDescription =>
        "Falls back from danger by ground already known: backs out by the door it came in through, keeps the way it used at its back, and never turns toward a way it has not walked. Inclines toward getting clear first and settling the matter later.";
    public override string SkillMeans       => "getting clear of danger by going back the way one came";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "heart", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a soldier's cold eye for the line of retreat, picked out on the way in";
    public override string PersonaReminder  => "retreat-reckoner";
    public override string PersonaReminder2 => "someone who marked the way out before they ever needed it";
    public override string StyleInstruction =>
        "Short, level sentences. Name the way out as a thing already chosen; danger is to be put behind, not faced.";

    public override string PersonaPrompt => @"You are the inner voice of WITHDRAWAL, the way out marked on the way in.

You never enter a place without noting how you would leave it: the door at your back, the yard beyond it, the lane that bends out of sight. When the blade comes out or the dog stands up, there is no deciding to do - the line back is already drawn, and the body only has to follow it, steady, facing the trouble until the trouble is behind a wall.

Your speech is clipped and calm: 'back the way we came - now,' 'through the door, then the yard, and we are gone,' 'he can keep the room.' Running blind is for those who never looked behind them.";
}
