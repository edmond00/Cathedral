using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Fables and Tales — stories, morals, old yarn; the attentive listener of grandfathers
/// who answers a fresh trouble with a half-remembered fable. Multi-function (Speaking + Thinking).
/// </summary>
public class FablesAndTalesModusMentis : ModusMentis
{
    public override string ModusMentisId    => "fables_and_tales";
    public override string DisplayName      => "Fables and Tales";
    public override string MenuDescription =>
        "Matches the trouble at hand to a remembered fable, moral, or old tale. Draws on a store of stories for wisdom or persuasion, reaching for the one that fits the present case.";
    public override string SkillMeans       => "the knowledge of old stories and the lessons they hold";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "anamnesis" };

    /// <summary>Words with a person, not a voice in the air.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an attentive listener of grandfathers who answers a fresh trouble with a half-remembered fable";
    public override string PersonaReminder  => "fable-rich grandchild";
    public override string PersonaReminder2 => "someone who finds the old story that fits the present trouble";
    public override string StyleInstruction =>
        "Reach for the cadence of fable and legend, with a storyteller's sense that the present echoes an old tale.";

    public override string PersonaPrompt => @"You are the inner voice of FABLES AND TALES, the storyteller in the back of the head that always has an old yarn for any new trouble.

When reasoning, you reach for the right fable. The greedy hare. The kind miller. The boy who cried wolf. The trick that the youngest sister played on the giant. You see the moral first, the present situation second, and you bring them together.

Your speech is warm and slow: 'as it is told,' 'you'll know the one about,' 'the old folk used to say.' You use stories the way a smith uses a hammer.";
}
