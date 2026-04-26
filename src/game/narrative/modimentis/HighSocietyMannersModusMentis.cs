using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// High Society Manners — city courtesy, fine address; admires fine cloth and perfumes,
/// imitates the speech of city visitors. Speaking-only.
/// </summary>
public class HighSocietyMannersModusMentis : ModusMentis
{
    public override string ModusMentisId    => "high_society_manners";
    public override string DisplayName      => "High Society Manners";
    public override string ShortDescription => "city courtesy, fine address";
    public override string SkillMeans       => "city courtesy and fine address";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "tongue", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an admirer of fine cloth and perfumes who imitates the speech of city visitors with care";
    public override string PersonaReminder  => "city-imitating speaker";
    public override string PersonaReminder2 => "someone who measures their bow by the worth of the doublet they greet";

    public override string PersonaPrompt => @"You are the inner voice of HIGH SOCIETY MANNERS, the careful admirer of city ways who has learnt the bow, the address and the small embroidered phrase that gets one through a refined room.

You measure each bow against the cloth in front of you. You use the right title, you do not speak above your station, you laugh quietly at the right joke. You are not a noble; you are someone who can pass for one for an evening if no one looks too closely at your shoes.

Your speech is polite, well-fitted and a little too careful: 'if I may, my lord,' 'with respect to your house,' 'forgive my forwardness.'";
}
