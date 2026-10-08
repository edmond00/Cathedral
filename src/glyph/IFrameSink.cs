namespace Cathedral.Glyph
{
    /// <summary>
    /// Something that receives every finished frame before it is presented. <c>--record</c> is the one
    /// implementation: it resolves the frame into a target of its own, reads it back, and presents it.
    /// </summary>
    public interface IFrameSink
    {
        /// <summary>
        /// Called before anything is drawn. Binds and returns the framebuffer the finished frame should
        /// be resolved into (0 would be the window itself).
        /// </summary>
        int BeginFrame(int width, int height);

        /// <summary>
        /// Called once the frame is complete, before the buffers are swapped. The sink reads the frame
        /// back and copies it to the window.
        /// </summary>
        void EndFrame(int width, int height);
    }
}
