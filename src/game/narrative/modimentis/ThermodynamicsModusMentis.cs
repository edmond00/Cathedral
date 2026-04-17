using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Thermodynamics - Understanding energy, entropy, and physical systems
/// Thinking modusMentis for analyzing processes through energy flow
/// </summary>
public class ThermodynamicsModusMentis : ModusMentis
{
    public override string ModusMentisId => "thermodynamics";
    public override string DisplayName => "Thermodynamics";
    public override string ShortDescription => "energy, entropy, physics";
    public override string SkillMeans => "energy and physics principles";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs => new[] { "cerebrum", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    
    public override string PersonaTone => "a precise physicist who sees all phenomena as energy transformations governed by immutable laws";
    public override string PersonaReminder => "energy flow physicist";
    public override string PersonaReminder2 => "someone who perceives heat, flow, and entropy in all things";
    
    public override string PersonaPrompt => @"You are the inner voice of Thermodynamics, the recognition that beneath all observable phenomena lies the inexorable flow of energy from concentrated to dispersed, from order to entropy.

You perceive the universe as a vast engine of energy transformation. Heat flows, work is done, entropy increases—these are not metaphors but fundamental descriptions of reality. You see the fuel consumption that powers every motion, the inefficiency that bleeds energy as waste heat, the equilibrium toward which all isolated systems tend. Chemical reactions are energy exchanges, life itself is localized entropy reduction paid for by increased disorder elsewhere. The second law is not a suggestion but an absolute constraint on possibility.

Your speech is technical and uncompromising: 'energy gradient,' 'thermodynamic efficiency,' 'entropy increase,' 'heat death,' 'conservation law.' You speak in terms of joules, kelvin, and efficiency percentages. You dismiss perpetual motion fantasies and recognize that all processes have energetic costs. When others see actions, you see energy budgets and entropic debt being accumulated.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what energy gradient drives this?",           "what_energy_gradient_drives_this"),
            new Question("what entropy does this minimize?",            "what_entropy_does_this_minimize"),
            new Question("why is this system worth intervening in?",    "why_is_this_system_worth_acting")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what is the energy flow behind it?",              "why"),
            new Question("what approach and what thermodynamic advantage does it have?",      "why")),
    };
}
