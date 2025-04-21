Shader "Custom/CoreFoliageBillboard"
{
    Properties
    {
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

        // Material Properties
        _OpacityMaskClip ("Opacity Mask Clip", Range(0,1)) = 0.3
        _Roughness ("Roughness", Float) = 0.5
        _Specular ("Specular", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off // Billboards are typically two-sided

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            // Properties
            sampler2D _BaseColorMap;
            sampler2D _TranslucencyMap;
            sampler2D _NormalMap;
            sampler2D _TintMask;
            fixed4 _BaseColor;
            float _OpacityMaskClip;
            float _Roughness;
            float _Specular;
            fixed4 _TintBase1;
            fixed4 _TintBase2;
            float _TintBiome;
            fixed4 _TintColor1;
            fixed4 _TintColor2;
            float _TintEnabled;
            float _TintMaskSource;
            fixed4 _Translucency;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Billboarding: Make the quad face the camera
                float3 worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz; // Object's pivot in world space
                float3 viewDir = _WorldSpaceCameraPos - worldPos;
                viewDir.y = 0; // Constrain to XZ plane to keep billboard upright
                viewDir = normalize(viewDir);

                // Create a billboard rotation matrix
                float3 up = float3(0, 1, 0); // World up vector
                float3 right = normalize(cross(up, viewDir));
                up = cross(viewDir, right); // Recalculate up to ensure orthogonality

                // Transform the vertex relative to the pivot
                float3 localPos = v.vertex.xyz;
                float3 billboardPos = localPos.x * right + localPos.y * up; // Ignore local Z for flat billboard
                billboardPos += worldPos; // Move to world position

                // Convert to clip space
                o.vertex = mul(UNITY_MATRIX_VP, float4(billboardPos, 1.0));
                o.uv = v.uv;
                o.worldPos = billboardPos;

                // Transform normal for lighting (billboard normal faces the camera)
                o.normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.normal = normalize(cross(right, up)); // Billboard normal faces the camera

                return o;
            }

			fixed4 frag(v2f i) : SV_Target
			{
				// Sample textures
				fixed4 baseColor = tex2D(_BaseColorMap, i.uv) * _BaseColor;

				// Alpha test
				clip(baseColor.a - _OpacityMaskClip);

				return fixed4(baseColor.rgb, baseColor.a);
			}
            ENDCG
        }
    }
}