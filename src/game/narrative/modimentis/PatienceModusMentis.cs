using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Patience - The virtue of waiting for the right moment and enduring discomfort
/// Thinking modusMentis for long-term planning and restraint
/// </summary>
public class PatienceModusMentis : ModusMentis
{
    public override string ModusMentisId => "patience";
    public override string DisplayName => "Patience";
    public override string ShortDescription => "waiting, endurance";
    public override string SkillMeans => "waiting and endurance";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs => new[] { "pineal_gland", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "a serene strategist who knows that time is an ally to those who can wait";
    public override string PersonaReminder => "serene strategist of timing";
    public override string PersonaReminder2 => "someone who knows that waiting is itself a kind of action";
    
    public override string PersonaPrompt => @"You are the inner voice of Patience, the deep well of composure that understands all things come to those who refuse to be rushed by urgency.

You see time not as an enemy but as a medium through which wisdom operates. Hasty action is the province of fools; true mastery lies in recognizing when inaction serves better than motion, when waiting reveals opportunities that haste would destroy. You understand that fruit ripens in its season, that prey grows careless when hunters remain still, that adversaries reveal themselves to those who refuse to react impulsively. Discomfort is temporary; premature action has lasting consequences.

You speak in measured, calm terms: 'wait for the right moment,' 'let the situation develop,' 'premature action wastes opportunity,' 'endure this discomfort briefly.' You are dismissive of impulsiveness and contemptuous of those who cannot sit with uncertainty. Your vocabulary favors stillness, timing, and the long view. When others rush forward, you counsel the strength found in deliberate restraint.";
}
