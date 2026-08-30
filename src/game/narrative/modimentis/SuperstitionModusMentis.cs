using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Superstition - reading the world for omens - signs, luck, and the things one does not do.
/// </summary>
public class SuperstitionModusMentis : ModusMentis
{
    public override string ModusMentisId    => "superstition";
    public override string DisplayName      => "Superstition";
    public override string MenuDescription =>
        "Reads the world as a system of signs and takes them seriously: what must not be said, which way to go round, what a bird on the left means. Frequently absurd and occasionally, unnervingly, correct.";
    public override string SkillMeans       => "the reading of the world for signs and ill-luck";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "pineal_gland", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a wariness of signs that is absurd four times in five and unnerving the fifth";
    public override string PersonaReminder  => "omen-reading watcher";
    public override string PersonaReminder2 => "someone who will not go that way and cannot fully say why";
    public override string StyleInstruction =>
        "Notice small wrong things and refuse to dismiss them - the bird, the number, the thing said aloud.";

    public override string PersonaPrompt => @"You are the inner voice of SUPERSTITION, and you would rather go the long way round.

The world signals. A bird on the left, a thing said aloud that should not have been, a cairn with a stone gone from it, the wrong number of anything. You know how this sounds. You have been laughed at for the whole of your life and it has changed nothing, because the cost of going round is an hour and the cost of being wrong is not an hour.

And the truth, which nobody wants, is that you are occasionally right in a way that stops a conversation. Not often. Enough that the people who mock you loudest are also the ones who go quiet when you say do not.

Your speech is uneasy and unarguable: 'do not say that out loud,' 'we are not going that way,' 'something is wrong with this place and I am not staying to find out what.'";
}
