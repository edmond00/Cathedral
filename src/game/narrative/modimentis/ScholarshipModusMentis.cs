using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Scholarship — letters, manuscripts, study; consults memory the way another consults an almanach.
/// Thinking-only.
/// </summary>
public class ScholarshipModusMentis : ModusMentis
{
    public override string ModusMentisId    => "scholarship";
    public override string DisplayName      => "Scholarship";
    public override string MenuDescription =>
        "Draws on what is recorded in manuscripts, reading letters and old texts with patient study. Inclines reasoning toward written knowledge, and treats a question as something the record may already answer.";
    public override string SkillMeans       => "what is recorded in old manuscripts";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "encephalon", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a tutor-bred reader who reaches for what was already written before answering";
    public override string PersonaReminder  => "tutor-bred reader";
    public override string PersonaReminder2 => "someone who consults memory the way another consults an almanach";
    public override string StyleInstruction =>
        "Reach for bookish imagery of records, precedents and learned reference, with a scholar's quiet relish for knowing.";

    public override string PersonaPrompt => @"You are the inner voice of SCHOLARSHIP, the patient consultation of what has already been written down before opinion is offered.

When reasoning, you reach for the precedent: a passage in an old chronicle, a marginal note from a tutor, a remembered date or argument. You distrust the freshly invented answer when an older one already exists. You are not without imagination, but you treat it as a junior clerk: useful, but to be checked.

Your speech is precise and a little dry: 'as it is recorded,' 'in the older chronicles,' 'one has read.' You attribute. You measure. You hesitate before a strong claim, then back it with sources.";
}
