// A copy and edited version of Unity6(Unity2023.3)'s motion vector pass hlsl

#ifndef NILOTOON_OBJECT_MOTION_VECTORS_INCLUDED
#define NILOTOON_OBJECT_MOTION_VECTORS_INCLUDED

#pragma target 3.5

#pragma vertex vert
#pragma fragment frag

//--------------------------------------
// GPU Instancing
//#pragma multi_compile_instancing
//#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

//-------------------------------------
// Other pragmas
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"

// -------------------------------------
// Includes
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"

// -------------------------------------
// Structs
struct MotionVectorAttributes
{
    float4 position             : POSITION;
#if _ALPHATEST_ON
    float2 uv                   : TEXCOORD0;
#endif
    float3 positionOld          : TEXCOORD4;
#if _ADD_PRECOMPUTED_VELOCITY
    float3 alembicMotionVector  : TEXCOORD5;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct MotionVectorVaryings
{
    float4 positionCS                 : SV_POSITION;
    float4 positionCSNoJitter         : POSITION_CS_NO_JITTER;
    float4 previousPositionCSNoJitter : PREV_POSITION_CS_NO_JITTER;
#if _ALPHATEST_ON
    float2 uv                         : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float4 TransformPositionCSByNiloFunctions(float4 inputPositionCS, float3 positionWS)
{  
    // if we should not render this material,
    // execute "disable rendering"
    if(ShouldDisableRendering())
    {
        // see [a trick to "delete" any vertex] below to understand what this line does
        inputPositionCS.w = 0;
        return inputPositionCS;
    }
    
    // zoffset
    inputPositionCS = NiloGetNewClipPosWithZOffsetVS(inputPositionCS, -(_ZOffset+_PerCharZOffset));

    // perspective removal
    inputPositionCS = NiloDoPerspectiveRemoval(inputPositionCS,positionWS,_NiloToonGlobalPerCharHeadBonePosWSArray[_CharacterID],_PerspectiveRemovalRadius,_PerspectiveRemovalAmount, _PerspectiveRemovalStartHeight, _PerspectiveRemovalEndHeight);

    return inputPositionCS;
}

// -------------------------------------
// Vertex
MotionVectorVaryings vert(MotionVectorAttributes input)
{
    MotionVectorVaryings output = (MotionVectorVaryings)0;
    
    // to support GPU instancing and Single Pass Stereo rendering(VR), add the following section
    //------------------------------------------------------------------------------------------------------------------------------
    UNITY_SETUP_INSTANCE_ID(input);                 // will turn into this in non OpenGL / non PSSL -> UnitySetupInstanceID(input.instanceID);
    UNITY_TRANSFER_INSTANCE_ID(input, output);      // will turn into this in non OpenGL / non PSSL -> output.instanceID = input.instanceID;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);  // will turn into this in non OpenGL / non PSSL -> output.stereoTargetEyeIndexAsRTArrayIdx = unity_StereoEyeIndex;
    //------------------------------------------------------------------------------------------------------------------------------

    const VertexPositionInputs vertexInput = GetVertexPositionInputs(input.position.xyz);

    #if defined(_ALPHATEST_ON)
        output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    #endif
    
#if defined(APLICATION_SPACE_WARP_MOTION)
    // We do not need jittered position in ASW
    output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.position));;
    output.positionCS = output.positionCSNoJitter;
#else
    // Jittered. Match the frame.
    output.positionCS = vertexInput.positionCS;
    output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.position));
#endif

    float4 prevPos = (unity_MotionVectorsParams.x == 1) ? float4(input.positionOld, 1) : input.position;

#if _ADD_PRECOMPUTED_VELOCITY
    prevPos = prevPos - float4(input.alembicMotionVector, 0);
#endif

    output.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, prevPos));

    // NiloToon added: edit all positionCS by NiloToon functions that affect motion vector pass
    //---------------------------------------------------------------------------------
    // enabled due to final quality is much more stable, this is required when user use dither/dissolve/enable rendering, else character area(without character rendering) will be distorted
    output.positionCS = TransformPositionCSByNiloFunctions(output.positionCS, vertexInput.positionWS);

    // disabled due to final quality actually reduced, maybe it is not a good idea to edit "no jitter positionCS"
    //output.positionCSNoJitter = TransformPositionCSByNiloFunctions(output.positionCSNoJitter, vertexInput.positionWS); 
    //output.previousPositionCSNoJitter = TransformPositionCSByNiloFunctions(output.previousPositionCSNoJitter, mul(UNITY_PREV_MATRIX_M, prevPos));
    //---------------------------------------------------------------------------------
    
    return output;
}

// -------------------------------------
// Fragment
float4 frag(MotionVectorVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(_ALPHATEST_ON)

    // URP original:
    //Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff); // TODO: use GetCombinedBaseColor()

    // Nilo implementation:
    Varyings varings = (Varyings)0;
    UVData uvData = (UVData)0;
    uvData.allUVs[0] = input.uv; // TODO: send all UVs
    half4 baseColor = GetFinalBaseColor(varings,uvData,1);
    DoClipTestToTargetAlphaValue(baseColor.a);
    #endif

    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
    #endif

    #if defined(APLICATION_SPACE_WARP_MOTION)
        return float4(CalcAswNdcMotionVectorFromCsPositions(input.positionCSNoJitter, input.previousPositionCSNoJitter), 1);
    #else
        return float4(CalcNdcMotionVectorFromCsPositions(input.positionCSNoJitter, input.previousPositionCSNoJitter), 0, 0);
    #endif
}


#endif // NILOTOON_OBJECT_MOTION_VECTORS_INCLUDED
