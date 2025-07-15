// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safeguard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

#include "NiloToonCharacter_RiderSupport.hlsl"

// calculate Albedo's shadow area's color, 
// any light's light color is NOT considered in this function yet!
// this function only apply hsv edit then multiply tint color to rawAlbedo, then returns a shadow color(with out considering light color)
half3 CalculateLightIndependentSelfShadowAlbedoColor(const ToonSurfaceData surfaceData, const ToonLightingData lightingData, const half finalShadowArea)
{
    // "Shadow Color" main group's toggle
    if(!_EnableShadowColor)
    {
        return surfaceData.albedo;
    }
    
    const half3 rawAlbedo = surfaceData.albedo;
    const half isFace = lightingData.isFaceArea;
    const half isSkin = lightingData.isSkinArea;
    const float2 uv = lightingData.uv;

    // calculate isLitToShadowTransitionArea,
    // if a pixel is inside LitToShadowTransitionArea, later we will do an additional hsv edit and color tint for this area, for producing a more artistic color expression of the LitToShadowTransitionArea
    const half isLitToShadowTransitionArea = saturate((1-abs(finalShadowArea-0.5)*2)*_LitToShadowTransitionAreaIntensity);
    //const half isLitToShadowTransitionArea = saturate(pow(4 * finalShadowArea * (1-finalShadowArea), 2) * _LitToShadowTransitionAreaIntensity); // it is also possible to use a smooth function
        
    // [hsv]
    const half HueOffset = _SelfShadowAreaHueOffset * _SelfShadowAreaHSVStrength + _LitToShadowTransitionAreaHueOffset * isLitToShadowTransitionArea; // 1 MAD
    const half SaturationBoost = _SelfShadowAreaSaturationBoost * _SelfShadowAreaHSVStrength+ _LitToShadowTransitionAreaSaturationBoost * isLitToShadowTransitionArea; // 1 MAD
    const half ValueMul = lerp(1,_SelfShadowAreaValueMul,_SelfShadowAreaHSVStrength) * lerp(1,_LitToShadowTransitionAreaValueMul, isLitToShadowTransitionArea);

    half3 originalColorHSV; // for receiving an extra output from ApplyHSVChange(...)
    half3 result = ApplyHSVChange(rawAlbedo, HueOffset, SaturationBoost, ValueMul, originalColorHSV);

    // [if albedo saturation is low, allow user to optionally replace to a fallback color instead of hue shift, in order to suppress hsv result's 0/low saturation color's random hue artifact due to GPU texture compression]
    const half3 fallbackColor = rawAlbedo * _LowSaturationFallbackColor.rgb;
    result = lerp(fallbackColor,result, lerp(1,saturate(originalColorHSV.y * 5),_LowSaturationFallbackColor.a)); //only 0~20% saturation area affected, 0% saturation area use 100% fallback

    // [tint]
    result *= _SelfShadowTintColor;

    // [lit to shadow area transition tint]
    result *= lerp(1,_LitToShadowTransitionAreaTintColor, isLitToShadowTransitionArea);

    ////////////////////////////////////////////
    // override if skin
    ////////////////////////////////////////////
    // skin can optionally completely override to just a simple single color tint
    result = lerp(result, rawAlbedo * _SkinShadowTintColor * _SkinShadowTintColor2 * _SkinShadowBrightness, isSkin * _OverrideBySkinShadowTintColor);

    ////////////////////////////////////////////
    // override if face
    ////////////////////////////////////////////
#if _ISFACE
    // face can optionally completely override to just a simple single color tint
    result = lerp(result, rawAlbedo * _FaceShadowTintColor * _FaceShadowTintColor2 * _FaceShadowBrightness, isFace * _OverrideByFaceShadowTintColor); 
#endif

    ////////////////////////////////////////////
    // override shadow color result by user's shadow color texture
    ////////////////////////////////////////////
#if _OVERRIDE_SHADOWCOLOR_BY_TEXTURE
    half4 overrideShadowColorTexValue = tex2D(_OverrideShadowColorTex, uv) * _OverrideShadowColorTexTintColor;
    overrideShadowColorTexValue.a = lerp(overrideShadowColorTexValue.a,1,_OverrideShadowColorTexIgnoreAlphaChannel);

    // when user provide a shadow color map that is to replace the shadow color, all tint edits to the albedo are lost(since it is a replacement), so we need to do tints again here
    const half3 externalBaseColorMul = GetCombinedBaseColor().rgb * _BaseMapBrightness * _PerCharacterBaseColorTint.rgb * _GlobalVolumeBaseColorTintColor.rgb;

    const half3 replaceModeResultShadowColor = overrideShadowColorTexValue.rgb * externalBaseColorMul;
    const half3 multiplyModeResultShadowColor = overrideShadowColorTexValue.rgb * rawAlbedo;

    // _OverrideShadowColorByTexMode (0,Replace or 1,Multiply)
    const half3 finalOverriddenShadowColor = _OverrideShadowColorByTexMode ? multiplyModeResultShadowColor : replaceModeResultShadowColor;

    half mask = dot(tex2D(_OverrideShadowColorMaskMap, uv),_OverrideShadowColorMaskMapChannelMask);
    mask = _OverrideShadowColorMaskMapInvertColor ? 1-mask : mask;
    
    const half finalApplyStrength = overrideShadowColorTexValue.a * _OverrideShadowColorByTexIntensity * mask;
    result = lerp(result, finalOverriddenShadowColor, finalApplyStrength);
#endif

    return result;
}

// [deprecated, this function is no longer used, the indirect light part is now happening in main light injection phase]
// calculate scene's light probe contribution
// will return _GlobalIndirectLightMinColor if light probe is completely black
/*
half3 ShadeGI(const ToonSurfaceData surfaceData, const ToonLightingData lightingData)
{
    // occlusion, force target area (defined by occlusion texture) become shadow
    // separated control for direct/indirect occlusion
    half indirectOcclusion = 1;

#if AnyOcclusionEnabled
    // default weaker occlusion for indirect
    indirectOcclusion = lerp(1, surfaceData.occlusion, _OcclusionStrengthIndirectMultiplier); // default 50% usage, but user can edit it
#endif

    // max() can prevent result completely black, if light probe was not baked and no direct light is active
    const half3 indirectLight = min(max(lightingData.SH,_GlobalIndirectLightMinColor),_GlobalIndirectLightMaxColor) * indirectOcclusion;

    return indirectLight * surfaceData.albedo; 
}
*/

// Most important function: lighting equation for main directional light
// Also this is the heaviest method!
// Return: rgb = result color, a = final rim rimAttenuation(rim area) 
half4 ShadeMainLight(inout ToonSurfaceData surfaceData, Varyings input, ToonLightingData lightingData, Light light, UVData uvData)
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Common data
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // unused result will be treated as dead code and removed by compiler, don't worry performance if you don't use it
    half3 N = lightingData.normalWS;
    half3 rawN = lightingData.normalWS_NoNormalMap;
    half3 L = light.direction;

    half3 V = lightingData.viewDirectionWS;
    half3 H = normalize(L+V);

    half NoL = dot(N,L); // don't saturate(), because we will remap NoL by smoothstep() later
    half saturateNoL = saturate(NoL);
    half NoV = saturate(dot(N,V));
    half NoH = saturate(dot(N,H));
    half LoH = saturate(dot(L,H));
    half VoV = saturate(dot(V,V));

    // unity_OrthoParams's x is orthographic camera’s width, y is orthographic camera’s height, z is unused and w is 1.0 when camera is orthographic, 0.0 when perspective.
    // (https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html)
    half orthographicCameraAmount = lerp(unity_OrthoParams.w,1,_PerspectiveRemovalAmount);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Lit (NoL cel diffuse, occlusion, URP shadow map)
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    half celShadeMidPointOffset = 0;

#if _SHADING_GRADEMAP
    half4 texValue = tex2D(_ShadingGradeMap, lightingData.uv);
    texValue = _ShadingGradeMapInvertColor ? 1 - texValue : texValue;
    half shadingGradeMapValue = dot(texValue, _ShadingGradeMapChannelMask);
    shadingGradeMapValue = invLerpClamp(_ShadingGradeMapRemapStart,_ShadingGradeMapRemapEnd, shadingGradeMapValue); // should remap first,
    shadingGradeMapValue = lerp(0.5, shadingGradeMapValue, _ShadingGradeMapStrength); // then apply per material fadeout.
    celShadeMidPointOffset = (shadingGradeMapValue - 0.5) * _ShadingGradeMapApplyRange + _ShadingGradeMapMidPointOffset;
#endif

    // remapped N dot L
    // simplest 1 line cel shade, you can always replace this line by your method, like a grayscale ramp texture.
    // celShadeResult: 0 is in shadow, 1 is in light
    half finalCelShadeMidPoint = _CelShadeMidPoint + celShadeMidPointOffset;
    half smoothstepNoL = smoothstep(finalCelShadeMidPoint-_CelShadeSoftness,finalCelShadeMidPoint+_CelShadeSoftness, NoL);

    // if you don't want direct lighting's NdotL cel shade effect looks too strong, set _MainLightIgnoreCelShade to a higher value
    half selfLightAttenuation = lerp(smoothstepNoL,1, _MainLightIgnoreCelShade);

    // selfLightAttenuation is a 0~1 value, 0 means completely in shadow, 1 means completely lit
    // later we will apply different types of shadow to selfLightAttenuation

