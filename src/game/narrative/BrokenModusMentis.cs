using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A modus mentis the acting body can no longer carry, and what to say about it.
///
/// <para><b>The rule.</b> A wound subtracts from every max-level contribution its source feeds
/// (<c>MaxLevelPenalty</c>: −2 disabled, −1 impaired), so enough damage drags a modus mentis's
/// ceiling to 0 or below. <see cref="PartyMember.GetEffectiveModusMentisLevel"/> caps what was
/// learned by that ceiling, and a result at or below 0 leaves nothing to roll — the modus mentis
/// cannot be used at all, however well it was once known.</para>
///
/// <para><b>Offered and refused, not withheld.</b> A broken modus mentis stays in the observation,
/// thinking and action pools, and the refusal is narrated in its own voice naming the part of the
/// body that failed. Filtering it out silently would make a player's skills disappear with no
/// account of where they went, which is the one thing the memory panel cannot tell them mid-scene.
/// It costs a noetic point for the same reason every other refusal does: something was attempted.
/// <b>Speech is the exception</b> — a dialogue reply is written straight into the option list with
/// no narration frame to carry a refusal, so a broken speaking modus mentis is dropped from the
/// sample instead (see <c>DialogueOptionGenerator</c>), and running out of them falls through to
/// <c>ZeroRepliesDialogueRule</c>.</para>
/// </summary>
public static class BrokenModusMentis
{
    /// <summary>
    /// The wounded sources behind <paramref name="modusMentis"/>, in the shape
    /// <see cref="NeutralNarration"/> templates from. Worst first, so the sentence leads with the
    /// part that is out of use rather than the one merely failing.
    /// </summary>
    public static List<NeutralNarration.BrokenSource> SourcesFor(
        PartyMember member, ModusMentis modusMentis) =>
        member.GetImpairedSourcesForModusMentis(modusMentis)
            .Select(s => new NeutralNarration.BrokenSource(
                Label:    s.Label,
                Disabled: s.Contribution <= MaxLevelPenaltyDisabled,
                Wounds:   s.Wounds.Select(w => w.WoundName).Distinct().ToList()))
            .ToList();

    /// <summary>
    /// Mirror of <c>MaxLevelPenalty.Disabled</c>, which is internal to the stats file. Kept as a
    /// constant rather than reaching for the stat so this class does not have to know how a
    /// contribution is computed — only that the worse of the two tiers means "out of use".
    /// </summary>
    private const int MaxLevelPenaltyDisabled = -2;

    /// <summary>
    /// The fragment a coded action rule uses as its refusal reason — "my arms will not answer at
    /// all — a severed tendon". <see cref="NeutralNarration.ActionImpossible"/> supplies the frame
    /// ("I cannot force the door: …"), so this must stay a fragment that reads on after a colon,
    /// like every other <c>IActionRule</c> message.
    /// </summary>
    public static string ReasonFor(PartyMember member, ModusMentis modusMentis) =>
        NeutralNarration.BrokenSourcesPhrase(SourcesFor(member, modusMentis));

    /// <summary>
    /// The whole neutral sentence, for the phases that have no action to name — an observation or a
    /// thought that never happened. Handed to the modus mentis's own slot to re-express.
    /// </summary>
    public static string NeutralFor(
        PartyMember member, ModusMentis modusMentis, NeutralNarration.BrokenFaculty faculty) =>
        NeutralNarration.BrokenModusMentis(
            modusMentis.DisplayName, faculty, SourcesFor(member, modusMentis));
}
