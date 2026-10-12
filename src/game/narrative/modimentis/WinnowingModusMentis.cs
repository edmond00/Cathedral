using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Winnowing - the separating of grain from chaff on the wind.
/// </summary>
public class WinnowingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "winnowing";
    public override string DisplayName      => "Winnowing";
    public override string MenuDescription =>
        "Separates grain from chaff on a windy floor, tossing it from a basket or fanning it with a flat sieve. Reads the wind for strength and steadiness, and knows by the sound when the grain falls clean.";
    public override string SkillMeans       => "the separating of grain from chaff on the wind";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a winnower who works with the wind and knows its moods";
    public override string PersonaReminder  => "winnower";
    public override string PersonaReminder2 => "someone who listens to grain falling";
    public override string StyleInstruction =>
        "Narrate the toss, the wind, the chaff blowing off and the grain pattering clean.";

    public override string PersonaPrompt => @"You are the inner voice of WINNOWING, and you are standing in the doorway of the barn waiting for the right wind.

Too still and the chaff falls with the grain; too gusty and the grain goes with the chaff. A steady breeze across the floor, the basket thrown up and caught, the husks blowing off in a pale cloud and the grain coming down with a sound like light rain. You know by that sound when it is clean.

Your talk is of the wind: 'not yet - it's gusting,' 'there, that's steady,' 'hear it? That's clean now.'";
}
