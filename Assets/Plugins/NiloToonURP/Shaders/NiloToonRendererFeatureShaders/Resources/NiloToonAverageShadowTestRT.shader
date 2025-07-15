// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

Shader "Hidden/NiloToon/AverageShadowTestRT"
{
    HLSLINCLUDE

    // we need URP's shadow map related keywords
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    // use _SURFACE_TYPE_TRANSPARENT to force URP Shadow running the classic sample path in Shadows.hlsl's half MainLightRealtimeShadow(float4 shadowCoord){...}
    // since screen space shadow will not work for this shader
    #define _SURFACE_TYPE_TRANSPARENT
    #endif

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

    // for most game type, 128 is a big enough number, but still not affect performance 
    #define MAX_CHARACTER_COUNT 128
    #define MAX_DATA_ARRAY_SIZE 512 // = 128 characters * 4 data slot
    #define TEST_COUNT 5 // usually 5 is smooth enough, it means (5+1+5)^3 = total of 1331 shadow tests for 1 character per frame

    float _GlobalAverageShadowTestBoundingSphereDataArray[MAX_DATA_ARRAY_SIZE];
    float _GlobalAverageShadowStrength;

    half Frag(Varyings input) : SV_Target
    {
        // SHADER_LIBRARY_VERSION_MAJOR is deprecated for Unity2022.2 or later, so we will use UNITY_VERSION instead
        // https://github.com/Cyanilux/URP_ShaderCodeTemplates/blob/main/URP_SimpleLitTemplate.shader#L145
    #if UNITY_VERSION >= 202220 // (for URP 14 or above)
        float2 uv = input.texcoord; // URP14 changed the naming from uv to texcoord, see URP14's Runtime\Utilities\Blit.hlsl
    #else // (for below URP 14)
        float2 uv = input.uv;
    #endif

        int index = floor(uv.x * MAX_CHARACTER_COUNT);
        float3 center;
        center.x =      _GlobalAverageShadowTestBoundingSphereDataArray[index*4+0];
        center.y =      _GlobalAverageShadowTestBoundingSphereDataArray[index*4+1];
        center.z =      _GlobalAverageShadowTestBoundingSphereDataArray[index*4+2];
        float radius =  _GlobalAverageShadowTestBoundingSphereDataArray[index*4+3];

        // for any not in use slots, radius should be 0,
        // early exit to improve performance, since usually not much characters are enabled in scene
        if(radius == 0.0) return 1;

        Light mainLight = GetMainLight();

        // TODO: [bug] when camera is close to character, shadow will fadeout if shadow cascade is > 1

        // [Disabled. because enable this will make shadow fadeout too easily when camera is close to character]
        // added to prevent generating shadow due to near by objects
        //center += mainLight.direction; // hardcode 1m, not the best solution

        // [Disabled. because enable this will make character darken when camera is close to character]
        // added to prevent out of bound
        //radius = min(radius, distance(center,_WorldSpaceCameraPos)-_ProjectionParams.y);

        // this crazy forloop^3 looks extremely scary,
        // but it will run once per active character only, so it will not affect performance at all
        float shadowTestSum = 0;
        for(int x = -TEST_COUNT ; x < TEST_COUNT ; x++)
        for(int y = -TEST_COUNT ; y < TEST_COUNT ; y++)
        for(int z = -TEST_COUNT ; z < TEST_COUNT ; z++)
        {
            float3 shadowTestPosWS = center + float3(x,y,z) / TEST_COUNT / 2.0 * radius;

            // copy from LitForwardPass.hlsl -> void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData);
            float4 shadowCoord = TransformWorldToShadowCoord(shadowTestPosWS);

            // copy from Lighting.hlsl -> half4 UniversalFragmentPBR(InputData inputData, SurfaceData surfaceData);
            float shadowAttenuation = MainLightRealtimeShadow(shadowCoord);

            shadowTestSum += shadowAttenuation;
        }
        float count1D = TEST_COUNT * 2 + 1;
        shadowTestSum /= float(count1D*count1D*count1D) * 0.25; // 0.25 can be any number. the bigger the number, the more easier to be in shadow

        return lerp(1,saturate(shadowTestSum),_GlobalAverageShadowStrength);
    }

    ENDHLSL

    SubShader
    {
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "RenderAverageShadowTestRT"

            HLSLPROGRAM
                #pragma vertex Vert // FullscreenVert
                #pragma fragment Frag
            ENDHLSL
        }
    }
}