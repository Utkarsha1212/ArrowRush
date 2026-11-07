Shader "Arrow Rush/Outlined Cube"
{
    Properties
    {
        _MainTex ("Surface Texture", 2D) = "white" {}
        _Color ("Cube Color", Color) = (1, 1, 1, 1)
        _BevelWidth ("Face Bevel Width", Range(0.01, 0.2)) = 0.07
        _EdgeShade ("Edge Shade", Range(0, 0.5)) = 0.2
        _OutlineColor ("Outline Color", Color) = (0.12, 0.055, 0.02, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.08)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
            };

            v2f vert(appdata input)
            {
                v2f output;
                float4 expandedVertex = input.vertex;
                expandedVertex.xyz += normalize(input.normal) * _OutlineWidth;
                output.position = UnityObjectToClipPos(expandedVertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        fixed4 _Color;
        sampler2D _MainTex;
        float _BevelWidth;
        float _EdgeShade;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            float edgeDistance = min(min(input.uv_MainTex.x, 1.0 - input.uv_MainTex.x),
                                     min(input.uv_MainTex.y, 1.0 - input.uv_MainTex.y));
            float faceInterior = smoothstep(0.0, _BevelWidth, edgeDistance);
            float textureShade = 1.0 - _EdgeShade * (1.0 - faceInterior);
            fixed4 surfaceTexture = tex2D(_MainTex, input.uv_MainTex);

            output.Albedo = surfaceTexture.rgb * _Color.rgb * textureShade;
            output.Alpha = surfaceTexture.a * _Color.a;
            output.Smoothness = 0.12;
            output.Metallic = 0;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
