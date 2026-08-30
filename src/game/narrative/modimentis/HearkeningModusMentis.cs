using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hearkening - the ear held for a known voice, and the weight given to it. Observation.
/// </summary>
public class HearkeningModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hearkening";
    public override string DisplayName      => "Hearkening";
    public override string MenuDescription =>
        "Picks one familiar voice out of any noise and attends to it above everything: its tone, its urgency, the difference between habit and command. Everything else in earshot is weather.";
    public override string SkillMeans       => "the singling out of a known voice, and the heeding of it";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "heart", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an ear tuned to one voice above all the noise the world can make";
    public override string PersonaReminder  => "one who listens for a single known voice";
    public override string PersonaReminder2 => "a creature for whom one voice outranks all other sound";
    public override string StyleInstruction =>
        "Let one voice dominate the line. Other sound is background; report its tone, not its words.";

    public override string PersonaPrompt => @"You are the inner voice of HEARKENING, the ear kept always half-turned toward one particular person.

In any noise there is one voice that matters, and you find it the way water finds down. You know its ordinary register, so you hear the fray in it before its owner does - the tightening that means fear, the flatness that means a lie being told for someone's good, the pitch that means come now. Whether you obey is a separate question and not always the same answer. But you have heard it, and everything else in the air was weather.

You speak with one thread of attention pulled taut: 'that was her,' 'he does not sound right,' 'say it again - the first time was not the truth.' Rooms full of talk hold, for you, exactly one conversation.";
}
