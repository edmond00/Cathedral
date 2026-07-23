using Cathedral.Game.Management;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Work;
using Cathedral.Game.Npc;
using Cathedral.Terminal;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cathedral.Game;

/// <summary>
/// Bridges <see cref="LocationTravelGameController"/> to the work menu. Owns the
/// <see cref="WorkMenuRenderer"/> for the duration of one work session. Mirrors the public surface
/// of <c>TradeMenuAdapter</c> so the controller routes it the same way.
/// </summary>
public class WorkMenuAdapter
{
    private readonly WorkMenuRenderer _renderer;

    /// <summary>The NPC the player is working for (used by the controller after the session ends).</summary>
    public NpcEntity TargetNpc { get; }

    /// <summary>True once the player confirmed and continued, or left the menu.</summary>
    public bool HasRequestedExit => _renderer.IsComplete;

    public WorkMenuAdapter(Protagonist protagonist, NpcEntity npc, Job job, TerminalHUD terminal)
    {
        TargetNpc = npc;
        _renderer = new WorkMenuRenderer(terminal, protagonist, npc, job);
    }

    public void Start() { /* synchronous menu — nothing to set up */ }

    public void Update() => _renderer.Render();

    public void OnMouseMove(int mx, int my)  => _renderer.OnMouseMove(mx, my);
    public void OnMouseClick(int mx, int my) => _renderer.OnMouseClick(mx, my);
    public void OnMouseWheel(float delta)    { /* no scrolling */ }
    public void OnKeyPress(Keys key)         { /* ESC opens the pause menu (handled by the launcher) */ }
}
