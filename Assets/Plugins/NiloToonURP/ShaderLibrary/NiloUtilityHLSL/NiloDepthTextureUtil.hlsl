// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// core functions
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
float Convert_SV_PositionZ_ToLinearViewSpaceDepthPerspectiveCamera(float rawDepthValueFromDepthTextureSample)
{
    // if perspective camera, URP's LinearEyeDepth will handle everything for you
    // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
    // remember we can't use LinearEyeDepth() for orthographic camera!
    return LinearEyeDepth(rawDepthValueFromDepthTextureSample,_ZBufferParams);
}
float Convert_SV_PositionZ_ToLinearViewSpaceDepthOrthographicCamera(float rawDepthValueFromDepthTextureSample)
{
    // if orthographic camera, _CameraDepthTexture store scene depth linearly within 0~1 range, no matter which platform, even OpenGL
    // if platform use reverse depth, make sure to 1-depth also
    // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
    #if defined(UNITY_REVERSED_Z)
    // + UNITY_NEAR_CLIP_VALUE check here to support some android emulator also
    // TODO: check if this check is still useful or not
    rawDepthValueFromDepthTextureSample = UNITY_NEAR_CLIP_VALUE == 1 ? 1-rawDepthValueFromDepthTextureSample : rawDepthValueFromDepthTextureSample;
    #endif

    // simply lerp(near,far, [0,1]depth) to get view space depth
    return lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepthValueFromDepthTextureSample);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// high level helper functions
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// expected input:
// - SV_POSITION.z in fragment shader
// - tex2D(_CameraDepthTexture).r, which is SV_POSITION.z of ShadowCaster pass
// *this function runs slower but support both orthographic and perspective camera projection
float Convert_SV_PositionZ_ToLinearViewSpaceDepth(float SV_POSITIONz)
{
    // use a ? b : c (conditional move / movc) instead of if() here because if() itself may introduce more cost then a few extra math
    return IsPerspectiveProjection() ?
        Convert_SV_PositionZ_ToLinearViewSpaceDepthPerspectiveCamera(SV_POSITIONz):
        Convert_SV_PositionZ_ToLinearViewSpaceDepthOrthographicCamera(SV_POSITIONz)
        ;
}

float LoadDepthTextureLinearDepthVS(int2 loadTexPos)
{
    // clamp loadTexPos to prevent loading outside of _CameraDepthTexture's valid area
    loadTexPos.x =  max(loadTexPos.x,0);
    loadTexPos.y =  max(loadTexPos.y,0);
    loadTexPos = min(loadTexPos,GetScaledScreenWidthHeight()-1); 

    float depthTextureRawSampleValue = LoadSceneDepth(loadTexPos); // using URP provided LoadSceneDepth(pos), this will make rendering correct in VR also  
    float depthTextureLinearDepthVS = Convert_SV_PositionZ_ToLinearViewSpaceDepth(depthTextureRawSampleValue);
    return depthTextureLinearDepthVS;
}

