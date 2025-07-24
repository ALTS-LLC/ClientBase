// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

//#include "NiloInvLerpRemapUtil.hlsl" // TODO: not sure why we can't include this, we should be able to include this if #pragma once exist in that .hlsl file

half ExtractSingleChannel(half4 texSampleValue, half4 extractChannelMask)
{
    return dot(texSampleValue, extractChannelMask);
}

// assume rawMaskValue is a 0~1 value from a mask texture's single channel
half PostProcessMaskValue(half rawMaskValue, bool maskMapAsIDMap, half maskMapExtractFromID, bool maskMapInvertColor, half maskMapRemapStart, half maskMapRemapEnd)
{
    rawMaskValue = maskMapAsIDMap ? abs(rawMaskValue * 255 - maskMapExtractFromID) < 8.0 ? 1 : 0 : rawMaskValue;
    rawMaskValue = maskMapInvertColor ? 1 - rawMaskValue : rawMaskValue;
    rawMaskValue = invLerpClamp(maskMapRemapStart,maskMapRemapEnd,rawMaskValue);

    return rawMaskValue;
}