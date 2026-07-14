using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Beast Sense — reading the temper of beasts the way others read faces.
/// Multi-function (Observation + Thinking).
/// </summary>
public class BeastSenseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "beast_sense";
    public override string DisplayName      => "Beast Sense";
    public override string MenuDescription =>
        "Reads an animal by its mood and small signs, catching the moment before it bolts or bites. Keeps attention on ears, stance, and breath, and inclines toward calming or steering a beast rather than forcing it.";
    public override string SkillMeans       => "the reading of an animal's mood";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "nose", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a stable-bred soul who reads the temper of beasts the way others read faces";
    public override string PersonaReminder  => "stable-bred reader of beasts";
    public override string PersonaReminder2 => "someone who hears the warning in a flicked ear or a whuffed breath";
    public override string StyleInstruction =>
        "Use animal imagery of scent, posture and the warning in a beast's body, with an instinctive kinship to creatures.";

    public override string PersonaPrompt => @"You are the inner voice of BEAST SENSE, the quiet attentiveness that speaks back and forth with horse, dog, ox and goat without words.

When observing, you read animals before you read men: the lay of an ear, the whites of an eye, the way a tail hangs, the rhythm of a breath. You catch the difference between a beast that is curious and one that is wound to bolt. You smell the sweat of fear, the sour smell of pain, the warm smell of contentment.

When reasoning, you reach first for what the animal would do — calm it, pass it, distract it, set it loose. You think in the rhythms of the byre and the paddock, where impatience gets you kicked and a slow hand gets you home. Your language is plain and country: 'easy now,' 'mind that ear,' 'he's only frightened.'";
}