#if _ISFACE
    // [calculate another set of selfLightAttenuation for face area]
    half celShadeResultForFaceArea = smoothstep(_CelShadeMidPointForFaceArea-_CelShadeSoftnessForFaceArea,_CelShadeMidPointForFaceArea+_CelShadeSoftnessForFaceArea, NoL);
    half ignoreCelShadeForFaceArea = _MainLightIgnoreCelShadeForFaceArea;
    #if _FACE_SHADOW_GRADIENTMAP
        ignoreCelShadeForFaceArea = max(ignoreCelShadeForFaceArea,_IgnoreDefaultMainLightFaceShadow);
    #endif
    half selfLightAttenuationForFaceArea = lerp(celShadeResultForFaceArea,1, ignoreCelShadeForFaceArea);
    // [if current fragment is face area, replace original selfLightAttenuation by face's selfLightAttenuation result]
    selfLightAttenuation = lerp(selfLightAttenuation, selfLightAttenuationForFaceArea, _OverrideCelShadeParamForFaceArea * lightingData.isFaceArea);
#endif

    // occlusion, force target area become shadow
    // separated control for direct/indirect occlusion
#if AnyOcclusionEnabled
    half directOcclusion = surfaceData.occlusion; // hardcode 100% usage, unlike indirectOcclusion in ShadeGI(), it is not adjustable
    selfLightAttenuation *= directOcclusion;
#endif

   
#if _ISFACE && (_FACE_SHADOW_GRADIENTMAP || _FACE_3D_RIMLIGHT_AND_SHADOW)
    // TODO: move this whole section to C# for optimization, when it is proven stable and bug free
    half3 faceForwardDirection = _NiloToonGlobalPerCharFaceForwardDirWSArray[_CharacterID];
    half3 faceUpwardDirection = _NiloToonGlobalPerCharFaceUpwardDirWSArray[_CharacterID];
    half3 faceRightDirWS = -cross(faceUpwardDirection, faceForwardDirection); //  right-hand rule cross(). Since both inputs are unit vector, normalize() is not needed

    // we can't just discard y (simply call directionVectorWS.xz) in world space, because character's head can have any kind of rotation,
    // we must transform all direction vector to head space first, then discard y (simply call directionVectorHeadSpace.xz in head space)
    // (what is head space? head space is a space where x basis vector is face's right, y basis vector is face's up, and z basis vector is face forward) 
    // Change of basis: https://www.3blue1brown.com/lessons/change-of-basis

    // fill in head space's basis vector x,y,z, similar to filling in a TBN matrix
    // since we only care rotation, 3x3 matrix is enough
    half3x3 worldSpaceToHeadSpaceMatrix = half3x3(faceRightDirWS,faceUpwardDirection,faceForwardDirection); 

    // [transform all directions to head space]
    // after a "rotation only" matrix mul, unit vector is still unit vector, no normalize() is needed 
    half3 faceForwardDirectionHeadSpace = mul(worldSpaceToHeadSpaceMatrix,faceForwardDirection);
    half3 faceRightDirectionHeadSpace = mul(worldSpaceToHeadSpaceMatrix,faceRightDirWS);
    half3 lightDirectionHeadSpace = mul(worldSpaceToHeadSpaceMatrix,L);

    // TODO: we need to handle a special case when "light(L) is parallel to _FaceUpDirection", 
    // which means we are going to call normalize(0 length vector) that will produce undefined result in GPU.
    // Currently we will just SafeNormalize(lightDirectionHeadSpace.xz) to make result at least consistent
    lightDirectionHeadSpace.y = 0;
    float2 lightDirectionHeadSpaceXZ = SafeNormalize(lightDirectionHeadSpace).xz;
    
    float2 faceForwardDirectionHeadSpaceXZ = normalize(faceForwardDirectionHeadSpace.xz);
    float2 faceRightDirectionHeadSpaceXZ = normalize(faceRightDirectionHeadSpace.xz);

    float FaceForwardDotL = dot(faceForwardDirectionHeadSpaceXZ, lightDirectionHeadSpaceXZ); 
    float FaceRightDotL = dot(faceRightDirectionHeadSpaceXZ, lightDirectionHeadSpaceXZ);
    
    #if _FACE_SHADOW_GRADIENTMAP
    //----------------------------------------------------------------------------------------
    // [correct implementation ref]
    /*
    float thresholdCompareValue = 1 - (dot(L, faceForward) * 0.5 + 0.5);
    float filpUVu = sign(dot(L, faceLeft));
    float shadowThresholdGradientFromTexture = tex2D(_FaceShadowGradientMap, uv * flaot2(filpUVu, 1)).g;

    float faceShadow = step(thresholdCompareValue, shadowThresholdGradientFromTexture); // or smoothstep depending on style preferred
    */
    //----------------------------------------------------------------------------------------
    float2 faceUV = uvData.GetUV(_FaceShadowGradientMapUVIndex);
    // uv tiling offset with pivot at center
    faceUV -= 0.5;
    faceUV = faceUV / _FaceShadowGradientMapUVCenterPivotScalePos.xy - _FaceShadowGradientMapUVCenterPivotScalePos.zw;
    faceUV += 0.5;
    // regular uv tiling offset
    faceUV = faceUV * _FaceShadowGradientMapUVScaleOffset.xy + _FaceShadowGradientMapUVScaleOffset.zw;

    // [old uv implementation]
    // assume texture _FaceShadowGradientMap can be mirrored horizontally(where nose is at the center of the texture), 
    // so we just flip uv.x to get another side's gradient data
    //float2 faceShadowGradientMapValueUV = faceUV * float2(sign(FaceRightDotL), 1);
    //faceShadowGradientMapValueUV.x *= _FaceShadowGradientMapUVxInvert ? -1 : 1;

    // [new uv implementation] 
    // _FaceShadowGradientMap can now place face at any position of the texture
    // but user need to define _FaceShadowGradientMapFaceMidPoint (default 0.5), so the shader can flip correctly no matter where the face is. 
    // For old material, since _FaceShadowGradientMapFaceMidPoint is default 0.5, they can produce the same result as the old uv implementation
    float2 faceShadowGradientMapValueUV = faceUV;

    if(sign(FaceRightDotL) < 0)
    {
        faceShadowGradientMapValueUV.x -= _FaceShadowGradientMapFaceMidPoint;
        faceShadowGradientMapValueUV.x *= -1;
        faceShadowGradientMapValueUV.x += _FaceShadowGradientMapFaceMidPoint;
    }
        
    faceShadowGradientMapValueUV.x -= _FaceShadowGradientMapFaceMidPoint;
    faceShadowGradientMapValueUV.x *= _FaceShadowGradientMapUVxInvert ? -1 : 1;
    faceShadowGradientMapValueUV.x += _FaceShadowGradientMapFaceMidPoint;

    //----------------------------------------------------------------------------------------
    half4 faceShadowGradientMapSampledValue = tex2D(_FaceShadowGradientMap, faceShadowGradientMapValueUV);
    half faceShadowGradientMapValue = dot(_FaceShadowGradientMapChannel, faceShadowGradientMapSampledValue);
    faceShadowGradientMapValue = _FaceShadowGradientMapInvertColor ? 1-faceShadowGradientMapValue : faceShadowGradientMapValue;
    
    // find apply area
    half faceShadowGradientMask = dot(tex2D(_FaceShadowGradientMaskMap, uvData.GetUV(_FaceShadowGradientMaskMapUVIndex)),_FaceShadowGradientMaskMapChannel);
    faceShadowGradientMask = _FaceShadowGradientMaskMapInvertColor? 1-faceShadowGradientMask : faceShadowGradientMask;
    half faceShadowGradientApplyArea = faceShadowGradientMask * lightingData.isFaceArea;
    
    // user debug
    if(_DebugFaceShadowGradientMap && faceShadowGradientApplyArea > 0)
    {
        return faceShadowGradientMapValue;
    }
    //----------------------------------------------------------------------------------------
    // final threshold test
    faceShadowGradientMapValue += _FaceShadowGradientOffset; // user controlled offset
    float resultThresholdValue = 1 - (FaceForwardDotL * 0.5 + 0.5);
    resultThresholdValue = max(_FaceShadowGradientThresholdMin, resultThresholdValue);
    resultThresholdValue = min(_FaceShadowGradientThresholdMax, resultThresholdValue);
    half faceShadowResult = smoothstep(resultThresholdValue-_FaceShadowGradientResultSoftness,resultThresholdValue+_FaceShadowGradientResultSoftness, faceShadowGradientMapValue);
    //----------------------------------------------------------------------------------------

    // intensity control (mask included)
    faceShadowResult = lerp(1,faceShadowResult,_FaceShadowGradientIntensity * faceShadowGradientApplyArea);

    selfLightAttenuation *= faceShadowResult;
    #endif

    #if _FACE_3D_RIMLIGHT_AND_SHADOW
    half3 smoothedN = input.smoothedNormalWS; // we want smoothed vertex face normal, not flattened face normal, for fresnel of the cheek/jawline
    half fresnel = 1-dot(smoothedN,V);
    half headSpaceLx = dot(L,faceRightDirWS); // TODO: light dir isn't flatten to head space xz, do we need to do it? it looks ok without doing it
    half headSpaceNx = dot(smoothedN,faceRightDirWS);

    // remap to show more rim & shadow
    half headSpaceLightingResult = smoothstep(0.4,0.6,((headSpaceNx * headSpaceLx) * 0.5 + 0.5)) * 2 - 1;

    // sample masks
    half noseRimAreaMask = dot(tex2D(_Face3DRimLightAndShadow_NoseRimLightMaskMap, lightingData.uv), _Face3DRimLightAndShadow_NoseRimLightMaskMapChannel);
    half noseShadowAreaMask = dot(tex2D(_Face3DRimLightAndShadow_NoseShadowMaskMap, lightingData.uv), _Face3DRimLightAndShadow_NoseShadowMaskMapChannel);
    half cheekRimAreaMask = dot(tex2D(_Face3DRimLightAndShadow_CheekRimLightMaskMap, lightingData.uv), _Face3DRimLightAndShadow_CheekRimLightMaskMapChannel);
    half cheekShadowAreaMask = dot(tex2D(_Face3DRimLightAndShadow_CheekShadowMaskMap, lightingData.uv), _Face3DRimLightAndShadow_CheekShadowMaskMapChannel);

    // calculate rim light (nose+cheek)
    half rimMidpoint = _Face3DRimLightAndShadow_CheekRimLightThreshold; // default 0.7
    half rimSmoothness = _Face3DRimLightAndShadow_CheekRimLightSoftness; // default 0.25
    half rimFresnel = smoothstep(rimMidpoint-rimSmoothness,rimMidpoint+rimSmoothness,fresnel);
    half rimLightComponent = saturate(headSpaceLightingResult) * lightingData.isFaceArea;

    half cheekRim = rimLightComponent * cheekRimAreaMask * rimFresnel * _Face3DRimLightAndShadow_CheekRimLightIntensity;
    half noseRim = rimLightComponent * noseRimAreaMask * _Face3DRimLightAndShadow_NoseRimLightIntensity;

    // calculate shadow
    half shadowMidpoint = _Face3DRimLightAndShadow_CheekShadowThreshold; // default 0.6
    half shadowSoftness = _Face3DRimLightAndShadow_CheekShadowSoftness; // default 0.05
    half shadowFresnel = smoothstep(shadowMidpoint-shadowSoftness,shadowMidpoint+shadowSoftness,fresnel);
    half shadowComponent = saturate(-headSpaceLightingResult) * lightingData.isFaceArea;

    half cheekShadow = shadowComponent * cheekShadowAreaMask * shadowFresnel * _Face3DRimLightAndShadow_CheekShadowIntensity;
    half noseShadow = shadowComponent * noseShadowAreaMask * _Face3DRimLightAndShadow_NoseShadowIntensity;

    // result
    half3 face3DRimAddColor = max(cheekRim * _Face3DRimLightAndShadow_CheekRimLightTintColor, noseRim * _Face3DRimLightAndShadow_NoseRimLightTintColor) * saturate(lightingData.mainLight.color) * 0.25; // *0.25 to make default value rim light looks correct
    half3 face3DShadowTintColor = min(lerp(1,_Face3DRimLightAndShadow_CheekShadowTintColor,cheekShadow), lerp(1,_Face3DRimLightAndShadow_NoseShadowTintColor, noseShadow));
    #endif
