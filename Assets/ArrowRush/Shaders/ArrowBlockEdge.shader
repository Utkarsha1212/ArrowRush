Shader "ArrowRush/Arrow Block Edge Detail"
{
    Properties
    {
        _Color ("Cube Color", Color) = (1, 1, 1, 1)
        _EdgeColor ("Edge Color", Color) = (0.12, 0.28, 0.45, 1)
        _EdgeWidth ("Edge Width", Range(0.005, 0.12)) = 0.035
        _EdgePatternScale ("Edge Pattern Scale", Range(4, 48)) = 24
        _EdgePatternStrength ("Edge Pattern Strength", Range(0, 1)) = 0.35
        _Glossiness ("Smoothness", Range(0, 1)) = 0.35
        _Metallic ("Metallic", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.0

        fixed4 _Color;
        fixed4 _EdgeColor;
        half _EdgeWidth;
        half _EdgePatternScale;
        half _EdgePatternStrength;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float3 cubePosition;
        };

        void vert(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            output.cubePosition = vertex.vertex.xyz;
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            float3 coord = abs(input.cubePosition) * 2.0;
            float largest = max(coord.x, max(coord.y, coord.z));
            float smallest = min(coord.x, min(coord.y, coord.z));
            float secondLargest = coord.x + coord.y + coord.z - largest - smallest;
            half edgeBand = smoothstep(1.0 - _EdgeWidth, 1.0, secondLargest);

            half stripe = step(0.5, frac((input.cubePosition.x + input.cubePosition.y + input.cubePosition.z) * _EdgePatternScale));
            half edgePattern = lerp(1.0 - _EdgePatternStrength, 1.0, stripe);
            half edgeAmount = edgeBand * edgePattern;

            output.Albedo = lerp(_Color.rgb, _EdgeColor.rgb, edgeAmount);
            output.Metallic = _Metallic;
            output.Smoothness = _Glossiness;
            output.Alpha = _Color.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
