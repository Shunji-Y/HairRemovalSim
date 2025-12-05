Shader "Custom/HairShader"
{
    Properties
    {
        // Body Settings
        _BaseMap("Body Texture", 2D) = "white" {}
        [HDR] _BodyColor("Body Color (HDR)", Color) = (1, 1, 1, 1)

        // Hair Settings
        _HairColor("Hair Color", Color) = (0.1, 0.05, 0.0, 1)
        _HairSpecular("Hair Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        _HairSmoothness("Hair Smoothness", Range(1.0, 500.0)) = 50.0
        _HairLength("Hair Length", Float) = 0.05
        _HairWidth("Hair Width", Float) = 0.005
        _HairDensity("Hair Density (Per Unit Area)", Float) = 1000.0
        _HairRandom("Hair Randomization", Range(0, 1)) = 0.5
        _HairCurve("Hair Curve", Range(0, 1)) = 0.2
        _HairFrizz("Hair Frizz", Range(0, 1)) = 0.1
        _Gravity("Gravity Strength", Float) = 0.02
        _MaskMap("Mask Map", 2D) = "white" {}
        
        // UV-Based Body Part System
        _HairGrowthMask("Hair Growth Mask (R=density)", 2D) = "white" {}
        _BodyPartMask("Body Part Mask (R=partID)", 2D) = "black" {}
        
        // Requested Parts (Set from script) - Default to -1 (no parts selected)
        _RequestedPartMasks("Requested Part Masks 1-4", Vector) = (-1,-1,-1,-1)
        _RequestedPartMasks2("Requested Part Masks 5-8", Vector) = (-1,-1,-1,-1)
        _RequestedPartMasks3("Requested Part Masks 9-12", Vector) = (-1,-1,-1,-1)
        _RequestedPartMasks4("Requested Part Masks 13-16", Vector) = (-1,-1,-1,-1)
        
        // Highlight Settings
        [HDR] _HighlightColor("Highlight Color", Color) = (1, 0.745, 0.29, 1)
        _HighlightIntensity("Highlight Emission", Float) = 2.0
        
        // Decal Settings (UV-based for tape preview)
        _DecalUVCenter("Decal UV Center", Vector) = (0.5, 0.5, 0, 0)
        _DecalUVSize("Decal UV Size", Vector) = (0.1, 0.1, 0, 0)
        _DecalUVAngle("Decal UV Angle", Float) = 0
        _DecalColor("Decal Color", Color) = (2, 2, 0.5, 1)
        _DecalEnabled("Decal Enabled", Float) = 0
        

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "BodyAndHair"
            Tags { "LightMode"="UniversalForward" }
            
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD3;
                float2 uv : TEXCOORD0;
                float isHair : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BodyColor;
                float4 _BaseMap_ST;
                float4 _HairColor;
                float4 _HairSpecular;
                float _HairSmoothness;
                float _HairLength;
                float _HairWidth;
                float _HairDensity;
                float _HairRandom;
                float _HairCurve;
                float _HairFrizz;
                float _Gravity;
                float4 _MaskMap_ST;
                
                // UV-Based Body Part System
                float4 _RequestedPartMasks;
                float4 _RequestedPartMasks2;
                float4 _RequestedPartMasks3;
                float4 _HighlightColor;
                float _HighlightIntensity;
                
                // Decal (UV-based)
                float4 _DecalUVCenter;
                float4 _DecalUVSize;
                float _DecalUVAngle;
                float4 _DecalColor;
                float _DecalEnabled;
                

                float4 _RequestedPartMasks4;

            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);
            TEXTURE2D(_HairGrowthMask); SAMPLER(sampler_HairGrowthMask);
            TEXTURE2D(_BodyPartMask); SAMPLER(sampler_BodyPartMask);

            float rand(float3 co)
            {
                return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(0,0,0,0));

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.isHair = 0.0;
                return output;
            }

            [maxvertexcount(75)] 
            void geom(triangle Varyings input[3], uint pid : SV_PrimitiveID, inout TriangleStream<Varyings> triStream)
            {
                // 1. Emit Body
                for(int k=0; k<3; k++)
                {
                    Varyings v = input[k];
                    v.isHair = 0.0;
                    // v.positionOS is already copied from input[k]
                    triStream.Append(v);
                }
                triStream.RestartStrip();

                // 2. Calculate Area & Count
                float3 p0 = input[0].positionWS;
                float3 p1 = input[1].positionWS;
                float3 p2 = input[2].positionWS;
                float area = length(cross(p1 - p0, p2 - p0)) * 0.5;

                float expectedCount = area * _HairDensity;
                int baseCount = (int)expectedCount;
                float remainder = frac(expectedCount);
                float rCount = rand(float3(pid, area, _HairDensity));
                if (rCount < remainder) baseCount++;

                int maxHairs = 9; 
                int hairCount = min(baseCount, maxHairs);

                float widthMultiplier = 1.0;
                if (baseCount > maxHairs)
                {
                    widthMultiplier = sqrt((float)baseCount / (float)maxHairs);
                }

                int segments = 3;

                for(int i=0; i<hairCount; i++)
                {
                    float3 seed = float3((float)pid, (float)i, (float)pid + (float)i);
                    float r1 = rand(seed);
                    float r2 = rand(seed + 1.0);
                    if (r1 + r2 > 1.0) { r1 = 1.0 - r1; r2 = 1.0 - r2; }
                    float r3 = 1.0 - r1 - r2;

                    float3 worldPos = input[0].positionWS * r3 + input[1].positionWS * r1 + input[2].positionWS * r2;
                    float3 worldNormal = normalize(input[0].normalWS * r3 + input[1].normalWS * r1 + input[2].normalWS * r2);
                    float2 uv = input[0].uv * r3 + input[1].uv * r1 + input[2].uv * r2;

                    float rLen = rand(seed + 2.0);
                    float rRot = rand(seed + 3.0);
                    float rCurveX = rand(seed + 4.0) - 0.5;
                    float rCurveZ = rand(seed + 5.0) - 0.5;

                    float currentLength = _HairLength * (1.0 - (_HairRandom * 0.5 * rLen)); 
                    
                    float3 baseTangent = normalize(cross(worldNormal, float3(0,1,0) + float3(0.01, 0.01, 0.01)));
                    float3 baseBitangent = normalize(cross(worldNormal, baseTangent));
                    float angle = rRot * 3.14159 * 2.0;
                    float cs = cos(angle);
                    float sn = sin(angle);
                    float3 t1 = baseTangent * cs + baseBitangent * sn;

                    // Calculate flow direction based on UV gradient
                    // Typically UV.v goes from root (0) to tip (1) on limbs
                    // We want hair to flow in the direction of increasing V
                    // Estimate gradient: hair should flow "down" the UV V axis
                    float3 flowDir = normalize(baseBitangent); // Approximation: bitangent often aligns with V direction
                    
                    float3 currentPos = worldPos;
                    float3 currentDir = worldNormal; 
                    
                    // Bias curve toward limb axis (flow direction) instead of random
                    // Mix some flow direction into the initial growth
                    float3 curveBias = normalize(flowDir * 0.3 + float3(rCurveX * 0.2, 0, rCurveZ * 0.2)) * _HairCurve;

                    for(int s=0; s<=segments; s++)
                    {
                        float t = (float)s / (float)segments; 
                        float w = _HairWidth * widthMultiplier * (1.0 - t); 
                        float3 frizz = (float3(rand(seed + t), rand(seed + t + 0.1), rand(seed + t + 0.2)) - 0.5) * _HairFrizz * 0.1;
                        
                        if (s > 0)
                        {
                            // Remove gravity to keep hair direction relative to surface
                            // Only apply curve and frizz effects
                            currentDir = normalize(currentDir + curveBias + frizz);
                            currentPos += currentDir * (currentLength / segments);
                        }

                        // Calculate Cylindrical Normal
                        // Face normal of the strip
                        float3 faceNormal = normalize(cross(currentDir, t1));
                        
                        Varyings v;
                        v.isHair = 1.0;
                        v.uv = uv;
                        
                        // Left Vertex
                        v.positionWS = currentPos - t1 * w; 
                        v.positionCS = TransformWorldToHClip(v.positionWS); 
                        // Tilt normal left to simulate cylinder
                        v.normalWS = normalize(faceNormal - t1); 
                        triStream.Append(v);

                        // Right Vertex
                        v.positionWS = currentPos + t1 * w; 
                        v.positionCS = TransformWorldToHClip(v.positionWS); 
                        // Tilt normal right
                        v.normalWS = normalize(faceNormal + t1);
                        triStream.Append(v);
                    }
                    triStream.RestartStrip();
                }
            }

            half4 frag(Varyings input) : SV_Target
            {
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 normal = normalize(input.normalWS);
                
                // Diffuse
                float NdotL = max(0, dot(normal, lightDir));
                half3 diffuse = mainLight.color * NdotL + 0.3;

                // === UV-Based Body Part System ===
                // Get body part ID from mask
                float partID = SAMPLE_TEXTURE2D(_BodyPartMask, sampler_BodyPartMask, input.uv).r;
                
                // Check if this part is requested (施術希望部位か判定)
                // Check if this part is requested (施術希望部位か判定)
                // Tolerance must be small enough to distinguish 0.01 steps (1/100)
                // 8-bit texture precision is 1/255 approx 0.0039
                // So 0.002 is a safe tolerance if values are exact, but with compression/8bit, maybe 0.005 is safer?
                // Let's try 0.005 to be safe but strict enough to separate 0.01 and 0.02
                float tolerance = 0.005; 
                bool isRequested = 
                    abs(partID - _RequestedPartMasks.x) < tolerance ||
                    abs(partID - _RequestedPartMasks.y) < tolerance ||
                    abs(partID - _RequestedPartMasks.z) < tolerance ||
                    abs(partID - _RequestedPartMasks.w) < tolerance ||
                    abs(partID - _RequestedPartMasks2.x) < tolerance ||
                    abs(partID - _RequestedPartMasks2.y) < tolerance ||
                    abs(partID - _RequestedPartMasks2.z) < tolerance ||
                    abs(partID - _RequestedPartMasks2.w) < tolerance ||
                    abs(partID - _RequestedPartMasks3.x) < tolerance ||
                    abs(partID - _RequestedPartMasks3.y) < tolerance ||
                    abs(partID - _RequestedPartMasks3.z) < tolerance ||
                    abs(partID - _RequestedPartMasks3.w) < tolerance ||
                    abs(partID - _RequestedPartMasks4.x) < tolerance ||
                    abs(partID - _RequestedPartMasks4.y) < tolerance ||
                    abs(partID - _RequestedPartMasks4.z) < tolerance ||
                    abs(partID - _RequestedPartMasks4.w) < tolerance;
                
                // Get hair growth density from mask
                float hairGrowth = SAMPLE_TEXTURE2D(_HairGrowthMask, sampler_HairGrowthMask, input.uv).r;

                if (input.isHair > 0.5)
                {
                    half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                    
                    // Discard if: no hair in mask, or no growth in this area
                    // Note: We removed the isRequested check - hair shows everywhere by default
                    if (mask.r < 0.1 || hairGrowth < 0.1) discard;

                    // Specular (Blinn-Phong)
                    float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                    float3 halfVec = normalize(lightDir + viewDir);
                    float NdotH = max(0, dot(normal, halfVec));
                    float specular = pow(NdotH, _HairSmoothness);
                    half3 specularColor = _HairSpecular.rgb * specular * mainLight.color;

                    return half4((_HairColor.rgb * diffuse) + specularColor, 1);
                }
                else
                {
                    half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                    half3 finalColor = (texColor.rgb * _BodyColor.rgb) * diffuse;
                    
                    // Apply highlight to requested parts (施術希望部位を光らせる)
                    if (isRequested)
                    {
                        finalColor += _HighlightColor.rgb * _HighlightIntensity;
                    }
                    
                    // Decal Overlay (UV-based)
                    if (_DecalEnabled > 0.5)
                    {
                        // Get UV offset from decal center
                        float2 uvOffset = input.uv - _DecalUVCenter.xy;
                        
                        // Apply inverse rotation to check if point is inside rotated rectangle
                        float cosAngle = cos(-_DecalUVAngle);
                        float sinAngle = sin(-_DecalUVAngle);
                        float2 rotatedOffset;
                        rotatedOffset.x = uvOffset.x * cosAngle - uvOffset.y * sinAngle;
                        rotatedOffset.y = uvOffset.x * sinAngle + uvOffset.y * cosAngle;
                        
                        // Check if within rectangular bounds
                        float halfWidth = _DecalUVSize.x * 0.5;
                        float halfHeight = _DecalUVSize.y * 0.5;
                        
                        if (abs(rotatedOffset.x) < halfWidth && abs(rotatedOffset.y) < halfHeight)
                        {
                            // Smooth falloff from edges
                            float edgeDistX = 1.0 - (abs(rotatedOffset.x) / halfWidth);
                            float edgeDistY = 1.0 - (abs(rotatedOffset.y) / halfHeight);
                            float alpha = min(edgeDistX, edgeDistY);
                            alpha = smoothstep(0.0, 1.0, alpha);
                            
                            finalColor = lerp(finalColor, _DecalColor.rgb, alpha * 0.7);
                        }
                    }
                    
                    return half4(finalColor, 1);
                }
            }
            ENDHLSL
        }
    }
}
