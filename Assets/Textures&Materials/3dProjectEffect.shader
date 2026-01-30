Shader "Custom/CustomBlit"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _SourceTexSize ("SourceTexSize", Vector) = (1,1,1,1)

        _ProjectionScale ("Projection Scale (Pixels)", Float) = 1.0
        _InitialProjectionAlpha ("Initial Projection Alpha", Range(0,1)) = 0.5
        _ProjectionDecay ("Projection Alpha Decay", Range(0,1)) = 0.85
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _SourceTexSize; // x = width, y = height
            float _ProjectionScale;
            float _InitialProjectionAlpha;
            float _ProjectionDecay;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                // Default output
                fixed4 result = fixed4(0, 0, 0, 0);

                fixed4 src = tex2D(_MainTex, uv);

                // Original pixel always wins
                if (src.a > 0.99)
                {
                    result = src;
                }
                else
                {
                    // Direction from texture center → current pixel
                    float2 centerUV = float2(0.5, 0.5);
                    float2 toCenter = uv - centerUV;
                    float dist = length(toCenter);

                    if (dist < 1e-5)
                    {
                        return src; // or just output nothing at center
                    }

                    float2 dir = toCenter / dist;

                    float2 pixelOffset = dir * (_ProjectionScale / _SourceTexSize.xy) * dist;

                    float alpha = _InitialProjectionAlpha;
                    bool foundProjection = false;

                    for (int l = 1; l <= 7 && !foundProjection; l++)
                    {
                        float2 projectedUV = uv - (pixelOffset * l);

                        // Bounds check
                        if (projectedUV.x < 0 || projectedUV.y < 0 ||
                            projectedUV.x > 1 || projectedUV.y > 1)
                            continue;

                        fixed4 projected = tex2D(_MainTex, projectedUV);

                        if (projected.a > 0.99)
                        {
                            projected.a = alpha;
                            result = projected;
                            foundProjection = true;
                        }

                        alpha *= _ProjectionDecay;
                    }
                }

                return result;
            }
            ENDCG
        }
    }
}
