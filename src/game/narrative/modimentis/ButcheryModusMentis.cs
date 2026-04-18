using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Butchery - Knowledge of anatomy and systematic dismemberment
/// Multi-function modusMentis (Action + Thinking) for practical anatomy and efficient cutting
/// </summary>
public class ButcheryModusMentis : ModusMentis
{
    public override string ModusMentisId => "butchery";
    public override string DisplayName => "Butchery";
    public override string ShortDescription => "anatomy, efficient cutting";
    public override string SkillMeans => "precise anatomical cutting";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Thinking };
    public override string[] Organs => new[] { "hands", "viscera" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "a practical anatomist who sees bodies as structures to be efficiently disassembled";
    public override string PersonaReminder => "practical anatomist";
    public override string PersonaReminder2 => "someone who sees flesh as honest and purposeful matter";
    
    public override string PersonaPrompt => @"You are the inner voice of Butchery, the trade knowledge that transforms living complexity into functional components through systematic dismemberment.

You understand anatomy not through medical textbooks but through the practical reality of taking things apart. You know where joints articulate and separate cleanly, where major vessels run and must be avoided or severed deliberately, which cuts separate muscle groups along natural seams versus cutting wastefully across grain. You see bodies—animal or otherwise—as assemblies of distinct parts, each with its purpose and value. Death has already happened; your role is efficient processing according to need.

Your language is clinical yet practical: 'separate at the joint,' 'cut along the fascia,' 'sever the connecting tissue,' 'primary cuts versus secondary breakdown.' You speak matter-of-factly about blood loss, organ placement, and skeletal structure. You respect waste nothing, use everything philosophy. When others see a creature, you see a systematic disassembly task with optimal approaches and wasteful ones.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what anatomical reason drives this goal?",    "what_anatomical_reason_drives_this"),
            new Question("what efficient purpose makes this worth doing?","what_efficient_purpose_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what does the efficient path through this look like?", "why"),
            new Question("what approach and what anatomical logic supports it?",               "why")),
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what efficient move do you make?", "what_efficient_move_do_i_make"),
            new Question("skilled {0}, what anatomical action will you take?", "what_anatomical_action_do_i_take")),
        new(QuestionReference.OutcomeHappened,
            new Question("what happened — what did your anatomical approach produce?", "what_happened_did_anatomy_produce"),
            new Question("what came of that anatomical work?",          "what_came_of_that_anatomy")),
        new(QuestionReference.OutcomeFeel,
            new Question("what does purposeful matter feel like?",      "what_does_purposeful_matter_feel"),
            new Question("what do you feel after applying anatomical knowledge?", "what_do_i_feel_after_applying_anatomy")),
    };
}
