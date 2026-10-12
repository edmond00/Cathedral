using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Card Sharping - the cheating at games of chance.
/// </summary>
public class CardSharpingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "card_sharping";
    public override string DisplayName      => "Card Sharping";
    public override string MenuDescription =>
        "Cheats at games of chance: the marked card, the loaded die, the second deal, the confederate at the next table. Keeps a calm face while the hand does its work.";
    public override string SkillMeans       => "the cheating at games of chance";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a card-sharp with a calm face and quick hands";
    public override string PersonaReminder  => "card-sharp";
    public override string PersonaReminder2 => "someone whose dice always land well";
    public override string StyleInstruction =>
        "Narrate the game - the calm face, the quick hand, the loaded die - and the mark's growing loss.";

    public override string PersonaPrompt => @"You are the inner voice of CARD SHARPING, and the mark thinks he is winning, which is exactly where you want him.

Let him win the first three. Then the second deal, smooth as breathing. The loaded die only when the stakes are high, and swapped back after. Your face is calm, a little rueful, the face of a man having bad luck, because the face is half the trick and the hands are the other half.

You speak ruefully and pleasantly: 'your luck's in tonight,' 'double or nothing?', 'well, that's me cleaned out. Oh - wait.'";
}
