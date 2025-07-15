// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// This shader is a direct copy of URP16.0.3's LitForwardPass.hlsl, but with some edit.
// If you want to see what is the difference, all edited lines will have a [NiloToon] tag, you can search [NiloToon] in this file,
// or compare URP16.0.3's LitForwardPass.hlsl with this file using tools like SourceGear DiffMerge.

// #pragma once is a safeguard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if UNITY_VERSION >= 202220
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif
#endif

// [NiloToon] add:
//==========================================================================================================
// + Rider code highlight support for all shader_feature and multi_compile
// (__RESHARPER__ is only defined while in IDE. Used to help editing this file with proper highlighting.)
#ifdef __RESHARPER__
#define _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE 1
#define _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2 1
#define _SPLATMAP 1
#endif

#include "../../ShaderLibrary/NiloUtilityHLSL/NiloAllUtilIncludes.hlsl"

#if defined(_SPLATMAP)
    #if !defined(_NORMALMAP)
        #define _NORMALMAP
    #endif
#endif
//==========================================================================================================

#if defined(_PARALLAXMAP)
#define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR
#endif

#if (defined(_NORMALMAP) || (defined(_PARALLAXMAP) && !defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR))) || defined(_DETAIL)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

// keep this file in sync with LitGBufferPass.hlsl

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 staticLightmapUV   : TEXCOORD1;
    float2 dynamicLightmapUV  : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS               : TEXCOORD1;
#endif

    float3 normalWS                 : TEXCOORD2;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    half4 tangentWS                : TEXCOORD3;    // xyz: tangent, w: sign
#endif

#if UNITY_VERSION < 202220
    float3 viewDirWS                : TEXCOORD4;
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light
#else
    half  fogFactor                 : TEXCOORD5;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD6;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS                : TEXCOORD7;
#endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
#ifdef DYNAMICLIGHTMAP_ON
    float2  dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
#endif

    // [NiloToon] add:
    //=======================================================
    float4 screenPos                : TEXCOORD10;
    //=======================================================

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(_NORMALMAP) || defined(_DETAIL)
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

    #if defined(_NORMALMAP)
    inputData.tangentToWorld = tangentToWorld;
    #endif
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
#endif

#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.positionCS.xy);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.vertexSH;
    #endif
    #endif
}

// [NiloToon] add:
//==========================================================================================================================================================
// debug on off
float _NiloToonGlobalEnviMinimumShader;

// shadow boader color
float4 _NiloToonGlobalEnviShadowBorderTintColor;

// global GI edit
float3 _NiloToonGlobalEnviGITintColor;
float3 _NiloToonGlobalEnviGIAddColor;

// global GI override
float4 _NiloToonGlobalEnviGIOverride;

// global albedo override
float4 _NiloToonGlobalEnviAlbedoOverrideColor;

// global surface color result override
float4 _NiloToonGlobalEnviSurfaceColorResultOverrideColor;

// global screen space outline settings
float _GlobalScreenSpaceOutlineIntensityForEnvi;
float _GlobalScreenSpaceOutlineWidthMultiplierForEnvi;
float _GlobalScreenSpaceOutlineNormalsSensitivityOffsetForEnvi;
float _GlobalScreenSpaceOutlineDepthSensitivityOffsetForEnvi;
float _GlobalScreenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForEnvi;
half3 _GlobalScreenSpaceOutlineTintColorForEnvi;

// global screen space outline V2 settings
float _GlobalScreenSpaceOutlineV2IntensityForEnvi;
float _GlobalScreenSpaceOutlineV2WidthMultiplierForEnvi;
float _GlobalScreenSpaceOutlineV2EnableGeometryEdgeForEnvi;
float _GlobalScreenSpaceOutlineV2GeometryEdgeThresholdForEnvi;
float _GlobalScreenSpaceOutlineV2EnableNormalAngleEdgeForEnvi;
float _GlobalScreenSpaceOutlineV2NormalAngleCosThetaThresholdForEnvi;

// global camera uniforms
float _CurrentCameraFOV;

// include files, include as late as possible to get all defines
#include "NiloToonEnvironment_ExtendFunctionsForUserCustomLogic.hlsl"
//==========================================================================================================================================================

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // [NiloToon] add:
    //==========================================================================================================================================================
    ApplyCustomUserLogicToVertexAttributeAtVertexShaderStart(input);
    //==========================================================================================================================================================
    
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

    half fogFactor = 0;
    #if !defined(_FOG_FRAGMENT)
        fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    #endif

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    output.tangentWS = tangentWS;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
#endif

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

    #if UNITY_VERSION >= 60000011
    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);
    #elif UNITY_VERSION >= 202310
    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH);
    #else
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
    #endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
    output.fogFactor = fogFactor;
