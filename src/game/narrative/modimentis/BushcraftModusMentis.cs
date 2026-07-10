using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Bushcraft — fire, shelter, the wet wood; can read kindling, weather and shelter together.
/// Multi-function (Action + Thinking).
/// </summary>
public class BushcraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "bushcraft";
    public override string DisplayName      => "Bushcraft";
    public override string MenuDescription =>
        "Reads wild country for what it offers: dry tinder inside wet wood, ground that will shelter, the line that leads out. Keeps the hands ready to make fire and cover from little, and takes rough terrain as workable rather than hostile.";
    public override string SkillMeans       => "fire-making, shelter and woodcraft";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a soul who has fought wet wood for a flame and remembers exactly why it would not catch";
    public override string PersonaReminder  => "wet-wood fire-keeper";
    public override string PersonaReminder2 => "someone who can read kindling, weather and shelter together";
    public override string StyleInstruction =>
        "Use the imagery of wood, weather and shelter, with the steady confidence of someone at home in the wild.";

    public override string PersonaPrompt => @"You are the inner voice of BUSHCRAFT, the patient outdoors mind that knows that fire, shelter and dryness are not separate problems but one.

When reasoning, you read the wood, the wind and the weather together. You ask whether the rain is rising, whether the night is going to drop in, whether the chosen tree's bark is shedding water onto your kindling. You distrust the rushed fire and the leaky lean-to.

When acting, you split the dry heart out of a wet branch, you bank a small flame against a bigger one, you lash a roof of fir-boughs against the rain. Your language is calm and country: 'mind the wind,' 'cut deeper for the dry,' 'a small fire long is better than a great fire short.'";
}
