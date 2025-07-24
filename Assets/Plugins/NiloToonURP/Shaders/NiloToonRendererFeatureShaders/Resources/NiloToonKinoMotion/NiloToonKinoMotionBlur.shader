Shader "Hidden/NiloToon/NiloToonKinoMotionBlur"
{
    HLSLINCLUDE

        #pragma target 3.0

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // (0) Velocity texture setup
        Pass
        {
            HLSLPROGRAM

                #include "NiloToonKinoMotionBlur.hlsl"
                #pragma vertex VertDefault
                #pragma fragment FragVelocitySetup

            ENDHLSL
        }

        // (1) TileMax filter (2 pixel width with normalization)
        Pass
        {
            HLSLPROGRAM

                #include "NiloToonKinoMotionBlur.hlsl"
                #pragma vertex VertDefault
                #pragma fragment FragTileMax1

            ENDHLSL
        }

        //  (2) TileMax filter (2 pixel width)
        Pass
        {
            HLSLPROGRAM

                #include "NiloToonKinoMotionBlur.hlsl"
                #pragma vertex VertDefault
                #pragma fragment FragTileMax2

            ENDHLSL
        }

        // (3) TileMax filter (variable width)
        Pass
        {
            HLSLPROGRAM

                #include "NiloToonKinoMotionBlur.hlsl"
                #pragma vertex VertDefault
                #pragma fragment FragTileMaxV

            ENDHLSL
        }

        // (4) NeighborMax filter
        Pass
        {
            HLSLPROGRAM

                #include "NiloToonKinoMotionBlur.hlsl"
                #pragma vertex VertDefault
                #pragma fragment FragNeighborMax

            ENDHLSL
        }

        // (5) Reconstruction filter
        Pass
        {
            HLSLPROGRAM

                #include "NiloToonKinoMotionBlur.hlsl"
                #pragma vertex VertMultitex
                #pragma fragment FragReconstruction

            ENDHLSL
        }
    }
}
