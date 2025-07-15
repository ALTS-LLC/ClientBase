// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

// _ScaledScreenParams doesn't exist in URP10 (only exist in URP13 or higher), 
// so for old URP versions, we use _CameraDepthTexture_TexelSize as a fallback
// _CameraDepthTexture_TexelSize is not the best fallback solution, but works for NiloToonURP for now
float2 GetScaledScreenWidthHeight()
{
	// SHADER_LIBRARY_VERSION_MAJOR is deprecated for Unity2022.2 or later, so we will use UNITY_VERSION instead
    // see -> https://github.com/Cyanilux/URP_ShaderCodeTemplates/blob/main/URP_SimpleLitTemplate.shader#L145
    #if UNITY_VERSION >= 202210 // (for URP 13 or above)
		return _ScaledScreenParams.xy;
	#else
		return _CameraDepthTexture_TexelSize.zw;
	#endif
}
float2 GetScaledScreenTexelSize()
{
	// SHADER_LIBRARY_VERSION_MAJOR is deprecated for Unity2022.2 or later, so we will use UNITY_VERSION instead
    // see -> https://github.com/Cyanilux/URP_ShaderCodeTemplates/blob/main/URP_SimpleLitTemplate.shader#L145
    #if UNITY_VERSION >= 202210 // (for URP 13 or above)
		return 1.0/_ScaledScreenParams.xy;
        // return _ScaledScreenParams.zw-float2(1.0,1.0); // this line will produce wrong result, due to precision?
	#else
		return _CameraDepthTexture_TexelSize.xy; 
	#endif
}




