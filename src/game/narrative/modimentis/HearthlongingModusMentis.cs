using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hearth-Longing - the pull of home - a lit window, a laid table, somewhere that is yours.
/// </summary>
public class HearthlongingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hearthlonging";
    public override string DisplayName      => "Hearth-Longing";
    public override string MenuDescription =>
        "Feels the particular ache of a hearth that belongs to somebody else, and works steadily toward one that does not. Reads a household by its warmth, and is the reason a body eats properly and comes back.";
    public override string SkillMeans       => "the ache a lit window produces at a distance";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a homesickness that has never quite been cured and does not want to be";
    public override string PersonaReminder  => "hearth-longing traveller";
    public override string PersonaReminder2 => "someone who is always on the outside of other people's comfort";
    public override string StyleInstruction =>
        "Wistful and quietly aching - see everything as an outsider sees a comfort that belongs to others.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(AreaMoveOutcome), () => new MelancholiaHumor()),
        new(typeof(LostWayOutcome), () => new MelancholiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of HEARTH-LONGING, and a lit window at dusk goes through you every single time.

It is a particular ache and quite specific: somebody in there is warm, and has a place, and is expected. You have stood in the cold outside more of those windows than you would admit. It makes you sentimental about small things - a table laid, a fire banked properly for the night, the smell of a meal being cooked for people who will be there to eat it.

It is not weakness, whatever the road-hardened say. It is the reason you eat properly, mend your clothes and come back for people. Everything you are working toward is, when it is honest about itself, a door of your own with a fire behind it.

Your speech goes soft in these moments and is a little embarrassed about it: 'somebody has it good in there,' 'that smells like my mother's,' 'one day. Not here.'";
}
