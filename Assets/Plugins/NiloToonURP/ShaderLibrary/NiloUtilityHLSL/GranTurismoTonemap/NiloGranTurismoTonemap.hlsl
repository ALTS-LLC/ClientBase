// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

// GT Tonemap
// https://www.desmos.com/calculator/gslcdxvipg

// Unity shader implementation of Gran Turismo Tonemapping. (by yaoling1997)
// https://github.com/yaoling1997/GT-ToneMapping
static const float e = 2.71828;

float W_f(float x,float e0,float e1) {
    if (x <= e0)
        return 0;
    if (x >= e1)
        return 1;
    float a = (x - e0) / (e1 - e0);
    return a * a*(3 - 2 * a);
}
float H_f(float x, float e0, float e1) {
    if (x <= e0)
        return 0;
    if (x >= e1)
        return 1;
    return (x - e0) / (e1 - e0);
}

float GranTurismoTonemapper(float x) {
    float P = 1;
    float a = 1;
    float m = 0.22;
    float l = 0.4;
    float c = 1.33;
    float b = 0;
    float l0 = (P - m)*l / a;
    float L0 = m - m / a;
    float L1 = m + (1 - m) / a;
    float L_x = m + a * (x - m);
    float T_x = m * pow(abs(x) / m, c) + b;
    float S0 = m + l0;
    float S1 = m + a * l0;
    float C2 = a * P / (P - S1);
    float S_x = P - (P - S1)*pow(e,-(C2*(x-S0)/P));
    float w0_x = 1 - W_f(x, 0, m);
    float w2_x = H_f(x, m + l0, m + l0); // this line is wrong?
    float w1_x = 1 - w0_x - w2_x;
    float f_x = T_x * w0_x + L_x * w1_x + S_x * w2_x;
    return f_x;
}
        
half3 GranTurismoTonemap(half3 input)
{
    float r = GranTurismoTonemapper(input.r);
    float g = GranTurismoTonemapper(input.g);
    float b = GranTurismoTonemapper(input.b);

    return half3(r,g,b);
}
