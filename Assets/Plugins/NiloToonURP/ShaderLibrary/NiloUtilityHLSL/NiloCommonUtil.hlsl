// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

half NiloAvg3(half3 inputColor)
{
    // Using dot product should be efficient on the GPU due to vectorization and SIMD.
    return dot(half3(1.0h/3.0h, 1.0h/3.0h, 1.0h/3.0h), inputColor);
    
    // Direct average calculation might be slightly faster on the CPU,
    // especially if SIMD optimizations are not in use.
    //return (inputColor.r + inputColor.g + inputColor.b) / 3.0h;
}

float NiloGetFacing(FRONT_FACE_TYPE IsFrontFace)
{
    return IS_FRONT_VFACE(IsFrontFace, 1.0, -1.0);
}

// TODO: revisit when min version is URP14, where sampler_PointClamp is defined by URP already
/*
struct NiloPrepassBufferRTData
{
    half materialID; // r
    half characterVisibleArea; // g
};

TEXTURE2D_X(_NiloToonPrepassBufferTex);
NiloPrepassBufferRTData NiloSamplePrepassBufferRT(float2 normalizeScreenSpaceUV)
{
    half4 sample = SAMPLE_TEXTURE2D_X(_NiloToonPrepassBufferTex, sampler_PointClamp, normalizeScreenSpaceUV);
    
    NiloPrepassBufferRTData data;
    data.materialID = sample.r * 255.0;
    data.characterVisibleArea = sample.g;
    
    return data;
}
*/
