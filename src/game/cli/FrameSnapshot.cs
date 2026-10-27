using System;
using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Cathedral.Glyph;

namespace Cathedral.Game.Cli;

/// <summary>
/// The CLI's <c>snap &lt;file.png&gt;</c>: the next frame drawn, written to a PNG, then the sink takes
/// itself off. <c>dump</c> sees cells and not colours, so this is what lets an agent look at a screen's
/// palette and layout. The frame is drawn into a target of its own and then blitted to the window, the
/// same way <see cref="Record.FrameRecorder"/> films: a hidden window's own back buffer is not
/// guaranteed to hold anything.
/// </summary>
public sealed class FrameSnapshot : IFrameSink
{
    private readonly GlyphSphereCore _core;
    private readonly string _path;
    private int _fbo, _tex, _rbo;

    /// <summary>Set once the PNG is written (or the capture failed); the error, if any.</summary>
    public bool Done { get; private set; }
    public string? Error { get; private set; }

    private FrameSnapshot(GlyphSphereCore core, string path) { _core = core; _path = path; }

    /// <summary>Arms the capture of the next frame. False when another sink (a recording) holds the frame.</summary>
    public static FrameSnapshot? Arm(GlyphSphereCore core, string path)
    {
        if (core.FrameSink != null) return null;
        var snap = new FrameSnapshot(core, path);
        core.FrameSink = snap;
        return snap;
    }

    public int BeginFrame(int width, int height)
    {
        _tex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _tex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        _rbo = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        _fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _tex, 0);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _rbo);
        GL.Viewport(0, 0, width, height);
        return _fbo;
    }

    public void EndFrame(int width, int height)
    {
        try
        {
            var buf = new byte[width * height * 4];
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbo);
            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, buf);
            using var image = Image.LoadPixelData<Rgba32>(buf, width, height);
            image.Mutate(i => i.Flip(FlipMode.Vertical));   // GL rows run bottom-up
            image.SaveAsPng(_path);
            CliMode.Emit($"snap: {_path}");
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            CliMode.HasFailedAssertion = true;
            CliMode.Emit($"FAIL: snap: {ex.Message}");
        }

        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbo);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        GL.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.DeleteFramebuffer(_fbo);
        GL.DeleteRenderbuffer(_rbo);
        GL.DeleteTexture(_tex);

        _core.FrameSink = null;
        Done = true;
    }
}
