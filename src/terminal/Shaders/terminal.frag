#version 330 core
in vec2 vUV;
in vec4 vTextColor;
in vec4 vBgColor;

uniform sampler2D uGlyphAtlas;
uniform int uRenderPass;
uniform float uDarkenFactor;

out vec4 FragColor;

void main()
{
    if (uRenderPass == 0) {
        // Background pass - render solid background with darkening
        FragColor = vec4(vBgColor.rgb * uDarkenFactor, vBgColor.a);
    } else {
        // Glyph pass - render character with atlas texture
        vec4 atlasTexel = texture(uGlyphAtlas, vUV);
        
        // Use the red channel as alpha mask (glyph texture is white on transparent)
        float glyphAlpha = atlasTexel.r;
        
        if (glyphAlpha < 0.1) {
            discard; // Transparent areas of the glyph
        }
        
        // Apply text color with glyph alpha and darkening
        FragColor = vec4(vTextColor.rgb * uDarkenFactor, vTextColor.a * glyphAlpha);
    }
}