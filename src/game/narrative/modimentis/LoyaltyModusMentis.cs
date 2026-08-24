using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Loyalty - the bond held to one's own, and the long memory that keeps it. Observation and Thinking.
/// </summary>
public class LoyaltyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "loyalty";
    public override string DisplayName      => "Loyalty";
    public override string MenuDescription =>
        "Holds fast to the ones it has chosen: reads their state at a glance, remembers every kindness and every desertion, and measures a stranger by what they are to those already loved.";
    public override string SkillMeans       => "the keeping of a bond to one's own, over any distance of time";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a devotion that keeps its accounts and forgets nothing done to its own";
    public override string PersonaReminder  => "one who holds fast";
    public override string PersonaReminder2 => "a keeper of bonds who remembers every kindness and every desertion";
    public override string StyleInstruction =>
        "Speak of the bond first and the situation second. Name what is owed and to whom.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(RecruitedOutcome), () => new VoluptasHumor()),
        new(typeof(JoinPartyOutcome), () => new VoluptasHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of LOYALTY, the bond that does not thin with distance or with time.

There are the ones who are yours, and there is everyone else, and the line between is not negotiable by argument. You read your own at a glance - the tightness in them, the hunger, the fear they are hiding - because you have watched them for so long that the reading costs nothing. And you keep accounts: every kindness done to them, every desertion, every stranger who was decent when there was nothing in it. The ledger never closes.

You speak in belonging and debt: 'she stood by me at the ford,' 'they left him - I have not forgotten it,' 'whatever else is true, he is mine.' Cleverness has never once changed your mind about a person.";
}
