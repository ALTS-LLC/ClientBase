// copy of URP10.5.0's UberPost.shader
// all change will have [NiloToon] tags
Shader "Hidden/Universal Render Pipeline/NiloToonUberPost"
{
    HLSLINCLUDE
        //#pragma exclude_renderers gles // [NiloToon removed]
        #pragma multi_compile_local_fragment _ _DISTORTION
        //#pragma multi_compile_local_fragment _ _CHROMATIC_ABERRATION // [NiloToon removed]
        #pragma multi_compile_local_fragment _ _BLOOM_LQ _BLOOM_HQ _BLOOM_LQ_DIRT _BLOOM_HQ_DIRT
        #pragma multi_compile_local_fragment _ _HDR_GRADING _TONEMAP_ACES _TONEMAP_NEUTRAL _TONEMAP_GRANTURISMO _TONEMAP_ACES_CUSTOM _TONEMAP_KHRONOS_PBR_NEUTRAL _TONEMAP_NILO_NPR_NEUTRAL_V1 _TONEMAP_NILO_HYBIRD_ACES// [NiloToon added: _TONEMAP_GRANTURISMO, _TONEMAP_ACES_CUSTOM, _TONEMAP_KHRONOS_PBR_NEUTRAL, _TONEMAP_NILO_NPR_NEUTRAL_V1, _TONEMAP_NILO_HYBIRD_ACES]
        //#pragma multi_compile_local_fragment _ _FILM_GRAIN // [NiloToon removed]
        //#pragma multi_compile_local_fragment _ _DITHERING // [NiloToon removed]
        #pragma multi_compile_local_fragment _ _LINEAR_TO_SRGB_CONVERSION
        #pragma multi_compile _ _USE_DRAW_PROCEDURAL

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        // [NiloToon added]
        //==========================================================
        #include "../../../ShaderLibrary/NiloUtilityHLSL/GranTurismoTonemap/NiloGranTurismoTonemap.hlsl"
        #include "../../../ShaderLibrary/NiloUtilityHLSL/NiloCustomACES.hlsl"
        #include "../../../ShaderLibrary/NiloUtilityHLSL/KhronosPBRNeutralTonemapper/NiloKhronosPBRNeutralTonemap.hlsl"
        
#if UNITY_VERSION < 202220
        #define _BlitTexture _SourceTex
        #define _BlitTexture_TexelSize _SourceTex_TexelSize
        TEXTURE2D_X(_SourceTex);
#endif

        #define NILOTOON_TONEMAPPING _TONEMAP_ACES || _TONEMAP_NEUTRAL || _TONEMAP_GRANTURISMO || _TONEMAP_ACES_CUSTOM || _TONEMAP_KHRONOS_PBR_NEUTRAL || _TONEMAP_NILO_NPR_NEUTRAL_V1 || _TONEMAP_NILO_HYBIRD_ACES
        //==========================================================
        
        // Hardcoded dependencies to reduce the number of variants
        #if _BLOOM_LQ || _BLOOM_HQ || _BLOOM_LQ_DIRT || _BLOOM_HQ_DIRT
            #define BLOOM
            #if _BLOOM_LQ_DIRT || _BLOOM_HQ_DIRT
                #define BLOOM_DIRT
            #endif
        #endif

        TEXTURE2D_X(_Bloom_Texture);
        TEXTURE2D(_LensDirt_Texture);
        TEXTURE2D(_Grain_Texture);
        TEXTURE2D(_InternalLut);
        TEXTURE2D(_UserLut);
        TEXTURE2D(_BlueNoise_Texture);

        float4 _Lut_Params;
        float4 _UserLut_Params;
        float4 _Bloom_Params;
        float _Bloom_RGBM;
        float4 _LensDirt_Params;
        float _LensDirt_Intensity;
        float4 _Distortion_Params1;
        float4 _Distortion_Params2;
        float _Chroma_Params;
        half4 _Vignette_Params1;
        float4 _Vignette_Params2;
        float2 _Grain_Params;
        float4 _Grain_TilingParams;
        float4 _Bloom_Texture_TexelSize;
        float4 _Dithering_Params;

        // [NiloToon added]
        //==========================================================
        TEXTURE2D_X(_NiloToonPrepassBufferTex);

        half _NiloToonBloomCharacterAreaIntensity;
        half _NiloToonTonemappingCharacterAreaRemove;
        half _NiloToonTonemappingCharacterAreaPreTonemapBrightnessMul;
        half _NiloToonTonemappingNonCharacterAreaRemove;
        half _NiloToonTonemappingNonCharacterAreaPreTonemapBrightnessMul;

        half _NiloToon_CustomACESParamA;
        half _NiloToon_CustomACESParamB;
        half _NiloToon_CustomACESParamC;
        half _NiloToon_CustomACESParamD;
        half _NiloToon_CustomACESParamE;
        //==========================================================

        #define DistCenter              _Distortion_Params1.xy
        #define DistAxis                _Distortion_Params1.zw
        #define DistTheta               _Distortion_Params2.x
        #define DistSigma               _Distortion_Params2.y
        #define DistScale               _Distortion_Params2.z
        #define DistIntensity           _Distortion_Params2.w

        #define ChromaAmount            _Chroma_Params.x

        #define BloomIntensity          _Bloom_Params.x
        #define BloomTint               _Bloom_Params.yzw
        #define BloomRGBM               _Bloom_RGBM.x
        #define LensDirtScale           _LensDirt_Params.xy
        #define LensDirtOffset          _LensDirt_Params.zw
        #define LensDirtIntensity       _LensDirt_Intensity.x

        #define VignetteColor           _Vignette_Params1.xyz
        #define VignetteCenter          _Vignette_Params2.xy
        #define VignetteIntensity       _Vignette_Params2.z
        #define VignetteSmoothness      _Vignette_Params2.w
        #define VignetteRoundness       _Vignette_Params1.w

        #define LutParams               _Lut_Params.xyz
        #define PostExposure            _Lut_Params.w
        #define UserLutParams           _UserLut_Params.xyz
        #define UserLutContribution     _UserLut_Params.w

        #define GrainIntensity          _Grain_Params.x
        #define GrainResponse           _Grain_Params.y
        #define GrainScale              _Grain_TilingParams.xy
        #define GrainOffset             _Grain_TilingParams.zw

        #define DitheringScale          _Dithering_Params.xy
        #define DitheringOffset         _Dithering_Params.zw

        float2 DistortUV(float2 uv)
        {
            // Note: this variant should never be set with XR
            #if _DISTORTION
            {
                uv = (uv - 0.5) * DistScale + 0.5;
                float2 ruv = DistAxis * (uv - 0.5 - DistCenter);
                float ru = length(float2(ruv));

                UNITY_BRANCH
                if (DistIntensity > 0.0)
                {
                    float wu = ru * DistTheta;
                    ru = tan(wu) * (rcp(ru * DistSigma));
                    uv = uv + ruv * (ru - 1.0);
                }
                else
                {
                    ru = rcp(ru) * DistTheta * atan(ru * DistSigma);
                    uv = uv + ruv * (ru - 1.0);
                }
            }
            #endif

            return uv;
        }

        // [NiloToon added]
        //==========================================================
        half3 NiloApplyTonemap(half3 input, half characterArea)
        {
        #if _TONEMAP_ACES
            float3 aces = unity_to_ACES(input);
            input = AcesTonemap(aces);
        #elif _TONEMAP_NEUTRAL
            input = NeutralTonemap(input);
        #elif _TONEMAP_GRANTURISMO
            input = GranTurismoTonemap(input);
        #elif _TONEMAP_ACES_CUSTOM
            input = AcesCustomTonemap(input, _NiloToon_CustomACESParamA, _NiloToon_CustomACESParamB, _NiloToon_CustomACESParamC, _NiloToon_CustomACESParamD, _NiloToon_CustomACESParamE);
        #elif _TONEMAP_KHRONOS_PBR_NEUTRAL
            input = KhronosPBRNeutralToneMapping(input);
        #elif _TONEMAP_NILO_NPR_NEUTRAL_V1
            input = NiloNPRNeutralToneMappingV1(input);
        #elif _TONEMAP_NILO_HYBIRD_ACES
            float3 aces = unity_to_ACES(input);
            float3 acesColor = AcesTonemap(aces);
            
            float3 charColor = NiloHybirdTonemap(input, acesColor);
            
            input = lerp(acesColor,charColor,characterArea);
        #endif

            return (input);
        }
        //==========================================================

        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            // SHADER_LIBRARY_VERSION_MAJOR is deprecated for Unity2022.2 or later, so we will use UNITY_VERSION instead
            // https://github.com/Cyanilux/URP_ShaderCodeTemplates/blob/main/URP_SimpleLitTemplate.shader#L145
        #if UNITY_VERSION >= 202220 // (for URP 14 or above)
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord); // URP14 changed the naming from uv to texcoord, see URP14's Runtime\Utilities\Blit.hlsl
        #else // (for below URP 14)
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
        #endif 

            float2 uvDistorted = DistortUV(uv);

            half4 color = 0.0;

            // [NiloToon delete]
            //==================================
            //... (removed all unrelated logic)
            //==================================

            color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvDistorted);

            float characterArea = SAMPLE_TEXTURE2D_X(_NiloToonPrepassBufferTex, sampler_LinearClamp, uv).g;
            
            #if defined(BLOOM)
            {
                #if _BLOOM_HQ && !defined(SHADER_API_GLES)
                half4 bloom = SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_Bloom_Texture, sampler_LinearClamp), uvDistorted, _Bloom_Texture_TexelSize.zwxy, (1.0).xx, unity_StereoEyeIndex);
                #else
                half4 bloom = SAMPLE_TEXTURE2D_X(_Bloom_Texture, sampler_LinearClamp, uvDistorted);
                #endif

                #if UNITY_COLORSPACE_GAMMA
                bloom.xyz *= bloom.xyz; // γ to linear
                #endif

                UNITY_BRANCH
                if (BloomRGBM > 0)
                {
                    bloom.xyz = DecodeRGBM(bloom);
                }

                //[NiloToon edited]
                //==========================================================
                // (original is just -> bloom.xyz *= BloomIntensity;)
                bloom.xyz *= lerp(BloomIntensity,_NiloToonBloomCharacterAreaIntensity,characterArea);
                //==========================================================

                color.rgb += bloom.xyz * BloomTint;

                #if defined(BLOOM_DIRT)
                {
                    // UVs for the dirt texture should be DistortUV(uv * DirtScale + DirtOffset) but
                    // considering we use a cover-style scale on the dirt texture the difference
                    // isn't massive so we chose to save a few ALUs here instead in case lens
                    // distortion is active.
                    half3 dirt = SAMPLE_TEXTURE2D(_LensDirt_Texture, sampler_LinearClamp, uvDistorted * LensDirtScale + LensDirtOffset).xyz;
                    dirt *= LensDirtIntensity;
                    color.rgb += dirt * bloom.xyz;
                }
                #endif
            }
            #endif
            
            //[NiloToon added]
            //==========================================================
            // pre-tonemap brightness mul
            color.rgb *= lerp(_NiloToonTonemappingNonCharacterAreaPreTonemapBrightnessMul, _NiloToonTonemappingCharacterAreaPreTonemapBrightnessMul, characterArea);

            // tonemap
            #if NILOTOON_TONEMAPPING
            {
                // LDR only grading, but removed character area tonemapping(0~100%)
                // (HDR grading of URP is not supported)
                half3 tonemappedColor = NiloApplyTonemap(color.rgb, characterArea);
                half3 characterAreaResultColor = lerp(tonemappedColor, color.rgb, _NiloToonTonemappingCharacterAreaRemove);
                half3 nonCharacterAreaResultColor = lerp(tonemappedColor, color.rgb, _NiloToonTonemappingNonCharacterAreaRemove);
                color.rgb = lerp(nonCharacterAreaResultColor, characterAreaResultColor, characterArea);
            }
            #endif
            //==========================================================
            
            return color; // return with alpha
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "UberPost"

            HLSLPROGRAM
                #pragma vertex Vert // FullscreenVert
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
