// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safeguard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

#include "NiloToonCharacter_RiderSupport.hlsl"

// note:
// suffix OS means object space     (e.g. positionOS = position object space)
// suffix WS means world space      (e.g. positionWS = position world space)
// suffix VS means view space       (e.g. positionVS = position view space)
// suffix CS means clip space       (e.g. positionCS = position clip space)

// just helper defines to help us write less #if define code
// we always write #define XXX 1, instead of just #define XXX, so we can use both #if or #ifdef in shader code without problem
#if NiloToonSelfOutlinePass || NiloToonExtraThickOutlinePass || NiloToonCharacterAreaStencilBufferFillPass || NiloToonDepthOnlyOrDepthNormalPass || NiloToonPrepassBufferPass
    #define NiloToonIsAnyOutlinePass 1
#endif
#if NiloToonForwardLitPass || NiloToonSelfOutlinePass
    #define NiloToonIsAnyLitColorPass 1
#endif
#if NiloToonIsAnyLitColorPass || NiloToonDepthOnlyOrDepthNormalPass || NiloToonExtraThickOutlinePass || NiloToonPrepassBufferPass
    #define ApplyZOffset 1 // foreach pass that will edit "ZOffsetFinalSum", will be included in "ApplyZOffset"
#endif
#if NiloToonIsAnyLitColorPass || NiloToonDepthOnlyOrDepthNormalPass || NiloToonExtraThickOutlinePass || NiloToonPrepassBufferPass || NiloToonCharacterAreaStencilBufferFillPass || NiloToonCharacterAreaColorFillPass
    #define ApplyPerspectiveRemoval 1
#endif

// because _DETAIL always sample detail normal map
#if _NORMALMAP || _DETAIL || _KAJIYAKAY_SPECULAR || _DYNAMIC_EYE || _PARALLAXMAP
    #define VaryingsHasTangentWS 1
#endif
#if _PARALLAXMAP
    #define VaryingsHasViewDirTS 1
#endif

#if _ISFACE && _FACE_MASK_ON
    #define NeedFaceMaskArea 1
#endif

#if _OCCLUSIONMAP || _MATCAP_OCCLUSION
    #define AnyOcclusionEnabled 1
#endif

#if _BASEMAP_STACKING_LAYER1 || _BASEMAP_STACKING_LAYER2 || _BASEMAP_STACKING_LAYER3 || _BASEMAP_STACKING_LAYER4 || _BASEMAP_STACKING_LAYER5 || _BASEMAP_STACKING_LAYER6 || _BASEMAP_STACKING_LAYER7 || _BASEMAP_STACKING_LAYER8 || _BASEMAP_STACKING_LAYER9 || _BASEMAP_STACKING_LAYER10
    #define AnyBaseMapStackingLayerEnabled 1
#endif

#if _NILOTOON_RECEIVE_URP_SHADOWMAPPING && _RECEIVE_URP_SHADOW
    #define ShouldReceiveURPShadow 1
#endif

// We don't have "UnityCG.cginc" in SRP/URP's package anymore, so:
// Including the following two hlsl files is enough for shading with Universal Pipeline. Everything is included in them.
// - Core.hlsl will include SRP shader library, all constant buffers not related to materials (perobject, percamera, perframe).
//   It also includes matrix/space conversion functions and fog.
// - Lighting.hlsl will include the light functions/data to abstract light constants. You should use GetMainLight and GetLight functions
//   that initialize Light struct. Lighting.hlsl also include GI, Light BDRF functions. It also includes Shadows.

// Required by all Universal Render Pipeline shaders.
// It will include Unity built-in shader variables (except the lighting variables)
// (https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html)
// It will also include many utilitary functions. 
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Include this if you are doing a lit shader. This includes lighting shader variables,
// lighting and shadow functions
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"

// include a bundle of small utility .hlsl files to help us write less code
#include "../../ShaderLibrary/NiloUtilityHLSL/NiloAllUtilIncludes.hlsl"

// we need to define this AFTER including Lighting.hlsl
#if _ADDITIONAL_LIGHTS || _ADDITIONAL_LIGHTS_VERTEX
    #define NeedCalculateAdditionalLight 1
#endif

// all pass will share this Attributes struct (define data needed from Unity app to our vertex shader)
// TODO: optimize struct size for each pass. There maybe some pass that don't need some attributes
struct Attributes
{
    float3 positionOS   : POSITION;     // vertex position in object space
    float3 normalOS     : NORMAL;       // GetVertexNormalInputs(...) expect float3 normalOS input, don't write half3 to produce unneeded type conversion cost
    float4 tangentOS    : TANGENT;      // GetVertexNormalInputs(...) expect float4 tangentOS input, don't write half4 to produce unneeded type conversion cost
    float2 uv           : TEXCOORD0;    // the first uv (UV#0)
    float2 uv2          : TEXCOORD1;
    float2 uv3          : TEXCOORD2;
    float2 uv4          : TEXCOORD3;
    float3 uv8          : TEXCOORD7;    // generated by NiloToon's editor script, store angle smoothed tangent space normal in mesh uv8((TEXCOORD7)), for improving outline and shadowmap normal bias
    float4 color        : COLOR;        // vertex color, used for outline width mask, not as a color, so use float not half
    uint vertexID       : SV_VertexID;  // SV_VertexID needs to be uint (https://docs.unity3d.com/Manual/SL-ShaderSemantics.html)

    // to support GPU instancing and Single Pass Stereo rendering(VR), add the following section
    //------------------------------------------------------------------------------------------------------------------------------
    UNITY_VERTEX_INPUT_INSTANCE_ID      // For non PSSL, equals to -> uint instanceID : SV_InstanceID;
    //------------------------------------------------------------------------------------------------------------------------------ 
};

// all pass will share this Varyings struct (define data needed from vertex shader to fragment shader)
// Note: once a field is written here, no matter fragment shader use it or not
// you will pay for the rasterization interpolation cost, 
// compiler can't help you in this case, you can check Unity's compiled shader code to confirm.
// so using #if to remove a field in this "v2f" Varyings struct is a meaningful optimization here
// Also, try to pack data into a single vec4 TEXCOORD
// "Pack values together as all varyings have four components, whether they are used or not. Putting two vec2 texture coordinates into a single vec4 value is a common practice"
// see https://developer.qualcomm.com/sites/default/files/docs/adreno-gpu/snapdragon-game-toolkit/gdg/gpu/best_practices_shaders.html#pack-shader-interpolators
struct Varyings
{
    float4 positionCS                           : SV_POSITION;

    float4 uv01                                 : TEXCOORD0;    // need float for uv (half is not enough), if texture size is larger than 2048, half is not enough (https://forum.unity.com/threads/why-does-unity-recommend-us-to-use-float-type-for-texture-coordinates-in-the-shader.732920/#post-4894619)
    float4 uv23                                 : TEXCOORD1;

    float4 positionWS_ZOffsetFinalSum           : TEXCOORD2;    // xyz = positionWS, w = ZOffsetFinalSum

    half4 SH_fogFactor                          : TEXCOORD3;    // (4 half pack into 1 TEXCOORD)  xyz: SampleSH(normalWS) * multipliers, w: fog factor
    float4 normalWS_averageShadowAttenuation    : TEXCOORD4;    // (4 float pack into 1 TEXCOORD)  xyz: normalWS, w: averageShadowAttenuation. URP14 LitForwardPass.hlsl define normalWS as float3 instead of half3, so we follow it.

    float3 smoothedNormalWS                     : TEXCOORD5;
    
#if VaryingsHasViewDirTS
    half3 viewDirTS                             : TEXCOORD6;     // URP14 LitForwardPass.hlsl's viewDirTS is half3, we follow it.
#endif

#if VaryingsHasTangentWS
    half4 tangentWS                             : TANGENT;      // xyz: tangent, w: sign. (TODO: is TANGENT safe to use? In URP's LitForwardPass.hlsl, it is TEXCOORD, not TANGENT.)
#endif

    half4 color                                 : COLOR;        // vertex color rgba, currently only for "control depth texture rimlight and shadow width by vertex color" or "debug shading"
   
    // Note: unlike URP's LitForwardPass.hlsl, NiloToon didn't have
    // float4 shadowCoord
    // shadowCoord in NiloToon is always calculated in fragment shader 

    // debug use
    //--------------------------------------------------------------------
#if _NILOTOON_DEBUG_SHADING
    float3 uv8                                  : TEXCOORD7;
#endif
    //--------------------------------------------------------------------

    // to support GPU instancing and Single Pass Stereo rendering(VR), add the following section
    //------------------------------------------------------------------------------------------------------------------------------
    // see ShadowCasterPass.hlsl for why these lines are removed for NiloToonCharSelfShadowCasterPass
#if !NiloToonCharSelfShadowCasterPass
    UNITY_VERTEX_INPUT_INSTANCE_ID  // For non PSSL, equals to -> uint instanceID : SV_InstanceID;
    UNITY_VERTEX_OUTPUT_STEREO      // For non OpenGL and non PSSL, equals to -> uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex; (when UNITY_STEREO_INSTANCING_ENABLED)
#endif
    //------------------------------------------------------------------------------------------------------------------------------
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// local(per material) samplers and textures
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// note(1):
// all sampler2D, SAMPLER and TEXTURE2D do not need to put inside UnityPerMaterial CBUFFER{}

// note(2): 
// > how to avoid [sampler max count = 16] limit(error message: Shader error in 'X': maximum ps_5_0 sampler register index (16) exceeded)?
// Everytime you write a sampler2D/SAMPLER, the sampler count for that shader will +1, when the sampler count reach the hardware limit, 
// the shader will not be able to compile.
// Usually the sampler count limit is 16 for PC, 8(can be more for some devices?) for mobile.  
// The solution to avoid sampler max count limit = reuse SAMPLER in shader, don't declare new SAMPLER/sampler2D for each texture if SAMPLER setting can be reused.
// But a disadvantage of reusing sampler is, reusing sampler will prevent user to change sampler setting from each texture's texture inspector
// Useful resources:
// - https://docs.unity3d.com/Manual/SL-SamplerStates.html
// - URP's TerrainLitPasses.hlsl -> SplatmapMix(...), you will see 4 different splat textures were sampled by 1 sampler(sampler reused)
// - https://forum.unity.com/threads/texture-samplers-limit-per-pass.605395/
// - https://forum.unity.com/threads/sampler-count-limit-for-pixel-shader-in-shader-model-5-0.452957/

// ref: https://docs.unity3d.com/Manual/SL-SamplerStates.html 
// "Just like separate texture + sampler syntax, inline sampler states are not supported on some platforms. 
// Currently they are implemented on Direct3D 11/12, PS4, XboxOne and Metal."
// *Note:
// We tested the above statement on some android mobile devices (GLES / Vulkan), 
// we found that inline sampler is OK to use even GLES/Vulkan are not listed in supported platforms, 
// so we will use inline sampler to help reducing sampler count.

// [sampler_linear_clamp, this sampler will be shared by some texture read if sampler state is linear & clamp]
// (see: https://docs.unity3d.com/Manual/SL-SamplerStates.html)
// use cases:
// - force linear clamp sampler to prevent ramp lighting / ramp specular sampling out of bound pixels of ramp texture using repeat wrap mode
SAMPLER(sampler_linear_clamp); 

// [sampler_linear_repeat, this sampler will be shared by some texture read if sampler state is linear & repeat]
// (see: https://docs.unity3d.com/Manual/SL-SamplerStates.html)
// use cases:
// - force half of the base stacking layer sharing sampler_linear_repeat to prevent [sampler max count = 16] limit
SAMPLER(sampler_linear_repeat);

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);

#if _ALPHAOVERRIDEMAP
    sampler2D _AlphaOverrideTex;
#endif

#if _PARALLAXMAP
    TEXTURE2D(_ParallaxMap);        SAMPLER(sampler_ParallaxMap);   
#endif

#if _BASEMAP_STACKING_LAYER1
    // only the first 2 layers use it's own sampler, to allow more control for user  
    TEXTURE2D(_BaseMapStackingLayer1Tex); SAMPLER(sampler_BaseMapStackingLayer1Tex);
    TEXTURE2D(_BaseMapStackingLayer1MaskTex);
#endif
#if _BASEMAP_STACKING_LAYER2
    // only the first 2 layers use it's own sampler, to allow more control for user   
    TEXTURE2D(_BaseMapStackingLayer2Tex); SAMPLER(sampler_BaseMapStackingLayer2Tex);
    TEXTURE2D(_BaseMapStackingLayer2MaskTex);
#endif
#if _BASEMAP_STACKING_LAYER3
    TEXTURE2D(_BaseMapStackingLayer3Tex);
    TEXTURE2D(_BaseMapStackingLayer3MaskTex);
#endif
#if _BASEMAP_STACKING_LAYER4
    TEXTURE2D(_BaseMapStackingLayer4Tex);
    TEXTURE2D(_BaseMapStackingLayer4MaskTex);
#endif
#if _BASEMAP_STACKING_LAYER5
    TEXTURE2D(_BaseMapStackingLayer5Tex);
    TEXTURE2D(_BaseMapStackingLayer5MaskTex);
#endif
#if _BASEMAP_STACKING_LAYER6
    TEXTURE2D(_BaseMapStackingLayer6Tex);
    TEXTURE2D(_BaseMapStackingLayer6MaskTex);
#endif
#if _BASEMAP_STACKING_LAYER7
    TEXTURE2D(_BaseMapStackingLayer7Tex);
    TEXTURE2D(_BaseMapStackingLayer7MaskTex);
#endif
#if _BASEMAP_STACKING_LAYER8
    TEXTURE2D(_BaseMapStackingLayer8Tex);
    TEXTURE2D(_BaseMapStackingLayer8MaskTex);
#endif
#if _BASEMAP_STACKING_LAYER9
    TEXTURE2D(_BaseMapStackingLayer9Tex);
    TEXTURE2D(_BaseMapStackingLayer9MaskTex);
#endif
#if _BASEMAP_STACKING_LAYER10
    TEXTURE2D(_BaseMapStackingLayer10Tex);
    TEXTURE2D(_BaseMapStackingLayer10MaskTex);
#endif

#if _NORMALMAP
    sampler2D _BumpMap;
#endif
#if _EMISSION 
    sampler2D _EmissionMap;
    TEXTURE2D(_EmissionMaskMap);
#endif
#if _ENVIRONMENTREFLECTIONS
    TEXTURE2D(_EnvironmentReflectionMaskMap);
#endif
#if _MATCAP_BLEND
    sampler2D _MatCapAlphaBlendMap;
    TEXTURE2D(_MatCapAlphaBlendMaskMap);
#endif
#if _MATCAP_ADD
    sampler2D _MatCapAdditiveMap;
    TEXTURE2D(_MatCapAdditiveMaskMap);
#endif
#if _MATCAP_OCCLUSION
    sampler2D _MatCapOcclusionMap;
    TEXTURE2D(_MatCapOcclusionMaskMap);
#endif
#if _RAMP_LIGHTING
    TEXTURE2D(_DynamicRampLightingTex);
    TEXTURE2D(_RampLightingTex);            
#endif
#if _RAMP_LIGHTING_SAMPLE_UVY_TEX
    sampler2D _RampLightingSampleUvYTex;
#endif
#if _RAMP_SPECULAR
    TEXTURE2D(_RampSpecularTex);            
#endif
#if _RAMP_SPECULAR_SAMPLE_UVY_TEX
    sampler2D _RampSpecularSampleUvYTex;
#endif
#if _OCCLUSIONMAP
    sampler2D _OcclusionMap;
#endif
#if _SHADING_GRADEMAP
    sampler2D _ShadingGradeMap;
#endif
#if _SMOOTHNESSMAP
    sampler2D _SmoothnessMap;
#endif
#if _SPECULARHIGHLIGHTS
    sampler2D _SpecularMap;
    #if _SPECULARHIGHLIGHTS_TEX_TINT
    sampler2D _SpecularColorTintMap;
    #endif
#endif
#if _KAJIYAKAY_SPECULAR
    #if _KAJIYAKAY_SPECULAR_TEX_TINT
    sampler2D _HairStrandSpecularTintMap;
    #endif
#endif
#if _ZOFFSETMAP
    sampler2D _ZOffsetMaskTex;
#endif
#if _OUTLINEWIDTHMAP
    sampler2D _OutlineWidthTex;
#endif
#if _OUTLINEZOFFSETMAP
    sampler2D _OutlineZOffsetMaskTex;
#endif
#if NeedFaceMaskArea
    sampler2D _FaceMaskMap;
#endif
#if _SKIN_MASK_ON
    TEXTURE2D(_SkinMaskMap);
#endif
#if _DYNAMIC_EYE
    sampler2D _DynamicEyePupilMap;
    sampler2D _DynamicEyePupilMaskTex;
    sampler2D _DynamicEyeWhiteMap;
#endif
#if _DETAIL
    TEXTURE2D(_DetailMask);                 SAMPLER(sampler_DetailMask);
    TEXTURE2D(_DetailAlbedoMap);            SAMPLER(sampler_DetailAlbedoMap);
    TEXTURE2D(_DetailNormalMap);            SAMPLER(sampler_DetailNormalMap);
#endif
#if _OVERRIDE_SHADOWCOLOR_BY_TEXTURE
    sampler2D _OverrideShadowColorTex;
    sampler2D _OverrideShadowColorMaskMap;
#endif
#if _OVERRIDE_OUTLINECOLOR_BY_TEXTURE
    sampler2D _OverrideOutlineColorTex;
#endif
#if _SCREENSPACE_OUTLINE
    sampler2D _ScreenSpaceOutlineDepthSensitivityTex;
    sampler2D _ScreenSpaceOutlineNormalsSensitivityTex;
#endif
#if _DEPTHTEX_RIMLIGHT_SHADOW_WIDTHMAP
    sampler2D _DepthTexRimLightAndShadowWidthTex;
#endif
#if _DEPTHTEX_RIMLIGHT_OPACITY_MASKMAP
    sampler2D _DepthTexRimLightMaskTex;
#endif
#if _NILOTOON_SELFSHADOW_INTENSITY_MAP
    sampler2D _NiloToonSelfShadowIntensityMultiplierTex;
#endif
#if _FACE_SHADOW_GRADIENTMAP
    sampler2D _FaceShadowGradientMap;
    sampler2D _FaceShadowGradientMaskMap;
#endif
#if _FACE_3D_RIMLIGHT_AND_SHADOW
    sampler2D _Face3DRimLightAndShadow_NoseRimLightMaskMap;
    sampler2D _Face3DRimLightAndShadow_NoseShadowMaskMap;
    sampler2D _Face3DRimLightAndShadow_CheekRimLightMaskMap;
    sampler2D _Face3DRimLightAndShadow_CheekShadowMaskMap;
#endif
#if _NILOTOON_DISSOLVE
    sampler2D _DissolveThresholdMap;
#endif
#if _NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE
    TEXTURE2D(_PerCharacterBaseMapOverrideMap);
#endif
#if NiloToonCharacterAreaColorFillPass
    TEXTURE2D(_CharacterAreaColorFillTexture);
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// NiloToon's global textures
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////  
#if NiloToonIsAnyLitColorPass  
TEXTURE2D(_NiloToonAverageShadowMapRT);
#endif

#if _NILOTOON_RECEIVE_SELF_SHADOW
TEXTURE2D(_NiloToonCharSelfShadowMapRT);    SAMPLER_CMP(sampler_NiloToonCharSelfShadowMapRT);
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CBUFFER (material local Uniforms) 
// *you should put all per material uniforms of ALL passes inside the following UnityPerMaterial CBUFFER! 
// else SRP batching is not possible!
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// Material shader variables are not defined in SRP or URP shader library.
// This means _BaseColor, _BaseMap, _BaseMap_ST, and all variables in the Properties section of a shader
// must be defined by the shader itself. If you define all those properties in CBUFFER named
// UnityPerMaterial, SRP can cache the material properties between frames and reduce significantly the cost
// of each drawcall.
// In this case, although URP's LitInput.hlsl contains the CBUFFER for some of the material
// properties defined in .shader file. As one can see this is not part of the ShaderLibrary, it specific to the
// URP Lit shader.
// So we are not going to use LitInput.hlsl, we will implement everything by ourself.
// #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl" (we don't use it)

// put all your uniforms(usually things inside .shader file's properties{}) inside this CBUFFER, in order to make SRP batcher compatible
// see -> https://blogs.unity3d.com/2019/02/28/srp-batcher-speed-up-your-rendering/

// IMPORTANT NOTE: Do not #ifdef #endif the properties inside CBUFFER as SRP batcher can NOT handle different CBUFFER layouts.
// In order to be SRP Batcher compatible, 
// the "UnityPerMaterial" cbuffer has to have the exact same size and layout across all variants of one subshader. 
// Because in SRP Batcher, UnityPerMaterial data are persistent in GPU memory (so we don't want to have tons of different layout to update in GPU memory ).
// Just use the same declaration for this cbuffer. It won't hurt performance because we won't upload the data to GPU per drawcall
// (won't hurt performance as long as material properties are not changing between frames, else a CBUFFER re upload to GPU is needed once any material properties changed,
// since this CBUFFER is huge, the cost of editing material properties is huge!)
// https://forum.unity.com/threads/cbuffer-inconsistent-size-inside-a-subshader.784994/#post-5725762

