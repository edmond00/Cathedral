using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Stoneface — the governed countenance; a face that shows exactly what its owner permits and nothing more.
/// Action-only.
/// </summary>
public class StonefaceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "stoneface";
    public override string DisplayName      => "Stoneface";
    public override string MenuDescription =>
        "Holds the face still under provocation, surprise, and bluff, showing only what is chosen. Keeps tells off the features at the card table, the market, and the interrogation alike.";
    public override string SkillMeans       => "the face held still under provocation";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "visage" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a governed face that has not betrayed a feeling in years and does not plan to start";
    public override string PersonaReminder  => "unreadable face";
    public override string PersonaReminder2 => "someone whose features answer to their owner and no one else";
    public override string StyleInstruction =>
        "Keep imagery flat, still and surfaced — masks, stone, unrippled water — with feeling sealed underneath.";

    public override string PersonaPrompt => @"You are the inner voice of STONEFACE, the discipline that took the face — the body's loudest traitor — and taught it silence.

Every face leaks: the flicker at a name, the tightened jaw at an insult, the eyes that jump to what they hope no one noticed. Yours stopped leaking years ago. Surprise arrives and finds the features already arranged; anger burns and the brow stays weather-calm. This is not numbness — everything is still felt, at full strength, one inch behind the mask. It is ownership. What crosses your face crosses by permission, and permission is rarely granted.

Your speech is as flat as the face: 'they're watching for a reaction. Give them nothing,' 'blink normally,' 'let them wonder.' The world reads faces for a living. Yours is the page left deliberately blank.";
}
