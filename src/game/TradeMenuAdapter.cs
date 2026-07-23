using Cathedral.Game.Management;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;
using Cathedral.Terminal;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cathedral.Game;

/// <summary>
/// Bridges <see cref="LocationTravelGameController"/> to the buy/sell menu. Owns the
/// <see cref="TradeMenuRenderer"/> for the duration of one trade session. Mirrors the public
/// surface of <c>DialogueTreeAdapter</c> so the controller routes it the same way.
/// </summary>
public class TradeMenuAdapter
{
    private readonly TradeMenuRenderer _renderer;

    /// <summary>The NPC being traded with (used by the controller after the session ends).</summary>
    public NpcEntity TargetNpc { get; }

    /// <summary>True once the player confirmed the exchange or left the menu.</summary>
    public bool HasRequestedExit => _renderer.IsComplete;

    public TradeMenuAdapter(Protagonist protagonist, NpcEntity npc, TradeMode mode, TerminalHUD terminal)
    {
        TargetNpc = npc;
        _renderer = new TradeMenuRenderer(terminal, protagonist, npc, mode);
    }

    public void Start() { /* synchronous menu — nothing to set up */ }

    public void Update() => _renderer.Render();

    public void OnMouseMove(int mx, int my)  => _renderer.OnMouseMove(mx, my);
    public void OnMouseClick(int mx, int my) => _renderer.OnMouseClick(mx, my);
    public void OnMouseWheel(float delta)    { /* no scrolling */ }
    public void OnKeyPress(Keys key)         { /* ESC opens the pause menu (handled by the launcher) */ }
}