// IMPORTANT NOTE: 
// [Vulkan SRP Batching note]
// Due to constant buffer(UnityPerMaterial CBUFFER) size limitation of vulkan (only in some android Mali GPU devices),
// if we make the (UnityPerMaterial CBUFFER) size too large, character rendering will be completely wrong(disappear / render as random color).
// We hardcode removed SRP batching on VULKAN for now (using macro SHADER_API_VULKAN), until we find a new solution.
// buggy devices list:
// - (vivo Y73s) (vivo Y72) (TAS_AN00) (mate40) (Huawei Honor 9x), all are Mali-G__ GPU + vulkan.
// safe devices list:
// - "Adreno GPU + vulkan" is safe (Xiaomi8)(Samsung A70)(RedmiNote9Pro)
// - "OpenGLES + any GPU" is safe also 
#ifndef SHADER_API_VULKAN
CBUFFER_START(UnityPerMaterial)
#endif

    float   _RenderCharacter;

    // identity if material is currently controlled by NiloToonPerCharacterRenderController script
    float   _ControlledByNiloToonPerCharacterRenderController;

    // enable rendering
    float   _EnableRendering;

    // UV Control
    float   _EnableUVEditGroup;
    float4  _UV0ScaleOffset;
    float4  _UV1ScaleOffset;
    float4  _UV2ScaleOffset;
    float4  _UV3ScaleOffset;
    float4  _UV0CenterPivotScalePos;
    float4  _UV1CenterPivotScalePos;
    float4  _UV2CenterPivotScalePos;
    float4  _UV3CenterPivotScalePos;
    float2  _UV0ScrollSpeed;
    float2  _UV1ScrollSpeed;
    float2  _UV2ScrollSpeed;
    float2  _UV3ScrollSpeed;
    float   _UV0RotatedAngle;
    float   _UV1RotatedAngle;
    float   _UV2RotatedAngle;
    float   _UV3RotatedAngle;
    float   _UV0RotateSpeed;
    float   _UV1RotateSpeed;
    float   _UV2RotateSpeed;
    float   _UV3RotateSpeed;
    float2  _MatCapUVTiling;

    // base color
    float4  _BaseMap_ST;
    float   _BaseMapUVIndex;
    half4   _BaseColor;
    half4   _BaseColor2;
    half    _BaseMapBrightness;
    half3   _PerCharacterBaseColorTint;

    half    _MultiplyBRPColor;
    half4   _Color; // BRP _Color

    // back face tint color, force shadow
    half4   _BackFaceBaseMapReplaceColor;
    half3   _BackFaceTintColor;
    half    _BackFaceForceShadow;

    // alpha override
    half    _AlphaOverrideStrength;
    half    _AlphaOverrideMode;
    float   _AlphaOverrideTexUVIndex;
    half    _AlphaOverrideTexInvertColor;
    half    _AlphaOverrideTexValueScale;
    half    _AlphaOverrideTexValueOffset;
    half    _ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCamera;
    half    _ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCameraRemapStart;
    half    _ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCameraRemapEnd;
    half4   _AlphaOverrideTexChannelMask;

    // alpha test
    half    _Cutoff;

    // fina output alpha
    half    _EditFinalOutputAlphaEnable;
    half    _ForceFinalOutputAlphaEqualsOne;

    // z offset
    float   _ZOffsetEnable;
    float   _ZOffset;
    float   _ZOffsetMultiplierForTraditionalOutlinePass;
    float4  _ZOffsetMaskMapChannelMask;
    float   _ZOffsetMaskMapInvertColor;

    // Parallax
    half    _Parallax;
    float   _ParallaxSampleUVIndex;
    uint    _ParallaxApplyToUVIndex;

    // BaseMap Alpha Blending Layer (1-10)
    half    _BaseMapStackingLayer1MasterStrength;
    float   _BaseMapStackingLayer1TexUVIndex;
    half4   _BaseMapStackingLayer1TintColor;
    half    _BaseMapStackingLayer1TexIgnoreAlpha;
    float2  _BaseMapStackingLayer1TexUVAnimSpeed;
    float4  _BaseMapStackingLayer1TexUVScaleOffset;
    float4  _BaseMapStackingLayer1TexUVCenterPivotScalePos;
    half4   _BaseMapStackingLayer1MaskTexChannel;
    float   _BaseMapStackingLayer1ApplytoFaces;
    uint    _BaseMapStackingLayer1MaskUVIndex;
    half    _BaseMapStackingLayer1MaskTexAsIDMap;
    half    _BaseMapStackingLayer1MaskTexExtractFromID;
    float   _BaseMapStackingLayer1MaskInvertColor;
    half    _BaseMapStackingLayer1MaskRemapStart;
    half    _BaseMapStackingLayer1MaskRemapEnd;
    uint    _BaseMapStackingLayer1ColorBlendMode;
    float   _BaseMapStackingLayer1TexUVRotatedAngle;
    float   _BaseMapStackingLayer1TexUVRotateSpeed;

    half    _BaseMapStackingLayer2MasterStrength;
    float   _BaseMapStackingLayer2TexUVIndex;
    half4   _BaseMapStackingLayer2TintColor;
    half    _BaseMapStackingLayer2TexIgnoreAlpha;
    float2  _BaseMapStackingLayer2TexUVAnimSpeed;
    float4  _BaseMapStackingLayer2TexUVScaleOffset;
    float4  _BaseMapStackingLayer2TexUVCenterPivotScalePos;
    half4   _BaseMapStackingLayer2MaskTexChannel;
    float   _BaseMapStackingLayer2ApplytoFaces;
    uint    _BaseMapStackingLayer2MaskUVIndex;
    half    _BaseMapStackingLayer2MaskTexAsIDMap;
    half    _BaseMapStackingLayer2MaskTexExtractFromID;
    float   _BaseMapStackingLayer2MaskInvertColor;
    half    _BaseMapStackingLayer2MaskRemapStart;
    half    _BaseMapStackingLayer2MaskRemapEnd;
    uint    _BaseMapStackingLayer2ColorBlendMode;
    float   _BaseMapStackingLayer2TexUVRotatedAngle;
    float   _BaseMapStackingLayer2TexUVRotateSpeed;

    half    _BaseMapStackingLayer3MasterStrength;
    float   _BaseMapStackingLayer3TexUVIndex;
    half4   _BaseMapStackingLayer3TintColor;
    half    _BaseMapStackingLayer3TexIgnoreAlpha;
    float2  _BaseMapStackingLayer3TexUVAnimSpeed;
    float4  _BaseMapStackingLayer3TexUVScaleOffset;
    float4  _BaseMapStackingLayer3TexUVCenterPivotScalePos;
    half4   _BaseMapStackingLayer3MaskTexChannel;
    float   _BaseMapStackingLayer3ApplytoFaces;
    uint    _BaseMapStackingLayer3MaskUVIndex;
    half    _BaseMapStackingLayer3MaskTexAsIDMap;
    half    _BaseMapStackingLayer3MaskTexExtractFromID;
    float   _BaseMapStackingLayer3MaskInvertColor;
    half    _BaseMapStackingLayer3MaskRemapStart;
    half    _BaseMapStackingLayer3MaskRemapEnd;
    uint    _BaseMapStackingLayer3ColorBlendMode;
    float   _BaseMapStackingLayer3TexUVRotatedAngle;
    float   _BaseMapStackingLayer3TexUVRotateSpeed;

    half    _BaseMapStackingLayer4MasterStrength;
    float   _BaseMapStackingLayer4TexUVIndex;
    half4   _BaseMapStackingLayer4TintColor;
    half    _BaseMapStackingLayer4TexIgnoreAlpha;
    float2  _BaseMapStackingLayer4TexUVAnimSpeed;
    float4  _BaseMapStackingLayer4TexUVScaleOffset;
    float4  _BaseMapStackingLayer4TexUVCenterPivotScalePos;
    half4   _BaseMapStackingLayer4MaskTexChannel;
    float   _BaseMapStackingLayer4ApplytoFaces;
    uint    _BaseMapStackingLayer4MaskUVIndex;
    half    _BaseMapStackingLayer4MaskTexAsIDMap;
    half    _BaseMapStackingLayer4MaskTexExtractFromID;
    float   _BaseMapStackingLayer4MaskInvertColor;
    half    _BaseMapStackingLayer4MaskRemapStart;
    half    _BaseMapStackingLayer4MaskRemapEnd;
    uint    _BaseMapStackingLayer4ColorBlendMode;
    float   _BaseMapStackingLayer4TexUVRotatedAngle;
    float   _BaseMapStackingLayer4TexUVRotateSpeed;

    half    _BaseMapStackingLayer5MasterStrength;
    float   _BaseMapStackingLayer5TexUVIndex;
    half4   _BaseMapStackingLayer5TintColor;
    half    _BaseMapStackingLayer5TexIgnoreAlpha;
    float2  _BaseMapStackingLayer5TexUVAnimSpeed;
    float4  _BaseMapStackingLayer5TexUVScaleOffset;
    float4  _BaseMapStackingLayer5TexUVCenterPivotScalePos;
    half4   _BaseMapStackingLayer5MaskTexChannel;
    float   _BaseMapStackingLayer5ApplytoFaces;
    uint    _BaseMapStackingLayer5MaskUVIndex;
    half    _BaseMapStackingLayer5MaskTexAsIDMap;
    half    _BaseMapStackingLayer5MaskTexExtractFromID;
    float   _BaseMapStackingLayer5MaskInvertColor;
    half    _BaseMapStackingLayer5MaskRemapStart;
    half    _BaseMapStackingLayer5MaskRemapEnd;
    uint    _BaseMapStackingLayer5ColorBlendMode;
    float   _BaseMapStackingLayer5TexUVRotatedAngle;
    float   _BaseMapStackingLayer5TexUVRotateSpeed;

    half    _BaseMapStackingLayer6MasterStrength;
    float   _BaseMapStackingLayer6TexUVIndex;
    half4   _BaseMapStackingLayer6TintColor;
    half    _BaseMapStackingLayer6TexIgnoreAlpha;
    float2  _BaseMapStackingLayer6TexUVAnimSpeed;
    float4  _BaseMapStackingLayer6TexUVScaleOffset;
    float4  _BaseMapStackingLayer6TexUVCenterPivotScalePos;
    half4   _BaseMapStackingLayer6MaskTexChannel;
    float   _BaseMapStackingLayer6ApplytoFaces;
    uint    _BaseMapStackingLayer6MaskUVIndex;
    half    _BaseMapStackingLayer6MaskTexAsIDMap;
    half    _BaseMapStackingLayer6MaskTexExtractFromID;
    float   _BaseMapStackingLayer6MaskInvertColor;
    half    _BaseMapStackingLayer6MaskRemapStart;
    half    _BaseMapStackingLayer6MaskRemapEnd;
    uint    _BaseMapStackingLayer6ColorBlendMode;
    float   _BaseMapStackingLayer6TexUVRotatedAngle;
    float   _BaseMapStackingLayer6TexUVRotateSpeed;

    half    _BaseMapStackingLayer7MasterStrength;
    float   _BaseMapStackingLayer7TexUVIndex;
    half4   _BaseMapStackingLayer7TintColor;
    half    _BaseMapStackingLayer7TexIgnoreAlpha;
    float2  _BaseMapStackingLayer7TexUVAnimSpeed;
    float4  _BaseMapStackingLayer7TexUVScaleOffset;
    float4  _BaseMapStackingLayer7TexUVCenterPivotScalePos;
    half4   _BaseMapStackingLayer7MaskTexChannel;
    float   _BaseMapStackingLayer7ApplytoFaces;
    uint    _BaseMapStackingLayer7MaskUVIndex;
    half    _BaseMapStackingLayer7MaskTexAsIDMap;
    half    _BaseMapStackingLayer7MaskTexExtractFromID;
    float   _BaseMapStackingLayer7MaskInvertColor;
    half    _BaseMapStackingLayer7MaskRemapStart;
    half    _BaseMapStackingLayer7MaskRemapEnd;
    uint    _BaseMapStackingLayer7ColorBlendMode;
    float   _BaseMapStackingLayer7TexUVRotatedAngle;
    float   _BaseMapStackingLayer7TexUVRotateSpeed;

    half    _BaseMapStackingLayer8MasterStrength;
    float   _BaseMapStackingLayer8TexUVIndex;
    half4   _BaseMapStackingLayer8TintColor;
    half    _BaseMapStackingLayer8TexIgnoreAlpha;
    float2  _BaseMapStackingLayer8TexUVAnimSpeed;
    float4  _BaseMapStackingLayer8TexUVScaleOffset;
    float4  _BaseMapStackingLayer8TexUVCenterPivotScalePos;
    half4   _BaseMapStackingLayer8MaskTexChannel;
    float   _BaseMapStackingLayer8ApplytoFaces;
    uint    _BaseMapStackingLayer8MaskUVIndex;
    half    _BaseMapStackingLayer8MaskTexAsIDMap;
    half    _BaseMapStackingLayer8MaskTexExtractFromID;
    float   _BaseMapStackingLayer8MaskInvertColor;
    half    _BaseMapStackingLayer8MaskRemapStart;
    half    _BaseMapStackingLayer8MaskRemapEnd;
    uint    _BaseMapStackingLayer8ColorBlendMode;
    float   _BaseMapStackingLayer8TexUVRotatedAngle;
    float   _BaseMapStackingLayer8TexUVRotateSpeed;

    half    _BaseMapStackingLayer9MasterStrength;
    float   _BaseMapStackingLayer9TexUVIndex;
    half4   _BaseMapStackingLayer9TintColor;
    half    _BaseMapStackingLayer9TexIgnoreAlpha;
    float2  _BaseMapStackingLayer9TexUVAnimSpeed;
    float4  _BaseMapStackingLayer9TexUVScaleOffset;
    float4  _BaseMapStackingLayer9TexUVCenterPivotScalePos;
    half4   _BaseMapStackingLayer9MaskTexChannel;
    float   _BaseMapStackingLayer9ApplytoFaces;
    uint    _BaseMapStackingLayer9MaskUVIndex;
    half    _BaseMapStackingLayer9MaskTexAsIDMap;
    half    _BaseMapStackingLayer9MaskTexExtractFromID;
    float   _BaseMapStackingLayer9MaskInvertColor;
    half    _BaseMapStackingLayer9MaskRemapStart;
    half    _BaseMapStackingLayer9MaskRemapEnd;
    uint    _BaseMapStackingLayer9ColorBlendMode;
    float   _BaseMapStackingLayer9TexUVRotatedAngle;
    float   _BaseMapStackingLayer9TexUVRotateSpeed;

    half    _BaseMapStackingLayer10MasterStrength;
    float   _BaseMapStackingLayer10TexUVIndex;
    half4   _BaseMapStackingLayer10TintColor;
    half    _BaseMapStackingLayer10TexIgnoreAlpha;
    float2  _BaseMapStackingLayer10TexUVAnimSpeed;
    float4  _BaseMapStackingLayer10TexUVScaleOffset;
    float4  _BaseMapStackingLayer10TexUVCenterPivotScalePos;
    half4   _BaseMapStackingLayer10MaskTexChannel;
    float   _BaseMapStackingLayer10ApplytoFaces;
    uint    _BaseMapStackingLayer10MaskUVIndex;
    half    _BaseMapStackingLayer10MaskTexAsIDMap;
    half    _BaseMapStackingLayer10MaskTexExtractFromID;
    float   _BaseMapStackingLayer10MaskInvertColor;
    half    _BaseMapStackingLayer10MaskRemapStart;
    half    _BaseMapStackingLayer10MaskRemapEnd;
    uint    _BaseMapStackingLayer10ColorBlendMode;
    float   _BaseMapStackingLayer10TexUVRotatedAngle;
    float   _BaseMapStackingLayer10TexUVRotateSpeed;

    // normal map
    half    _BumpScale;
    float   _BumpMapApplytoFaces;
    float   _BumpMapUVIndex;
    float4  _BumpMapUVScaleOffset;
    float2  _BumpMapUVScrollSpeed;

    // emission
    float4  _EmissionMapTilingXyOffsetZw;
    float2  _EmissionMapUVScrollSpeed;
    half    _EmissionIntensity;
    half3   _EmissionColor;
    half    _MultiplyBaseColorToEmissionColor;
    half    _MultiplyLightColorToEmissionColor;
    half    _EmissionMapUseSingleChannelOnly;
    half4   _EmissionMapSingleChannelMask;
    half4   _EmissionMaskMapChannelMask;
    half    _EmissionMaskMapInvertColor;
    half    _EmissionMaskMapRemapStart;
    half    _EmissionMaskMapRemapEnd; 

    // mat cap (alpha blend)
    half    _MatCapAlphaBlendUsage;
    half4   _MatCapAlphaBlendTintColor;
    float   _MatCapAlphaBlendUvScale;
    half    _MatCapAlphaBlendMapAlphaAsMask;
    half4   _MatCapAlphaBlendMaskMapChannelMask;
    half    _MatCapAlphaBlendMaskMapInvertColor;
    half    _MatCapAlphaBlendMaskMapRemapStart;
    half    _MatCapAlphaBlendMaskMapRemapEnd;

    // mat cap (additive)
    half    _MatCapAdditiveMapAlphaAsMask;
    half    _MatCapAdditiveIntensity;
    half    _MatCapAdditiveExtractBrightArea;
    half4   _MatCapAdditiveColor;
    half    _MatCapAdditiveMixWithBaseMapColor;
    float   _MatCapAdditiveUvScale;   
    half4   _MatCapAdditiveMaskMapChannelMask;
    half    _MatCapAdditiveMaskMapInvertColor;
    half    _MatCapAdditiveMaskMapRemapStart;
    half    _MatCapAdditiveMaskMapRemapEnd;
    float   _MatCapAdditiveApplytoFaces;

    // mat cap (occlusion)
    half    _MatCapOcclusionIntensity;
    half4   _MatCapOcclusionMapChannelMask;
    half    _MatCapOcclusionMapRemapStart;
    half    _MatCapOcclusionMapRemapEnd;
    half    _MatCapOcclusionMapAlphaAsMask;
    float   _MatCapOcclusionUvScale;   
    half4   _MatCapOcclusionMaskMapChannelMask;
    half    _MatCapOcclusionMaskMapInvert;
    half    _MatCapOcclusionMaskMapRemapStart;
    half    _MatCapOcclusionMaskMapRemapEnd;

    // occlusion
    half    _OcclusionStrength;
    half    _OcclusionStrengthIndirectMultiplier;
    half4   _OcclusionMapChannelMask;
    float   _OcclusionMapUVIndex;
    half    _OcclusionMapInvertColor;
    half    _OcclusionRemapStart;
    half    _OcclusionRemapEnd;
    float   _OcclusionMapApplytoFaces;

    // shading grade map
    half    _ShadingGradeMapStrength;
    half    _ShadingGradeMapApplyRange;
    half    _ShadingGradeMapMidPointOffset;
    half4   _ShadingGradeMapChannelMask;
    half    _ShadingGradeMapInvertColor;
    half    _ShadingGradeMapRemapStart;
    half    _ShadingGradeMapRemapEnd;

    // Face Shadow Gradient Map
    half4   _FaceShadowGradientMapChannel;
    float   _FaceShadowGradientMapUVIndex;
    half    _FaceShadowGradientMapInvertColor;
    half    _FaceShadowGradientOffset;
    float   _FaceShadowGradientMapUVxInvert;
    float4  _FaceShadowGradientMapUVCenterPivotScalePos;
    float4  _FaceShadowGradientMapUVScaleOffset;
    float   _DebugFaceShadowGradientMap;
    half    _FaceShadowGradientIntensity;
    half    _FaceShadowGradientMapFaceMidPoint;
    half    _FaceShadowGradientResultSoftness;
    half4   _FaceShadowGradientMaskMapChannel;
    half    _FaceShadowGradientMaskMapInvertColor;
    float   _FaceShadowGradientMaskMapUVIndex;
    half    _FaceShadowGradientThresholdMin;
    half    _FaceShadowGradientThresholdMax;
    half    _IgnoreDefaultMainLightFaceShadow;

    // Face 3D Rim Light and Shadow
    half    _Face3DRimLightAndShadow_NoseRimLightIntensity;
    half3   _Face3DRimLightAndShadow_NoseRimLightTintColor;
    half4   _Face3DRimLightAndShadow_NoseRimLightMaskMapChannel;

    half    _Face3DRimLightAndShadow_NoseShadowIntensity;
    half3   _Face3DRimLightAndShadow_NoseShadowTintColor;
    half4   _Face3DRimLightAndShadow_NoseShadowMaskMapChannel;

    half    _Face3DRimLightAndShadow_CheekRimLightIntensity;
    half3   _Face3DRimLightAndShadow_CheekRimLightTintColor;
    half4   _Face3DRimLightAndShadow_CheekRimLightMaskMapChannel;
    half    _Face3DRimLightAndShadow_CheekRimLightThreshold;
    half    _Face3DRimLightAndShadow_CheekRimLightSoftness;

    half    _Face3DRimLightAndShadow_CheekShadowIntensity;
    half3   _Face3DRimLightAndShadow_CheekShadowTintColor;
    half4   _Face3DRimLightAndShadow_CheekShadowMaskMapChannel;
    half    _Face3DRimLightAndShadow_CheekShadowThreshold;
    half    _Face3DRimLightAndShadow_CheekShadowSoftness;
    
    // smoothness
    half    _Smoothness;
    half    _SmoothnessMapInputIsRoughnessMap;
    half4   _SmoothnessMapChannelMask;
    half    _SmoothnessMapRemapStart;
    half    _SmoothnessMapRemapEnd;   

    // specular
    float   _SpecularMapUVIndex;
    half4   _SpecularMapChannelMask;
    half    _SpecularMapAsIDMap;
    half    _SpecularMapExtractFromID;
    half    _SpecularMapInvertColor;
    half    _SpecularMapRemapStart;
    half    _SpecularMapRemapEnd;
    half    _UseGGXDirectSpecular;
    half    _SpecularReactToLightDirMode;
    half    _SpecularIntensity;
    half3   _SpecularColor;
    half    _SpecularUseReplaceBlending;
    half    _MultiplyBaseColorToSpecularColor;
    half    _GGXDirectSpecularSmoothnessMultiplier;
    half    _SpecularColorTintMapUsage;
    float   _SpecularColorTintMapUseSecondUv;
    float4  _SpecularColorTintMapTilingXyOffsetZw;
    half    _SpecularAreaRemapUsage;
    half    _SpecularAreaRemapMidPoint;
    half    _SpecularAreaRemapRange;
    half    _SpecularShowInShadowArea;
    float   _SpecularApplytoFaces;

    // Environment Reflections
    half    _EnvironmentReflectionUsage;
    half    _EnvironmentReflectionShouldApplyToFaceArea;
    half    _EnvironmentReflectionBrightness;
    half3   _EnvironmentReflectionColor;
    half    _EnvironmentReflectionTintAlbedo;
    half    _EnvironmentReflectionApplyReplaceBlending;
    half    _EnvironmentReflectionApplyAddBlending;
    half    _EnvironmentReflectionSmoothnessMultiplier;
    half    _EnvironmentReflectionFresnelEffect;
    half    _EnvironmentReflectionFresnelPower;
    half    _EnvironmentReflectionFresnelRemapStart;
    half    _EnvironmentReflectionFresnelRemapEnd;
    half4   _EnvironmentReflectionMaskMapChannelMask;
    half    _EnvironmentReflectionMaskMapInvertColor;
    half    _EnvironmentReflectionMaskMapRemapStart;
    half    _EnvironmentReflectionMaskMapRemapEnd;
    float   _EnvironmentReflectionApplytoFaces;

    // kayjiya-kay hair specular
    half    _HairStrandSpecularMixWithBaseMapColor;
    half    _HairStrandSpecularTintMapUsage;
    float4  _HairStrandSpecularTintMapTilingXyOffsetZw;
    half    _HairStrandSpecularOverallIntensity;
    half    _HairStrandSpecularShapeFrequency;
    half    _HairStrandSpecularShapeShift;
    half    _HairStrandSpecularShapePositionOffset;
    half    _HairStrandSpecularMainIntensity;
    half    _HairStrandSpecularSecondIntensity;
    half3   _HairStrandSpecularMainColor;
    half3   _HairStrandSpecularSecondColor;
    half    _HairStrandSpecularMainExponent;
    half    _HairStrandSpecularSecondExponent;

    // detail map
    float   _DetailUseSecondUv;
    half4   _DetailMaskChannelMask;
    half    _DetailMaskInvertColor;
    float4  _DetailMapsScaleTiling;
    half    _DetailAlbedoWhitePoint;
    half    _DetailAlbedoMapScale;
    half    _DetailNormalMapScale;

    // shadow color
    half    _EnableShadowColor;
    half    _SelfShadowAreaHSVStrength;
    half    _SelfShadowAreaHueOffset;
    half    _SelfShadowAreaSaturationBoost;
    half    _SelfShadowAreaValueMul;
    half3   _SelfShadowTintColor;
    half    _LitToShadowTransitionAreaIntensity;
    half    _LitToShadowTransitionAreaHueOffset;
    half    _LitToShadowTransitionAreaSaturationBoost;
    half    _LitToShadowTransitionAreaValueMul;
    half3   _LitToShadowTransitionAreaTintColor;
    half4   _LowSaturationFallbackColor;
    half    _OverrideBySkinShadowTintColor;
    half3   _SkinShadowTintColor;
    half3   _SkinShadowTintColor2;
    half    _SkinShadowBrightness;
    half    _OverrideByFaceShadowTintColor;
    half3   _FaceShadowTintColor;
    half3   _FaceShadowTintColor2;
    half    _FaceShadowBrightness;

    // override shadow color by tex
    half    _OverrideShadowColorByTexMode;
    half    _OverrideShadowColorByTexIntensity;
    half4   _OverrideShadowColorTexTintColor;
    half    _OverrideShadowColorTexIgnoreAlphaChannel;
    half4   _OverrideShadowColorMaskMapChannelMask;
    half    _OverrideShadowColorMaskMapInvertColor;

    // ramp lighting texture
    float   _RampLightTexMode;
    half    _RampLightingTexSampleUvY;
    half    _RampLightingNdotLRemapStart;
    half    _RampLightingNdotLRemapEnd;
    half4   _RampLightingSampleUvYTexChannelMask;
    half    _RampLightingSampleUvYTexInvertColor;
    half    _RampLightingUvYRemapStart;
    half    _RampLightingUvYRemapEnd;
    half    _RampLightingFaceAreaRemoveEffect;

    // ramp specular texture
    half    _RampSpecularTexSampleUvY;
    half    _RampSpecularWhitePoint;

    // lighting style
    half    _AsUnlit;
    half    _CelShadeMidPoint;
    half    _CelShadeSoftness;
    half    _MainLightIgnoreCelShade;
    half    _IndirectLightFlatten;
    half    _AdditionalLightCelShadeMidPoint;
    half    _AdditionalLightCelShadeSoftness;
    half    _AdditionalLightIgnoreCelShade;
    half    _AdditionalLightIgnoreOcclusion;
    half    _AdditionalLightDistanceAttenuationClamp;

    // lighting style for face area
    float   _OverrideCelShadeParamForFaceArea;
    half    _CelShadeMidPointForFaceArea;
    half    _CelShadeSoftnessForFaceArea;
    half    _MainLightIgnoreCelShadeForFaceArea;    
    float   _OverrideAdditionalLightCelShadeParamForFaceArea;
    half    _AdditionalLightCelShadeMidPointForFaceArea;
    half    _AdditionalLightCelShadeSoftnessForFaceArea;
    half    _AdditionalLightIgnoreCelShadeForFaceArea;    

    // URP shadow mapping (directional light)
    float   _ReceiveURPShadowMapping; // on/off toggle per material
    half    _ReceiveURPShadowMappingAmount;
    half    _ReceiveURPShadowMappingAmountForFace;
    half    _ReceiveURPShadowMappingAmountForNonFace;
    float   _ReceiveSelfShadowMappingPosOffset;
    float   _ReceiveSelfShadowMappingPosOffsetForFaceArea;
    half3   _URPShadowMappingTintColor;

    // URP shadow mapping (additional light)
    half    _ReceiveURPAdditionalLightShadowMapping;
    half    _ReceiveURPAdditionalLightShadowMappingAmount;
    half    _ReceiveURPAdditionalLightShadowMappingAmountForNonFace;
    half    _ReceiveURPAdditionalLightShadowMappingAmountForFace;

    // depth texture rim light and shadow
    float   _PerMaterialEnableDepthTextureRimLightAndShadow;
    float   _NiloToonEnableDepthTextureRimLightAndShadow;

    float   _DepthTexRimLightAndShadowWidthMultiplier;
    float   _DepthTexRimLight3DRimMaskEnable;
    float   _DepthTexRimLight3DRimMaskThreshold;
    float   _DepthTexRimLightAndShadowWidthExtraMultiplier;
    float   _DepthTexRimLightAndShadowReduceWidthWhenCameraIsClose;

    float   _DepthTexRimLightIgnoreLightDir;
    float   _DepthTexShadowIgnoreLightDir;
    float   _DepthTexShadowFixedDirectionForFace;

    float   _DepthTexRimLightWidthMultiplier;
    float   _DepthTexShadowWidthMultiplier;

    float   _UseDepthTexRimLightAndShadowWidthMultiplierFromVertexColor;
    float4  _DepthTexRimLightAndShadowWidthMultiplierFromVertexColorChannelMask;
    float4  _DepthTexRimLightAndShadowWidthTexChannelMask;

    half    _DepthTexRimLightUsage;
    half4   _DepthTexRimLightMaskTexChannelMask;
    half    _DepthTexRimLightMaskTexInvertColor;
    half    _DepthTexRimLightIntensity;
    half3   _DepthTexRimLightTintColor;
    half    _DepthTexRimLightMixWithBaseMapColor;
    half    _DepthTexRimLightBlockByShadow;
    float   _DepthTexRimLightThresholdOffset;
    float   _DepthTexRimLightFadeoutRange;

    half    _DepthTexRimLight3DFallbackMidPoint;
    half    _DepthTexRimLight3DFallbackSoftness;
    half    _DepthTexRimLight3DFallbackRemoveFlatPolygonRimLight;

    float   _DepthTexRimLightFixDottedLineArtifactsExtendMultiplier;
    
    half    _DepthTexShadowUsage;
    half    _DepthTexShadowBrightness;
    half    _DepthTexShadowBrightnessForFace;
    half3   _DepthTexShadowTintColor;
    half3   _DepthTexShadowTintColorForFace;
    float   _DepthTexShadowThresholdOffset;
    float   _DepthTexShadowFadeoutRange;

    float   _FaceAreaCameraDepthTextureZWriteOffset; // a just enough ZOffset for face area when writing depth into _CameraDepthTexture.Good ZOffset range is about 0.04 to 0.06, which makes depth texture shadow easier to appear

    // Character ID for read global 1D array or load 1D texture
    uint   _CharacterID;

    // NiloToon self shadow mapping
    float   _EnableNiloToonSelfShadowMapping;
    float   _NiloToonSelfShadowMappingDepthBias;
    float   _NiloToonSelfShadowMappingNormalBias;
    half    _NiloToonSelfShadowIntensity;
    half    _NiloToonSelfShadowIntensityForNonFace;
    half    _NiloToonSelfShadowIntensityForFace;
    half3   _NiloToonSelfShadowMappingTintColor;
    float   _EnableNiloToonSelfShadowMappingDepthBias;
    float   _EnableNiloToonSelfShadowMappingNormalBias;

    // per char receive shadows
    half   _PerCharReceiveAverageURPShadowMap;
    half   _PerCharReceiveStandardURPShadowMap;
    half   _PerCharReceiveNiloToonSelfShadowMap;

    // outline
    float   _RenderOutline;
    float   _PerCharacterRenderOutline;
    float   _OutlineUseBakedSmoothNormal;
    float   _UnityCameraDepthTextureWriteOutlineExtrudedPosition;
    float   _OutlineUniformLengthInViewSpace;
    half3   _OutlineTintColor;
    half4   _OutlineTintColorSkinAreaOverride;
    half3   _OutlineOcclusionAreaTintColor;
    half    _OutlineUseReplaceColor;
    half3   _OutlineReplaceColor;
    half    _OutlineUsePreLightingReplaceColor;
    half3   _OutlinePreLightingReplaceColor;

    float   _OutlineWidth;
    float   _OutlineWidthExtraMultiplier;

    float   _OutlineBaseZOffset;
    float   _OutlineZOffset;
    float   _OutlineZOffsetForFaceArea;
    float   _UseOutlineZOffsetTex;
    float4  _OutlineZOffsetMaskTexChannelMask;
    float   _OutlineZOffsetMaskTexInvertColor;
    float   _OutlineZOffsetMaskRemapStart;
    float   _OutlineZOffsetMaskRemapEnd;
    float   _UseOutlineZOffsetMaskFromVertexColor;
    float4  _OutlineZOffsetMaskFromVertexColor;
    float   _UseOutlineWidthMaskFromVertexColor;
    float4  _OutlineWidthMaskFromVertexColor;
    float4  _OutlineWidthTexChannelMask;   

    float   _PerCharacterOutlineWidthMultiply;
    half3   _PerCharacterOutlineColorTint;
    half4   _PerCharacterOutlineColorLerp;

    // override outline color by tex
    half4   _OverrideOutlineColorTexTintColor;
    half    _OverrideOutlineColorTexIgnoreAlphaChannel;
    half    _OverrideOutlineColorByTexIntensity;

    // screen space outline
    float   _ScreenSpaceOutlineWidth;
    float   _ScreenSpaceOutlineWidthIfFace;
    float   _ScreenSpaceOutlineDepthSensitivity;
    float   _ScreenSpaceOutlineDepthSensitivityIfFace;
    float   _ScreenSpaceOutlineNormalsSensitivity;
    float   _ScreenSpaceOutlineNormalsSensitivityIfFace;
    half3   _ScreenSpaceOutlineTintColor;
    half3   _ScreenSpaceOutlineOcclusionAreaTintColor;
    half    _ScreenSpaceOutlineUseReplaceColor;
    half3   _ScreenSpaceOutlineReplaceColor;   
    half4   _ScreenSpaceOutlineDepthSensitivityTexChannelMask;
    half4   _ScreenSpaceOutlineNormalsSensitivityTexChannelMask;
    half    _ScreenSpaceOutlineDepthSensitivityTexRemapStart;
    half    _ScreenSpaceOutlineDepthSensitivityTexRemapEnd;
    half    _ScreenSpaceOutlineNormalsSensitivityTexRemapStart;
    half    _ScreenSpaceOutlineNormalsSensitivityTexRemapEnd;

    // dynamic eye
    float   _DynamicEyeSize;
    half    _DynamicEyeFinalBrightness;
    half3   _DynamicEyeFinalTintColor;
    half4   _DynamicEyePupilMaskTexChannelMask;
    half3   _DynamicEyePupilColor;
    float   _DynamicEyePupilDepthScale;
    float   _DynamicEyePupilSize;
    float   _DynamicEyePupilMaskSoftness;
    float4  _DynamicEyeWhiteMap_ST;

    // color fill
    half    _CharacterAreaColorFillEnabled;
    half4   _CharacterAreaColorFillColor;
    uint    _CharacterAreaColorFillTextureUVIndex;
    float4  _CharacterAreaColorFillTextureUVTilingOffset;
    float2  _CharacterAreaColorFillTextureUVScrollSpeed;
    half    _CharacterAreaColorFillRendersVisibleArea;
    half    _CharacterAreaColorFillRendersBlockedArea;

    // extra thick outline
    float   _ExtraThickOutlineEnabled;
    float   _ExtraThickOutlineWidth;
    float   _ExtraThickOutlineMaxFinalWidth;
    float3  _ExtraThickOutlineViewSpacePosOffset;
    half4   _ExtraThickOutlineColor;
    float   _ExtraThickOutlineZOffset;
    float   _ExtraThickOutlineZWrite;
    float   _ExtraThickOutlineWriteIntoDepthTexture;

    // gameplay effect
    half3   _PerCharEffectTintColor;
    half3   _PerCharEffectAddColor;
    half4   _PerCharEffectLerpColor;
    half3   _PerCharEffectRimColor;
    half    _PerCharEffectRimSharpnessPower;
    half    _PerCharEffectDesaturatePercentage;

    // skin
    float   _IsSkin;
    half4   _SkinMaskMapChannelMask;
    half    _SkinMaskMapInvertColor;
    half    _SkinMaskMapRemapStart;
    half    _SkinMaskMapRemapEnd;
    half    _SkinMaskMapAsIDMap;
    half    _SkinMaskMapExtractFromID;

    // face
    half4   _FaceMaskMapChannelMask;
    half    _FaceMaskMapInvertColor;
    half    _FaceMaskMapRemapStart;
    half    _FaceMaskMapRemapEnd;
    //half3   _FaceForwardDirection; // converted to global array
    //half3   _FaceUpDirection; // converted to global array
    half    _FixFaceNormalAmount;
    half    _FixFaceNormalUseFlattenOrProxySphereMethod;

    // per char bounding sphere
    //float3  _CharacterBoundCenterPosWS; // converted to global array
    float   _CharacterBoundRadius;

    // dither fadeout
    float   _DitherFadeoutAmount; // clip() only accept float
    float   _DitherFadeoutNormalScaleFix;

    // perspective removal
    float   _PerspectiveRemovalAmount; // total amount

    // perspective removal(sphere)
    float   _PerspectiveRemovalRadius;
    //float3  _HeadBonePositionWS; // converted to global array

    // perspective removal(world height)
    float   _PerspectiveRemovalStartHeight; // usually is world space pos.y 0
    float   _PerspectiveRemovalEndHeight;

    // ZWrite (for disabling all _CameraDepthTexture related effect when _ZWrite = off)
    float   _ZWrite;
    // Cull (for fixing outline problem when rendering planar reflection pass)
    float   _Cull;
    // Blending (for outputing correct alpha at the exit of fragment shader)
    float   _SrcBlend;
    float   _DstBlend;

    // _AllowNiloToonBloomOverrideGroup
    float   _AllowNiloToonBloomCharacterAreaOverride;
    float   _AllowedNiloToonBloomOverrideStrength;

    // _NILOTOON_DISSOLVE
    float   _DissolveAmount;
    float   _DissolveMode;
    float   _DissolveThresholdMapTilingX;
    float   _DissolveThresholdMapTilingY;
    float   _DissolveNoiseStrength;
    float   _DissolveBorderRange;
    half3   _DissolveBorderTintColor;

    // per character basemap override
    half    _PerCharacterBaseMapOverrideAmount;
    float4  _PerCharacterBaseMapOverrideTilingOffset;
    float2  _PerCharacterBaseMapOverrideUVScrollSpeed;
    half3   _PerCharacterBaseMapOverrideTintColor;
    uint    _PerCharacterBaseMapOverrideUVIndex;
    uint    _PerCharacterBaseMapOverrideBlendMode;

    // per character ZOffset
    float   _PerCharZOffset;

    // Pass On/Off
    float   _AllowRenderURPShadowCasterPass;
    float   _AllowRenderNiloToonSelfShadowPass;
    float   _AllowRenderExtraThickOutlinePass;
    float   _AllowRenderDepthOnlyOrDepthNormalsPass;

    // Per character effect on/off
    float   _AllowPerCharacterDissolve;
    float   _AllowPerCharacterDitherFadeout;

    // Pre-Multiply alpha options
    half    _PreMultiplyAlphaIntoRGBOutput;

