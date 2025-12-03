Shader "Hidden/UVLayout"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                // Transform UV [0,1] to Clip Space [-1,1]
                // UV (0,0) -> (-1, -1)
                // UV (1,1) -> (1, 1)
                o.vertex = float4(v.uv.x * 2.0 - 1.0, v.uv.y * 2.0 - 1.0, 0.0, 1.0);
                
                // Fix for Direct3D (flip Y) if needed, but usually UV space is consistent
                #if UNITY_UV_STARTS_AT_TOP
                o.vertex.y *= -1;
                #endif
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1, 1, 1, 1); // White
            }
            ENDCG
        }
    }
}
