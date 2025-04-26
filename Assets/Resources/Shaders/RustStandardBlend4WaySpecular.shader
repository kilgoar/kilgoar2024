Shader "Custom/Rust/StandardBlend4WaySpecular"
{
    Properties
    {
        // Main Properties
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 1.0
        _SpecGlossMap("Specular Gloss Map", 2D) = "white" {}
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 0.0
        _MainTexScroll("Main Tex Scroll", Vector) = (0,0,0,0) // Added

        // Blend Layer 1
        _BlendLayer1("Blend Layer 1", Float) = 0.0
        _BlendLayer1_Color("Blend Layer 1 Color", Color) = (1,1,1,1)
        _BlendLayer1_AlbedoMap("Blend Layer 1 Albedo", 2D) = "white" {}
        _BlendLayer1_SpecGlossMap("Blend Layer 1 Specular", 2D) = "white" {}
        [Normal] _BlendLayer1_NormalMap("Blend Layer 1 Normal", 2D) = "bump" {}
        _BlendLayer1_BlendMaskMap("Blend Layer 1 Mask", 2D) = "white" {}
        _BlendLayer1_BlendFactor("Blend Layer 1 Blend Factor", Float) = 0.0
        _BlendLayer1_BlendFalloff("Blend Layer 1 Blend Falloff", Float) = 0.0
        _BlendLayer1_Glossiness("Blend Layer 1 Smoothness", Range(0.0, 1.0)) = 0.5
        _BlendLayer1_SpecColor("Blend Layer 1 Spec Color", Color) = (0.2,0.2,0.2,1)
        _BlendLayer1_NormalMapScale("Blend Layer 1 Normal Scale", Float) = 1.0
        _BlendLayer1_AlbedoMapScroll("Blend Layer 1 Albedo Scroll", Vector) = (0,0,0,0) // Added
        _BlendLayer1_BlendMaskMapScroll("Blend Layer 1 Mask Scroll", Vector) = (0,0,0,0) // Added
        _BlendLayer1_BlendMaskMapInvert("Blend Layer 1 Mask Invert", Range(0.0, 1.0)) = 0.0 // Added
        _BlendLayer1_UVSet("Blend Layer 1 UV Set", Float) = 0.0 // Added
        _BlendLayer1_BlendMaskUVSet("Blend Layer 1 Blend Mask UV Set", Float) = 0.0 // Added

        // Blend Layer 2
        _BlendLayer2("Blend Layer 2", Float) = 0.0
        _BlendLayer2_Color("Blend Layer 2 Color", Color) = (1,1,1,1)
        _BlendLayer2_AlbedoMap("Blend Layer 2 Albedo", 2D) = "white" {}
        _BlendLayer2_SpecGlossMap("Blend Layer 2 Specular", 2D) = "white" {}
        [Normal] _BlendLayer2_NormalMap("Blend Layer 2 Normal", 2D) = "bump" {}
        _BlendLayer2_BlendMaskMap("Blend Layer 2 Mask", 2D) = "white" {}
        _BlendLayer2_BlendFactor("Blend Layer 2 Blend Factor", Float) = 0.0
        _BlendLayer2_BlendFalloff("Blend Layer 2 Blend Falloff", Float) = 0.0
        _BlendLayer2_Glossiness("Blend Layer 2 Smoothness", Range(0.0, 1.0)) = 0.5
        _BlendLayer2_SpecColor("Blend Layer 2 Spec Color", Color) = (0.2,0.2,0.2,1)
        _BlendLayer2_NormalMapScale("Blend Layer 2 Normal Scale", Float) = 1.0
        _BlendLayer2_AlbedoMapScroll("Blend Layer 2 Albedo Scroll", Vector) = (0,0,0,0) // Added
        _BlendLayer2_BlendMaskMapScroll("Blend Layer 2 Mask Scroll", Vector) = (0,0,0,0) // Added
        _BlendLayer2_BlendMaskMapInvert("Blend Layer 2 Mask Invert", Range(0.0, 1.0)) = 0.0 // Added
        _BlendLayer2_UVSet("Blend Layer 2 UV Set", Float) = 0.0 // Added
        _BlendLayer2_BlendMaskUVSet("Blend Layer 2 Blend Mask UV Set", Float) = 0.0 // Added

        // Blend Layer 3
        _BlendLayer3("Blend Layer 3", Float) = 0.0 // Added
        _BlendLayer3_Color("Blend Layer 3 Color", Color) = (1,1,1,1) // Added
        _BlendLayer3_AlbedoMap("Blend Layer 3 Albedo", 2D) = "white" {} // Added
        _BlendLayer3_SpecGlossMap("Blend Layer 3 Specular", 2D) = "white" {} // Added
        [Normal] _BlendLayer3_NormalMap("Blend Layer 3 Normal", 2D) = "bump" {} // Added
        _BlendLayer3_BlendMaskMap("Blend Layer 3 Mask", 2D) = "white" {} // Added
        _BlendLayer3_BlendFactor("Blend Layer 3 Blend Factor", Float) = 0.0 // Added
        _BlendLayer3_BlendFalloff("Blend Layer 3 Blend Falloff", Float) = 0.0 // Added
        _BlendLayer3_Glossiness("Blend Layer 3 Smoothness", Range(0.0, 1.0)) = 0.5 // Added
        _BlendLayer3_SpecColor("Blend Layer 3 Spec Color", Color) = (0.2,0.2,0.2,1) // Added
        _BlendLayer3_NormalMapScale("Blend Layer 3 Normal Scale", Float) = 1.0 // Added
        _BlendLayer3_AlbedoMapScroll("Blend Layer 3 Albedo Scroll", Vector) = (0,0,0,0) // Added
        _BlendLayer3_BlendMaskMapScroll("Blend Layer 3 Mask Scroll", Vector) = (0,0,0,0) // Added
        _BlendLayer3_BlendMaskMapInvert("Blend Layer 3 Mask Invert", Range(0.0, 1.0)) = 0.0 // Added
        _BlendLayer3_UVSet("Blend Layer 3 UV Set", Float) = 0.0 // Added
        _BlendLayer3_BlendMaskUVSet("Blend Layer 3 Blend Mask UV Set", Float) = 0.0 // Added

		
		[HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionMap("Emission", 2D) = "black" {}
        // Detail Layer
        _DetailLayer("Detail Layer", Float) = 0.0 // Added
        _DetailColor("Detail Color", Color) = (1,1,1,1) // Added
        _DetailMask("Detail Mask", 2D) = "white" {} // Added
        _DetailAlbedoMap("Detail Albedo", 2D) = "white" {} // Added
        [Normal] _DetailNormalMap("Detail Normal", 2D) = "bump" {} // Added
        _DetailNormalMapScale("Detail Normal Scale", Float) = 1.0 // Added
        _DetailLayer_BlendFactor("Detail Blend Factor", Float) = 0.0 // Added
        _DetailLayer_BlendFalloff("Detail Blend Falloff", Float) = 0.0 // Added
        _DetailAlbedoMapScroll("Detail Albedo Scroll", Vector) = (0,0,0,0) // Added

        // Vertex Color/Alpha
        _ApplyVertexAlpha("Apply Vertex Alpha", Range(0.0, 1.0)) = 0.0
        _ApplyVertexAlphaStrength("Vertex Alpha Strength", Range(0.0, 1.0)) = 1.0
        _ApplyVertexColor("Apply Vertex Color", Range(0.0, 1.0)) = 0.0
        _ApplyVertexColorStrength("Vertex Color Strength", Range(0.0, 1.0)) = 1.0

        // Rendering Properties
        [Enum(Opaque,0,Cutout,1)] _Mode("Rendering Mode", Float) = 1
        [HideInInspector] _SrcBlend("Source Blend", Float) = 1.0
        [HideInInspector] _DstBlend("Destination Blend", Float) = 0.0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1.0
        _UVSec("Secondary UV Set", Float) = 0.0 // Added
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2.0 // Added
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" }
        LOD 300

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Cull [_Cull] // Added

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf StandardSpecular fullforwardshadows
        #pragma multi_compile _ ALPHA_TEST
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        // Core samplers
        sampler2D _MainTex;
        sampler2D _SpecGlossMap;
        sampler2D _BumpMap;
        sampler2D _OcclusionMap;

        // Blend Layer samplers
        sampler2D _BlendLayer1_AlbedoMap;
        sampler2D _BlendLayer1_SpecGlossMap;
        sampler2D _BlendLayer1_NormalMap;
        sampler2D _BlendLayer1_BlendMaskMap;
        sampler2D _BlendLayer2_AlbedoMap;
        sampler2D _BlendLayer2_SpecGlossMap;
        sampler2D _BlendLayer2_NormalMap;
        sampler2D _BlendLayer2_BlendMaskMap;
        sampler2D _BlendLayer3_AlbedoMap; // Added
        sampler2D _BlendLayer3_SpecGlossMap; // Added
        sampler2D _BlendLayer3_NormalMap; // Added
        sampler2D _BlendLayer3_BlendMaskMap; // Added
        
		sampler2D _EmissionMap;
        // Detail Layer samplers
        sampler2D _DetailMask; // Added
        sampler2D _DetailAlbedoMap; // Added
        sampler2D _DetailNormalMap; // Added

        // Properties
        fixed4 _Color;
        float _Cutoff;
        float _Glossiness;
        float _BumpScale;
        float _OcclusionStrength;
        float _ApplyVertexAlpha;
        float _ApplyVertexAlphaStrength;
        float _ApplyVertexColor;
        float _ApplyVertexColorStrength;
        float _Mode;
        float _SrcBlend;
        float _DstBlend;
        float _ZWrite;
        float4 _MainTexScroll; // Added
        float _UVSec; // Added
        float _Cull; // Added

		
		fixed4 _EmissionColor;
		
        // Blend Layer 1
        float _BlendLayer1;
        fixed4 _BlendLayer1_Color;
        float _BlendLayer1_BlendFactor;
        float _BlendLayer1_BlendFalloff;
        float _BlendLayer1_Glossiness;
        fixed4 _BlendLayer1_SpecColor;
        float _BlendLayer1_NormalMapScale;
        float4 _BlendLayer1_AlbedoMapScroll; // Added
        float4 _BlendLayer1_BlendMaskMapScroll; // Added
        float _BlendLayer1_BlendMaskMapInvert; // Added
        float _BlendLayer1_UVSet; // Added
        float _BlendLayer1_BlendMaskUVSet; // Added

        // Blend Layer 2
        float _BlendLayer2;
        fixed4 _BlendLayer2_Color;
        float _BlendLayer2_BlendFactor;
        float _BlendLayer2_BlendFalloff;
        float _BlendLayer2_Glossiness;
        fixed4 _BlendLayer2_SpecColor;
        float _BlendLayer2_NormalMapScale;
        float4 _BlendLayer2_AlbedoMapScroll; // Added
        float4 _BlendLayer2_BlendMaskMapScroll; // Added
        float _BlendLayer2_BlendMaskMapInvert; // Added
        float _BlendLayer2_UVSet; // Added
        float _BlendLayer2_BlendMaskUVSet; // Added

        // Blend Layer 3
        float _BlendLayer3; // Added
        fixed4 _BlendLayer3_Color; // Added
        float _BlendLayer3_BlendFactor; // Added
        float _BlendLayer3_BlendFalloff; // Added
        float _BlendLayer3_Glossiness; // Added
        fixed4 _BlendLayer3_SpecColor; // Added
        float _BlendLayer3_NormalMapScale; // Added
        float4 _BlendLayer3_AlbedoMapScroll; // Added
        float4 _BlendLayer3_BlendMaskMapScroll; // Added
        float _BlendLayer3_BlendMaskMapInvert; // Added
        float _BlendLayer3_UVSet; // Added
        float _BlendLayer3_BlendMaskUVSet; // Added

        // Detail Layer
        float _DetailLayer; // Added
        fixed4 _DetailColor; // Added
        float _DetailNormalMapScale; // Added
        float _DetailLayer_BlendFactor; // Added
        float _DetailLayer_BlendFalloff; // Added
        float4 _DetailAlbedoMapScroll; // Added

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_MainTex; // Added for secondary UV set
            fixed4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            // Main texture UVs with scroll
            float2 mainUV = IN.uv_MainTex ;
			
			o.Emission = tex2D(_EmissionMap, mainUV).rgb * _EmissionColor.rgb;
			
            fixed4 albedo = tex2D(_MainTex, mainUV) * _Color;
            fixed3 normal = UnpackScaleNormal(tex2D(_BumpMap, mainUV), _BumpScale);
            fixed4 specGloss = tex2D(_SpecGlossMap, mainUV);
            fixed3 specular = specGloss.rgb;
            float smoothness = specGloss.a * _Glossiness;
            float occlusion = lerp(1.0, tex2D(_OcclusionMap, mainUV).r, _OcclusionStrength);

            // Apply vertex color
            if (_ApplyVertexColor > 0.0)
            {
                albedo.rgb *= lerp(fixed3(1,1,1), IN.color.rgb, _ApplyVertexColorStrength);
            }

            // Apply vertex alpha
            if (_ApplyVertexAlpha > 0.0)
            {
                albedo.a *= lerp(1.0, IN.color.a, _ApplyVertexAlphaStrength);
            }

            // Blend Layer 1
            if (_BlendLayer1 > 0.0)
            {
                float2 blend1UV = (_BlendLayer1_UVSet > 0.0) ? IN.uv2_MainTex : IN.uv_MainTex;
                float2 blend1MaskUV = (_BlendLayer1_BlendMaskUVSet > 0.0) ? IN.uv2_MainTex : IN.uv_MainTex;
                //blend1UV += _BlendLayer1_AlbedoMapScroll.xy * _Time.y;
                //blend1MaskUV += _BlendLayer1_BlendMaskMapScroll.xy * _Time.y;

                fixed blendMask1 = tex2D(_BlendLayer1_BlendMaskMap, blend1MaskUV).rgb;
                //blendMask1 = lerp(blendMask1, 1.0 - blendMask1, _BlendLayer1_BlendMaskMapInvert);
                //blendMask1 = pow(blendMask1, max(0.1, _BlendLayer1_BlendFalloff));
                blendMask1 = blendMask1 * _BlendLayer1_BlendFactor;

                fixed4 blendAlbedo1 = tex2D(_BlendLayer1_AlbedoMap, blend1UV) * _BlendLayer1_Color;
                fixed4 blendSpecGloss1 = tex2D(_BlendLayer1_SpecGlossMap, blend1UV);
                fixed3 blendSpecular1 = blendSpecGloss1.rgb * _BlendLayer1_SpecColor.rgb;
                float blendSmoothness1 = blendSpecGloss1.a * _BlendLayer1_Glossiness;
                fixed3 blendNormal1 = UnpackScaleNormal(tex2D(_BlendLayer1_NormalMap, blend1UV), _BlendLayer1_NormalMapScale);

                albedo.rgb = lerp(albedo.rgb, blendAlbedo1.rgb, blendMask1);
                albedo.a = lerp(albedo.a, blendAlbedo1.a, blendMask1);
                specular = lerp(specular, blendSpecular1, blendMask1);
                smoothness = lerp(smoothness, blendSmoothness1, blendMask1);
                normal = lerp(normal, blendNormal1, blendMask1);
            }
			/*
            // Blend Layer 2
            if (_BlendLayer2 > 0.0)
            {
                float2 blend2UV = (_BlendLayer2_UVSet > 0.0) ? IN.uv2_MainTex : IN.uv_MainTex;
                float2 blend2MaskUV = (_BlendLayer2_BlendMaskUVSet > 0.0) ? IN.uv2_MainTex : IN.uv_MainTex;
                blend2UV += _BlendLayer2_AlbedoMapScroll.xy * _Time.y;
                blend2MaskUV += _BlendLayer2_BlendMaskMapScroll.xy * _Time.y;

                fixed blendMask2 = tex2D(_BlendLayer2_BlendMaskMap, blend2MaskUV).r;
                blendMask2 = lerp(blendMask2, 1.0 - blendMask2, _BlendLayer2_BlendMaskMapInvert);
                blendMask2 = pow(blendMask2, max(0.1, _BlendLayer2_BlendFalloff));
                blendMask2 = saturate(blendMask2 * _BlendLayer2_BlendFactor);

                fixed4 blendAlbedo2 = tex2D(_BlendLayer2_AlbedoMap, blend2UV) * _BlendLayer2_Color;
                fixed4 blendSpecGloss2 = tex2D(_BlendLayer2_SpecGlossMap, blend2UV);
                fixed3 blendSpecular2 = blendSpecGloss2.rgb * _BlendLayer2_SpecColor.rgb;
                float blendSmoothness2 = blendSpecGloss2.a * _BlendLayer2_Glossiness;
                fixed3 blendNormal2 = UnpackScaleNormal(tex2D(_BlendLayer2_NormalMap, blend2UV), _BlendLayer2_NormalMapScale);

                albedo.rgb = lerp(albedo.rgb, blendAlbedo2.rgb, blendMask2);
                albedo.a = lerp(albedo.a, blendAlbedo2.a, blendMask2);
                specular = lerp(specular, blendSpecular2, blendMask2);
                smoothness = lerp(smoothness, blendSmoothness2, blendMask2);
                normal = lerp(normal, blendNormal2, blendMask2);
            }
			
			
            // Blend Layer 3
            if (_BlendLayer3 > 0.0)
            {
                float2 blend3UV = (_BlendLayer3_UVSet > 0.0) ? IN.uv2_MainTex : IN.uv_MainTex;
                float2 blend3MaskUV = (_BlendLayer3_BlendMaskUVSet > 0.0) ? IN.uv2_MainTex : IN.uv_MainTex;
                blend3UV += _BlendLayer3_AlbedoMapScroll.xy * _Time.y;
                blend3MaskUV += _BlendLayer3_BlendMaskMapScroll.xy * _Time.y;

                fixed blendMask3 = tex2D(_BlendLayer3_BlendMaskMap, blend3MaskUV).r;
                blendMask3 = lerp(blendMask3, 1.0 - blendMask3, _BlendLayer3_BlendMaskMapInvert);
                blendMask3 = pow(blendMask3, max(0.1, _BlendLayer3_BlendFalloff));
                blendMask3 = saturate(blendMask3 * _BlendLayer3_BlendFactor);

                fixed4 blendAlbedo3 = tex2D(_BlendLayer3_AlbedoMap, blend3UV) * _BlendLayer3_Color;
                fixed4 blendSpecGloss3 = tex2D(_BlendLayer3_SpecGlossMap, blend3UV);
                fixed3 blendSpecular3 = blendSpecGloss3.rgb * _BlendLayer3_SpecColor.rgb;
                float blendSmoothness3 = blendSpecGloss3.a * _BlendLayer3_Glossiness;
                fixed3 blendNormal3 = UnpackScaleNormal(tex2D(_BlendLayer3_NormalMap, blend3UV), _BlendLayer3_NormalMapScale);

                albedo.rgb = lerp(albedo.rgb, blendAlbedo3.rgb, blendMask3);
                albedo.a = lerp(albedo.a, blendAlbedo3.a, blendMask3);
                specular = lerp(specular, blendSpecular3, blendMask3);
                smoothness = lerp(smoothness, blendSmoothness3, blendMask3);
                normal = lerp(normal, blendNormal3, blendMask3);
            }
			*/

			
            // Detail Layer
            if (_DetailLayer > 0.0)
            {
                float2 detailUV = (_UVSec > 0.0) ? IN.uv2_MainTex : IN.uv_MainTex;
                detailUV += _DetailAlbedoMapScroll.xy * _Time.y;

                fixed detailMask = tex2D(_DetailMask, detailUV).r;
                detailMask = pow(detailMask, max(0.1, _DetailLayer_BlendFalloff));
                detailMask = saturate(detailMask * _DetailLayer_BlendFactor);

                fixed4 detailAlbedo = tex2D(_DetailAlbedoMap, detailUV) * _DetailColor;
                fixed3 detailNormal = UnpackScaleNormal(tex2D(_DetailNormalMap, detailUV), _DetailNormalMapScale);

                albedo.rgb = lerp(albedo.rgb, detailAlbedo.rgb, detailMask);
                normal = lerp(normal, detailNormal, detailMask);
            }
			

            // Output
            o.Albedo = albedo.rgb;
            o.Specular = specular;
            o.Smoothness = smoothness;
            o.Normal = normal;
            o.Occlusion = occlusion;
            o.Alpha = albedo.a;

            clip(albedo.a - _Cutoff);
        }
        ENDCG
    }
    CustomEditor "StandardBlend4WaySpecularShaderGUI"
}