#ifndef SHADER_API_VULKAN
CBUFFER_END
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Global uniforms
// if an uniform is not a per material uniform, 
// it is fine to write it outside of CBUFFER_START(UnityPerMaterial)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// a special uniform for applyShadowBiasFixToHClipPos() only
float3  _LightDirection;
float3  _LightPosition;

// global outline uniforms
float   _GlobalShouldRenderOutline;
float   _GlobalOutlineWidthMultiplier;
half3   _GlobalOutlineTintColor;
float   _GlobalOutlineWidthAutoAdjustToCameraDistanceAndFOV;

// global shadow mapping uniforms
float   _GlobalShouldReceiveShadowMapping;
half    _GlobalReceiveShadowMappingAmount;
float   _GlobalToonShaderNormalBiasMultiplier;
half3   _GlobalMainLightURPShadowAsDirectResultTintColor;
half    _GlobalURPShadowAsDirectLightTintIgnoreMaterialURPUsageSetting;
half    _GlobalNiloToonReceiveURPShadowblurriness;

// global self shadow mapping
float4x4 _NiloToonSelfShadowWorldToClip;
float4  _NiloToonSelfShadowParam;
float4  _NiloToonSelfShadowSoftShadowParam; // x: soft shadow quality | y: should reshape | z: reshape blur size
float   _NiloToonSelfShadowRange;
float   _NiloToonGlobalSelfShadowCasterDepthBias;
float   _NiloToonGlobalSelfShadowCasterNormalBias;
float   _NiloToonGlobalSelfShadowReceiverDepthBias;
float   _NiloToonGlobalSelfShadowReceiverNormalBias;
half3   _NiloToonSelfShadowLightDirection;
half    _NiloToonSelfShadowUseNdotLFix;
half    _GlobalReceiveNiloToonSelfShadowMap;
float   _GlobalReceiveSelfShadowMappingPosOffset;

// global occlusion uniforms
half    _GlobalOcclusionStrength;

// global lighting uniforms
half    _GlobalIndirectLightMultiplier;
half3   _GlobalIndirectLightMinColor;

// global depth diff rim light and shadow uniforms
float   _GlobalEnableDepthTextureRimLigthAndShadow;
float   _GlobalDepthTexRimLightAndShadowWidthMultiplier;
float   _GlobalDepthTexRimLightDepthDiffThresholdOffset;
float   _GlobalDepthTexRimLightCameraDistanceFadeoutStartDistance;
float   _GlobalDepthTexRimLightCameraDistanceFadeoutEndDistance;

// global light uniforms
half3   _GlobalUserOverriddenMainLightDirVS;
half3   _GlobalUserOverriddenMainLightDirWS;
half3   _GlobalUserOverriddenMainLightColor;
half4   _GlobalUserOverriddenFinalMainLightDirWSParam; // xyz: final main light direction, w: 1 if enabled, else 0
half4   _GlobalUserOverriddenFinalMainLightColorParam; // rgb: final main light color, w: 1 if enabled, else 0

// global camera uniforms
float   _CurrentCameraFOV;

// global camera fix
float2  _GlobalAspectFix;
float   _GlobalFOVorOrthoSizeFix;

// global volume
half3   _GlobalVolumeMulColor;
half4   _GlobalVolumeLerpColor;
half3   _GlobalRimLightMultiplier;
half3   _GlobalRimLightMultiplierForOutlineArea;
half3   _GlobalIndirectLightMaxColor;
half3   _GlobalMainDirectionalLightMaxContribution;
half3   _GlobalAdditionalLightMaxContribution;
half3   _GlobalSpecularTintColor;
half    _GlobalSpecularMinIntensity;
half    _GlobalSpecularReactToLightDirectionChange;
half3   _GlobalMainDirectionalLightMultiplier;
half3   _GlobalAdditionalLightMultiplier;
half3   _GlobalAdditionalLightMultiplierForFaceArea;
half3   _GlobalAdditionalLightMultiplierForOutlineArea;
half    _GlobalAdditionalLightApplyRimMask;
half    _GlobalAdditionalLightRimMaskPower;
half    _GlobalAdditionalLightRimMaskSoftness;
half3   _GlobalVolumeBaseColorTintColor;
half3   _GlobalCharacterOverallShadowTintColor;
half    _GlobalCharacterOverallShadowStrength;

half    _GlobalCinematic3DRimMaskEnabled;
half    _GlobalCinematic3DRimMaskStrength_ClassicStyle;
half    _GlobalCinematic3DRimMaskStrength_DynamicStyle;
half    _GlobalCinematic3DRimMaskSharpness_DynamicStyle;
half    _GlobalCinematic3DRimMaskStrength_StableStyle;
half    _GlobalCinematic3DRimMaskSharpness_StableStyle;
half    _GlobalCinematic2DRimMaskStrength;
half    _GlobalCinematicRimTintBaseMap;

half    _GlobalAdditionalLightInjectIntoMainLightColor_Strength;
half    _GlobalAdditionalLightInjectIntoMainLightColor_AllowCloseLightOverBright;
half    _GlobalAdditionalLightInjectIntoMainLightColor_Desaturate;
half    _GlobalAdditionalLightInjectIntoMainLightDirection_Strength;

// global screen space outline settings
float   _GlobalScreenSpaceOutlineIntensityForChar;
float   _GlobalScreenSpaceOutlineWidthMultiplierForChar;
float   _GlobalScreenSpaceOutlineNormalsSensitivityOffsetForChar;
float   _GlobalScreenSpaceOutlineDepthSensitivityOffsetForChar;
float   _GlobalScreenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForChar;
half3   _GlobalScreenSpaceOutlineTintColorForChar;

// global screen space outline V2 settings
float _GlobalScreenSpaceOutlineV2IntensityForChar;
float _GlobalScreenSpaceOutlineV2WidthMultiplierForChar;
float _GlobalScreenSpaceOutlineV2EnableGeometryEdgeForChar;
float _GlobalScreenSpaceOutlineV2GeometryEdgeThresholdForChar;
float _GlobalScreenSpaceOutlineV2EnableNormalAngleEdgeForChar;
float _GlobalScreenSpaceOutlineV2NormalAngleCosThetaThresholdForChar;

// global utility
float   _GlobalShouldDisableNiloToonDepthTextureRimLightAndShadow;

// rendering path related
float   _NiloToonGlobalAllowUnityCameraDepthTextureWriteOutlineExtrudedPosition;

// global debug
float   _GlobalToonShadeDebugCase;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Global Arrays
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Must match: NiloToonSetToonParamPass.k_MaxCharacterCount***** values!
#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES30))
#define MAX_CHARACTER_COUNT 16
#elif defined(SHADER_API_MOBILE) || (defined(SHADER_API_GLCORE) && !defined(SHADER_API_SWITCH)) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) // Workaround because SHADER_API_GLCORE is also defined when SHADER_API_SWITCH is
#define MAX_CHARACTER_COUNT 32
#else
#define MAX_CHARACTER_COUNT 128
#endif

// For properties that will change every frame, we don't put those properties in CBUFFER due to performance cost,
// so we use a global array instead (each character will using _CharacterID as index to get the data for that character)
// this method is similar to how URP get per additional light data from constant size global arrays (256 size light data arrays)
float3  _NiloToonGlobalPerCharFaceForwardDirWSArray[MAX_CHARACTER_COUNT];
float3  _NiloToonGlobalPerCharFaceUpwardDirWSArray[MAX_CHARACTER_COUNT];
float3  _NiloToonGlobalPerCharBoundCenterPosWSArray[MAX_CHARACTER_COUNT];
float3  _NiloToonGlobalPerCharHeadBonePosWSArray[MAX_CHARACTER_COUNT];
half3   _NiloToonGlobalPerCharMainDirectionalLightTintColorArray[MAX_CHARACTER_COUNT];
half3   _NiloToonGlobalPerCharMainDirectionalLightAddColorArray[MAX_CHARACTER_COUNT];

// max array size decided by URP's MAX_VISIBLE_LIGHTS,
// it matches C#'s UniversalRenderPipeline.maxVisibleAdditionalLights
// 16 (low-end mobile)
// 32 (mobile | SWITCH)
// 256 (other)
#if NeedCalculateAdditionalLight
half4   _NiloToonGlobalPerUnityLightDataArray[MAX_VISIBLE_LIGHTS];
half4   _NiloToonGlobalPerUnityLightDataArray2[MAX_VISIBLE_LIGHTS];
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Data Structs
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Passing multiple data between functions can be difficult if the param count is too large,
// so we write some struct to help us organize function calls param
struct UVData
{
    // index 0: UV0
    // index 1: UV1
    // index 2: UV2
    // index 3: UV3
    // index 4: MatCapUV
    // index 5: CharBoundUV
    // index 6: ScreenSpaceUV
    float2 allUVs[7];

    // Performance warning of "dynamic index":
    // dynamic index accessing an array may force the compiler to store the element values in off-chip memory!
    // because you can't do dynamic indexing into registers.
    // if we want the element values store in register for quick read, we need to use compile time constant index.
    // it means a "ifelse chain or switch case that looks stupid" may perform better than a clean code dynamic index array access..!
    // see:
    // - https://developer.nvidia.com/blog/identifying-shader-limiters-with-the-shader-profiler-in-nvidia-nsight-graphics/
    // - https://www.reddit.com/r/opengl/comments/ryig28/struct_switchindexing_with_20_values_faster_than/
    float2 GetUV(uint index)
    {
        // In mobile test (Adreno612,Adreno619,MaliG57MC2):
        // - UVsMethod 0 (constant index) is debug control, it is expected to be the fastest 
        // - UVsMethod 1/2 (ifelse chain / switch) is faster than UVsMethod 3
        // - UVsMethod 3 (dynamic index) is the worst (NiloToonSampleScene)
        // -------------------------------------
        // NiloToon 0.16.16 (Method3, control):
        // - low = 60fps (adreno619), 60fps (MaliG57), 25fps(adreno612)
        // - mid = 38fps (adreno619), 60fps (MaliG57), 14fps(adreno612)
        // - high = 25fps (adreno619), 44fps (MaliG57), 9fps(adreno612)
        // - highest = 8fps (adreno619), 15fps (MaliG57), 3fps(adreno612)
        // -------------------------------------
        // NiloToon 0.16.16 (Method2, switch case):
        // - low = 60fps (adreno619), 60fps (MaliG57), 22fps(adreno612)
        // - mid = 35fps (adreno619), 60fps (MaliG57), 13fps(adreno612)
        // - high = 21fps (adreno619), 41fps (MaliG57), 7fps(adreno612)
        // - highest = 7fps (adreno619), 15fps (MaliG57), 3fps(adreno612)
        // -------------------------------------
        // NiloToon 0.16.16 (Method1, ifelse chain):
        // - low = 60fps (adreno619), 60fps (MaliG57), 23fps(adreno612)
        // - mid = 35fps (adreno619), 60fps (MaliG57), 13fps(adreno612)
        // - high = 21fps (adreno619), 47fps (MaliG57), 7fps(adreno612)
        // - highest = 8fps (adreno619), 11fps (MaliG57), 3fps(adreno612)
        // -------------------------------------
        // NiloToon 0.16.16 (Method0, dynamic index):
        // - low = 60fps (adreno619), 60fps (MaliG57), 22fps(adreno612)
        // - mid = 33fps (adreno619), 60fps (MaliG57), 12fps(adreno612)
        // - high = 17fps (adreno619), 46fps (MaliG57), 6fps(adreno612)
        // - highest = 6fps (adreno619), 10fps (MaliG57), 3fps(adreno612)
        #define UVsMethod 1
        
        #if UVsMethod == 0
            // dynamic index array access, clean code but slower
            return allUVs[index];
        #elif UVsMethod == 1
            // ifelse chain
            if(index == 0) return  allUVs[0];
            if(index == 1) return  allUVs[1];
            if(index == 2) return  allUVs[2];
            if(index == 3) return  allUVs[3];
            if(index == 4) return  allUVs[4];
            if(index == 5) return  allUVs[5];
            if(index == 6) return  allUVs[6];
            return 0;
        #elif UVsMethod == 2
            // switch case
            switch (index)
            {
                case 0: return  allUVs[0];
                case 1: return  allUVs[1];
                case 2: return  allUVs[2];
                case 3: return  allUVs[3];
                case 4: return  allUVs[4];
                case 5: return  allUVs[5];
                case 6: return  allUVs[6];
            }
            return  0;
        #elif UVsMethod == 3
            // constant index array access (debug control)
            return allUVs[0]; 
        #endif
    }
};
// this struct is similar to URP's SurfaceData.hlsl -> "SurfaceData" struct
// Note: don't name it "SurfaceData", because it will conflict with URP's own "SurfaceData" struct in URP's SurfaceData.hlsl
struct ToonSurfaceData
{
    half3   albedo;
    half    alpha;
    half3   emission;
    half    occlusion;
    half3   specular;
    half    specularMask; // store user's remapped specular mask texture, for ramp specular
    half3   normalTS;
    half    smoothness;
};
// this struct is similar to URP's Input.hlsl -> "InputData" struct
// any useful data for lighting will be put into this struct
struct ToonLightingData
{
    float2  uv;

    half3   normalWS;
    half3   normalWS_NoNormalMap;
    float3  positionWS;
    half3   viewDirectionWS;
    float   selfLinearEyeDepth;
    half    averageShadowAttenuation;
    half3   SH;
    half    isFaceArea;
    half    isSkinArea;
    float2  SV_POSITIONxy;
    half3   normalVS;
    half3   reflectionVectorWS;
    half    NdotV;
    half    NdotV_NoNormalMap;
    half    rawURPShadowAttenuation;

#if VaryingsHasTangentWS
    half3x3 TBN_WS;
    half3   viewDirectionTS;
#endif

    half4   vertexColor;

    float   ZOffsetFinalSum;

    float2  normalizedScreenSpaceUV;
    float2  aspectCorrectedScreenSpaceUV;

    float   facing;

    float3  characterBoundBottomWS_positionWS;
    float3  characterBoundTopWS_positionWS;

    float2  characterBound2DRectUV01;

    float2  matcapUV;

