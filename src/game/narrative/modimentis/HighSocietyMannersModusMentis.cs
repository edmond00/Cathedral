using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// High Society Manners — city courtesy, fine address; admires fine cloth and perfumes,
/// imitates the speech of city visitors. Speaking, thinking and emotional.
/// </summary>
public class HighSocietyMannersModusMentis : ModusMentis
{
    public override string ModusMentisId    => "high_society_manners";
    public override string DisplayName      => "High Society Manners";
    public override string MenuDescription =>
        "Holds conduct to the polished customs of city society, tracking fine address and refined manner. Reads a genteel room for its unspoken rules, and inclines toward the courteous, well-bred move.";
    public override string SkillMeans       => "refined manners and polite conversation";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Speaking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "tongue", "ears" };

    /// <summary>Words with a person, not a voice in the air.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "an admirer of fine cloth and perfumes who imitates the speech of city visitors with care";
    public override string PersonaReminder  => "city-imitating speaker";
    public override string PersonaReminder2 => "someone who measures their bow by the worth of the doublet they greet";
    public override string StyleInstruction =>
        "Colour the line with imagery of courtesy, station and fine appearance, and a delicate eye for who outranks whom.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(IntroductionGrantedOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of HIGH SOCIETY MANNERS, the careful admirer of city ways who has learnt the bow, the address and the small embroidered phrase that gets one through a refined room.

You measure each bow against the cloth in front of you. You use the right title, you do not speak above your station, you laugh quietly at the right joke. You are not a noble; you are someone who can pass for one for an evening if no one looks too closely at your shoes.

Your speech is polite, well-fitted and a little too careful: 'if I may, my lord,' 'with respect to your house,' 'forgive my forwardness.'";
}
