
Shader "Hidden/NiloToon/NiloToonMotionBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        ZWrite Off ZTest Always Blend Off Cull Off
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

        TEXTURE2D_X(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D_X(_TileMaxTex);
        SAMPLER(sampler_TileMaxTex);
        TEXTURE2D_X(_NeighborMaxTex);
        SAMPLER(sampler_NeighborMaxTex_linear_clamp);
        TEXTURE2D_X(_MotionVectorTexture);
        SAMPLER(sampler_MotionVectorTexture);
        TEXTURE2D_X(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);
#if UNITY_VERSION <= 202310
        SAMPLER(sampler_LinearClamp);
        SAMPLER(sampler_PointClamp);
#endif


        TEXTURE2D_X(_CopyTex);
        SAMPLER(sampler_CopyTex);
        
        //float4 _MainTex_TexelSize;
        float _NumSamples;
        float _TileSize;
        float _Intensity;
        float _SoftZExtent;
        float4 _SourceMV_TexelSize;
        float _MaxBlurRadius;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
            output.texcoord = input.texcoord;
            return output;
        }

        float2 GetMotionVector(float2 uv)
        {
            // Unity doc to sample URP's motion vector
            // https://docs.unity3d.com/6000.0/Documentation/Manual/urp/features/motion-vectors-sample.html
            return SAMPLE_TEXTURE2D_X(_MotionVectorTexture, sampler_MotionVectorTexture, uv);
        }

        // return = is A at the back relative to B
        float SoftDepthCompare(float depthA, float depthB)
        {
            // Soft depth comparison function
            return saturate(1.0 - (depthA - depthB) / _SoftZExtent);
        }

        float ConeWeight(float2 X, float2 Y, float2 v)
        {
            // Cone function: linear falloff based on distance along the motion vector
            return saturate(1.0 - length(X - Y) / (length(v) + 1e-4));
        }

        float CylinderWeight(float2 X, float2 Y, float2 v)
        {
            // Cylinder function for simultaneous blurriness
            float dist = length(X - Y);
            float vLen = length(v) + 1e-4;
            return saturate(1.0 - smoothstep(0.95 * vLen, 1.05 * vLen, dist));
        }

         float Random(float2 uv)
        {
            // Simple hash-based random function
            return frac(sin(dot(uv , float2(12.9898,78.233))) * 43758.5453);
        }

        // copied from URP's CameraMotionBLur.shader
        float2 ClampVelocity(float2 velocity, float maxVelocity)
        {
            float len = length(velocity);
            return (len > 0.0) ? min(len, maxVelocity) * (velocity * rcp(len)) : 0.0;
        }
        
        float4 TileMaxFilter(Varyings input) : SV_Target
        {
            // Source texture dimensions
            float sourceWidth = _SourceMV_TexelSize.z;
            float sourceHeight = _SourceMV_TexelSize.w;

            // The size of the tile in pixels
            int tileSize = _TileSize;

            // Compute number of tiles (ensure integer values)
            float numTilesX = ceil(sourceWidth / tileSize);
            float numTilesY = ceil(sourceHeight / tileSize);

            // Compute tile indices in TileMax RT
            float2 tileCoord = input.texcoord * float2(numTilesX, numTilesY);
            int2 tileIndex = int2(tileCoord);

            // Compute the origin of the tile in source texture pixel coordinates
            float2 tileOrigin = float2(tileIndex) * tileSize;

            // Initialize variables
            float2 maxVelocity = float2(0, 0);
            float maxLength = 0;

            // Loop over the pixels in the tile
            for (int y = 0; y < tileSize; y++)
            {
                int py = int(tileOrigin.y) + y;
                //if (py >= sourceHeight)
                //    break; // Avoid accessing beyond texture height

                for (int x = 0; x < tileSize; x++)
                {
                    int px = int(tileOrigin.x) + x;
                    //if (px >= sourceWidth)
                    //    break; // Avoid accessing beyond texture width

                    float2 pixelCoord = float2(px, py);

                    // Convert pixel coordinates to UVs in the source texture
                    float2 uv = pixelCoord / float2(sourceWidth, sourceHeight);
                    uv = clamp(uv, 0.0, 1.0); // Ensure UVs are within [0,1]

                    // Sample the motion vector at this UV
                    float2 velocity = GetMotionVector(uv);

                    float velocityLength = length(velocity);

                    if (velocityLength > maxLength)
                    {
                        maxVelocity = velocity;
                        maxLength = velocityLength;
                    }
                }
            }

            return float4(maxVelocity, 0, 1);
        }

        float4 NeighborMaxFilter(Varyings input) : SV_Target
        {
            float2 maxVelocity = float2(0, 0);
            float maxLength = 0;
            
            // Compute the texel size of TileMaxTex in UV space
            float2 texelSize = _TileSize * _SourceMV_TexelSize.xy; // Equivalent to tile size in UVs

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    float2 offset = float2(x, y) * texelSize;
                    float2 uv = input.texcoord + offset;

                    // Clamp UVs to prevent sampling outside the texture
                    uv = clamp(uv, 0.0, 1.0);

                    float2 velocity = SAMPLE_TEXTURE2D(_TileMaxTex, sampler_TileMaxTex, uv).xy;
                    float velocityLength = length(velocity);
                    
                    if (velocityLength > maxLength)
                    {
                        maxVelocity = velocity;
                        maxLength = velocityLength;
                    }
                }
            }
            
            return float4(maxVelocity, 0, 1);
        }

        float4 NaiveMotionBlur(Varyings input) : SV_TARGET
        {
            float2 velocity = GetMotionVector(input.texcoord);

            float4 color = 0;
            for (int i = -32; i < 32; i++)
            {
                color += SAMPLE_TEXTURE2D(_CopyTex, sampler_CopyTex, input.texcoord + velocity * i * 0.005);
            }

            return color * (1.0/65.0);
        }
        float4 MotionBlur(Varyings input) : SV_Target
        {
            float _ExposureTime = (1000/60) * _Intensity;
            float _SampleCount = 15; // atleast 3, better to be odd number
            float _EarlyExitThreshold = 1e-4;
            //---------------------------
            float2 uv = input.texcoord;
            float2 pixelPos = uv * _ScaledScreenParams.xy;

            // Retrieve the dominant motion vector from NeighborMax texture
            float2 vN = SAMPLE_TEXTURE2D(_NeighborMaxTex, sampler_NeighborMaxTex_linear_clamp, uv).xy;
            vN *= _ExposureTime;
            
            vN = ClampVelocity(vN, _MaxBlurRadius);

            float vNLength = length(vN);

            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            
            // Check for significant motion
            if (vNLength <= _EarlyExitThreshold)
            {
                // No significant motion, return original color
                return color;
            }

            // Initialize accumulation variables
            float weightSum = 0.5;
            float3 colorSum = color * weightSum;

            // Get per-pixel motion vector
            float2 velocity = SAMPLE_TEXTURE2D(_MotionVectorTexture, sampler_MotionVectorTexture, uv).xy;
            velocity *= _ExposureTime;

            // Clamp velocity
            velocity = ClampVelocity(velocity, _MaxBlurRadius);

            // Get depth of current pixel
            float depth = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r,_ZBufferParams); // DX: 1 at near, 0 at far. Linear

            // Jitter to prevent ghosting
            float jitter = Random(uv) - 0.5;

            //jitter *= 0.5;
            
            // Number of samples along motion vector
            int S = _SampleCount;

            for (int i = 0; i < S; i++)
            {
                // Fraction along the motion vector (-0.5 to 0.5)
                float t = (i + jitter) / S - 0.5;

                // Sample position along the motion vector
                float2 sampleOffset = vN * t;
                float2 sampleUv = uv + sampleOffset * _SourceMV_TexelSize.xy;

                // Ensure sampleUv is within texture bounds
                sampleUv = saturate(sampleUv);

                // Retrieve sample color and depth
                float3 sampleColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, sampleUv).rgb;
                float sampleDepth = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, sampleUv).r, _ZBufferParams); // DX: 1 at near, 0 at far. Linear
                float2 sampleVelocity = GetMotionVector(sampleUv);
                sampleVelocity *= _ExposureTime;

                // Clamp the length of velocity to _MaxBlurRadius
                sampleVelocity = ClampVelocity(sampleVelocity, _MaxBlurRadius);

                // [copy directly according to McGuire12Blur paper]
                // Fore- vs. background classification of Y relative to X
                // f means front
                // b means back
                // depth input is 0~1 (we assume linear is better)
                // Note: we swap f&b's value, it is different to paper's design, but it is the correct way in unity 
                float f = SoftDepthCompare(sampleDepth, depth); // f = is offseted pixel(Y) at the front of original pixel(X)
                float b = SoftDepthCompare(depth, sampleDepth); // b = is offseted pixel(Y) at the back of original pixel(X)
                float alphaY = 0.0;

                // [copy directly according to McGuire12Blur paper]
                float2 X = pixelPos; // original pixel coord index
                float2 Y = pixelPos + sampleOffset; // offseted pixel coord index
                float2 VX = velocity; // original pixel velocity
                float2 VY = sampleVelocity; // offseted pixel velocity

                // TODO: length(X-Y) and Velocity should be using the same unit
                alphaY += f * ConeWeight(Y, X, VY); // case 1: Y contributes when Y is moving+blurry and passes in front of X
                alphaY += b * ConeWeight(X, Y, VX); // case 2: X itself is moving+blurry so we should be able to see through
                alphaY += CylinderWeight(Y, X, VY) * CylinderWeight(X, Y, VX) * 2.0; // case 3: 2 moving blur together

                // test
                alphaY *= 1.0 / max(1,distance(X,Y)/_TileSize);
                    
                // Accumulate color and weight
                colorSum += sampleColor * alphaY;
                weightSum += alphaY;
            }

            // Normalize accumulated color
            float3 finalColor = colorSum / max(weightSum, 1e-4);

            return float4(finalColor, 1.0);
        }
        
        ENDHLSL
        
        Pass // Tile max
        {
            Name "Tile Max"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment TileMaxFilter
            ENDHLSL
        }
        
        Pass // Neighbor max
        {
            Name "Neighbor Max"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment NeighborMaxFilter
            ENDHLSL
        }
        
        Pass // Motion blur
        {
            Name "Motion Blur"
            
            HLSLPROGRAM
            #pragma vertex Vert
            //#pragma fragment NaiveMotionBlur
            #pragma fragment MotionBlur
            ENDHLSL
        }
    }
}