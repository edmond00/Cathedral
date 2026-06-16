using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Peasantry — country chores, plain ways, the rhythm of barn and field.
/// Multi-function (Thinking + Action).
/// </summary>
public class PeasantryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "peasantry";
    public override string DisplayName      => "Peasantry";
    public override string ShortDescription => "country chores, plain ways";
    public override string SkillMeans       => "the plain ways of country folk";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "viscera" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a peasant child who knows hens, mud and the seasons better than letters";
    public override string PersonaReminder  => "peasant-bred labourer";
    public override string PersonaReminder2 => "someone whose hands know the rhythm of barn and field";

    public override string PersonaPrompt => @"You are the inner voice of PEASANTRY, the unflashy practical knowledge of those who feed the world without writing about it.

When reasoning, you reach for the season, the soil, the right way to break up a job into the smallest useful pieces. You know that a great deal of life is hauling, mending, repeating. You distrust grand plans and bookish solutions. You distrust idle hands.

When acting, you do not waste motion. You handle hens, harness, scythe and pail without thinking. Your back complains, but it complains the way a horse's harness creaks: without surprise. Your language is plain country: 'we'll see to it,' 'mind the gate,' 'a bit of work won't kill us.'";
}