#endif

#if ShouldReceiveURPShadow
    // regular URP shadowmap but with sample position offset (extra depth bias), this extra depth bias is usually for avoiding ugly shadow map on face
    // invLerpClamp() to let user optionally remap shadow from soft shadow to sharp shadow, but shadow may flicker
    selfLightAttenuation *= invLerpClamp(0.5-_GlobalNiloToonReceiveURPShadowblurriness,0.5+_GlobalNiloToonReceiveURPShadowblurriness,light.shadowAttenuation);
#endif

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // "_CameraDepthTexture depth" vs "fragment self depth", linear view space depth difference as 2D rim light and 2D shadow 's show area
    // only renders if enableDepthTextureRimLightAndShadow enabled
    // (if enableDepthTextureRimLightAndShadow is off, 2D rim light will fall back to classic NoV rim light)
    // (if enableDepthTextureRimLightAndShadow is off, 2D shadow don't have any fallback)
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    half depthDiffShadow = 1;

    half rimAttenuation = 0;
#if NiloToonForwardLitPass || NiloToonSelfOutlinePass
    // [Why not using a faster multi_compile_local here, but choosing to use a slower shader if+else here?]
    // Before NiloToonURP 0.8.3, we use a global shader keyword _NILOTOON_ENABLE_DEPTH_TEXTURE_RIMLIGHT_AND_SHADOW for on/off this section to improve performance.
    // After NiloToonURP 0.8.3, we are using a shader if+else instead of shader keyword due to:
    // 1)shader memory will reduce 50%
    // 2)a shader if+else will allow per character on/off, while a global shader keyword can't
    // 3)we know that it is a static uniform branch(but with texture load inside), it is not fast due to texture load inside, but it is not extremely slow, which is usable.
    // 4)we tried turning _NILOTOON_ENABLE_DEPTH_TEXTURE_RIMLIGHT_AND_SHADOW to a local keyword, 
    //      but doing this will prevent user seeing correct result in edit mode, 
    //      since we can't enable/disable material keyword in editmode(which will pollute version control)
    //-------------------------------------------------------------------------------------------------------------
    // So we now rely on this bool enableDepthTextureRimLightAndShadow to control on/off
    // Only when "character script && material && global" all allows depth texture rim light, then we can render depth texture rim light safely
    // - _DitherFadeout will destroy DepthTextureRimLightAndShadow's result since it rely on a correct depth texture, so when dither fadeout is active, we disabled DepthTextureRimLightAndShadow
    bool enableDepthTextureRimLightAndShadow =  _PerMaterialEnableDepthTextureRimLightAndShadow && 
                                                (_ControlledByNiloToonPerCharacterRenderController ? _NiloToonEnableDepthTextureRimLightAndShadow : 1) && // _NiloToonEnableDepthTextureRimLightAndShadow is always 0 if NiloToonPerCharacterScript doesn't exist
                                                _GlobalEnableDepthTextureRimLigthAndShadow &&
                                                _AllowRenderDepthOnlyOrDepthNormalsPass &&
                                                !_GlobalShouldDisableNiloToonDepthTextureRimLightAndShadow &&
                                                ((_DitherFadeoutAmount == 0) || !_AllowPerCharacterDitherFadeout);
    if(enableDepthTextureRimLightAndShadow)
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Get _CameraDepthTexture's linear view space depth at offseted screen position(screen space uv offset)
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // for perspective camera:
        //      Assume 
        //      - d is distance to camera,
        //      - w is final width
        //      when eyeDepth > 1, keep rim width constant(character relative constant) when camera move away, using w=1/d
        //      when eyeDepth <= 1, let user pick a lerp between w=1/d and w=-d+2, these 2 curves meets at d=1 with the same slope
        // for orthographic camera:
        //      disable camera distance fix(only return a constant 0.875 to match perspective's result) since distance or depth will not affect polygon NDC xy position on screen in orthographic camera
        
        // lightingData.selfLinearEyeDepth already contains the effect of ZOffset
        // but we don't want ZOffset affecting the cameraDistanceFix(= affecting uv = affecting rim&shadow width), so we "+ lightingData.ZOffsetFinalSum" to cancel out ZOffset for uv calculation
        float selfLinearEyeDepthWithoutZOffset = lightingData.selfLinearEyeDepth + lightingData.ZOffsetFinalSum;

        float eyeDepthRCP = rcp(selfLinearEyeDepthWithoutZOffset); // w=1/d, this method will make rim light width ALWAYS constant (width relative to character)
        float flatLineExit = lerp(2, 1, selfLinearEyeDepthWithoutZOffset); // w=-d+2, this method will reduce rim light width when camera is too close(width relative to character)
        // Note: for flatlineExit, it is also possible to simply flatLineExit = 1, it can prevent rim light width too large on closeup, but we believe it is too conservation, it will make rim width too small
        float cameraDistanceFix_perspective = selfLinearEyeDepthWithoutZOffset > 1 ? eyeDepthRCP : lerp(eyeDepthRCP, flatLineExit, _DepthTexRimLightAndShadowReduceWidthWhenCameraIsClose);
        float cameraDistanceFix = IsPerspectiveProjection() ? cameraDistanceFix_perspective : 0.875; // no need to care orthographicCameraAmount, this line is already correct
        
        // [calculate finalDepthTexRimLightAndShadowWidthMultiplier]
        // allow width edit by global/per material/per vertex/per fragment
        // (* global * per material width multiplier)
        float finalDepthTexRimLightAndShadowWidthMultiplier = _DepthTexRimLightAndShadowWidthMultiplier * _DepthTexRimLightAndShadowWidthExtraMultiplier *  _GlobalDepthTexRimLightAndShadowWidthMultiplier;
        // (* vertex color width multiplier)
        float depthTexRimLightAndShadowWidthMultiplierFromVertexColor = dot(lightingData.vertexColor,_DepthTexRimLightAndShadowWidthMultiplierFromVertexColorChannelMask);
        finalDepthTexRimLightAndShadowWidthMultiplier *= _UseDepthTexRimLightAndShadowWidthMultiplierFromVertexColor ? depthTexRimLightAndShadowWidthMultiplierFromVertexColor : 1;
        // (* texture width multiplier)
        #if _DEPTHTEX_RIMLIGHT_SHADOW_WIDTHMAP
        {
            finalDepthTexRimLightAndShadowWidthMultiplier *= dot(tex2D(_DepthTexRimLightAndShadowWidthTex, lightingData.uv),_DepthTexRimLightAndShadowWidthTexChannelMask);
        }
        #endif

        #if _ISFACE
        {
            // face area shadow width is smaller since it is usually for receiving hair shadow
            // (in theory hair shadow caster is very close to face surface), so smaller shadow width is better
            finalDepthTexRimLightAndShadowWidthMultiplier *= lerp(1,0.66666,lightingData.isFaceArea); // hardcode 0.666. TODO: should we expose it to material UI?            
        }
        #endif

        // [Calculate fovFix]
        // when compared between 0.1/1/10/30/60/90 fov cameras,
        // using UNITY_MATRIX_P[1][1] will produce a more consistent rim light width then using FOV,
        // since UNITY_MATRIX_P[1][1] = 1 / tan(verticalFOV / 2), which equals to the final scaling of all vertices in NDC space directly
        // note: UNITY_MATRIX_P[0][0] = 1 / (aspectRatio * tan(verticalFOV / 2))
        float fovFix = abs(UNITY_MATRIX_P[1][1]) * 0.0054;
        //fovFix = _GlobalFOVorOrthoSizeFix; // fov is a usable but not a perfect value to use
        
        // group all float1 to calculate first for better performance
        float2 UvOffsetMultiplier = _GlobalAspectFix * (finalDepthTexRimLightAndShadowWidthMultiplier * fovFix * cameraDistanceFix);





        float2 originalUvOffsetMultiplier = UvOffsetMultiplier;



        UvOffsetMultiplier*= 0.707;
        
        // when rim light is off screen, it looks ugly with hard cutoff, so here we fadeout the rim light width before going outside of the screen(5% buffer)
        UvOffsetMultiplier.y *= smoothstep(1.0,0.95,lightingData.normalizedScreenSpaceUV.y);
        
        // offset according to view space light direction
        // light direction can be affected by additional light, so we can't use uniform directly
        float3 userOverridenMainLightDirVS = mul((float3x3)UNITY_MATRIX_V, lightingData.mainLight.direction);
        float3 DirVS360 = mul(input.smoothedNormalWS, (float3x3)UNITY_MATRIX_V);

        float3 userOverridenMainLightDirVSForShadow = mul((float3x3)UNITY_MATRIX_V, lerp(lightingData.mainLight.direction, _NiloToonGlobalPerCharFaceUpwardDirWSArray[_CharacterID], lightingData.isFaceArea * _DepthTexShadowFixedDirectionForFace));
        
        float2 depthTexRimlightFinalUVOffset = UvOffsetMultiplier * normalize(lerp(userOverridenMainLightDirVS.xy, DirVS360.xy, _DepthTexRimLightIgnoreLightDir)) * _DepthTexRimLightWidthMultiplier;
        float2 depthTexShadowFinalUVOffset = UvOffsetMultiplier * normalize(lerp(userOverridenMainLightDirVSForShadow.xy, DirVS360.xy, _DepthTexShadowIgnoreLightDir)) * _DepthTexShadowWidthMultiplier;



        float2 depthTexRimlightUvOffsetMultiplier = originalUvOffsetMultiplier * _DepthTexRimLightWidthMultiplier;
        float2 depthTexShadowUvOffsetMultiplier = originalUvOffsetMultiplier * _DepthTexShadowWidthMultiplier;

        

        // WIP(Danger)
        /*
        float2 UVOffset360 = .xy;
        
        float2 depthTexRimlightDir = normalize(lerp(defaultUVOffsetToLightDir,UVOffset360,_DepthTexRimLightIgnoreLightDir));
        float2 depthTexShadowDir = normalize(lerp(defaultUVOffsetToLightDir, UVOffset360, _DepthTexShadowIgnoreLightDir));
        
        float2 depthTexRimlightFinalUVOffset = depthTexRimlightDir * UvOffsetMultiplier * _DepthTexRimLightWidthMultiplier;
        float2 depthTexShadowFinalUVOffset = depthTexShadowDir * UvOffsetMultiplier * _DepthTexShadowWidthMultiplier;
        */
        
        // [get _CameraDepthTexture's camera depth data for finding the edge of large depth difference to character]
        // here we load _CameraDepthTexture once only, and use it as both rim light and shadow's input, 
        // very nice performance win if compared to load _CameraDepthTexture twice, but we lost the ability to edit width separately (e.g. rim and shadow separated width)
        // use LOAD instead of sample for better performance since we don't need mipmap of depth texture, and don't want bilinear filtering to depth data,which is meaningless
        int2 depthTexRimLightLoadTexPos = lightingData.SV_POSITIONxy + depthTexRimlightFinalUVOffset * GetScaledScreenWidthHeight();
        float depthTexRimLightLinearDepthVS = LoadDepthTextureLinearDepthVS(depthTexRimLightLoadTexPos);

        int2 depthTexShadowLoadTexPos = lightingData.SV_POSITIONxy + depthTexShadowFinalUVOffset * GetScaledScreenWidthHeight();
        float depthTexShadowLinearDepthVS = LoadDepthTextureLinearDepthVS(depthTexShadowLoadTexPos);

        #if _DEPTHTEX_RIMLIGHT_FIX_DOTTED_LINE_ARTIFACTS
            float2 offset = _DepthTexRimLightFixDottedLineArtifactsExtendMultiplier * depthTexRimlightUvOffsetMultiplier;

            int2 loadTexPosExtra1 = lightingData.SV_POSITIONxy + (depthTexRimlightFinalUVOffset + float2(-1.0,0) * offset) * GetScaledScreenWidthHeight();
            float depthTextureLinearDepthVSExtra1 = LoadDepthTextureLinearDepthVS(loadTexPosExtra1);

            int2 loadTexPosExtra2 = lightingData.SV_POSITIONxy + (depthTexRimlightFinalUVOffset + float2(+1.0 ,0) * offset) * GetScaledScreenWidthHeight();
            float depthTextureLinearDepthVSExtra2 = LoadDepthTextureLinearDepthVS(loadTexPosExtra2);

            int2 loadTexPosExtra3 = lightingData.SV_POSITIONxy + (depthTexRimlightFinalUVOffset + float2(0,+1.0) * offset) * GetScaledScreenWidthHeight();
            float depthTextureLinearDepthVSExtra3 = LoadDepthTextureLinearDepthVS(loadTexPosExtra3);
        #endif

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Calculate depth texture screen space rim light
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // - cancel out material ZOffset in depth tex rim's calculation, don't let material ZOffset affect rim result
        // - but still keep the per character ZOffset affecting rim result
        float selfLinearEyeDepthForDepthTexRimLight = lightingData.selfLinearEyeDepth + (lightingData.ZOffsetFinalSum - (-_PerCharZOffset)); 

        float depthTexRimLightDepthDiffThreshold = 0.05;

        // give user per material control (default 0)
        depthTexRimLightDepthDiffThreshold += _DepthTexRimLightThresholdOffset;

        float rimLightdepthDiffThreshold = saturate(depthTexRimLightDepthDiffThreshold + _GlobalDepthTexRimLightDepthDiffThresholdOffset);

        // counter/cancel face area ZOffset rendered in _CameraDepthTexture(NiloToonDepthOnlyOrDepthNormalPass)
        rimLightdepthDiffThreshold += _FaceAreaCameraDepthTextureZWriteOffset * lightingData.isFaceArea; 

        // let rim light fadeout softly and smoothly when depth difference is too small,
        // _DepthTexRimLightFadeoutRange will allow user controlling how the fadeout should look like
        rimAttenuation = saturate((depthTexRimLightLinearDepthVS - (selfLinearEyeDepthForDepthTexRimLight + rimLightdepthDiffThreshold)) * 10 / _DepthTexRimLightFadeoutRange);

        #if _DEPTHTEX_RIMLIGHT_FIX_DOTTED_LINE_ARTIFACTS
            float rimAttenuationExtraTest1 = saturate((depthTextureLinearDepthVSExtra1 - (selfLinearEyeDepthForDepthTexRimLight + rimLightdepthDiffThreshold)) * 10 / _DepthTexRimLightFadeoutRange);
            float rimAttenuationExtraTest2 = saturate((depthTextureLinearDepthVSExtra2 - (selfLinearEyeDepthForDepthTexRimLight + rimLightdepthDiffThreshold)) * 10 / _DepthTexRimLightFadeoutRange);
            float rimAttenuationExtraTest3 = saturate((depthTextureLinearDepthVSExtra3 - (selfLinearEyeDepthForDepthTexRimLight + rimLightdepthDiffThreshold)) * 10 / _DepthTexRimLightFadeoutRange);

            rimAttenuation = min(rimAttenuation,rimAttenuationExtraTest1);
            rimAttenuation = min(rimAttenuation,rimAttenuationExtraTest2);
            rimAttenuation = min(rimAttenuation,rimAttenuationExtraTest3);

            // test code to Anti Aliasing of rim light, a simple average 
            //rimAttenuation = (rimAttenuation+rimAttenuationExtraTest1+rimAttenuationExtraTest2+rimAttenuationExtraTest3)/4;
        #endif

        // TODO: write a better fwidth() method to produce anti-aliased 2D rim light, which worth the performance cost
        //      reference resources (need SDF input, which we don't have):
        //      - https://forum.unity.com/threads/antialiasing-circle-shader.432119/#post-2796401
        // the below example line = if fwidth() detected any difference within a 2x2 pixel block, replace rimAttenuation 1 to 0.6666, replace depthDiffRimAttenuation 0 to 0.3333
        // but it doesn't look good, so disabled
        //rimAttenuation = fwidth(rimAttenuation) > 0 ? rimAttenuation * 0.3333 + 0.333 : rimAttenuation;     

        // prevent small object like finger to have too much 2D rim light
        if(_DepthTexRimLight3DRimMaskEnable)
        {
            float blur = 0.025;
            rimAttenuation *= smoothstep(_DepthTexRimLight3DRimMaskThreshold-blur, _DepthTexRimLight3DRimMaskThreshold + blur, (saturate(NoL) * (1.0 - saturate(NoV)))); // 0.2(no blur) or 0.05~0.075(blur) threshold seems good
        }

        // rim light AA (test)
        //rimAttenuation = saturate(applyAA(rimAttenuation,0.33333,0.6666)); // assume rimAttenuation is 0~1

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // calculate depth texture screen space shadow
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // hardcode threshold to reduce material inspector complexity
        float depthTexShadowDepthDiffThreshold = 0.03;
        float depthTexShadowDepthDiffThresholdForFace = 0.01;
        // face area using a smaller threshold, since face area's depth was pushed in _CameraDepthTexture already
        #if _ISFACE
            depthTexShadowDepthDiffThreshold = lerp(depthTexShadowDepthDiffThreshold, depthTexShadowDepthDiffThresholdForFace, lightingData.isFaceArea);
        #endif
        // give user per material control (default 0)
        depthTexShadowDepthDiffThreshold += _DepthTexShadowThresholdOffset;

        // let shadow fadeout softly and smoothly when depth difference is too small
        // _DepthTexShadowFadeoutRange will allow user controlling how the fadeout should look like
        depthDiffShadow = saturate((depthTexShadowLinearDepthVS - (lightingData.selfLinearEyeDepth - depthTexShadowDepthDiffThreshold)) * 50 / _DepthTexShadowFadeoutRange);
        depthDiffShadow = lerp(1,depthDiffShadow,_DepthTexShadowUsage);
    }
    else
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // (if depth texture rim light is not enabled)
        // fall back to this classic NoV rim
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // extract camera forward from V matrix, it works because V matrix's scale is always 1, 
        // so we can use Z basis vector inside the 3x3 part of V matrix as camera forward directly 
        // https://www.youtube.com/playlist?list=PLZHQObOWTQDPD3MizzM2xVFitgF8hE_ab
        // https://answers.unity.com/questions/192553/camera-forward-vector-in-shader.html
        // *use UNITY_MATRIX_I_V instead of unity_CameraToWorld, since unity_CameraToWorld is not flipped between PC & PCVR mode
        half3 neg_cameraViewForwardDirWS = UNITY_MATRIX_I_V._m02_m12_m22;

        // [fresnel]
        // A note of V(view vector) for orthographics camera:
        // - for orthographic camera: use dot(N, -cameraForward) instead of dot(N,V), so rim result is the same no matter where the pixel is on screen 
        // - for perspective camera: use dot(N,V), as usual
        // A note of N(normal vector):
        // - We should NOT consider normal map contribution in order to produce better rim light(more similar to depth texture rim light), so we use PolygonNdotV(rawN) instead of NdotV
        half NoVForRimOrtho = saturate(dot(rawN, neg_cameraViewForwardDirWS));
        half NoVForRimPerspective = lightingData.NdotV_NoNormalMap;
        half NoVForRimResult = lerp(NoVForRimPerspective,NoVForRimOrtho, orthographicCameraAmount);
        rimAttenuation = 1 - NoVForRimResult; 

        // rim light area *= NoL mask
        rimAttenuation *= saturateNoL * 0.25 + 0.75;
        rimAttenuation *= NoL > 0;

        // rim light area *= remove rim on face
        rimAttenuation *= 1-lightingData.isFaceArea;

        // remap rim, try to match result to depth tex screen space 2D rim light's default result
        // use fwidth() to remove big area flat polygon's rim light pop
        half midPoint = _DepthTexRimLight3DFallbackMidPoint;
        half softness = _DepthTexRimLight3DFallbackSoftness;
        half fwidthFix = _DepthTexRimLight3DFallbackRemoveFlatPolygonRimLight ? saturate(fwidth(rimAttenuation) * 50) : 1;
        rimAttenuation = smoothstep(midPoint-softness,midPoint+softness,rimAttenuation) * fwidthFix;
    }

    // let rim light fadeout softly when character is very far away from camera, to avoid rim light flicker artifact(due to pixel count not enough)
    // it is similar to a linear fog, where fog amount(0~1) replaced to rim light fadeout amount(0~1)
    rimAttenuation = lerp(0, rimAttenuation, smoothstep(_GlobalDepthTexRimLightCameraDistanceFadeoutEndDistance, _GlobalDepthTexRimLightCameraDistanceFadeoutStartDistance, lightingData.selfLinearEyeDepth));
