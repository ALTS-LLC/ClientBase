// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

// normal blending that is same as a Photoshop normal blending layer
half4 BlendNormalRGBA(half4 baseLayerRGBA, half4 topLayerRGBA)
{
    // Calculate the final alpha after blending
    half finalAlpha = topLayerRGBA.a + baseLayerRGBA.a * (1 - topLayerRGBA.a);
    
    // Calculate the final color
    half4 blendedColor;

    // use "finalAlpha > 0" will not work for mobile
    if (finalAlpha > HALF_EPS)
    {
        blendedColor.rgb = (topLayerRGBA.rgb * topLayerRGBA.a + baseLayerRGBA.rgb * baseLayerRGBA.a * (1 - topLayerRGBA.a)) / finalAlpha;
        blendedColor.a = finalAlpha;
    }
    else
    {
        blendedColor = baseLayerRGBA;
    }

    return blendedColor;
}
half4 BlendNormalRGB(half4 baseLayerRGBA, half4 topLayerRGBA)
{
    half4 blendedColor;
    blendedColor.rgb = lerp(baseLayerRGBA.rgb, topLayerRGBA.rgb, topLayerRGBA.a);
    blendedColor.a = baseLayerRGBA.a;
    return blendedColor;
}
half4 BlendAddRGB(half4 baseLayerRGBA, half4 topLayerRGBA)
{
    half4 blendedColor;
    blendedColor.rgb = baseLayerRGBA.rgb + topLayerRGBA.rgb * topLayerRGBA.a;
    blendedColor.a = baseLayerRGBA.a;
    return blendedColor;
}

half4 BlendScreenRGB(half4 baseLayerRGBA, half4 topLayerRGBA)
{
    // Can't handle HDR.
    // Optimized version of "1.0 - (1.0 - dstCol) * (1.0 - srcCol)"
    half4 blendedColor;
    blendedColor.rgb = baseLayerRGBA.rgb + (1 - baseLayerRGBA.rgb) * topLayerRGBA.rgb * topLayerRGBA.a;
    blendedColor.a = baseLayerRGBA.a;
    return blendedColor;
}

half4 BlendMultiplyRGB(half4 baseLayerRGBA, half4 topLayerRGBA)
{
    half4 blendedColor;
    blendedColor.rgb = baseLayerRGBA.rgb * lerp(1,topLayerRGBA.rgb, topLayerRGBA.a);
    blendedColor.a = baseLayerRGBA.a;
    return blendedColor;
}

// blendMode:
// - 0 (Normal RGBA)
// - 1 (Normal RGB)
// - 2 (Add RGB)
// - 3 (Screen RGB)
// - 4 (Multiply RGB)
// - 5 (None)
half4 BlendColor(half4 baseLayerRGBA, half4 topLayerRGBA, uint blendMode)
{
    if (blendMode == 0) // 0 (Normal RGBA, same as Photoshop)
    {
        return BlendNormalRGBA(baseLayerRGBA, topLayerRGBA);
    }
    else if (blendMode == 1) // 1 (Normal RGB)
    {
        return BlendNormalRGB(baseLayerRGBA, topLayerRGBA);
    }
    else if (blendMode == 2) // 2 (Add RGB)
    {
        return BlendAddRGB(baseLayerRGBA, topLayerRGBA);
    }
    else if (blendMode == 3) // 3 (Screen RGB)
    {
        return BlendScreenRGB(baseLayerRGBA, topLayerRGBA);
    }
    else if (blendMode == 4) // 4 (Multiply RGB)
    {
        return BlendMultiplyRGB(baseLayerRGBA, topLayerRGBA);
    }
    else if (blendMode == 5) // 5 (None)
    {
        return  baseLayerRGBA;
    }
    
    return baseLayerRGBA; // the default value without edit, if no matching blendMode
}
