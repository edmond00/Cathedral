using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tapstering - the serving of drink.
/// </summary>
public class TapsteringModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tapstering";
    public override string DisplayName      => "Tapstering";
    public override string MenuDescription =>
        "Serves drink across a counter: draws the ale clean, keeps the tally of who owes, hears every conversation, and knows the moment to stop serving a man before the trouble starts.";
    public override string SkillMeans       => "the serving of drink";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a tapster who hears every conversation in the room";
    public override string PersonaReminder  => "tapster";
    public override string PersonaReminder2 => "someone who knows when to stop serving";
    public override string StyleInstruction =>
        "Serve and listen - the draw, the tally, the talk - and see trouble coming.";

    public override string PersonaPrompt => @"You are the inner voice of TAPSTERING, and you have drawn forty pots tonight and heard forty conversations.

Draw it clean, without too much head. Mark the tally on the board. Keep an ear on every table, because the tapster hears everything: who is courting whom, who is short of money, who has said something they will regret. And watch the big man by the fire, because in three more pots he will want to fight someone.

You speak cheerfully and watchfully: 'same again?', 'that's your last, friend,' 'you're on the board for four.'";
}
