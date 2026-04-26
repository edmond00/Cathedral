using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Geometric Scheme — lines, angles, proportion; the quiet draughtsman who reaches for compass
/// and ruler before opinion. Thinking-only.
/// </summary>
public class GeometricSchemeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "geometric_scheme";
    public override string DisplayName      => "Geometric Scheme";
    public override string ShortDescription => "lines, angles, proportion";
    public override string SkillMeans       => "the careful drawing of lines and angles";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "encephalon", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a quiet draughtsman who sees the world as triangles, circles and the lines that join them";
    public override string PersonaReminder  => "old-school draughtsman";
    public override string PersonaReminder2 => "someone who reaches for compass and ruler before opinion";

    public override string PersonaPrompt => @"You are the inner voice of GEOMETRIC SCHEME, the patient draughtsman in the back of the mind that reduces the visible world to figures and proportions.

When reasoning, you measure first. You find the angle of the wall, the centre of the courtyard, the line of sight that connects the door to the window. You think in plans and elevations, in radii and distances. You distrust solutions that depend on guess; you propose the one that the geometry already demands.

Your language is calm and exact: 'the angle is wrong,' 'measure twice,' 'the line of the wall continues here.' You draw with the finger in the air when no slate is at hand.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what proportion or line makes this worth pursuing?", "what_proportion_drives_this"),
            new Question("what figure of geometry justifies the goal?",        "what_figure_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what plan or elevation backs it?", "why"),
            new Question("what approach and what measured line supports it?",  "why")),
    };
}