    Light mainLight;
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Shared functions across .hlsl(s)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// optimized function to get the UV0 (fastest)
float2 GetUV(Varyings varyings)
{
    return varyings.uv01.xy;
}
// generic function to get any UV (fast)
float2 GetUV(Varyings varyings, uint index)
{
    if(index == 0) return varyings.uv01.xy;
    if(index == 1) return varyings.uv01.zw;
    if(index == 2) return varyings.uv23.xy;
    if(index == 3) return varyings.uv23.zw;

    return 0;
    /*
    // same as the above code, but should be faster
    float2 uvArray[4];
    uvArray[0] = varyings.uv01.xy;
    uvArray[1] = varyings.uv01.zw;
    uvArray[2] = varyings.uv23.xy;
    uvArray[3] = varyings.uv23.zw;
    

    return uvArray[index];
    */
}

// complex function to get a final uv (slow)
float2 CalcUV(UVData uvData, uint index, float4 scaleOffset, float2 scrollSpeed)
{
    return CalcUV(uvData.GetUV(index), scaleOffset, _Time.y, scrollSpeed);
}

// Full complex function to get a final uv(slowest)
float2 CalcUV(UVData uvData, uint index, float4 scaleOffset, float2 scrollSpeed, float rotatedAngle, float rotateSpeed)
{
    return CalcUV(uvData.GetUV(index), scaleOffset, _Time.y, scrollSpeed, rotatedAngle, rotateSpeed);
}

half4 GetCombinedBaseColor()
{
    half4 combinedBaseColor =  _BaseColor * _BaseColor2;
    combinedBaseColor *= _MultiplyBRPColor ? _Color : 1;

    return combinedBaseColor;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// #include .hlsl
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// all lighting equations will be inside this .hlsl,
// just by editing this .hlsl can control most of the visual result.
#include "NiloToonCharacter_LightingEquation.hlsl"

// all NiloToon supported ExternalAsset extension will go here
#include "NiloToonCharacter_ExtendDefinesForExternalAsset.hlsl" 
#include "NiloToonCharacter_ExtendFunctionsForExternalAsset.hlsl"

// if you want to extend this shader without future update merge conflict, you should edit this .hlsl
// it is made for NiloToonURP's user to extend more global features by themselves in an isolated .hlsl file
#include "NiloToonCharacter_ExtendFunctionsForUserCustomLogic.hlsl" 

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// vertex shared functions
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// Return: skin area
// Skin area is mainly for shadow color overriding to a skin shadow color.
half GetSkinArea(float2 uv)
{
    half isSkinArea = _IsSkin;
    
#if _SKIN_MASK_ON
    half skinMask = ExtractSingleChannel(SAMPLE_TEXTURE2D(_SkinMaskMap, sampler_BaseMap, uv), _SkinMaskMapChannelMask);// reuse sampler_BaseMap to save sampler count
    skinMask = PostProcessMaskValue(skinMask, _SkinMaskMapAsIDMap, _SkinMaskMapExtractFromID, _SkinMaskMapInvertColor, _SkinMaskMapRemapStart, _SkinMaskMapRemapEnd);
    isSkinArea *= skinMask;               
#endif

    return isSkinArea;
}
half GetFaceArea(float2 uv)
{
    half isFaceArea = 0;

#if _ISFACE
    #if _FACE_MASK_ON
        // if enabled face mask texture, we treat only white area on _FaceMaskMap is face
        isFaceArea = dot(tex2Dlod(_FaceMaskMap, float4(uv,0,0)), _FaceMaskMapChannelMask);
        isFaceArea = _FaceMaskMapInvertColor? 1-isFaceArea : isFaceArea;
        isFaceArea = invLerpClamp(_FaceMaskMapRemapStart,_FaceMaskMapRemapEnd,isFaceArea);             
    #else
        // if no face mask, we assume the whole material is face when _ISFACE is on
        isFaceArea = 1; 
    #endif
#endif

    return isFaceArea;
}
// output: 
// .xyz  = lightingNormalWS (with face normal edit applied)
// .w    = isFaceArea
half4 GetLightingNormalWS_FaceArea(half3 normalWS, float3 positionWS, float2 uv)
{
    // default use original normalWS
    half3 resultNormalWS = normalWS;

    half isFaceArea = GetFaceArea(uv);

    // disabled NiloToonDepthOnlyOrDepthNormalPass's face normal edit, so the normal in _CameraNormalTexture store the real normal
    // in case any postprocess effect uses normal on face's pixels
#if _ISFACE && !NiloToonDepthOnlyOrDepthNormalPass
    // [apply face normal edit]

    // methodA: by forcing face normal becoming face's forward direction vector
    half3 flatDirectionFixFaceNormalWS = _NiloToonGlobalPerCharFaceForwardDirWSArray[_CharacterID];

    // methodB: by forcing face normal becoming sphere proxy normal, which use _HeadBonePositionWS as proxy sphere center
    half3 proxySphereFixFaceNormalWS = positionWS - _NiloToonGlobalPerCharHeadBonePosWSArray[_CharacterID];

    // let user select methodA or methodB
    half3 fixedFaceNormalWS = lerp(flatDirectionFixFaceNormalWS,proxySphereFixFaceNormalWS,_FixFaceNormalUseFlattenOrProxySphereMethod);

    //fixedFaceNormalWS = proxySphereFixFaceNormalWS; //TODO: this line better for face area additional light

    resultNormalWS = normalize(lerp(resultNormalWS, fixedFaceNormalWS, _FixFaceNormalAmount * isFaceArea * _ControlledByNiloToonPerCharacterRenderController)); // only normalize() once at final to improve performance
#endif

    return half4(resultNormalWS,isFaceArea); 
}
bool IsSmoothedNormalTSAvailableInMeshUV8(Attributes input)
{
    // checks to confirm if uv8 is a correct smoothed normal:
    // 1) is uv8 a unit vector?
    // 2) uv8.xy should never equals to uv4.xy if uv8 is a correctly baked smoothed normal vector.
    // 3) if uv8.z is 0, it is not a baked smooth normal (vrm character do not bake smoothed normal)
    return  (abs(dot(input.uv8,input.uv8)-1.0) < (1.0/255.0)) &&
            all(input.uv8.xy != input.uv4.xy) && 
            (input.uv8.z != 0)
            ;
}
bool ShouldOutlineUseBakedSmoothNormal(bool isSmoothedNormalAvailableInMeshUV8)
{
    // apply material setting by user
    return  isSmoothedNormalAvailableInMeshUV8 && _OutlineUseBakedSmoothNormal;
}
float3 GetPossibleBakedSmoothedNormalWS(VertexNormalInputs vertexNormalInputs, bool isSmoothedNormalTSAvailableInMeshUV8, float3 smoothedNormalTS)
{
    // by default we will use lighting normalWS(regular vertex normal) as extrude direction,
    // it is usable but not perfect due to smoothing group's split normal
    // which will produce discontinue outline for hard edge polygons(e.g. a cube's corner/edge, hair's end vertex)
    float3 extrudeDirectionWS = vertexNormalInputs.normalWS;

    // "world space smoothed normal" is a much better extrude direction for outline than simply using lighting normal(regular vertex normal),
    // because smoothed normal doesn't have any split normal, so the outline will always be continuous(even on a cube), which looks much better. 
    // If we baked "tangent space smoothed normal" in model's uv8, 
    // we can convert it back to world space here and use it as a much better extrude direction for outline
    if(isSmoothedNormalTSAvailableInMeshUV8)
    {
        extrudeDirectionWS = ConvertNormalTSToNormalTargetSpace(smoothedNormalTS, vertexNormalInputs.tangentWS,vertexNormalInputs.bitangentWS, vertexNormalInputs.normalWS);
    }

    return extrudeDirectionWS;
}
float3 TransformPositionWSToOutlinePositionWS(float width, VertexPositionInputs vertexPositionInputs, float3 extrudeDirectionWS)
{
    width *= GetOutlineCameraFovAndDistanceFixMultiplier(vertexPositionInputs.positionVS.z, _CurrentCameraFOV, _GlobalOutlineWidthAutoAdjustToCameraDistanceAndFOV);

    // [normalize length in view space]
    if(_OutlineUniformLengthInViewSpace)
    {
        float3 extrudeDirectionVS = mul(UNITY_MATRIX_V,float4(extrudeDirectionWS,0)).xyz;
        extrudeDirectionVS.z = 0;
        extrudeDirectionVS = normalize(extrudeDirectionVS);
        extrudeDirectionWS = mul(UNITY_MATRIX_I_V, float4(extrudeDirectionVS,0)).xyz;        
    } 

    // [this part is optional]
    // you can make extrude direction normalized in screen space, which will produce screen space constant width outline
    // https://www.videopoetics.com/tutorials/pixel-perfect-outline-shaders-unity/
    // https://github.com/Santarh/MToon/blob/master/MToon/Resources/Shaders/MToonCore.cginc#L90
    // TODO: do we need to support this option?
    // ...

    // [normalize in NDC]
    // this part is wrong, just experimental code
    /*
    float3 extrudeDirectionNDC = mul(UNITY_MATRIX_VP,float4(extrudeDirectionWS,0));
    extrudeDirectionNDC.z = 0;
    extrudeDirectionNDC = normalize(extrudeDirectionNDC) * 12;
    extrudeDirectionWS = mul(UNITY_MATRIX_I_VP, float4(extrudeDirectionNDC,0));
    */

    // produce and return outline vertex position in world space 
    float3 outlinePositionWS = vertexPositionInputs.positionWS + extrudeDirectionWS * width;
    return outlinePositionWS;
}
float ShouldRenderOutline()
{
    return _RenderOutline && _PerCharacterRenderOutline;
}

bool ShouldDisableRendering()
{
    // user can disable rendering per material
    bool shouldDisableRendering = !(_EnableRendering && _RenderCharacter);

    // when _DitherFadeoutAmount or _DissolveAmount is 100% (character is completely not visible due to NiloToonPerCharacterRenderController's dither or dissolve setting),
    // discard all rendering in this vertex shader so we don't pollute URP's shadowmap and depth texture / depth normal texture
    // it also helps improving performance because of skipping the fragment shader
    shouldDisableRendering = shouldDisableRendering ||
            ((_DitherFadeoutAmount == 1) && _AllowPerCharacterDitherFadeout) ||
            ((_DissolveAmount == 1) && _AllowPerCharacterDissolve);

    // when rendering to _CameraColorTexture, this section will allow material's _RenderOutline toggle or per character script's _PerCharacterRenderOutline to control render outline or not, per material.
    // Be careful not to run this section in DepthOnly/DepthNormalsOnly pass, 
    // in DepthOnly pass we only need to skip the outline position extrude, not invalid the vertex!
    // so here only #if NiloToonSelfOutlinePass is used instead of #if NiloToonIsAnyOutlinePass.
    // ---
    // Ideally you want to remove the outline pass completely and not using this trick if the material's _RenderOutline toggle is off for best performance,
    // but it will require a custom material editor inspector, which may make things too complicated without gaining much, 
    // since only a small amount of material will want to NOT render outline, like eyeball,eyebrow,teeth....
#if NiloToonSelfOutlinePass
    shouldDisableRendering = shouldDisableRendering || !ShouldRenderOutline();
#endif

    // Materials's "Pass On/Off" section
    bool shouldRenderingExtraThickOutline = _AllowRenderExtraThickOutlinePass && _ExtraThickOutlineEnabled;
    bool shouldRenderingCharacterAreaColorFill = _CharacterAreaColorFillEnabled;
#if NiloToonCharacterAreaStencilBufferFillPass
    shouldDisableRendering = shouldDisableRendering || (!shouldRenderingExtraThickOutline && !shouldRenderingCharacterAreaColorFill);
#endif
#if NiloToonExtraThickOutlinePass
    shouldDisableRendering = shouldDisableRendering || !shouldRenderingExtraThickOutline;
#endif
#if NiloToonCharacterAreaColorFillPass
    shouldDisableRendering = shouldDisableRendering || !shouldRenderingCharacterAreaColorFill;
#endif    
#if NiloToonShadowCasterPass
    shouldDisableRendering = shouldDisableRendering || !_AllowRenderURPShadowCasterPass;
#endif  
#if NiloToonDepthOnlyOrDepthNormalPass
    shouldDisableRendering = shouldDisableRendering || !_AllowRenderDepthOnlyOrDepthNormalsPass;
#endif
#if NiloToonCharSelfShadowCasterPass
    shouldDisableRendering = shouldDisableRendering || !_AllowRenderNiloToonSelfShadowPass;
#endif

    return shouldDisableRendering;
}
// [All pass's vertex shader will share this function]
// - if "NiloToonIsAnyOutlinePass" is not defined    = do regular MVP transform
// - if "NiloToonIsAnyOutlinePass" is defined        = do regular MVP transform + outline extrude vertex + all outline related task
Varyings VertexShaderAllWork(Attributes input)
{
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // init Varyings struct
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // init output struct with all 0 bits just to avoid "struct not init" warning/error
    // but even passing 0 from vertex to fragment via Varying struct still has Rasterisation/interpolation cost
    // so make sure Varyings struct is as small as possible by using {#if #endif} to remove unneeded things inside Varying struct
    Varyings output = (Varyings)0;
    
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // disable rendering (early exit)
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // if we should not render this material,
    // execute "disable rendering"
    if(ShouldDisableRendering())
    {
        // see [a trick to "delete" any vertex] below to understand what this line does
        output.positionCS.w = 0;
        return output;

        //---------------------------------------------------------------------------------------------------------------------------
        // [a trick to "delete" any vertex]
        // https://forum.unity.com/threads/ignoring-some-triangles-in-a-vertex-shader.170834/#post-5327751

        // if the output vertex's positionCS.w is NaN, GPU will invalid this vertex and all directly connected vertices(Degenerate triangles),
        // we can use "positionCS.w = NaN" to invalid any vertices if needed.

        // 1.this section is a correct and safe implementation, 0.0/0.0 is NaN, which can invalid any target vertex, but will produce "divide by zero" warning
        // { 
        //      output.positionCS.w = 0.0/0.0; //0.0/0.0 is NaN
        //      return output;
        // }

        // 2.this section is a correct and safe implementation, _NaN is NaN from C#, which can invalid any target vertex, and will NOT produce "divide by zero" warning
        // {
        //      // C# -> Shader.SetGlobalFloat("_NaN", System.Single.NaN); 
        //      output.positionCS.w = _NaN;
        //      return output;
        // }

        // 3.this section is a correct and safe implementation, asfloat(0x7fc00000) is NaN, which can invalid any target vertex, and will NOT produce "divide by zero" warning
        // {
        //      output.positionCS = asfloat(0x7fc00000);
        //      return output;
        // }
    
        // 4.this section will work only if you are discarding the whole mesh, but the nice thing is, it will NOT produce any warning, so we choose this for discarding the whole mesh. 
        // {  
        //      output.positionCS.w = 0; // it is 0 already even without writing this line, since Varyings struct was init with all 0 bits, but we still write this line for clarity
        //      return output;
        // } 
        //---------------------------------------------------------------------------------------------------------------------------
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // after invalid/discard vertex, do this part asap.
    // to support GPU instancing and Single Pass Stereo rendering(VR), add the following section
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    UNITY_SETUP_INSTANCE_ID(input);                 // will turn into this in non OpenGL and non PSSL -> UnitySetupInstanceID(input.instanceID);

    // see URP's ShadowCasterPass.hlsl for why these lines are removed for NiloToonCharSelfShadowCasterPass
#if !NiloToonCharSelfShadowCasterPass
    UNITY_TRANSFER_INSTANCE_ID(input, output);      // will turn into this in non OpenGL and non PSSL -> output.instanceID = input.instanceID;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);  // will turn into this in non OpenGL and non PSSL -> output.stereoTargetEyeIndexAsRTArrayIdx = unity_StereoEyeIndex;
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // UV
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // TRANSFORM_TEX is the same as the old shader library.
    
    // is it correct to apply the same _BaseMap_ST uv change to all UVs? maybe not!
    // we should not affect all uv0-uv3 by _BaseMap's tiling offset, it maybe is a wrong design
    // but removing it is a big breaking change, so let's keep it unchanged, and add a "UV Control" group as the alternative control
    output.uv01.xy = TRANSFORM_TEX(input.uv ,_BaseMap);
    output.uv01.zw = TRANSFORM_TEX(input.uv2,_BaseMap);
    output.uv23.xy = TRANSFORM_TEX(input.uv3,_BaseMap);
    output.uv23.zw = TRANSFORM_TEX(input.uv4,_BaseMap);
    
    if(_EnableUVEditGroup)
    {
        output.uv01.xy = CalcUV(output.uv01.xy, _UV0ScaleOffset, _UV0CenterPivotScalePos, _Time.y, _UV0ScrollSpeed, _UV0RotatedAngle, _UV0RotateSpeed);
        output.uv01.zw = CalcUV(output.uv01.zw, _UV1ScaleOffset, _UV1CenterPivotScalePos, _Time.y, _UV1ScrollSpeed, _UV1RotatedAngle, _UV1RotateSpeed);
        output.uv23.xy = CalcUV(output.uv23.xy, _UV2ScaleOffset, _UV2CenterPivotScalePos, _Time.y, _UV2ScrollSpeed, _UV2RotatedAngle, _UV2RotateSpeed);
        output.uv23.zw = CalcUV(output.uv23.zw, _UV3ScaleOffset, _UV3CenterPivotScalePos, _Time.y, _UV3ScrollSpeed, _UV3RotatedAngle, _UV3RotateSpeed);
    }
    
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // insert a performance debug early exit
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // exit as early as possible, to maximize performance difference, which can be used to estimate how expensive this shader is on target device.
#if _NILOTOON_FORCE_MINIMUM_SHADER
    #if NiloToonIsAnyOutlinePass
        output.positionCS = TransformObjectToHClip(input.positionOS + input.normalOS * 0.005); // any visible debug outline width is ok
    #else
        output.positionCS = TransformObjectToHClip(input.positionOS);
    #endif

    return output;    
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Edit Attributes struct by other .hlsl
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // allow NiloToon's developer to support external AssetStore/GitHub assets by editing Attributes struct, 
    // using NiloToonCharacter_ExtendFunctionsForExternalAsset.hlsl (used by NiloToon's developer)
    ApplyExternalAssetSupportLogicToVertexAttributeAtVertexShaderStart(input);
    
    // allow NiloToon's user to edit Attributes struct,
    // using NiloToonCharacter_ExtendFunctionsForUserCustomLogic.hlsl (used by NiloToon's user)
    ApplyCustomUserLogicToVertexAttributeAtVertexShaderStart(input);

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Fill in VertexPositionInputs and VertexNormalInputs utility struct
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // VertexPositionInputs struct contains the position in multiple spaces (world, view, homogeneous clip space, NDC)
    // Unity compiler will strip all unused references (say you don't use view space).
    // Therefore there is more flexibility at no additional cost when using VertexPositionInputs struct.
    VertexPositionInputs vertexPositionInput = GetVertexPositionInputs(input.positionOS);

    // Similar to VertexPositionInputs, VertexNormalInputs will contain normal, tangent and bitangent
    // in world space. If not used it will be stripped.
    // NormalWS and tangentWS are already normalized,
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation if needed
    VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    float3 positionWS = vertexPositionInput.positionWS;
    float3 positionVS = vertexPositionInput.positionVS;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Smoothed normal WS cache
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    bool isSmoothedNormalTSAvailableInMeshUV8 = IsSmoothedNormalTSAvailableInMeshUV8(input);

    float3 possibleSmoothedNormalWS = GetPossibleBakedSmoothedNormalWS(vertexNormalInput, isSmoothedNormalTSAvailableInMeshUV8, input.uv8.xyz); // mesh's uv8 is smoothedNormalTS if baked by NiloToon
    output.smoothedNormalWS = possibleSmoothedNormalWS;
    
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ShouldOutlineUseBakedSmoothNormal cache
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    bool shouldOutlineUseBakedSmoothNormal = ShouldOutlineUseBakedSmoothNormal(isSmoothedNormalTSAvailableInMeshUV8);
    
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Fog
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
#if NiloToonIsAnyLitColorPass
    // we must calculate fogFactor before any positionCS.z's edit (e.g. ZOffset edit)
    const half fogFactor = ComputeFogFactor(vertexPositionInput.positionCS.z);
    output.SH_fogFactor.w = fogFactor;
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Extrude positionWS for outline
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////    
    // extrude positionWS if this pass is any outline pass and this material wants to render outline
#if NiloToonIsAnyOutlinePass
    // Inside NiloToonIsAnyOutlinePass, there are 5 passes.
    
    // if ShouldRenderOutline() is false,
    // - NiloToonSelfOutlinePass already early exit (see "discard rendering (early exit)" in the above section)
    // - ExtraThickOutlinePass will ignore ShouldRenderOutline()
    // so only:
    // - NiloToonDepthOnlyOrDepthNormalPass || NiloToonPrepassBufferPass || NiloToonCharacterAreaStencilBufferFillPass
    // will need to include the following if() section, 
    // these passes should be always in sync, since NiloToonPrepassBuffer pass rely on _CameraDepthTexture (DepthOnly | DepthNormalsOnly)
    // else the depth comparision test in NiloToonPrepassBuffer pass will be incorrect.
    // * NiloToonCharacterAreaStencilBufferFillPass should still be included here, but does not care any depth texture related checks
    #if NiloToonDepthOnlyOrDepthNormalPass || NiloToonPrepassBufferPass || NiloToonCharacterAreaStencilBufferFillPass
    // In this section, "Character non-color passes" means these passes:
    // - DepthOnly | DepthNormalsOnly
    // - NiloToonPrepassBuffer
    // - NiloToonCharacterAreaStencilBufferFill
    
    // 1)If ShouldRenderOutline() is false, 
    // no need to extrude in "Character non-color passes", simple to understand.
    
    // 2)If shouldOutlineUseBakedSmoothNormal is false,
    // which means we are trying to use split normal for extrude.
    // Due to smoothing group's regular split normal, TransformPositionWSToOutlinePositionWS() may produce holes between polygons after extrude,
    // which pollute "Character non-color passes" (e.g., pollute _CameraDepthTexture, which makes depth texture has holes, lead to 2D rim light producing bad result).
    // so when the extrude normal is split normal, it is better NOT doing TransformPositionWSToOutlinePositionWS() if shouldOutlineUseBakedSmoothNormal is false for these "Character non-color passes"
    
    // 3)If per material
    // - _UnityCameraDepthTextureWriteOutlineExtrudedPosition 
    // is false (default is false),
    // which means user want to skip this section, usually user will keep turn off _UnityCameraDepthTextureWriteOutlineExtrudedPosition in material to avoid ugly 2D rim light artifact appeared

    // 4)If global uniform 
    //   - _NiloToonGlobalAllowUnityCameraDepthTextureWriteOutlineExtrudedPosition
    // is false, it means we are in deferred rendering mode, where true depth texture write is a must.
    // we have no choice but to write the true depth without any extrude
    if( ShouldRenderOutline()
        #if !NiloToonCharacterAreaStencilBufferFillPass
        && shouldOutlineUseBakedSmoothNormal
        && _UnityCameraDepthTextureWriteOutlineExtrudedPosition
        && _NiloToonGlobalAllowUnityCameraDepthTextureWriteOutlineExtrudedPosition 
        #endif
        )
    #endif
    {
        float finalOutlineWidth = _OutlineWidth * _OutlineWidthExtraMultiplier * _PerCharacterOutlineWidthMultiply * _GlobalOutlineWidthMultiplier;

        // outline width mul from texture
        #if _OUTLINEWIDTHMAP
            float outlineWidthTex2DlodExplicitMipLevel = 0;
            float4 outlineWidthTexReadValueRGBA = tex2Dlod(_OutlineWidthTex, float4(input.uv,0,outlineWidthTex2DlodExplicitMipLevel));
            float outlineWidthMultiplierByTex = dot(outlineWidthTexReadValueRGBA,_OutlineWidthTexChannelMask);
            finalOutlineWidth *= outlineWidthMultiplierByTex;           
        #endif

        // outline width mul from vertex color
        finalOutlineWidth = _UseOutlineWidthMaskFromVertexColor? finalOutlineWidth * dot(input.color, _OutlineWidthMaskFromVertexColor) : finalOutlineWidth;

        // ExtraThickOutlinePass will affect finalOutlineWidth at last, to ensure width control is isolated
        #if NiloToonExtraThickOutlinePass || NiloToonDepthOnlyOrDepthNormalPass

            #if NiloToonDepthOnlyOrDepthNormalPass
            if(_ExtraThickOutlineEnabled && _ExtraThickOutlineZWrite && _ExtraThickOutlineWriteIntoDepthTexture)
            #endif
            {
                // apply by add, not mul, to make extra thick outline's width has it's own isolated width
                finalOutlineWidth += _ExtraThickOutlineWidth;
                // if user used different outline width per material, 
                // here we expose min(x,_ExtraThickOutlineMaxFinalWidth) for user to have a uniform final ExtraThickOutline width 
                finalOutlineWidth = min(finalOutlineWidth, _ExtraThickOutlineMaxFinalWidth);
            } 
        #endif

        if(_GlobalShouldDisableNiloToonZOffset)
        {
            // in planar reflection pass, _GlobalShouldDisableNiloToonZOffset will set to true (= ZOffset is disabled),
            // so we don't render face's outline, else ugly outline will appear in planar reflection pass
            #if _ISFACE
                // see [a trick to "delete" any vertex] about this section's logic
                output.positionCS.w = 0;
                return output;
            #endif

            // https://docs.unity3d.com/ScriptReference/Rendering.CullMode.html
            // 0 is Off
            // 1 is Front
            // 2 is Back
            // in planar reflection pass, _GlobalShouldDisableNiloToonZOffset will set to true (= ZOffset is disabled),
            // so don't render Cull off material's outline
            // only Cull Off has problem, so only disable outline when Cull Off
            finalOutlineWidth = _Cull == 0 ? 0 : finalOutlineWidth;
        }

        // do positionWS extrude for outline
        positionWS = TransformPositionWSToOutlinePositionWS(finalOutlineWidth, vertexPositionInput, shouldOutlineUseBakedSmoothNormal ? possibleSmoothedNormalWS : vertexNormalInput.normalWS);

        // do ExtraThickOutline's view space pos offset(in world space)
        #if NiloToonExtraThickOutlinePass
            // transform view space position offset to world space, apply offset in world space
            positionWS += mul((float3x3)UNITY_MATRIX_I_V, _ExtraThickOutlineViewSpacePosOffset).xyz;
        #endif
    }        
#endif

#if _NILOTOON_DEBUG_SHADING
    output.uv8 = input.uv8; // for showing tangent space smoothed normal as debug color, in fragment shader
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // PositionWS
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    output.positionWS_ZOffsetFinalSum.xyz = positionWS;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // TangentWS
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if VaryingsHasTangentWS
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(vertexNormalInput.tangentWS.xyz, sign);
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // PositionCS
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // we can't reuse vertexPositionInput.positionCS directly, because positionWS maybe edited in "Extrude positionWS for outline" section,
    // hence we will need to recompute positionCS using TransformWorldToHClip() to ensure positionCS's correctness

    #if !NiloToonCharSelfShadowCasterPass
    output.positionCS = TransformWorldToHClip(positionWS);
    #else
    // in XR, VP matrix will be overridden by URP, you can't edit it via cmd.SetViewProjectionMatrices, so we need to use our own VP matrix
    output.positionCS = mul(_NiloToonSelfShadowWorldToClip, float4(positionWS,1));
    #endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Find out face area, and edit normalWS if vertex is within face area
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    half4 lightingNormalWS_faceArea = GetLightingNormalWS_FaceArea(vertexNormalInput.normalWS, positionWS, GetUV(output));
    output.normalWS_averageShadowAttenuation.xyz = lightingNormalWS_faceArea.xyz; //normalized already by GetLightingNormalWS_FaceArea(...)

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // viewDirTS
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // note: put this section after all require vectors by GetViewDirectionTangentSpace() are ready
#if VaryingsHasViewDirTS
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexPositionInput.positionWS); 
    half3 viewDirTS = GetViewDirectionTangentSpace(output.tangentWS, output.normalWS_averageShadowAttenuation.xyz, viewDirWS);
    output.viewDirTS = viewDirTS;
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ZOffset sum init       
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    float ZOffsetFinalSum = 0;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Outline ZOffset
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // this section will apply clip space ZOffset (view space unit input) to outline position
    // doing this can hide ugly/unwanted outline, usually we will apply Outline ZOffset for face/eye
    // Only apply to _CameraColorTexture's outline pass(NiloToonSelfOutlinePass), don't apply to any depth texture pass
#if NiloToonSelfOutlinePass
    // we have separated settings for "face" and "not face" vertices 
    float outlineZOffset = lerp(_OutlineZOffset,_OutlineZOffsetForFaceArea,lightingNormalWS_faceArea.w);

    // [ZOffset mask from vertex color]
    outlineZOffset *= _UseOutlineZOffsetMaskFromVertexColor? dot(input.color, _OutlineZOffsetMaskFromVertexColor) : 1;

    // [ZOffset mask texture]
    #if _OUTLINEZOFFSETMAP
        float outlineZOffsetMask = 1;

        // [Read ZOffset mask texture]
        // we can't use tex2D() in vertex shader because ddx & ddy is unknown before rasterization, 
        // so use tex2Dlod() with an explicit mip level 0, you need to put explicit mip level 0 inside the 4th component of tex2Dlod()'s' input param
        float outlineZOffsetTex2DlodExplicitMipLevel = 0;
        outlineZOffsetMask = dot(tex2Dlod(_OutlineZOffsetMaskTex, float4(input.uv,0,outlineZOffsetTex2DlodExplicitMipLevel)),_OutlineZOffsetMaskTexChannelMask);

        // [Remap ZOffset texture value]
        // flip texture read value so default black area = apply ZOffset, because usually outline mask texture are using this format(black = hide outline, white = do nothing)
        outlineZOffsetMask = 1 - outlineZOffsetMask;
        // [allow user to invert again if needed]
        outlineZOffsetMask = _OutlineZOffsetMaskTexInvertColor ? 1-outlineZOffsetMask : outlineZOffsetMask;
        outlineZOffsetMask = invLerpClamp(_OutlineZOffsetMaskRemapStart,_OutlineZOffsetMaskRemapEnd,outlineZOffsetMask);// allow user to remap

        // [Apply ZOffset, Use remapped value as ZOffset mask]
        outlineZOffset *= outlineZOffsetMask;           
    #endif

    outlineZOffset += _OutlineBaseZOffset;

    // this line make ZOffset sync with camera distance corrected outline width
    // If we don't do this, once camera is far away, zoffset will become not enough because outline width keep growing larger
    // also stop reduce zoffset when camera is too close using max(1,x)
    // TODO: we should share the GetOutlineCameraFovAndDistanceFixMultiplier() call to save some performance?  
    outlineZOffset *= max(1,GetOutlineCameraFovAndDistanceFixMultiplier(positionVS.z, _CurrentCameraFOV, _GlobalOutlineWidthAutoAdjustToCameraDistanceAndFOV) / 0.0025); 

    ZOffsetFinalSum += -outlineZOffset;
#endif

#if NiloToonExtraThickOutlinePass
    ZOffsetFinalSum += -_ExtraThickOutlineZOffset;
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // URP Shadow bias (matching URP16's ShadowCasterPass.hlsl)
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ShadowCaster pass needs special process to edit positionCS, else shadow artifact will appear
    // see GetShadowPositionHClip() in URP/Shaders/ShadowCasterPass.hlsl
#if NiloToonShadowCasterPass
    // Why not just call URP's GetShadowPositionHClip() directly?
    // because it will require us to include ShadowCasterPass.hlsl
    // which will include ShadowCasterPass.hlsl's struct Attributes, it is not what we want

    // this part(PUNCTUAL_LIGHT_SHADOW) exists in URP12 or above's GetShadowPositionHClip()
    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
        float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
        float3 lightDirectionWS = _LightDirection;
    #endif

    float3 normalWSForShadowCaster = possibleSmoothedNormalWS; // replace to smoothed normal if possible
    
    // normalBias will produce "holes" in shadowmap, so we add a slider to control it globally
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWSForShadowCaster * _GlobalToonShaderNormalBiasMultiplier, lightDirectionWS));

    #if UNITY_REVERSED_Z
        positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
        positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif

    output.positionCS = positionCS;
    return output; //return, since later calculations should be not related to shadow caster pass anymore 
#endif
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // NiloToon Self Shadow caster pass's Shadow bias (matching URP16's ShadowCasterPass.hlsl)
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // NiloToon's self ShadowCaster pass needs special process to edit positionCS, else shadow artifact will appear
#if NiloToonCharSelfShadowCasterPass
    {
        // TODO: improve shadow bias using: https://zhuanlan.zhihu.com/p/370951892
        //------------------------------------------------------------
        // settings     
        float depthBias = min(_NiloToonGlobalSelfShadowCasterDepthBias + -_NiloToonSelfShadowMappingDepthBias * _EnableNiloToonSelfShadowMappingDepthBias,0);
        float normalBias = min(_NiloToonGlobalSelfShadowCasterNormalBias + -_NiloToonSelfShadowMappingNormalBias * _EnableNiloToonSelfShadowMappingNormalBias,0);
        //------------------------------------------------------------

        // referencing GetShadowPositionHClip() in URP/Shaders/ShadowCasterPass.hlsl
        float3 positionWS = vertexPositionInput.positionWS;
        float3 normalWS = possibleSmoothedNormalWS; // replace to smoothed normal will produce better result

        float3 lightDirection = _NiloToonSelfShadowLightDirection; // pointing from vertex to light

        // copy from URP/ShaderLibrary/Shadows.hlsl's ApplyShadowBias(...)
        //-------------------------------------------------------------
        float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
        float scale = invNdotL * normalBias;

        // normal bias is negative since we want to apply an inset normal offset
        positionWS = lightDirection * depthBias.xxx + positionWS;
        positionWS = normalWS * scale.xxx + positionWS;
        //-------------------------------------------------------------

        // copy from URP/Shaders/ShadowCasterPass.hlsl's GetShadowPositionHClip(...)
        //-------------------------------------------------------------
        #if !NiloToonCharSelfShadowCasterPass
        float4 positionCS = TransformWorldToHClip(positionWS);
        #else
        // in XR, VP matrix will be overridden by URP, you can't edit it via cmd.SetViewProjectionMatrices, so we need to use our own VP matrix
        float4 positionCS = mul(_NiloToonSelfShadowWorldToClip, float4(positionWS,1));
        #endif
        
        #if UNITY_REVERSED_Z
            positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
        #else
            positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
        #endif

        output.positionCS = positionCS;
        return output; //return, since later calculations should be not related to shadow caster pass anymore 
        //-------------------------------------------------------------
    }
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ZOffset
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    // per material ZOffset (originally created to push eyebrow over hair / face expression mesh (e.g >///<) over face like a alpha blend decal/sticker)
#if NiloToonIsAnyLitColorPass
    if(_ZOffsetEnable)
    {
        float zoffset = -_ZOffset;

        // let ZOffset affect outline pass also, with extra control
        #if NiloToonSelfOutlinePass
            zoffset *= _ZOffsetMultiplierForTraditionalOutlinePass;
        #endif

        #if _ZOFFSETMAP
            float zOffsetTex2DlodExplicitMipLevel = 0;
            float zOffsetMultiplierByTex = dot(_ZOffsetMaskMapChannelMask,tex2Dlod(_ZOffsetMaskTex, float4(input.uv,0,zOffsetTex2DlodExplicitMipLevel)));
            zOffsetMultiplierByTex = _ZOffsetMaskMapInvertColor ? 1-zOffsetMultiplierByTex : zOffsetMultiplierByTex;
            zoffset *= zOffsetMultiplierByTex;
        #endif

        ZOffsetFinalSum += zoffset;        
    }
#endif

    // per character ZOffset
#if ApplyZOffset
    ZOffsetFinalSum += -_PerCharZOffset;
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Vertex color
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    output.color = input.color;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // SH (indirect from lightprobe) 
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // *moved from fragment shader to vertex shader for performance reason, 
    // it is ok due to lightprobe's low frequency result color data

    // [calculate SH in vertex shader to save some cycles]
    // If we call SampleSH(0), this will hide all 3D feeling by ignoring all detail SH.
    // We default only use the constant term, = SampleSH(0)
    // because we just want to get some average envi indirect color only.
    // Hardcode 0 can enable compiler optimization to remove no-op,
    // but here we don't hardcode 0, instead we use a uniform variable(_IndirectLightFlatten) to control the normal,
    // which is slower but allow more flexibility for user
#if NiloToonIsAnyLitColorPass
    half3 normalWSForSH = lightingNormalWS_faceArea.xyz;
    normalWSForSH *= 1-_IndirectLightFlatten; // make the normal become 0 when _IndirectLightFlatten is 1

    half3 SH = SampleSH(normalWSForSH) * _GlobalIndirectLightMultiplier;
    output.SH_fogFactor.rgb = SH;
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Average Shadow Attenuation
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // use LOAD_TEXTURE2D instead of tex2dLOD for simplify uv code and also for better performance
    // _NiloToonAverageShadowMapRT is a "N x 1" texture, so LOAD index is (charID,0)
#if NiloToonIsAnyLitColorPass
    if(_ControlledByNiloToonPerCharacterRenderController)
    {
        output.normalWS_averageShadowAttenuation.w = lerp(1,LOAD_TEXTURE2D(_NiloToonAverageShadowMapRT,float2(_CharacterID,0)).r,_PerCharReceiveAverageURPShadowMap); // floating point RT, so r channel
    }
    else
    {
        output.normalWS_averageShadowAttenuation.w = 1;
    }
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Face's depth texture ZOffset
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // for face vertices, use ZOffset to push back depth write to _CameraDepthTexture (not _CameraColorTexture), 
    // doing this can:
    // - prevent face cast 2D depth texture self shadow on face (which looks like artifact)
    // - make hair/hat/sunglass... easier to cast 2D depth texture shadow on face
#if NiloToonDepthOnlyOrDepthNormalPass && _ISFACE
    float cameraDepthTextureZOffsetMask = 1;
    #if NeedFaceMaskArea
        cameraDepthTextureZOffsetMask = lightingNormalWS_faceArea.w;
    #endif
    // zoffset should be always greater or equal to depthDiffThreshold, to avoid face cast 2D depth texture self shadow
    ZOffsetFinalSum += -_FaceAreaCameraDepthTextureZWriteOffset * cameraDepthTextureZOffsetMask; 
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Apply ZOffset
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // The reason to only apply ZOffset(NiloGetNewClipPosWithZOffsetVS()) only once per vertex shader, instead of multiple times, 
    // is to reduce precision loss.
    // Precision loss exists if we call NiloGetNewClipPosWithZOffsetVS() multiple times in the same vertex shader.
#if ApplyZOffset
    output.positionCS = NiloGetNewClipPosWithZOffsetVS(output.positionCS, ZOffsetFinalSum); 
    output.positionWS_ZOffsetFinalSum.w = ZOffsetFinalSum;
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Remove perspective camera distortion
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if ApplyPerspectiveRemoval
    output.positionCS = NiloDoPerspectiveRemoval(output.positionCS,positionWS,_NiloToonGlobalPerCharHeadBonePosWSArray[_CharacterID],_PerspectiveRemovalRadius,_PerspectiveRemovalAmount, _PerspectiveRemovalStartHeight, _PerspectiveRemovalEndHeight);
#endif

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // User custom logic
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    ApplyCustomUserLogicToVertexShaderOutputAtVertexShaderEnd(output, input);
    
    return output;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Above is vertex shader section, below is fragment shader section
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step1: if DEBUG, run these DEBUG functions and early exit at fragment functions's early start point)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
half4 Get_NILOTOON_FORCE_MINIMUM_FRAGMENT_SHADER_result(Varyings input)
{
    half3 debugColor = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap, GetUV(input)).rgb;
    #if NiloToonIsAnyOutlinePass
        debugColor *= 0.25; // any value is valid, we just want to darken outline a bit
    #endif
    return half4(debugColor,1);   
}
half4 Get_NILOTOON_DEBUG_SHADING_result(Varyings input, ToonSurfaceData surfaceData, ToonLightingData lightingData, UVData uvData)
{
#if _NILOTOON_DEBUG_SHADING
    // Performance is not important here, since it is a debug shader function.

    // if _NILOTOON_DEBUG_SHADING is active, we only want to render NiloToonForwardLitPass
    #if !NiloToonForwardLitPass
    {
        clip(-1);
        return 0;
    }
    #endif

    // Example of switch case in hlsl:
    // https://github.com/SickheadGames/HL2GLSL/blob/master/tests/HL2GLSL/FlowControl.hlsl
    switch(_GlobalToonShadeDebugCase)
    {
        case 0:     return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap, lightingData.uv);
        case 1:     return half4(surfaceData.albedo, surfaceData.alpha);
        case 2:     return 1;
        case 3:     return half4(surfaceData.occlusion.xxx,1);
        case 4:     return half4(surfaceData.emission,1);
        case 5:     return half4(lightingData.normalWS * 0.5 + 0.5,1); // remap to match URP's normalWS in URP Rendering Debugger
        case 6:     return half4(lightingData.uv,0,1);
        case 7:     return half4(input.color.r.xxx,1);
        case 8:     return half4(input.color.g.xxx,1);
        case 9:     return half4(input.color.b.xxx,1);
        case 10:    return half4(input.color.a.xxx,1);
        case 11:    return half4(input.color.rgb,1);
        case 12:    return half4(surfaceData.specular.rgb * surfaceData.specularMask,1);
        case 13:    return half4(input.uv8 * 0.5 + 0.5,1);
        case 14:    return half4(surfaceData.albedo * dot(lightingData.normalWS,lightingData.viewDirectionWS), surfaceData.alpha);
        case 15:    return half4(lightingData.isFaceArea.xxx,1);
        case 16:    return half4(lightingData.isSkinArea.xxx,1);
        case 17:    return half4(surfaceData.alpha.xxx,1);
        case 18:    return half4(lightingData.normalVS,1);
        case 19:    return half4(lightingData.averageShadowAttenuation.xxx,1);
        case 20:    return half4(lightingData.SH,1);
        case 21:    return half4(uvData.GetUV(0),0,1);
        case 22:    return half4(uvData.GetUV(1),0,1);
        case 23:    return half4(uvData.GetUV(2),0,1);
        case 24:    return half4(uvData.GetUV(3),0,1);
        case 25:    return half4(input.SH_fogFactor.w.xxx,1);
        case 26:    return half4(lightingData.ZOffsetFinalSum.xxx,1);
        case 27:    return half4(lightingData.viewDirectionWS,1);
        case 28:    return half4(surfaceData.normalTS * 0.5 + 0.5,1); // remap to match URP's normalTS in URP Rendering Debugger
        case 29:    return half4(surfaceData.smoothness.xxx,1);
        case 30:    return half4(lightingData.facing.xxx,1);
        case 31:    return half4(lightingData.characterBound2DRectUV01,0,1);
        case 32:    return half4(lightingData.matcapUV * 0.5 + 0.5,0,1);
    }
