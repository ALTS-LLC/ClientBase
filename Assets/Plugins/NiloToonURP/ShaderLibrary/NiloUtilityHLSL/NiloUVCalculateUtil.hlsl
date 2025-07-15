// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

//------------------------------------------------------------------------------------------------------------------------------
// A list of util functions for UV calculation
//------------------------------------------------------------------------------------------------------------------------------
// matching lilToon (lil_common_functions.hls), in order to make lilToon -> NiloToon convertor works

// Rotation
float2 RotateUV(float2 uv, float2x2 rotationMatrix)
{
    return mul(rotationMatrix, uv - 0.5) + 0.5;
}

float2 RotateUV(float2 uv, float rotatedAngleInDegree)
{
    float si,co;
    sincos(DegToRad(rotatedAngleInDegree), si, co);
    float2 outuv = uv - 0.5;
    outuv = float2(
        outuv.x * co - outuv.y * si,
        outuv.x * si + outuv.y * co
    );
    outuv += 0.5;
    return outuv;
}

// [this function only exist in NiloToon]
float2 RotateAndCenterPivotScalePosUV(float2 uv, float rotatedAngleInDegree, float4 centerPivotScalePos)
{
    float si,co;
    sincos(DegToRad(rotatedAngleInDegree), si, co);
    float2 outuv = uv - 0.5;
    outuv = outuv / centerPivotScalePos.xy - centerPivotScalePos.zw;
    outuv = float2(
        outuv.x * co - outuv.y * si,
        outuv.x * si + outuv.y * co
    );
    outuv += 0.5;
    return outuv;
}

// Tiling, offset, animation calculations
float2 CalcUV(float2 uv, float4 tilingOffset)
{
    return uv * tilingOffset.xy + tilingOffset.zw;
}

float2 CalcUV(float2 uv, float4 tilingOffset, float rotatedAngleInDegree)
{
    float2 outuv = uv * tilingOffset.xy + tilingOffset.zw;
    outuv = RotateUV(outuv, rotatedAngleInDegree);
    return outuv;
}

float2 CalcUV(float2 uv, float4 tilingOffset, float timeInSeconds, float2 scrollSpeed)
{
    return uv * tilingOffset.xy + tilingOffset.zw + frac(timeInSeconds * scrollSpeed);
}

float2 CalcUV(float2 uv, float4 tilingOffset, float timeInSeconds, float2 scrollSpeed, float rotatedAngle, float rotateSpeed)
{
    float2 outuv = uv * tilingOffset.xy + tilingOffset.zw;
    outuv = RotateUV(outuv, rotatedAngle + rotateSpeed * timeInSeconds) + frac(scrollSpeed * timeInSeconds);
    return outuv;
}

// [this function only exist in NiloToon]
float2 CalcUV(float2 uv, float4 tilingOffset, float4 centerPivotScalePos, float timeInSeconds, float2 scrollSpeed, float rotatedAngle, float rotateSpeed)
{
    float2 outuv = uv * tilingOffset.xy + tilingOffset.zw;
    outuv = RotateAndCenterPivotScalePosUV(outuv, rotatedAngle + rotateSpeed * timeInSeconds, centerPivotScalePos) + frac(scrollSpeed * timeInSeconds);
    return outuv;
}

// An improved method when compared to a simple "view space normal remap as uv",
// it can reduce uv bad distortion when object is near the edge of the screen
// https://twitter.com/bgolus/status/1487224443688554497
// https://gist.github.com/bgolus/02e37cd76568520e20219dc51653ceaa
float2 CalcMatCapUV(float3 viewDirectionWS, float3 normalWS)
{
    // optimized version of float3 up = mul((float3x3)UNITY_MATRIX_I_V, float3(0,1,0));
    float3 up = float3(UNITY_MATRIX_I_V[0][1], UNITY_MATRIX_I_V[1][1], UNITY_MATRIX_I_V[2][1]);

    const float3 right = normalize(cross(up, viewDirectionWS));
    up = cross(viewDirectionWS, right);

    // optimized version of mul(float3x3(right, up, viewDirectionWS), normalWS).xy;
    // *Appended a negate to result uv's x, so result uv's sample will look the same as the texture
    return float2(-dot(right, normalWS), dot(up, normalWS));
}

// better method (less distortion when object is at the edge of screen)
float2 Calc3DSphereTo2DUV(float3 positionWS, float3 sphereCenterPosWS, float sphereRadius)
{
    // Convert view space positions to clip space using provided projection matrix
    float4 sphereCenterPosCS = mul(UNITY_MATRIX_VP, float4(sphereCenterPosWS, 1.0));
    float4 positionCS = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));
    
    // Perform perspective divide (homogeneous divide) to get normalized device coordinates (NDC)
    float2 sphereCenterNDC = sphereCenterPosCS.xy / sphereCenterPosCS.w;
    float2 positionNDC = positionCS.xy / positionCS.w;

    // Aspect ratio correction for the x-coordinate
    float aspectRatio = _ScreenParams.x/_ScreenParams.y;
    sphereCenterNDC.x *= aspectRatio;
    positionNDC.x *= aspectRatio;
    
    // Calculate UV coordinates based on NDC
    float2 resultUV;
    float halfSizeNDC = (sphereRadius / sphereCenterPosCS.w) * UNITY_MATRIX_P[1][1]; // Convert sphere radius to NDC space, P[1][1] to correct sync different FOV
    resultUV.x = -(positionNDC.x - sphereCenterNDC.x) / (2.0 * halfSizeNDC) + 0.5;
    resultUV.y = +(positionNDC.y - sphereCenterNDC.y) / (2.0 * halfSizeNDC) + 0.5;
    
    return resultUV;
}

float2 GetAspectRatioCorrectedNormalizedScreenSpaceUV(float2 normalizedScreenSpaceUV)
{
    float aspectRatio = _ScreenParams.x / _ScreenParams.y;
    float2 aspectCorrectedScreenSpaceUV = normalizedScreenSpaceUV;
    if (aspectRatio >= 1)
    {
        // Screen is wider than the texture
        aspectCorrectedScreenSpaceUV.x -= 0.5;
        aspectCorrectedScreenSpaceUV.x *= aspectRatio;
        aspectCorrectedScreenSpaceUV.x += 0.5;
    }
    else
    {
        // Screen is taller than the texture
        aspectCorrectedScreenSpaceUV.y -= 0.5;
        aspectCorrectedScreenSpaceUV.y /= aspectRatio;
        aspectCorrectedScreenSpaceUV.y += 0.5;
    }
    return aspectCorrectedScreenSpaceUV;
}

