Shader "Custom/Rust/Ocean"
{
    Properties
    {
        // Water Color and Appearance
        _Color("Water Color", Color) = (0.13, 0.57, 0.64, 0.8) // Briny bluish-greenish
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.9
        _Opacity("Opacity", Range(0.0, 1.0)) = 0.8

        // Wave Animation Properties
        _WaveSpeed("Wave Speed", Float) = 1.0
        _WaveScale("Wave Scale", Float) = 0.1
        _WaveStrength("Wave Strength", Float) = 0.05
        _NoiseScale("Noise Scale", Float) = 10.0

        _OceanYLevel("Ocean Y Level", Float) = 500.0 // Flat y level for ocean
        _TopologyBit("Ocean Topology Bit", Int) = 8 // Topology bit for ocean

        // Rendering Properties
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(Opaque,0,Transparent,1)] _Mode("Rendering Mode", Float) = 0 // Changed to Opaque
        [HideInInspector] _SrcBlend("Source Blend", Float) = 1.0 // One
        [HideInInspector] _DstBlend("Destination Blend", Float) = 0.0 // Zero
        [HideInInspector] _ZWrite("ZWrite", Float) = 1.0 // Enable ZWrite
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "TerrainCompatible"="True" } // Changed RenderType to Opaque
        LOD 200
        ZWrite On
        ZTest LEqual

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard vertex:vert
        #pragma multi_compile _ ALPHA_TEST
        #define TERRAIN_SPLATMAP_COMMON_CGINC_INCLUDED
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"
        #include "TerrainSplatmapCommon.cginc"

        // Texture Declarations
        UNITY_DECLARE_TEX2D(Terrain_Topologies);

        // Properties
        float4 _Color;
        float _Metallic;
        float _Smoothness;
        float _Opacity;
        float _WaveSpeed;
        float _WaveScale;
        float _WaveStrength;
        float _NoiseScale;
        float _OceanYLevel;
        int _TopologyBit;
        float _Cutoff;
        float _SrcBlend;
        float _DstBlend;
        float _ZWrite;

        float2 random2(float2 p)
        {
            return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
        }

        float perlinNoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            float2 u = f * f * (3.0 - 2.0 * f);

            float2 a = random2(i + float2(0.0, 0.0));
            float2 b = random2(i + float2(1.0, 0.0));
            float2 c = random2(i + float2(0.0, 1.0));
            float2 d = random2(i + float2(1.0, 1.0));

            float va = dot(a, f - float2(0.0, 0.0));
            float vb = dot(b, f - float2(1.0, 0.0));
            float vc = dot(c, f - float2(0.0, 1.0));
            float vd = dot(d, f - float2(1.0, 1.0));

            return lerp(lerp(va, vb, u.x), lerp(vc, vd, u.x), u.y);
        }

        struct Input
        {
            float2 tc_Control0;
            float3 worldPos;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.tc_Control0 = v.texcoord1.xy;
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            
            float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
            worldPos.y = _OceanYLevel;
            v.vertex = mul(unity_WorldToObject, worldPos);

            // Compute tangent for normal mapping
            v.tangent = float4(cross(v.normal, float3(0, 0, 1)), 1.0);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Sample current pixel's topology
            float4 topologyColor = UNITY_SAMPLE_TEX2D_SAMPLER(Terrain_Topologies, Terrain_Topologies, IN.tc_Control0);
            int r = floor(topologyColor.r * 255.0);
            int g = floor(topologyColor.g * 255.0);
            int b = floor(topologyColor.b * 255.0);
            int a = floor(topologyColor.a * 255.0);
            int topologyBitmask = (a << 24) | (b << 16) | (g << 8) | r;

            // Check for ocean topology
            bool isOcean = (topologyBitmask & (1 << _TopologyBit)) != 0;

            if (!isOcean)
            {
                o.Alpha = 0.0;
                clip(o.Alpha - _Cutoff);
                return;
            }

            float3 albedo = _Color.rgb;
            o.Albedo = albedo;

            // Wave animation using Perlin noise
            float2 uv = IN.worldPos.xz * _NoiseScale;
            float time = _Time.y * _WaveSpeed;
            
            float noise = perlinNoise(uv + time);
            float3 normal = float3(0, 1, 0);
            normal.x += (perlinNoise(uv + float2(0.1, 0.0) + time) - noise) * _WaveScale;
            normal.z += (perlinNoise(uv + float2(0.0, 0.1) + time) - noise) * _WaveScale;
            normal = normalize(normal);
            
            o.Normal = normal;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = 1.0; // Set to 1.0 for opaque rendering
        }
        ENDCG
    }

    Fallback "Diffuse"
    CustomEditor "RustOceanShaderGUI"
}