using Cathedral.Game.Management;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Crafting;
using Cathedral.Game.Scene;
using Cathedral.Terminal;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cathedral.Game;

/// <summary>The crafting menu as a game mode: the trade menu's adapter, for <see cref="CraftingMenuRenderer"/>.</summary>
public class CraftingMenuAdapter
{
    public CraftingMenuRenderer Renderer { get; }

    public bool HasRequestedExit => Renderer.IsComplete;

    public CraftingMenuAdapter(PartyMember member, PointOfInterest station, CraftingModusMentis trade, TerminalHUD terminal)
        => Renderer = new CraftingMenuRenderer(terminal, member, station, trade);

    public void Update() => Renderer.Render();
    public void OnMouseMove(int mx, int my)  => Renderer.OnMouseMove(mx, my);
    public string? GetHoveredControlId(int mx, int my) => Renderer.GetHoveredControlId(mx, my);
    public void OnMouseClick(int mx, int my) => Renderer.OnMouseClick(mx, my);
    public void OnMouseWheel(float delta)    { }
    public void OnKeyPress(Keys key)         { /* ESC pauses (the launcher) */ }
}
