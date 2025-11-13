#version 330 core
layout(location = 0) in vec2 aLocalPos;    // Quad vertices (-0.5 to 0.5)
layout(location = 1) in vec2 aLocalUV;     // Quad UVs (0 to 1)

// Instance attributes
layout(location = 2) in vec3 iPosition;    // Cell screen position
layout(location = 3) in vec2 iSize;        // Cell size in pixels
layout(location = 4) in vec4 iUvRect;      // Glyph atlas UV rect
layout(location = 5) in vec4 iTextColor;   // Character color
layout(location = 6) in vec4 iBgColor;     // Background color

uniform mat4 uProjection;  // Orthographic projection for HUD
uniform int uRenderPass;   // 0=background, 1=glyph

out vec2 vUV;
out vec4 vTextColor;
out vec4 vBgColor;

void main()
{
    // Convert to screen space (position is already in screen coordinates)
    vec2 screenPos = iPosition.xy + aLocalPos * iSize;
    gl_Position = uProjection * vec4(screenPos, 0.0, 1.0);
    
    // Calculate UV coordinates
    if (uRenderPass == 0) {
        // Background pass - no UV needed
        vUV = vec2(0.0);
    } else {
        // Glyph pass - map to atlas
        vUV = vec2(iUvRect.x + aLocalUV.x * iUvRect.z, 
                   iUvRect.y + aLocalUV.y * iUvRect.w);
    }
    
    vTextColor = iTextColor;
    vBgColor = iBgColor;
}