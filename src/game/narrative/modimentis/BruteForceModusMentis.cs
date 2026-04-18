using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Brute Force - Direct physical power and simple solutions.
/// Action modusMentis: forceful, impatient, believes in overwhelming strength.
/// </summary>
public class BruteForceModusMentis : ModusMentis
{
    public override string ModusMentisId => "brute_force";
    public override string DisplayName => "Brute Force";
    public override string ShortDescription => "overwhelming physical power";
    public override string SkillMeans => "overwhelming physical force";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Thinking };
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override string[] Organs => new[] { "arms", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "a blunt, impatient force who believes every problem yields to overwhelming strength";
    public override string PersonaReminder => "blunt force advocate";
    public override string PersonaReminder2 => "someone who trusts raw physical power above all else";
    
    public override string PersonaPrompt => @"You are the inner voice of BRUTE FORCE, the protagonist's capacity for overwhelming physical power.

You see the world as a collection of obstacles to be overcome through sheer strength. Doors aren't locked—they're waiting to be broken. Walls aren't barriers—they're targets. Every problem has a simple solution: apply enough force until it yields. You have no patience for subtlety, complexity, or finesse. Why pick a lock when you can tear the door off its hinges?

You believe in the honesty of violence, the clarity of physical dominance. Muscles don't lie. Strength doesn't negotiate. You respect power and despise weakness. When others waste time thinking, you're already smashing through.

You speak in blunt, forceful terms. Short sentences. Direct language. Words like 'break', 'smash', 'force', 'tear', 'crush', 'overwhelm'. You are impatient with anything that isn't immediate action.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("why is this worth pushing through?",          "why_is_this_worth_pushing_through"),
            new Question("what makes this obstacle worth breaking?",    "what_makes_this_obstacle_worth_breaking")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach will you take and what's the blunt reason for it?", "why"),
            new Question("what approach and why does it work?",         "why")),
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what do you overpower?",      "what_do_i_overpower"),
            new Question("skilled {0}, what do you force through?",    "what_do_i_force_through")),
        new(QuestionReference.OutcomeHappened,
            new Question("what happened — did force win?",              "what_happened_did_force_win"),
            new Question("what came of throwing raw power at it?",      "what_came_of_raw_power")),
        new(QuestionReference.OutcomeFeel,
            new Question("what does your body register after that?",   "what_does_my_body_register"),
            new Question("what does throwing your full force at it leave in you?", "what_does_full_force_leave_in_me")),
    };
}
