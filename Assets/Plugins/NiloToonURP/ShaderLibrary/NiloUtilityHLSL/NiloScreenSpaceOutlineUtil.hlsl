// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

// [Screen Space Outline]
// TODO: result is not consistance when screen resolution / renderscale is changing, or when camera distance is changing
float IsScreenSpaceOutline(
    float2 SV_POSITIONxy,
    float OutlineThickness,
    float DepthSensitivity,
    float NormalsSensitivity,
    float selfLinearEyeDepth,
    float depthSensitivityDistanceFadeoutStrength,
    float cameraFOV,
    float3 viewDirectionWS)
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // We start from a screen space outline method by "alexander ameye", and develop base on it
    // *Code: https://gist.github.com/alexanderameye/d956574a67adf885f4e008d68b1c3238
    // *Tutorial: https://alexanderameye.github.io/notes/edge-detection-outlines/
    //
    // currently this method is using both depth and normals texture's screen space difference
    // TODO: add color's delta
    // TODO: add user defined color RT's difference (blue protocal material ID difference as outline method)?
    // TODO: add depth fix (https://twitter.com/bgolus/status/1532830971573129216)
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // [Should we use Raw depth (Raw depth texture sample value) or View space depth for depth delta/compare in screen space?]
    // = must use Raw depth! Why?
    // - Raw depth is linear in screen space(great for postprocess edge detection!), but non-linear in view space (bad for soft particle)
    // - view space depth is non-linear in screen space(bad for postprocess edge detection!), but linear in view space (great for soft particle)
    // https://www.humus.name/index.php?ID=255
    //
    //These days the depth buffer is increasingly being used for other purposes than just hidden surface removal.
    //Being linear in screen space turns out to be a very desirable property for post-processing.
    //Assume for instance that you want to do edge detection on the depth buffer, perhaps for antialiasing by blurring edges.
    //This is easily done by comparing a pixel's depth with its neighbors' depths. With Z values you have constant pixel-to-pixel deltas, except for across edges of course.
    //This is easy to detect by comparing the delta to the left and to the right, and if they don't match (with some epsilon) you crossed an edge.
    //And then of course the same with up-down and diagonally as well. This way you can also reject pixels that don't belong to the same surface if you implement say a blur filter but don't want to blur across edges, for instance for smoothing out artifacts in screen space effects, such as SSAO with relatively sparse sampling.
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    float2 texelSize = GetScaledScreenTexelSize(); // (x = 1/texture width, y = 1/texture height)
    float2 UV = SV_POSITIONxy * texelSize.xy; // UV is screen space [0,1] uv

    // far away pixels auto reduce depth sensitivity in shader, to avoid far object always = outline
    DepthSensitivity /= (1 + selfLinearEyeDepth * depthSensitivityDistanceFadeoutStrength * cameraFOV * 0.01); // 0.01 is a magic number that prevent most of the outline artifact

    // make screen space outline result not affected by render scale / game resolution
    OutlineThickness *= GetScaledScreenWidthHeight().y / 1080; // TODO: currently hardcode using height only, make it better for all aspect

    float halfScaleFloor = floor(OutlineThickness * 0.5);
    float halfScaleCeil = ceil(OutlineThickness * 0.5);
    
    float2 uvSamples[4];
    float depthSamples[4];
    float3 normalSamples[4];

    uvSamples[0] = UV - float2(texelSize.x, texelSize.y) * halfScaleFloor;
    uvSamples[1] = UV + float2(texelSize.x, texelSize.y) * halfScaleCeil;
    uvSamples[2] = UV + float2(texelSize.x * halfScaleCeil, -texelSize.y * halfScaleFloor);
    uvSamples[3] = UV + float2(-texelSize.x * halfScaleFloor, texelSize.y * halfScaleCeil);

    for(int i = 0; i < 4 ; i++)
    {
        depthSamples[i] = SampleSceneDepth(uvSamples[i]);
        normalSamples[i] = SampleSceneNormals(uvSamples[i]);
    }

    // Depth
    float depthFiniteDifference0 = depthSamples[1] - depthSamples[0];
    float depthFiniteDifference1 = depthSamples[3] - depthSamples[2];
    float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
    float depthThreshold = (1/DepthSensitivity) * depthSamples[0];
    edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

    // Normals
    float3 normalFiniteDifference0 = normalSamples[1] - normalSamples[0];
    float3 normalFiniteDifference1 = normalSamples[3] - normalSamples[2];
    float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
    edgeNormal = edgeNormal > (1/NormalsSensitivity) ? 1 : 0;

    float edge = max(edgeDepth, edgeNormal);
    return edge;
}


