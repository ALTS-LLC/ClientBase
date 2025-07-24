using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace NiloToon.NiloToonURP
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class NiloToonRendererRedrawer : MonoBehaviour
    {
        [Serializable]
        public class DrawData
        {
            public bool enabled = true;
            
            /// <summary>
            /// If you want to edit the material in runtime(e.g., control tint color), consider calling
            /// - "ActualMaterialUsedForRendering"
            /// instead of this "sharedMaterial".
            /// </summary>
            [FormerlySerializedAs("material")] // DO NOT REMOVE THIS LINE! it will produce material reference lost if the prefab is very old which is still saved with 'material' instead of 'sharedMaterial'
            public Material sharedMaterial;
            
            public int subMeshIndex;

            Material _materialInstance;
            Material _materialInstanceSource;
            /// <summary>
            /// Call this will create a material instance, similar to calling Renderer.material
            /// </summary>
            public Material MaterialInstance
            {
                get
                {
                    if (!sharedMaterial)
                    {
                        Debug.LogError("because sharedMaterial is null, calling NiloToonRendererRedrawer.MaterialInstance will now return null");
                        return null;
                    }
                    
                    // a safe way to edit material in playmode,
                    // is to convert sharedMaterial to material instance,
                    // similar to calling Renderer.material
                    // *Don't create material instance in Edit mode.
                    if (!Application.isPlaying)
                    {
                        Debug.LogError("Tried to instantiating material due to calling NiloToonRendererRedrawer.MaterialInstance during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. *Now returning sharedMaterial instead of MaterialInstance to prevent material leak*.");
                        return sharedMaterial;
                    }
                    
                    // mimic the result of calling Renderer.material
                    if ((!_materialInstance && sharedMaterial) || // not yet clone material
                        (_materialInstanceSource != sharedMaterial)) // sharedMaterial changed to another material 
                    {
                        _materialInstance = Instantiate(sharedMaterial);
                        _materialInstanceSource = sharedMaterial;
                        _materialInstance.name = sharedMaterial.name + " (Instance)";
                        _materialInstance.hideFlags = HideFlags.DontSave;
                    }

                    return _materialInstance;
                }
            }

            public Material ActualMaterialUsedForRendering
            {
                get
                {
                    if (_materialInstance) return _materialInstance;

                    return sharedMaterial;
                }
            }
        }

        public List<DrawData> DrawList = new();
        
        [Header("Sync options")]
        [Tooltip("Should this script also stop rendering when the Renderer of this GameObject is disabled?")]
        public bool deactivateWithRenderer = true;

        [Header("Optimization")]
        [Tooltip("Should this script also stop rendering when the Renderer of this GameObject is not visible by any camera?\n" +
                 "*Please note that the editor scene window camera is considered also. To profile the game window CPU/GPU performance, you should hide the scene window.")]
        public bool deactivateWhenRendererIsNotVisible = true;

        private NiloToonPerCharacterRenderController _niloToonPerCharacterRenderController;
        public NiloToonPerCharacterRenderController NiloToonPerCharacterRenderController
        {
            get
            {
                if (!_niloToonPerCharacterRenderController)
                {
                    // search parent for the closest NiloToonPerCharacterRenderController
                    _niloToonPerCharacterRenderController = GetComponentInParent<NiloToonPerCharacterRenderController>();
                }

                return _niloToonPerCharacterRenderController;
            }
        }

        private Renderer _Renderer;
        public Renderer Renderer
        {
            get
            {
                if (!_Renderer)
                    _Renderer = GetComponent<Renderer>();

                return _Renderer;
            }
        }
        
        private MaterialPropertyBlock _MPB;
        public MaterialPropertyBlock MPB
        {
            get
            {
                if (_MPB == null)
                    _MPB = new MaterialPropertyBlock();

                return _MPB;
            }
        }
        
        private Mesh _bakedMeshHolder;

        public Mesh BakedMeshHolder
        {
            get
            {
                if (_bakedMeshHolder == null)
                    _bakedMeshHolder = new Mesh();

                return _bakedMeshHolder;
            }
        }

        public void GetMaterials(List<Material> outList)
        {
            outList.Clear();
            foreach (var drawData in DrawList)
            {
                if(drawData == null) continue;
                
                if (drawData.sharedMaterial)
                {
                    outList.Add(drawData.MaterialInstance);
                }
            }
        }

        Mesh GetMeshFromRenderer(Renderer renderer)
        {
            if (renderer is MeshRenderer meshRenderer)
            {
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (!meshFilter)
                {
                    Debug.LogError($"No MeshFilter found on GameObject {renderer.gameObject.name}.");
                    return null;
                }

                return meshFilter.sharedMesh;
            }
            
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                // TODO:
                // SkinnedMeshRenderer.BakeMesh() is extremely slow but works perfectly,
                // we should provide a new option to use an extra SkinnedMeshRenderer instead of SkinnedMeshRenderer.BakeMesh()+Graphics.RenderMesh()
                skinnedMeshRenderer.BakeMesh(BakedMeshHolder);
                return BakedMeshHolder;
            }

            Debug.LogError($"No Mesh or SkinnedMeshRenderer found on GameObject {renderer.gameObject.name}.");
            return null;
        }

        void FindParentNiloToonPerCharacterRenderController()
        {
            // add a link with that NiloToonPerCharacterRenderController
            if (NiloToonPerCharacterRenderController)
            {
                if (!NiloToonPerCharacterRenderController.nilotoonRendererRedrawerList.Contains(this))
                {
                    NiloToonPerCharacterRenderController.nilotoonRendererRedrawerList.Add(this);
                }
            }
        }
        
        Vector3 CalculateWorldScale(Transform current)
        {
            if (current.parent == null)
            {
                // This is a root object, return its scale directly
                return current.localScale;
            }
            else
            {
                // This is not a root object, calculate the total scale
                Vector3 parentScale = CalculateWorldScale(current.parent);
                return new Vector3(
                    current.localScale.x * parentScale.x,
                    current.localScale.y * parentScale.y,
                    current.localScale.z * parentScale.z);
            }
        }

        void SyncSkinnedMeshRenderer(SkinnedMeshRenderer from, SkinnedMeshRenderer to)
        {
            to.sharedMesh = from.sharedMesh;
            to.bones = from.bones;
            to.rootBone = from.rootBone;
            to.quality = from.quality;
            to.updateWhenOffscreen = from.updateWhenOffscreen;
            to.skinnedMotionVectors = from.skinnedMotionVectors;
            to.localBounds = from.localBounds;

            // Renderer properties
            to.enabled = from.enabled;
            to.shadowCastingMode = from.shadowCastingMode;
            to.receiveShadows = from.receiveShadows;
            to.motionVectorGenerationMode = from.motionVectorGenerationMode;
            to.lightProbeUsage = from.lightProbeUsage;
            to.reflectionProbeUsage = from.reflectionProbeUsage;
            to.probeAnchor = from.probeAnchor;
            to.lightProbeProxyVolumeOverride = from.lightProbeProxyVolumeOverride;
            to.rendererPriority = from.rendererPriority;
            to.sortingLayerID = from.sortingLayerID;
            to.sortingLayerName = from.sortingLayerName;
            to.sortingOrder = from.sortingOrder;
            to.allowOcclusionWhenDynamic = from.allowOcclusionWhenDynamic;
            to.sharedMaterials = from.sharedMaterials;
    
            // Copy blendShape weights
            for (int i = 0; i < from.sharedMesh.blendShapeCount; i++)
            {
                to.SetBlendShapeWeight(i, from.GetBlendShapeWeight(i));
            }
        }

        private void OnValidate()
        {
            foreach (var drawData in DrawList)
            {
                // prevent negative index in UI
                drawData.subMeshIndex = Mathf.Max(0, drawData.subMeshIndex);
            }

            LateUpdate();
        }

        private void OnEnable()
        {
            FindParentNiloToonPerCharacterRenderController();
        }
        
        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                // keep update to parent NiloToonPerCharacterRenderController in edit mode
                FindParentNiloToonPerCharacterRenderController();
            }

            // Cache Renderer
            Renderer renderer = Renderer;
            
            if (!renderer) return;
            if (deactivateWhenRendererIsNotVisible && !renderer.isVisible) return;
            if (deactivateWithRenderer && !renderer.enabled) return;

            Mesh meshToDraw = GetMeshFromRenderer(renderer);

            Matrix4x4 ObjToWorldMatrix = renderer.transform.localToWorldMatrix;

            if (renderer is SkinnedMeshRenderer)
            {
                // in case the SkinnedMeshRenderer transform for any parent transform is not using (1,1,1) scale, cancel out the scale for Graphics.RenderMesh()
                Vector3 worldScale = CalculateWorldScale(renderer.transform);
                worldScale.x = 1f / worldScale.x;
                worldScale.y = 1f / worldScale.y;
                worldScale.z = 1f / worldScale.z;
                ObjToWorldMatrix = ObjToWorldMatrix * Matrix4x4.Scale(worldScale);
            }
            
            renderer.GetPropertyBlock(MPB);
            
            foreach (var drawData in DrawList)
            {
                // safe check
                if (!drawData.enabled) continue;
                if (!drawData.ActualMaterialUsedForRendering) continue;

                // https://docs.unity3d.com/ScriptReference/RenderParams.html
                RenderParams renderParams = new RenderParams(drawData.ActualMaterialUsedForRendering)
                {
                    layer = renderer.gameObject.layer,
                    renderingLayerMask = renderer.renderingLayerMask,
                    rendererPriority = renderer.rendererPriority,
                    worldBounds = renderer.bounds,
                    camera = null,
                    motionVectorMode = renderer.motionVectorGenerationMode,
                    reflectionProbeUsage = renderer.reflectionProbeUsage,
                    material = drawData.ActualMaterialUsedForRendering,
                    matProps = MPB,
                    shadowCastingMode = renderer.shadowCastingMode,
                    receiveShadows = renderer.receiveShadows,
                    lightProbeUsage = renderer.lightProbeUsage,
                    //lightProbeProxyVolume = renderer.lightProbeProxyVolumeOverride.GetComponent<LightProbeProxyVolume>(), // TODO: not sure how to sync
                };
                
                int safeSubMeshIndex = drawData.subMeshIndex;
                if (drawData.subMeshIndex >= meshToDraw.subMeshCount)
                {
                    Debug.LogError($"SubMeshIndex out of bounds. Current index: {drawData.subMeshIndex}, Maximum index (subMeshCount - 1): {meshToDraw.subMeshCount - 1}. Skipping draw. Click to highlight the GameObject for fixing.", this);
                    continue;
                }

                if (drawData.subMeshIndex < 0)
                {
                    Debug.LogError($"SubMeshIndex out of bounds. Current index: {drawData.subMeshIndex}, Minimum index : {0}. Skipping draw. Click to highlight the GameObject for fixing.", this);
                    continue;
                }
                
                // use Graphics.RenderMesh() instead of RendererFeature's cmd.DrawRenderer(),
                // since Graphics.RenderMesh() can ensure lighting, shadow, multi shader passes, and RenderQueue are all working correct in both editor and build.
                // To Unity/URP, Graphics.RenderMesh() is a high-level method, it is very similar to adding a regular Renderer GameObject(but without GameObject).
                // while cmd.DrawRenderer() is more low-level, it is not a good method to solve this high-level redraw material problem.
                Graphics.RenderMesh(renderParams, meshToDraw, safeSubMeshIndex, ObjToWorldMatrix);
            }
        }

        private void OnDisable()
        {
            // remove any link with NiloToonPerCharacterRenderController
            if (NiloToonPerCharacterRenderController)
            {
                NiloToonPerCharacterRenderController.nilotoonRendererRedrawerList?.Remove(this);
            }
        }

        private void OnDestroy()
        {
            if (_bakedMeshHolder)
            {
                if (Application.isPlaying)
                {
                    // We're in play mode, so use Destroy().
                    Destroy(_bakedMeshHolder);
                }
                else
                {
                    // We're in edit mode, so use DestroyImmediate().
                    DestroyImmediate(_bakedMeshHolder);
                }
            }
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(NiloToonRendererRedrawer))]
    [CanEditMultipleObjects]
    public class NiloToonRendererRedrawerUI : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Render extra materials using this GameObject's Renderer.\n" +
                                    "- Same as manually adding materials to Renderer's Materials list, but here you can decide which SubMesh(material slot) is used for rendering each material\n" +
                                    "- Automatically be affected by the closest parent NiloToonPerCharacterRenderController\n" +
                                    "- This script can be used independently without any other NiloToon's material, script or renderer feature", MessageType.Info);

            EditorGUILayout.HelpBox("This script uses SkinnedMeshRenderer.BakeMesh() to redraw a SkinnedMeshRenderer. This process can be extremely slow, so please use this script with caution when working with SkinnedMeshRenderers.", MessageType.Info);
            EditorGUILayout.HelpBox("This script doesn't work with MotionVector pass, so effects like TAA and motion blur may not work", MessageType.Info);
            
            if (Application.isPlaying && EditorApplication.isPaused)
            {
                EditorGUILayout.HelpBox("This script will stop rendering if you pause the game while in editor's play mode", MessageType.Warning);
            }
            
            DrawDefaultInspector();
        }
    }
#endif  
}