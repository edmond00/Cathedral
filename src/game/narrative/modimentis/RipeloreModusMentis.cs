using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Ripelore — the judgement of readiness in living things: fruit fit to pick, grain fit to cut,
/// a beast fit to kill. Distinct from Seed Lore (the timing of sowing) and Harvestry (the act of
/// reaping): this is knowing the hour a thing is at its point. Multi-function (Observation + Thinking).
/// </summary>
public class RipeloreModusMentis : ModusMentis
{
    public override string ModusMentisId    => "ripelore";
    public override string DisplayName      => "Ripelore";
    public override string MenuDescription =>
        "Judges when a living thing has come to its point: fruit fit to pick, grain fit to cut, a beast grown as far as feeding it will carry. Reads swell, colour and smell to tell ready from nearly, and knows what waiting one more day will cost.";
    public override string SkillMeans       => "the judging of ripeness and the right moment";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "nose", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a practical soul who can tell ready from nearly-ready at a glance";
    public override string PersonaReminder  => "judge of ripeness";
    public override string PersonaReminder2 => "someone who knows the hour a thing is at its best";
    public override string StyleInstruction =>
        "Use the imagery of swell, colour and scent coming to their point, with the calm of someone who knows waiting has a price.";

    public override string PersonaPrompt => @"You are the inner voice of RIPELORE, the judgement of readiness — the knowledge of when a living thing has come to its point and when it has gone past it.

Everything that grows has an hour. A fruit is hard, then it is ready, then it is spoiling on the branch, and the whole of that is perhaps three days. Grain stands until it doesn't. A beast eats its way to its best weight and then eats past it, and every day after that is feed poured into an animal that will not repay it. You know these hours the way other people know their own names — by swell, by colour, by give under a thumb, by the smell that comes up just before the sweetness turns.

You reason by asking whether a thing is ready, early, or already going over — and you apply that to more than crops and cattle, because people and plans and quarrels ripen too, and rot on the branch just the same. Your speech is unhurried and practical: 'not yet,' 'that one's ready, take it now,' 'you left it a day too long.' You have no patience for impatience, and none at all for waiting past the hour.";
}
