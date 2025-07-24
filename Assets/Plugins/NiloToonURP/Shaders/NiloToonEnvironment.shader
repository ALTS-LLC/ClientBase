// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// This shader is a direct copy of URP16.0.3's ComplexLit.shader, but with some edit.
// If you want to see what is the difference, all edited lines will have a [NiloToon] tag, you can search [NiloToon] in this file,
// or compare URP16.0.3's ComplexLit.shader with this file using tools like SourceGear DiffMerge.

// Complex Lit is superset of Lit, but provides
// advanced material properties and is always forward rendered.
// It also has higher hardware and shader model requirements.
Shader "Universal Render Pipeline/NiloToon/NiloToon_Environment"
{
    Properties
    {
        // Specular vs Metallic workflow
        _WorkflowMode("WorkflowMode", Float) = 1.0

        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [ToggleUI] _ClearCoat("Clear Coat", Float) = 0.0
        _ClearCoatMap("Clear Coat Map", 2D) = "white" {}
        _ClearCoatMask("Clear Coat Mask", Range(0.0, 1.0)) = 0.0
        _ClearCoatSmoothness("Clear Coat Smoothness", Range(0.0, 1.0)) = 1.0

        // NiloToon added:
        //==========================================================================

        ////////////////////////////////////////////////////////////////////
        // Splat map
        ////////////////////////////////////////////////////////////////////
        _SplatMapFeatureOnOff("_SplatMapFeatureOnOff", Range(0,1)) = 0
        
        _SplatMaskMap("_SplatMaskMap", 2D) = "white" {}
        _SplatMaskBlendingSoftness("_SplatMaskBlendingSoftness", Range(0,1)) = 0.5
        
        // R
        _SplatAlbedoMapRTintColor("_SplatAlbedoMapRTintColor", Color) = (1,1,1,1)
        _SplatAlbedoMapR("_SplatAlbedoMapR", 2D) = "white" {}
        _SplatNormalMapR("_SplatNormalMapR", 2D) = "bump" {}
        _SplatPackedDataMapR("_SplatPackedDataMapR", 2D) = "white" {}
        _SplatAlbedoMapRTiling("_SplatAlbedoMapRTiling", Float) = 1
        _SplatNormalMapIntensityR("_SplatNormalMapIntensityR", Float) = 1
        _SplatSmoothnessMultiplierR("_SplatSmoothnessMultiplierR", Float) = 1
        _SplatHeightMultiplierR("_SplatHeightMultiplierR", Float) = 1

        // G
        _SplatAlbedoMapGTintColor("_SplatAlbedoMapGTintColor", Color) = (1,1,1,1)
        _SplatAlbedoMapG("_SplatAlbedoMapG", 2D) = "white" {}
        _SplatNormalMapG("_SplatNormalMapG", 2D) = "bump" {}
        _SplatPackedDataMapG("_SplatPackedDataMapG", 2D) = "white" {}
        _SplatAlbedoMapGTiling("_SplatAlbedoMapGTiling", Float) = 1
        _SplatNormalMapIntensityG("_SplatNormalMapIntensityG", Float) = 1
        _SplatSmoothnessMultiplierG("_SplatSmoothnessMultiplierG", Float) = 1
        _SplatHeightMultiplierG("_SplatHeightMultiplierG", Float) = 1

        // B
        _SplatAlbedoMapBTintColor("_SplatAlbedoMapBTintColor", Color) = (1,1,1,1)
        _SplatAlbedoMapB("_SplatAlbedoMapB", 2D) = "white" {}
        _SplatNormalMapB("_SplatNormalMapB", 2D) = "bump" {}
        _SplatPackedDataMapB("_SplatPackedDataMapB", 2D) = "white" {}
        _SplatAlbedoMapBTiling("_SplatAlbedoMapBTiling", Float) = 1
        _SplatNormalMapIntensityB("_SplatNormalMapIntensityB", Float) = 1
        _SplatSmoothnessMultiplierB("_SplatSmoothnessMultiplierB", Float) = 1
        _SplatHeightMultiplierB("_SplatHeightMultiplierB", Float) = 1

        // A
        _SplatAlbedoMapATintColor("_SplatAlbedoMapATintColor", Color) = (1,1,1,1)
        _SplatAlbedoMapA("_SplatAlbedoMapA", 2D) = "white" {}
        _SplatNormalMapA("_SplatNormalMapA", 2D) = "bump" {}
        _SplatPackedDataMapA("_SplatPackedDataMapA", 2D) = "white" {}
        _SplatAlbedoMapATiling("_SplatAlbedoMapATiling", Float) = 1
        _SplatNormalMapIntensityA("_SplatNormalMapIntensityA", Float) = 1
        _SplatSmoothnessMultiplierA("_SplatSmoothnessMultiplierA", Float) = 1
        _SplatHeightMultiplierA("_SplatHeightMultiplierA", Float) = 1

        ////////////////////////////////////////////////////////////////////
        // Screen space outline
        ////////////////////////////////////////////////////////////////////
        _ScreenSpaceOutlineIntensity("_ScreenSpaceOutlineIntensity", Range(0,1)) = 1
        [HDR]_ScreenSpaceOutlineColor("_ScreenSpaceOutlineColor", Color) = (1,1,1,1)
        _ScreenSpaceOutlineWidth("_ScreenSpaceOutlineWidth", Float) = 1
        //==========================================================================

        // Blending state
        _Surface("__surface", Float) = 0.0
        _Blend("__mode", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _BlendOp("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
        [HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        // Editmode props
        _QueueOffset("Queue offset", Float) = 0.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "ComplexLit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        // ------------------------------------------------------------------
        // Forward only pass.
        // Acts also as an opaque forward fallback for deferred rendering.
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }

            // -------------------------------------
            // Render State Commands
            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _ _CLEARCOAT _CLEARCOATMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#if UNITY_VERSION >= 202220
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
#endif
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
#if UNITY_VERSION >= 202220
            #pragma multi_compile _ _FORWARD_PLUS
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#else
            #pragma multi_compile _ _CLUSTERED_RENDERING
#endif
            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
#if UNITY_VERSION >= 202220
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#else
            #pragma multi_compile _ DOTS_INSTANCING_ON
#endif

            //[NiloToon] add:
            //==========================================================================================================
            #pragma shader_feature_local _SPLATMAP
            #pragma multi_compile_fragment _ _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE
            //#pragma multi_compile_fragment _ _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2 //WIP, temp disabled
            //==========================================================================================================

            //[NiloToon] remove:
            //==========================================================================================================
            // -------------------------------------
            // Includes
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            //==========================================================================================================

            //[NiloToon] add:
            //==========================================================================================================
            #include "NiloToonEnvironment_HLSL/NiloToonEnvironment_LitInput.hlsl"
            #include "NiloToonEnvironment_HLSL/NiloToonEnvironment_LitForwardPass.hlsl"
            //==========================================================================================================

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
#if UNITY_VERSION >= 202220
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#else
            #pragma multi_compile _ DOTS_INSTANCING_ON
#endif
            // -------------------------------------
            // Unity defined keywords
#if UNITY_VERSION >= 202220
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
#endif

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            //[NiloToon] remove:
            //==========================================================================================================
            // -------------------------------------
            // Includes
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            //==========================================================================================================

            //[NiloToon] add:
            //==========================================================================================================
            #include "NiloToonEnvironment_HLSL/NiloToonEnvironment_LitInput.hlsl"
            //==========================================================================================================

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
#if UNITY_VERSION >= 202220
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
#endif

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
#if UNITY_VERSION >= 202220
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#else
            #pragma multi_compile _ DOTS_INSTANCING_ON
#endif

            //[NiloToon] remove:
            //==========================================================================================================
            // -------------------------------------
            // Includes
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            //==========================================================================================================

            //[NiloToon] add:
            //==========================================================================================================
            #include "NiloToonEnvironment_HLSL/NiloToonEnvironment_LitInput.hlsl"
            //==========================================================================================================

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // This pass is used when drawing to a _CameraNormalsTexture texture with the forward renderer or the depthNormal prepass with the deferred renderer.
        Pass
        {
            Name "DepthNormalsOnly"
            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT // forward-only variant
#if UNITY_VERSION >= 202220
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#endif

            // -------------------------------------
            // Unity defined keywords
#if UNITY_VERSION >= 202220
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
#endif

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
#if UNITY_VERSION >= 202220
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#else
            #pragma multi_compile _ DOTS_INSTANCING_ON
#endif

            //[NiloToon] remove:
            //==========================================================================================================
            // -------------------------------------
            // Includes
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            //==========================================================================================================
            
            //[NiloToon] add:
            //==========================================================================================================
            #include "NiloToonEnvironment_HLSL/NiloToonEnvironment_LitInput.hlsl"

            // - when using LitDepthNormalsPass, the normal buffer RT will include normalmap's information
            // - when using DepthNormalsPass, the normal buffer RT will only include polygon normal's information
            // we changed from LitDepthNormalsPass to DepthNormalsPass, 
            // because we want the normal buffer RT for NiloToon's screen space outline not including normalmap's information
            // else the screen space outline(using normal) is too complex and dirty
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl" 
            //==========================================================================================================

            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }

            // -------------------------------------
            // Render State Commands
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECGLOSSMAP
            #pragma shader_feature EDITOR_VISUALIZATION


            //[NiloToon] remove:
            //==========================================================================================================
            // -------------------------------------
            // Includes
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            //==========================================================================================================

            //[NiloToon] add:
            //==========================================================================================================
            #include "NiloToonEnvironment_HLSL/NiloToonEnvironment_LitInput.hlsl"
            //==========================================================================================================

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Universal2D"
            Tags
            {
                "LightMode" = "Universal2D"
            }

            // -------------------------------------
            // Render State Commands
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON

            //[NiloToon] remove:
            //==========================================================================================================
            // -------------------------------------
            // Includes
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            //==========================================================================================================

            //[NiloToon] add:
            //==========================================================================================================
            #include "NiloToonEnvironment_HLSL/NiloToonEnvironment_LitInput.hlsl"
            //==========================================================================================================
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Universal2D.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "MotionVectors"
            Tags { "LightMode" = "MotionVectors" }
            ColorMask RG

            HLSLPROGRAM
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma shader_feature_local_vertex _ADD_PRECOMPUTED_VELOCITY

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl"
            ENDHLSL
        }
    }
    
    //////////////////////////////////////////////////////

    FallBack "Hidden/Universal Render Pipeline/Lit"
    FallBack "Hidden/Universal Render Pipeline/FallbackError"

    //[NiloToon] remove:
    //==========================================================================================================
    //CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
    //==========================================================================================================

    //[NiloToon] added:
    //==========================================================================================================
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.NiloToonEnvironmentShaderGUI"
    //==========================================================================================================
}