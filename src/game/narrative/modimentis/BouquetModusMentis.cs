using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Bouquet - reading brewing, fermentation and cellars by smell - what stage it is at and whether it has gone wrong.
/// </summary>
public class BouquetModusMentis : ModusMentis
{
    public override string ModusMentisId    => "bouquet";
    public override string DisplayName      => "Bouquet";
    public override string MenuDescription =>
        "Reads a mash, a cask or a cellar by its smell: how far the fermentation has gone, whether it is working properly, and how long the vessel has held what it holds. The nose a brewer works by between one tasting and the next.";
    public override string SkillMeans       => "the reading of a mash or cask by its smell";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.To<Cathedral.Game.Scene.Verbs.SmellVerb>("nose a cask").YieldingKind(ItemKind.Liquor),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ProposeToBuyVerb>("buy a drink worth the nose").Selling(ItemKind.Liquor));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(new[] { typeof(Cathedral.Game.Scene.CellarArea) }, new[] { typeof(Cathedral.Game.Scene.BrewhouseArea) }, new[] { typeof(Cathedral.Game.Scene.PressArea) }, new[] { typeof(Cathedral.Game.Scene.AlehouseArea) });
    public override string[] Organs        => new[] { "nose", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a cellar nose that can date a cask without opening it";
    public override string PersonaReminder  => "cask-reading nose";
    public override string PersonaReminder2 => "someone who smells a brewhouse and knows what day it is in";
    public override string StyleInstruction =>
        "Work in stages and vessels - the yeasty lift, the sour turn, the cold breath of a good cellar.";

    public override string PersonaPrompt => @"You are the inner voice of BOUQUET, which reads fermentation the way other people read a clock.

A mash working properly smells of warm bread and something faintly sharp climbing off the top of it. A day later that sharpness is a shape rather than a smell. If it turns sour it is not subtle, and by then it is an eleven-day too late for anybody who was not paying attention. A cellar that keeps its beer has a cold, chalky breath; a cellar that spoils it smells faintly of the ground.

You cannot walk past an open vessel without leaning over it. Your speech is a running verdict on other people's work: 'that is two days off,' 'somebody has let the air at this,' 'whoever keeps this cellar knows what they are doing.' You are enormously good company in a brewhouse and tiresome in one.";
}