#endif

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Char self shadow map
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    half selfShadowMapShadow = 1; // default no self shadow map effect

#if _NILOTOON_RECEIVE_SELF_SHADOW
    // use an if() here to allow per material on/off
    if(_EnableNiloToonSelfShadowMapping && _ControlledByNiloToonPerCharacterRenderController) 
    {
        float3 selfPositionWS = lightingData.positionWS.xyz;
        float3 smoothedNormalWSUnitVector = normalize(input.smoothedNormalWS); // TODO: cache in lightingData

        // lightDirection pointing from vertex to light
        // (1 slope base +0.1 constant)
        float biasMultiplier = 1.1-dot(smoothedNormalWSUnitVector, _NiloToonSelfShadowLightDirection);
        
        // apply shadow receiver's normal bias, slope based
        // to inflate the surface position for shadow test (very important to hide shadow acne)
        // it is similar to finding the outline vertex position
        selfPositionWS += smoothedNormalWSUnitVector * biasMultiplier * _NiloToonGlobalSelfShadowReceiverNormalBias;
    
        // apply shadow receiver's depth bias, slope based
        selfPositionWS += _NiloToonSelfShadowLightDirection * biasMultiplier * _NiloToonGlobalSelfShadowReceiverDepthBias;

        // calculate shadowmap uv(x,y,z)
        // matrix mul is heavy(= 3 dot()), but better than passing float4 from Varyings due to interpolation cost 
        // this vector's xyz value is similar to "shadowCoord" of URP's Shadows.hlsl
        float4 positionSelfShadowCS = mul(_NiloToonSelfShadowWorldToClip, float4(selfPositionWS,1));

        // similar to URP's Shadows.hlsl->SampleShadowMap()'s isPerspectiveProjection,
        // ortho camera shadow map can ignore w divide since positionSelfShadowCS.w is always 1
        //float3 positionSelfShadowNDC = positionSelfShadowCS.xyz / positionSelfShadowCS.w; // ignored, no need this line's /w
        float3 positionSelfShadowNDC = positionSelfShadowCS.xyz; // this line is enough

        // TODO: see URP's Shadows.hlsl->SampleShadowmap(...),
        // it is possible to sample the shadowmap rightnow without any calculation,
        // since the following lines can be applied to _NiloToonSelfShadowWorldToClip in C#
        // see URP's MainLightShadowCasterPass.cs-> SetupMainLightShadowReceiverConstants(...) for calculate _WorldToShadow matrix

        // convert ndc.xy[-1,1] to uv[0,1]
        float2 shadowMapUV_XY = positionSelfShadowNDC.xy * 0.5 + 0.5;

        // calculate SAMPLE_TEXTURE2D_SHADOW(...)'s ndc.z compare value
        //---------------------------------------------------------
        float ndcZCompareValue = positionSelfShadowNDC.z;

        // if OpenGL, convert ndc.z [-1,1] to shadowmap's [0,1], because shadowmap is within 1~0 range (flipped when compared to DirectX)
        // if DirectX, do nothing, it is 0~1 range already
        ndcZCompareValue = UNITY_NEAR_CLIP_VALUE < 0 ? ndcZCompareValue * 0.5 + 0.5 : ndcZCompareValue;

        // +z compare bias (depth bias) in ndc.z [0,1] space, also apply DirectX's reverse depth to bias
        //ndcZCompareValue += (_NiloToonGlobalSelfShadowDepthBias+_NiloToonSelfShadowMappingDepthBias) * UNITY_NEAR_CLIP_VALUE;
        //---------------------------------------------------------

        // if DirectX, flip shadowMapUV's y (y = 1-y)
        // if OpenGL, do nothing
        #if UNITY_UV_STARTS_AT_TOP
        shadowMapUV_XY.y = 1 - shadowMapUV_XY.y;
        #endif

        float4 selfShadowmapUV = float4(shadowMapUV_XY,ndcZCompareValue,0); // packing for SAMPLE_TEXTURE2D_SHADOW

        // [Shadow test code, not used]
        // URP's 4 tap(mobile)/ 9 tap(non-mobile) soft shadow, reuse URP's _SHADOWS_SOFT keyword to avoid using more multi_compile, 
        // but it may be still too costly for mobile even it is just 4 tap
    #if _SHADOWS_SOFT && false // disabled soft shadow due to filter bug in 2021.3 or later

        // SHADER_LIBRARY_VERSION_MAJOR is deprecated for Unity2022.2 or later, so we will use UNITY_VERSION instead
        // see -> https://github.com/Cyanilux/URP_ShaderCodeTemplates/blob/main/URP_SimpleLitTemplate.shader#L145
        #if UNITY_VERSION >= 202220 // (for URP 14 or above)
        ShadowSamplingData shadowData;
        shadowData.shadowOffset0 = float4(+_NiloToonSelfShadowParam.x,+_NiloToonSelfShadowParam.y,-_NiloToonSelfShadowParam.x,+_NiloToonSelfShadowParam.y);
        shadowData.shadowOffset1 = float4(+_NiloToonSelfShadowParam.x,-_NiloToonSelfShadowParam.y,-_NiloToonSelfShadowParam.x,-_NiloToonSelfShadowParam.y);
        shadowData.shadowmapSize = _NiloToonSelfShadowParam;
        shadowData.softShadowQuality = SOFT_SHADOW_QUALITY_HIGH;
        #else
        ShadowSamplingData shadowData;
        shadowData.shadowOffset0 = float4(+_NiloToonSelfShadowParam.x,+_NiloToonSelfShadowParam.y,0,0);
        shadowData.shadowOffset1 = float4(-_NiloToonSelfShadowParam.x,+_NiloToonSelfShadowParam.y,0,0);
        shadowData.shadowOffset2 = float4(+_NiloToonSelfShadowParam.x,-_NiloToonSelfShadowParam.y,0,0);
        shadowData.shadowOffset3 = float4(-_NiloToonSelfShadowParam.x,-_NiloToonSelfShadowParam.y,0,0);
        shadowData.shadowmapSize = _NiloToonSelfShadowParam;
        #endif
        selfShadowMapShadow = SampleShadowmapFiltered(TEXTURE2D_SHADOW_ARGS(_NiloToonCharSelfShadowMapRT, sampler_NiloToonCharSelfShadowMapRT),selfShadowmapUV,shadowData);
    #endif
        
        // 4 shadow options(Off, low, medium, high)
        // the if-else chain pattern is similar to URP Shadows.hlsl -> SampleShadowmapFiltered(...)
        if(_NiloToonSelfShadowSoftShadowParam.x > 0)
        {
            // use URP's SampleShadowmapFiltered(...) directly
            
            ShadowSamplingData shadowSamplingData;

            // just for low quality 4-tap, offset is half pixel uv size to 4 directions
            float offset = _NiloToonSelfShadowParam.x / 2.0;
            
            #if UNITY_VERSION >= 202220 // (for URP 14 or above)
            shadowSamplingData.shadowOffset0 = float4(-offset, -offset, -offset, +offset);
            shadowSamplingData.shadowOffset1 = float4(+offset, +offset, +offset, -offset);
            shadowSamplingData.shadowmapSize = _NiloToonSelfShadowParam; // (1/w,1/h,w,h) of shadow map RT
            shadowSamplingData.softShadowQuality = _NiloToonSelfShadowSoftShadowParam.x; // (1~3 quality)
            #else
            shadowSamplingData.shadowOffset0 = float4(+offset,+offset,0,0);
            shadowSamplingData.shadowOffset1 = float4(-offset,+offset,0,0);
            shadowSamplingData.shadowOffset2 = float4(+offset,-offset,0,0);
            shadowSamplingData.shadowOffset3 = float4(-offset,-offset,0,0);
            shadowSamplingData.shadowmapSize = _NiloToonSelfShadowParam;
            #endif
            
            selfShadowMapShadow = SampleShadowmapFiltered(TEXTURE2D_SHADOW_ARGS(_NiloToonCharSelfShadowMapRT, sampler_NiloToonCharSelfShadowMapRT), selfShadowmapUV, shadowSamplingData);

            // NiloToon added: add a stupid method to temp fix shadow acne, as URP's SampleShadowmapFiltered doesn't apply shadow bias cone for uv away from the center sample
            selfShadowMapShadow = saturate(selfShadowMapShadow * 1.375); 
            
            // re-sharp shadow
            if(_NiloToonSelfShadowSoftShadowParam.y)
            {
                selfShadowMapShadow = smoothstep(0.5-_NiloToonSelfShadowSoftShadowParam.z,0.5+_NiloToonSelfShadowSoftShadowParam.z,selfShadowMapShadow);
            }
        }
        else
        {
            // a simple shadow map sample. The hardware will perform a 1-tap bilinear filter,
            // which is a no-cost filtering operation.
            selfShadowMapShadow = SAMPLE_TEXTURE2D_SHADOW(_NiloToonCharSelfShadowMapRT, sampler_NiloToonCharSelfShadowMapRT, selfShadowmapUV);
        }
        
        // [Shadow test code, not used]
        // URP16's high quality softshadow test code
        #if 0
        {
            float attenuation;
            
            ShadowSamplingData samplingData;
            samplingData.shadowmapSize = _NiloToonSelfShadowParam;

            float3 shadowCoord = selfShadowmapUV;
            TEXTURE2D(ShadowMap) = _NiloToonCharSelfShadowMapRT;          
            SAMPLER_CMP(sampler_ShadowMap) = sampler_NiloToonCharSelfShadowMapRT;
            //-------------------------------------------------------------------------------------------------------------------------
            real fetchesWeights[16];
            real2 fetchesUV[16];
            SampleShadow_ComputeSamples_Tent_7x7(samplingData.shadowmapSize, shadowCoord.xy, fetchesWeights, fetchesUV);

            attenuation = fetchesWeights[0] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[0].xy, shadowCoord.z))
                    + fetchesWeights[1] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[1].xy, shadowCoord.z))
                    + fetchesWeights[2] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[2].xy, shadowCoord.z))
                    + fetchesWeights[3] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[3].xy, shadowCoord.z))
                    + fetchesWeights[4] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[4].xy, shadowCoord.z))
                    + fetchesWeights[5] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[5].xy, shadowCoord.z))
                    + fetchesWeights[6] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[6].xy, shadowCoord.z))
                    + fetchesWeights[7] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[7].xy, shadowCoord.z))
                    + fetchesWeights[8] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[8].xy, shadowCoord.z))
                    + fetchesWeights[9] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[9].xy, shadowCoord.z))
                    + fetchesWeights[10] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[10].xy, shadowCoord.z))
                    + fetchesWeights[11] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[11].xy, shadowCoord.z))
                    + fetchesWeights[12] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[12].xy, shadowCoord.z))
                    + fetchesWeights[13] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[13].xy, shadowCoord.z))
                    + fetchesWeights[14] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[14].xy, shadowCoord.z))
                    + fetchesWeights[15] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[15].xy, shadowCoord.z));
            //-------------------------------------------------------------------------------------------------------------------------
            selfShadowMapShadow = attenuation;
            //return selfShadowMapShadow;
        } 
        #endif

        // fadeout self shadow map if reaching the end of shadow distance (always use 2m from start fade to end fade)
        float fadeTotalDistance = 1; // hardcode now, can expose to C# if needed
        selfShadowMapShadow = lerp(selfShadowMapShadow,1, saturate((1/fadeTotalDistance) * (abs(lightingData.selfLinearEyeDepth)-(_NiloToonSelfShadowRange-fadeTotalDistance))));

        // use additional N dot ShadowLight's L to hide self shadowmap artifact
        // smoothstep values 0.1,0.2 are based on observation only just to hide the artifact, no meaning
        selfShadowMapShadow *= _NiloToonSelfShadowUseNdotLFix ? smoothstep(0.1,0.2,saturate(dot(lightingData.normalWS_NoNormalMap, _NiloToonSelfShadowLightDirection))) : 1;

        // let user control self shadow intensity per material
        // For non-face, intensity is default 1
        // For face, intensity is default 0, because most of the time shadow map on face is ugly
        half selfShadowIntensity = lerp(_NiloToonSelfShadowIntensityForNonFace,_NiloToonSelfShadowIntensityForFace,lightingData.isFaceArea) * _NiloToonSelfShadowIntensity;
        selfShadowMapShadow = lerp(1,selfShadowMapShadow, selfShadowIntensity * _PerCharReceiveNiloToonSelfShadowMap * _GlobalReceiveNiloToonSelfShadowMap);

    #if _NILOTOON_SELFSHADOW_INTENSITY_MAP
        half SelfShadowIntensityMultiplierTexValue = tex2D(_NiloToonSelfShadowIntensityMultiplierTex, lightingData.uv).g;
        selfShadowMapShadow = lerp(1,selfShadowMapShadow,SelfShadowIntensityMultiplierTexValue);
    #endif
    }
