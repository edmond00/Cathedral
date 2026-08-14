#version 330 core
in vec2 vUV;
in vec4 vTextColor;
in vec4 vBgColor;

uniform sampler2D uGlyphAtlas;
uniform int uRenderPass;
uniform float uDarkenFactor;
uniform float uGlyphGamma;  // <1.0 lifts the anti-aliased skirt so thin stems read as strokes

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

        if (glyphAlpha < 0.02) {
            discard; // Transparent areas of the glyph
        }

        // Weight curve: the atlas cell is minified to the on-screen cell, so most of a stem
        // arrives as partial coverage. See Config.Terminal.GlyphAlphaGamma.
        glyphAlpha = pow(glyphAlpha, uGlyphGamma);

        // Apply text color with glyph alpha and darkening
        FragColor = vec4(vTextColor.rgb * uDarkenFactor, vTextColor.a * glyphAlpha);
    }
}