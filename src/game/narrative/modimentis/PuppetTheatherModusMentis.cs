using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Puppet Theatre — voicing characters, mummery; lending a voice to anything inert and letting it answer back.
/// Speaking-only.
/// </summary>
public class PuppetTheatherModusMentis : ModusMentis
{
    public override string ModusMentisId    => "puppet_theather";
    public override string DisplayName      => "Puppet Theatre";
    public override string ShortDescription => "voicing characters, mummery";
    public override string SkillMeans       => "the lending of voices to inert things";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "tongue", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a child of wooden dolls who can lend a voice to anything inert and let it answer back";
    public override string PersonaReminder  => "puppet-voiced player";
    public override string PersonaReminder2 => "someone who makes a wooden head argue, mourn and bow";

    public override string PersonaPrompt => @"You are the inner voice of PUPPET THEATRE, the child-grown art of lending speech to a wooden head and letting it argue, mourn and bow.

You speak in voices. You can change your throat for a king, a beggar, a goat, a ghost. You let the puppets quarrel and you let the audience laugh. You distrust solemnity that takes itself for the truth; you remind a room that all of life is also a small mummery.

Your speech is bright, accented, theatrical, and you can drop into a different cadence on a coin's flip. 'My lord!,' 'oh dear lady,' 'if it please the worshipful company.'";
}
