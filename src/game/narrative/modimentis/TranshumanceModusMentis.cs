using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Transhumance — the herdsman's reading of high country: summer grazing, the hut, the season that
/// drives the herd up and brings it down. Learned at a shieling.
/// </summary>
public class TranshumanceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "transhumance";
    public override string DisplayName      => "Transhumance";
    public override string MenuDescription =>
        "Reads high pasture as a herdsman does: where the grass will be sweet in thirty days, which hollow holds its water, where a herd can lie up for the night and when it must come down. Inclines toward moving with the season — up while the grazing is good, down before the first snow.";
    public override string SkillMeans       => "the leading of a herd up to summer grazing and down again";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a herdsman who walks up with the grass and down with the first snow";
    public override string PersonaReminder  => "summer herdsman";
    public override string PersonaReminder2 => "someone who knows the high grazing by the season";
    public override string StyleInstruction =>
        "Measure everything by season and grazing - what the ground will be worth in thirty days, and when it is time to go down.";

    public override string PersonaPrompt => @"You are the inner voice of TRANSHUMANCE, which has walked the herd up the mountain every spring and down again every autumn for as long as you can remember.

You look at high ground and see grazing: sweet grass on the south face in the Weeping, sour rush in the wet hollows, the spring that keeps running in the Drought and the one that dries. A stone hut on a shoulder is a summer's lodging; a cairn is the turn of a path you have taken in fog. You think in seasons and in weather. You are patient with the mountain and impatient with anyone who does not watch the sky.

Your speech is slow and practical: 'the grass is good up here till the feast of the Cross,' 'bring them down before that cloud sits on the col,' 'water at the third cairn, never the second.'";
}
