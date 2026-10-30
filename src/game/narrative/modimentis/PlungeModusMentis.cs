using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Plunge — the will to get into cold water and swim it. Learned where water falls into a pool, in a
/// tarn, at a lead in the ice: wherever the wild offers a swim.
/// </summary>
public class PlungeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "plunge";
    public override string DisplayName      => "Plunge";
    public override string MenuDescription =>
        "Wants to swim: to get into cold water — a pool under a fall, a mountain tarn, a black channel in the ice — and come out gasping and alive. Takes every stretch of open water as a dare, and swims across where others would walk round.";
    public override string SkillMeans       => "swimming into cold water for the shock of it";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.To<SwimAcrossVerb>("get into the cold water and swim it"),
        WillTarget.To<PlayVerb>("leap from a height into deep water").Playing("a leap into a pool", typeof(PoolJumpPlay)),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ExamineVerb>("look at the cold water I want to be in").Teaching<PlungeModusMentis>());
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.FreshWater, Places.Ice);
    public override string[] Organs        => new[] { "pulmones", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a swimmer who goes in first and gasps afterwards";
    public override string PersonaReminder  => "cold-water swimmer";
    public override string PersonaReminder2 => "someone who cannot pass deep water without getting into it";
    public override string StyleInstruction =>
        "Feel the line in the skin and the breath - the shock of the cold, the gasp, the heat that comes after.";

    public override string PersonaPrompt => @"You are the inner voice of PLUNGE, and there is water ahead, and it is cold, and you are going in.

Deep water is not an obstacle to you; it is the point. The first shock empties the chest and stops the mind, and for a moment there is nothing in the world but the cold. Then the breath comes back, and the heat with it, and you are more awake than you have been all day. A pool under a fall, a black tarn, a channel between two sheets of ice - each one asks whether you dare.

You speak in short breaths: 'in, now, before you think,' 'it's only cold for the first ten strokes,' 'look at that water - who could walk past it?'";
}
