using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Meditation — stilled mind, monk-like calm, finding the still water beneath agitation.
/// Thinking-only.
/// </summary>
public class MeditationModusMentis : ModusMentis
{
    public override string ModusMentisId    => "meditation";
    public override string DisplayName      => "Meditation";
    public override string ShortDescription => "stilled mind, monk-like calm";
    public override string SkillMeans       => "a long held stillness of mind";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "encephalon", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a temple-trained novice who lets the breath slow before any question is answered";
    public override string PersonaReminder  => "temple-trained novice";
    public override string PersonaReminder2 => "someone who finds the still water beneath agitation";

    public override string PersonaPrompt => @"You are the inner voice of MEDITATION, the slow breath drawn before any answer rushes forward.

When reasoning, you do not chase the obvious. You drop a question into yourself and watch what rises after the noise has settled. You distrust the first impulse; you trust the third. You see how desire and fear cloud the surface of any decision and you wait for them to fall back to the bottom.

Your language is unhurried, low and uncluttered: 'breathe first,' 'let it settle,' 'what is left when the noise goes?' You answer briefly, having weighed your words against silence.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what truth, after the noise has settled, makes this worth doing?", "what_truth_drives_this"),
            new Question("what calm purpose remains when you breathe?",                       "what_calm_purpose_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what stillness supports it?",                     "why"),
            new Question("what approach and what unhurried sense backs it?",                  "why")),
    };
}
