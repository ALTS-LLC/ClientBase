// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safeguard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

//--------------------------------------------------------
// + Rider hlsl code highlight and autocomplete support for all shader_feature and multi_compile sections
// (__RESHARPER__ is only defined while in IDE. Used to help editing this file with proper highlighting.)
#ifdef __RESHARPER__ 

// shader_feature_local / multi_complile_local define
#define _ISFACE 1
#define _FACE_SHADOW_GRADIENTMAP 1
#define _OCCLUSIONMAP 1
#define _MATCAP_OCCLUSION 1
#define _SHADING_GRADEMAP 1
#define _OVERRIDE_SHADOWCOLOR_BY_TEXTURE 1
#define _DEPTHTEX_RIMLIGHT_FIX_DOTTED_LINE_ARTIFACTS 1
#define _SCREENSPACE_OUTLINE 1
#define _SCREENSPACE_OUTLINE_V2 1
#define _PARALLAXMAP 1
#define _MATCAP_BLEND 1
#define _ENVIRONMENTREFLECTIONS 1
#define _EMISSION 1
#define _BASEMAP_STACKING_LAYER1 1
#define _BASEMAP_STACKING_LAYER2 1
#define _BASEMAP_STACKING_LAYER3 1
#define _BASEMAP_STACKING_LAYER4 1
#define _BASEMAP_STACKING_LAYER5 1
#define _BASEMAP_STACKING_LAYER6 1
#define _BASEMAP_STACKING_LAYER7 1
#define _BASEMAP_STACKING_LAYER8 1
#define _BASEMAP_STACKING_LAYER9 1
#define _BASEMAP_STACKING_LAYER10 1
#define _ALPHAOVERRIDEMAP 1
#define _NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE 1
#define _NILOTOON_DISSOLVE 1
#define _ADDITIONAL_LIGHTS 1
#define _ADDITIONAL_LIGHTS_VERTEX 1
#define _NILOTOON_RECEIVE_SELF_SHADOW 1
#define _OVERRIDE_SHADOWCOLOR_BY_TEXTURE 1
#define _NORMALMAP 1
#define _DYNAMIC_EYE 1
#define _MATCAP_ADD 1
#define _SKIN_MASK_ON 1
#define _SPECULARHIGHLIGHTS 1
#define _SPECULARHIGHLIGHTS_TEX_TINT 1
#define _SMOOTHNESSMAP 1
#define _FACE_MASK_ON 1
#define _ZOFFSETMAP 1
#define _OUTLINEWIDTHMAP 1
#define _OUTLINEZOFFSETMAP 1
#define _DETAIL 1
#define _OVERRIDE_OUTLINECOLOR_BY_TEXTURE 1
#define _DEPTHTEX_RIMLIGHT_SHADOW_WIDTHMAP 1
#define _NILOTOON_SELFSHADOW_INTENSITY_MAP 1
#define _KAJIYAKAY_SPECULAR 1
#define _KAJIYAKAY_SPECULAR_TEX_TINT 1
#define _RAMP_LIGHTING 1
#define _RAMP_LIGHTING_SAMPLE_UVY_TEX 1
#define _RAMP_SPECULAR 1
#define _RAMP_SPECULAR_SAMPLE_UVY_TEX 1
#define _RECEIVE_URP_SHADOW 1
#define _ALPHATEST_ON 1
#define _NILOTOON_DITHER_FADEOUT 1
#define _FACE_SHADOW_GRADIENTMAP 1
#define _FACE_3D_RIMLIGHT_AND_SHADOW 1
#define _KAJIYAKAY_SPECULAR 1
#define _KAJIYAKAY_SPECULAR_TEX_TINT 1

// URP global define
#define _WRITE_RENDERING_LAYERS 1
#define _LIGHT_COOKIES 1
#define _DBUFFER 1
#define USE_FORWARD_PLUS 1
#define _LIGHT_LAYERS 1

// NiloToon global define
#define _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE 1
#define _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2 1
#define _NILOTOON_RECEIVE_URP_SHADOWMAPPING 1

// global define that is not useful due to -> return;
//#define _NILOTOON_DEBUG_SHADING 1
//#define _NILOTOON_FORCE_MINIMUM_SHADER 1

// pass define
#define NiloToonForwardLitPass 1
#define NiloToonSelfOutlinePass 1

#endif
//--------------------------------------------------------