#endif

    return 1;    
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step1.5: edit uv by parallax map)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// copy and modified using URP's LitInput.hlsl -> ApplyPerPixelDisplacement(...)
void ApplyPerPixelDisplacement(inout Varyings input)
{
    // Note: this section will affect detail uv
#if _PARALLAXMAP
    float2 uvoffset = ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), input.viewDirTS, _Parallax, GetUV(input,_ParallaxSampleUVIndex));
    switch(_ParallaxApplyToUVIndex)
    {
        case 0: input.uv01.xy += uvoffset; break;
        case 1: input.uv01.zw += uvoffset; break;
        case 2: input.uv23.xy += uvoffset; break;
        case 3: input.uv23.zw += uvoffset; break;        
    }
#endif
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step2: prepare data structs for lighting calculation)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if AnyBaseMapStackingLayerEnabled
half4 GetStackingLayerRGBAResult(
    float2 layerTexUV, 
    float4 layerTexUVTilingOffset,
    float4 layerTexUVCenterPivotScalePos,
    float2 layerTexUVScrollSpeed,
    float layerTexRotatedAngle,
    float layerTexRotateSpeed,
    half4 layerTexTintColor,
    half4 layerMaskTexChannel,
    float2 layerMaskUV,
    Texture2D layerTex,
    Texture2D layerMaskTex,
    SamplerState layerTexSamplerState,
    float facing,
    bool layerTexIgnoreAlpha,
    float applyToFaces,
    bool invertMask,
    bool layerMaskAsID,
    half layerMaskExtractFromID,
    half layerMaskRemapStart,
    half layerMaskRemapEnd
    )
{
    // UV's old method (in NiloToon 0.15.x or below)
    //float2 stackingLayerUV = (uv + layerTexUVScaleOffset.zw + frac(_Time.yy * layerTexUVScrollSpeed)) * layerTexUVScaleOffset.xy;

    // UV's new method (in NiloToon 0.16.0 or above, match lilToon's logic)
    // (the result is different to the old method, it is an intended breaking change in order to match lilToon)
    float2 stackingLayerUV = CalcUV(
        layerTexUV,
        layerTexUVTilingOffset,
        layerTexUVCenterPivotScalePos,
        _Time.y,
        layerTexUVScrollSpeed,
        layerTexRotatedAngle,
        layerTexRotateSpeed);

    // Note:
    // "The Team Color Problem" by Ben Golus
    // It is possible to improve the result by using "Pre-multiplied Color",
    // but it will require the user to prepare special textures for it, which means it is not practical for NiloToon
    // https://bgolus.medium.com/the-team-color-problem-b70ec69d109f
    
    // Sample Texture, sharing the sampler to avoid sampler max count = 16 limit
    half4 stackingLayerRGBAResult = SAMPLE_TEXTURE2D(layerTex, layerTexSamplerState, stackingLayerUV);
    if(layerTexIgnoreAlpha)
    {
        stackingLayerRGBAResult.a = 1;
    }
    stackingLayerRGBAResult *= layerTexTintColor;

    // for mask texture, we reuse _BaseMap's sampler to reduce sampler count since in most cases the setting should be the same 
    half stackingLayerMask = ExtractSingleChannel(SAMPLE_TEXTURE2D(layerMaskTex, sampler_BaseMap, layerMaskUV), layerMaskTexChannel);
    stackingLayerMask = PostProcessMaskValue(stackingLayerMask,layerMaskAsID,layerMaskExtractFromID,invertMask,layerMaskRemapStart,layerMaskRemapEnd);
    stackingLayerRGBAResult.a *= stackingLayerMask;

    // render face mask
    if(applyToFaces != 0)
    {
        if(applyToFaces == 1) stackingLayerRGBAResult.a *= saturate(-facing);
        if(applyToFaces == 2) stackingLayerRGBAResult.a *= saturate(facing);
    }

    return stackingLayerRGBAResult;
}

void ApplyStackingLayer(inout half4 baseLayerRGBA, half4 topLayerRGBA, uint colorBlendMode, half applyStrength, bool isOpaque)
{
    topLayerRGBA.a *= applyStrength;

    // to prevent any opaque material using basemap with 0 alpha causing bug in NormalRGBA blending
    if(isOpaque && colorBlendMode == 0)
    {
        // convert NormalRGBA -> NormalRGB
        colorBlendMode = 1;
    }
    
    baseLayerRGBA = BlendColor(baseLayerRGBA, topLayerRGBA, colorBlendMode);
}
#endif

half4 GetFinalBaseColor(Varyings input, UVData uvData, float facing)
{
    half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvData.GetUV(_BaseMapUVIndex));

#if _ALPHAOVERRIDEMAP
    half alphaOverrideStrength = _AlphaOverrideStrength;
    if(_ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCamera > 0)
    {
        // copied from https://docs.unity3d.com/Packages/com.unity.shadergraph@12.1/manual/Camera-Node.html?q=camera%20node
        half3 cameraBackwardDir =  mul(UNITY_MATRIX_M, float4(transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V)) [2].xyz,0)).xyz;

        half faceForwardFadeoutControl = invLerpClamp(_ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCameraRemapStart, _ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCameraRemapEnd,dot(_NiloToonGlobalPerCharFaceForwardDirWSArray[_CharacterID], cameraBackwardDir)); // find the angle between faceForward vector and cameraBackwardDir
        faceForwardFadeoutControl = pow(faceForwardFadeoutControl,4); // fadeout faster
        faceForwardFadeoutControl = lerp(1,faceForwardFadeoutControl, _ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCamera); // allow user to control it
        alphaOverrideStrength *= faceForwardFadeoutControl;
    }
    half newAlpha = dot(tex2D(_AlphaOverrideTex, uvData.GetUV(_AlphaOverrideTexUVIndex)),_AlphaOverrideTexChannelMask);
    newAlpha = _AlphaOverrideTexInvertColor ? 1-newAlpha : newAlpha;
    newAlpha = saturate(newAlpha * _AlphaOverrideTexValueScale + _AlphaOverrideTexValueOffset); // alpha MAD of liltoon
    
    if(_AlphaOverrideMode == 0)
    {
        // Mode: Replace
        color.a = lerp(color.a,newAlpha,alphaOverrideStrength);
    }
    else if(_AlphaOverrideMode == 1)
    {
        // Mode: Multiply
        color.a = color.a * lerp(1,newAlpha,alphaOverrideStrength);
    }
    else if(_AlphaOverrideMode == 2)
    {
        // Mode: Add
        color.a = saturate(color.a + newAlpha * alphaOverrideStrength);
    }
    else
    {
        // Mode: Subtract
        color.a = saturate(color.a - newAlpha * alphaOverrideStrength);
    }
#endif

    color *= GetCombinedBaseColor(); // edit color's rgba(including alpha also, not just rgb), since _BaseColor/_BaseColor2/_Color is per material, using _BaseColor/_BaseColor2/_Color's alpha to control rendering alpha should be intentional by user
    color.rgb *= _BaseMapBrightness;

    // to prevent opaque material with 0 alpha to use RGBA normal blending
    bool isOpaque = (_SrcBlend == 1) && (_DstBlend == 0);
    #if _ALPHATEST_ON
    isOpaque = isOpaque && (_Cutoff <= 0);
    #endif
    
    // add 10 photoshop layer, after _BaseMap's edit finished.
    // This is designed for face makeup, decal, logo, tattoo etc
#if _BASEMAP_STACKING_LAYER1
    {
        const half4 layer1Content = GetStackingLayerRGBAResult(
            uvData.GetUV(_BaseMapStackingLayer1TexUVIndex),
            _BaseMapStackingLayer1TexUVScaleOffset,
            _BaseMapStackingLayer1TexUVCenterPivotScalePos,
            _BaseMapStackingLayer1TexUVAnimSpeed,
            _BaseMapStackingLayer1TexUVRotatedAngle,
            _BaseMapStackingLayer1TexUVRotateSpeed,
            _BaseMapStackingLayer1TintColor,
            _BaseMapStackingLayer1MaskTexChannel,
            uvData.GetUV(_BaseMapStackingLayer1MaskUVIndex),
            _BaseMapStackingLayer1Tex,
            _BaseMapStackingLayer1MaskTex,
            sampler_BaseMapStackingLayer1Tex, // only the first 2 layers use it's own sampler, to allow more control for user
            facing,
            _BaseMapStackingLayer1TexIgnoreAlpha,
            _BaseMapStackingLayer1ApplytoFaces,
            _BaseMapStackingLayer1MaskInvertColor,
            _BaseMapStackingLayer1MaskTexAsIDMap,
            _BaseMapStackingLayer1MaskTexExtractFromID,
            _BaseMapStackingLayer1MaskRemapStart,
            _BaseMapStackingLayer1MaskRemapEnd);
        ApplyStackingLayer(color, layer1Content, _BaseMapStackingLayer1ColorBlendMode, _BaseMapStackingLayer1MasterStrength, isOpaque);
    }
#endif

#if _BASEMAP_STACKING_LAYER2
    {
        const half4 layer2Content = GetStackingLayerRGBAResult(
            uvData.GetUV(_BaseMapStackingLayer2TexUVIndex),
            _BaseMapStackingLayer2TexUVScaleOffset,
            _BaseMapStackingLayer2TexUVCenterPivotScalePos,
            _BaseMapStackingLayer2TexUVAnimSpeed,
            _BaseMapStackingLayer2TexUVRotatedAngle,
            _BaseMapStackingLayer2TexUVRotateSpeed,
            _BaseMapStackingLayer2TintColor,
            _BaseMapStackingLayer2MaskTexChannel,
            uvData.GetUV(_BaseMapStackingLayer2MaskUVIndex),
            _BaseMapStackingLayer2Tex,
            _BaseMapStackingLayer2MaskTex,
            sampler_BaseMapStackingLayer2Tex, // only the first 2 layers use it's own sampler, to allow more control for user
            facing,
            _BaseMapStackingLayer2TexIgnoreAlpha,
            _BaseMapStackingLayer2ApplytoFaces,
            _BaseMapStackingLayer2MaskInvertColor,
            _BaseMapStackingLayer2MaskTexAsIDMap,
            _BaseMapStackingLayer2MaskTexExtractFromID,
            _BaseMapStackingLayer2MaskRemapStart,
            _BaseMapStackingLayer2MaskRemapEnd);
        ApplyStackingLayer(color, layer2Content, _BaseMapStackingLayer2ColorBlendMode, _BaseMapStackingLayer2MasterStrength, isOpaque);
    }
#endif
#if _BASEMAP_STACKING_LAYER3
    {
        const half4 layer3Content = GetStackingLayerRGBAResult(
            uvData.GetUV(_BaseMapStackingLayer3TexUVIndex),
            _BaseMapStackingLayer3TexUVScaleOffset,
            _BaseMapStackingLayer3TexUVCenterPivotScalePos,
            _BaseMapStackingLayer3TexUVAnimSpeed,
            _BaseMapStackingLayer3TexUVRotatedAngle,
            _BaseMapStackingLayer3TexUVRotateSpeed,
            _BaseMapStackingLayer3TintColor,
            _BaseMapStackingLayer3MaskTexChannel,
            uvData.GetUV(_BaseMapStackingLayer3MaskUVIndex),
            _BaseMapStackingLayer3Tex,
            _BaseMapStackingLayer3MaskTex,
            sampler_linear_clamp, // for layer 3-10, we hardcode sampler_linear_clamp or sampler_linear_repeat to avoid adding more sampler count
            facing,
            _BaseMapStackingLayer3TexIgnoreAlpha,
            _BaseMapStackingLayer3ApplytoFaces,
            _BaseMapStackingLayer3MaskInvertColor,
            _BaseMapStackingLayer3MaskTexAsIDMap,
            _BaseMapStackingLayer3MaskTexExtractFromID,
            _BaseMapStackingLayer3MaskRemapStart,
            _BaseMapStackingLayer3MaskRemapEnd);
        ApplyStackingLayer(color, layer3Content, _BaseMapStackingLayer3ColorBlendMode, _BaseMapStackingLayer3MasterStrength, isOpaque);
    }
#endif
#if _BASEMAP_STACKING_LAYER4
    {
        const half4 layer4Content = GetStackingLayerRGBAResult(
            uvData.GetUV(_BaseMapStackingLayer4TexUVIndex),
            _BaseMapStackingLayer4TexUVScaleOffset,
            _BaseMapStackingLayer4TexUVCenterPivotScalePos,
            _BaseMapStackingLayer4TexUVAnimSpeed,
            _BaseMapStackingLayer4TexUVRotatedAngle,
            _BaseMapStackingLayer4TexUVRotateSpeed,
            _BaseMapStackingLayer4TintColor,
            _BaseMapStackingLayer4MaskTexChannel,
            uvData.GetUV(_BaseMapStackingLayer4MaskUVIndex),
            _BaseMapStackingLayer4Tex,
            _BaseMapStackingLayer4MaskTex,
            sampler_linear_repeat, // for layer 3-10, we hardcode sampler_linear_clamp or sampler_linear_repeat to avoid adding more sampler count
            facing,
            _BaseMapStackingLayer4TexIgnoreAlpha,
            _BaseMapStackingLayer4ApplytoFaces,
            _BaseMapStackingLayer4MaskInvertColor,
            _BaseMapStackingLayer4MaskTexAsIDMap,
            _BaseMapStackingLayer4MaskTexExtractFromID,
            _BaseMapStackingLayer4MaskRemapStart,
            _BaseMapStackingLayer4MaskRemapEnd);
        ApplyStackingLayer(color, layer4Content, _BaseMapStackingLayer4ColorBlendMode, _BaseMapStackingLayer4MasterStrength, isOpaque);
    }
#endif
#if _BASEMAP_STACKING_LAYER5
    {
        const half4 layer5Content = GetStackingLayerRGBAResult(
            uvData.GetUV(_BaseMapStackingLayer5TexUVIndex),
            _BaseMapStackingLayer5TexUVScaleOffset,
            _BaseMapStackingLayer5TexUVCenterPivotScalePos,
            _BaseMapStackingLayer5TexUVAnimSpeed,
            _BaseMapStackingLayer5TexUVRotatedAngle,
            _BaseMapStackingLayer5TexUVRotateSpeed,
            _BaseMapStackingLayer5TintColor,
            _BaseMapStackingLayer5MaskTexChannel,
            uvData.GetUV(_BaseMapStackingLayer5MaskUVIndex),
            _BaseMapStackingLayer5Tex,
            _BaseMapStackingLayer5MaskTex,
            sampler_linear_clamp, // for layer 3-10, we hardcode sampler_linear_clamp or sampler_linear_repeat to avoid adding more sampler count
            facing,
            _BaseMapStackingLayer5TexIgnoreAlpha,
            _BaseMapStackingLayer5ApplytoFaces,
            _BaseMapStackingLayer5MaskInvertColor,
            _BaseMapStackingLayer5MaskTexAsIDMap,
            _BaseMapStackingLayer5MaskTexExtractFromID,
            _BaseMapStackingLayer5MaskRemapStart,
            _BaseMapStackingLayer5MaskRemapEnd);
        ApplyStackingLayer(color, layer5Content, _BaseMapStackingLayer5ColorBlendMode, _BaseMapStackingLayer5MasterStrength, isOpaque);
    }
#endif
#if _BASEMAP_STACKING_LAYER6
    {
        const half4 layer6Content = GetStackingLayerRGBAResult(
            uvData.GetUV(_BaseMapStackingLayer6TexUVIndex),
            _BaseMapStackingLayer6TexUVScaleOffset,
            _BaseMapStackingLayer6TexUVCenterPivotScalePos,
            _BaseMapStackingLayer6TexUVAnimSpeed,
            _BaseMapStackingLayer6TexUVRotatedAngle,
            _BaseMapStackingLayer6TexUVRotateSpeed,
            _BaseMapStackingLayer6TintColor,
            _BaseMapStackingLayer6MaskTexChannel,
            uvData.GetUV(_BaseMapStackingLayer6MaskUVIndex),
            _BaseMapStackingLayer6Tex,
            _BaseMapStackingLayer6MaskTex,
            sampler_linear_repeat, // for layer 3-10, we hardcode sampler_linear_clamp or sampler_linear_repeat to avoid adding more sampler count
            facing,
            _BaseMapStackingLayer6TexIgnoreAlpha,
            _BaseMapStackingLayer6ApplytoFaces,
            _BaseMapStackingLayer6MaskInvertColor,
            _BaseMapStackingLayer6MaskTexAsIDMap,
            _BaseMapStackingLayer6MaskTexExtractFromID,
            _BaseMapStackingLayer6MaskRemapStart,
            _BaseMapStackingLayer6MaskRemapEnd);
        ApplyStackingLayer(color, layer6Content, _BaseMapStackingLayer6ColorBlendMode, _BaseMapStackingLayer6MasterStrength, isOpaque);
    }
#endif
#if _BASEMAP_STACKING_LAYER7
    {
        const half4 layer7Content = GetStackingLayerRGBAResult(
            uvData.GetUV(_BaseMapStackingLayer7TexUVIndex),
            _BaseMapStackingLayer7TexUVScaleOffset,
            _BaseMapStackingLayer7TexUVCenterPivotScalePos,
            _BaseMapStackingLayer7TexUVAnimSpeed,
            _BaseMapStackingLayer7TexUVRotatedAngle,
            _BaseMapStackingLayer7TexUVRotateSpeed,
            _BaseMapStackingLayer7TintColor,
            _BaseMapStackingLayer7MaskTexChannel,
            uvData.GetUV(_BaseMapStackingLayer7MaskUVIndex),
            _BaseMapStackingLayer7Tex,
            _BaseMapStackingLayer7MaskTex,
            sampler_linear_clamp, // for layer 3-10, we hardcode sampler_linear_clamp or sampler_linear_repeat to avoid adding more sampler count
            facing,
            _BaseMapStackingLayer7TexIgnoreAlpha,
            _BaseMapStackingLayer7ApplytoFaces,
            _BaseMapStackingLayer7MaskInvertColor,
            _BaseMapStackingLayer7MaskTexAsIDMap,
            _BaseMapStackingLayer7MaskTexExtractFromID,
            _BaseMapStackingLayer7MaskRemapStart,
            _BaseMapStackingLayer7MaskRemapEnd);
        ApplyStackingLayer(color, layer7Content, _BaseMapStackingLayer7ColorBlendMode, _BaseMapStackingLayer7MasterStrength, isOpaque);
    }
#endif
#if _BASEMAP_STACKING_LAYER8
    {
        const half4 layer8Content = GetStackingLayerRGBAResult(
            uvData.GetUV(_BaseMapStackingLayer8TexUVIndex),
            _BaseMapStackingLayer8TexUVScaleOffset,
            _BaseMapStackingLayer8TexUVCenterPivotScalePos,
            _BaseMapStackingLayer8TexUVAnimSpeed,
            _BaseMapStackingLayer8TexUVRotatedAngle,
            _BaseMapStackingLayer8TexUVRotateSpeed,
            _BaseMapStackingLayer8TintColor,
            _BaseMapStackingLayer8MaskTexChannel,
            uvData.GetUV(_BaseMapStackingLayer8MaskUVIndex),
            _BaseMapStackingLayer8Tex,
            _BaseMapStackingLayer8MaskTex,
            sampler_linear_repeat, // for layer 3-10, we hardcode sampler_linear_clamp or sampler_linear_repeat to avoid adding more sampler count
            facing,
            _BaseMapStackingLayer8TexIgnoreAlpha,
            _BaseMapStackingLayer8ApplytoFaces,
            _BaseMapStackingLayer8MaskInvertColor,
            _BaseMapStackingLayer8MaskTexAsIDMap,
            _BaseMapStackingLayer8MaskTexExtractFromID,
            _BaseMapStackingLayer8MaskRemapStart,
            _BaseMapStackingLayer8MaskRemapEnd);
        ApplyStackingLayer(color, layer8Content, _BaseMapStackingLayer8ColorBlendMode, _BaseMapStackingLayer8MasterStrength, isOpaque);
    }
#endif
#if _BASEMAP_STACKING_LAYER9
    {
        const half4 layer9Content = GetStackingLayerRGBAResult(
            uvData.GetUV(_BaseMapStackingLayer9TexUVIndex),
            _BaseMapStackingLayer9TexUVScaleOffset,
            _BaseMapStackingLayer9TexUVCenterPivotScalePos,
            _BaseMapStackingLayer9TexUVAnimSpeed,
            _BaseMapStackingLayer9TexUVRotatedAngle,
            _BaseMapStackingLayer9TexUVRotateSpeed,
            _BaseMapStackingLayer9TintColor,
            _BaseMapStackingLayer9MaskTexChannel,
            uvData.GetUV(_BaseMapStackingLayer9MaskUVIndex),
            _BaseMapStackingLayer9Tex,
            _BaseMapStackingLayer9MaskTex,
            sampler_linear_clamp, // for layer 3-10, we hardcode sampler_linear_clamp or sampler_linear_repeat to avoid adding more sampler count
            facing,
            _BaseMapStackingLayer9TexIgnoreAlpha,
            _BaseMapStackingLayer9ApplytoFaces,
            _BaseMapStackingLayer9MaskInvertColor,
            _BaseMapStackingLayer9MaskTexAsIDMap,
            _BaseMapStackingLayer9MaskTexExtractFromID,
            _BaseMapStackingLayer9MaskRemapStart,
            _BaseMapStackingLayer9MaskRemapEnd);
        ApplyStackingLayer(color, layer9Content, _BaseMapStackingLayer9ColorBlendMode, _BaseMapStackingLayer9MasterStrength, isOpaque);
    }
#endif
#if _BASEMAP_STACKING_LAYER10
    {
        const half4 layer10Content = GetStackingLayerRGBAResult(
            uvData.GetUV(_BaseMapStackingLayer10TexUVIndex),
            _BaseMapStackingLayer10TexUVScaleOffset,
            _BaseMapStackingLayer10TexUVCenterPivotScalePos,
            _BaseMapStackingLayer10TexUVAnimSpeed,
            _BaseMapStackingLayer10TexUVRotatedAngle,
            _BaseMapStackingLayer10TexUVRotateSpeed,
            _BaseMapStackingLayer10TintColor,
            _BaseMapStackingLayer10MaskTexChannel,
            uvData.GetUV(_BaseMapStackingLayer10MaskUVIndex),
            _BaseMapStackingLayer10Tex,
            _BaseMapStackingLayer10MaskTex,
            sampler_linear_repeat, // for layer 3-10, we hardcode sampler_linear_clamp or sampler_linear_repeat to avoid adding more sampler count
            facing,
            _BaseMapStackingLayer10TexIgnoreAlpha,
            _BaseMapStackingLayer10ApplytoFaces,
            _BaseMapStackingLayer10MaskInvertColor,
            _BaseMapStackingLayer10MaskTexAsIDMap,
            _BaseMapStackingLayer10MaskTexExtractFromID,
            _BaseMapStackingLayer10MaskRemapStart,
            _BaseMapStackingLayer10MaskRemapEnd);
        ApplyStackingLayer(color, layer10Content, _BaseMapStackingLayer10ColorBlendMode, _BaseMapStackingLayer10MasterStrength, isOpaque);
    }
#endif

    // final per character + global edit
    color.rgb *= _PerCharacterBaseColorTint * _GlobalVolumeBaseColorTintColor; // edit rgb only, since they are not per material, edit alpha per character/globally should be not intentional by user
    
    return color;
}

half3 GetFinalEmissionColor(Varyings input, half3 baseColor)
{
#if _EMISSION
    float2 uv = GetUV(input) * _EmissionMapTilingXyOffsetZw.xy + _EmissionMapTilingXyOffsetZw.zw + frac(_Time.yy * _EmissionMapUVScrollSpeed);

    half4 emissionMapSampleValue = tex2D(_EmissionMap, uv);
    half3 emissionResult = _EmissionMapUseSingleChannelOnly ? dot(emissionMapSampleValue,_EmissionMapSingleChannelMask) : emissionMapSampleValue.rgb; // alpha is ignored if using rgb mode

    emissionResult *= _EmissionColor.rgb * _EmissionIntensity; 
    emissionResult *= lerp(1,baseColor,_MultiplyBaseColorToEmissionColor); // let user optionally mix base color to emission color

    // mask by an optional mask texture
    // (decided to not use shader_feature for this mask section, else too much shader_feature is used)
    float2 maskMapUV = GetUV(input);
    half emissionMask = dot(SAMPLE_TEXTURE2D(_EmissionMaskMap, sampler_BaseMap, maskMapUV),_EmissionMaskMapChannelMask); // reuse sampler_BaseMap to save sampler count
    emissionMask = _EmissionMaskMapInvertColor? 1-emissionMask : emissionMask;
    emissionMask = invLerpClamp(_EmissionMaskMapRemapStart, _EmissionMaskMapRemapEnd, emissionMask);
    emissionResult *= emissionMask;

    return emissionResult;
#else
    return 0; // default emission value is black when turn off
#endif
}
half GetFinalOcculsion(UVData uvData, float facing)
{
#if _OCCLUSIONMAP
    half4 texValue = tex2D(_OcclusionMap, uvData.GetUV(_OcclusionMapUVIndex));
    half occlusionValue = dot(texValue, _OcclusionMapChannelMask);
    occlusionValue = _OcclusionMapInvertColor? 1-occlusionValue : occlusionValue;
    occlusionValue = invLerpClamp(_OcclusionRemapStart, _OcclusionRemapEnd, occlusionValue); // should remap first,

    // render face mask (Both,0 | Front,2 | Back,1)
    half shouldApplyToTargetRenderFace = !((_OcclusionMapApplytoFaces == 1 && facing == 1.0) || (_OcclusionMapApplytoFaces == 2 && facing == -1.0));
    
    occlusionValue = lerp(1, occlusionValue, _OcclusionStrength * _GlobalOcclusionStrength * shouldApplyToTargetRenderFace); // then apply per material and per volume fadeout.

    return occlusionValue;
#else
    return 1; // default occulusion value is 1 when turn off
#endif
}
half GetFinalSmoothness(Varyings input)
{
    half smoothnessResult = _Smoothness;
#if _SMOOTHNESSMAP
    half4 texValue = tex2D(_SmoothnessMap, GetUV(input));
    half smoothnessMultiplierByTexture = dot(texValue, _SmoothnessMapChannelMask);
    smoothnessMultiplierByTexture = _SmoothnessMapInputIsRoughnessMap ? 1-smoothnessMultiplierByTexture : smoothnessMultiplierByTexture;
    smoothnessMultiplierByTexture = invLerpClamp(_SmoothnessMapRemapStart, _SmoothnessMapRemapEnd, smoothnessMultiplierByTexture); // remap
    smoothnessResult *= smoothnessMultiplierByTexture; // apply
#endif
    return smoothnessResult;
}
// return .rgb is specular RGB, return .a is specular mask 
half4 GetFinalSpecularRGBA(UVData uvData, half3 baseColor, float facing)
{
#if _SPECULARHIGHLIGHTS
    // mask
    half4 texValue = tex2D(_SpecularMap, uvData.GetUV(_SpecularMapUVIndex));
    half specularMask = dot(texValue, _SpecularMapChannelMask);

    specularMask = _SpecularMapAsIDMap ? abs(specularMask * 255 - _SpecularMapExtractFromID) < 2 ? 1 : 0 : specularMask;
    specularMask = _SpecularMapInvertColor ? 1 - specularMask : specularMask;
    specularMask = invLerpClamp(_SpecularMapRemapStart, _SpecularMapRemapEnd, specularMask); // should remap first,

    // * render face mask
    if(_SpecularApplytoFaces != 0)
    {
        if(_SpecularApplytoFaces == 1) specularMask *= saturate(-facing);
        if(_SpecularApplytoFaces == 2) specularMask *= saturate(facing);
    }

    half3 specularRGBResult = _SpecularColor * _SpecularIntensity;// then apply intensity / color
    specularRGBResult *= lerp(1,baseColor,_MultiplyBaseColorToSpecularColor); // let user optionally mix base color to specular color
    #if _SPECULARHIGHLIGHTS_TEX_TINT
        float2 specularColorTintMapUV = uvData.GetUV(_SpecularColorTintMapUseSecondUv ? 1 : 0);
        specularColorTintMapUV = specularColorTintMapUV * _SpecularColorTintMapTilingXyOffsetZw.xy + _SpecularColorTintMapTilingXyOffsetZw.zw; 
        specularRGBResult *= lerp(1,tex2D(_SpecularColorTintMap, specularColorTintMapUV).rgb,_SpecularColorTintMapUsage);
    #endif
    return half4(specularRGBResult,specularMask);
#else
    return 0; // default specular value is 0 when turn off
#endif
}
half3 GetFinalNormalTS(UVData uvData, float facing)
{
#if _NORMALMAP
    // render face mask (Both,0 | Front,2 | Back,1)
    if((_BumpMapApplytoFaces == 1 && facing == 1.0) || (_BumpMapApplytoFaces == 2 && facing == -1.0))
    {
        return half3(0,0,1);
    }

    const float2 uv = CalcUV(uvData, _BumpMapUVIndex, _BumpMapUVScaleOffset, _BumpMapUVScrollSpeed);
    return UnpackNormalScale(tex2D(_BumpMap, uv), _BumpScale);
#else
    return half3(0,0,1); //default value of normal map when turn off, pointing out in tangent space, if converted back to world space it will be the same as raw vertex normal
#endif
}
void DoClipTestToTargetAlphaValue(half alpha) 
{
#if _ALPHATEST_ON
    clip(alpha - _Cutoff);
#endif
}

