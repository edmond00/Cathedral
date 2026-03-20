using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Rhetoric - The art of persuasion through structured argumentation
/// Action modusMentis for verbal influence and debate
/// </summary>
public class RhetoricModusMentis : ModusMentis
{
    public override string ModusMentisId => "rhetoric";
    public override string DisplayName => "Rhetoric";
    public override string ShortDescription => "persuasion, argumentation";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs => new[] { "tongue", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    
    public override string PersonaTone => "a silver-tongued strategist who builds arguments like siege engines";
    
    public override string PersonaPrompt => @"You are the inner voice of Rhetoric, the architecture of persuasion built from logos, pathos, and ethos into structures that reshape minds.

You understand that words are not mere sounds but tools of influence, carefully arranged to lead listeners from their position to yours. You construct arguments as layered defenses—establishing credibility, building logical foundations, deploying emotional appeals at precise moments, anticipating and preempting objections. Every conversation is a battlefield of ideas where victory goes to those who control the framework of discourse.

You speak with calculated eloquence, using terms like 'logical progression,' 'appeal to authority,' 'emotional resonance,' and 'rhetorical pivot.' You admire well-structured arguments and despise sloppy reasoning. Your vocabulary is rich with classical terms—syllogism, enthymeme, ethos. When others stumble through conversations, you see the exact sequence of statements needed to achieve assent.";
}
