// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

// Source:
// https://github.com/KhronosGroup/ToneMapping/blob/main/PBR_Neutral/pbrNeutral.glsl
// https://modelviewer.dev/examples/tone-mapping
// Input color is non-negative and resides in the Linear Rec. 709 color space.
// Output color is also Linear Rec. 709, but in the [0, 1] range.
float3 KhronosPBRNeutralToneMapping(float3 color) {
    const float startCompression = 0.8 - 0.04;
    const float desaturation = 0.15;

    float x = min(color.r, min(color.g, color.b));
    float offset = x < 0.08 ? x - 6.25 * x * x : 0.04;
    color -= offset;

    float peak = max(color.r, max(color.g, color.b));
    if (peak < startCompression) return color;

    const float d = 1. - startCompression;
    float newPeak = 1. - d * d / (peak + d - startCompression);
    color *= newPeak / peak;

    float g = 1. - 1. / (desaturation * (peak - newPeak) + 1.);
    return lerp(color, newPeak, g);
}

// similar to KhronosPBRNeutralToneMapping, but with linear NPR character color range in mind
half3 NiloNPRNeutralToneMappingV1(half3 color) {
    const float startCompression = 0.85; // 0.7~1 is good
    const float desaturation = 0.15;
    
    // NiloToon_Character shader's usual range is mostly in 0~1 range, unless it is specular/rim light/emission.
    // Here we use a linear multiply for this 0~1 input range to keep most of the character color range(especially skin) doing a linear remap only and exit,
    // curve tone mapping will not be applied to any 0~1 input range
    color *= startCompression; 

    float peak = max(color.r, max(color.g, color.b));
    if (peak < startCompression) return color;

    const float d = 1. - startCompression;
    float newPeak = 1. - d * d / (peak + d - startCompression);
    color *= newPeak / peak;

    float g = 1. - 1. / (desaturation * (peak - newPeak) + 1.);
    return lerp(color, newPeak, g);
}

float NiloApplyDarkenToToe(float x)
{
    const float toeStrength = 1.0;  // Adjust this to control the strength of the toe
    const float toeThreshold = 0.08;
    
    if (x >= toeThreshold)
        return x;
    
    float t = x / toeThreshold;
    float toeValue = t * t * (3.0 - 2.0 * t);  // Smoothstep function
    
    return lerp(x * (1.0 - toeStrength), x, toeValue);
}

float3 NiloApplyDarkenToToeToColor(float3 color)
{
    return float3(
        NiloApplyDarkenToToe(color.r),
        NiloApplyDarkenToToe(color.g),
        NiloApplyDarkenToToe(color.b)
    );
}

half3 NiloHybirdTonemap(half3 color, half3 originalTonemappedColor)
{
    // settings
    const float startCompression = 1.2; // any 1~max
    const float transitionToACESSpeed = 4;
    
    // keep low intensity range (0 ~ startCompression) unchanged and return as is
    float peak = max(color.r, max(color.g, color.b));
    if (peak < startCompression) return NiloApplyDarkenToToeToColor(color);

    // the brighter the value, the more towards the original tonemapped color, so in extreme high value, the tonemapping result will be in sync
    return lerp(originalTonemappedColor,color, 1.0 / (1.0 + (peak - startCompression) * transitionToACESSpeed));
    
    /*
    // [attempt 1]
    // desaturate HDR (1~MAX)
    //float compressionFactor = (peak - startCompression) / (peak * desaturation);
    //float a = 1.0 / (1.0 + compressionFactor);
    //return lerp(peak, color, a);

    // [attempt 2]
    // Apply ACES-inspired saturation to HDR range
    float3 nColor = color / peak; // Normalize color
    float3 mColor = saturate((nColor - 0.18) / (1.0 - 0.18)); // Apply shoulder
    float3 sColor = lerp(mColor, nColor, desaturation); // Control saturation strength

    // Blend between original color and saturated color based on how far into HDR we are
    float blend = saturate((peak - startCompression) / (2.0 - startCompression));
    return lerp(color, sColor * peak, blend);
    */
}
