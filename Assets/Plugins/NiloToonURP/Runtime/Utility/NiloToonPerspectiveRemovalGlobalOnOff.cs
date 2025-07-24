using UnityEngine;
using UnityEngine.Rendering;

namespace NiloToon.NiloToonURP
{
    public static class NiloToonPerspectiveRemovalGlobalOnOff
    {
        public static readonly int _GlobalShouldDisableNiloToonPerspectiveRemoval = Shader.PropertyToID("_GlobalShouldDisableNiloToonPerspectiveRemoval");

        // originally created to solve planar reflection problems
        // uniform's value default is 0 (all bit 0 in GPU), so default is allow PerspectiveRemoval if no user call the following functions

        ////////////////////////////////////////////////////////
        // for non command buffer
        ////////////////////////////////////////////////////////
        public static void SetEnable(bool shouldEnable)
        {
            Shader.SetGlobalFloat(_GlobalShouldDisableNiloToonPerspectiveRemoval, shouldEnable ? 0 : 1);
        }

        ////////////////////////////////////////////////////////
        // for command buffer
        ////////////////////////////////////////////////////////
        public static void SetEnable(bool shouldEnable, ref CommandBuffer cmd)
        {
            cmd.SetGlobalFloat(_GlobalShouldDisableNiloToonPerspectiveRemoval, shouldEnable ? 0 : 1);
        }
    }
}


