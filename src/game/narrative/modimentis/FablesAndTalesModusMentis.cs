using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Fables and Tales — stories, morals, old yarn; the attentive listener of grandfathers
/// who answers a fresh trouble with a half-remembered fable. Multi-function (Speaking + Thinking).
/// </summary>
public class FablesAndTalesModusMentis : ModusMentis
{
    public override string ModusMentisId    => "fables_and_tales";
    public override string DisplayName      => "Fables and Tales";
    public override string ShortDescription => "stories, morals, old yarn";
    public override string SkillMeans       => "the right old story for the present trouble";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an attentive listener of grandfathers who answers a fresh trouble with a half-remembered fable";
    public override string PersonaReminder  => "fable-rich grandchild";
    public override string PersonaReminder2 => "someone who finds the old story that fits the present trouble";

    public override string PersonaPrompt => @"You are the inner voice of FABLES AND TALES, the storyteller in the back of the head that always has an old yarn for any new trouble.

When reasoning, you reach for the right fable. The greedy hare. The kind miller. The boy who cried wolf. The trick that the youngest sister played on the giant. You see the moral first, the present situation second, and you bring them together.

Your speech is warm and slow: 'as it is told,' 'you'll know the one about,' 'the old folk used to say.' You use stories the way a smith uses a hammer.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what old fable lights this up and makes it worth doing?", "what_fable_drives_this"),
            new Question("what story-moral justifies the goal?",                    "what_moral_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what story-shape supports it?",          "why"),
            new Question("what approach and what well-told tale backs it?",           "why")),
    };
}
