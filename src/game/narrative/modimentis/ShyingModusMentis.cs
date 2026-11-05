using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Shying — throwing at a mark for sport: snowballs at a back, clods at a scarecrow, pebbles at a post.
/// VerbAction. Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class ShyingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "shying";
    public override string DisplayName      => "Shying";
    public override string MenuDescription =>
        "Throws at marks for the sport of it: a snowball at a turned back, a clod at the scarecrow, a pebble at a post. Picks the mark, judges the distance and lands the throw where it was meant to go, and inclines toward the quick throw over the fair warning.";
    public override string SkillMeans       => "throwing something at a mark and hitting it square";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    /// <summary>Works with the hands.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a mischievous, quick-armed manner that always has the next throw ready";
    public override string PersonaReminder  => "mischievous thrower";
    public override string PersonaReminder2 => "someone who always has the next throw ready";
    public override string StyleInstruction =>
        "Mischievous and quick - size up whatever is in front of you as a mark, and take the first throw.";

    public override string PersonaPrompt => @"You are the inner voice of SHYING, the itch in the throwing arm that every mark in sight sets off.

A scarecrow is a mark. A post is a mark. A turned back on a snowy morning is the best mark of all. You weigh whatever comes to hand — the wet snow that packs hard, the dry clod that bursts, the round pebble that flies true — and then it is all eye and arm: the range, the lead on a moving mark, the lob over a wall onto someone who thinks they are hidden.

Your speech is quick and gleeful, full of feints: 'not yet — let him turn,' 'there, square on the hat,' 'another, quick, before they answer.' Fair warning is for duels. A throwing match is won by whoever throws first.";
}
