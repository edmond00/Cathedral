using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Vigil - the watching through the night.
/// </summary>
public class VigilModusMentis : ModusMentis
{
    public override string ModusMentisId    => "vigil";
    public override string DisplayName      => "Vigil";
    public override string MenuDescription =>
        "Keeps watch through the night in prayer or beside the dead: notices every sound in the dark hours, the candle guttering, the changes in the quiet. Is most awake when everyone else is asleep.";
    public override string SkillMeans       => "the watching through the night";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Emotion };
    public override WillTarget? Will => WillTarget.To<SitAndWaitVerb>("keep a vigil");
    public override string[] Organs        => new[] { "ears", "pineal_gland" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a watcher by candlelight in the dark hours";
    public override string PersonaReminder  => "keeper of vigils";
    public override string PersonaReminder2 => "someone most awake at the dead hour";
    public override string StyleInstruction =>
        "Heightened and wakeful - notice the quality of the quiet around you, and every small change in it.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(FocusLostOutcome), () => new PudorHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of VIGIL, and it is the hour when everyone else is asleep and you have never been more awake.

The candle gutters and straightens. A beam ticks as it cools. Somewhere a dog dreams aloud. Beside the dead or before the altar, the silence has a texture that daytime never has, and you have learned to read it: the quiet that is peace and the quiet that is something waiting.

You speak in whispers: 'listen,' 'the candle moved,' 'it is nearly dawn.'";
}
