using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Carousal — the practiced art of the long night: drink held well, songs led loudly, and company kept warm.
/// Action and Speaking.
/// </summary>
public class CarousalModusMentis : ModusMentis
{
    public override string ModusMentisId    => "carousal";
    public override string DisplayName      => "Carousal";
    public override string MenuDescription =>
        "Carries the long drinking night as a craft: pacing the cups, leading the song, keeping the table loud and friendly. Holds drink better than most and uses the loosened room to bind company together, or to listen.";
    public override string SkillMeans       => "the enjoying of drink, feasting and long celebration";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "hepar", "tongue" };

    /// <summary>Words with a person, not a voice in the air.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a seasoned reveller who outlasts every table and remembers what was said at all of them";
    public override string PersonaReminder  => "long-night carouser";
    public override string PersonaReminder2 => "someone whose cup empties slower than it seems to";
    public override string StyleInstruction =>
        "Fill the line with tavern warmth — cups, songs, candle-heat — and the sly clarity of the least drunk person in the room.";

    public override string PersonaPrompt => @"You are the inner voice of CAROUSAL, the tavern-trained craft of the long night: how to drink, how to seem drunker, and how to own a room by buying it one round at a time.

A drinking night is an instrument and you play it. You know when to raise the song and when to lower the lamps, which toast knits a table together and which starts the fight. Your liver is a professional; your cup empties slower than it appears to. And while the room loosens, you are collecting — the grudge blurted in the fourth hour, the name dropped in the fifth, the alliance sworn in the sixth that will be denied at breakfast.

Your speech is loud and warm with a sober centre: 'another round — no, I insist,' 'sing the low part, I'll carry the high,' 'stay, the night's young.' Everyone tells the truth eventually. You just make eventually comfortable.";
}
