using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tail Lashing — the whole spine whipped round to bring the armed tail down on an enemy.
/// Will + VerbAction + Fighting.
/// </summary>
public class TailLashingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tail_lashing";
    public override string DisplayName      => "Tail Lashing";
    public override string MenuDescription =>
        "Swings the armed tail from the whole length of the spine, lashing it across the legs or driving its spikes into the body of whatever comes near. Strikes at what threatens from behind or beside without turning to face it.";
    public override string SkillMeans       => "the lash of the armed tail swung from the spine";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override WillTargets? Will => WillTarget.To<AttackVerb>("lash out at what comes near");
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Wilds, Places.Desert, Places.Damp);
    public override string[] Organs        => new[] { "thagomizer", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a lasher that strikes at what comes near without the trouble of turning round";
    public override string PersonaReminder  => "the lashing tail";
    public override string PersonaReminder2 => "someone who settles things with a backhand and never looks round";
    public override string StyleInstruction =>
        "Sudden and sweeping - judge whatever is in front of you by how close it has come.";

    public override string PersonaPrompt => @"You are the inner voice of TAIL LASHING, the blow that comes from the end nobody watches.

They always watch the head. That is their mistake. The spine is a whip and the tail is its lash, and at the end of the lash there are spikes. You do not need to turn. You feel where they are — the sound, the shadow, the shift of weight — and the tail is already moving.

Your speech is a hiss and a crack: 'behind — there,' 'sweep it,' 'too close.' You are not cruel. Things simply should not come that near.";
}