uint NiloGetMeshRenderingLayer()
{
    // SHADER_LIBRARY_VERSION_MAJOR is deprecated for Unity2022.2 or later, so we will use UNITY_VERSION instead
    // see -> https://github.com/Cyanilux/URP_ShaderCodeTemplates/blob/main/URP_SimpleLitTemplate.shader#L145
    #if UNITY_VERSION >= 202220 // (for URP 14 or above)
    uint meshRenderingLayers = GetMeshRenderingLayer();
    #else
    uint meshRenderingLayers = GetMeshRenderingLightLayer();
    #endif

    return meshRenderingLayers;
}

InputData NiloGetLightLoopInputData(ToonLightingData lightingData)
{
    // LIGHT_LOOP_BEGIN needs:
    // - inputData.normalizedScreenSpaceUV
    // - inputData.positionWS
    // so we create a temp InputData
    InputData inputData = (InputData)0;
    inputData.normalizedScreenSpaceUV = lightingData.normalizedScreenSpaceUV;
    inputData.positionWS = lightingData.positionWS;

    return inputData;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copy and simplify from URP10.2.2's LitInput.hlsl's detail map logic - START
// this section only exist #if _DETAIL
#if _DETAIL
    // Used for scaling detail albedo. Main features:
    // - Depending if detailAlbedo brightens or darkens, scale magnifies effect.
    // - No effect is applied if detailAlbedo is 0.5.
    half3 ScaleDetailAlbedo(half3 detailAlbedo, half scale)
    {
        // detailAlbedo = detailAlbedo * 2.0h - 1.0h;
        // detailAlbedo *= _DetailAlbedoMapScale;
        // detailAlbedo = detailAlbedo * 0.5h + 0.5h;
        // return detailAlbedo * 2.0f;

        // A bit more optimized
        return 2.0h * detailAlbedo * scale - scale + 1.0h;
    }
    half3 ApplyDetailAlbedo(float2 detailUv, half3 albedo, half detailMask)
    {
        half3 detailAlbedo = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, detailUv).rgb;
        detailAlbedo = ScaleDetailAlbedo(detailAlbedo, _DetailAlbedoMapScale);
        detailAlbedo *= 0.5 / _DetailAlbedoWhitePoint;
        return albedo * LerpWhiteTo(detailAlbedo, detailMask); // apply detail albedo's method is just 1 simple multiply
    }
    half3 ApplyDetailNormal(float2 detailUv, half3 normalTS, half detailMask)
    {
        // Not using UnpackNormal(...) for mobile & switch now
        // because we want to unify mobile and non mobile detail normalmap rendering, since performance differernce is small
        // honestly no one will care 1 more MUL in 2021, even for mobile
        half3 detailNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUv), _DetailNormalMapScale);

        // With UNITY_NO_DXT5nm unpacked vector is not normalized for BlendNormalRNM
        // For visual consistancy we should do normalize() in all cases,
        // but here we only normalize #if UNITY_NO_DXT5nm, for performance reason
        #if UNITY_NO_DXT5nm
            detailNormalTS = normalize(detailNormalTS);
        #endif

        // TODO: detailMask should lerp the angle of the quaternion rotation, not the normals
        return lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask); 
    }
#endif
// Copy and simplify from URP10.2.2's LitInput.hlsl's detail map logic - END
///////////////////////////////////////////////////////////////////////////////////////////////////////////

// similar to URP's LitForwardPass.hlsl -> InitializeStandardLitSurfaceData()
ToonSurfaceData InitializeSurfaceData(Varyings input, UVData uvData, float facing, half3 normalTS, half detailMask, float2 detailUV)
{
    ToonSurfaceData output;

    // albedo(Base Color) & alpha
    half4 baseColorFinal = GetFinalBaseColor(input, uvData, facing);
    ApplyExternalAssetSupportLogicToBaseColor(baseColorFinal, input, uvData, facing);
    ApplyCustomUserLogicToBaseColor(baseColorFinal, input, uvData, facing);
    output.albedo = baseColorFinal.rgb;
    output.alpha = baseColorFinal.a;

    // alpha clip and dither fadeout
    DoClipTestToTargetAlphaValue(output.alpha);// let clip() early exit asap once alpha value is known
#if _NILOTOON_DITHER_FADEOUT
    NiloDoDitherFadeoutClip(input.positionCS.xy, 1-_DitherFadeoutAmount*_AllowPerCharacterDitherFadeout);
#endif

    // occlusion
    output.occlusion = GetFinalOcculsion(uvData, facing);

    // smoothness
    output.smoothness = GetFinalSmoothness(input);

    // normalTS
    output.normalTS = normalTS;

    // Detail albedo and normal (only enable in non-DEBUG)
#if _DETAIL && !_NILOTOON_DEBUG_SHADING
    output.albedo = ApplyDetailAlbedo(detailUV, output.albedo, detailMask);
#endif

    ///////////////////////////////////////////////////////////
    // after Detail albedo and normal,
    // do all functions that require detail albedo and normal
    ///////////////////////////////////////////////////////////
    // emission
    output.emission = GetFinalEmissionColor(input, output.albedo);

    // specular & specular mask (not roughness)
    half4 specularResult = GetFinalSpecularRGBA(uvData, output.albedo, facing);
    output.specular = specularResult.rgb;
    output.specularMask = specularResult.a;

    return output;
}

UVData InitializeUV0ToUV3(Varyings input)
{
    UVData uvData = (UVData)0; // init to 0 to make compiler happy

    uvData.allUVs[0] = input.uv01.xy;
    uvData.allUVs[1] = input.uv01.zw;
    uvData.allUVs[2] = input.uv23.xy;
    uvData.allUVs[3] = input.uv23.zw;

    return uvData;
}

half3 CalculateLightInjectionWeightedDirection(Light light, half Avg3AdditionalLightPixelColor)
{
    return light.direction * Avg3AdditionalLightPixelColor;
}

#if NeedCalculateAdditionalLight
struct NiloToonPerAdditionalLightData
{
    half    injectIntoMainLightColor;
    half    injectIntoMainLightDirection;
    half    injectIntoMainLightColorApplyDesaturate;
    half    additiveLightIntensity;

    half    injectIntoMainLightBackLightOcclusion2D;
    half    injectIntoMainLightBackLightOcclusion3D;
};

NiloToonPerAdditionalLightData GetNiloToonPerAdditionalLightData(int lightIndex)
{
    // copy the code of GetAdditionalLight(...) for find the final GPU array index
    #if USE_FORWARD_PLUS
    int realGPULightArrayIndex = lightIndex;
    #else
    int realGPULightArrayIndex = GetPerObjectLightIndex(lightIndex);
    #endif
    
    half4 niloToonPerUnityLightData = _NiloToonGlobalPerUnityLightDataArray[realGPULightArrayIndex];
    half4 niloToonPerUnityLightData2 = _NiloToonGlobalPerUnityLightDataArray2[realGPULightArrayIndex];

    NiloToonPerAdditionalLightData data;
    data.injectIntoMainLightColor = niloToonPerUnityLightData.x;
    data.injectIntoMainLightDirection = niloToonPerUnityLightData.y;
    data.injectIntoMainLightColorApplyDesaturate = niloToonPerUnityLightData.z;
    data.additiveLightIntensity = niloToonPerUnityLightData.w;

    data.injectIntoMainLightBackLightOcclusion2D = niloToonPerUnityLightData2.x;
    data.injectIntoMainLightBackLightOcclusion3D = niloToonPerUnityLightData2.y;

    return data;
}
#endif

// similar to URP's LitForwardPass.hlsl -> InitializeInputData()
ToonLightingData InitializeLightingData(Varyings input, half3 normalTS, float facing)
{
    ToonLightingData lightingData;

    lightingData.uv = GetUV(input);
    lightingData.positionWS = input.positionWS_ZOffsetFinalSum.xyz;

    lightingData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(lightingData.positionWS);

    half3 normalWS = input.normalWS_averageShadowAttenuation.xyz;

    // We should re-normalize all direction unit vector after interpolation.
    // Here even _NORMALMAP is false, we still normalize() to ensure correctness (unit vector in vertex shader, after interpolation, is NOT always unit vector in fragment shader).
    // Not doing NormalizeNormalPerPixel(normalWS) will affect GGX specular result greatly since specular depends on normal quality heavily!
    normalWS = NormalizeNormalPerPixel(normalWS);

    lightingData.normalWS = normalWS;
    lightingData.normalWS_NoNormalMap = normalWS; // extra: save the normalized interpolated vertex normal into lightingData, don't let normal map affect this vector

#if VaryingsHasTangentWS
    // [you can reference this section from URP's LitForwardPass.hlsl -> InitializeInputData(...)]
    float sgn = input.tangentWS.w; // should be either +1 or -1. No need to apply unity_WorldTransformParams here because it was applied in vertex shader already

    // no need to normalize bitangentWS if you only use normalWS in later calculations, 
    // since we normalize normalWS at the end anyway when we assign normalWS's result to lightingData.
    float3 bitangentWS = sgn * cross(input.normalWS_averageShadowAttenuation.xyz, input.tangentWS.xyz);

    lightingData.TBN_WS = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS_averageShadowAttenuation.xyz);

    #if defined(VaryingsHasViewDirTS)
        half3 viewDirTS = input.viewDirTS;
    #else
        half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS_averageShadowAttenuation.xyz, lightingData.viewDirectionWS);
    #endif
    lightingData.viewDirectionTS = viewDirTS;
#endif

    // if any normalmap is enabled, convert normalTS in normalmap to normalWS
    #if _NORMALMAP || _DETAIL
    normalWS = TransformTangentToWorld(normalTS, lightingData.TBN_WS); // apply normal-mapped result normalWS (rotation-only matrix's transpose equals inverse)
    normalWS = NormalizeNormalPerPixel(normalWS); // since T & B are not normalized, we need to normalize result normalWS after TBN matrix mul
    lightingData.normalWS = normalWS;
    #endif

    //---------------------------------------------------------------------------------------
    // all normal map related data should init after normalmap is applied to lightingData.normalWS
    //---------------------------------------------------------------------------------------
    
    // if no one use lightingData.normalVS, unity's shader compiler will remove this line's calculation,
    // same as URP's VertexPositionInputs and VertexNormalInputs struct
    lightingData.normalVS = mul((half3x3)UNITY_MATRIX_V, lightingData.normalWS).xyz;

    // see URP's GlobalIllumination.hlsl -> half3 GlobalIllumination(...)'s reflectVector
    lightingData.reflectionVectorWS = reflect(-lightingData.viewDirectionWS,lightingData.normalWS); 

    lightingData.NdotV = saturate(dot(lightingData.normalWS,lightingData.viewDirectionWS));
    
    // Note: NiloToon don't have "shadowCoord" from vertex shader, "shadowCoord" is always calculated in fragment shader
    // ...

    // [Why not passing isFaceArea from vertex shader, but recalculate in frag shader again instead?]
    // because the gap between vertices will produce ugly isFaceArea in fragment shader due to interpolation, so we need to calculate in frag shader again to ensure correctness

    // toggle "_ISFACE" will affect face lighting and shadowing
    // if is face: override normalWS by user defined face forward direction & face area mask, in vertex shader
    // if is not face: don't edit normal, use mesh's normal directly
    // normalWS face area edit already done in vertex shader
    // so here we don't need to edit normalWS
    lightingData.isFaceArea = GetFaceArea(lightingData.uv);
    
    lightingData.isSkinArea = GetSkinArea(lightingData.uv);
    
    lightingData.SV_POSITIONxy = input.positionCS.xy;

    // note for future XR development:
    // ref code that may help fixing VR problem: https://docs.unity3d.com/Manual/SinglePassStereoRendering.html
    //lightingData.screenUV = input.screenPos.xy / input.screenPos.w;
    //lightingData.screenUV = UnityStereoTransformScreenSpaceTex(lightingData.screenUV);
    
    // [why not just pass abs(positionVS.z) from vertex shader? it seems it is also linearEyeDepth]
    // because we want lightingData.selfLinearEyeDepth having the same format as Convert_SV_PositionZ_ToLinearViewSpaceDepth(tex2D(_CameraDepthTexture)),
    // recalculate selfLinearEyeDepth from positionCS.z provided much better precision when doing depth texture shadow depth comparison logic.
    // A "Samsung A70" mobile precision test @ 2021-3-23 confirmed depth texture shadow precision improved a lot using this line instead of abs(positionVS.z) where positionVS is from vertex shader  
    lightingData.selfLinearEyeDepth = Convert_SV_PositionZ_ToLinearViewSpaceDepth(input.positionCS.z);

    lightingData.ZOffsetFinalSum = input.positionWS_ZOffsetFinalSum.w;

    lightingData.SH = input.SH_fogFactor.xyz;

    lightingData.averageShadowAttenuation = input.normalWS_averageShadowAttenuation.w;
    
    lightingData.NdotV_NoNormalMap = saturate(dot(lightingData.normalWS_NoNormalMap, lightingData.viewDirectionWS));

    lightingData.vertexColor = input.color;

    lightingData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    lightingData.aspectCorrectedScreenSpaceUV = GetAspectRatioCorrectedNormalizedScreenSpaceUV(lightingData.normalizedScreenSpaceUV);
    
    lightingData.facing = facing;

    float3 characterBoundCenterPosWS = _NiloToonGlobalPerCharBoundCenterPosWSArray[_CharacterID];
    lightingData.characterBoundBottomWS_positionWS = characterBoundCenterPosWS - float3(0,_CharacterBoundRadius,0);
    lightingData.characterBoundTopWS_positionWS = characterBoundCenterPosWS + float3(0,_CharacterBoundRadius,0);

    // Note: 
    // for (NiloToonShadowCasterPass || NiloToonCharSelfShadowCasterPass), UNITY_MATRIX_V is converting the vertex to shadow camera's View space,
    // but for a correct dissolve, it should be converting the vertex to main camera's View space, it is a contradiction since there can be many cameras.
    // This will result the screen space dissolve not having a stable direction for (NiloToonShadowCasterPass || NiloToonCharSelfShadowCasterPass)
    // there is no 100% correct solution to this problem, the best we can do is to match the dissolve to main camera's view space, and ignore other cameras.
    lightingData.characterBound2DRectUV01 = Calc3DSphereTo2DUV(lightingData.positionWS, characterBoundCenterPosWS, _CharacterBoundRadius);

    // make characterBound2DRectUV01 uv matching in all pass
    // TODO: this line is not needed, it only flip the uv.y, without fixing the problem
#if NiloToonShadowCasterPass || NiloToonCharSelfShadowCasterPass
    lightingData.characterBound2DRectUV01.y = 1-lightingData.characterBound2DRectUV01.y;
#endif

    // [require normalmap finish first]
    // a shared matcap uv for all matcap features
    lightingData.matcapUV = CalcMatCapUV(lightingData.viewDirectionWS,lightingData.normalWS);
    lightingData.matcapUV = _EnableUVEditGroup ? lightingData.matcapUV * _MatCapUVTiling : lightingData.matcapUV;

    //--------------------------------------------------------------------
    // MainLight Note
    //--------------------------------------------------------------------
    // [Which directional light in main light?]
    // - If Sun Light is present, Sun Light will be treated as Main Light
    // - If Sun Light is not present, the brightest directional light will be treated as Main Light
    //
    // It is shaded outside the light loop and it has a specific set of variables and shading path
    // so we can be as fast as possible in the case when there's only a single directional light in scene
    // There are 3 overloads of GetMainLight(...) in URP16:
    // - Light GetMainLight()
    // - Light GetMainLight(float4 shadowCoord)
    // - Light GetMainLight(float4 shadowCoord, float3 positionWS, half4 shadowMask)
    //
    // we will call the most basic GetMainLight() first, then complete
    // - shadowAttenuation
    // - color (light cookie color multiply)
    // in later steps. So we can skip shadowAttenuation calculation if not used

    //--------------------------------------------------------------------
    // Inject all additional lights into mainLight
    //--------------------------------------------------------------------
    uint meshRenderingLayers = NiloGetMeshRenderingLayer();
    Light mainLight = GetMainLight();

    // cache original value
    half3 originalMainLightDirection = mainLight.direction;
    half3 originalMainLightColor = mainLight.color;

    // init main light color as 0, fill in main light only if mainLight's IsMatchingLightLayer is true
    mainLight.color = 0;

    //----------------------------------------------------------------
    // 0.init Main light's color and direction
#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        // user can override main light's direction + color by 
        // - NiloToonCharRenderingControlVolume
        // - NiloToonCharacterMainLightOverrider
        // if user didn't override, the value assigned will be the same as original mainLight.color/direction
        mainLight.direction = _GlobalUserOverriddenMainLightDirWS;
        mainLight.color = _GlobalUserOverriddenMainLightColor;

        // support main light's light cookie
        // since we are calling a basic version GetMainLight(), we have to fill in required data by ourselves
        #if defined(_LIGHT_COOKIES)
            real3 cookieColor = SampleMainLightCookie(lightingData.positionWS);
            mainLight.color *= cookieColor;
        #endif

        // although main directional light doesn't have a "DistanceAttenuation" like point or spot light,
        // multiply distanceAttenuation with a directional light color will make Light's Culling mask works(the game object's layer, not the new URP rendering layer),
        // this line equals to: *= unity_LightData.z; // unity_LightData.z is 1 when not culled by the culling mask, otherwise 0.
        // (see URP's Lighting.hlsl's GetMainLight())
        mainLight.color *= mainLight.distanceAttenuation;

        mainLight.color *= lightingData.averageShadowAttenuation;

        mainLight.color *= _GlobalMainDirectionalLightMultiplier;
    }
    
    // apply per char main light control (from NiloToonCharacterLightController scripts)
    // *don't put this in the above if(){...}, since this feature don't care mainLight exist or not, it should always execute
    mainLight.color = mainLight.color * _NiloToonGlobalPerCharMainDirectionalLightTintColorArray[_CharacterID] + _NiloToonGlobalPerCharMainDirectionalLightAddColorArray[_CharacterID];

    //----------------------------------------------------------------
    // 1. inject indirect light into main light

    // [sample APV and replace SH, if project enabled APV]
    #if (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    lightingData.SH = SAMPLE_GI(lightingData.SH,
        GetAbsolutePositionWS(lightingData.positionWS),
        lightingData.normalWS *= 1-_IndirectLightFlatten, // do normal flatten for indirect light
        lightingData.viewDirectionWS,
        lightingData.SV_POSITIONxy,
        1, // probe occlusion, since this shader is for dynamic character, we use a constant 1
        1); // shadowmask, since this shader is for dynamic character, we use a constant 1
    #endif

    // [pick the highest between indirect and main directional light = max(direct light,indirect light)]
    // anime character artist usually don't want the concept of indirect + direct light
    // because it will ruin the final color easily,
    // what anime character artist want is:
    // - if direct lighting is bright enough -> keep character's result same as albedo texture that they draw
    // - if in a dark environment -> switch to use light probe/APV to make character blend into the environment color
    // *max() can prevent result completely black, if light probe was not baked and no direct light is active
    const half3 indirectLight = min(max(lightingData.SH,_GlobalIndirectLightMinColor),_GlobalIndirectLightMaxColor);
    mainLight.color = max(mainLight.color, indirectLight);

    //----------------------------------------------------------------
    // [the following light loop will apply "additional light inject into main light" (from NiloToonAdditionalLightStyle volume)]
    //----------------------------------------------------------------
#if NeedCalculateAdditionalLight
    InputData inputData = NiloGetLightLoopInputData(lightingData);
    
    uint pixelLightCount = GetAdditionalLightsCount();

    half3 additionalLightsDirectionWeightedSum = 0;
    half3 additionalLightsColorSum = 0;

    //----------------------------------------------------------------
    // 2.inject Forward+'s directional additional light into main light [Forward+ only]
    
    // this is an extra for-loop that only exists in Forward+,
    // and is only looping directional lights as additional light, not including main directional light, or any point/spot light
    // Note: as Directional lights would be in all clusters since they don't have a 3D volume, so they don't go into the cluster structure.
    // Instead, they are stored first in the light buffer(array), so here we can loop them easily starting from index 0.
    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {                
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, input.positionWS_ZOffsetFinalSum.xyz); // since URP10, you must provide shadowMask in order to receive shadowmap

#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            // support additional directional light's light cookie
            // since we are calling a basic version GetMainLight(), we have to fill in required data by ourselves
            #if defined(_LIGHT_COOKIES)
            real3 cookieColor = SampleAdditionalLightCookie(lightIndex, inputData.positionWS);
            light.color *= cookieColor;
            #endif

            // - don't * light.shadowAttenuation, shadow map looks ugly (shadow map doesn't exist for additional directional light in URP)
            half3 additionalLightPixelColor = light.color * light.distanceAttenuation;

            NiloToonPerAdditionalLightData niloToonPerAdditionalLightData = GetNiloToonPerAdditionalLightData(lightIndex);
            
            half Avg3AdditionalLightPixelColor = NiloAvg3(additionalLightPixelColor);
            // simple sum, don't need to multiply any dot(N,L) mask, keep the color sum clean and blurry!
            additionalLightsColorSum += lerp(additionalLightPixelColor,Avg3AdditionalLightPixelColor,niloToonPerAdditionalLightData.injectIntoMainLightColorApplyDesaturate) * niloToonPerAdditionalLightData.injectIntoMainLightColor; 

            // [1]
            // It is possible to additionally multiply "saturate(dot(light.direction,lightingData.normalWS) * k + (1-k))" to weighted sum,
            // it will produce double side rim light when multiple lights having different direction,
            // but double side rim light is not the style that NiloToon want to produce
            // [2]
            // simply use Avg3() as weight for weighted sum, not Luminance(), since red/blue light should have the same directional weight as green light 
            additionalLightsDirectionWeightedSum += CalculateLightInjectionWeightedDirection(light,Avg3AdditionalLightPixelColor) * niloToonPerAdditionalLightData.injectIntoMainLightDirection;
        }
    }
    #endif

    //----------------------------------------------------------------
    // 3.inject only point/spot additional light into main light [if Forward+]
    // 3.inject all additional light into main light [if Forward/Deferred]
    
    LIGHT_LOOP_BEGIN(pixelLightCount)

        // TODO: try characterBoundCenterPosWS for GetAdditionalLight(...)
        Light light = GetAdditionalLight(lightIndex, lightingData.positionWS); // not provide shadowMask in order to not receive additional light's shadowmap
    
#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            // support additional directional light's light cookie
            // since we are calling a basic version GetMainLight(), we have to fill in required data by ourselves
            #if defined(_LIGHT_COOKIES)
            real3 cookieColor = SampleAdditionalLightCookie(lightIndex, inputData.positionWS);
            light.color *= cookieColor;
            #endif

            // - don't * light.shadowAttenuation, shadow map looks ugly
            // - optionally apply saturate() to light.distanceAttenuation to cap it in 0~1, since it is 1/(d^2), it can go very high when light is close to character where d < 1
            half3 additionalLightPixelColor = light.color * lerp(saturate(light.distanceAttenuation),light.distanceAttenuation,_GlobalAdditionalLightInjectIntoMainLightColor_AllowCloseLightOverBright); 

            NiloToonPerAdditionalLightData niloToonPerAdditionalLightData = GetNiloToonPerAdditionalLightData(lightIndex);
            
            half Avg3AdditionalLightPixelColor = NiloAvg3(additionalLightPixelColor);
            
            half backLightOcclusion2DMask = saturate(dot(lightingData.viewDirectionWS, light.direction));
            half backLightOcclusion3DMask = smoothstep(0,0.1,dot(lightingData.normalWS, light.direction));
            //maskTerm = dot(V, L) * 0.5 + 0.5; // possible
            //maskTerm = smoothstep(0.4,0.6,dot(N, L)); // face may look ugly, not te best one.
            
            // simple sum, don't need to multiply any dot(N,L) mask, keep the color sum clean and blurry!
            additionalLightsColorSum += lerp(additionalLightPixelColor,Avg3AdditionalLightPixelColor,niloToonPerAdditionalLightData.injectIntoMainLightColorApplyDesaturate)
            * niloToonPerAdditionalLightData.injectIntoMainLightColor
            * lerp(1, backLightOcclusion2DMask,niloToonPerAdditionalLightData.injectIntoMainLightBackLightOcclusion2D)
            * lerp(1, backLightOcclusion3DMask,niloToonPerAdditionalLightData.injectIntoMainLightBackLightOcclusion3D)
            ; 

            // [1]
            // It is possible to additionally multiply "saturate(dot(light.direction,lightingData.normalWS) * k + (1-k))" to weighted sum,
            // it will produce double side rim light when multiple lights having different direction,
            // but double side rim light is not the style that NiloToon want to produce
            // [2]
            // simply use Avg3() as weight for weighted sum, not Luminance(), since red/blue light should have the same directional weight as green light 
            additionalLightsDirectionWeightedSum += CalculateLightInjectionWeightedDirection(light,Avg3AdditionalLightPixelColor) * niloToonPerAdditionalLightData.injectIntoMainLightDirection; 
        }
    LIGHT_LOOP_END

    // Note: NiloToon doesn't have any additional vertex light
    
    additionalLightsColorSum *= _GlobalAdditionalLightInjectIntoMainLightColor_Strength;
    additionalLightsDirectionWeightedSum *= _GlobalAdditionalLightInjectIntoMainLightDirection_Strength;

    // *Avg3 as weight for weighted sum, not Luminance, since red/blue light should have the same weight as green light
    // (you must make it sync with the weight method of other additionalLightsDirectionWeightedSum)
    half Avg3DirectionalLightPixelColor = NiloAvg3(mainLight.color);
    half3 mainDirectionalLightDirectionWeightedSum = CalculateLightInjectionWeightedDirection(mainLight,Avg3DirectionalLightPixelColor);
    
    // normalize the weighted sum of all light's direction, to get the final merged main light's direction
    // brighter light will affect the final merged main light's direction more due to using Avg3 as weight
    // *Add a constant to avoid lightDirectionsWeightedSum is 0, where normal(0) will produce error
    mainLight.direction = normalize(mainDirectionalLightDirectionWeightedSum + additionalLightsDirectionWeightedSum + half3(0,0.001,0));

    // apply mainLight.color injection at last, don't let it affect mainLight.direction
    // *Also apply an optional Desaturate to the inject color
    mainLight.color += lerp(additionalLightsColorSum,Luminance(additionalLightsColorSum),_GlobalAdditionalLightInjectIntoMainLightColor_Desaturate);
#endif

    // [final override mainLight]
    mainLight.direction = _GlobalUserOverriddenFinalMainLightDirWSParam.w ? _GlobalUserOverriddenFinalMainLightDirWSParam.xyz : mainLight.direction;
    mainLight.color = _GlobalUserOverriddenFinalMainLightColorParam.w ? _GlobalUserOverriddenFinalMainLightColorParam.rgb : mainLight.color;
    
    // [_ReceiveSelfShadowMappingPosOffset]
    // this uniform will control the extra depth bias of URP shadowmap, 
    // doing this extra depth bias is usually for hiding ugly self shadow for shadow sensitive area like face
#if ShouldReceiveURPShadow
    float materialDepthBias = lerp(_ReceiveSelfShadowMappingPosOffset,_ReceiveSelfShadowMappingPosOffsetForFaceArea,lightingData.isFaceArea);
    float3 URPShadowTestPosWS = lightingData.positionWS + originalMainLightDirection * (materialDepthBias+_GlobalReceiveSelfShadowMappingPosOffset);

    // always compute any type of URP's ShadowCoord in the fragment shader instead of vertex shader now, due to URP having shadow-cascades
    // https://forum.unity.com/threads/shadow-cascades-weird-since-7-2-0.828453/#post-5516425
    float4 URPShadowCoord = TransformWorldToShadowCoord(URPShadowTestPosWS);
    mainLight.shadowAttenuation = MainLightRealtimeShadow(URPShadowCoord);
