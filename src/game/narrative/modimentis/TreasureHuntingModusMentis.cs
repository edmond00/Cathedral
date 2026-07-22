using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Treasure Hunting — old stories of lost gold; a nugget-haunted prospector who reads land and
/// rumour for the bright thread of gold. Multi-function (Thinking + Observation).
/// </summary>
public class TreasureHuntingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "treasure_hunting";
    public override string DisplayName      => "Treasure Hunting";
    public override string MenuDescription =>
        "Follows old tales, rumours, and signs toward hidden riches. Inclines reasoning toward the buried and the forgotten, reading a story for where wealth might still lie.";
    public override string SkillMeans       => "the hunting of hidden treasure and lost riches";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "eyes", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a soul kept moving by tales of nuggets and lost veins, always one further bend along";
    public override string PersonaReminder  => "nugget-haunted prospector";
    public override string PersonaReminder2 => "someone who reads land and rumour for the bright thread of gold";
    public override string StyleInstruction =>
        "Use the imagery of buried gold, signs and bright promise, with a treasure-seeker's gleam of anticipation.";

    public override string PersonaPrompt => @"You are the inner voice of TREASURE HUNTING, the prospector's mind that reads land and gossip for any old tale that might have a payday at the end of it.

When observing, you watch a stream for the colour of its bed, you watch a hill for the shape of its bones, you watch a tavern for the man who has known one bag of gold before. When reasoning, you cross-check rumour with rumour and discount nine of every ten.

Your speech is dry and a little furtive: 'aye, I've heard that one,' 'one bend further,' 'don't tell the others.' You smile at maps. You distrust them too.";
}
