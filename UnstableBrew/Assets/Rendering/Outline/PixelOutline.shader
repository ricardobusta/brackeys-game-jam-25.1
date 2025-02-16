Shader "Busta/PixelOutline"
{
    Properties {
        _OutlineColor ("OutlineColor", Color) = (1, 1, 1, 1)
        _FillColor ("FillColor", Color) = (1, 1, 1, 0.5)
        _OutlineThickness ("Outline Thickness", Float) = 1
    }
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // The Blit.hlsl file provides the vertex shader (Vert),
    // the input structure (Attributes), and the output structure (Varyings)
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    
    half4 _OutlineColor;
    half4 _FillColor;
    float _OutlineThickness;

    float4 PixelOutline(Varyings i) : SV_Target
    {
        half4 c = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord).a;
        
        half is_object = floor(c.a);
 
        half2 north = half2(0, _BlitTexture_TexelSize.y * _OutlineThickness);
        half2 south = half2(0, -_BlitTexture_TexelSize.y * _OutlineThickness);
        half2 east = half2(_BlitTexture_TexelSize.x * _OutlineThickness, 0);
        half2 west = half2(-_BlitTexture_TexelSize.x * _OutlineThickness, 0);
 
        half has_opaque_neighbor = 1 - SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + north + east).a;
        has_opaque_neighbor *= 1 - SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + north + west).a;
        has_opaque_neighbor *= 1 - SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + south + east).a;
        has_opaque_neighbor *= 1 - SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + south + west).a;
        has_opaque_neighbor = 1 - has_opaque_neighbor;
        
        half is_outline = (1-is_object) * has_opaque_neighbor;
 
        half4 col;
        col.rgb = _OutlineColor.rgb * (_OutlineColor.a * is_outline) + _FillColor.rgb * (_FillColor.a * is_object);
        col.a = is_outline * _OutlineColor.a + is_object * _FillColor.a;
        return col;
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"="Transparent"
        }
        LOD 100
        ZWrite Off
        Cull Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            Name "PixelOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment PixelOutline
            ENDHLSL
        }
    }
}