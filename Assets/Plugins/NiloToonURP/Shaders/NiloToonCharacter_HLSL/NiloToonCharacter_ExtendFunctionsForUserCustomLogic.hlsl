// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// This file is intended for you to extend NiloToon Character shader with your own custom logic.
// Add whatever code you want in this file, there are some empty functions below for you to override by your own method
// *Recommend using Rider as shader IDE to get auto complete from function's input struct

// You can extend NiloToon Character shader by writing additional code here without worrying about merge conflict in future updates, 
// because this .hlsl is just an almost empty .hlsl file with empty functions for you to fill in extra code (NiloToon's developer wont make change to this file often).
// You can use empty functions below to apply your global effect, similar to character-only postprocess (e.g. add fog of war/dithered transparency/scan line/...).
// If you want us to expose more empty functions at another shading timing, feel free contact nilotoon@gmail.com

// #pragma once is a safeguard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

// [write your custom local/global uniforms, includes and texture/samplers here]

// #include "YourCustomLogic.hlsl"

// sampler2D _YourGlobalTexture;
// float _YourGlobalUniform;

// sampler2D _YourLocalTexture;
// float _YourLocalUniform; // will break SRP batching, because it is not inside CBUFFER_START(UnityPerMaterial)

void ApplyCustomUserLogicToVertexAttributeAtVertexShaderStart(inout Attributes attribute)
{
	// edit vertex Attributes by your custom logic here
	
	// similar to the vertex output node of URP's Lit ShaderGraph:
	// - (Object Space) Vertex 
	// - (Object Space) Normal
	// - (Object Space) Tangent

	//attribute.positionOS *= 0.5; // example code, make character mesh smaller
}

void ApplyCustomUserLogicToVertexShaderOutputAtVertexShaderEnd(inout Varyings output, Attributes attribute)
{
	// edit vertex to fragment Varying struct by your custom logic here
	
	//output.positionCS.xy += sin(_Time) * 0.25; // example code, make character vertex move in clip space
}

void ApplyCustomUserLogicToBaseColor(inout half4 baseColor, Varyings input, UVData uvData, float facing)
{
	// edit baseColor by your custom logic here

	//baseColor *= half4(1,0,0,1); // example code, tint character's baseColor with red color
}

void ApplyCustomUserLogicBeforeFog(inout half3 color, ToonSurfaceData surfaceData, ToonLightingData lightingData, Varyings input, UVData uvData)
{
    // edit color by your custom logic here

    //color = 1-color; // example code, invert character's color before applying fog
}

void ApplyCustomUserLogicAfterFog(inout half3 color, ToonSurfaceData surfaceData, ToonLightingData lightingData, Varyings input, UVData uvData)
{
    // edit color by your custom logic here

    //color *= half3(0,1,0); // example code, tint character's final display pixels with green color
}
