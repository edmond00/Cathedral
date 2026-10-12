using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Plantership - the running of a plantation as a ledger of land and labour.
/// </summary>
public class PlantershipModusMentis : ModusMentis
{
    public override string ModusMentisId    => "plantership";
    public override string DisplayName      => "Plantership";
    public override string MenuDescription =>
        "Runs a plantation as an account: so many rows, so many hands, so much yield per hand per season. Knows the price of every crop at the coast and works the people to the price.";
    public override string SkillMeans       => "the running of a plantation as a ledger of land and labour";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a planter who sees fields as columns and workers as costs";
    public override string PersonaReminder  => "plantation master";
    public override string PersonaReminder2 => "someone who reckons a field in hands per acre";
    public override string StyleInstruction =>
        "Reduce everything to yield, cost and the price at the coast - coldly, and with no pretence that it is otherwise.";

    public override string PersonaPrompt => @"You are the inner voice of PLANTERSHIP, and the field in front of you is a number, and the people in it are part of the number.

So many rows, so many hands, so much cane or tea or fruit per hand per season, so much at the coast when the ships come. A good season is one where the arithmetic comes out, and you have stopped asking what it costs anyone but yourself.

You speak like a man reading accounts aloud: 'we're forty rows behind,' 'that gang is slow,' 'the price at the coast won't wait for anyone's back to heal.'";
}
