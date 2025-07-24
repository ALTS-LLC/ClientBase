using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NiloToon.NiloToonURP
{
    // user should call this class's method if they are rendering planar reflection
    public static class NiloToonPlanarReflectionHelper
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // for non command buffer (e.g. user renders planar reflection in monobehaviour)
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // example code:
        // {
        //      NiloToonPlanarReflectionHelper.BeginPlanarReflectionCameraRender(); // add this line
        //      UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
        //      NiloToonPlanarReflectionHelper.EndPlanarReflectionCameraRender(); // add this line
        // }
        public static void BeginPlanarReflectionCameraRender()
        {
            NiloToonZOffsetGlobalOnOff.SetEnable(false);
            NiloToonPerspectiveRemovalGlobalOnOff.SetEnable(false);
            NiloToonDepthTexRimLightAndShadowGlobalOnOff.SetEnable(false);
        }
        public static void EndPlanarReflectionCameraRender()
        {
            NiloToonZOffsetGlobalOnOff.SetEnable(true);
            NiloToonPerspectiveRemovalGlobalOnOff.SetEnable(true);
            NiloToonDepthTexRimLightAndShadowGlobalOnOff.SetEnable(true);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // for command buffer (e.g. user renders planar reflection in renderer feature)
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // example code:
        // {
        //      NiloToonPlanarReflectionHelper.BeginPlanarReflectionCameraRender(ref cmd); // add this line
        //      context.ExecuteCommandBuffer(cmd); // remember to execute cmd first before calling DrawRenderers
        //      
        //      context.DrawRenderers(cullResults, ref drawSetting, ref filterSetting);
        //      
        //      cmd.clear();
        //      NiloToonPlanarReflectionHelper.EndPlanarReflectionCameraRender(ref cmd); // add this line
        //      context.ExecuteCommandBuffer(cmd);
        // }
        public static void BeginPlanarReflectionCameraRender(ref CommandBuffer cmd)
        {
            NiloToonZOffsetGlobalOnOff.SetEnable(false, ref cmd);
            NiloToonPerspectiveRemovalGlobalOnOff.SetEnable(false, ref cmd);
            NiloToonDepthTexRimLightAndShadowGlobalOnOff.SetEnable(false, ref cmd);
        }
        public static void EndPlanarReflectionCameraRender(ref CommandBuffer cmd)
        {
            NiloToonZOffsetGlobalOnOff.SetEnable(true, ref cmd);
            NiloToonPerspectiveRemovalGlobalOnOff.SetEnable(true, ref cmd);
            NiloToonDepthTexRimLightAndShadowGlobalOnOff.SetEnable(true, ref cmd);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Calling camera.gameobject.name will have GC alloc per frame.
        // All the added HashSet complexity here is for removing GC alloc per frame,
        // we decided to use 2 HashSet to cache Cameras reference so we don't need to call camera.gameobject.name per frame
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        // HashSet will provide O(1) access time,
        // these HashSet will never be released, they will stay forever until Unity or Application shut down
        // since the number of Camera should not be huge, there should be no memory usage problem even the HashSet added ~1000 Camera references
        static HashSet<Camera> allPlanarReflectionCamerasHashSet;
        static HashSet<Camera> allNonPlanarReflectionCamerasHashSet;
        const int HashSetMaxSize = 128; // this will ensure the HashSet wont increase it's size forever

        public static bool IsPlanarReflectionCamera(Camera camera)
        {
            if (camera == null)
            {
                Debug.LogError($"Calling method {nameof(IsPlanarReflectionCamera)} with a null camera as input param, the method will return false");
                return false;
            }

            // why not using (camera.cameraType == CameraType.Reflection)? 
            // CameraType.Reflection indicate a camera that is used for rendering reflection probes, it is not related to planar reflection, so don't use it for planar reflection camera decision.
            // https://docs.unity3d.com/ScriptReference/CameraType.Reflection.html

            // lazy init
            if (allPlanarReflectionCamerasHashSet == null) allPlanarReflectionCamerasHashSet = new HashSet<Camera>();
            if (allNonPlanarReflectionCamerasHashSet == null) allNonPlanarReflectionCamerasHashSet = new HashSet<Camera>();

            bool isCameraInPlanarReflectionCamerasHashSet = allPlanarReflectionCamerasHashSet.Contains(camera);
            bool isCameraInNonPlanarReflectionCamerasHashSet = allNonPlanarReflectionCamerasHashSet.Contains(camera);

            // confirm the camera is a non planar reflection camera, early exit as "false"
            if(isCameraInNonPlanarReflectionCamerasHashSet && !isCameraInPlanarReflectionCamerasHashSet)
            {
                return false;
            }
            // confirm the camera is a planar reflection camera, early exit as "true"
            if (!isCameraInNonPlanarReflectionCamerasHashSet && isCameraInPlanarReflectionCamerasHashSet)
            {
                return true;
            }
            // if a camera is in both hash, something is wrong, reset everything
            if (isCameraInPlanarReflectionCamerasHashSet && isCameraInNonPlanarReflectionCamerasHashSet)
            {
                allPlanarReflectionCamerasHashSet.Clear();
                allNonPlanarReflectionCamerasHashSet.Clear();
            }

            //---------------------------------------------------------------------------------------------
            // when we enter the section below, it means the current camera is not in any HashSet,
            // so we need to decide if the camera is a planar reflection camera or not
            //---------------------------------------------------------------------------------------------

            bool isPlanarReflectionCamera = false;
            
            bool ContainsStringIgnoreCase(string source, string value) => source.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;

            // for example, Asset {PIDI Planar Reflection 5}'s spawned reflection camera will always have a name of "_URPSceneReflectionCamera"
            // for example, Asset {Mirrors and reflections for VR}'s spawned reflection camera will always have a name of "Reflection Camera for " + renderCamera.name + " (autoGen, not saved in scene)"
            string cameraName = camera.gameObject.name; // gameobject.name will have GC alloc
            isPlanarReflectionCamera |= ContainsStringIgnoreCase(cameraName, "Planar");
            isPlanarReflectionCamera |= ContainsStringIgnoreCase(cameraName, "Reflect");
            isPlanarReflectionCamera |= ContainsStringIgnoreCase(cameraName, "Mirror");
            
            // planar reflection camera always render to a custom RenderTexture
            isPlanarReflectionCamera &= camera.targetTexture != null;
            
            // planar reflection camera is never the main camera
            isPlanarReflectionCamera &= !camera.CompareTag("MainCamera");

            if(isPlanarReflectionCamera)
            {
                if(allPlanarReflectionCamerasHashSet.Count > HashSetMaxSize)
                {
                    allPlanarReflectionCamerasHashSet.Clear();
                }
                allPlanarReflectionCamerasHashSet.Add(camera);
                return true;
            }
            else
            {
                if (allNonPlanarReflectionCamerasHashSet.Count > HashSetMaxSize)
                {
                    allNonPlanarReflectionCamerasHashSet.Clear();
                }

                allNonPlanarReflectionCamerasHashSet.Add(camera);
                return false;
            }
        }
    }
}

