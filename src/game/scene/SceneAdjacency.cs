using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene;

/// <summary>
/// What counts as being close enough to <b>overhear</b> something. Shared by
/// <see cref="WitnessSelector"/> and <see cref="ThreatSelector"/>, which asked this question in two
/// identical private copies.
///
/// <para><b>Earshot is the section.</b> A section is a building — a forge and its rooms, a farmhouse,
/// a stretch of open ground — and the model is simply that everyone inside one can hear what happens
/// in it. Sections are small enough for that to be fair: 2–7 areas, mean around 4, and no larger
/// outdoors than in. It also puts this in step with everything else nearby that already reasons in
/// sections — the witness and threat selectors both prefer the section's <i>owner</i>, and a fight
/// recruits its bystanders from the section.</para>
///
/// <para><b>Why not the AreaGraph.</b> A gate connector — a door, a stair, a cliff — deliberately has
/// <i>no</i> graph edge beside it; an edge would hand <c>MoveToAreaVerb</c> (difficulty 1, never
/// fails) a way around the gate, and <c>BuildingAudit</c> reports one as a fault. Right for
/// <i>walking</i>, wrong for <i>hearing</i>: read from the graph, two rooms joined by a door are not
/// neighbours, and the entire Audio tier vanished indoors. Of 3618 sampled private-area situations,
/// every witness was Visual (blocked outright) and not one was Audio — so nothing could ever be
/// overheard inside a building, and the confrontation that follows was unreachable there.</para>
///
/// <para>A door is a gate, not a wall. <c>--crime-audit</c> reports the tier distribution and fails
/// if the Audio tier is ever empty again.</para>
/// </summary>
public static class SceneAdjacency
{
    /// <summary>
    /// Every area within earshot of <paramref name="area"/>: the rest of its section. Never the area
    /// itself — that is the Visual tier, and the callers check it first.
    ///
    /// <para>Sections partition the areas (<c>BuildingAudit</c> makes an area in two sections or in
    /// none a fault), so the first match is the only match. An area in no section at all hears
    /// nothing rather than throwing: the audit is where that gets reported, not here.</para>
    /// </summary>
    public static IEnumerable<Area> WithinEarshot(Scene scene, Area area)
    {
        var section = scene.Sections.FirstOrDefault(s => s.Areas.Contains(area));
        if (section == null) yield break;

        foreach (var other in section.Areas)
            if (other.Id != area.Id) yield return other;
    }
}
