Shader "Custom/CoreFoliage" {
    Properties {
        // Textures
        _BaseColorMap ("Base Color Map", 2D) = "white" {}
        _TranslucencyMap ("Translucency Map", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _TintMask ("Tint Mask", 2D) = "white" {}
        
        // Colors
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _TintBase1 ("Tint Base 1", Color) = (1,1,1,1)
        _TintBase2 ("Tint Base 2", Color) = (1,1,1,1)
        _TintColor1 ("Tint Color 1", Color) = (1,1,1,1)
        _TintColor2 ("Tint Color 2", Color) = (1,1,1,1)
        _Translucency ("Translucency Color", Color) = (1,1,1,1)
        
        // Tinting
        _TintEnabled ("Enable Tinting", Float) = 0
        _TintBiome ("Biome Tint Blend", Range(0,1)) = 0
        _TintMaskSource ("Tint Mask Source", Float) = 0
        
        // Wind
        _WindEnabled ("Enable Wind", Float) = 0
        _WavesEnabled ("Enable Waves", Float) = 0
        _WavesScale ("Waves Scale", Float) = 1
        _WindBranchAmplitude ("Branch Amplitude", Float) = 0.1
        _WindBranchFrequency ("Branch Frequency", Float) = 1
        _WindFlutterAmplitude ("Flutter Amplitude", Float) = 0.05
        _WindFlutterFrequency ("Flutter Frequency", Float) = 2
        _WindHeightBendAmplitude ("Height Bend Amplitude", Float) = 0.1
        _WindHeightBendDirAdherence ("Height Bend Dir Adherence", Float) = 0.5
        _WindHeightBendExponent ("Height Bend Exponent", Float) = 1
        _WindHeightBendFrequency ("Height Bend Frequency", Float) = 0.5
        
        // Distance Fade
        _DistanceFadeEnabled ("Enable Distance Fade", Float) = 0
        _DistanceFadeStart ("Fade Start Distance", Float) = 10
        _DistanceFadeLength ("Fade Length", Float) = 5
        _DistanceFadeToNormal ("Fade to Normal", Vector) = (0,0,0,0)
        _DistanceFadeToSizeScale ("Fade to Size Scale", Float) = 1
        _DistanceFadeToSmoothnessScale ("Fade to Smoothness Scale", Float) = 1
        
        // Debug
        _DebugEnabled ("Enable Debug", Float) = 0
        _DebugView ("Debug View Mode", Float) = 0
        
        // Advanced
        _DecalLayerMask ("Decal Layer Mask", Float) = 0
        _DisplacementOverride ("Displacement Override", Float) = 0
        _DisplacementStrength ("Displacement Strength", Float) = 0
        _EdgeMaskContrast ("Edge Mask Contrast", Float) = 1
        _EdgeMaskMin ("Edge Mask Min", Float) = 0
        _FaceCulling ("Face Culling", Float) = 0
        _NormalScale ("Normal Scale", Float) = 1
        _NormalScaleFromThicknessEnabled ("Normal Scale from Thickness", Float) = 0
        _OpacityMaskClip ("Opacity Mask Clip", Range(0,1)) = 0.3
        _Roughness ("Roughness", Float) = 0.5
        _ShadowAlphaLODBias ("Shadow Alpha LOD Bias", Float) = 0
        _ShadowBias ("Shadow Bias", Float) = 0
        _ShadowIntensity ("Shadow Intensity", Float) = 1
        _Specular ("Specular", Float) = 0
        _TwoSided ("Two Sided", Float) = 0
        _VertexOcclusionStrength ("Vertex Occlusion Strength", Float) = 0
        _AmbientBoost ("Ambient Boost", Range(0,5)) = 2.0
        _MinBrightness ("Minimum Brightness", Range(0,1)) = 0.2
    }
    SubShader {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull [_FaceCulling]
        Pass {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"

            // Properties 
            sampler2D _BaseColorMap;
            sampler2D _TranslucencyMap;
            sampler2D _NormalMap;
            sampler2D _TintMask;
            fixed4 _BaseColor;
            float _DebugEnabled;
            float _DebugView;
            float _DecalLayerMask;
            float _DisplacementOverride;
            float _DisplacementStrength;
            float _DistanceFadeEnabled;
            float _DistanceFadeLength;
            float _DistanceFadeStart;
            float4 _DistanceFadeToNormal;
            float _DistanceFadeToSizeScale;
            float _DistanceFadeToSmoothnessScale;
            float _EdgeMaskContrast;
            float _EdgeMaskMin;
            float _FaceCulling;
            float _NormalScale;
            float _NormalScaleFromThicknessEnabled;
            float _OpacityMaskClip;
            float _Roughness;
            float _ShadowAlphaLODBias;
            float _ShadowBias;
            float _ShadowIntensity;
            float _Specular;
            fixed4 _TintBase1;
            fixed4 _TintBase2;
            float _TintBiome;
            fixed4 _TintColor1;
            fixed4 _TintColor2;
            float _TintEnabled;
            float _TintMaskSource;
            fixed4 _Translucency;
            float _TwoSided;
            float _VertexOcclusionStrength;
            float _WavesEnabled;
            float _WavesScale;
            float _WindBranchAmplitude;
            float _WindBranchFrequency;
            float _WindEnabled;
            float _WindFlutterAmplitude;
            float _WindFlutterFrequency;
            float _WindHeightBendAmplitude;
            float _WindHeightBendDirAdherence;
            float _WindHeightBendExponent;
            float _WindHeightBendFrequency;
            float _AmbientBoost;
            float _MinBrightness;

            // Vertex input structure
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 color : COLOR; // Vertex color for additional control (e.g., wind mask)
            };

            // Vertex-to-fragment structure
            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float4 tangent : TEXCOORD3;
                float4 vertexColor : TEXCOORD4;
                float distance : TEXCOORD5;
                SHADOW_COORDS(6)
            };

            // Vertex shader
            v2f vert(appdata v) {
                v2f o;
                
                // Wind animation (if enabled)
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float windEffect = 0.0;
                if (_WindEnabled > 0.5) {
                    // Branch bending based on height
                    float heightFactor = pow(v.vertex.y * _WindHeightBendExponent, _WindHeightBendExponent);
                    float branchPhase = sin(_Time.y * _WindBranchFrequency + worldPos.x + worldPos.z) * _WindBranchAmplitude;
                    float heightBend = sin(_Time.y * _WindHeightBendFrequency) * _WindHeightBendAmplitude * heightFactor;
                    
                    // Flutter effect
                    float flutterPhase = sin(_Time.y * _WindFlutterFrequency + worldPos.x * 0.5 + worldPos.z * 0.5) * _WindFlutterAmplitude;
                    
                    // Combine wind effects
                    windEffect = (branchPhase + heightBend + flutterPhase) * v.color.r; // Use vertex color red channel as wind mask
                    v.vertex.xyz += windEffect * _WavesScale * v.normal;
                }
                
                // Transform vertex to clip space
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = worldPos;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
                o.vertexColor = v.color;
                o.distance = length(_WorldSpaceCameraPos - o.worldPos);
                TRANSFER_SHADOW(o);
                
                return o;
            }

            // Fragment shader
            fixed4 frag(v2f i) : SV_Target {
                // Sample textures
                fixed4 baseColor = tex2D(_BaseColorMap, i.uv);
                fixed4 translucency = tex2D(_TranslucencyMap, i.uv) * _Translucency;
                float3 normalMap = UnpackNormal(tex2D(_NormalMap, i.uv));
                float tintMask = tex2D(_TintMask, i.uv).r;
                
                // Apply tinting (if enabled)
                if (_TintEnabled > 0.5) {
                    fixed4 tint = lerp(_TintColor1, _TintColor2, tintMask);
                    baseColor.rgb *= lerp(fixed3(1,1,1), tint.rgb, _TintBiome);
                }
                
                // Normal mapping
                float3x3 TBN = float3x3(
                    normalize(i.tangent.xyz),
                    normalize(cross(i.normal, i.tangent.xyz) * i.tangent.w),
                    normalize(i.normal)
                );
                float3 normal = normalize(mul(normalMap * _NormalScale, TBN));
                
                // Distance fading (if enabled)
                float fadeFactor = 1.0;
                if (_DistanceFadeEnabled > 0.5) {
                    fadeFactor = saturate((i.distance - _DistanceFadeStart) / _DistanceFadeLength);
                    baseColor.a *= (1.0 - fadeFactor);
                }
                
                // Alpha test
                clip(baseColor.a - _OpacityMaskClip);
                
                // Lighting
                float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float NdotL = max(0, dot(normal, lightDir));
                
                // Shadow attenuation with clamped low range
                float shadow = SHADOW_ATTENUATION(i);
                shadow = lerp(0.6, 1.0, shadow); // Minimum 60% light in shadows
                
                // Diffuse lighting with shadows
                fixed3 diffuse = baseColor.rgb * NdotL * shadow * _LightColor0.rgb;
                
                // Ambient lighting (spherical harmonics with boost + constant term)
                fixed3 ambient = ShadeSH9(float4(normal, 1.0)) * baseColor.rgb * _AmbientBoost;
                ambient += baseColor.rgb * _MinBrightness; // Constant minimum brightness
                
                // Translucency (modulated by shadows, reduced intensity)
                float translucencyEffect = translucency.r * max(0, dot(-lightDir, viewDir)) * shadow * 0.3;
                diffuse += translucencyEffect * _Translucency.rgb;
                
                // Combine lighting
                fixed3 finalColor = diffuse + ambient;
                
                // Debug view (if enabled)
                if (_DebugEnabled > 0.5) {
                    if (_DebugView == 1.0) return fixed4(normal * 0.5 + 0.5, 1.0);
                    if (_DebugView == 2.0) return fixed4(tintMask, tintMask, tintMask, 1.0);
                    if (_DebugView == 3.0) return translucency;
                    if (_DebugView == 4.0) return fixed4(shadow, shadow, shadow, 1.0); // Visualize shadow attenuation
                }
                
                return fixed4(finalColor, baseColor.a);
            }
            ENDCG
        }
    }
    CustomEditor "CoreFoliageGUI"
}