#endif

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Lit
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    half finalShadowArea = selfLightAttenuation * depthDiffShadow * selfShadowMapShadow;

    #if _ISFACE && _FACE_3D_RIMLIGHT_AND_SHADOW
    finalShadowArea *= min(1-noseShadow, 1-cheekShadow);
    #endif
    
    finalShadowArea = saturate(lerp(1,finalShadowArea,_GlobalCharacterOverallShadowStrength));
    
    // [ramp lighting override]    
    // if enabled ramp lighting texture, we will ignore ALU lighting color, and use _RampLightingTex as shadow color instead
#if _RAMP_LIGHTING
    // 1.find ramp uv.x
    half rampUvX = ((NoL+celShadeMidPointOffset) * 0.5 + 0.5);
    rampUvX = invLerp(_RampLightingNdotLRemapStart,_RampLightingNdotLRemapEnd,rampUvX);// allow user to remap
    // for any shadow area, force uv.x = 0, so we will sample the shadow side of ramp tex
    rampUvX *= depthDiffShadow;
    #if AnyOcclusionEnabled
        rampUvX *= surfaceData.occlusion;
    #endif
    #if _NILOTOON_RECEIVE_SELF_SHADOW
        rampUvX *= selfShadowMapShadow;
    #endif
    #if ShouldReceiveURPShadow
        // regular URP shadowmap but with sample position offset (extra depth bias), usually for avoiding ugly shadow map on face
        rampUvX *= light.shadowAttenuation;
    #endif

    // 2.find ramp uv.y
    half rampLightingTexSamplingUvY;
    #if _RAMP_LIGHTING_SAMPLE_UVY_TEX
        rampLightingTexSamplingUvY = dot(tex2D(_RampLightingSampleUvYTex, lightingData.uv),_RampLightingSampleUvYTexChannelMask); // select channel from tex
        rampLightingTexSamplingUvY = _RampLightingSampleUvYTexInvertColor ? 1-rampLightingTexSamplingUvY : rampLightingTexSamplingUvY; // optional invert
        rampLightingTexSamplingUvY = rampLightingTexSamplingUvY * (_RampLightingUvYRemapEnd-_RampLightingUvYRemapStart) + _RampLightingUvYRemapStart; // optional remap target UvY range
    #else
        rampLightingTexSamplingUvY = _RampLightingTexSampleUvY;
    #endif

    // we need to sample ramp texture using clamp sampler, else uv.x == 0 may still sampled uv.x == 1's position
    // force LOD 0, not regular auto mip sample, because mixing mipmap of ramp texture is wrong
    half3 rampSampledColor = 0;
    if(_RampLightTexMode)
    {
        rampSampledColor = SAMPLE_TEXTURE2D_LOD(_RampLightingTex, sampler_linear_clamp, half2(rampUvX, rampLightingTexSamplingUvY),0).rgb;
    }
    else
    {
        rampSampledColor = SAMPLE_TEXTURE2D_LOD(_DynamicRampLightingTex, sampler_linear_clamp, half2(rampUvX, 0.5),0).rgb;
    }

    // if rampUvX > 1, it is out of _RampLightingTex's range, we treat it as no-op ramp (*= 1)
    half useRampPercentage = rampUvX <= 1;
    // remove ramp in face area, since usually it is not looking good
    #if _ISFACE
        useRampPercentage *= lerp(1,0, lightingData.isFaceArea * _RampLightingFaceAreaRemoveEffect);
    #endif
    // 5.apply "remove ramp"
    rampSampledColor = lerp(1,rampSampledColor,useRampPercentage);

    half3 lightColorIndependentLitColor = surfaceData.albedo * rampSampledColor; 
