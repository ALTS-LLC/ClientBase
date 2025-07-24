// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

// reference: From mobile to high-end PC: Achieving high quality anime style rendering on Unity (https://youtu.be/egHSE0dpWRw?t=1222)

half3 ShiftTangent(half3 T, half3 N, half uvX, half frequency = 750, half shift = 0.015, half offset = 0)
{
    //distort T without texture read
    float ALU_shift = sin(uvX * frequency) * shift + offset;
    return normalize(T + ALU_shift * N);
}
// https://web.engr.oregonstate.edu/~mjb/cs519/Projects/Papers/HairRendering.pdf - page 10 & 11
half StrandSpecular(half3 T, half3 H, half exponent)
{
    half dotTH = dot(T,H);
    half sinTH = sqrt(1-dotTH*dotTH);
    half dirAtten = smoothstep(-1,0,dotTH);
    return dirAtten *pow(sinTH,exponent);
}
/////////////////////////////////////////////////////////////////////////////////
// helper functions
/////////////////////////////////////////////////////////////////////////////////
/*
half StrandSpecular(half3 T, half3 H, half3 N, half exponent, half uvX)
{
    return StrandSpecular(ShiftTangent(T,N,uvX), H, exponent);
}
half StrandSpecular(half3 T, half3 V, half3 L, half3 N, half exponent, half uvX)
{
    half3 H = normalize(L+V);
    return StrandSpecular(T, H, N, exponent, uvX);
}
*/
