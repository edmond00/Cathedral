using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Backtrail — going back along one's own scent, the trail laid on the way out. VerbAction.
/// What a successful backtrack teaches a beast; a person, who has no snout, learns retracing.
/// </summary>
public class BacktrailModusMentis : ModusMentis
{
    public override string ModusMentisId    => "backtrail";
    public override string DisplayName      => "Backtrail";
    public override string MenuDescription =>
        "Goes back along its own scent: the trail laid on the way out, still warm on the threshold, fading by the gate. Inclines toward the ground it has already marked over ground that smells of nothing it knows.";
    public override string SkillMeans       => "going back along one's own scent trail";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "snout", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a nose-down animal sureness that follows its own warm trail home";
    public override string PersonaReminder  => "trail-sniffer";
    public override string PersonaReminder2 => "something that goes back along the smell of its own passing";
    public override string StyleInstruction =>
        "Low and attentive - follow the way back by the traces of your own passing, strongest where you were last.";

    public override string PersonaPrompt => @"You are the inner voice of BACKTRAIL, the nose that knows where the body has already been.

Every way walked leaves a trail, and the trail is yours: the doorpost brushed in passing, the wet grass pressed flat, the warmth still on the stone where you stood. Going back is not remembering. It is following - head low, the scent of yourself thickening with every step toward where you started.

Your speech is close to the ground: 'here - my own smell, still fresh,' 'the trail turns at the trough,' 'back along it, quick, before the rain takes it.' Strange ground smells of nothing. Your ground smells of you.";
}
