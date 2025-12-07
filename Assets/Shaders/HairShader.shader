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
        
        // Hair Growth Mask
        _HairGrowthMask("Hair Growth Mask (R=density)", 2D) = "white" {}
        
        // === Per-Part Binary Mask Textures (14 parts) ===
        // Each texture: white = this part, black = not this part
        
        // Head (1 part)
        _Mask_Beard("Beard Mask", 2D) = "black" {}
        
        // Body (5 parts)
        _Mask_Chest("Chest Mask", 2D) = "black" {}
        _Mask_Abs("Abs Mask", 2D) = "black" {}
        _Mask_Back("Back Mask", 2D) = "black" {}
        _Mask_LeftArmpit("Left Armpit Mask", 2D) = "black" {}
        _Mask_RightArmpit("Right Armpit Mask", 2D) = "black" {}
        
        // Arm (4 parts)
        _Mask_LeftUpperArm("Left Upper Arm Mask", 2D) = "black" {}
        _Mask_LeftLowerArm("Left Lower Arm Mask", 2D) = "black" {}
        _Mask_RightUpperArm("Right Upper Arm Mask", 2D) = "black" {}
        _Mask_RightLowerArm("Right Lower Arm Mask", 2D) = "black" {}
        
        // Leg (4 parts)
        _Mask_LeftThigh("Left Thigh Mask", 2D) = "black" {}
        _Mask_LeftCalf("Left Calf Mask", 2D) = "black" {}
        _Mask_RightThigh("Right Thigh Mask", 2D) = "black" {}
        _Mask_RightCalf("Right Calf Mask", 2D) = "black" {}
        
        // === Request Flags (set from C# script) ===
        // 1.0 = highlight this part, 0.0 = don't highlight
        _Request_Beard("Request Beard", Float) = 0
        _Request_Chest("Request Chest", Float) = 0
        _Request_Abs("Request Abs", Float) = 0
        _Request_Back("Request Back", Float) = 0
        _Request_LeftArmpit("Request Left Armpit", Float) = 0
        _Request_RightArmpit("Request Right Armpit", Float) = 0
        _Request_LeftUpperArm("Request Left Upper Arm", Float) = 0
        _Request_LeftLowerArm("Request Left Lower Arm", Float) = 0
        _Request_RightUpperArm("Request Right Upper Arm", Float) = 0
        _Request_RightLowerArm("Request Right Lower Arm", Float) = 0
        _Request_LeftThigh("Request Left Thigh", Float) = 0
        _Request_LeftCalf("Request Left Calf", Float) = 0
        _Request_RightThigh("Request Right Thigh", Float) = 0
        _Request_RightCalf("Request Right Calf", Float) = 0
        
        // === Completed Flags (set from C# script) ===
        _Completed_Beard("Completed Beard", Float) = 0
        _Completed_Chest("Completed Chest", Float) = 0
        _Completed_Abs("Completed Abs", Float) = 0
        _Completed_Back("Completed Back", Float) = 0
        _Completed_LeftArmpit("Completed Left Armpit", Float) = 0
        _Completed_RightArmpit("Completed Right Armpit", Float) = 0
        _Completed_LeftUpperArm("Completed Left Upper Arm", Float) = 0
        _Completed_LeftLowerArm("Completed Left Lower Arm", Float) = 0
        _Completed_RightUpperArm("Completed Right Upper Arm", Float) = 0
        _Completed_RightLowerArm("Completed Right Lower Arm", Float) = 0
        _Completed_LeftThigh("Completed Left Thigh", Float) = 0
        _Completed_LeftCalf("Completed Left Calf", Float) = 0
        _Completed_RightThigh("Completed Right Thigh", Float) = 0
        _Completed_RightCalf("Completed Right Calf", Float) = 0
        
        // Highlight Settings
        [HDR] _HighlightColor("Highlight Color", Color) = (1, 0.745, 0.29, 1)
        _HighlightIntensity("Highlight Emission", Float) = 2.0
        
        // Decal Settings (UV-based for tape preview)
        _DecalUVCenter("Decal UV Center", Vector) = (0.5, 0.5, 0, 0)
        _DecalUVSize("Decal UV Size", Vector) = (0.1, 0.1, 0, 0)
        _DecalUVAngle("Decal UV Angle", Float) = 0
        _DecalColor("Decal Color", Color) = (2, 2, 0.5, 1)
        _DecalEnabled("Decal Enabled", Float) = 0
        
        // === Per-Part Flash Properties ===
        _Flash_Beard("Flash Beard", Float) = 0
        _Flash_Chest("Flash Chest", Float) = 0
        _Flash_Abs("Flash Abs", Float) = 0
        _Flash_Back("Flash Back", Float) = 0
        _Flash_LeftArmpit("Flash Left Armpit", Float) = 0
        _Flash_RightArmpit("Flash Right Armpit", Float) = 0
        _Flash_LeftUpperArm("Flash Left Upper Arm", Float) = 0
        _Flash_LeftLowerArm("Flash Left Lower Arm", Float) = 0
        _Flash_RightUpperArm("Flash Right Upper Arm", Float) = 0
        _Flash_RightLowerArm("Flash Right Lower Arm", Float) = 0
        _Flash_LeftThigh("Flash Left Thigh", Float) = 0
        _Flash_LeftCalf("Flash Left Calf", Float) = 0
        _Flash_RightThigh("Flash Right Thigh", Float) = 0
        _Flash_RightCalf("Flash Right Calf", Float) = 0
        
        // Debug Mode: 0=Off, 1=Show part masks
        _DebugMode ("Debug Mode", Float) = 0
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
                
                // Per-Part Request Flags (1.0 = highlight, 0.0 = don't)
                float _Request_Beard;
                float _Request_Chest;
                float _Request_Abs;
                float _Request_Back;
                float _Request_LeftArmpit;
                float _Request_RightArmpit;
                float _Request_LeftUpperArm;
                float _Request_LeftLowerArm;
                float _Request_RightUpperArm;
                float _Request_RightLowerArm;
                float _Request_LeftThigh;
                float _Request_LeftCalf;
                float _Request_RightThigh;
                float _Request_RightCalf;
                
                // Per-Part Completed Flags
                float _Completed_Beard;
                float _Completed_Chest;
                float _Completed_Abs;
                float _Completed_Back;
                float _Completed_LeftArmpit;
                float _Completed_RightArmpit;
                float _Completed_LeftUpperArm;
                float _Completed_LeftLowerArm;
                float _Completed_RightUpperArm;
                float _Completed_RightLowerArm;
                float _Completed_LeftThigh;
                float _Completed_LeftCalf;
                float _Completed_RightThigh;
                float _Completed_RightCalf;
                
                float4 _HighlightColor;
                float _HighlightIntensity;
                
                // Decal (UV-based)
                float4 _DecalUVCenter;
                float4 _DecalUVSize;
                float _DecalUVAngle;
                float4 _DecalColor;
                float _DecalEnabled;
                
                // Per-Part Flash Properties
                float _Flash_Beard;
                float _Flash_Chest;
                float _Flash_Abs;
                float _Flash_Back;
                float _Flash_LeftArmpit;
                float _Flash_RightArmpit;
                float _Flash_LeftUpperArm;
                float _Flash_LeftLowerArm;
                float _Flash_RightUpperArm;
                float _Flash_RightLowerArm;
                float _Flash_LeftThigh;
                float _Flash_LeftCalf;
                float _Flash_RightThigh;
                float _Flash_RightCalf;
                
                float _DebugMode;

            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);
            TEXTURE2D(_HairGrowthMask); SAMPLER(sampler_HairGrowthMask);
            
            // Per-Part Binary Mask Textures (share sampler to save resources)
            TEXTURE2D(_Mask_Beard);
            TEXTURE2D(_Mask_Chest);
            TEXTURE2D(_Mask_Abs);
            TEXTURE2D(_Mask_Back);
            TEXTURE2D(_Mask_LeftArmpit);
            TEXTURE2D(_Mask_RightArmpit);
            TEXTURE2D(_Mask_LeftUpperArm);
            TEXTURE2D(_Mask_LeftLowerArm);
            TEXTURE2D(_Mask_RightUpperArm);
            TEXTURE2D(_Mask_RightLowerArm);
            TEXTURE2D(_Mask_LeftThigh);
            TEXTURE2D(_Mask_LeftCalf);
            TEXTURE2D(_Mask_RightThigh);
            TEXTURE2D(_Mask_RightCalf);
            SAMPLER(sampler_Mask_Beard); // Shared sampler for all masks

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

                // === Per-Part Binary Mask System ===
                // Sample each part mask and check against request/completed flags
                
                // Sample all 14 part masks
                float maskBeard = SAMPLE_TEXTURE2D(_Mask_Beard, sampler_Mask_Beard, input.uv).r;
                float maskChest = SAMPLE_TEXTURE2D(_Mask_Chest, sampler_Mask_Beard, input.uv).r;
                float maskAbs = SAMPLE_TEXTURE2D(_Mask_Abs, sampler_Mask_Beard, input.uv).r;
                float maskBack = SAMPLE_TEXTURE2D(_Mask_Back, sampler_Mask_Beard, input.uv).r;
                float maskLeftArmpit = SAMPLE_TEXTURE2D(_Mask_LeftArmpit, sampler_Mask_Beard, input.uv).r;
                float maskRightArmpit = SAMPLE_TEXTURE2D(_Mask_RightArmpit, sampler_Mask_Beard, input.uv).r;
                float maskLeftUpperArm = SAMPLE_TEXTURE2D(_Mask_LeftUpperArm, sampler_Mask_Beard, input.uv).r;
                float maskLeftLowerArm = SAMPLE_TEXTURE2D(_Mask_LeftLowerArm, sampler_Mask_Beard, input.uv).r;
                float maskRightUpperArm = SAMPLE_TEXTURE2D(_Mask_RightUpperArm, sampler_Mask_Beard, input.uv).r;
                float maskRightLowerArm = SAMPLE_TEXTURE2D(_Mask_RightLowerArm, sampler_Mask_Beard, input.uv).r;
                float maskLeftThigh = SAMPLE_TEXTURE2D(_Mask_LeftThigh, sampler_Mask_Beard, input.uv).r;
                float maskLeftCalf = SAMPLE_TEXTURE2D(_Mask_LeftCalf, sampler_Mask_Beard, input.uv).r;
                float maskRightThigh = SAMPLE_TEXTURE2D(_Mask_RightThigh, sampler_Mask_Beard, input.uv).r;
                float maskRightCalf = SAMPLE_TEXTURE2D(_Mask_RightCalf, sampler_Mask_Beard, input.uv).r;
                
                // DEBUG MODE: Show which parts are active
                if (_DebugMode > 0.5)
                {
                    half3 debugColor = half3(0, 0, 0);
                    // Show highest priority mask as color
                    if (maskBeard > 0.5)        debugColor = half3(0.5, 0, 0.5);  // Purple
                    else if (maskChest > 0.5)   debugColor = half3(0, 0, 1);      // Blue
                    else if (maskAbs > 0.5)     debugColor = half3(0, 0.5, 1);    // Light Blue
                    else if (maskBack > 0.5)    debugColor = half3(0, 1, 1);      // Cyan
                    else if (maskLeftArmpit > 0.5 || maskRightArmpit > 0.5) debugColor = half3(1, 0.5, 0); // Orange
                    else if (maskLeftUpperArm > 0.5) debugColor = half3(0, 1, 0.5); // Teal
                    else if (maskLeftLowerArm > 0.5) debugColor = half3(0, 1, 0);   // Green
                    else if (maskRightUpperArm > 0.5) debugColor = half3(1, 1, 0);  // Yellow
                    else if (maskRightLowerArm > 0.5) debugColor = half3(1, 0.7, 0);// Gold
                    else if (maskLeftThigh > 0.5) debugColor = half3(1, 0.3, 0);    // Dark Orange
                    else if (maskLeftCalf > 0.5)  debugColor = half3(1, 0, 0);      // Red
                    else if (maskRightThigh > 0.5) debugColor = half3(1, 0, 0.5);   // Pink
                    else if (maskRightCalf > 0.5) debugColor = half3(1, 0, 1);      // Magenta
                    return half4(debugColor, 1.0);
                }
                
                // Check if any requested part is active at this pixel (and not completed)
                bool isRequested = 
                    (maskBeard > 0.5 && _Request_Beard > 0.5) ||
                    (maskChest > 0.5 && _Request_Chest > 0.5) ||
                    (maskAbs > 0.5 && _Request_Abs > 0.5) ||
                    (maskBack > 0.5 && _Request_Back > 0.5) ||
                    (maskLeftArmpit > 0.5 && _Request_LeftArmpit > 0.5) ||
                    (maskRightArmpit > 0.5 && _Request_RightArmpit > 0.5) ||
                    (maskLeftUpperArm > 0.5 && _Request_LeftUpperArm > 0.5) ||
                    (maskLeftLowerArm > 0.5 && _Request_LeftLowerArm > 0.5) ||
                    (maskRightUpperArm > 0.5 && _Request_RightUpperArm > 0.5) ||
                    (maskRightLowerArm > 0.5 && _Request_RightLowerArm > 0.5) ||
                    (maskLeftThigh > 0.5 && _Request_LeftThigh > 0.5) ||
                    (maskLeftCalf > 0.5 && _Request_LeftCalf > 0.5) ||
                    (maskRightThigh > 0.5 && _Request_RightThigh > 0.5) ||
                    (maskRightCalf > 0.5 && _Request_RightCalf > 0.5);
                
                // Check if any completed part is active at this pixel
                bool isCompleted = 
                    (maskBeard > 0.5 && _Completed_Beard > 0.5) ||
                    (maskChest > 0.5 && _Completed_Chest > 0.5) ||
                    (maskAbs > 0.5 && _Completed_Abs > 0.5) ||
                    (maskBack > 0.5 && _Completed_Back > 0.5) ||
                    (maskLeftArmpit > 0.5 && _Completed_LeftArmpit > 0.5) ||
                    (maskRightArmpit > 0.5 && _Completed_RightArmpit > 0.5) ||
                    (maskLeftUpperArm > 0.5 && _Completed_LeftUpperArm > 0.5) ||
                    (maskLeftLowerArm > 0.5 && _Completed_LeftLowerArm > 0.5) ||
                    (maskRightUpperArm > 0.5 && _Completed_RightUpperArm > 0.5) ||
                    (maskRightLowerArm > 0.5 && _Completed_RightLowerArm > 0.5) ||
                    (maskLeftThigh > 0.5 && _Completed_LeftThigh > 0.5) ||
                    (maskLeftCalf > 0.5 && _Completed_LeftCalf > 0.5) ||
                    (maskRightThigh > 0.5 && _Completed_RightThigh > 0.5) ||
                    (maskRightCalf > 0.5 && _Completed_RightCalf > 0.5);

                // Final highlight condition: requested but NOT completed
                bool shouldHighlight = isRequested && !isCompleted;
                
                // Get hair growth density from mask
                float hairGrowth = SAMPLE_TEXTURE2D(_HairGrowthMask, sampler_HairGrowthMask, input.uv).r;

                if (input.isHair > 0.5)
                {
                    half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                    
                    // Discard if: no hair in mask, or no growth in this area, or part is completed
                    if (mask.r < 0.1 || hairGrowth < 0.1 || isCompleted) discard;

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
                    
                    // Sample MaskMap to check if hair still exists in this area
                    half4 bodyMask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                    bool hairStillExists = bodyMask.r > 0.1;
                    
                    if (shouldHighlight && hairGrowth > 0.1 && hairStillExists)
                    {
                        finalColor.rgb += _HighlightColor.rgb * _HighlightIntensity;
                    }
                    
                    // Per-part flash effect (only flashes the specific completed part)
                    float flashAmount = 0.0;
                    if (maskBeard > 0.5) flashAmount = max(flashAmount, _Flash_Beard);
                    if (maskChest > 0.5) flashAmount = max(flashAmount, _Flash_Chest);
                    if (maskAbs > 0.5) flashAmount = max(flashAmount, _Flash_Abs);
                    if (maskBack > 0.5) flashAmount = max(flashAmount, _Flash_Back);
                    if (maskLeftArmpit > 0.5) flashAmount = max(flashAmount, _Flash_LeftArmpit);
                    if (maskRightArmpit > 0.5) flashAmount = max(flashAmount, _Flash_RightArmpit);
                    if (maskLeftUpperArm > 0.5) flashAmount = max(flashAmount, _Flash_LeftUpperArm);
                    if (maskLeftLowerArm > 0.5) flashAmount = max(flashAmount, _Flash_LeftLowerArm);
                    if (maskRightUpperArm > 0.5) flashAmount = max(flashAmount, _Flash_RightUpperArm);
                    if (maskRightLowerArm > 0.5) flashAmount = max(flashAmount, _Flash_RightLowerArm);
                    if (maskLeftThigh > 0.5) flashAmount = max(flashAmount, _Flash_LeftThigh);
                    if (maskLeftCalf > 0.5) flashAmount = max(flashAmount, _Flash_LeftCalf);
                    if (maskRightThigh > 0.5) flashAmount = max(flashAmount, _Flash_RightThigh);
                    if (maskRightCalf > 0.5) flashAmount = max(flashAmount, _Flash_RightCalf);
                    
                    if (flashAmount > 0.01)
                    {
                        finalColor.rgb += float3(1, 1, 1) * flashAmount;
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
        
        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            float3 _LightDirection;
            
            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }
            
            half4 ShadowFrag(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
