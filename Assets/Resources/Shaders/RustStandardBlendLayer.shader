Shader "Custom/Rust/StandardBlendLayer"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.0
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        // Simplified Detail Properties
        _DetailAlbedoMap("Detail Albedo", 2D) = "grey" {}
        [Normal] _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
        _DetailNormalMapScale("Detail Normal Scale", Float) = 1.0
        _DetailBlendMaskMap("Detail Blend Mask", 2D) = "white" {}
        _DetailColor("Detail Color", Color) = (1,1,1,1)
        _DetailBlendFactor("Detail Blend Factor", Float) = 0.0
        _DetailBlendFalloff("Detail Blend Falloff", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        // Samplers
        sampler2D _MainTex;
        sampler2D _MetallicGlossMap;
        sampler2D _BumpMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;
        sampler2D _DetailAlbedoMap;
        sampler2D _DetailNormalMap;
        sampler2D _DetailBlendMaskMap;

        // Properties
        fixed4 _Color;
        float _Cutoff;
        float _Glossiness;
        float _Metallic;
        float _BumpScale;
        float _OcclusionStrength;
        fixed4 _EmissionColor;
        fixed4 _DetailColor;
        float _DetailBlendFactor;
        float _DetailBlendFalloff;
        float _DetailNormalMapScale;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            fixed4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;

            // Main Textures
            fixed4 albedo = tex2D(_MainTex, uv) * _Color;
            fixed4 metallicGloss = tex2D(_MetallicGlossMap, uv);
            float metallic = metallicGloss.r;
            float smoothness = metallicGloss.a * _Glossiness;
            float3 normal = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
            float occlusion = lerp(1.0, tex2D(_OcclusionMap, uv).r, _OcclusionStrength);
            fixed3 emission = tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb;

            // Detail Layer
            float blendMask = tex2D(_DetailBlendMaskMap, uv).r;
            blendMask = pow(blendMask, max(0.1, _DetailBlendFalloff));
            blendMask = saturate(blendMask * _DetailBlendFactor);

            fixed4 detailAlbedo = tex2D(_DetailAlbedoMap, uv) * _DetailColor;
            float3 detailNormal = UnpackScaleNormal(tex2D(_DetailNormalMap, uv), _DetailNormalMapScale);

            albedo.rgb = lerp(albedo.rgb, detailAlbedo.rgb, blendMask);
            normal = lerp(normal, detailNormal, blendMask);

            // Output
            o.Albedo = albedo.rgb;
            //o.Metallic = metallic;
            //o.Smoothness = smoothness;
            o.Normal = normal;
            o.Occlusion = occlusion;
            o.Emission = emission;
            o.Alpha = albedo.a;

            clip(albedo.a - _Cutoff);
        }
        ENDCG
    }

    CustomEditor "RustStandardBlendLayerShaderGUI"
}