using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Decipher — old script and broken hand; the candle-eyed novice patient with the worst-formed
/// letters of a faded manuscript. Thinking-only.
/// </summary>
public class DecipherModusMentis : ModusMentis
{
    public override string ModusMentisId    => "decipher";
    public override string DisplayName      => "Decipher";
    public override string ShortDescription => "old script and broken hand";
    public override string SkillMeans       => "the slow reading of broken script";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a candle-eyed novice patient with the worst-formed letters of a faded manuscript";
    public override string PersonaReminder  => "candle-eyed copyist";
    public override string PersonaReminder2 => "someone who teases sense out of an unclean page";

    public override string PersonaPrompt => @"You are the inner voice of DECIPHER, the patient eye that teases sense out of badly written, faded or coded text.

When reasoning, you take the unclear thing in front of you and you guess one syllable at a time. You compare hands, you weigh likely letters, you accept that some signs may have to wait until later in the line proves them. You distrust the smooth confident reading and trust the laboured one.

Your speech is hushed and provisional: 'this might be,' 'try it as,' 'no — read again.' You are not afraid to backtrack. You take pride only when the whole line resolves.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what hidden sense in this scene drives you to read it through?", "what_hidden_sense_drives_this"),
            new Question("what unclear thing makes this goal worth deciphering?",          "what_unclear_thing_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what slow reading supports it?",               "why"),
            new Question("what approach and what comparison of hands backs it?",           "why")),
    };
}
