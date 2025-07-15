// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

Shader "Hidden/NiloToon/AnimePostProcess"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    sampler2D _NiloToonAverageShadowMapRT;

    float _TopLightRotationDegree;
    float _BottomDarkenRotationDegree;

    float _TopLightDrawAreaHeight;
    float _BottomDarkenDrawAreaHeight;
    half _TopLightIntensity;
    half _TopLightMultiplyLightColor;
    half _TopLightDesaturate;
    half3 _TopLightTintColor;
    half3 _TopLightSunTintColor;
    
    half _BottomDarkenIntensity;

    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 uv         : TEXCOORD0;
    };
    struct Varyings
    {
        float4 positionCS   : SV_POSITION;
        half3 color         : TEXCOORD0;
    };

    // https://docs.unity3d.com/Packages/com.unity.shadergraph@12.0/manual/Rotate-Node.html
    void Unity_Rotate_Degrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
    {
        Rotation = Rotation * (3.1415926f/180.0f);
        UV -= Center;
        float s = sin(Rotation);
        float c = cos(Rotation);
        float2x2 rMatrix = float2x2(c, -s, s, c);
        rMatrix *= 0.5;
        rMatrix += 0.5;
        rMatrix = rMatrix * 2 - 1;
        UV.xy = mul(UV.xy, rMatrix);
        UV += Center;
        Out = UV;
    }

    // input.positionOS is (-1,-1) to (1,1) rect full screen mesh
    Varyings ScreenVertFullscreenMesh(Attributes input)
    {
        // default (_TopLightDrawAreaHeight == 0.5) only render top half of the screen to save fill-rate
        input.positionOS.y -= 1; // after this line, input.positionOS.y's top = 0, bottom = -2
        input.positionOS.y *= _TopLightDrawAreaHeight;
        input.positionOS.y += 1;

        // extend x to make rotation possible
        input.positionOS.x *= 10;

        // extend y to make rotation possible
        if(input.uv.y > 0.5)
        {
            input.positionOS.y += 1;
            input.uv.y += 1;
        }
        
        // apply rotate (rotation center is (0,0))
        Unity_Rotate_Degrees_float(input.positionOS.xy, float2(0.0,0.0), _TopLightRotationDegree, input.positionOS.xy);

        Varyings output = (Varyings)0;
        output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

        // light color
        Light mainLight = GetMainLight();
        mainLight.color = lerp(1,mainLight.color, _TopLightMultiplyLightColor);
        half3 lightColor = mainLight.color * _TopLightSunTintColor; // tint orange to make it default looks better
        float lightLuminance = Luminance(mainLight.color);
        lightColor = lerp(lightColor, lightLuminance, _TopLightDesaturate); // we should lerp(mainLight.color,lightLuminance,_TopLightDesaturate), but since lots of user are using the wrong method to set _TopLightDesaturate already, we can't change it
        lightColor *= _TopLightTintColor;
        lightColor *= 1 / (1 + lightLuminance) * 3;

        ////////////////////////////////////
        //skylight screen
        ////////////////////////////////////
        float skyLightGradient = min(2,(max(0,input.uv.y * 2 - 1)));//linear gradient from top to middle, minmax(0,2) due to "extend y to make roation possible" section added

         // reserved the right most slot for camera (uv.x == 1)
        half averageShadowSampleValue = tex2Dlod(_NiloToonAverageShadowMapRT, float4(1,0,0,0)).r;

        float skyLightIntensity = 1.5 * _TopLightIntensity * averageShadowSampleValue; // for intensity, control this
        half3 col = skyLightGradient * lightColor * skyLightIntensity;

        output.color = col;
        return output;
    }
    half4 ScreenFrag(Varyings input) : SV_Target
    {
        return half4(input.color * input.color,1); // better fadeout curve than linear
    }

    Varyings MultiplyVertFullscreenMesh(Attributes input)
    {
        // default (_BottomDarkenDrawAreaHeight == 0.5) only render bottom half of the screen to save fill-rate
        input.positionOS.y += 1;
        input.positionOS.y *= _BottomDarkenDrawAreaHeight;
        input.positionOS.y -= 1;

        // extend x to make rotation possible
        input.positionOS.x *= 10;

        // extend y to make rotation possible
        if(input.uv.y < 0.5)
        {
            input.positionOS.y -= 1;
            input.uv.y -= 1;
        }
        
        // apply rotate
        Unity_Rotate_Degrees_float(input.positionOS.xy, float2(0.0,0.0), _BottomDarkenRotationDegree, input.positionOS.xy);

        Varyings output = (Varyings)0;
        output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

        Light mainLight = GetMainLight();
        float3 lightColor = mainLight.color;
        float lightLuminance = Luminance(mainLight.color);
        
        ////////////////////////////////////
        //bottom darken
        ////////////////////////////////////
        half bottomDarkenGradient = input.uv.y; //linear gradient from bottom to middle

        half darkenTo = 0.9 / (1.0 + lightLuminance * 0.25);
        half3 col = lerp(1 , bottomDarkenGradient, saturate(darkenTo * _BottomDarkenIntensity));

        output.color = col;
        return output;       
    }
    half4 MultiplyFrag(Varyings input) : SV_Target
    {
        return half4(input.color,1);
    }

    ENDHLSL

    SubShader
    {
        ZTest Off ZWrite Off Cull Off

        // Pass 0
        Pass
        {
            Name "RenderAnimePostProcessScreen"

            Blend OneMinusDstColor One // screen

            ColorMask RGB

            HLSLPROGRAM
                #pragma vertex ScreenVertFullscreenMesh
                #pragma fragment ScreenFrag
            ENDHLSL
        }

        
        // Pass 1
        Pass
        {
            Name "RenderAnimePostProcessMultiply"

            Blend DstColor Zero // multiply

            ColorMask RGB
            
            HLSLPROGRAM
                #pragma vertex MultiplyVertFullscreenMesh
                #pragma fragment MultiplyFrag
            ENDHLSL
        }

    }
}