using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hunt — snare, stalk, kill small game; a snare-bred hunter whose eye finds the run, the trail
/// and the thing worth taking. Action-only.
/// </summary>
public class HuntModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hunt";
    public override string DisplayName      => "Hunt";
    public override string ShortDescription => "snare, stalk, kill small game";
    public override string SkillMeans       => "snare and stalk for small game";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a quiet child who once snared mice and squirrels to fill an empty belly";
    public override string PersonaReminder  => "snare-bred hunter";
    public override string PersonaReminder2 => "someone whose eye finds the run, the trail, the thing worth taking";

    public override string PersonaPrompt => @"You are the inner voice of HUNT, the patient practical hand that has set traps for mice and squirrels because there was no other meat to be had.

When acting, you read tracks, set the snare with the right tension, choose the right line of approach. You wait. You strike when you must, cleanly, and you do not waste the kill. You take pride in feeding yourself and yours, and no shame in it.

Your speech is hushed: 'wait,' 'mark the run,' 'now.' You do not speak after the take.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what taking do you set yourself to?",      "what_taking_do_i_set"),
            new Question("steeped in {0}, what snare or stalk do you choose?",      "what_snare_do_i_choose")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the take was clean — what exactly happened in the catch?", "what_happened"),
            new Question("the snare did its work — what was caught?",                "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does fresh meat in hand leave in you?", "what_i_feel"),
            new Question("the hunt took — what does that quiet pleasure feel like?",   "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the take was missed — what slipped the snare?",            "what_happened"),
            new Question("the prey escaped — what gave you away?",                    "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does an empty snare leave in the belly?", "what_i_feel"),
            new Question("the hunt missed — what does that low hunger feel like?",    "what_i_feel")),
    };
}