#else
    // if NOT enable ramp lighting texture, calculate shadow color by ALU (HSV edit, see CalculateLightIndependentSelfShadowAlbedoColor(...))
    half3 inSelfShadowAlbedoColor = CalculateLightIndependentSelfShadowAlbedoColor(surfaceData, lightingData, finalShadowArea);
    half3 lightColorIndependentLitColor = lerp(inSelfShadowAlbedoColor,surfaceData.albedo, finalShadowArea);
#endif

    // [extra user defined color tint control to shadow area]
#if NiloToonForwardLitPass || NiloToonSelfOutlinePass
    if(enableDepthTextureRimLightAndShadow)
    { 
        // make depth tex shadow a little bit darker
        // because depth tex shadow is similar to contact shadow, shadow caster and receiver position is close, which means shadow is strong (not much indirect light can reach)
        // we want depth tex shadow to have a bit different to self shadow to produce richer shadow, so default * 0.85
        half3 finalDepthTexShadowTintColor = _DepthTexShadowTintColor * _DepthTexShadowBrightness;
        #if _ISFACE
            // if face, override to constant high saturation red tint
            half3 faceDepthTexShadowTintColor = _DepthTexShadowTintColorForFace * _DepthTexShadowBrightnessForFace; //half3(1,0.85,0.85);
            finalDepthTexShadowTintColor = lerp(finalDepthTexShadowTintColor, faceDepthTexShadowTintColor, lightingData.isFaceArea);
        #endif
        lightColorIndependentLitColor *= lerp(finalDepthTexShadowTintColor,1,depthDiffShadow);
    }
    #if _NILOTOON_RECEIVE_SELF_SHADOW
        lightColorIndependentLitColor *= lerp(_NiloToonSelfShadowMappingTintColor,1,selfShadowMapShadow);
    #endif
    #if ShouldReceiveURPShadow 
        lightColorIndependentLitColor *= lerp(_URPShadowMappingTintColor,1,light.shadowAttenuation);
    #endif

    // global volume -> shadow tint color
    lightColorIndependentLitColor *= lerp(_GlobalCharacterOverallShadowTintColor,1,finalShadowArea);
