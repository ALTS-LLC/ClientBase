// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

/// <summary>
/// Applies changes to an original color in the RGB color space, based on provided HSV modifications.
/// </summary>
/// <param name="originalColor">The original color in RGB space.</param>
/// <param name="hueOffset">The value to offset the hue. This value is added to the original hue.</param>
/// <param name="saturationBoost">The factor to boost the saturation. [0,HALF_MAX]</param>
/// <param name="valueMul">The factor to multiply the value (brightness). This value is multiplied with the original value.</param>
/// <param name="originalColorHSV">Output parameter that gets assigned the original color converted to HSV before applying changes.</param>
/// <returns>The color in RGB space after applying HSV changes.</returns>
half3 ApplyHSVChange(const half3 originalColor, const half hueOffset, const half saturationBoost, const half valueMul, out half3 originalColorHSV)
{
    half3 HSV = RgbToHsv(originalColor);
    originalColorHSV = HSV;

    // Hue(H)
    HSV.x += hueOffset;
 	
    // Saturation(S)
    // [old method]
    //   it has a design failure that it will produce random hue pixels if texture saturation is low, 
    //   since textures are usually GPU compressed(ETC/ASTC....), hue is not always preserved correctly.
 	//   HSV.y = lerp(HSV.y, 1, saturationBoost); 
 	// [new method]
 	//   tried to solve the design failure that the old method has, by preventing saturation boost if original saturation is low
	HSV.y = lerp(HSV.y,pow(abs(HSV.y),1/(1+saturationBoost * 3)), smoothstep(0,0.25,originalColorHSV.y)); 
    
	// Value(V)
    HSV.z *= valueMul;
	
	return  HsvToRgb(HSV);
}

half3 ApplyHSVChange(half3 originalColor, half hueOffset, half saturationBoost, half valueMul)
{
	// ReSharper disable once CppEntityAssignedButNoRead
	half3 dummy;
	// ReSharper disable once CppAssignedValueIsNeverUsed
	return ApplyHSVChange(originalColor,hueOffset,saturationBoost,valueMul,dummy);
}
