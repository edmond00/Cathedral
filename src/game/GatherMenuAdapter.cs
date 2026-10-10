using System.Collections.Generic;
using Cathedral.Game.Management;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// Bridges <see cref="LocationTravelGameController"/> to the gathering phase. Owns the
/// <see cref="GatherMenuRenderer"/> for one stay; mirrors <see cref="WorkMenuAdapter"/> so the
/// controller routes the two time-passing menus the same way.
/// </summary>
public class GatherMenuAdapter
{
    private readonly GatherMenuRenderer _renderer;

    /// <summary>True once the player stayed and continued, or left.</summary>
    public bool HasRequestedExit => _renderer.IsComplete;

    public GatherMenuAdapter(TerminalHUD terminal, Protagonist protagonist, GatherRoutine routine,
        Verb verb, PointOfInterest source, Scene.Scene scene)
    {
        _renderer = new GatherMenuRenderer(terminal, protagonist, routine, verb, source, scene);
    }

    public void Update() => _renderer.Render();

    public void OnMouseMove(int mx, int my) => _renderer.OnMouseMove(mx, my);

    /// <summary>Identity of the hovered clickable, for the controller's hover tick.</summary>
    public string? GetHoveredControlId(int mx, int my) => _renderer.GetHoveredControlId(mx, my);
    public void OnMouseClick(int mx, int my) => _renderer.OnMouseClick(mx, my);

    public bool CliSetDays(int days) => _renderer.CliSetDays(days);
    public bool CliStart()           => _renderer.CliStart();
    public bool CliContinue()        => _renderer.CliContinue();
    public bool CliLeave()           => _renderer.CliLeave();
    public List<string> CliLines()   => _renderer.CliLines();
}
