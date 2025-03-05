Shader "Custom/TerrainShader"
{
    Properties
    {
        _GrassTex ("Grass Texture", 2D) = "white" {}
        _RockTex ("Rock Texture", 2D) = "white" {}
        _SnowTex ("Snow Texture", 2D) = "white" {}
        _HeightThreshold1 ("Height Threshold 1", Range(0, 400)) = 0.3
        _HeightThreshold2 ("Height Threshold 2", Range(0, 400)) = 0.6
        _SlopeThreshold ("Slope Threshold", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _GrassTex;
        sampler2D _RockTex;
        sampler2D _SnowTex;
        half _HeightThreshold1;
        half _HeightThreshold2;
        half _SlopeThreshold;

        struct Input
        {
            float2 uv_GrassTex;
            float2 uv_RockTex;
            float2 uv_SnowTex;
            float3 worldPos;
            float3 worldNormal;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float height = IN.worldPos.y;
            float slope = abs(dot(IN.worldNormal, float3(0, 1, 0)));

            fixed4 grassTex = tex2D(_GrassTex, IN.uv_GrassTex);
            fixed4 rockTex = tex2D(_RockTex, IN.uv_RockTex);
            fixed4 snowTex = tex2D(_SnowTex, IN.uv_SnowTex);

            float heightLerp = saturate((height - _HeightThreshold2) / (_HeightThreshold2 - _HeightThreshold1));
            float slopeLerp = saturate((slope - _SlopeThreshold) / (1 - _SlopeThreshold));

            fixed4 finalColor = grassTex;

            // if (height >= _HeightThreshold2)
            // {
            //     finalColor = lerp(grassTex, snowTex, heightLerp);
            // }

            // else if (height > _HeightThreshold1)
            // {
            //     finalColor = lerp(grassTex, rockTex, heightLerp);
            // }

            // if (slope > _SlopeThreshold && height < _HeightThreshold2)
            // {
            //     finalColor = lerp(finalColor, grassTex, slopeLerp);
            // }

            // Height blending
            if (height >= _HeightThreshold2)
            {
                float heightLerp = saturate((height - _HeightThreshold2) / (400 - _HeightThreshold2));
                finalColor = lerp(rockTex, snowTex, heightLerp);
            }
            else if (height >= _HeightThreshold1)
            {
                float heightLerp = saturate((height - _HeightThreshold1) / (_HeightThreshold2 - _HeightThreshold1));
                finalColor = lerp(grassTex, rockTex, heightLerp);
            }

            // Slope blending
            if (slope > _SlopeThreshold && height < _HeightThreshold2)
            {
                float slopeLerp = saturate((slope - _SlopeThreshold) / (1 - _SlopeThreshold));
                finalColor = lerp(finalColor, grassTex, slopeLerp);
            }

            o.Albedo = finalColor.rgb;
            o.Alpha = finalColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