#endif

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = vertexInput.positionWS;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;

    // [NiloToon] add:
    //==========================================================================================================================================================
    output.screenPos = ComputeScreenPos(output.positionCS);

    ApplyCustomUserLogicToVertexShaderOutputAtVertexShaderEnd(output, input);
    //==========================================================================================================================================================
    return output;
}

// [NiloToon] add:
//==========================================================================================================================================================
half4 CalculateFinalSplatBlendFactor(half blendSoftness, half depth1, half depth2, half depth3, half depth4, half4 splatControlMapRGBA)
{
    half4 blend;
    
    blend.r = depth1 * splatControlMapRGBA.r;
    blend.g = depth2 * splatControlMapRGBA.g;
    blend.b = depth3 * splatControlMapRGBA.b;
    blend.a = depth4 * splatControlMapRGBA.a;
    
    half ma = max(blend.r, max(blend.g, max(blend.b, blend.a)));
    blend = max(blend - ma + max(0.001,blendSoftness), 0) * splatControlMapRGBA; // 0.001 is safer than 0.00001 when testing with different textures
    return blend/(blend.r + blend.g + blend.b + blend.a);
}
void ApplySplatMapToSurfaceData(inout SurfaceData surfaceData, Varyings input)
{
    // for reference see URP's TerrainLitPasses.hlsl -> SplatmapMix()
#if defined(_SPLATMAP)

    // uv
    float2 uvR = input.uv * _SplatAlbedoMapRTiling;
    float2 uvG = input.uv * _SplatAlbedoMapGTiling;
    float2 uvB = input.uv * _SplatAlbedoMapBTiling;
    float2 uvA = input.uv * _SplatAlbedoMapATiling;

    // sample packed data map (not yet apply mask)
    half4 packedDataR = SAMPLE_TEXTURE2D(_SplatPackedDataMapR, sampler_SplatPackedDataMap_linear_repeat, uvR);
    half4 packedDataG = SAMPLE_TEXTURE2D(_SplatPackedDataMapG, sampler_SplatPackedDataMap_linear_repeat, uvG);
    half4 packedDataB = SAMPLE_TEXTURE2D(_SplatPackedDataMapB, sampler_SplatPackedDataMap_linear_repeat, uvB);
    half4 packedDataA = SAMPLE_TEXTURE2D(_SplatPackedDataMapA, sampler_SplatPackedDataMap_linear_repeat, uvA);

    // get height from each type of material
    half heightR = packedDataR.b * _SplatHeightMultiplierR;
    half heightG = packedDataG.b * _SplatHeightMultiplierG;
    half heightB = packedDataB.b * _SplatHeightMultiplierB;
    half heightA = packedDataA.b * _SplatHeightMultiplierA;

    // sample and remap mask (using height from PackedDataMap)
    half4 mask = SAMPLE_TEXTURE2D(_SplatMaskMap, sampler_SplatMaskMap, input.uv);
    mask = CalculateFinalSplatBlendFactor(_SplatMaskBlendingSoftness,heightR,heightG,heightB,heightA,mask);

    // sample albedo + smoothness
    half4 albedoSmoothnessR = SAMPLE_TEXTURE2D(_SplatAlbedoMapR, sampler_SplatAlbedoMap_linear_repeat, uvR) * mask.r;
    half4 albedoSmoothnessG = SAMPLE_TEXTURE2D(_SplatAlbedoMapG, sampler_SplatAlbedoMap_linear_repeat, uvG) * mask.g;
    half4 albedoSmoothnessB = SAMPLE_TEXTURE2D(_SplatAlbedoMapB, sampler_SplatAlbedoMap_linear_repeat, uvB) * mask.b;
    half4 albedoSmoothnessA = SAMPLE_TEXTURE2D(_SplatAlbedoMapA, sampler_SplatAlbedoMap_linear_repeat, uvA) * mask.a;

    // apply albedo
    half3 albedoR = albedoSmoothnessR.rgb * _SplatAlbedoMapRTintColor;
    half3 albedoG = albedoSmoothnessG.rgb * _SplatAlbedoMapGTintColor;
    half3 albedoB = albedoSmoothnessB.rgb * _SplatAlbedoMapBTintColor;
    half3 albedoA = albedoSmoothnessA.rgb * _SplatAlbedoMapATintColor;
    surfaceData.albedo = lerp(surfaceData.albedo, albedoR + albedoG + albedoB + albedoA, _SplatMapFeatureOnOff);

    /*
    // apply metallic
    half metallicR = lerp(1,packedDataR.r,_SplatOverrideMetallicR) * mask.r;
    half metallicG = lerp(1,packedDataG.r,_SplatOverrideMetallicG) * mask.g;
    half metallicB = lerp(1,packedDataB.r,_SplatOverrideMetallicB) * mask.b;
    half metallicA = lerp(1,packedDataA.r,_SplatOverrideMetallicA) * mask.a;
    surfaceData.metallic = lerp(surfaceData.metallic, metallicR + metallicG + metallicB + metallicA, _SplatMapFeatureOnOff);

    // apply occlusion
    half occlusionR = packedDataR.g * mask.r;
    half occlusionG = packedDataG.g * mask.g;
    half occlusionB = packedDataB.g * mask.b;
    half occlusionA = packedDataA.g * mask.a;
    surfaceData.occlusion = lerp(surfaceData.occlusion, occlusionR + occlusionG + occlusionB + occlusionA, _SplatMapFeatureOnOff);
    */

    // apply smoothness
    half smoothnessR = saturate(albedoSmoothnessR.a * _SplatSmoothnessMultiplierR);
    half smoothnessG = saturate(albedoSmoothnessG.a * _SplatSmoothnessMultiplierG);
    half smoothnessB = saturate(albedoSmoothnessB.a * _SplatSmoothnessMultiplierB);
    half smoothnessA = saturate(albedoSmoothnessA.a * _SplatSmoothnessMultiplierA);
    surfaceData.smoothness = lerp(surfaceData.smoothness, smoothnessR + smoothnessG + smoothnessB + smoothnessA, _SplatMapFeatureOnOff);

    // sample and apply normal
    half3 normalTSR = UnpackNormalScale(SAMPLE_TEXTURE2D(_SplatNormalMapR, sampler_SplatNormalMap_linear_repeat, uvR),_SplatNormalMapIntensityR) * mask.r;
    half3 normalTSG = UnpackNormalScale(SAMPLE_TEXTURE2D(_SplatNormalMapG, sampler_SplatNormalMap_linear_repeat, uvG),_SplatNormalMapIntensityG) * mask.g;
    half3 normalTSB = UnpackNormalScale(SAMPLE_TEXTURE2D(_SplatNormalMapB, sampler_SplatNormalMap_linear_repeat, uvB),_SplatNormalMapIntensityB) * mask.b;
    half3 normalTSA = UnpackNormalScale(SAMPLE_TEXTURE2D(_SplatNormalMapA, sampler_SplatNormalMap_linear_repeat, uvA),_SplatNormalMapIntensityA) * mask.a;
    surfaceData.normalTS = lerp(surfaceData.normalTS, (normalTSR + normalTSG + normalTSB + normalTSA), _SplatMapFeatureOnOff);
#endif
}
//==========================================================================================================================================================

