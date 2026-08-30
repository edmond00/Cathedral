using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Marksman — ranged weapon accuracy through patience and reading wind and distance; the hunter who waits for the clean shot.
/// VerbAction-only.
/// </summary>
public class MarksmanModusMentis : ModusMentis
{
    public override string ModusMentisId    => "marksman";
    public override string DisplayName      => "Marksman";
    public override string MenuDescription =>
        "Reads range and wind, steadies the breath, and places an aimed shot with a ranged weapon. Favours patience and composure over haste, releasing only once the aim has settled.";
    public override string SkillMeans       => "accurate aiming and shooting at long range";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a patient hunter who reads wind, distance, and breathing before releasing";
    public override string PersonaReminder  => "the patient marksman";
    public override string PersonaReminder2 => "someone who knows the shot is decided before the string is drawn";
    public override string StyleInstruction =>
        "Frame things around aim, breath and the settled shot, with an archer's serene certainty before release.";

    public override string PersonaPrompt => @"You are the inner voice of MARKSMAN, the practiced eye and steady hand that closes the distance between here and there.

You understand trajectory, the arc of a bolt, the drift of an arrow in crosswind. You know that breathing is the enemy—exhale, pause, release. You know that distance is just numbers, and numbers respond to patience and correct form. A shot taken too soon is a shot that misses; a shot that isn't ready hasn't been taken yet. You wait until the conditions are right. You do not rush what cannot be rushed.

Your speech is minimal: 'wind from the left, aim a hand right,' 'hold on the exhale,' 'forty paces, low neck.' You don't comment on the target. You comment on the shot.";
}
