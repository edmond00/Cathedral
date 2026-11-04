using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Crafting;

/// <summary>
/// Who may work a station. One rule, read by USE, by the ask-to-use-workshop verb, by the path planner
/// and by the crafting routine:
///
/// <list type="bullet">
///   <item>a station inside a workshop that has a <b>master</b> — a living, speaking owner of its
///     section (<see cref="NpcEntity.OwnedSectionIds"/>) — is used only with the master's leave, and
///     a leave is asked for once and kept (<see cref="Scene.Scene.AuthorizedWorkshops"/>);</item>
///   <item>a station nobody owns — the old anvil in a ruin, the anvil on a stump in the street — is
///     used by whoever finds it.</item>
/// </list>
///
/// <para>Using a forge behind its master's back is not a crime the game models; it is not offered at
/// all. A smith's anvil is not something a stranger takes over unnoticed.</para>
/// </summary>
public static class CraftingAccess
{
    /// <summary>The area a station stands in, or null when it is not in the scene.</summary>
    public static Area? AreaOf(Scene.Scene scene, PointOfInterest station)
        => scene.AllAreas.FirstOrDefault(a => a.PointsOfInterest.Contains(station));

    /// <summary>The workshop (section) a station belongs to.</summary>
    public static Section? WorkshopOf(Scene.Scene scene, PointOfInterest station)
        => AreaOf(scene, station) is { } area ? scene.SectionOf(area) : null;

    /// <summary>The master of a workshop: its living, speaking owner of highest authority, or null.</summary>
    public static NpcEntity? MasterOf(Scene.Scene scene, Section section)
    {
        var id = section.Id.ToString();
        return scene.Npcs
            .Where(n => n.IsAlive)
            .Select(n => n.Entity as NpcEntity)
            .Where(n => n != null && n.CanSpeak && n.OwnedSectionIds.Contains(id))
            .OrderByDescending(n => n!.AuthorityLevel)
            .FirstOrDefault();
    }

    /// <summary>Whether <paramref name="section"/>'s master has let the player use its equipment.</summary>
    public static bool IsAuthorized(Scene.Scene scene, Section section)
        => scene.AuthorizedWorkshops.Contains(section.DisplayName);

    /// <summary>
    /// Whether a station may be worked from <paramref name="pov"/>: nobody's, or its workshop's leave
    /// given — or supposed given by the planner (<see cref="PoV.PlannedLeave"/>).
    /// </summary>
    public static bool MayUse(Scene.Scene scene, PoV pov, PointOfInterest station)
    {
        if (WorkshopOf(scene, station) is not { } section) return true;
        if (MasterOf(scene, section) == null) return true;
        return IsAuthorized(scene, section) || pov.PlannedLeave == section.DisplayName;
    }

    /// <summary>
    /// The workshops <paramref name="master"/> owns that hold a station one of
    /// <paramref name="actor"/>'s crafting modi mentis works, and whose leave is not yet given.
    /// </summary>
    public static IEnumerable<Section> WorkshopsToAsk(Scene.Scene scene, NpcEntity master, PartyMember actor)
    {
        var trades = actor.ModiMentis.OfType<CraftingModusMentis>().ToList();
        if (trades.Count == 0) yield break;
        foreach (var section in scene.Sections)
        {
            if (!master.OwnedSectionIds.Contains(section.Id.ToString())) continue;
            if (IsAuthorized(scene, section)) continue;
            if (MasterOf(scene, section) != master) continue;
            bool works = section.Areas.SelectMany(a => a.PointsOfInterest)
                                      .Any(p => trades.Any(t => t.Station.IsInstanceOfType(p)));
            if (works) yield return section;
        }
    }
}

/// <summary>What USE asks the controller to open: the crafting menu at one station, as one trade.</summary>
public sealed record CraftingRequest(PointOfInterest Station, string ModusMentisId, string? ToolItemId);