// Used in Standard (Physically Based) shader
void LitPassFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(_PARALLAXMAP)
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
#else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
#endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
#endif

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

#if UNITY_VERSION >= 202220
#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif
#endif

     //[NiloToon] add:
    //==========================================================================================================================================================
    // albedo,normal,smoothness override by splat map
    ApplySplatMapToSurfaceData(surfaceData, input);

    ApplyCustomUserLogicToSurfaceData(surfaceData, input);
    
    // global albedo override
    surfaceData.albedo.rgb = lerp(surfaceData.albedo.rgb, _NiloToonGlobalEnviAlbedoOverrideColor.rgb, _NiloToonGlobalEnviAlbedoOverrideColor.a);    
    //==========================================================================================================================================================
    
    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

#if UNITY_VERSION >= 600000
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv);
#else
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);
#endif

#ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    //[NiloToon] add:
    //==========================================================================================================================================================
    // debug on off
    if(_NiloToonGlobalEnviMinimumShader)
    {
        Light mainLight = GetMainLight();
        outColor = half4(saturate(dot(mainLight.direction, input.normalWS)) * mainLight.color * surfaceData.albedo,1);
        return;
    }

    // GI edit and override
    inputData.bakedGI = inputData.bakedGI * _NiloToonGlobalEnviGITintColor + _NiloToonGlobalEnviGIAddColor;
    inputData.bakedGI = lerp(inputData.bakedGI, _NiloToonGlobalEnviGIOverride.rgb, _NiloToonGlobalEnviGIOverride.a);
    //==========================================================================================================================================================

    half4 color = UniversalFragmentPBR(inputData, surfaceData);

    //[NiloToon] add:
    //==========================================================================================================================================================
    // copy from URP10.4's Lighting.hlsl->UniversalFragmentPBR()
    // To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
    half4 shadowMask = inputData.shadowMask;
#elif !defined (LIGHTMAP_ON)
    half4 shadowMask = unity_ProbesOcclusion;