#endif

    // copy before edit
    lightingData.rawURPShadowAttenuation = mainLight.shadowAttenuation;
    
    // apply URP shadowmap intensity (material, per character, renderer feature)
#if ShouldReceiveURPShadow
    half materialReceiveShadowMappingAmount = lerp(_ReceiveURPShadowMappingAmountForNonFace,_ReceiveURPShadowMappingAmountForFace, lightingData.isFaceArea) * _ReceiveURPShadowMappingAmount;
    mainLight.shadowAttenuation = lerp(1,mainLight.shadowAttenuation, materialReceiveShadowMappingAmount * _GlobalReceiveShadowMappingAmount * _PerCharReceiveStandardURPShadowMap);
#endif

    lightingData.mainLight = mainLight;
    
    return lightingData;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step3: override surfaceData.albedo)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// [Dynamic Eye]
// currently using a simple method only
// (not HDRP's method: https://github.com/Unity-Technologies/Graphics/blob/c6ae7599b33ca48852eddd592b3e40f33f2dd982/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/EyeUtils.hlsl)
#if _DYNAMIC_EYE
half3 CalculateDynamicEyeColor(float2 inputUV, float3 inputViewDirTS)
{
    // scale uv using eye center as pivot 
    float2 uv = _DynamicEyeSize * (inputUV - 0.5) + 0.5;

    // eye Pupil Alpha
    float eyePupilAlpha = dot(tex2D(_DynamicEyePupilMaskTex, uv),_DynamicEyePupilMaskTexChannelMask);

    // inner eye color (eye pupil)
    float2 offset = -0.73 * inputViewDirTS * _DynamicEyePupilDepthScale * eyePupilAlpha + uv;
    float2 normalizeResult = normalize((offset - 0.5) * 0.5);
    float2 innerEyeUV = lerp(offset, 0.5 + normalizeResult, ((0.8 / _DynamicEyeSize * _DynamicEyePupilSize ) * (1 - 2 * _DynamicEyeSize * length(inputUV - 0.5))));
    half3 innerEyeColor = tex2D(_DynamicEyePupilMap, innerEyeUV) *_DynamicEyePupilColor;

    // outer eye color (eye white)
    float2 outerEyeUV = inputUV * _DynamicEyeWhiteMap_ST.xy + _DynamicEyeWhiteMap_ST.zw;
    half3 outerEyeColor = tex2D(_DynamicEyeWhiteMap, outerEyeUV);

    // combine eye color (inner or outer by mask)
    half finalPupilMask = saturate(eyePupilAlpha / _DynamicEyePupilMaskSoftness);
    half3 combinedEyeColor = lerp(outerEyeColor, innerEyeColor, finalPupilMask);

    return combinedEyeColor * _DynamicEyeFinalTintColor * _DynamicEyeFinalBrightness;
}
#endif
void ApplyDynamicEye(inout ToonSurfaceData surfaceData, Varyings varyings, ToonLightingData lightingData)
{
#if _DYNAMIC_EYE
    surfaceData.albedo = CalculateDynamicEyeColor(GetUV(varyings), lightingData.viewDirectionTS);
#endif
}
void ApplyPostLightingPerCharacterBaseMapOverride(inout half3 originalSurfaceColor, UVData uvData)
{
#if _NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE
    float2 uv = uvData.GetUV(_PerCharacterBaseMapOverrideUVIndex);
    uv -= 0.5;
    uv = CalcUV(uv,_PerCharacterBaseMapOverrideTilingOffset, _Time.y, _PerCharacterBaseMapOverrideUVScrollSpeed);
    uv += 0.5;
    half4 texValue = SAMPLE_TEXTURE2D(_PerCharacterBaseMapOverrideMap, sampler_BaseMap, uv);
    texValue.rgb *= _PerCharacterBaseMapOverrideTintColor;
    texValue.a *= _PerCharacterBaseMapOverrideAmount;

    half4 originalColorRGBA = half4(originalSurfaceColor.r,originalSurfaceColor.g,originalSurfaceColor.b,1);
    half4 resultColorRGBA = BlendColor(originalColorRGBA,texValue, _PerCharacterBaseMapOverrideBlendMode);

    originalSurfaceColor = resultColorRGBA.rgb;
    //we don't want to edit alpha, so just edit rgb
#endif
}

// [MatCap(Color Replace)]
void ApplyMatCapColorReplace(inout ToonSurfaceData surfaceData, Varyings varyings, ToonLightingData lightingData)
{
#if _MATCAP_BLEND
    half matCapAlphaBlendFinalUsage = _MatCapAlphaBlendUsage;

    // mask by an optional mask texture
    // (decided to not use shader_feature for this mask section, else too much shader_feature is used)
    half matCapAlphaBlendMask = dot(SAMPLE_TEXTURE2D(_MatCapAlphaBlendMaskMap, sampler_BaseMap, GetUV(varyings)),_MatCapAlphaBlendMaskMapChannelMask); // reuse sampler_BaseMap to save sampler count
    matCapAlphaBlendMask = _MatCapAlphaBlendMaskMapInvertColor? 1-matCapAlphaBlendMask : matCapAlphaBlendMask;
    matCapAlphaBlendMask = invLerpClamp(_MatCapAlphaBlendMaskMapRemapStart, _MatCapAlphaBlendMaskMapRemapEnd, matCapAlphaBlendMask);
    matCapAlphaBlendFinalUsage *= matCapAlphaBlendMask;

    float2 matCapBlendUV = lightingData.matcapUV.xy * 0.5 * _MatCapAlphaBlendUvScale + 0.5;
    half4 matCapTextureReadRGBA = tex2D(_MatCapAlphaBlendMap, matCapBlendUV) *_MatCapAlphaBlendTintColor;

    // do [alpha as mask]
    matCapAlphaBlendFinalUsage *= lerp(1,matCapTextureReadRGBA.a,_MatCapAlphaBlendMapAlphaAsMask); // allow MatCap texture's alpha channel as mask also
    surfaceData.albedo = lerp(surfaceData.albedo,matCapTextureReadRGBA.rgb,matCapAlphaBlendFinalUsage);
#endif
}

// [Mat Cap(additive)]
void ApplyMatCapAdditive(inout ToonSurfaceData surfaceData, Varyings varyings, ToonLightingData lightingData)
{
#if _MATCAP_ADD
    // read matcap texture
    float2 matCapAddUV = lightingData.matcapUV.xy * (0.5 * _MatCapAdditiveUvScale) + 0.5;
    half4 matCapTextureReadRGBA = tex2D(_MatCapAdditiveMap, matCapAddUV);
    matCapTextureReadRGBA = pow(abs(matCapTextureReadRGBA),abs(_MatCapAdditiveExtractBrightArea+1));

    half4 matCapAdditiveRGBA = _MatCapAdditiveColor; // contains alpha also

    // intensity slider
    matCapAdditiveRGBA.rgb *= _MatCapAdditiveIntensity; // only affect rgb

    // let user select mix with basecolor or not
    matCapAdditiveRGBA.rgb *= lerp(1,surfaceData.albedo,_MatCapAdditiveMixWithBaseMapColor);
    
    // mask by an optional mask texture
    // (decided to not use shader_feature for this mask section, else too much shader_feature is used)
    half matCapAdditiveMask = dot(SAMPLE_TEXTURE2D(_MatCapAdditiveMaskMap, sampler_BaseMap, GetUV(varyings)),_MatCapAdditiveMaskMapChannelMask); // reuse sampler_BaseMap to save sampler count
    matCapAdditiveMask = _MatCapAdditiveMaskMapInvertColor ? 1-matCapAdditiveMask : matCapAdditiveMask;
    matCapAdditiveMask = invLerpClamp(_MatCapAdditiveMaskMapRemapStart, _MatCapAdditiveMaskMapRemapEnd, matCapAdditiveMask);
    matCapAdditiveRGBA.rgb *= matCapAdditiveMask; // only affect rgb

    // mix RGBA with matcap texture
    half4 result = matCapTextureReadRGBA * matCapAdditiveRGBA;

    // do [alpha as mask]
    half alphaAsMask = lerp(1,result.a,_MatCapAdditiveMapAlphaAsMask); // allow MatCap texture's alpha channel as mask also
    result.rgb *= alphaAsMask;

    // render face mask
    if(_MatCapAdditiveApplytoFaces != 0)
    {
        if(_MatCapAdditiveApplytoFaces == 1) result.rgb *= saturate(-lightingData.facing);
        if(_MatCapAdditiveApplytoFaces == 2) result.rgb *= saturate(lightingData.facing);
    }

    half3 finalAdd = result.rgb;
    surfaceData.albedo += finalAdd;
    surfaceData.alpha = saturate(surfaceData.alpha + Luminance(finalAdd)); // semi-transparent's reflection should NOT be affected by alpha, so we add alpha on specular area to cancel it
#endif
}
// [Mat Cap(occlusion)]
void ApplyMatCapOcclusion(inout ToonSurfaceData surfaceData, Varyings varyings, ToonLightingData lightingData)
{
#if _MATCAP_OCCLUSION
    // read matcap texture
    float2 matCapOcclusionUV = lightingData.matcapUV.xy * (0.5 * _MatCapOcclusionUvScale) + 0.5;
    half4 matCapTextureReadRGBA = tex2D(_MatCapOcclusionMap, matCapOcclusionUV); 
    half matCapTextureReadOcclusion = dot(matCapTextureReadRGBA, _MatCapOcclusionMapChannelMask);
    matCapTextureReadOcclusion = invLerpClamp(_MatCapOcclusionMapRemapStart, _MatCapOcclusionMapRemapEnd, matCapTextureReadOcclusion); // remap

    // mask by an optional mask texture
    // (decided to not use shader_feature for this mask section, else too much shader_feature is used)
    half matCapOcclusionMask = dot(SAMPLE_TEXTURE2D(_MatCapOcclusionMaskMap, sampler_BaseMap, GetUV(varyings)),_MatCapOcclusionMaskMapChannelMask); // reuse sampler_BaseMap to save sampler count
    matCapOcclusionMask = _MatCapOcclusionMaskMapInvert ? 1-matCapOcclusionMask : matCapOcclusionMask;
    matCapOcclusionMask = invLerpClamp(_MatCapOcclusionMaskMapRemapStart, _MatCapOcclusionMaskMapRemapEnd, matCapOcclusionMask);

    // occlsion *= matcap texture occlsion & [alpha as mask]
    half resultOcclusion = lerp(1, matCapTextureReadOcclusion, saturate(matCapOcclusionMask * lerp(1,matCapTextureReadRGBA.a,_MatCapOcclusionMapAlphaAsMask) * _MatCapOcclusionIntensity));

    // apply to occlusion
    surfaceData.occlusion *= resultOcclusion;
#endif
}

// [Environment Reflections]
void ApplyEnvironmentReflections(inout ToonSurfaceData surfaceData, Varyings varyings, ToonLightingData lightingData)
{
    // [How to sample reflection probe correctly?]
    // see URP's GlobalIllumination.hlsl -> 
    // half3 GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    //  half3 bakedGI, half occlusion, float3 positionWS,
    //  half3 normalWS, half3 viewDirectionWS)
#if _ENVIRONMENTREFLECTIONS
    const half smoothness = saturate(_EnvironmentReflectionSmoothnessMultiplier * surfaceData.smoothness);

    // PerceptualRoughness is equal to the (1-raw smoothness texture) linear value 
    half roughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);

    #if UNITY_VERSION > 202220
    half3 GlossyEnvironmentReflectionResult = GlossyEnvironmentReflection(lightingData.reflectionVectorWS, lightingData.positionWS, roughness, surfaceData.occlusion, lightingData.normalizedScreenSpaceUV);
    #else
    half3 GlossyEnvironmentReflectionResult = GlossyEnvironmentReflection(lightingData.reflectionVectorWS, lightingData.positionWS, roughness, surfaceData.occlusion);
    #endif

    half3 environmentReflection = GlossyEnvironmentReflectionResult * _EnvironmentReflectionColor * _EnvironmentReflectionBrightness;
    environmentReflection *= lerp(1,surfaceData.albedo,_EnvironmentReflectionTintAlbedo);

    half applyIntensity = _EnvironmentReflectionUsage;

    // mask by an optional mask texture
    // (decided to not use shader_feature for this mask section, else too much shader_feature is used)
    half mask = dot(SAMPLE_TEXTURE2D(_EnvironmentReflectionMaskMap, sampler_BaseMap, GetUV(varyings)),_EnvironmentReflectionMaskMapChannelMask); // reuse sampler_BaseMap to save sampler count
    mask = _EnvironmentReflectionMaskMapInvertColor? 1-mask : mask;
    mask = invLerpClamp(_EnvironmentReflectionMaskMapRemapStart, _EnvironmentReflectionMaskMapRemapEnd, mask);
    applyIntensity *= mask; // only affect rgb

    // NdotV as mask (fresnel effect)
    applyIntensity *= lerp(1,invLerpClamp(_EnvironmentReflectionFresnelRemapStart,_EnvironmentReflectionFresnelRemapEnd,pow(1-lightingData.NdotV,abs(_EnvironmentReflectionFresnelPower))),_EnvironmentReflectionFresnelEffect);

    // render face mask (Both,0 | Front,2 | Back,1)
    applyIntensity *= !((_EnvironmentReflectionApplytoFaces == 1 && lightingData.facing == 1.0) || (_EnvironmentReflectionApplytoFaces == 2 && lightingData.facing == -1.0));

    // remove face area apply
    #if _ISFACE
    applyIntensity *= (1-lightingData.isFaceArea * (1-_EnvironmentReflectionShouldApplyToFaceArea));
    #endif
    
    // apply to albedo
    surfaceData.albedo = lerp(surfaceData.albedo,environmentReflection,applyIntensity * _EnvironmentReflectionApplyReplaceBlending);
    surfaceData.albedo += environmentReflection * applyIntensity * _EnvironmentReflectionApplyAddBlending;
    //surfaceData.alpha = lerp(surfaceData.alpha,1, applyIntensity); // TODO: find out a good way to do environment reflection, similar to matcap(additive) 
#endif
}

// [back face color tint]
void ApplyBackFaceColorTintIfForwardLitPass(inout half3 originalSurfaceColor, Varyings varyings, ToonLightingData lightingData, half facing)
{
#if NiloToonForwardLitPass
    originalSurfaceColor *= facing < 0 ? _BackFaceTintColor : 1;
#endif
}
void ApplyBackFaceForceShadowIfForwardLitPass(inout ToonSurfaceData surfaceData, ToonLightingData lightingData)
{
#if NiloToonForwardLitPass
    surfaceData.occlusion *= lightingData.facing < 0 ? 1 - _BackFaceForceShadow : 1;
#endif
}
void ApplyBackFaceReplaceBaseMapColorIfForwardLitPass(inout ToonSurfaceData surfaceData, ToonLightingData lightingData)
{
#if NiloToonForwardLitPass
    surfaceData.albedo = lightingData.facing < 0 ? lerp(surfaceData.albedo, _BackFaceBaseMapReplaceColor.rgb, _BackFaceBaseMapReplaceColor.a) : surfaceData.albedo;
#endif   
}

void ApplyAlbedoPreLightingEditIfOutlinePass(inout ToonSurfaceData surfaceData)
{
#if NiloToonIsAnyOutlinePass
    surfaceData.albedo = lerp(surfaceData.albedo, _OutlinePreLightingReplaceColor, _OutlineUsePreLightingReplaceColor);

    // override by texture
    //ApplyOutlineColorOverrideByTexture(originalSurfaceColor, lightingData);
#endif
}

void ApplyURPDecal(inout ToonSurfaceData surfaceData, Varyings varyings, ToonLightingData lightingData)
{
    #ifdef _DBUFFER
    half tempMetallic = 0; // we don't use metallic for now
    ApplyDecal(
        varyings.positionCS,
        surfaceData.albedo,
        surfaceData.specular,
        lightingData.normalWS,
        tempMetallic,
        surfaceData.occlusion,
        surfaceData.smoothness
        );
    #endif
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step4: calculate lighting & final color)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// Similar to URP's Lighting.hlsl -> UniversalFragmentPBR(...),
// This function contains no lighting logic, it just pass lighting results data around and sum them.
// The job done in this function is "do shadow mapping depth test positionWS offset"
half3 ShadeAllLights(inout ToonSurfaceData surfaceData, ToonLightingData lightingData, Varyings input, UVData uvData)
{
    ///////////////////////////////////////////////////////////////////
    // Main Directional Light
    ///////////////////////////////////////////////////////////////////
    //----------------------------------------------------------------------------
    // Light struct is provided by URP to abstract light shader variables.
    //
    // struct Light
    // {
    //     half3   direction;
    //     half3   color;
    //     float   distanceAttenuation; // full-float precision required on some platforms
    //     half    shadowAttenuation;
    //     uint    layerMask;
    // };
    //
    // URP takes different shading approaches depending on light and platform.
    // You should never reference light shader variables in your shader, instead use the 
    // - GetMainLight(...)
    // - GetAdditionalLight(...)
    // funcitons to fill this Light struct.
    // 
    // (see struct and function calls, see URP's RealtimeLights.hlsl)
    //----------------------------------------------------------------------------

    uint meshRenderingLayers = NiloGetMeshRenderingLayer();

    half3 mainLightResult = 0;
    half depthTexRimArea = 0;

#ifdef _LIGHT_LAYERS
    // Note:
    // Since NiloToon 0.16.0, all additional lights will be merged into lightingData.mainLight's color & direction,
    // so it doesn't make sense to use mainLight.layerMask to cull ShadeMainLight(...) anymore,
    // the chance of 0 lights in scene is so low, so we skip the if check completely now.
    //if (IsMatchingLightLayer(lightingData.mainLight.layerMask, meshRenderingLayers)) // Disabled since NiloToon 0.16.0
#endif
    {
        half4 result = ShadeMainLight(surfaceData, input, lightingData, lightingData.mainLight, uvData);
        mainLightResult = result.rgb;
        depthTexRimArea = result.a;
    }

    ///////////////////////////////////////////////////////////////////
    // additional light (as additive rim light in any style)
    ///////////////////////////////////////////////////////////////////
    half3 additionalLightResult = 0;
    
#if NeedCalculateAdditionalLight
    // NOTE: for additional light's sample code, you can copy from URP's Lighting.hlsl's UniversalFragmentPBR(...)

    // LIGHT_LOOP_BEGIN needs:
    // - inputData.normalizedScreenSpaceUV
    // - inputData.positionWS
    // so we create a temp InputData
    InputData inputData = (InputData)0;
    inputData.normalizedScreenSpaceUV = lightingData.normalizedScreenSpaceUV;
    inputData.positionWS = lightingData.positionWS;

    half3 additionalLightSum = 0;

    // shadowMask is related to lightmap's baked shadow mask, which we can ignore it by setting default value (1,1,1,1), 
    // we still need this to call the correct GetAdditionalLight(...) overload method
    half4 shadowMask = half4(1, 1, 1, 1); 

    uint pixelLightCount = GetAdditionalLightsCount();

    // this is an extra for-loop that only exists in Forward+,
    // and is only looping directional lights as additional light, not including main directional light, or any point/spot light
    // Note: as Directional lights would be in all clusters since they don't have a 3D volume, so they don't go into the cluster structure.
    // Instead, they are stored first in the light buffer array, so here we can loop them easily starting from index 0.
    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {                
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, input.positionWS_ZOffsetFinalSum.xyz, shadowMask); // since URP10, you must provide shadowMask in order to receive shadowmap

#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            NiloToonPerAdditionalLightData niloToonPerAdditionalLightData = GetNiloToonPerAdditionalLightData(lightIndex);
            
            // Different function(simpler and faster function) used to shade additional lights.
            additionalLightSum += CalculateAdditiveSingleAdditionalLight(light, lightingData) * niloToonPerAdditionalLightData.additiveLightIntensity;
        }
    }
    #endif

    // [If Forward+]
    // this is another for-loop, is for looping all effective point and spot additional lights, without any directional lights.
    // since point/spot lights have their own 3D volume inside camera frustum, URP cull them by building Forward+'s light list per 3D cell using CPU jobs for intersection test + binning.
    // [If non Forward+]
    // this is the only for-loop, and will loop all additional lights(maximum 8) in a simple Forward way,
    // loop can include any additional directional/point/spot lights
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, input.positionWS_ZOffsetFinalSum.xyz, shadowMask); // you must provide shadowMask in order to receive additional light's shadowmap

#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            NiloToonPerAdditionalLightData niloToonPerAdditionalLightData = GetNiloToonPerAdditionalLightData(lightIndex);
            
            // A different function(simpler and faster function) is used to shade additional lights.
            // which do not look as good as ShadeMainLight, but it is faster and support NiloToonCinematicRimLightVolume 
            additionalLightSum += CalculateAdditiveSingleAdditionalLight(light, lightingData)  * niloToonPerAdditionalLightData.additiveLightIntensity; 
        }
    LIGHT_LOOP_END

    // [NiloToon's Volume] max contribution clamp (will not clamp rim area)
    additionalLightSum = lerp(min(additionalLightSum,_GlobalAdditionalLightMaxContribution),additionalLightSum,depthTexRimArea);

    // [NiloToon's Volume] rim mask
    half additionalLightRimMask = pow(1-lightingData.NdotV_NoNormalMap,_GlobalAdditionalLightRimMaskPower);
    additionalLightRimMask = smoothstep(0.5-_GlobalAdditionalLightRimMaskSoftness,0.5+_GlobalAdditionalLightRimMaskSoftness,additionalLightRimMask);
    additionalLightSum *= lerp(1, additionalLightRimMask, _GlobalAdditionalLightApplyRimMask);

    // [NiloToon's Volume] cinematic 2D rim mask
    additionalLightSum *= lerp(1, depthTexRimArea, _GlobalCinematic2DRimMaskStrength);
    
    // [NiloToon's Volume] since light can become over bright after modification by Cinematic rim, so we remove light intensity for outline area, which looks better
#if NiloToonSelfOutlinePass
    additionalLightSum *= 1.0 - _GlobalCinematic3DRimMaskEnabled;
#endif

    // [NiloToon's Volume] intensity multiplier for different area
    additionalLightSum *= _GlobalAdditionalLightMultiplier;
    additionalLightSum *= lerp(1,_GlobalAdditionalLightMultiplierForFaceArea,lightingData.isFaceArea);
#if NiloToonSelfOutlinePass
    additionalLightSum *= _GlobalAdditionalLightMultiplierForOutlineArea;
#endif

    additionalLightResult = additionalLightSum * lerp(surfaceData.occlusion,1,_AdditionalLightIgnoreOcclusion);
    additionalLightResult *= lerp(surfaceData.albedo, 1, _GlobalCinematic3DRimMaskEnabled * (1-_GlobalCinematicRimTintBaseMap)); // when cinematic is on, additional light is rim light, so it should not * albedo 100%

    // when cinematic rim light is on, it is better to remove the additional light on face area,
    // since NiloToon's face normal is usually flatten to face forward direction, which can producing weird rim light.
    // TODO: make 3D rim light use raw normal, and provide option in cinematic rim light volume to allow showing face rim light
    additionalLightResult *= lerp(1,1-_GlobalCinematic3DRimMaskEnabled,lightingData.isFaceArea);
    
#endif

    ///////////////////////////////////////////////////////////////////
    // Emission
    ///////////////////////////////////////////////////////////////////
    half3 emissionResult = 0;
#if _EMISSION
    emissionResult = ShadeEmission(surfaceData, lightingData, lightingData.mainLight);
#endif

    ///////////////////////////////////////////////////////////////////
    // Composite all lighting result
    ///////////////////////////////////////////////////////////////////
    return CompositeAllLightResults(mainLightResult, additionalLightResult, emissionResult);
}


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step5: outline color edit)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ApplyOutlineColorOverrideByTexture(inout half3 originalSurfaceColor, ToonLightingData lightingData)
{
#if _OVERRIDE_OUTLINECOLOR_BY_TEXTURE
    half4 outlineColorOverrideTexSampleValue = tex2D(_OverrideOutlineColorTex, lightingData.uv) * _OverrideOutlineColorTexTintColor;
    outlineColorOverrideTexSampleValue.a = lerp(outlineColorOverrideTexSampleValue.a,1,_OverrideOutlineColorTexIgnoreAlphaChannel);
    originalSurfaceColor = lerp(originalSurfaceColor, outlineColorOverrideTexSampleValue.rgb, outlineColorOverrideTexSampleValue.a * _OverrideOutlineColorByTexIntensity);
#endif
}

void ApplySurfaceToClassicOutlineColorEdit(inout half3 originalSurfaceColor, ToonSurfaceData surfaceData, ToonLightingData lightingData)
{
    half3 outlineTintColor = lerp(_OutlineTintColor, _OutlineTintColorSkinAreaOverride.rgb, _OutlineTintColorSkinAreaOverride.a * lightingData.isSkinArea);
    originalSurfaceColor *= outlineTintColor * _PerCharacterOutlineColorTint * _GlobalOutlineTintColor;
    originalSurfaceColor *= lerp(_OutlineOcclusionAreaTintColor, 1, surfaceData.occlusion);

    // _OutlineUseReplaceColor will not account for lighting. To account for lighting, see _OutlineUsePreLightingReplaceColor
    originalSurfaceColor = lerp(originalSurfaceColor, _OutlineReplaceColor, _OutlineUseReplaceColor); 

    // override by texture
    ApplyOutlineColorOverrideByTexture(originalSurfaceColor, lightingData);

    // override by per char script gameplay effect
    originalSurfaceColor = lerp(originalSurfaceColor, _PerCharacterOutlineColorLerp.rgb, _PerCharacterOutlineColorLerp.a);
}
// [Second Pass Extrude Cull front Outline]
void ApplySurfaceToClassicOutlineColorEditIfOutlinePass(inout half3 originalSurfaceColor, ToonSurfaceData surfaceData, ToonLightingData lightingData)
{
#if NiloToonIsAnyOutlinePass
    ApplySurfaceToClassicOutlineColorEdit(originalSurfaceColor,surfaceData,lightingData);
#endif
}

