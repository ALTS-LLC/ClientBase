// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

half3 AcesCustomTonemap(half3 input, half paramA, half paramB, half paramC, half paramD, half paramE)
{
    float3 u = paramA * input + paramB;
    float3 v = paramC * input + paramD;
    return saturate((input * u) / (input * v + paramE));
}
