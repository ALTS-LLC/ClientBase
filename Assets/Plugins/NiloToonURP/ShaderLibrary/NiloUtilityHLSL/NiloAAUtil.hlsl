// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

// for example, for 0~1 value 'x', you can input:
// - value = x
// - rangeMin = 0.33333
// - rangeMax = 0.66666
float applyAA(float value, float rangeMin, float rangeMax, float smoothScale = 1.0, float maxDerivative = 1.0)
{
    // Calculate the derivatives of the value along the x and y axes
    float dx = ddx(value);
    float dy = ddy(value);
    float derivative = abs(dx) + abs(dy);

    // Scale and clamp the derivative to control the smoothing effect
    derivative *= smoothScale;
    derivative = clamp(derivative, 0.0, maxDerivative);

    // Calculate a smooth transition between rangeMin and rangeMax, adjusted by the derivative
    float smoothedValue = smoothstep(rangeMin - derivative, rangeMax + derivative, value);

    return smoothedValue;
}

float screenSpaceGradientSmooth(float value, float edgeWidth, float2 uv) {
    float3 gradSample = float3(value, uv.x, uv.y);
    float3 ddxVal = ddx(gradSample);
    float3 ddyVal = ddy(gradSample);
    float gradMagnitude = sqrt(ddxVal.x * ddxVal.x + ddyVal.x * ddyVal.x);

    return smoothstep(0.5 - edgeWidth, 0.5 + edgeWidth, value + 0.5 * gradMagnitude);
}