void ApplySurfaceToScreenSpaceOutlineColorEditIfSurfacePass(inout half3 originalSurfaceColor, ToonSurfaceData surfaceData, ToonLightingData lightingData)
{
    // this function will only affect ForwardLit pass's surface pixels!
    // while outline pass will only use ApplySurfaceToOutlineColorEditIfOutlinePass(...), but not this function
#if NiloToonForwardLitPass && _SCREENSPACE_OUTLINE && _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE

    // if _ZWrite is off (= 0), screen space outline is disabled. 
    // because if _ZWrite is off (= 0), this material should be a transparent material,
    // and transparent material won't write to _CameraDepthTexture,
    // which makes screen space outline not correct (screen space outline rely on _CameraDepthTexture and _CameraNormalsTexture),
    // so disable screen space outline automatically if _Zwrite is off 
    if(_ZWrite)
    {
        // outline width
        float finalOutlineWidth = lerp(_ScreenSpaceOutlineWidth, _ScreenSpaceOutlineWidthIfFace, lightingData.isFaceArea) * _GlobalScreenSpaceOutlineWidthMultiplierForChar;

        // depth sensitivity
        float finalDepthSensitivity = max(0,lerp(_ScreenSpaceOutlineDepthSensitivity, _ScreenSpaceOutlineDepthSensitivityIfFace, lightingData.isFaceArea) + _GlobalScreenSpaceOutlineDepthSensitivityOffsetForChar);
        // depth sensitivity texture apply
        half4 depthSensitivityTexValue = tex2D(_ScreenSpaceOutlineDepthSensitivityTex, lightingData.uv);
        half depthSensitivityMultiplierByTex = dot(depthSensitivityTexValue, _ScreenSpaceOutlineDepthSensitivityTexChannelMask);
        depthSensitivityMultiplierByTex = invLerpClamp(_ScreenSpaceOutlineDepthSensitivityTexRemapStart, _ScreenSpaceOutlineDepthSensitivityTexRemapEnd, depthSensitivityMultiplierByTex); // should remap first,
        finalDepthSensitivity *= depthSensitivityMultiplierByTex;

        // normals sensitivity
        float finalNormalsSensitivity = max(0,lerp(_ScreenSpaceOutlineNormalsSensitivity, _ScreenSpaceOutlineNormalsSensitivityIfFace, lightingData.isFaceArea) + _GlobalScreenSpaceOutlineNormalsSensitivityOffsetForChar);
        // normals sensitivity texture apply
        half4 normalsSensitivityTexValue = tex2D(_ScreenSpaceOutlineNormalsSensitivityTex, lightingData.uv);
        half normalsSensitivityMultiplierByTex = dot(normalsSensitivityTexValue, _ScreenSpaceOutlineNormalsSensitivityTexChannelMask);
        normalsSensitivityMultiplierByTex = invLerpClamp(_ScreenSpaceOutlineNormalsSensitivityTexRemapStart, _ScreenSpaceOutlineNormalsSensitivityTexRemapEnd, normalsSensitivityMultiplierByTex); // should remap first,
        finalNormalsSensitivity *= normalsSensitivityMultiplierByTex;
        //----------------------------------------------------------------------------------------------------------------------------------------
        float isScreenSpaceOutlineArea = IsScreenSpaceOutline(lightingData.SV_POSITIONxy, 
            finalOutlineWidth, 
            finalDepthSensitivity, 
            finalNormalsSensitivity, 
            lightingData.selfLinearEyeDepth, 
            _GlobalScreenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForChar,
            _CurrentCameraFOV,
            lightingData.viewDirectionWS);
        
        isScreenSpaceOutlineArea *= _GlobalScreenSpaceOutlineIntensityForChar; // intensity control
        originalSurfaceColor *= lerp(1,_ScreenSpaceOutlineTintColor * _GlobalScreenSpaceOutlineTintColorForChar,isScreenSpaceOutlineArea);
        originalSurfaceColor *= lerp(_ScreenSpaceOutlineOcclusionAreaTintColor, 1, surfaceData.occlusion);

        originalSurfaceColor = lerp(originalSurfaceColor, _ScreenSpaceOutlineReplaceColor, _ScreenSpaceOutlineUseReplaceColor * isScreenSpaceOutlineArea);
        
        // override by texture
        half3 originalSurfaceColorBeforeOverrideByTexture = originalSurfaceColor;
        ApplyOutlineColorOverrideByTexture(originalSurfaceColor, lightingData);
        originalSurfaceColor = lerp(originalSurfaceColorBeforeOverrideByTexture, originalSurfaceColor, isScreenSpaceOutlineArea);       
    }
#endif
}
void ApplySurfaceToScreenSpaceOutlineV2ColorEditIfSurfacePass(inout half3 originalSurfaceColor, ToonSurfaceData surfaceData, ToonLightingData lightingData)
{
    // this function will only affect ForwardLit pass's surface pixels!
    // while outline pass will only use ApplySurfaceToOutlineColorEditIfOutlinePass(...), but not this function
    #if NiloToonForwardLitPass && _SCREENSPACE_OUTLINE_V2 && _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2
    // if _ZWrite is off (= 0), screen space outline is disabled. 
    // because if _ZWrite is off (= 0), this material should be a transparent material,
    // and transparent material won't write to _CameraDepthTexture,
    // which makes screen space outline not correct (screen space outline rely on _CameraDepthTexture and _CameraNormalsTexture),
    // so disable screen space outline automatically if _Zwrite is off 
    if(_ZWrite)
    {
        // TODO: receive from renderer feature V2
        float width = _GlobalScreenSpaceOutlineV2WidthMultiplierForChar;
        float GeometryEdgeThreshold = _GlobalScreenSpaceOutlineV2GeometryEdgeThresholdForChar;
        float NormalAngleCosThetaThresholdMin = _GlobalScreenSpaceOutlineV2NormalAngleCosThetaThresholdForChar;
        float NormalAngleCosThetaThresholdMax = cos(DegToRad(180));
        bool DrawGeometryEdge = _GlobalScreenSpaceOutlineV2EnableGeometryEdgeForChar;
        bool DrawNormalAngleEdge = _GlobalScreenSpaceOutlineV2EnableNormalAngleEdgeForChar;
        bool DrawMaterialIDBoundary = true;
        bool DrawCustomIDBoundary = true;
        bool DrawWireframe = false;

        if(lightingData.isFaceArea)
        {
            DrawGeometryEdge = false;
            DrawNormalAngleEdge = false;
        }
        if(lightingData.isSkinArea)
        {
            DrawNormalAngleEdge = false;
        }

        float isScreenSpaceOutlineArea = IsScreenSpaceOutlineV2(
            lightingData.SV_POSITIONxy, 
            width, 
            GeometryEdgeThreshold, 
            NormalAngleCosThetaThresholdMin, 
            NormalAngleCosThetaThresholdMax,
            DrawGeometryEdge,
            DrawNormalAngleEdge,
            DrawMaterialIDBoundary, 
            DrawCustomIDBoundary,
            DrawWireframe,
            lightingData.positionWS);

        half3 outlineSurfaceColor = originalSurfaceColor;
        ApplySurfaceToClassicOutlineColorEdit(outlineSurfaceColor,surfaceData,lightingData);
        originalSurfaceColor = lerp(originalSurfaceColor,outlineSurfaceColor,isScreenSpaceOutlineArea * _GlobalScreenSpaceOutlineV2IntensityForChar);
    }
    #endif
}
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step6: per volume and per character color edit)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// [Volume]
void ApplyVolumeEffectColorEdit(inout half3 color)
{
    //////////////////////////////////////////////////////////////////////////////////
    // (not lighting, usually for cinematic needs like cut scene)
    //////////////////////////////////////////////////////////////////////////////////
    color *= _GlobalVolumeMulColor;
    color = lerp(color,_GlobalVolumeLerpColor.rgb,_GlobalVolumeLerpColor.a);
}
// [Per Character script]
void ApplyPerCharacterEffectColorEdit(inout half3 color, ToonLightingData lightingData)
{
    //////////////////////////////////////////////////////////////////////////////////
    // (not lighting, usually for gameplay needs like character selection)
    //////////////////////////////////////////////////////////////////////////////////
    // desaturate
    color = lerp(color,Luminance(color), _PerCharEffectDesaturatePercentage);

    // mul add
    color.rgb = color.rgb * _PerCharEffectTintColor + _PerCharEffectAddColor;

    // lerp
    color.rgb = lerp(color.rgb,_PerCharEffectLerpColor.rgb,_PerCharEffectLerpColor.a);

    // rim light (outline pass don't need this)
#if NiloToonForwardLitPass
    // Use PolygonNdotV instead of NdotV,
    // because ToonLightingData's NdotV is normalmap applied & normalized, we DONT want normalmap affecting rim light here.
    half NdotV = lightingData.NdotV_NoNormalMap;
    half rim = 1-NdotV;

    rim = pow(rim,_PerCharEffectRimSharpnessPower);

    // apply(additive)
    color.rgb += (rim * (1-lightingData.isFaceArea)) * _PerCharEffectRimColor; // rim light not show on face because it looks ugly
#endif

}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step7: dissolve)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void ApplyDissolve(inout half3 color, Varyings input, ToonLightingData lightingData, UVData uvData)
{
#if _NILOTOON_DISSOLVE
    // matching to NiloToonPerCharacterRenderController.cs's enum DissolveMode
    #define DISSOLVEMODE_UV1 1
    #define DISSOLVEMODE_UV2 2
    #define DISSOLVEMODE_WorldSpaceNoise 3
    #define DISSOLVEMODE_WorldSpaceVerticalUpward 4
    #define DISSOLVEMODE_WorldSpaceVerticalDownward 5
    #define DISSOLVEMODE_ScreenSpaceNoise 6
    #define DISSOLVEMODE_ScreenSpaceVerticalUpward 7
    #define DISSOLVEMODE_ScreenSpaceVerticalDownward 8

    half finalDissolveAmount = _DissolveAmount * _AllowPerCharacterDissolve;
    if(finalDissolveAmount <= 0) return;

    float noiseStrength = _DissolveNoiseStrength * 0.1;
    float2 uvTiling = float2(_DissolveThresholdMapTilingX,_DissolveThresholdMapTilingY);

    half dissolveMapThresholdMapValue = 0;

    // UV1
    if(_DissolveMode == DISSOLVEMODE_UV1)
    {
        float2 uv = GetUV(input) * uvTiling;
        dissolveMapThresholdMapValue = tex2D(_DissolveThresholdMap, uv).g;
    } 
    else  
    // UV2
    if(_DissolveMode == DISSOLVEMODE_UV2)
    {
        float2 uv =  uvData.GetUV(1) * uvTiling;
        dissolveMapThresholdMapValue = tex2D(_DissolveThresholdMap, uv).g;
    }
    else
    // WorldSpaceNoise
    if(_DissolveMode == DISSOLVEMODE_WorldSpaceNoise)
    {
        float2 uv; 

        uv = lightingData.positionWS.xz * uvTiling;
        dissolveMapThresholdMapValue = tex2D(_DissolveThresholdMap, uv).g;

        uv = lightingData.positionWS.xy * uvTiling;
        dissolveMapThresholdMapValue *= tex2D(_DissolveThresholdMap, uv).g;

        uv = lightingData.positionWS.yz * uvTiling;
        dissolveMapThresholdMapValue *= tex2D(_DissolveThresholdMap, uv).g;
    }
    else
    // WorldSpaceVerticalUpward
    if(_DissolveMode == DISSOLVEMODE_WorldSpaceVerticalUpward)
    {
        float2 uv = lightingData.positionWS.xz * uvTiling;
        float noise = tex2D(_DissolveThresholdMap, uv).g * noiseStrength;
        dissolveMapThresholdMapValue = invLerpClamp(lightingData.characterBoundBottomWS_positionWS.y,lightingData.characterBoundTopWS_positionWS.y,lightingData.positionWS.y + noise);
    }
    else
    // WorldSpaceVerticalDownward
    if(_DissolveMode == DISSOLVEMODE_WorldSpaceVerticalDownward)
    {
        float2 uv = lightingData.positionWS.xz * uvTiling;
        float noise = tex2D(_DissolveThresholdMap, uv).g * noiseStrength;
        dissolveMapThresholdMapValue = invLerpClamp(lightingData.characterBoundTopWS_positionWS.y,lightingData.characterBoundBottomWS_positionWS.y,lightingData.positionWS.y + noise);
    }
    else
    // ScreenSpaceNoise
    if(_DissolveMode == DISSOLVEMODE_ScreenSpaceNoise)
    {
        // since the default bounding sphere is 2.5m, when compared to world space, it should have 2.5x Tiling
        float2 uv = lightingData.characterBound2DRectUV01 * uvTiling * 2.5; 
        dissolveMapThresholdMapValue = tex2D(_DissolveThresholdMap, uv).g;
    }
    else
    // ScreenSpaceVerticalUpward
    if(_DissolveMode == DISSOLVEMODE_ScreenSpaceVerticalUpward)
    {
        // since the default bounding sphere is 2.5m, when compared to world space, it should have 2.5x Tiling
        float2 uv = lightingData.characterBound2DRectUV01 * uvTiling * 2.5;
        float noise = tex2D(_DissolveThresholdMap, uv).g * noiseStrength;
        dissolveMapThresholdMapValue = saturate(lightingData.characterBound2DRectUV01.y + noise);
    }
    else
    // ScreenSpaceVerticalDownward
    if(_DissolveMode == DISSOLVEMODE_ScreenSpaceVerticalDownward)
    {
        // since the default bounding sphere is 2.5m, when compared to world space, it should have 2.5x Tiling
        float2 uv = lightingData.characterBound2DRectUV01 * uvTiling * 2.5;
        float noise = tex2D(_DissolveThresholdMap, uv).g * noiseStrength;
        dissolveMapThresholdMapValue = saturate((1-lightingData.characterBound2DRectUV01.y) + noise);
    }
    
    half dissolve = dissolveMapThresholdMapValue - finalDissolveAmount;

    // clip threshold
    clip(dissolve-0.0001);

    // HDR color tint to "near threshold area" 
    color = lerp(color, _DissolveBorderTintColor, smoothstep(finalDissolveAmount + _DissolveBorderRange, finalDissolveAmount, dissolveMapThresholdMapValue));
#endif
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step8: fog)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ApplyFog(inout half3 color, Varyings input)
{
    // Mix the pixel color with fog color. 
    // You can optionally use MixFogColor to override the fog color with a custom one.
    float fogCoord = InitializeInputDataFog(float4(input.positionWS_ZOffsetFinalSum.xyz, 1.0), input.SH_fogFactor.w);
    color = MixFog(color, fogCoord);
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step9: override final output alpha)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ApplyOverrideOutputAlpha(inout half outputAlpha)
{
    // not worth an if() here, so use a?b:c
    outputAlpha = _EditFinalOutputAlphaEnable ? (_ForceFinalOutputAlphaEqualsOne ? 1.0 : outputAlpha) : outputAlpha;

    // auto output "alpha == 1" for default opaque material (_SrcBlend = One, _DstBlend = Zero)
    // since when rendering opaque material, most of the cases the alpha value on RT is expected to be one, but not alpha value of _BaseMap
    // forcing 1 will make rendering character mask(RT's alpha) always correct.
    // https://docs.unity3d.com/ScriptReference/Rendering.BlendMode.html
    if((_SrcBlend == 1) && (_DstBlend == 0))
        outputAlpha = 1;

#if NiloToonSelfOutlinePass
    outputAlpha = 1; // outline pass always render as opaque colors, semi-transparent outline is not allowed, so for outline we force output alpha = 1 to make alpha correct when user use RenderTexture's alpha as mask
#endif
}

void InitAllData(inout Varyings input, float facing, out UVData uvData, out ToonLightingData lightingData, out ToonSurfaceData surfaceData)
{
    //////////////////////////////////////////////////////////////////////////////////////////
    // Apply Parallax, edit uv before any uv copy or texture sampling
    //////////////////////////////////////////////////////////////////////////////////////////
    ApplyPerPixelDisplacement(input);

    //////////////////////////////////////////////////////////////////////////////////////////
    // UV0~3 copy to struct
    //////////////////////////////////////////////////////////////////////////////////////////
    uvData = InitializeUV0ToUV3(input);

    //////////////////////////////////////////////////////////////////////////////////////////
    // prepare all data struct for lighting function
    //////////////////////////////////////////////////////////////////////////////////////////

    half3 normalTS = GetFinalNormalTS(uvData,facing);

    // normalTS apply detail normal
    half detailMask = 1;
    float2 detailUV = 0;
    #if _DETAIL && !_NILOTOON_DEBUG_SHADING
    detailMask = dot(_DetailMaskChannelMask, SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, GetUV(input)));
    detailMask = _DetailMaskInvertColor? 1-detailMask : detailMask;
    detailUV = uvData.GetUV(_DetailUseSecondUv ? 1 : 0) * _DetailMapsScaleTiling.xy + _DetailMapsScaleTiling.zw; // TODO: we want to allow UV0~3, but it will be a breaking change of _DetailUseSecondUv
    normalTS = ApplyDetailNormal(detailUV, normalTS, detailMask);
    #endif

    lightingData = InitializeLightingData(input,normalTS, facing);

    // fill-in remaining UVData slots
    uvData.allUVs[4] = lightingData.matcapUV * 0.5 + 0.5;
    uvData.allUVs[5] = lightingData.characterBound2DRectUV01;
    uvData.allUVs[6] = lightingData.aspectCorrectedScreenSpaceUV;

    surfaceData = InitializeSurfaceData(input, uvData, facing, normalTS, detailMask, detailUV);
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// only NiloToonCharacter.shader will be calling this function by using
// #pragma fragment FragmentShaderAllWork
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// [Note: VFACE vs SV_IsFrontFace] 
// Don't use VFACE anymore(it is a DX9 legacy semantics), use SV_IsFrontFace instead, 
// because VFACE can't compile correctly(no compile error log, but rendering is wrong in build) on DX12(XBox One GameCore)
// - https://forum.unity.com/threads/using-vface-in-surface-shader.460941/#post-3787066
// - https://github.com/microsoft/DirectXShaderCompiler/issues/3494
// - https://forum.unity.com/threads/unity-is-adding-a-new-dxc-hlsl-compiler-backend-option.1086272/
// - https://docs.google.com/document/d/1yHARKE5NwOGmWKZY2z3EPwSz5V_ZxTDT8RnRl521iyE/edit#
// We now use these highlevel define and macro provided by Unity:
// - FRONT_FACE_TYPE (usually defined as -> bool)
// - FRONT_FACE_SEMANTIC (usually defined as -> SV_IsFrontFace)
// - IS_FRONT_VFACE(IsFrontFace, 1.0, -1.0) (usually defined as -> IsFrontFace ? 1.0 : -1.0)
void FragmentShaderAllWork(Varyings input, FRONT_FACE_TYPE IsFrontFace : FRONT_FACE_SEMANTIC
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
    )
{
    // [Note: SV_POSITION (input.positionCS)'s value in fragment shader]
    // In the vertex shader, you output SV_POSITION, which contains the clip space position (positionCS) after the model-view-projection (MVP) transformation.
    // In the fragment shader, if you read SV_POSITION's value, it's not the same as the clip space position from the vertex shader, because it has been transformed to screen space coordinates.
    // When you access input.positionCS.xyzw (SV_POSITION) in the fragment shader, you will get:
    // - x: for DX10 or later = the pixel center position: integer index x on RenderTarget + 0.5  (e.g. 0.5~1919.5 in a 1920x1080 render target), you can use input.positionCS.xy for LOAD texture. You can also get a screen space 0~1 uv by dividing it by the render target width
    // - y: for DX10 or later = the pixel center position: integer index y on RenderTarget + 0.5  (e.g. 0.5~1079.5 in a 1920x1080 render target), you can use input.positionCS.xy for LOAD texture. You can also get a screen space 0~1 uv by dividing it by the render target height
    // - z: SV_DEPTH, it is the same as NDC's z, the depth value that is written to the depth buffer, it is 0~1 for all graphics API
    // - w: positive value of view space depth or view space z, which is the same as the w component in clip space (positionCS)
    // or
    // fragment SV_POSITION.x = (CLIP_POSITION.x / CLIP_POSITION.w + 1) / 2 * w
    // fragment SV_POSITION.y = (CLIP_POSITION.y / CLIP_POSITION.w + 1) / 2 * h
    // fragment SV_POSITION.z = CLIP_POSITION.z / CLIP_POSITION.w
    // fragment SV_POSITION.w = CLIP_POSITION.w
    // * the NDC range of DirectX is x：[-1, 1]，y：[-1, 1]，z：[0, 1]
    // * For more information about SV_POSITION's value in the fragment shader, see:
    // https://zhuanlan.zhihu.com/p/597918725
    // https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics
    // https://zhuanlan.zhihu.com/p/455189480

    // to support GPU instancing and Single Pass Stereo rendering(VR), add the following section
    //------------------------------------------------------------------------------------------------------------------------------
    // see ShadowCasterPass.hlsl for why these lines are removed for NiloToonCharSelfShadowCasterPass
    #if !NiloToonCharSelfShadowCasterPass
    UNITY_SETUP_INSTANCE_ID(input);                     // in non OpenGL and non PSSL, MACRO will turn into -> UnitySetupInstanceID(input.instanceID);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);    // in non OpenGL and non PSSL, MACRO will turn into -> unity_StereoEyeIndex = input.stereoTargetEyeIndexAsRTArrayIdx;
    #endif
    //------------------------------------------------------------------------------------------------------------------------------

    //////////////////////////////////////////////////////////////////////////////////////////
    // flip normalWS by IS_FRONT_VFACE()
    //////////////////////////////////////////////////////////////////////////////////////////
    float facing = IS_FRONT_VFACE(IsFrontFace, 1.0, -1.0);

    // not apply "flip normal" to "NiloToonOutline" lit color pass, because we need to original normal for light shading and shadowmap normal bias
#if !NiloToonIsAnyOutlinePass
    input.normalWS_averageShadowAttenuation.xyz *= facing;
    input.smoothedNormalWS *= facing;
#endif
    
    //////////////////////////////////////////////////////////////////////////////////////////
    // insert a performance debug minimum shader early exit section here
    //////////////////////////////////////////////////////////////////////////////////////////
    // exit asap, to maximize performance difference
    // doing this can create a net GPU time difference between "Unlit/Texture" vs "full fragment shader"
    // which reflect the cost of this fragment shader quite correctly on target device
#if _NILOTOON_FORCE_MINIMUM_SHADER
    outColor = Get_NILOTOON_FORCE_MINIMUM_FRAGMENT_SHADER_result(input);
    return;
#endif

    //////////////////////////////////////////////////////////////////////////////////////////
    // Init all data structs
    //////////////////////////////////////////////////////////////////////////////////////////
    UVData uvData;
    ToonLightingData lightingData;
    ToonSurfaceData surfaceData;
    InitAllData(input, facing, uvData, lightingData, surfaceData);

    //////////////////////////////////////////////////////////////////////////////////////////
    // insert debug shading early exit section here
    //////////////////////////////////////////////////////////////////////////////////////////
#if _NILOTOON_DEBUG_SHADING
    outColor = Get_NILOTOON_DEBUG_SHADING_result(input, surfaceData, lightingData, uvData);
    return;
#endif

    //////////////////////////////////////////////////////////////////////////////////////////
    // apply struct ToonSurfaceData's edit (using lightingData's data also)
    //////////////////////////////////////////////////////////////////////////////////////////
    // (apply first) edit albedo
    ApplyDynamicEye(surfaceData, input, lightingData);
    ApplyMatCapColorReplace(surfaceData, input, lightingData);
    // (apply later) also edit albedo, by reflection features 
    ApplyEnvironmentReflections(surfaceData, input, lightingData);
    ApplyMatCapAdditive(surfaceData, input, lightingData);
    // (apply last) replace albedo for back face
    ApplyBackFaceReplaceBaseMapColorIfForwardLitPass(surfaceData, lightingData);
    
    // edit occlusion
    ApplyMatCapOcclusion(surfaceData, input, lightingData);
    ApplyBackFaceForceShadowIfForwardLitPass(surfaceData, lightingData);
    
    //////////////////////////////////////////////////////////////////////////////////////////
    // apply URP decal
    //////////////////////////////////////////////////////////////////////////////////////////
    ApplyURPDecal(surfaceData, input, lightingData);

    //////////////////////////////////////////////////////////////////////////////////////////
    // apply pre-lighting outline color calculation
    //////////////////////////////////////////////////////////////////////////////////////////
    ApplyAlbedoPreLightingEditIfOutlinePass(surfaceData);

    //////////////////////////////////////////////////////////////////////////////////////////
    // apply all lighting calculation
    //////////////////////////////////////////////////////////////////////////////////////////    
    half3 color = ShadeAllLights(surfaceData, lightingData, input, uvData);

    //////////////////////////////////////////////////////////////////////////////////////////
    // apply outline color calculation
    //////////////////////////////////////////////////////////////////////////////////////////  
    ApplySurfaceToClassicOutlineColorEditIfOutlinePass(color, surfaceData, lightingData);
    ApplySurfaceToScreenSpaceOutlineColorEditIfSurfacePass(color, surfaceData, lightingData);
    ApplySurfaceToScreenSpaceOutlineV2ColorEditIfSurfacePass(color, surfaceData, lightingData);

    //////////////////////////////////////////////////////////////////////////////////////////
    // apply per "material>character>volume" color edit
    // (Apply this section after outline, due to outline color user settings are usually for the original color)
    //////////////////////////////////////////////////////////////////////////////////////////
    // apply order = from local to global (per material > per character > per volume(global))
    ApplyBackFaceColorTintIfForwardLitPass(color, input, lightingData, facing);
    ApplyPostLightingPerCharacterBaseMapOverride(color, uvData);
    ApplyPerCharacterEffectColorEdit(color, lightingData);  
    ApplyVolumeEffectColorEdit(color);

    //////////////////////////////////////////////////////////////////////////////////////////
    // apply dissolve
    //////////////////////////////////////////////////////////////////////////////////////////
    ApplyDissolve(color, input, lightingData, uvData);

    //////////////////////////////////////////////////////////////////////////////////////////
    // apply fog (with extend logic functions before and after fog)
    //////////////////////////////////////////////////////////////////////////////////////////
    ApplyExternalAssetSupportLogicBeforeFog(color, surfaceData, lightingData, input, uvData);
    ApplyCustomUserLogicBeforeFog(color, surfaceData, lightingData, input, uvData);
    ApplyFog(color, input);
    ApplyExternalAssetSupportLogicAfterFog(color, surfaceData, lightingData, input, uvData);
    ApplyCustomUserLogicAfterFog(color, surfaceData, lightingData, input, uvData);

    //////////////////////////////////////////////////////////////////////////////////////////
    // override output alpha
    //////////////////////////////////////////////////////////////////////////////////////////
    ApplyOverrideOutputAlpha(surfaceData.alpha);

    //////////////////////////////////////////////////////////////////////////////////////////
    // output
    //////////////////////////////////////////////////////////////////////////////////////////
    outColor = half4(color, surfaceData.alpha);

    // pre-multiply alpha option
    outColor.rgb *= _PreMultiplyAlphaIntoRGBOutput ? outColor.a : 1;

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions 
// (only for ShadowCaster pass, DepthOnly and NiloToonSelfShadowCaster pass to use only)
// (not used in other pass)
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
half BaseColorAlphaClipTest(Varyings input, FRONT_FACE_TYPE IsFrontFace : FRONT_FACE_SEMANTIC) : SV_TARGET
{
    #if !NiloToonCharSelfShadowCasterPass
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    #endif
    //////////////////////////////////////////////////////////////////////////////////////////
    // flip normalWS by IS_FRONT_VFACE()
    //////////////////////////////////////////////////////////////////////////////////////////
    float facing = IS_FRONT_VFACE(IsFrontFace, 1.0, -1.0);
    
    //////////////////////////////////////////////////////////////////////////////////////////
    // Init all data structs
    //////////////////////////////////////////////////////////////////////////////////////////
    UVData uvData;
    ToonLightingData lightingData;
    ToonSurfaceData surfaceData;
    InitAllData(input, facing, uvData, lightingData, surfaceData);

    //////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////
    // if _AlphaClip is off, there is no reason to call GetFinalBaseColor()
    // so here we use #if _ALPHATEST_ON to avoid calling GetFinalBaseColor() as an optimization
#if _ALPHATEST_ON
    DoClipTestToTargetAlphaValue(GetFinalBaseColor(input,uvData, facing).a);
#endif

    // dither fade out should affect URP's shadowmap and depth texture / depth normal texture also
#if _NILOTOON_DITHER_FADEOUT
    NiloDoDitherFadeoutClip(input.positionCS.xy, 1-_DitherFadeoutAmount*_AllowPerCharacterDitherFadeout);
#endif

    // dissolve should affect URP's shadowmap and depth texture / depth normal texture also
    half3 dummy = half3(0,0,0);
    ApplyDissolve(dummy,input,lightingData, uvData);

    return input.positionCS.z;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions 
// (only for DepthNormal pass to use only)
// (not used in other pass)
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void BaseColorAlphaClipTest_AndDepthNormalColorOutput(Varyings input, FRONT_FACE_TYPE IsFrontFace : FRONT_FACE_SEMANTIC
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif 
    )
{
    #if !NiloToonCharSelfShadowCasterPass
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    #endif
    BaseColorAlphaClipTest(input, IsFrontFace);
    
    #if defined(_GBUFFER_NORMALS_OCT)
    float3 normalWS = normalize(input.normalWS_averageShadowAttenuation.xyz);
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms.
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
    outNormalWS = half4(packedNormalWS, 0.0);
    #else
    float3 normalWS = NormalizeNormalPerPixel(input.normalWS_averageShadowAttenuation.xyz);
    outNormalWS = half4(normalWS, 0.0);
    #endif

    // TODO: should we apply?
    //outNormalWS *= saturate(1 - _DitherFadeoutAmount * _AllowPerCharacterDitherFadeout * _DitherFadeoutNormalScaleFix);
    
    #ifdef _WRITE_RENDERING_LAYERS
        uint renderingLayers = GetMeshRenderingLayer();
        outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
    #endif
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions 
// (only for NiloToonPrepassBuffer pass to use only)
// (not used in other pass)
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
float4 BaseColorAlphaClipTest_AndNiloToonPrepassBufferColorOutput(Varyings input, FRONT_FACE_TYPE IsFrontFace : FRONT_FACE_SEMANTIC) : SV_TARGET
{
    BaseColorAlphaClipTest(input, IsFrontFace);

    // use the same method to calculate linear depth from self(SV_POSITION.z) & scene(Load(_CameraDepthTexture))
    float sceneLinearDepth = Convert_SV_PositionZ_ToLinearViewSpaceDepth(LoadSceneDepth(input.positionCS.xy));
    float selfLinearDepth = Convert_SV_PositionZ_ToLinearViewSpaceDepth(input.positionCS.z);
    
    // When the RT depth and MSAA format of _CameraDepthTexture match _NiloToonPrepassBufferRT,
    // we can perform precise depth comparisons to reject "scene blocked char pixels."
    // but still, it is possible to have problems similar Shadow acne, so a very small depth bias may help 
    float depthBias;
    // Bias should not be affected by platform, since platform difference (Z reverse) is handled inside Convert_SV_PositionZ_ToLinearViewSpaceDepth()'s LinearEyeDepth()
    // DX11 = - correct , + wrong
    // Vulkan = - correct , + wrong
    // OPENGLES = - correct , + wrong
    depthBias = - 0.00001; //0.00001 is the minimum number to work, should we allow user to increase it? (expose in renderer feature?)
    
    if(sceneLinearDepth < selfLinearDepth + depthBias)
        return 0;

    // if character pixel is still visible(not blocked by scene), draw to _NiloToonPrepassBufferTex
    return float4(0,_AllowNiloToonBloomCharacterAreaOverride * _AllowedNiloToonBloomOverrideStrength,0,1);
}