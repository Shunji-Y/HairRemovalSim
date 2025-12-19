Shader "Custom/PaintShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BrushPos ("Brush Position (UV)", Vector) = (0,0,0,0)
        _BrushSize ("Brush Size (Circle)", Float) = 0.1
        _BrushRect ("Brush Rect (UV x, y, width, height)", Vector) = (0,0,0.1,0.1)
        _BrushAngle ("Brush Angle (Radians)", Float) = 0
        _BrushMode ("Brush Mode (0=Circle, 1=Rect)", Float) = 0
        _BrushColor ("Brush Color", Color) = (0,0,0,0)
        _BrushIntensity ("Brush Intensity", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One Zero // Overwrite mode - we handle blending inside fragment shader

        Pass
        {
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            // Global properties set by C#
            float4 _BrushPos;
            float _BrushAngle;
            float _BrushSize;
            float4 _BrushRect; // x,y,w,h
            float _BrushMode; // 0=Circle, 1=Rect
            float _BrushIntensity; // The target value (0.2 for shaver, 0.0 for laser)

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);
                
                bool shouldPaint = false;
                
                if (_BrushMode < 0.5)
                {
                    // Circle mode
                    float dist = distance(i.uv, _BrushPos.xy);
                    shouldPaint = (dist < _BrushSize);
                }
                else
                {
                    // Rectangle mode with rotation
                    float2 center = _BrushRect.xy;
                    float2 halfSize = _BrushRect.zw * 0.5;
                    
                    // Rotate UVs around center
                    float2 uv = i.uv - center;
                    float s = sin(-_BrushAngle);
                    float c = cos(-_BrushAngle);
                    float2 rotatedUV = float2(
                        uv.x * c - uv.y * s,
                        uv.x * s + uv.y * c
                    );
                    
                    float2 offset = abs(rotatedUV);
                    shouldPaint = (offset.x < halfSize.x && offset.y < halfSize.y);
                }
                
                if (shouldPaint)
                {
                    // Apply the brush intensity
                    // Use MIN blending: preserve darker values (0.0 laser) over lighter ones (0.2 shaver)
                    // If current pixel is 1.0 (hair), it becomes 0.2 (stubble)
                    // If current pixel is 0.0 (laser), it stays 0.0 (shaver doesn't regrow hair)
                    float currentVal = col.r;
                    float targetVal = _BrushIntensity;
                    float finalVal = min(currentVal, targetVal);
                    
                    return half4(finalVal, finalVal, finalVal, 1.0);
                }
                
                return col;
            }
            ENDCG
        }
    }
}
