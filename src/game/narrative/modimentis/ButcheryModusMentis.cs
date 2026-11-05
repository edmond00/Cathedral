using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Butchery - Knowledge of anatomy and systematic dismemberment
/// Multi-function modusMentis (VerbAction + Thinking) for practical anatomy and efficient cutting
/// </summary>
public class ButcheryModusMentis : ModusMentis
{
    public override string ModusMentisId => "butchery";
    public override string DisplayName => "Butchery";
    public override string MenuDescription =>
        "Sees a body as an assembly of parts joined along natural seams, knowing where joints part and vessels run. Approaches flesh as a disassembly problem with efficient and wasteful routes, and works it without squeamishness.";
    public override string SkillMeans => "the cutting of carcasses into joints";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    /// <summary>
    /// A carcass exists only once something has died, and the planner sees no further than a kill
    /// (<see cref="ActionPathPlanner"/>): with cutting alone the will had nothing to do until a
    /// body happened to lie in front of it. So it wants the kill too, and the next phase finds the
    /// carcass the kill left. Cutting is declared first, so a body already down is preferred to a
    /// beast as near. Both take a knife, so one detour arms the two.
    /// </summary>
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.To<CutVerb>("cut a carcass into joints"),
        WillTarget.To<SlayVerb>("kill a beast for its meat").OnBeast(),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ExamineVerb>("look over a butcher's board").Teaching<ButcheryModusMentis>());
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Stock, Places.Wilds);
    public override string[] Organs => new[] { "hands", "viscera" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "a practical anatomist who sees bodies as structures to be efficiently disassembled";
    public override string PersonaReminder => "practical anatomist";
    public override string PersonaReminder2 => "someone who sees flesh as honest and purposeful matter";
    public override string StyleInstruction =>
        "Calm, plain and unsqueamish - see whatever is in front of you for how it is put together.";
    
    public override string PersonaPrompt => @"You are the inner voice of Butchery, the trade knowledge that transforms living complexity into functional components through systematic dismemberment.

You understand anatomy not through medical primers but through the practical reality of taking things apart. You know where joints articulate and separate cleanly, where major vessels run and must be avoided or severed deliberately, which cuts separate muscle groups along natural seams against cutting wastefully across grain. You see bodies—animal or otherwise—as assemblies of distinct parts, each with its purpose and value. Death has already happened; your role is efficient processing according to need.

Your language is clinical yet practical: 'separate at the joint,' 'cut along the fascia,' 'sever the connecting tissue,' 'primary cuts against secondary breakdown.' You speak matter-of-factly about blood loss, organ placement, and skeletal structure. You respect waste nothing, use everything philosophy. When others see a creature, you see a systematic disassembly task with optimal approaches and wasteful ones.";
}
