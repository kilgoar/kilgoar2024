Shader "Developer/LocalCoordDiffuse"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _BaseOffset ("Base Offset", Vector) = (1,1,1,0)
        _BaseScale ("Base Tiling", Vector) = (1,1,1,0)
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _SpecColor ("Specular", Color) = (0.2,0.2,0.2,1)
        _SpecGlossMap ("Specular (RGB) Occlusion (G)", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Scale", Float) = 1
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _EmissionMap ("Emission", 2D) = "white" {}
        [Toggle] _EmissionFresnel ("Emission Fresnel", Float) = 0
        _EmissionFresnelPower ("Power", Range(0, 16)) = 1
        [Toggle] _EmissionFresnelInvert ("Invert", Float) = 0
        
        // Blend Layer 1
        [Toggle] _BlendLayer1 ("Blend Layer 1 Enabled", Float) = 0
        _BlendLayer1_Color ("Color", Color) = (1,1,1,1)
        [Enum(White,0,Albedo Alpha,1)] _BlendLayer1_AlbedoTintMask ("Albedo Tint Mask", Float) = 0
        _BlendLayer1_AlbedoMap ("Albedo", 2D) = "grey" {}
        _BlendLayer1_AlbedoMapScroll ("Offset Scroll", Vector) = (0,0,0,0)
        _BlendLayer1_Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _BlendLayer1_SpecGlossMap ("Specular (RGB) Occlusion (G)", 2D) = "white" {}
        _BlendLayer1_SpecColor ("Specular", Color) = (1,1,1,1)
        _BlendLayer1_NormalMapScale ("Scale", Float) = 1
        [Normal] _BlendLayer1_NormalMap ("Normal Map", 2D) = "bump" {}
        _BlendLayer1_BlendMaskMap ("Height Map", 2D) = "black" {}
        [Toggle] _BlendLayer1_BlendMaskMapInvert ("Invert", Float) = 0
        _BlendLayer1_BlendMaskMapScroll ("Offset Scroll", Vector) = (0,0,0,0)
        [PowerSlider(4.0)] _BlendLayer1_BlendFactor ("Blend Factor", Range(0, 16)) = 8
        [PowerSlider(4.0)] _BlendLayer1_BlendFalloff ("Blend Falloff", Range(0.001, 128)) = 1
        
        // Blend Layer 2
        [Toggle] _BlendLayer2 ("Blend Layer 2 Enabled", Float) = 0
        _BlendLayer2_Color ("Color", Color) = (1,1,1,1)
        [Enum(White,0,Albedo Alpha,1)] _BlendLayer2_AlbedoTintMask ("Albedo Tint Mask", Float) = 0
        _BlendLayer2_AlbedoMap ("Albedo", 2D) = "grey" {}
        _BlendLayer2_AlbedoMapScroll ("Offset Scroll", Vector) = (0,0,0,0)
        _BlendLayer2_Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _BlendLayer2_SpecGlossMap ("Specular (RGB) Occlusion (G)", 2D) = "white" {}
        _BlendLayer2_SpecColor ("Specular", Color) = (1,1,1,1)
        _BlendLayer2_NormalMapScale ("Scale", Float) = 1
        [Normal] _BlendLayer2_NormalMap ("Normal Map", 2D) = "bump" {}
        _BlendLayer2_BlendMaskMap ("Height Map", 2D) = "black" {}
        [Toggle] _BlendLayer2_BlendMaskMapInvert ("Invert", Float) = 0
        _BlendLayer2_BlendMaskMapScroll ("Offset Scroll", Vector) = (0,0,0,0)
        [PowerSlider(4.0)] _BlendLayer2_BlendFactor ("Blend Factor", Range(0, 16)) = 8
        [PowerSlider(4.0)] _BlendLayer2_BlendFalloff ("Blend Falloff", Range(0.001, 128)) = 1
        
        // Blend Layer 3
        [Toggle] _BlendLayer3 ("Blend Layer 3 Enabled", Float) = 0
        _BlendLayer3_Color ("Color", Color) = (1,1,1,1)
        [Enum(White,0,Albedo Alpha,1)] _BlendLayer3_AlbedoTintMask ("Albedo Tint Mask", Float) = 0
        _BlendLayer3_AlbedoMap ("Albedo", 2D) = "grey" {}
        _BlendLayer3_AlbedoMapScroll ("Offset Scroll", Vector) = (0,0,0,0)
        _BlendLayer3_Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _BlendLayer3_SpecGlossMap ("Specular (RGB) Occlusion (G)", 2D) = "white" {}
        _BlendLayer3_SpecColor ("Specular", Color) = (1,1,1,1)
        _BlendLayer3_NormalMapScale ("Scale", Float) = 1
        [Normal] _BlendLayer3_NormalMap ("Normal Map", 2D) = "bump" {}
        _BlendLayer3_BlendMaskMap ("Height Map", 2D) = "black" {}
        [Toggle] _BlendLayer3_BlendMaskMapInvert ("Invert", Float) = 0
        _BlendLayer3_BlendMaskMapScroll ("Offset Scroll", Vector) = (0,0,0,0)
        [PowerSlider(4.0)] _BlendLayer3_BlendFactor ("Blend Factor", Range(0, 16)) = 8
        [PowerSlider(4.0)] _BlendLayer3_BlendFalloff ("Blend Falloff", Range(0.001, 128)) = 1

        // Rendering options
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 0
        [Enum(Opaque,0,Cutout,1,Fade,2,Transparent,3)] _Mode ("Blend Mode", Float) = 1
        _SrcBlend ("Src Blend", Float) = 1
        _DstBlend ("Dst Blend", Float) = 0
        _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "PerformanceChecks"="False" }
        LOD 300
        Cull [_Cull]
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf StandardSpecular fullforwardshadows vertex:vert alphatest:_Cutoff
        #pragma multi_compile_local _ _BLENDLAYER1
        #pragma multi_compile_local _ _BLENDLAYER2
        #pragma multi_compile_local _ _BLENDLAYER3
        #pragma multi_compile_local _ _EMISSIONFRESNEL

        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        // Core samplers
        sampler2D _MainTex;
        sampler2D _SpecGlossMap;
        sampler2D _BumpMap;
        sampler2D _EmissionMap;

        // Blend Layer 1 samplers
        sampler2D _BlendLayer1_AlbedoMap;
        sampler2D _BlendLayer1_SpecGlossMap;
        sampler2D _BlendLayer1_NormalMap;
        sampler2D _BlendLayer1_BlendMaskMap;

        // Blend Layer 2 samplers
        sampler2D _BlendLayer2_AlbedoMap;
        sampler2D _BlendLayer2_SpecGlossMap;
        sampler2D _BlendLayer2_NormalMap;
        sampler2D _BlendLayer2_BlendMaskMap;

        // Blend Layer 3 samplers
        sampler2D _BlendLayer3_AlbedoMap;
        sampler2D _BlendLayer3_SpecGlossMap;
        sampler2D _BlendLayer3_NormalMap;
        sampler2D _BlendLayer3_BlendMaskMap;

        // Properties
        fixed4 _Color;
        float _Glossiness;
        float _BumpScale;
        float _OcclusionStrength;
        fixed4 _EmissionColor;
        float _EmissionFresnelPower;
        float _EmissionFresnelInvert;
        float4 _BaseOffset;
        float4 _BaseScale;

        // Blend Layer 1 properties
        fixed4 _BlendLayer1_Color;
        float _BlendLayer1_AlbedoTintMask;
        float4 _BlendLayer1_AlbedoMapScroll;
        float _BlendLayer1_Glossiness;
        fixed4 _BlendLayer1_SpecColor;
        float _BlendLayer1_NormalMapScale;
        float _BlendLayer1_BlendMaskMapInvert;
        float4 _BlendLayer1_BlendMaskMapScroll;
        float _BlendLayer1_BlendFactor;
        float _BlendLayer1_BlendFalloff;

        // Blend Layer 2 properties
        fixed4 _BlendLayer2_Color;
        float _BlendLayer2_AlbedoTintMask;
        float4 _BlendLayer2_AlbedoMapScroll;
        float _BlendLayer2_Glossiness;
        fixed4 _BlendLayer2_SpecColor;
        float _BlendLayer2_NormalMapScale;
        float _BlendLayer2_BlendMaskMapInvert;
        float4 _BlendLayer2_BlendMaskMapScroll;
        float _BlendLayer2_BlendFactor;
        float _BlendLayer2_BlendFalloff;

        // Blend Layer 3 properties
        fixed4 _BlendLayer3_Color;
        float _BlendLayer3_AlbedoTintMask;
        float4 _BlendLayer3_AlbedoMapScroll;
        float _BlendLayer3_Glossiness;
        fixed4 _BlendLayer3_SpecColor;
        float _BlendLayer3_NormalMapScale;
        float _BlendLayer3_BlendMaskMapInvert;
        float4 _BlendLayer3_BlendMaskMapScroll;
        float _BlendLayer3_BlendFactor;
        float _BlendLayer3_BlendFalloff;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos; // Changed from localPos to worldPos
            float3 viewDir;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.uv_MainTex = v.texcoord.xy;
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; // Transform to world space
            o.viewDir = WorldSpaceViewDir(v.vertex);
            o.worldNormal = UnityObjectToWorldNormal(v.normal);
        }

        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            // Compute world-space position with offset and scale for tiling
            float3 worldPos = IN.worldPos;
            float3 scaledPos = worldPos * max(_BaseScale.xyz, 0.0001) + _BaseOffset.xyz; // Prevent zero scale

            // Select UVs based on dominant normal component for triplanar mapping
            float3 absNormal = abs(normalize(IN.worldNormal));
            float2 uv;
            if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
            {
                // X-facing face (±X), use YZ plane
                uv = frac(scaledPos.yz); // frac ensures seamless tiling
            }
            else if (absNormal.y > absNormal.z)
            {
                // Y-facing face (±Y), use XZ plane
                uv = frac(scaledPos.xz); // frac ensures seamless tiling
            }
            else
            {
                // Z-facing face (±Z), use XY plane
                uv = frac(scaledPos.xy); // frac ensures seamless tiling
            }

            // Sample albedo
            fixed4 albedo = tex2D(_MainTex, uv) * _Color;

            // Output
            o.Albedo = albedo.rgb;
            o.Alpha = 1.0; // Opaque
            o.Smoothness = 0.5;
        }
        ENDCG
    }
    CustomEditor "DevLocalCoordShaderGUI"
}