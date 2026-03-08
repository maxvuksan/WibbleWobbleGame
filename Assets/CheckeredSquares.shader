Shader "Unlit/CheckeredScroll"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.1, 0.1, 0.1, 1)
        _ColorB ("Color B", Color) = (0.9, 0.9, 0.9, 1)

        _SquareCount ("Squares Per Axis", Float) = 8

        _ScrollSpeed ("Scroll Speed (XY)", Vector) = (0.1, 0.0, 0, 0)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "IgnoreProjector"="True"
        }

        LOD 100
        ZWrite On
        Cull Off
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _ColorA;
            float4 _ColorB;
            float _SquareCount;
            float4 _ScrollSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Scroll UVs over time
                float2 uv = i.uv + _ScrollSpeed.xy * _Time.y;

                // Calculate aspect ratio (width / height)
                float aspect = _ScreenParams.x / _ScreenParams.y;

                // Correct UV so squares stay square
                uv.x *= aspect;

                // Scale by square count
                float2 grid = floor(uv * _SquareCount);

                // Checker pattern
                float checker = fmod(grid.x + grid.y, 2.0);

                return lerp(_ColorA, _ColorB, checker);
            }
            ENDCG
        }
    }
}
