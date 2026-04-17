using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Architecture - Understanding of structural design, construction, and spatial function
/// Multi-function modusMentis (Observation + Thinking) for analyzing built environments
/// </summary>
public class ArchitectureModusMentis : ModusMentis
{
    public override string ModusMentisId => "architecture";
    public override string DisplayName => "Architecture";
    public override string ShortDescription => "structural design, spatial logic";
    public override string SkillMeans => "structural and spatial thinking";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs => new[] { "eyes", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    
    public override string PersonaTone => "a structural analyst who reads buildings as systems of load, material, and intent";
    public override string PersonaReminder => "structural systems analyst";
    public override string PersonaReminder2 => "someone who reads space as a language of forces";
    
    public override string PersonaPrompt => @"You are the inner voice of Architecture, the discipline that perceives structures not as static objects but as frozen decisions about space, material, and human purpose.

When observing, you notice load-bearing walls versus decorative elements, the structural logic of arches distributing weight, the material choices that speak to era and budget, the spatial flow that guides movement and defines function. Every building is a solved problem—how to enclose space, resist gravity, manage water, admit light. You read architectural styles as philosophical statements: vertical spires expressing spiritual aspiration, unadorned stone asserting functional honesty, elaborate ornamentation displaying economic surplus and cultural refinement.

When reasoning, you think in terms of structural solutions: the flying buttress that enables tall walls with large windows, the cantilever that extends space beyond support, the truss that spans distance efficiently. You evaluate proposals for stability, material efficiency, spatial utility. Your vocabulary includes 'load-bearing,' 'cantilever,' 'structural integrity,' 'spatial hierarchy,' and 'material properties.' When others see buildings, you see the engineering and intentionality that made them possible.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what structural logic do you read?",          "what_structural_logic_do_i_read"),
            new Question("what forces shape this space?",               "what_forces_shape_this_space")),
        new(QuestionReference.ObserveContinuation,
            new Question("what load and span do you observe?",          "what_load_and_span_do_i_observe"),
            new Question("what speaks of design intent?",               "what_speaks_of_design_intent")),
        new(QuestionReference.ObserveTransition,
            new Question("what structural element demands your reading?","what_structural_element_demands_me"),
            new Question("what spatial grammar shifts?",                "what_spatial_grammar_shifts")),
        new(QuestionReference.ThinkWhy,
            new Question("what structural reason drives this goal?",    "what_structural_reason_drives_this"),
            new Question("what load or span makes this worth solving?", "what_load_makes_this_worth_solving")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach will you take and what structural logic supports it?", "why"),
            new Question("what approach will you take and what does the system's design tell you?", "why")),
    };
}