#endif

    // for all additive light logic to use in the following code(specular,rim light, hair strand specular....), 
    // to reduce (to 25%) but not completely remove add light result if in shadow
    half inShadow25PercentMul = lerp(lightingData.averageShadowAttenuation,1,0.25);

    // 3D rim light and shadow apply shadow
    #if _ISFACE && _FACE_3D_RIMLIGHT_AND_SHADOW
    lightColorIndependentLitColor *= face3DShadowTintColor;
    #endif

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Specular
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if _SPECULARHIGHLIGHTS
    half3 specularLitAdd = surfaceData.specular * surfaceData.specularMask;

    // allow user select simple specular or GGX specular method
    half SpecularRawArea;
    bool reactToLightDir = _SpecularReactToLightDirMode == 0 ? _GlobalSpecularReactToLightDirectionChange : (_SpecularReactToLightDirMode == 1 ? true : false);
    if(_UseGGXDirectSpecular)
    {
        // [GGX specular method]
        half roughness = max(1-saturate(surfaceData.smoothness * _GGXDirectSpecularSmoothnessMultiplier),0.04); // don't let roughness = 0, it will produce bad bloom. Make it minimum 0.04 (atlease HALF_MIN_SQRT, but 0.04 can prevent bad bloom also)
        half F0 = 0.04; // only support non-metal for now, hardcode 0.04 F0
        
        if(reactToLightDir)
        {
            // normal GGX specular function (consider light direction), produce specular result like a PBR shader
            SpecularRawArea = GGXDirectSpecular_Optimized(H,saturateNoL,NoV,NoH,LoH, roughness, F0);
        }
        else
        {
            // L equals V GGX specular function (NOT consider light direction), produce specular result like a matcap specular
            SpecularRawArea = GGXDirectSpecular_LequalsV_Optimized(NoV,VoV, roughness, F0);
        }
    }
    else
    {
        // [simple specular]
        if(reactToLightDir)
        {
            // (consider light direction)
            SpecularRawArea = NoH;
        }
        else
        {
            // (NOT consider light direction)
            SpecularRawArea = NoV;
        }
    }
    half specularRemapStartPoint = max(0,_SpecularAreaRemapMidPoint-_SpecularAreaRemapRange);
    half specularRemapEndPoint = min(1,_SpecularAreaRemapMidPoint+_SpecularAreaRemapRange);
    half remappedSpecularRawArea = smoothstep(specularRemapStartPoint,specularRemapEndPoint, SpecularRawArea);
    remappedSpecularRawArea = lerp(SpecularRawArea, remappedSpecularRawArea, _SpecularAreaRemapUsage); // allow user to choose apply specular remap or not
    half3 specularResultRGBMultiplier = remappedSpecularRawArea;

    #if _RAMP_SPECULAR
    {
        // this section will override specularResultRGBMultiplier
        half rampSpecularTexSamplingUvY;
        #if _RAMP_SPECULAR_SAMPLE_UVY_TEX
            rampSpecularTexSamplingUvY = tex2D(_RampSpecularSampleUvYTex, lightingData.uv).g;
        #else
            rampSpecularTexSamplingUvY = _RampSpecularTexSampleUvY;
        #endif

        half rampUvX = saturate(remappedSpecularRawArea);
        half4 specularRampSampleValue = SAMPLE_TEXTURE2D(_RampSpecularTex, sampler_linear_clamp, half2(rampUvX, rampSpecularTexSamplingUvY));
        specularRampSampleValue.rgb /= _RampSpecularWhitePoint; // default mid gray in ramp texture = do nothing

        specularResultRGBMultiplier = lerp(1, specularRampSampleValue.rgb, specularRampSampleValue.a); // when ramp map's alpha = 0, don't apply specular
    }
    #endif

    specularLitAdd *= specularResultRGBMultiplier;

    // if ramp specular enabled, no need to do the this section, because ramp specular don't respect light color anyway
    #if !_RAMP_SPECULAR
    {
        // saturate(x), to prevent over bright
        specularLitAdd *= saturate(light.color) * _GlobalSpecularTintColor; 
        // keep 25% specular in 0% direct light(in shadow) enviroment
        specularLitAdd *= lerp(lightingData.averageShadowAttenuation,1,_GlobalSpecularMinIntensity); 
    }
    #endif

    if(_SpecularUseReplaceBlending)
    {
        half applyArea = saturate(remappedSpecularRawArea * surfaceData.specularMask * lerp(finalShadowArea,1,_SpecularShowInShadowArea));

        half3 applyColor = surfaceData.specular;

        // apply (replace blending)
        lightColorIndependentLitColor = lerp(lightColorIndependentLitColor, applyColor, applyArea);
        
        // cancel all additive specular
        specularLitAdd = 0;
    }
#endif

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Kajiya-Kay specular for hair
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if _KAJIYAKAY_SPECULAR
    half3 kajiyaSpecularAdd = 0;
    half3 shiftedT = ShiftTangent(lightingData.TBN_WS[1], lightingData.normalWS, lightingData.uv.x,_HairStrandSpecularShapeFrequency,_HairStrandSpecularShapeShift,_HairStrandSpecularShapePositionOffset);
    half3 HforStrandSpecular = normalize(_NiloToonGlobalPerCharFaceUpwardDirWSArray[_CharacterID] + V); // L+V, where L = _FaceUpDirection because it looks more stable
    kajiyaSpecularAdd += ((StrandSpecular(shiftedT, HforStrandSpecular,_HairStrandSpecularMainExponent))) * _HairStrandSpecularMainColor * _HairStrandSpecularMainIntensity; //sharp
    kajiyaSpecularAdd += ((StrandSpecular(shiftedT, HforStrandSpecular,_HairStrandSpecularSecondExponent))) * _HairStrandSpecularSecondColor * _HairStrandSpecularSecondIntensity; //soft
    kajiyaSpecularAdd *= 0.02 * _HairStrandSpecularOverallIntensity; // * 0.02 to allow _HairStrandSpecularMainColor & _HairStrandSpecularSecondColor's default value is white
    kajiyaSpecularAdd *= lerp(1, surfaceData.albedo, _HairStrandSpecularMixWithBaseMapColor);
    #if _KAJIYAKAY_SPECULAR_TEX_TINT
    {
        half3 kajiyaSpecularTexTint = tex2D(_HairStrandSpecularTintMap, lightingData.uv * _HairStrandSpecularTintMapTilingXyOffsetZw.xy +_HairStrandSpecularTintMapTilingXyOffsetZw.zw).rgb;
        kajiyaSpecularAdd *= lerp(1, kajiyaSpecularTexTint, _HairStrandSpecularTintMapUsage);
    }
    #endif
    // not mul inShadow25PercentMul because it looks better in shadow area
    // ...

    // max(0.25,x), to prevent 0 specular if Directional Light is off
    // saturate(x), to prevent over bright
    kajiyaSpecularAdd *= saturate(max(0.25,light.color));
    kajiyaSpecularAdd = max(0,kajiyaSpecularAdd); // prevent negative additive color       
