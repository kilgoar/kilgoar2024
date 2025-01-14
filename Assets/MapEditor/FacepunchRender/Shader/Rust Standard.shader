Shader "Rust/Standard"
{
    Properties
    {
        [MaterialToggle] _DoubleSided ("Double Sided", Float) = 0
        [KeywordEnum(Standard,Specular Color,Anisotropic,Transmission,Subsurface Scattering)] _MaterialType ("Material Type", Float) = 0
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _MainTexScroll ("Scroll", Vector) = (0,0,0,0)
        [MaterialToggle] _AlphaDither ("Alpha Dither", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        [Enum(UV0,0,UV1,1)] _AlphaUVSec ("UV Set for Alpha", Float) = 0
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0
        _MetallicGlossMap ("Metallic", 2D) = "white" {}
        _SpecColor ("Specular", Color) = (0.2,0.2,0.2,1)
        _SpecGlossMap ("Specular", 2D) = "white" {}
        [ToggleUI] _EnergyConservingSpecularColor ("Energy Conserving Specular Color", Float) = 1
        _BumpScale ("Scale", Float) = 1
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _TangentMap ("Tangent Map", 2D) = "bump" {}
        _Anisotropy ("Anisotropy", Range(-1, 1)) = 0
        _AnisotropyMap ("Anisotropy", 2D) = "white" {}
        _OcclusionStrength ("Strength", Range(0, 1)) = 1
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        [Enum(UV0,0,UV1,1)] _OcclusionUVSet ("UV Set for Occlusion", Float) = 0
        _TransmissionColor ("Transmission Color", Color) = (0,0,0,1)
        _TransmissionMap ("Transmission Map", 2D) = "white" {}
        [Enum(Thin,0,Thick,1)] _TransmissionMode ("Transmission Mode", Float) = 1
        _SubsurfaceScale ("Subsurface Scale", Range(0, 1)) = 1
        _SubsurfaceMaskMap ("Subsurface Mask", 2D) = "white" {}
        _TransmissionScale ("Transmission Scale", Range(0, 1)) = 1
        _TransmissionMaskMap ("Transmission Mask", 2D) = "white" {}
        _EmissionColor ("Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission", 2D) = "white" {}
        [Toggle] _EmissionFresnel ("Emission Fresnel", Float) = 0
        _EmissionFresnelPower ("Power", Range(0, 16)) = 1
        [MaterialToggle] _EmissionFresnelInvert ("Invert", Float) = 0
        [Enum(UV0,0,UV1,1)] _EmissionUVSec ("UV Set for Emission", Float) = 0
        _EnvReflOcclusionStrength ("Env. Refl. Occlusion Strength", Range(0, 1)) = 0
        _EnvReflHorizonFade ("Env. Refl. Horizon Fade", Range(0, 2)) = 0
        [Enum(Off,0,Albedo Color,1,AO,2)] _ApplyVertexColor ("Apply Vertex Color", Float) = 0
        [Enum(Off,0,Albedo Alpha,1,AO,2)] _ApplyVertexAlpha ("Apply Vertex Alpha", Float) = 0
        _ApplyVertexColorStrength ("Strength", Range(0, 1)) = 1
        _ApplyVertexAlphaStrength ("Strength", Range(0, 1)) = 1
        [ToggleUI] _OffsetEmissionOnly ("Offset Emission Only", Float) = 0
        _DecalLayerMask ("Decal Layer Mask", Float) = 1
        [Enum(None,0,Plane,1,Sphere,2,Thin,3)] _Refraction ("Refraction Model", Float) = 0
        _Ior ("Index Of Refraction", Range(1, 2.5)) = 1.5
        _Thickness ("Thickness", Float) = 1
        _TransmittanceColor ("Transmittance Color", Color) = (1,1,1,1)
        _ATDistance ("Absorption Distance", Float) = 1
        [Toggle] _DetailLayer ("Enabled", Float) = 0
        [Enum(MULX2,0,LERP,1)] _DetailBlendType ("Blend Type", Float) = 0
        _DetailBlendFlags ("Blend Flags", Float) = -1
        _DetailMask ("Detail Mask", 2D) = "white" {}
        _DetailColor ("Color", Color) = (1,1,1,1)
        _DetailAlbedoMap ("Albedo", 2D) = "grey" {}
        _DetailAlbedoMapScroll ("Scroll", Vector) = (0,0,0,0)
        _DetailMetallicGlossMap ("Metallic", 2D) = "white" {}
        _DetailSpecGlossMap ("Specular", 2D) = "white" {}
        _DetailOverlayMetallic ("Overlay Metallic", Range(0, 1)) = 0
        _DetailOverlaySpecular ("Overlay Specular", Range(0, 1)) = 0
        _DetailOverlaySmoothness ("Overlay Smoothness", Range(0, 1)) = 0
        _DetailNormalMapScale ("Scale", Float) = 1
        _DetailNormalMap ("Normal Map", 2D) = "bump" {}
        _DetailOcclusionStrength ("Strength", Range(0, 1)) = 1
        _DetailOcclusionMap ("Occlusion", 2D) = "white" {}
        [Enum(UV0,0,UV1,1)] _UVSec ("Layer UV Set", Float) = 0
        [Toggle] _ParticleLayer ("Enabled", Float) = 0
        _ParticleLayer_MapTiling ("Map Tiling", Float) = 0.02
        _ParticleLayer_Thickness ("Thickness", Range(0.001, 1)) = 0.2
        _ParticleLayer_BlendFactor ("Blend Factor", Range(0, 16)) = 0.2
        _ParticleLayer_BlendFalloff ("Blend Falloff", Range(1, 128)) = 4.5
        _ParticleLayer_WorldDirection ("World Direction", Vector) = (0,1,0,0)
        _ParticleLayer_AlbedoColor ("Color", Color) = (1,1,1,1)
        _ParticleLayer_AlbedoMap ("Albedo Map", 2D) = "white" {}
        _ParticleLayer_Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _ParticleLayer_Metallic ("Metallic", Range(0, 1)) = 0
        _ParticleLayer_MetallicGlossMap ("Metallic", 2D) = "white" {}
        _ParticleLayer_SpecColor ("Specular Color", Color) = (0.2,0.2,0.2,1)
        _ParticleLayer_SpecGlossMap ("Specular Map", 2D) = "white" {}
        _ParticleLayer_NormalScale ("Scale", Float) = 1
        _ParticleLayer_NormalMap ("Normal Map", 2D) = "bump" {}
        [Toggle] _BiomeLayer ("Enabled", Float) = 0
        _BiomeLayer_TintMask ("Mask", 2D) = "white" {}
        [MaterialEnum(Dirt,0,Sand,2,Rock,3,Grass,4,Forest,5,Stones,6,Gravel,7)] _BiomeLayer_TintSplatIndex ("Tint Splat Index", Float) = 0
        [Toggle] _WetnessLayer ("Enabled", Float) = 0
        _WetnessLayer_Mask ("Mask", 2D) = "white" {}
        _WetnessLayer_Wetness ("Wetness", Range(0, 1)) = 1
        _WetnessLayer_WetAlbedoScale ("Wet Albedo Scale", Range(0, 1)) = 0.7
        _WetnessLayer_WetSmoothness ("Wet Smoothness", Range(0, 1)) = 0.7
        [Header(Shore Wetness Layer)] [Toggle] _ShoreWetnessLayer ("Enabled", Float) = 0
        _ShoreWetnessLayer_Range ("Range", Float) = 5
        [PowerSlider(4.0)] _ShoreWetnessLayer_BlendFactor ("Blend Factor", Range(0, 128)) = 2
        [PowerSlider(4.0)] _ShoreWetnessLayer_BlendFalloff ("Blend Falloff", Range(0.001, 128)) = 2
        _ShoreWetnessLayer_WetAlbedoScale ("Wet Albedo Scale", Range(0, 1)) = 0.8
        _ShoreWetnessLayer_WetSmoothness ("Wet Smoothness", Range(0, 1)) = 0.85
        [Toggle] _Wind ("Enabled", Float) = 0
        _WindFrequency ("Frequency", Range(0.001, 10)) = 0.33
        _WindPhase ("Phase", Range(0.001, 10)) = 1
        _WindAmplitude1 ("Amplitude 1", Range(0.001, 10)) = 2
        _WindAmplitude2 ("Amplitude 2", Range(0.001, 10)) = 5
        _WindNoiseScale ("Noise Scale", Range(0.01, 2)) = 1
        _WindNormalOffset ("Normal Offset", Range(-0.1, 0.1)) = 0
        _ShadowBiasScale ("Shadow Bias Scale", Float) = 1
        _SubsurfaceProfile ("Subsurface Profile", Float) = 0
        _Mode ("__mode", Float) = 0
        _SrcBlend ("__src", Float) = 1
        _DstBlend ("__dst", Float) = 0
        _ZWrite ("__zw", Float) = 1
        [MaterialEnum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "PerformanceChecks" = "False" "RenderType" = "Opaque" "SurfaceType" = "Standard" }
        LOD 300

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" "PerformanceChecks" = "False" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" "SurfaceType" = "Standard" }
            Blend Zero Zero, Zero Zero
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            // Declare textures and samplers
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _BumpMap;
            float4 _BumpMap_ST;
            sampler2D _EmissionMap;

            // Declare uniform variables
            fixed4 _Color;
            half _Glossiness;
            half _Metallic;
            float _BumpScale;
            fixed4 _EmissionColor;
            float _TransmissionScale;
            fixed4 _TransmissionColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float3 tangent : TEXCOORD3;
                float3 binormal : TEXCOORD4;
                UNITY_FOG_COORDS(5)
                UNITY_SHADOW_COORDS(6)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                o.uv1 = TRANSFORM_TEX(v.uv1, _BumpMap);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.binormal = cross(o.normal, o.tangent) * v.tangent.w;

                UNITY_TRANSFER_FOG(o, o.pos);
                TRANSFER_SHADOW(o);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                fixed4 col = tex2D(_MainTex, i.uv0) * _Color;
                fixed3 normal = UnpackNormal(tex2D(_BumpMap, i.uv1)) * _BumpScale;
                float3 worldNormal = normalize(i.normal);
                float3 worldTangent = normalize(i.tangent);
                float3 worldBinormal = normalize(i.binormal);
                float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.pos));

                // Convert tangent space normal to world space
                normal = normalize(worldNormal * normal.z + worldTangent * normal.x + worldBinormal * normal.y);

                // Lighting
                UNITY_LIGHT_ATTENUATION(attenuation, i, i.pos.xyz);
                fixed3 lightDir = normalize(UnityWorldSpaceLightDir(i.pos));
                fixed3 diffuse = col.rgb * max(0, dot(normal, lightDir)) * attenuation;

                // Specular - Manual calculation
                float3 halfDir = normalize(lightDir + worldViewDir);
                float spec = pow(max(0, dot(normal, halfDir)), _Glossiness * 128); 
                fixed3 specular = _SpecColor.rgb * spec * attenuation;

                // Emission
                fixed3 emission = _EmissionColor.rgb * tex2D(_EmissionMap, i.uv0).rgb;

                // Transmission (simplified)
                fixed transmission = _TransmissionScale * dot(normal, lightDir) * _TransmissionColor;

                fixed4 finalColor = fixed4(diffuse + specular + emission + transmission, col.a);
                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return finalColor;
            }
            ENDCG
        }
    }

	SubShader
	{
		Tags { "PerformanceChecks" = "False" "RenderType" = "Opaque" "SurfaceType" = "Standard" }
		LOD 150

		Pass
		{
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" "PerformanceChecks" = "False" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" "SurfaceType" = "Standard" }
			Blend Zero Zero, Zero Zero
			ZWrite Off
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv0 : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv0 : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				UNITY_SHADOW_COORDS(2)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);

				UNITY_TRANSFER_FOG(o, o.pos);
				TRANSFER_SHADOW(o);

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				fixed4 col = tex2D(_MainTex, i.uv0) * _Color;
				UNITY_LIGHT_ATTENUATION(attenuation, i, i.pos.xyz);

				// A very basic lighting calculation, just applying albedo with attenuation
				fixed4 finalColor = col * attenuation;
				
				UNITY_APPLY_FOG(i.fogCoord, finalColor);
				return finalColor;
			}
			ENDCG
		}
	}

    FallBack "Standard"
}