//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------
#define HQ_AA 0

float3 positionWSToFlatNormalWSUnitVector(float3 positionWS)
{
    // Calculate the rate of change of the position in the screen space x and y directions
    float3 ddxPosition = ddx(positionWS);
    float3 ddyPosition = ddy(positionWS);

    // The cross product of these two vectors gives the surface normal
    float3 normal = cross(ddyPosition,ddxPosition);

    // Normalize the result to ensure it's a unit vector
    normal = normalize(normal);

    return normal;
}

TEXTURE2D_X(_NiloToonPrepassBufferTex);

// Unity Core defined common inline sampler already in URP14
// see GlobalSamplers.hlsl
#if UNITY_VERSION < 202220
SAMPLER(sampler_PointClamp);
#endif

float SampleNiloCharacterMaterialID(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_NiloToonPrepassBufferTex, sampler_PointClamp, uv).r * 255.0;
}

float IsScreenSpaceOutlineV2(
    float2 SV_POSITIONxy,
    float OutlineThickness,
    float GeometryEdgeThreshold,
    float NormalAngleCosThetaThresholdMin,
    float NormalAngleCosThetaThresholdMax,
    bool DrawGeometryEdge,
    bool DrawNormalAngleEdge,
    bool DrawMaterialIDBoundary,
    bool DrawCustomIDBoundary,
    bool DrawWireframe,
    float3 positionWS)
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // We start from a screen space outline method by "alexander ameye", and develop base on it
    // *Code: https://gist.github.com/alexanderameye/d956574a67adf885f4e008d68b1c3238
    // *Tutorial: https://alexanderameye.github.io/notes/edge-detection-outlines/
    // also by "Robin Seibold"
    // https://youtu.be/LMqio9NsqmM?si=Lsgwp573KvCKV1rP
    //
    // Outline Shader
    // https://roystan.net/articles/outline-shader/
    // https://github.com/IronWarrior/UnityOutlineShader
    // https://github.com/Robinseibold/Unity-URP-Outlines
    //
    // Edge Detection Outlines
    // https://ameye.dev/notes/edge-detection-outlines/
    //
    // (SIGGRAPH 2020) That's a wrap: a Manifold Garden Rendering Retrospective
    // https://youtu.be/5VozHerlOQw?si=PsNHPRHkqwhSc6QA
    //
    // @bgolus's twitter about screen space outline 
    // https://twitter.com/bgolus/status/1532830971573129216
    //
    // currently this method is using both depth and normals texture's screen space difference
    // TODO: add color's delta
    // TODO: add user defined color RT's difference (blue protocal material ID difference as outline method)?
    // TODO: add depth fix (https://twitter.com/bgolus/status/1532830971573129216)
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    float2 texelSize = GetScaledScreenTexelSize(); // (x = 1/RT width, y = 1/RT height)
    float2 UV = SV_POSITIONxy * texelSize; // UV is screen space [0,1] uv, do not + 0.5 to SV_POSITIONxy

    // make screen space outline result not affected by render scale / game resolution
    float2 screenWidthHeight = GetScaledScreenWidthHeight();
    OutlineThickness *= min(screenWidthHeight.x,screenWidthHeight.y) / 1080;

    float halfScaleFloor = max(floor(OutlineThickness * 0.5),1); // max(x,1) to prevent 0 pixel offset
    float halfScaleCeil = ceil(OutlineThickness * 0.5);
    
    float resultOutline = 0;
 
    #if HQ_AA
    const uint SAMPLE_COUNT = 8;
    #else
    const uint SAMPLE_COUNT = 3;
    #endif
    
    float2 uvSamples[SAMPLE_COUNT];
    float depthSamples[SAMPLE_COUNT];
    float3 normalSamples[SAMPLE_COUNT];
    float3 positionWSSamples[SAMPLE_COUNT];
    float objectIDSamples[SAMPLE_COUNT];
    float sdf[SAMPLE_COUNT];
    float closestEyeDepthOfAllSamples = FLT_MAX;

    // 2x2
    uvSamples[0] = UV + float2(texelSize.x, 0);
    uvSamples[1] = UV + float2(texelSize.x, texelSize.y);
    uvSamples[2] = UV + float2(0, texelSize.y);

    #if HQ_AA
    uvSamples[3] = UV + float2(-texelSize.x, texelSize.y);
    uvSamples[4] = UV + float2(-texelSize.x, 0);
    uvSamples[5] = UV + float2(-texelSize.x, -texelSize.y);
    uvSamples[6] = UV + float2(0, -texelSize.y);
    uvSamples[7] = UV + float2(texelSize.x, -texelSize.y);
    #endif
    
    // sample all data of adjacent pixels around center pixel
    for(uint i = 0; i < SAMPLE_COUNT ; i++)
    {
        depthSamples[i] = SampleSceneDepth(uvSamples[i]);
        normalSamples[i] = SampleSceneNormals(uvSamples[i]);
        objectIDSamples[i] = SampleNiloCharacterMaterialID(uvSamples[i]);

        // Reconstruct the world space positions of pixels from the depth texture
        // https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/writing-shaders-urp-reconstruct-world-position.html
        //---------------------------------------------------------------------------------------
        float depth = depthSamples[i];

        // Adjust z to match NDC for OpenGL
        #if !UNITY_REVERSED_Z
        depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
        #endif
        
        positionWSSamples[i] = ComputeWorldSpacePosition(uvSamples[i],depth,UNITY_MATRIX_I_VP);
        //---------------------------------------------------------------------------------------
        
        sdf[i] = distance(UV / texelSize, uvSamples[i] / texelSize);

        closestEyeDepthOfAllSamples = min(closestEyeDepthOfAllSamples, LinearEyeDepth(depthSamples[i],_ZBufferParams));
    }

    // sample self pixel's depth+normal
    float selfDepthSample = SampleSceneDepth(UV);
    float3 selfNormalSample = SampleSceneNormals(UV); // sample N from camera normal texture is required, don't use fragment's normalWS since result is not correct

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // find Normals difference
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    float minDistanceToAnyEdge_normalMethod = FLT_MAX;
    for(uint j = 0; j < SAMPLE_COUNT; j++)
    {
        bool isOutline = dot(selfNormalSample, normalSamples[j]) < NormalAngleCosThetaThresholdMin;
        if(isOutline)
        {
            minDistanceToAnyEdge_normalMethod = min(minDistanceToAnyEdge_normalMethod, sdf[j]);
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Manifold Garden's "is adjacent positionWS on plane?" edge detection, it will prevent generating edge within a flat surface(e.g. big flat ground)
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------
    // do not use this as input -> ComputeWorldSpacePosition(UV,selfDepthSample,UNITY_MATRIX_I_VP);
    float3 selfFlatN = positionWSToFlatNormalWSUnitVector(positionWS); // since we are fitting a plane to center pixel, flatN is a must, do not use fragment's smoothed normal
    float flatNSmoothedNDiff = dot(selfNormalSample,selfFlatN);
    float minDistanceToAnyEdge_flatPolyDepthMethod = FLT_MAX;

    // only run when flat normal and smoothed normal are not having a big difference
    if(flatNSmoothedNDiff > 0.99)
    {
        for(uint i = 0; i < SAMPLE_COUNT; i++)
        {
            float3 adjacentPixelPositionWSRelativeToOriginalPixel = positionWSSamples[i] - positionWS;
            
            // for a plane passing origin with normal unit vector N,
            // and for a random position P,
            // dot(P, N) is the signed projected shortest distance from P to plane
            // (dot(P,N) is |P||N|cos(theta) = |P|cos(theta))
            float unsignedProjectedDistanceWSOfPointToPlane = abs(dot(selfFlatN,adjacentPixelPositionWSRelativeToOriginalPixel));

            // default 0.003 = 0.3cm is good
            bool isOutline = unsignedProjectedDistanceWSOfPointToPlane > GeometryEdgeThreshold;
            if(isOutline)
            {
                minDistanceToAnyEdge_flatPolyDepthMethod = min(minDistanceToAnyEdge_flatPolyDepthMethod, sdf[i]);
            }
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Material border edge
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    float selfObjectID = SampleNiloCharacterMaterialID(UV);
    float minDistanceToAnyEdge_objectIDMethod = FLT_MAX;
    for(uint k = 0; k < SAMPLE_COUNT; k++)
    {
        bool isOutline = selfObjectID != objectIDSamples[k];
        if(isOutline)
        {
            minDistanceToAnyEdge_objectIDMethod = min(minDistanceToAnyEdge_objectIDMethod, sdf[k]);
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // return result
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    float finalMinDistanceToAnyEdge = FLT_MAX;

    if(DrawGeometryEdge)
    {
        finalMinDistanceToAnyEdge = min(finalMinDistanceToAnyEdge,minDistanceToAnyEdge_flatPolyDepthMethod);
    }

    if(DrawNormalAngleEdge)
    {
        finalMinDistanceToAnyEdge = min(finalMinDistanceToAnyEdge,minDistanceToAnyEdge_normalMethod);
    }

    if(DrawMaterialIDBoundary)
    {
        finalMinDistanceToAnyEdge = min(finalMinDistanceToAnyEdge,minDistanceToAnyEdge_objectIDMethod);
    }

    //saturate(1-smoothstep(sqrt(2),sqrt(2)*3,finalMinDistanceToAnyEdge));
    if(abs(finalMinDistanceToAnyEdge-sqrt(2)) < 0.1) resultOutline = 0.25;
    if(abs(finalMinDistanceToAnyEdge-1.0) < 0.1) resultOutline = 1;
    //resultOutline = finalMinDistanceToAnyEdge < 100;// test

    // aa using fwidth
    /*
    if(resultOutline < 10)
    {
        float gradient = fwidth(resultOutline);
        resultOutline = smoothstep(0.5 * gradient, 1 - 0.5 * gradient, resultOutline);
    }
    */
 
    
    // apply distance fadeout
    float linearEyeDepth = LinearEyeDepth(closestEyeDepthOfAllSamples,_ZBufferParams);
    float fadeoutStart = 50;
    float fadeoutExtendRange = 100;
    float fadeoutFactor = smoothstep(fadeoutStart,fadeoutStart+fadeoutExtendRange,abs(linearEyeDepth));
    resultOutline = lerp(resultOutline,0,fadeoutFactor);

    return  resultOutline;
}

// [Notes of Manifold gargen's edge detection]
// (SIGGRAPH 2020) That's a wrap: a Manifold Garden Rendering Retrospective
// https://youtu.be/5VozHerlOQw?si=dSAwmKkJItHDJPhe

// MG_Siggraph: That's a wrap: A Manifold Garden Rendering Retrospective
// https://docs.google.com/presentation/d/166oOh7IS1uV_4mKzer_kVdBysAch6fqH/edit#slide=id.p1

// https://docs.google.com/document/d/1EcPQ2v7fAHhrHjnwMjJnL4Ijx5tQO4Sqv6kcq2fIdBg/edit
/*

Edges: Manifold Garden’s aesthetic relies heavily on its painterly shaded edges. 

We render these as a post processing effect, reading from an extra buffer containing normal/depth metadata.
First, consider a naive edge implementation: Check if any 4-neighbor pixel (up, down, left, right) has a ‘different‘ normal or depth; if so, this pixel is an outline!
Unfortunately this aliases terribly. It effectively renders outlines at a quarter resolution: if A->B is an edge so is B->A!
Additionally, the edges have no defined width meaning the theoretical nyquist frequency is infinitely large! 

To fix this, first let’s create a good definition of when pixels are considered ‘different’:
We propose to use a threshold on the angle between the world-space normals, and a threshold on the worldspace distance of a neighbouring pixel,
to a plane fit to the normal and position of the central pixel.
This definition is much easier to tweak than a naive straight threshold approach, and solves cases of false edges at high incidence angles.

Secondly, let’s consider a better definition of how to shade an edge:
If two pixels differ, there is an edge somewhere between them, meaning every pixel has some distance to its nearest edge - this is effectively a signed distance field (SDF)!
We can now render anti-aliased iso-lines of this SDF using standard techniques. Most importantly, this definition extends to arbitrary sample positions.
Manifold Gardens render loop works with MSAA - and we can calculate a distance for each subsample by testing for an edge against every other subsample,
effectively sampling the SDF at a much higher resolution.
The performance of this is not great however. Two observations made this approach feasible:
Most pixels are not edges. We can do a simpler broad check first, to see if a pixel can be an edge, and if not, skip the slow path.
Edges are symmetric, and we can prove the coarse pass only needs to be done at a ¼ resolution!

*/

/*
Achieving Rollerdrome's iconic comic book aesthetic | Unity
https://youtu.be/G1NY0LKDqJo?si=hPsLpmOkxpwMiKxh
https://unity.com/case-study/rollerdrome

outline produced from 2 RTs:

[1]
edge buffer = solid color ID RT, then draw edge on any color diff
- material ID
- vertex color (in modeling program, manual select faces and flood with target color)
- shadow map shadow result (like a screen space shadow map result)

[2 (not sure about this)]
another texture mask buffer =
- manual paint edge textures and mask textures
*/
