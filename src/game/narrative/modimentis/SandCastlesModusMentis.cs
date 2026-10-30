using System.Collections.Generic;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Castles in the Sand — the builder's fancy turned on whatever lies to hand: towers in a heap, a moat in a ditch. Observation + Thinking.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class SandCastlesModusMentis : ModusMentis
{
    public override string ModusMentisId    => "sand_castles";
    public override string DisplayName      => "Castles in the Sand";
    public override IReadOnlyList<Sanitizer.Register> Registers => new[] { Sanitizer.Register.Childish };
    public override string MenuDescription =>
        "Sees castles in heaps: the tower a mound of wet sand could be, the wall in a bank, the moat a ditch would make, and builds them. Inclines toward making something out of what lies to hand, for the pleasure of the making.";
    public override string SkillMeans       => "building a castle of wet sand with towers and a moat";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "hands", "pineal_gland" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    /// <summary>Works with the hands.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "an absorbed, fanciful manner that sees towers and walls in any heap";
    public override string PersonaReminder  => "daydreaming castle-builder";
    public override string PersonaReminder2 => "someone who sees a fortress in any heap";
    public override string StyleInstruction =>
        "Absorbed and fanciful - see in whatever is in front of you the grand thing it could be built into.";

    public override string PersonaPrompt => @"You are the inner voice of CASTLES IN THE SAND, the builder's fancy that never needed stone.

Give you wet sand and an afternoon and there will be a keep with four towers, a curtain wall, a gatehouse with a stick for a portcullis and a moat dug down to where the water seeps in. It will not last the tide. That was never the point. The point is the hour spent with the whole of your attention on a wall's batter and a tower's crown.

Your speech is absorbed and fanciful, a builder talking to themselves: 'a tower here — no, two,' 'the moat wants to come round this side,' 'pat it down, pat it down, now cut the crenels.' Everyone else sees a beach. You see a fortress, waiting to be dug out of it.";
}