#endif

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Composite final main light color
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // apply light color to the whole body at the final stage, it is better for toon/anime shading style's need 
    // min(LightMaxContribution,light.color) to prevent over bright, ideally we want to tonemap it, but for performance reason, just min looks good enough
    half3 result = min(_GlobalMainDirectionalLightMaxContribution, light.color) * lightColorIndependentLitColor;

    // darken direct light by URP shadow (control by volume)
#if ShouldReceiveURPShadow
    // regular URP shadowmap but with sample position offset (extra depth bias), usually for avoiding ugly shadow map on face
    result *= lerp(_GlobalMainLightURPShadowAsDirectResultTintColor,1, lerp(light.shadowAttenuation, lightingData.rawURPShadowAttenuation, _GlobalURPShadowAsDirectLightTintIgnoreMaterialURPUsageSetting));
#endif
    
    // apply all specular light to lit pass (ignore outline pass)
#if NiloToonForwardLitPass && _SPECULARHIGHLIGHTS
    half specularShadowMask = lerp(finalShadowArea,1,_SpecularShowInShadowArea);
    #if _RAMP_SPECULAR
    result = lerp(result,result * specularLitAdd, remappedSpecularRawArea * selfShadowMapShadow * lightingData.averageShadowAttenuation * specularShadowMask * surfaceData.specularMask);
    #else
    half3 specularAddResult = specularLitAdd * specularShadowMask;
    result += specularAddResult;
    surfaceData.alpha = saturate(surfaceData.alpha + Luminance(specularAddResult)); // semi-transparent's reflection should NOT be affected by alpha, so we add alpha on specular area to cancel it
    #endif 
#endif

#if NiloToonForwardLitPass || NiloToonSelfOutlinePass
    // (1):
    // min(2,light.color) to prevent over bright light.color
    // (2):
    // _ZWrite is * to _DepthTexRimLightUsage, 
    // because if _ZWrite is off (=0), this material should be a transparent material,
    // and transparent material wont write to _CameraDepthTexture,
    // which makes depth texture rim light not correct,
    // so disable depth texture rim light automatically if _Zwrite is off 
    half3 rimLightColor = min(2,light.color) * _DepthTexRimLightTintColor * lerp(1,surfaceData.albedo,_DepthTexRimLightMixWithBaseMapColor);
    rimLightColor = (rimLightColor / 6.0);

    rimLightColor *= _DepthTexRimLightIntensity;

    half3 RimLightMultiplier = _GlobalRimLightMultiplier;
    #if NiloToonSelfOutlinePass
    {
        RimLightMultiplier = _GlobalRimLightMultiplierForOutlineArea;
    }
    #endif
    rimLightColor *= RimLightMultiplier;

    // rimAttenuation will affect additional light additive rim 
    rimAttenuation *= _DepthTexRimLightUsage * _ZWrite;
    #if _DEPTHTEX_RIMLIGHT_OPACITY_MASKMAP
    half mask = dot(tex2D(_DepthTexRimLightMaskTex, lightingData.uv), _DepthTexRimLightMaskTexChannelMask);
    mask = _DepthTexRimLightMaskTexInvertColor ? 1-mask : mask;
    rimAttenuation *= mask;
    #endif
    rimAttenuation *= inShadow25PercentMul; // if in shadow area, reduce rim light
    rimAttenuation *= lerp(1,finalShadowArea,_DepthTexRimLightBlockByShadow);
    
    result += rimAttenuation * rimLightColor;
#endif

#if NiloToonForwardLitPass && _ISFACE && _FACE_3D_RIMLIGHT_AND_SHADOW
    result += face3DRimAddColor * finalShadowArea; // face 3D rim block by shadow
    // TODO: should we add to rimAttenuation?
#endif

#if NiloToonForwardLitPass && _KAJIYAKAY_SPECULAR
    result += kajiyaSpecularAdd;
#endif

    return half4(lerp(result,surfaceData.albedo,_AsUnlit),rimAttenuation);
}

half3 CalculateAdditiveSingleAdditionalLight(Light light, ToonLightingData lightingData)
{
    half NoL = dot(lightingData.normalWS,light.direction);

    // remapped N dot L
    // simplest 1 line cel shade
    // celShadeResult: 0 is in shadow, 1 is in light
    half smoothstepNoL = smoothstep(_AdditionalLightCelShadeMidPoint-_AdditionalLightCelShadeSoftness,_AdditionalLightCelShadeMidPoint+_AdditionalLightCelShadeSoftness, NoL);

    // if you don't want direct lighting's NdotL cel shade effect looks too strong, set _AdditionalLightIgnoreCelShade to a higher value
    half lightAttenuation = lerp(smoothstepNoL,1, _AdditionalLightIgnoreCelShade);

#if _ISFACE
    // [calculate another set of selfLightAttenuation for face area]
    half celShadeResultForFaceArea = smoothstep(_AdditionalLightCelShadeMidPointForFaceArea-_AdditionalLightCelShadeSoftnessForFaceArea,_AdditionalLightCelShadeMidPointForFaceArea+_AdditionalLightCelShadeSoftnessForFaceArea, NoL);
    half lightAttenuationForFaceArea = lerp(celShadeResultForFaceArea,1, _AdditionalLightIgnoreCelShadeForFaceArea);
    // [if current fragment is face area, replace original selfLightAttenuation by face's selfLightAttenuation result]
    lightAttenuation = lerp(lightAttenuation, lightAttenuationForFaceArea, _OverrideAdditionalLightCelShadeParamForFaceArea * lightingData.isFaceArea);
#endif

    // distance and shadow
    // distance attenuation clamp to prevent over bright if light is too close to vertex
    half receiveShadowMappingAmount = lerp(_ReceiveURPAdditionalLightShadowMappingAmountForNonFace, _ReceiveURPAdditionalLightShadowMappingAmountForFace,lightingData.isFaceArea) * _ReceiveURPAdditionalLightShadowMappingAmount * _ReceiveURPAdditionalLightShadowMapping;
    half shadowAttenuation = lerp(1,light.shadowAttenuation,receiveShadowMappingAmount);
    lightAttenuation *= min(_AdditionalLightDistanceAttenuationClamp,light.distanceAttenuation) * shadowAttenuation;

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Cinematic rim mask 3D
    //////////////////////////////////////////////////////////////////////////////////////////////////
    half3 L = light.direction;
    half3 V = lightingData.viewDirectionWS;
    half3 N = lightingData.normalWS;
    half3 H = normalize(L+V);
    half NdotL = saturate(NoL);
    half NdotV = saturate(dot(N,V));
    half fresnelTerm = 1.0-NdotV;

    // LdotV = is light infront of the character? (is light comes from camera's direction?)
    half LdotV = dot(L,V); 

    // ------------------------------------------------
    // [dynamic style]
    if(_GlobalCinematic3DRimMaskStrength_DynamicStyle > 0)
    {
        // forcing the light comes from the "side direction" of the character
        // (make V shorter than unit vector to avoid 0 length result vector)
        // (No need to saturate() LdotV, since we want to edit L when light is behind the characters also)
        half3 editedL = normalize(L+(-V * 0.999 * LdotV)); // can remove LdotV to produce "less bright+more stable" rim light 
        // perform dot(N,L), where L is always at the "side" of character
        half editedNdotL = saturate(dot(N,editedL));
        half editedRim = pow(editedNdotL,_GlobalCinematic3DRimMaskSharpness_DynamicStyle);
        lightAttenuation *= lerp(1,editedRim * 5.0,_GlobalCinematic3DRimMaskStrength_DynamicStyle);
    }
    // ------------------------------------------------
    // [stable style]
    // similar to PBR's DFG's Fresnel term ^2
    if(_GlobalCinematic3DRimMaskStrength_StableStyle > 0)
    {
        half F = pow(saturate(1.0-dot(V,H)),_GlobalCinematic3DRimMaskSharpness_StableStyle); // sharpness = default 10
        half width = 0.02;
        half midPoint = 0.02;
        //lightAttenuation *= lerp(1,smoothstep(midPoint-width,midPoint+width,F * NdotL)* (5.0 * 5.0), _GlobalCinematic3DRimMaskStrength_StableStyle); // can replace NdotL with editedNdotL
        lightAttenuation *= lerp(1,F * NdotL * (5.0 * 5.0 * 5.0), _GlobalCinematic3DRimMaskStrength_StableStyle); // can replace NdotL with editedNdotL
    }
    // ------------------------------------------------
    // [classic rim]
    if(_GlobalCinematic3DRimMaskStrength_ClassicStyle > 0)
    {
        half width = 0.02;
        half midPoint = 0.7;
        lightAttenuation *= lerp(1,smoothstep(midPoint-width,midPoint+width, sqrt(NdotL) * fresnelTerm),_GlobalCinematic3DRimMaskStrength_ClassicStyle);
    } 
    // ------------------------------------------------

    // finish all float multiply first, then do float3 multiply. (performance)
    return light.color * lightAttenuation;  
}

half3 ShadeEmission(ToonSurfaceData surfaceData, ToonLightingData lightingData, Light mainLight)
{
    half3 emissionResult = surfaceData.emission;

    emissionResult *= lerp(1,saturate(mainLight.color * mainLight.shadowAttenuation),_MultiplyLightColorToEmissionColor); // let user optionally mix light color to emission color

    return emissionResult;
}

half3 CompositeAllLightResults(half3 mainLightResult, half3 additionalLightSumResult, half3 emissionResult)
{
    // [simply add them all]
    half3 lightResult = mainLightResult;
    
#if NeedCalculateAdditionalLight
    lightResult += additionalLightSumResult;
#endif

#if _EMISSION
    lightResult += emissionResult;
#endif

    return lightResult;
}
