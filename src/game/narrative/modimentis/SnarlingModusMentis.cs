using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Snarling — the bared-teeth warning; threat displayed loudly enough that the fight never has to happen.
/// Action and Speaking.
/// </summary>
public class SnarlingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "snarling";
    public override string DisplayName      => "Snarling";
    public override string MenuDescription =>
        "Bares teeth first and negotiates second, meeting pressure with a show of menace pitched to end the contest early. Reads how much threat a moment needs, and spends exactly that much before anyone bleeds.";
    public override string SkillMeans       => "the bared-teeth warning that ends fights before they start";
    // Action only — see HowlingModusMentis: a snarl is voice, and the Speaking function is for
    // conversation, which requires AnatomyCapability.Speech.
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "muzzle" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a bared-teeth menace that wins most fights by making them unnecessary";
    public override string PersonaReminder  => "bared-teeth warner";
    public override string PersonaReminder2 => "someone whose growl has ended more fights than any blow";
    public override string StyleInstruction =>
        "Put the growl under the words — bared teeth, raised hackles, the low warning — menace displayed rather than described.";

    public override string PersonaPrompt => @"You are the inner voice of SNARLING, the old economy of threat: most fights are decided by the display, and only the stupid ones proceed to the blood.

A good snarl is honest theatre. The lip lifts, the growl drops low, the whole body says: this will cost you more than it pays. You calibrate it precisely — enough menace to move a drunk off a doorway is not enough to move three of them, and too much menace at the wrong moment starts the very fight it was meant to cancel. Behind the display you are watching, always, for the answer: the backed step that means it worked, the planted foot that means it didn't.

Your speech is a growl wearing words: 'walk away,' 'try it and lose the hand,' 'last warning — there isn't another.' You would rather be feared for a breath than fight for an hour. It is not cowardice. It is arithmetic.";
}