#else
    half4 shadowMask = half4(1, 1, 1, 1);
#endif

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);

    // shadow border tint color
    float isShadowEdge = 1-abs(mainLight.shadowAttenuation-0.5)*2;
    color.rgb = lerp(color.rgb,color.rgb * _NiloToonGlobalEnviShadowBorderTintColor.rgb, isShadowEdge * _NiloToonGlobalEnviShadowBorderTintColor.a);

    // global surface color result override
    color.rgb = lerp(color.rgb,_NiloToonGlobalEnviSurfaceColorResultOverrideColor.rgb, _NiloToonGlobalEnviSurfaceColorResultOverrideColor.a);

    float2 SV_POSITIONxy = input.positionCS.xy;
    
    // screen space outline
#if _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE || _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2
    // we receive screen space outline only when material's SurfaceType is Opaque. 
    // (_Surface == 0 when SurfaceType is Opaque)
    // (_Surface == 1 when SurfaceType is Transparent)
    // *see BaseShaderGUI.cs in URP package
    if(_Surface == 0) 
    {
        #if _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE
        {
            float finalOutlineWidth = _ScreenSpaceOutlineWidth * _GlobalScreenSpaceOutlineWidthMultiplierForEnvi;
            float finalNormalsSensitivity = max(0,1 + _GlobalScreenSpaceOutlineNormalsSensitivityOffsetForEnvi); // max(0,x) to prevent negative sensitivity
            float finalDepthSensitivity = max(0,1 + _GlobalScreenSpaceOutlineDepthSensitivityOffsetForEnvi); // max(0,x) to prevent negative
            float selfLinearDepth = abs(mul(UNITY_MATRIX_V,float4(input.positionWS,1)).z);

            // reduce finalDepthSensitivity according to depth

            finalDepthSensitivity *= 0.35; // make GUI's default value is 1

            float isScreenSpaceOutlineArea = IsScreenSpaceOutline(
                SV_POSITIONxy, 
                finalOutlineWidth, 
                finalDepthSensitivity, 
                finalNormalsSensitivity, 
                selfLinearDepth, 
                _GlobalScreenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForEnvi, 
                _CurrentCameraFOV,
                inputData.viewDirectionWS);

            isScreenSpaceOutlineArea *= _GlobalScreenSpaceOutlineIntensityForEnvi * _ScreenSpaceOutlineIntensity;
            color.rgb = lerp(color.rgb, color.rgb * _GlobalScreenSpaceOutlineTintColorForEnvi * _ScreenSpaceOutlineColor.rgb, isScreenSpaceOutlineArea);
        }
        #endif

        //---------------------------------------------------------------

        #if _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2
        {
            // TODO: receive from renderer feature V2
            float width = _GlobalScreenSpaceOutlineV2WidthMultiplierForEnvi;
            float GeometryEdgeThreshold = _GlobalScreenSpaceOutlineV2GeometryEdgeThresholdForEnvi;
            float NormalAngleCosThetaThresholdMin = _GlobalScreenSpaceOutlineV2NormalAngleCosThetaThresholdForEnvi;
            float NormalAngleCosThetaThresholdMax = cos(DegToRad(180));
            bool DrawGeometryEdge = _GlobalScreenSpaceOutlineV2EnableGeometryEdgeForEnvi;
            bool DrawNormalAngleEdge = _GlobalScreenSpaceOutlineV2EnableNormalAngleEdgeForEnvi;
            bool DrawMaterialIDBoundary = false; // not supported for envi
            bool DrawCustomIDBoundary = false; // not supported for envi
            bool DrawWireframe = false;

            float isScreenSpaceOutlineArea = IsScreenSpaceOutlineV2(
                SV_POSITIONxy, 
                width, 
                GeometryEdgeThreshold, 
                NormalAngleCosThetaThresholdMin, 
                NormalAngleCosThetaThresholdMax,
                DrawGeometryEdge,
                DrawNormalAngleEdge,
                DrawMaterialIDBoundary, 
                DrawCustomIDBoundary,
                DrawWireframe,
                input.positionWS);

            color.rgb *= 1-isScreenSpaceOutlineArea * _GlobalScreenSpaceOutlineV2IntensityForEnvi;
        }
        #endif
    }
#endif

    ApplyCustomUserLogicBeforeFog(color, surfaceData, inputData);
    //==========================================================================================================================================================

    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    //[NiloToon] add:
    //==========================================================================================================================================================
    ApplyCustomUserLogicAfterFog(color, surfaceData, inputData);
    //==========================================================================================================================================================
    
#if UNITY_VERSION >= 202220
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));
#else
    color.a = OutputAlpha(color.a, _Surface);
#endif

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}

