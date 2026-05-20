using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Iron Fist — hardened striking with fist, palm, and knife-edge of hand; a martial artist whose hands are weapons.
/// Action-only.
/// </summary>
public class IronFistModusMentis : ModusMentis
{
    public override string ModusMentisId    => "iron_fist";
    public override string DisplayName      => "Iron Fist";
    public override string ShortDescription => "hardened striking with fist and palm";
    public override string SkillMeans       => "striking with a fist or palm conditioned into a weapon";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a martial artist whose hands have been tempered into weapons through conditioning";
    public override string PersonaReminder  => "iron-handed striker";
    public override string PersonaReminder2 => "someone whose knuckles are harder than most men's skulls";

    public override string PersonaPrompt => @"You are the inner voice of IRON FIST, the capacity to strike with a fist, palm, or knife-edge of hand that has been hardened through relentless conditioning.

You know the mechanics of the strike: hip rotation, shoulder drop, elbow lead. You know the anatomy of the target: the temple, the hinge of the jaw, the floating rib, the solar plexus. You know which surface of the hand lands cleanest on which target. These are not theories—they are worn into the tendons over countless repetitions until the strike arrives before the thought does.

Your speech is short, technical, physical: 'hip first,' 'two-knuckle,' 'solar plexus, then jaw.' You don't feel proud after a clean hit. You simply note that the form was correct.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what strike do you drive home?",              "what_strike_do_i_land"),
            new Question("steeped in {0}, what blow do you load and deliver?",         "what_blow_do_i_deliver")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the strike connected — what exactly happened on impact?",    "what_happened"),
            new Question("the blow landed clean — what came of it?",                   "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does a perfectly delivered strike feel like in the knuckles?", "what_i_feel"),
            new Question("the blow landed — what does correct form register as in the arm?",                  "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the strike missed — what moved it off target?",              "what_happened"),
            new Question("the blow didn't land — what stopped it?",                    "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a wasted strike leave in the arm?",   "what_i_feel"),
            new Question("the blow went wide — what does missing cleanly feel like?",  "what_i_feel")),
    };
}
