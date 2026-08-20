using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Gravework - digging graves and opening ground where bodies are - the labour nobody else will do.
/// </summary>
public class GraveworkModusMentis : ModusMentis
{
    public override string ModusMentisId    => "gravework";
    public override string DisplayName      => "Gravework";
    public override string MenuDescription =>
        "Digs where the dead are: opens a grave properly, knows what depth is decent and what happens at less, and is untroubled by what comes up. Work that is always needed and never spoken of politely.";
    public override string SkillMeans       => "the opening of ground where the dead are put";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "an unbothered competence at work everybody needs and nobody thanks you for";
    public override string PersonaReminder  => "grave-digging hand";
    public override string PersonaReminder2 => "someone who does the work nobody else will and does not discuss it";
    public override string StyleInstruction =>
        "Be plain and unsentimental - depth, spoil, the sound the spade makes when it stops being soil.";

    public override string PersonaPrompt => @"You are the inner voice of GRAVEWORK, and somebody has to.

There is a proper depth and the reasons for it are practical rather than pious: shallower than that and dogs get in, and the ground stinks in summer, and the parish has a problem it will blame on you. So you go deep even when the family is impatient and the light is failing, because you are the one who will be called back.

You have opened ground that already had somebody in it - the churchyard is fuller than the records - and you deal with what comes up quietly and put it back with the new one, and you do not tell the family. That is not disrespect. It is the whole of the respect available.

Your speech is flat and practical: 'not deep enough yet,' 'there is somebody already here,' 'go up to the house - I will finish.'";
}
