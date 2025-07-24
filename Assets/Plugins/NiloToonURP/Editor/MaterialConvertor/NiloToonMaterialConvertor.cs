using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using System.Linq;
using NiloToon.NiloToonURP;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NiloToon.NiloToonURP
{
    public static class NiloToonMaterialConvertor
    {
        private static readonly int SurfaceTypePreset = Shader.PropertyToID("_SurfaceTypePreset");
        private static readonly int RenderOutline = Shader.PropertyToID("_RenderOutline");
        private static readonly int IsSkin = Shader.PropertyToID("_IsSkin");
        private static readonly int IsFace = Shader.PropertyToID("_IsFace");

        private static readonly int PerMaterialEnableDepthTextureRimLightAndShadow =
            Shader.PropertyToID("_PerMaterialEnableDepthTextureRimLightAndShadow");

        enum NiloToonSurfaceTypePreset
        {
            Opaque_Outline = 0,
            Opaque = 1,
            Transparent_ZWrite_Outline = 2,
            Transparent = 3,
            Transparent_ZWrite = 4,
            CutoutOpaque_Outline = 5,
            CutoutOpaque = 6,
            CutoutTransparent_ZWrite_Outline = 7,
            CutoutTransparent = 8,
            CutoutTransparent_ZWrite = 9,

            TransparentQueueTransparent_ZWrite_Outline = 10,
            TransparentQueueTransparent_ZWrite = 11,
            CutoutTransparentQueueTransparent_ZWrite_Outline = 12,
            CutoutTransparentQueueTransparent_ZWrite = 13,
        }

        enum RenderQueueGroup
        {
            Opaque,
            Cutout,
            Transparent
        }

        public static void AutoConvertMaterialsToNiloToon(List<Material> allTargetMaterials)
        {
            Shader niloToonCharShader = Shader.Find("Universal Render Pipeline/NiloToon/NiloToon_Character");
            Shader URPComplexLitShader = Shader.Find("Universal Render Pipeline/Complex Lit");
            Shader URPLitShader = Shader.Find("Universal Render Pipeline/Lit");
            Shader URPSimpleLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            Shader URPUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            Shader UniGLTFUnlitShader = Shader.Find("UniGLTF/UniUnlit");
            Shader VRMBRPMToon00Shader = Shader.Find("VRM/MToon"); // has it's own shader properties
            Shader VRMBRPMToon10Shader =
                Shader.Find("VRM10/MToon10"); // share the exact same shader properties with URP version
            Shader VRMURPMToon10Shader =
                Shader.Find(
                    "VRM10/Universal Render Pipeline/MToon10"); // share the exact same shader properties with BRP version
            Shader builtinRPStandardShader = Shader.Find("Standard");

            // clone all original materials to a new List with the same order, for later reference, since we will edit the real material later
            List<Material> allOriginalMaterialsClone = new List<Material>(allTargetMaterials);
            for (int i = 0; i < allTargetMaterials.Count; i++)
            {
                allOriginalMaterialsClone[i] = Object.Instantiate(allTargetMaterials[i]);
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Find the correct NiloToonSurfaceTypePreset index of each original material
            // (matching the index of NiloToonCharacter_SurfaceType_LWGUI_ShaderPropertyPreset.asset)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            NiloToonSurfaceTypePreset[] niloToonSurfaceTypePresetIDArray =
                new NiloToonSurfaceTypePreset[allTargetMaterials.Count];
            for (int i = 0; i < niloToonSurfaceTypePresetIDArray.Length; i++)
            {
                niloToonSurfaceTypePresetIDArray[i] =
                    NiloToonSurfaceTypePreset
                        .Opaque_Outline; // init as Opaque+Outline(index is 0), it is the default preset
            }

            for (int i = 0; i < allOriginalMaterialsClone.Count; i++)
            {
                Material matOriginal = allOriginalMaterialsClone[i];

                if (matOriginal.shader == UniGLTFUnlitShader)
                {
                    //public enum UniUnlitRenderMode
                    //{
                    //    Opaque = 0,
                    //    Cutout = 1,
                    //    Transparent = 2,
                    //}
                    int _BlendMode = (int)matOriginal.GetFloat("_BlendMode");

                    switch (_BlendMode)
                    {
                        case 0:
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Opaque_Outline;
                            break;
                        case 1:
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutOpaque_Outline;
                            break;
                        case 2:
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent;
                            break;
                        default:
                            throw new NotImplementedException(
                                "seems that VRM shader(UniGLTF/UniUnlit) added a new UniUnlitRenderMode that is not handled by NiloToon yet, contact NiloToon's developer to support this.");
                    }
                }

                if (matOriginal.shader == URPComplexLitShader ||
                    matOriginal.shader == URPLitShader ||
                    matOriginal.shader == URPSimpleLitShader ||
                    matOriginal.shader == URPUnlitShader)
                {
                    //public enum SurfaceType
                    //{
                    //    Opaque = 0,
                    //    Transparent = 1,
                    //}
                    int _Surface = (int)matOriginal.GetFloat("_Surface");

                    bool _AlphaClip = matOriginal.GetFloat("_AlphaClip") > 0.5f;

                    switch (_Surface)
                    {
                        case 0:
                            niloToonSurfaceTypePresetIDArray[i] = _AlphaClip
                                ? NiloToonSurfaceTypePreset.CutoutOpaque_Outline
                                : NiloToonSurfaceTypePreset.Opaque_Outline;
                            break;
                        case 1:
                            niloToonSurfaceTypePresetIDArray[i] = _AlphaClip
                                ? NiloToonSurfaceTypePreset.CutoutTransparent
                                : NiloToonSurfaceTypePreset.Transparent;
                            break;
                        default:
                            throw new NotImplementedException(
                                "seems that URP's Lit,Complex Lit,Simple Lit or Unlit added a new SurfaceType that is not handled by NiloToon yet, contact NiloToon's developer to support this.");
                    }
                }

                if (matOriginal.shader == builtinRPStandardShader)
                {
                    // Opaque = 0,
                    // Cutout = 1,
                    // Fade = 2, (ZWrite is off)
                    // Transparent = 3, (ZWrite is on)
                    int _Mode = (int)matOriginal.GetFloat("_Mode");

                    switch (_Mode)
                    {
                        case 0:
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Opaque_Outline;
                            break;
                        case 1:
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutOpaque_Outline;
                            break;
                        case 2:
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent;
                            break;
                        case 3:
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent_ZWrite_Outline;
                            break;
                        default:
                            throw new NotImplementedException(
                                "seems that BRP Standard shader added a new Mode that is not handled by NiloToon yet, contact NiloToon's developer to support this.");
                    }
                }

                // for any VRM00 migrated material's shader
                if (matOriginal.shader == VRMBRPMToon00Shader)
                {
                    //public enum RenderMode
                    //{
                    //    Opaque = 0,
                    //    Cutout = 1,
                    //    Transparent = 2,
                    //    TransparentWithZWrite = 3,
                    //}
                    int _BlendMode =
                        (int)matOriginal
                            .GetFloat(
                                "_BlendMode"); // '_BlendMode' in vrm00's MToon shader is the 'Rendering Type' in UI, 'RenderMode' in C#

                    // vrm00 MToon's _OutlineWidthMode
                    //public enum OutlineWidthMode
                    //{
                    //    None = 0,
                    //    WorldCoordinates = 1,
                    //    ScreenCoordinates = 2,
                    //}
                    int _OutlineWidthMode = (int)matOriginal.GetFloat("_OutlineWidthMode");
                    bool hasOutline = _OutlineWidthMode > 0;

                    switch (_BlendMode)
                    {
                        case 0:
                            niloToonSurfaceTypePresetIDArray[i] = hasOutline
                                ? NiloToonSurfaceTypePreset.Opaque_Outline
                                : NiloToonSurfaceTypePreset.Opaque;
                            break;
                        case 1:
                            niloToonSurfaceTypePresetIDArray[i] = hasOutline
                                ? NiloToonSurfaceTypePreset.CutoutOpaque_Outline
                                : NiloToonSurfaceTypePreset.CutoutOpaque;
                            break;
                        case 2:
                            niloToonSurfaceTypePresetIDArray[i] =
                                NiloToonSurfaceTypePreset.Transparent; // 'Transparent' don't have outline in NiloToon
                            break;
                        case 3:
                            niloToonSurfaceTypePresetIDArray[i] = hasOutline
                                ? NiloToonSurfaceTypePreset.Transparent_ZWrite_Outline
                                : NiloToonSurfaceTypePreset.Transparent_ZWrite;
                            break;
                        default:
                            throw new NotImplementedException(
                                "seems that VRM MToon shader(VRM/MToon) added a new Rendering Type that is not handled by NiloToon yet, contact NiloToon's developer to support this.");
                    }
                }

                // for any VRM10 migrated material's shader
                if (matOriginal.shader == VRMBRPMToon10Shader ||
                    matOriginal.shader == VRMURPMToon10Shader)
                {
                    // all VRM1 shader(BRP/URP) share the same properties{}!

                    //public enum MToon10AlphaMode
                    //{
                    //    Opaque = 0,
                    //    Cutout = 1,
                    //    Transparent = 2,
                    //}
                    int _AlphaMode = (int)matOriginal.GetFloat("_AlphaMode");

                    switch (_AlphaMode)
                    {
                        case 0:
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Opaque_Outline;
                            break;
                        case 1:
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutOpaque_Outline;
                            break;
                        case 2:
                            bool _TransparentWithZWrite = matOriginal.GetFloat("_TransparentWithZWrite") > 0.5f;
                            niloToonSurfaceTypePresetIDArray[i] = _TransparentWithZWrite
                                ? NiloToonSurfaceTypePreset.Transparent_ZWrite_Outline
                                : NiloToonSurfaceTypePreset.Transparent;
                            break;
                        default:
                            throw new NotImplementedException(
                                "seems that VRM1 MToon shader(VRM10's MToon10) added a new MToon10AlphaMode that is not handled by NiloToon yet, contact NiloToon's developer to support this.");
                    }
                }

                if (matOriginal.shader == niloToonCharShader)
                {
                    // NiloToon->NiloToon, so nothing to do, just pass SurfaceType without edit
                    niloToonSurfaceTypePresetIDArray[i] =
                        (NiloToonSurfaceTypePreset)(int)matOriginal.GetFloat("_SurfaceTypePreset");
                }

                if (matOriginal.shader.name.Contains("lilToon"))
                {
                    /*
                    // [Rendering Mode]
                    // Opaque = 0
                    // Cutout = 1
                    // Transparent = 2
                    // Refraction
                    // Fur
                    // FurCutout
                    // Gem
                    int _RenderingMode = ... there is no rendering mode in material info. _TransparentMode is not what we are looking for

                    switch (_RenderingMode)
                    {
                        case 0:
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Opaque_Outline;
                            break;
                        case 1:
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutOpaque_Outline;
                            break;
                        case 2:
                            bool _ZWrite = matOriginal.GetFloat("_ZWrite") > 0.5f;
                            niloToonSurfaceTypePresetIDArray[i] = _ZWrite ? NiloToonSurfaceTypePreset.Transparent_ZWrite_Outline : NiloToonSurfaceTypePreset.Transparent;
                            break;
                        default:
                            // it is not support, use a generic preset
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutOpaque_Outline;
                            break;
                    }
                    */

                    // so we need some complex logic to find out the correct NiloToonSurfaceType,
                    // copy from lilInspector.cs -> CheckShaderType(Material material)
                    bool isCutout = matOriginal.shader.name.Contains("Cutout");
                    bool isTransparent = matOriginal.shader.name.Contains("Transparent") ||
                                         matOriginal.shader.name.Contains("Overlay");
                    bool isOutl = matOriginal.shader.name.Contains("Outline");
                    bool isTransparentQueue = matOriginal.renderQueue > 2500;
                    bool isZWrite = matOriginal.GetFloat("_ZWrite") > 0.5f;

                    // if ZWrite is off, we treat the material as transparent
                    if      (!isCutout && !isTransparent && !isOutl && !isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Opaque; // 1
                    else if (!isCutout && !isTransparent && !isOutl && !isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent; // 2
                    else if (!isCutout && !isTransparent && !isOutl &&  isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.TransparentQueueTransparent_ZWrite; // 3
                    else if (!isCutout && !isTransparent && !isOutl &&  isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent; // 4
                    else if (!isCutout && !isTransparent &&  isOutl && !isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Opaque_Outline; // 5
                    else if (!isCutout && !isTransparent &&  isOutl && !isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent; // 6
                    else if (!isCutout && !isTransparent &&  isOutl &&  isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent_ZWrite_Outline; // 7
                    else if (!isCutout && !isTransparent &&  isOutl &&  isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent; // 8
                    else if (!isCutout &&  isTransparent && !isOutl && !isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent_ZWrite; // 9
                    else if (!isCutout &&  isTransparent && !isOutl && !isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent; // 10
                    else if (!isCutout &&  isTransparent && !isOutl &&  isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent_ZWrite; // 11
                    else if (!isCutout &&  isTransparent && !isOutl &&  isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent; // 12
                    else if (!isCutout &&  isTransparent &&  isOutl && !isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent_ZWrite_Outline; // 13
                    else if (!isCutout &&  isTransparent &&  isOutl && !isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent; // 14
                    else if (!isCutout &&  isTransparent &&  isOutl &&  isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.TransparentQueueTransparent_ZWrite; // 15
                    else if (!isCutout &&  isTransparent &&  isOutl &&  isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.Transparent; // 16
                    else if ( isCutout && !isTransparent && !isOutl && !isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutOpaque; // 17
                    else if ( isCutout && !isTransparent && !isOutl && !isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparent; // 18
                    else if ( isCutout && !isTransparent && !isOutl &&  isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutOpaque; // 19
                    else if ( isCutout && !isTransparent && !isOutl &&  isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparent; // 20
                    else if ( isCutout && !isTransparent &&  isOutl && !isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutOpaque_Outline; // 21
                    else if ( isCutout && !isTransparent &&  isOutl && !isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparent; // 22
                    else if ( isCutout && !isTransparent &&  isOutl &&  isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparentQueueTransparent_ZWrite_Outline; // 23
                    else if ( isCutout && !isTransparent &&  isOutl &&  isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparent; // 24
                    else if ( isCutout &&  isTransparent && !isOutl && !isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparentQueueTransparent_ZWrite; // 25
                    else if ( isCutout &&  isTransparent && !isOutl && !isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparent; // 26
                    else if ( isCutout &&  isTransparent && !isOutl &&  isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparent_ZWrite; // 27
                    else if ( isCutout &&  isTransparent && !isOutl &&  isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparent; // 28
                    else if ( isCutout &&  isTransparent &&  isOutl && !isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparent_ZWrite_Outline; // 29
                    else if ( isCutout &&  isTransparent &&  isOutl && !isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparent; // 30
                    else if ( isCutout &&  isTransparent &&  isOutl &&  isTransparentQueue &&  isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparentQueueTransparent_ZWrite_Outline; //31
                    else if ( isCutout &&  isTransparent &&  isOutl &&  isTransparentQueue && !isZWrite) niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutTransparent; // 32
                }

                // since there are many model's texture that alpha is not used for transparency(e.g. store special data that is not alpha),
                // we will not use texture's alpha to decide if Cutout should be enabled or not anymore
                /*
                // [feature removed]
                // If the NiloToonSurfaceTypePreset is the default one that ignores alpha, use _BaseMap's alpha to determine if cutout is needed
                // this step is useful when there is no information about what NiloToonSurfaceTypePreset should be,
                // the only information we have is the _BaseMap's alpha, the alpha will hint us what NiloToonSurfaceTypePreset is suitable
                if (matOriginal.shader == URPLitShader && niloToonSurfaceTypePresetIDArray[i] == NiloToonSurfaceTypePreset.Opaque_Outline)
                {
                    Texture2D baseMapTexture;

                    // try to get _MainTex first from any BRP material
                    baseMapTexture = matOriginal.mainTexture as Texture2D;

                    // if we can't get one, try to get _BaseMap from any URP material
                    if (!baseMapTexture)
                    {
                        if(matOriginal.HasProperty("_BaseMap"))
                        {
                            baseMapTexture = matOriginal.GetTexture("_BaseMap") as Texture2D;
                        }
                    }

                    // if the material has a _MainTex or _BaseMap
                    if (baseMapTexture != null)
                    {
                        // and if texture has alpha value
                        if(TextureHasAlpha(baseMapTexture))
                        {
                            // convert Opaque_Outline -> CutoutOpaque_Outline
                            niloToonSurfaceTypePresetIDArray[i] = NiloToonSurfaceTypePreset.CutoutOpaque_Outline;
                        }
                    }
                }
                */
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // if the material is not a URP shader, it won't have _BaseMap property,
            // so it should be using a built-in RP only shader currently(e.g. Built-in RP vrm/mtoon or any error shader material),
            // we will first switch the material's shader to built-in RP's "Standard" shader for auto material convertion by URP in the next section
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////            
            foreach (var mat in allTargetMaterials)
            {
                // skip for vrm mtoon, since we will not rely on URP's material upgrader, and handle everything by ourself
                if (mat.shader == VRMBRPMToon00Shader ||
                    mat.shader == VRMBRPMToon10Shader ||
                    mat.shader == VRMURPMToon10Shader)
                    continue;

                // support these special shaders from URP's package:
                // - PhysicalMaterial3DsMax
                // - ArnoldStandardSurface
                if (mat.HasProperty("_BASE_COLOR_MAP"))
                {
                    Texture basmeap = mat.GetTexture("_BASE_COLOR_MAP");
                    if (basmeap)
                    {
                        mat.shader = builtinRPStandardShader;
                        mat.mainTexture = basmeap;
                    }
                }

                if (!mat.HasProperty("_BaseMap") || !mat.GetTexture("_BaseMap"))
                {
                    // switch material to use built-in RP's standard shader first, so that it is prepared for URP's material upgrade
                    mat.shader = builtinRPStandardShader;
                    EditorUtility.SetDirty(mat);
                }
            }

            AssetDatabase.SaveAssets();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // if the material is not a URP's shader, we have converted it to built-in RP's "Standard" 
            // we then simulate a click to "Edit/Rendering/Materials/Convert Selected Built-in Materials to URP",
            // which means calling to UniversalRenderPipelineMaterialUpgrader.UpgradeSelectedMaterials().
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref upgraders);

            HashSet<string> shaderNamesToIgnore = new HashSet<string>();
            GetShaderNamesToIgnore(ref shaderNamesToIgnore);

            // set selection to materials that we want to upgrade
            UnityEngine.Object[] targets = new UnityEngine.Object[allTargetMaterials.Count];
            targets = allTargetMaterials.ToArray();
            var originalSelectionObjects = Selection.objects;
            Selection.objects = targets;

            // call SRP's UpgradeSelection()
            MaterialUpgrader.UpgradeSelection(upgraders, shaderNamesToIgnore, "Upgrade to UniversalRP Materials",
                MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound);

            // restore Selection
            Selection.objects = originalSelectionObjects;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // switch material to "NiloToon_Character" shader,
            // and edit any important properties
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////           
            for (int i = 0; i < allTargetMaterials.Count; i++)
            {
                Material mat = allTargetMaterials[i];
                Material originalMatClone = allOriginalMaterialsClone[i];
                NiloToonSurfaceTypePreset nilotoonSurfaceTypePresetID = niloToonSurfaceTypePresetIDArray[i];

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // if material is URP package's material (e.g. Lit.mat),
                // skip editing the material,
                // else we will be editing URP's default material that is inside URP package
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                {
                    string path = UnityEditor.AssetDatabase.GetAssetPath(mat);
                    if (path.StartsWith("Packages/com.unity.render-pipelines.universal"))
                    {
                        continue;
                    }
                }

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // if material is Particle or Sprite shader, skip editing it
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                if (mat.shader.name.Contains("Particle") ||
                    mat.shader.name.Contains("Sprite"))
                {
                    continue;
                }

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // if the current shader is not any NiloToon shader
                // switch this shader to NiloToon_Character shader
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                if (!mat.shader.name.Contains("NiloToon"))
                {
                    int originalRenderQueue = mat.renderQueue;

                    // this line will reset render queue to shader's default!
                    mat.shader = niloToonCharShader;

                    // so we restore the render queue
                    mat.renderQueue = originalRenderQueue;

                    EditorUtility.SetDirty(mat);
                }

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // if the current shader is NiloToon_Character shader (not NiloToon's sticker/environment shaders)
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                if (mat.shader == niloToonCharShader)
                {
                    string[] IsFaceTargetNames = { "face", "facial", "express", "kao", "head" };
                    string[]
                        IsFaceBanNames =
                        {
                            "Kaoru", "Surface", "phone", "headset"
                        }; // avoid "Kaoru" treated as face due to "kao", avoid "headphone" or "headset" treated as head

                    // TODO: should we include "facial" as eye target name?
                    string[] IsEyeTargetNames =
                    {
                        "eye", "iris", "pupil", "lash", "brow", "mayu", "hitomi", "shirome", "matsuge", "matuge",
                        "hairaito",
                        "doukou", "guruguru", ".me", "Canthus", "sclera", "cornea", "limbus"
                    };
                    string[] IsEyeBanNames = { "brown" }; // avoid "brown" treated as eye due to "brow"

                    string[] IsMouthTargetNames = { "mouth", "oral", "tongue", "kuchi", ".ha" };
                    string[] IsMouthBanNames = { ".hada" };

                    string[] IsTeethTargetNames = { "teeth", "tooth" };
                    string[] IsTeethBanNames = { };

                    string[]
                        IsSkinTargetNames =
                        {
                            "skin", "hada", "body", "karada"
                        }; // material with a name "body / karada" is not always skin, but we still put "body" into this array,
                    // since a skin material without enabling "IsSkin" is far worse than a non-skin material enabled "IsSkin" wrongly.
                    string[] IsSkinBanNames = { };

                    string[] IsNoOutlineTargetNames = { "noline", "nooutline", "no_line", "no_outline" };
                    string[] IsNoOutlineBanNames = { };
                    //----------------------------------------------------
                    // FaceFinal = face + (eye + mouth + teeth)
                    string[] IsFaceFinalTargetNames = new string[] { };
                    IsFaceFinalTargetNames = IsFaceFinalTargetNames.Concat(IsFaceTargetNames).ToArray();
                    IsFaceFinalTargetNames = IsFaceFinalTargetNames.Concat(IsEyeTargetNames).ToArray();
                    IsFaceFinalTargetNames = IsFaceFinalTargetNames.Concat(IsMouthTargetNames).ToArray();
                    IsFaceFinalTargetNames = IsFaceFinalTargetNames.Concat(IsTeethTargetNames).ToArray();

                    string[] IsFaceFinalBanNames = new string[] { };
                    IsFaceFinalBanNames = IsFaceFinalBanNames.Concat(IsFaceBanNames).ToArray();
                    IsFaceFinalBanNames = IsFaceFinalBanNames.Concat(IsEyeBanNames).ToArray();
                    IsFaceFinalBanNames = IsFaceFinalBanNames.Concat(IsMouthBanNames).ToArray();
                    IsFaceFinalBanNames = IsFaceFinalBanNames.Concat(IsTeethBanNames).ToArray();
                    //----------------------------------------------------
                    // SkinFinal = skin + (face + mouth)
                    string[] IsSkinFinalTargetNames = new string[] { };
                    IsSkinFinalTargetNames = IsSkinFinalTargetNames.Concat(IsSkinTargetNames).ToArray();
                    IsSkinFinalTargetNames = IsSkinFinalTargetNames.Concat(IsFaceTargetNames).ToArray();
                    IsSkinFinalTargetNames = IsSkinFinalTargetNames.Concat(IsMouthTargetNames).ToArray();

                    string[] IsSkinFinalBanNames = new string[] { };
                    IsSkinFinalBanNames = IsSkinFinalBanNames.Concat(IsSkinBanNames).ToArray();
                    //IsSkinFinalBanNames = IsSkinFinalBanNames.Concat(IsFaceBanNames).ToArray();
                    //IsSkinFinalBanNames = IsSkinFinalBanNames.Concat(IsMouthBanNames).ToArray();
                    //----------------------------------------------------
                    List<string> IsNoOutlineFinalTargetNames =
                        IsFaceFinalTargetNames.Where(x => x != "face" && x != "head").ToList();
                    IsNoOutlineFinalTargetNames.Concat(IsNoOutlineTargetNames);

                    List<string> IsNoOutlineFinalBanNames = IsFaceFinalBanNames.ToList();
                    IsNoOutlineFinalBanNames.Concat(IsNoOutlineBanNames);

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // auto disable classic outline if material name contain face/outline related keywords that should not render classic outline
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    bool isNoOutline = false;
                    foreach (var keyword in IsNoOutlineFinalTargetNames)
                    {
                        isNoOutline |= NiloToonUtils.NameHasKeyword(mat.name, keyword);
                    }

                    foreach (var keyword in IsNoOutlineFinalBanNames)
                    {
                        isNoOutline &= !NiloToonUtils.NameHasKeyword(mat.name, keyword);
                    }

                    if (isNoOutline)
                    {
                        switch (nilotoonSurfaceTypePresetID)
                        {
                            case NiloToonSurfaceTypePreset.Opaque_Outline:
                                nilotoonSurfaceTypePresetID = NiloToonSurfaceTypePreset.Opaque;
                                break;
                            case NiloToonSurfaceTypePreset.Transparent_ZWrite_Outline:
                                nilotoonSurfaceTypePresetID = NiloToonSurfaceTypePreset.Transparent_ZWrite;
                                break;
                            case NiloToonSurfaceTypePreset.CutoutOpaque_Outline:
                                nilotoonSurfaceTypePresetID = NiloToonSurfaceTypePreset.CutoutOpaque;
                                break;
                            case NiloToonSurfaceTypePreset.CutoutTransparent_ZWrite_Outline:
                                nilotoonSurfaceTypePresetID = NiloToonSurfaceTypePreset.CutoutTransparent_ZWrite;
                                break;
                            case NiloToonSurfaceTypePreset.TransparentQueueTransparent_ZWrite_Outline:
                                nilotoonSurfaceTypePresetID =
                                    NiloToonSurfaceTypePreset.TransparentQueueTransparent_ZWrite;
                                break;
                            case NiloToonSurfaceTypePreset.CutoutTransparentQueueTransparent_ZWrite_Outline:
                                nilotoonSurfaceTypePresetID = NiloToonSurfaceTypePreset
                                    .CutoutTransparentQueueTransparent_ZWrite;
                                break;
                            default:
                                // do nothing for non-outline material input
                                break;
                        }
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // auto set SurfaceType preset
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////                            
                    SetMaterialNiloToonSurfaceTypeAndProperties(mat, nilotoonSurfaceTypePresetID);

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // port common vrm00 mtoon00 & vrm10 mtoon10 to nilotoon
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    if (originalMatClone.shader == VRMBRPMToon00Shader ||
                        originalMatClone.shader == VRMBRPMToon10Shader ||
                        originalMatClone.shader == VRMURPMToon10Shader)
                    {
                        // port base map
                        mat.SetTexture("_BaseMap", originalMatClone.GetTexture("_MainTex"));

                        // port base color (all vrm00 & vrm10 rely on _Color)
                        mat.SetFloat("_MultiplyBRPColor", 1);
                        //mat.SetColor("_BaseColor", originalMatClone.GetColor("_Color")); // not replacing _BaseColor as _Color, since we now * _Color already, so _BaseColor should be in default value(1,1,1,1)

                        // port outline tint color
                        mat.SetColor("_OutlineTintColor", originalMatClone.GetColor("_OutlineColor"));
                        // TODO: handle 'outline color = replace' mode  

                        // cancel skin outline override
                        Color skinOutlineOverrideShadowColor = mat.GetColor("_OutlineTintColorSkinAreaOverride");
                        skinOutlineOverrideShadowColor.a = 0;
                        mat.SetColor("_OutlineTintColorSkinAreaOverride", skinOutlineOverrideShadowColor);
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // auto set outline extra mul for VRM00 mtoon00 material
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    if (originalMatClone.shader == VRMBRPMToon00Shader)
                    {
                        const float niloToonWidthRelativeToMToon00 = 4;
                        mat.SetFloat("_OutlineWidthExtraMultiplier", niloToonWidthRelativeToMToon00);

                        // extra check to limit any unexpected large outline width
                        float maxFinalWidth = 0.6f; // 0.6 is NiloToon's default outline width
                        float finalWidth = Mathf.Min(maxFinalWidth,
                            mat.GetFloat("_OutlineWidth") * niloToonWidthRelativeToMToon00);
                        mat.SetFloat("_OutlineWidth", finalWidth / niloToonWidthRelativeToMToon00);
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // auto set outline extra mul for VRM10 mtoon10 material
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    if (originalMatClone.shader == VRMBRPMToon10Shader ||
                        originalMatClone.shader == VRMURPMToon10Shader)
                    {
                        const float niloToonWidthRelativeToMToon10 = 512;
                        mat.SetFloat("_OutlineWidthExtraMultiplier", niloToonWidthRelativeToMToon10);

                        // extra check to limit any unexpected large outline width
                        float maxFinalWidth = 0.6f; // 0.6 is NiloToon's default outline width
                        float finalWidth = Mathf.Min(maxFinalWidth,
                            mat.GetFloat("_OutlineWidth") * niloToonWidthRelativeToMToon10);
                        mat.SetFloat("_OutlineWidth", finalWidth / niloToonWidthRelativeToMToon10);
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // port vrm00 mtoon00 properties to nilotoon
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    if (originalMatClone.shader == VRMBRPMToon00Shader)
                    {
                        float MToonScrollX = originalMatClone.GetFloat("_UvAnimScrollX");
                        float MToonScrollY = originalMatClone.GetFloat("_UvAnimScrollY");
                        float MToonRotate = originalMatClone.GetFloat("_UvAnimRotation");
                        Vector4 _UV0ScrollSpeed = new Vector4();
                        _UV0ScrollSpeed.x = MToonScrollX;
                        _UV0ScrollSpeed.y = MToonScrollY;
                        mat.SetVector("_UV0ScrollSpeed", _UV0ScrollSpeed);
                        mat.SetFloat("_UV0RotateSpeed", MToonRotate);
                        if (MToonScrollX != 0 || MToonScrollY != 0 || MToonRotate != 0)
                        {
                            mat.SetFloat("_EnableUVEditGroup", 1);
                        }

                        //public enum CullMode
                        //{
                        //    Off = 0,
                        //    Front = 1,
                        //    Back = 2,
                        //}
                        int _CullMode = (int)originalMatClone.GetFloat("_CullMode");
                        switch (_CullMode)
                        {
                            // Cull Off = Render Both
                            case 0:
                                mat.SetFloat("_RenderFacePreset", 2);
                                mat.SetFloat("_Cull", 0);
                                mat.SetFloat("_CullOutline", 1);
                                break;
                            // Cull Front = Render Back
                            case 1:
                                mat.SetFloat("_RenderFacePreset", 1);
                                mat.SetFloat("_Cull", 1);
                                mat.SetFloat("_CullOutline", 2);
                                break;
                            // Cull Back = Render Front
                            case 2:
                                mat.SetFloat("_RenderFacePreset", 0);
                                mat.SetFloat("_Cull", 2);
                                mat.SetFloat("_CullOutline", 1);
                                break;
                        }

                        Texture2D _ShadeTexture = originalMatClone.GetTexture("_ShadeTexture") as Texture2D;
                        if (_ShadeTexture)
                        {
                            mat.SetTexture("_OverrideShadowColorTex", _ShadeTexture);

                            mat.SetFloat("_UseOverrideShadowColorByTexture", 1);
                            mat.EnableKeyword("_OVERRIDE_SHADOWCOLOR_BY_TEXTURE");

                            mat.SetColor("_OverrideShadowColorTexTintColor",
                                originalMatClone.GetColor("_ShadeColor"));
                        }

                        Texture2D _OutlineWidthTexture =
                            originalMatClone.GetTexture("_OutlineWidthTexture") as Texture2D;
                        if (_OutlineWidthTexture)
                        {
                            mat.SetTexture("_OutlineWidthTex", _OutlineWidthTexture);
                        }

                        // TODO: matcap additive direct port

                        // TODO: additive param rim
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // port vrm10 mtoon10 properties to nilotoon
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    if (originalMatClone.shader == VRMBRPMToon10Shader ||
                        originalMatClone.shader == VRMURPMToon10Shader)
                    {
                        float MToonScrollX = originalMatClone.GetFloat("_UvAnimScrollXSpeed");
                        float MToonScrollY = originalMatClone.GetFloat("_UvAnimScrollXSpeed");
                        float MToonRotate = originalMatClone.GetFloat("_UvAnimScrollXSpeed");
                        Vector4 _UV0ScrollSpeed = new Vector4();
                        _UV0ScrollSpeed.x = MToonScrollX;
                        _UV0ScrollSpeed.y = MToonScrollY;
                        mat.SetVector("_UV0ScrollSpeed", _UV0ScrollSpeed);
                        mat.SetFloat("_UV0RotateSpeed", MToonRotate);
                        if (MToonScrollX != 0 || MToonScrollY != 0 || MToonRotate != 0)
                        {
                            mat.SetFloat("_EnableUVEditGroup", 1);
                        }

                        int _M_CullMode = (int)originalMatClone.GetFloat("_M_CullMode");
                        switch (_M_CullMode)
                        {
                            // Cull Off = Render Both
                            case 0:
                                mat.SetFloat("_RenderFacePreset", 2);
                                mat.SetFloat("_Cull", 0);
                                mat.SetFloat("_CullOutline", 1);
                                break;
                            // Cull Front = Render Back
                            case 1:
                                mat.SetFloat("_RenderFacePreset", 1);
                                mat.SetFloat("_Cull", 1);
                                mat.SetFloat("_CullOutline", 2);
                                break;
                            // Cull Back = Render Front
                            case 2:
                                mat.SetFloat("_RenderFacePreset", 0);
                                mat.SetFloat("_Cull", 2);
                                mat.SetFloat("_CullOutline", 1);
                                break;
                        }

                        mat.renderQueue += (int)originalMatClone.GetFloat("_RenderQueueOffset");

                        Texture2D _ShadeTex = originalMatClone.GetTexture("_ShadeTex") as Texture2D;
                        if (_ShadeTex)
                        {
                            mat.SetTexture("_OverrideShadowColorTex", _ShadeTex);

                            mat.SetFloat("_UseOverrideShadowColorByTexture", 1);
                            mat.EnableKeyword("_OVERRIDE_SHADOWCOLOR_BY_TEXTURE");

                            mat.SetColor("_OverrideShadowColorTexTintColor",
                                originalMatClone.GetColor("_ShadeColor"));
                        }

                        // TODO: matcap additive direct port

                        // TODO: additive param rim
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // port liltoon properties to nilotoon
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    if (originalMatClone.shader.name.Contains("lilToon"))
                    {
                        Port_lilToon_properties_To_NiloToon(originalMatClone, mat);
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // Auto enable NiloToon mat's texture feature(uniform+keyword) if any textures are assigned.
                    // *Skip if the input material is:
                    //  - NiloToon_Character shader
                    //  - any lilToon shader
                    // since we handled them already, if we do it again here, we may enable texture features that should be keep disabled)
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    if ((originalMatClone.shader != niloToonCharShader) &&
                        !(originalMatClone.name.Contains("lilToon")))
                    {
                        if (mat.GetTexture("_OutlineWidthTex"))
                        {
                            mat.SetFloat("_UseOutlineWidthTex", 1);
                            mat.EnableKeyword("_OUTLINEWIDTHMAP");

                            mat.SetVector("_OutlineWidthTexChannelMask",
                                new Vector4(0, 1, 0, 0)); // vrmc mtoon's _OutlineWidthTex use G channel
                        }

                        if (mat.GetTexture("_BumpMap"))
                        {
                            mat.SetFloat("_UseNormalMap", 1);
                            mat.EnableKeyword("_NORMALMAP");
                        }

                        if (mat.GetTexture("_EmissionMap"))
                        {
                            mat.SetFloat("_UseEmission", 1);
                            mat.EnableKeyword("_EMISSION");
                        }
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // Reset _Parallax from 0.02 -> 0.005(default value) if _ParallaxMap doesn't exist
                    // so _Parallax is not a modified property which confuses user
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    if (originalMatClone.HasProperty("_ParallaxMap"))
                    {
                        if (!originalMatClone.GetTexture("_ParallaxMap"))
                        {
                            mat.SetFloat("_Parallax", 0.005f);
                        }
                    }
                    else
                    {
                        mat.SetFloat("_Parallax", 0.005f);
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // auto enable 'IsFace' if material name contains 'face' related keywords
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////     
                    bool isFace = false;
                    foreach (var keyword in IsFaceFinalTargetNames)
                    {
                        isFace |= NiloToonUtils.NameHasKeyword(mat.name, keyword);
                    }

                    foreach (var keyword in IsFaceFinalBanNames)
                    {
                        isFace &= !NiloToonUtils.NameHasKeyword(mat.name, keyword);
                    }

                    if (isFace)
                    {
                        SetMaterialAsIsFace(mat);
                        // *do not set material as skin
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // auto enable 'IsSkin' if material name contains 'skin' related keywords
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////                           
                    bool isSkin = false;
                    foreach (var keyword in IsSkinFinalTargetNames)
                    {
                        isSkin |= NiloToonUtils.NameHasKeyword(mat.name, keyword);
                    }

                    foreach (var keyword in IsSkinFinalBanNames)
                    {
                        isSkin &= !NiloToonUtils.NameHasKeyword(mat.name, keyword);
                    }

                    if (isSkin)
                    {
                        SetMaterialAsIsSkin(mat);
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // auto disable depth tex shadow if
                    // - it is eye (e.g. eye white material)
                    // - it doesn't draw depth texture (e.g., render queue >= 2500)
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    if (NiloToonUtils.NameHasKeyword(mat.name, "eye") ||
                        mat.renderQueue >= 2500)
                    {
                        mat.SetFloat(PerMaterialEnableDepthTextureRimLightAndShadow, 0);
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // Stencil preset matching
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    int _StencilComp = (int)mat.GetFloat("_StencilComp");
                    int _StencilPass = (int)mat.GetFloat("_StencilPass");

                    int resultStencilPresetID = 0; // default = Disabled

                    // [copy logic from NiloToonCharacter_StencilPreset_LWGUI_ShaderPropertyPreset]
                    // matching [1]: 1st>Writer (Eyebrow)
                    if (_StencilComp == 8 && _StencilPass == 2)
                    {
                        resultStencilPresetID = 1;
                    }

                    // matching [2]: 2nd>Reader (Original Hair)
                    if (_StencilComp == 6 && _StencilPass == 0)
                    {
                        resultStencilPresetID = 2;
                    }

                    // matching [3]: 3rd>Reader-Invert (Redraw Hair)
                    if (_StencilComp == 3 && _StencilPass == 0)
                    {
                        resultStencilPresetID = 3;
                    }

                    mat.SetFloat("_StencilPreset", resultStencilPresetID);

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // tell unity we edited material
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        private static void Port_lilToon_properties_To_NiloToon(Material fromlilToonMat, Material toNiloToonMat)
        {
            // try to match as many lilToon 1.7.2's lts_o.shader properties as possible
            // (?) _***** means a liltoon property not yet supported by nilotoon, under consideration
            // (X) _***** means a liltoon property not supported by nilotoon by design
            // (XV) _***** means a liltoon property not supported by nilotoon material by design, but it should be controlled by nilotoon's volume
            // (Same) _***** means a liltoon property is supported by nilotoon already, since the name is the same, no action needed

            //----------------------------------------------------------------------------------------------------------------------
            // Dummy
            // (X) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // Base
            toNiloToonMat.SetFloat("_EnableRendering",
                fromlilToonMat.GetFloat("_Invisible") > 0.5
                    ? 0
                    : 1); // (lilToon: early return "init as 0 struct" in vertex shader, same as NiloToon)
            // (XV) _AsUnlit (lilToon: lerp main light color to white, lerp additional light color to black)
            // (Same)_Cutoff (lilToon: standard alpha clip threshold, same as NiloToon/Lit)
            // (?) _SubpassCutoff (NiloToon: similar to _CutOff, but only for "subpass", not sure what Subpass is)
            // (X) _FlipNormal (NiloToon: always perform flipNormal, there is no option in material) (lilToon: sFlipBackfaceNormal -> N = facing < (_FlipNormal-1.0) ? -N : N. Similar to NiloToon)
            // (?) _ShiftBackfaceUV (NiloToon: too specific, not sure if it is a good idea to add it) (lilToon: back face UV += (1,0) or not. lilToon's doc: Shift the UVs on the back by 1.0 in the X-axis direction. By turning this on and setting the Tiling's X to 0.5, and doubling the texture's resolution horizontally to place two textures side by side, you can create an expression where different textures are applied to the front and back.)
            toNiloToonMat.SetFloat("_BackFaceForceShadow",
                fromlilToonMat.GetFloat(
                    "_BackfaceForceShadow")); // lilToon:  float bfshadow = (fd.facing < 0.0) ? 1.0 - _BackfaceForceShadow : 1.0;
            toNiloToonMat.SetColor("_BackFaceBaseMapReplaceColor",
                fromlilToonMat.GetColor(
                    "_BackfaceColor")); // lilToon: fd.col.rgb = (fd.facing < 0.0) ? lerp(fd.col.rgb, _BackfaceColor.rgb * fd.lightColor, _BackfaceColor.a) : fd.col.rgb;
            // (X) _VertexLightStrength (NiloToon: this lil prop is for BRP only, ignored) (lilToon: for BRP only)
            // (XV) _LightMinLimit (lilToon: lightColor = clamp(lightColor, _LightMinLimit, _LightMaxLimit);)               
            // (XV) _LightMaxLimit (lilToon: lightColor = clamp(lightColor, _LightMinLimit, _LightMaxLimit);)              
            // (X) _BeforeExposureLimit (NiloToon: this lilToon prop is for HDRP only, ignored)        
            // (XV) _MonochromeLighting (lilToon: lightColor = lerp(lightColor, lilGray(lightColor), _MonochromeLighting);)         
            // (X) _AlphaBoostFA (NiloToon: this lilToon prop is for forward add (FA) pass, ignored)(lilToon: fd.col.rgb *= saturate(fd.col.a * _AlphaBoostFA);)            
            // (XV) _lilDirectionalLightStrength (lilToon: lightColor *= _lilDirectionalLightStrength)
            // (XV) _LightDirectionOverride (lilToon: add a object->world space light vector to light dir, re-normalize? (not sure))    
            // (?) _AAStrength (lilToon: for value with sharp change, apply fwidth AA by -> (value - borderMin) / saturate(borderMax - borderMin + fwidth(value) * aascale))                 
            // (?) _UseDither (lilToon: dither fade0out per material)                  
            // (?) _DitherTex (lilToon: dither fadeout per material)                 
            // (?) _DitherMaxValue (lilToon: dither fadeout per material)           

            //----------------------------------------------------------------------------------------------------------------------
            // Main
            toNiloToonMat.SetColor("_BaseColor", fromlilToonMat.GetColor("_Color"));
            toNiloToonMat.SetTexture("_BaseMap", fromlilToonMat.GetTexture("_MainTex"));
            toNiloToonMat.SetVector("_BaseMap_ST", fromlilToonMat.GetVector("_MainTex_ST")); // auto uniform by Unity
            // (!) _MainTex_ScrollRotate // TODO: apply the BaseMap's uv, not UV0!
            // (?) _MainTexHSVG (lilToon: hsv = float3(hsv.x+hsvg.x,saturate(hsv.y*hsvg.y),saturate(hsv.z*hsvg.z));)        
            // (X) _MainGradationStrength (NiloToon: too specific, ignored)(lilToon: rgb per channel color 1D remap)
            // (X) _MainGradationTex (NiloToon: too specific, ignored)(lilToon: rgb per channel color 1D remap)     
            // (?) _MainColorAdjustMask (lilToon: mask of _MainTexHSVG)

            //----------------------------------------------------------------------------------------------------------------------
            // Main2nd (port to NiloToon's BaseMapStackingLayer1)
            float _UseMain2ndTex = fromlilToonMat.GetFloat("_UseMain2ndTex");
            toNiloToonMat.SetFloat("_BaseMapStackingLayer1Enable", _UseMain2ndTex);
            if (_UseMain2ndTex > 0.5)
            {
                toNiloToonMat.EnableKeyword("_BASEMAP_STACKING_LAYER1");
            }
            else
            {
                toNiloToonMat.DisableKeyword("_BASEMAP_STACKING_LAYER1");
            }

            toNiloToonMat.SetColor("_BaseMapStackingLayer1TintColor", fromlilToonMat.GetColor("_Color2nd"));
            toNiloToonMat.SetTexture("_BaseMapStackingLayer1Tex", fromlilToonMat.GetTexture("_Main2ndTex"));
            toNiloToonMat.SetVector("_BaseMapStackingLayer1TexUVScaleOffset",
                fromlilToonMat.GetVector("_Main2ndTex_ST"));

            // liltoon / nilotoon are both in degree
            toNiloToonMat.SetFloat("_BaseMapStackingLayer1TexUVRotatedAngle",
                fromlilToonMat.GetFloat("_Main2ndTexAngle") * 180f / Mathf.PI);

            {
                Vector4 _Main2ndTex_ScrollRotate = fromlilToonMat.GetVector("_Main2ndTex_ScrollRotate");
                toNiloToonMat.SetVector("_BaseMapStackingLayer1TexUVAnimSpeed",
                    new Vector4(_Main2ndTex_ScrollRotate.x, _Main2ndTex_ScrollRotate.y, 0, 0));

                // [Don't use _Main2ndTex_ScrollRotate.z since it is not used by liltoon]
                // [use the above _Main2ndTexAngle instead]
                //toNiloToonMat.SetFloat("_BaseMapStackingLayer1TexUVRotatedAngle", _Main2ndTex_ScrollRotate.z * Mathf.Rad2Deg);

                // liltoon's speed is "cycle per second", nilotoon is "degree per second"
                toNiloToonMat.SetFloat("_BaseMapStackingLayer1TexUVRotateSpeed", _Main2ndTex_ScrollRotate.w * 360);
            }

            // liltoon's _Main2ndTex_UVMode = UV0|UV1|UV2|UV3|MatCap
            toNiloToonMat.SetFloat("_BaseMapStackingLayer1TexUVIndex", fromlilToonMat.GetFloat("_Main2ndTex_UVMode"));
            toNiloToonMat.SetFloat("_BaseMapStackingLayer1ApplytoFaces", fromlilToonMat.GetFloat("_Main2ndTex_Cull"));
            // (X) _Main2ndTexDecalAnimation   
            // (X) _Main2ndTexDecalSubParam    
            // (X) _Main2ndTexIsDecal          
            // (X) _Main2ndTexIsLeftOnly       
            // (X) _Main2ndTexIsRightOnly      
            // (X) _Main2ndTexShouldCopy       
            // (X) _Main2ndTexShouldFlipMirror 
            // (X) _Main2ndTexShouldFlipCopy   
            // (X) _Main2ndTexIsMSDF           
            toNiloToonMat.SetTexture("_BaseMapStackingLayer1MaskTex", fromlilToonMat.GetTexture("_Main2ndBlendMask"));
            {
                // liltoon's blend mode:
                // 0: Normal
                // 1: Add
                // 2: Screen
                // 3: Multiply

                // NiloToon's blend mode:
                // 0: Normal RGBA
                // 1: Normal (same as liltoon)
                // 2: Add (same as liltoon)
                // 3: Screen (same as liltoon)
                // 4: Multiply (same as liltoon)
                // 5: None
                float _Main2ndTexBlendMode = fromlilToonMat.GetFloat("_Main2ndTexBlendMode");
                float NiloToonBlendMode = 0; // default 0 (Normal RGBA)
                switch (_Main2ndTexBlendMode)
                {
                    case 0:
                        NiloToonBlendMode = 1;
                        break;
                    case 1:
                        NiloToonBlendMode = 2;
                        break;
                    case 2:
                        NiloToonBlendMode = 3;
                        break;
                    case 3:
                        NiloToonBlendMode = 4;
                        break;
                }

                toNiloToonMat.SetFloat("_BaseMapStackingLayer1ColorBlendMode", NiloToonBlendMode);
            }
            // (X) _Main2ndTexAlphaMode // TODO?       
            // (X) _Main2ndEnableLighting      
            // (X) _Main2ndDissolveMask        
            // (X) _Main2ndDissolveNoiseMask   
            // (X) _Main2ndDissolveNoiseMask_ScrollRotate
            // (X) _Main2ndDissolveNoiseStrength
            // (X) _Main2ndDissolveColor       
            // (X) _Main2ndDissolveParams      
            // (X) _Main2ndDissolvePos         
            // (X) _Main2ndDistanceFade

            //----------------------------------------------------------------------------------------------------------------------
            // Main3rd (port to NiloToon's BaseMapStackingLayer2)
            float _UseMain3rdTex = fromlilToonMat.GetFloat("_UseMain3rdTex");
            toNiloToonMat.SetFloat("_BaseMapStackingLayer2Enable", _UseMain3rdTex);
            if (_UseMain3rdTex == 1)
            {
                toNiloToonMat.EnableKeyword("_BASEMAP_STACKING_LAYER2");
            }
            else
            {
                toNiloToonMat.DisableKeyword("_BASEMAP_STACKING_LAYER2");
            }

            toNiloToonMat.SetColor("_BaseMapStackingLayer2TintColor", fromlilToonMat.GetColor("_Color3rd"));
            toNiloToonMat.SetTexture("_BaseMapStackingLayer2Tex", fromlilToonMat.GetTexture("_Main3rdTex"));
            toNiloToonMat.SetVector("_BaseMapStackingLayer2TexUVScaleOffset",
                fromlilToonMat.GetVector("_Main3rdTex_ST"));

            // liltoon / nilotoon are both in degree
            toNiloToonMat.SetFloat("_BaseMapStackingLayer2TexUVRotatedAngle",
                fromlilToonMat.GetFloat("_Main3rdTexAngle"));

            {
                Vector4 _Main3rdTex_ScrollRotate = fromlilToonMat.GetVector("_Main3rdTex_ScrollRotate");
                toNiloToonMat.SetVector("_BaseMapStackingLayer2TexUVAnimSpeed",
                    new Vector4(_Main3rdTex_ScrollRotate.x, _Main3rdTex_ScrollRotate.y, 0, 0));

                // [Don't use _Main3rdTex_ScrollRotate.z since it is not used by liltoon]
                // [use the above _Main3rdTexAngle instead]
                //toNiloToonMat.SetFloat("_BaseMapStackingLayer2TexUVRotatedAngle", _Main3rdTex_ScrollRotate.z * Mathf.Rad2Deg);

                // liltoon's speed is "cycle per second", nilotoon is "degree per second"
                toNiloToonMat.SetFloat("_BaseMapStackingLayer2TexUVRotateSpeed", _Main3rdTex_ScrollRotate.w * 360);
            }

            // liltoon's _Main3rdTex_UVMode = UV0|UV1|UV2|UV3|MatCap
            toNiloToonMat.SetFloat("_BaseMapStackingLayer2TexUVIndex", fromlilToonMat.GetFloat("_Main3rdTex_UVMode"));
            toNiloToonMat.SetFloat("_BaseMapStackingLayer2ApplytoFaces", fromlilToonMat.GetFloat("_Main3rdTex_Cull"));
            // (X) _Main3rdTexDecalAnimation   
            // (X) _Main3rdTexDecalSubParam    
            // (X) _Main3rdTexIsDecal          
            // (X) _Main3rdTexIsLeftOnly       
            // (X) _Main3rdTexIsRightOnly      
            // (X) _Main3rdTexShouldCopy       
            // (X) _Main3rdTexShouldFlipMirror 
            // (X) _Main3rdTexShouldFlipCopy   
            // (X) _Main3rdTexIsMSDF           
            toNiloToonMat.SetTexture("_BaseMapStackingLayer2MaskTex", fromlilToonMat.GetTexture("_Main3rdBlendMask"));
            {
                // liltoon's blend mode:
                // 0: Normal
                // 1: Add
                // 2: Screen
                // 3: Multiply

                // NiloToon's blend mode:
                // 0: Normal RGBA
                // 1: Normal (same as liltoon)
                // 2: Add (same as liltoon)
                // 3: Screen (same as liltoon)
                // 4: Multiply (same as liltoon)
                // 5: None
                float _Main3rdTexBlendMode = fromlilToonMat.GetFloat("_Main3rdTexBlendMode");
                float NiloToonBlendMode = 0; // default 0 (Normal RGBA)
                switch (_Main3rdTexBlendMode)
                {
                    case 0:
                        NiloToonBlendMode = 1;
                        break;
                    case 1:
                        NiloToonBlendMode = 2;
                        break;
                    case 2:
                        NiloToonBlendMode = 3;
                        break;
                    case 3:
                        NiloToonBlendMode = 4;
                        break;
                }

                toNiloToonMat.SetFloat("_BaseMapStackingLayer2ColorBlendMode", NiloToonBlendMode);
            }
            // (X) _Main3rdTexAlphaMode // TODO?       
            // (X) _Main3rdEnableLighting      
            // (X) _Main3rdDissolveMask        
            // (X) _Main3rdDissolveNoiseMask   
            // (X) _Main3rdDissolveNoiseMask_ScrollRotate
            // (X) _Main3rdDissolveNoiseStrength
            // (X) _Main3rdDissolveColor       
            // (X) _Main3rdDissolveParams      
            // (X) _Main3rdDissolvePos         
            // (X) _Main3rdDistanceFade        

            //----------------------------------------------------------------------------------------------------------------------
            // Alpha Mask

            // lilToon's _AlphaMaskMode
            // 0 = None
            // 1 = Replace
            // 2 = Multiply
            // 3 = Add
            // 4 = Subtract
            int _AlphaMaskMode = (int)fromlilToonMat.GetFloat("_AlphaMaskMode");
            if (_AlphaMaskMode != 0)
            {
                toNiloToonMat.SetFloat("_UseAlphaOverrideTex", 1);
                toNiloToonMat.EnableKeyword("_ALPHAOVERRIDEMAP");
                toNiloToonMat.SetVector("_AlphaOverrideTexChannelMask", new Vector4(1f, 0f, 0f, 0f)); //r channel

                // convert to nilotoon
                // 0 = Replace
                // 1 = Multiply
                // 2 = Add
                // 3 = Subtract
                toNiloToonMat.SetFloat("_AlphaOverrideMode", Mathf.Min(3, _AlphaMaskMode - 1));
            }
            else
            {
                toNiloToonMat.SetFloat("_UseAlphaOverrideTex", 0);
                toNiloToonMat.DisableKeyword("_ALPHAOVERRIDEMAP");
            }

            toNiloToonMat.SetTexture("_AlphaOverrideTex", fromlilToonMat.GetTexture("_AlphaMask"));

            // liltoon GUI's invert toggle is not a property, it translate to a MAD (* scale + value)
            toNiloToonMat.SetFloat("_AlphaOverrideTexValueScale", fromlilToonMat.GetFloat("_AlphaMaskScale"));
            toNiloToonMat.SetFloat("_AlphaOverrideTexValueOffset", fromlilToonMat.GetFloat("_AlphaMaskValue"));

            //----------------------------------------------------------------------------------------------------------------------
            // NormalMap
            int _UseBumpMap = (int)fromlilToonMat.GetFloat("_UseBumpMap");
            if (_UseBumpMap == 1)
            {
                toNiloToonMat.SetFloat("_UseNormalMap", 1);
                toNiloToonMat.EnableKeyword("_NORMALMAP");
            }
            else
            {
                toNiloToonMat.SetFloat("_UseNormalMap", 0);
                toNiloToonMat.DisableKeyword("_NORMALMAP");
            }
            // (Same)_BumpMap   
            // (Same)_BumpScale

            //----------------------------------------------------------------------------------------------------------------------
            // NormalMap 2nd (apply to nilotoon's detail map?)
            // (?) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // Anisotropy
            // (X) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // Backlight
            // (X) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // Shadow

            toNiloToonMat.SetFloat("_EnableShadowColor", fromlilToonMat.GetFloat("_UseShadow"));

            Texture2D occlusionMap = fromlilToonMat.GetTexture("_ShadowBorderMask") as Texture2D;
            toNiloToonMat.SetTexture("_OcclusionMap", occlusionMap);
            if (occlusionMap)
            {
                toNiloToonMat.SetFloat("_UseOcclusion", 1);
                toNiloToonMat.EnableKeyword("_OCCLUSIONMAP");
            }
            else
            {
                toNiloToonMat.SetFloat("_UseOcclusion", 0);
                toNiloToonMat.DisableKeyword("_OCCLUSIONMAP");
            }

            //----------------------------------------------------------------------------------------------------------------------
            // Rim Shade

            //----------------------------------------------------------------------------------------------------------------------
            // Reflection
            // TODO

            //----------------------------------------------------------------------------------------------------------------------
            // MatCap
            // TODO (apply to basemap stacking layer3, base on blend mode)

            //----------------------------------------------------------------------------------------------------------------------
            // MatCap 2nd
            // TODO (apply to basemap stacking layer4, apply base on blend mode)

            //----------------------------------------------------------------------------------------------------------------------
            // Rim
            // TODO (we should write a ALU rim in NiloToon, similar to lilToon)

            //----------------------------------------------------------------------------------------------------------------------
            // Glitter
            // (X) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // Emission
            int _UseEmission = (int)fromlilToonMat.GetFloat("_UseEmission");
            if (_UseEmission == 1)
            {
                toNiloToonMat.SetFloat("_UseEmission", 1);
                toNiloToonMat.EnableKeyword("_EMISSION");
            }
            else
            {
                toNiloToonMat.SetFloat("_UseEmission", 0);
                toNiloToonMat.DisableKeyword("_EMISSION");
            }

            // (Same)_EmissionColor
            toNiloToonMat.SetFloat("_EmissionIntensity",
                fromlilToonMat.GetColor("_EmissionColor").a); // lilToon's _EmissionColor.a is the intensity

            // (Same)_EmissionMap                
            toNiloToonMat.SetVector("_EmissionMapUVScrollSpeed",
                fromlilToonMat.GetVector("_EmissionMap_ScrollRotate"));
            // (X) _EmissionMap_UVMode         
            toNiloToonMat.SetFloat("_MultiplyBaseColorToEmissionColor",
                fromlilToonMat.GetFloat("_EmissionMainStrength"));
            //_EmissionBlend              
            //_EmissionBlendMask          
            // (X) _EmissionBlendMask_ScrollRot
            // (X) _EmissionBlendMode          
            // (X) _EmissionBlink              
            // (X) _EmissionUseGrad            
            // (X) _EmissionGradTex            
            // (X) _EmissionGradSpeed          
            // (X) _EmissionParallaxDepth      
            // (X) _EmissionFluorescence       

            //----------------------------------------------------------------------------------------------------------------------
            // Emission2nd
            // (X) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // Parallax
            int _UseParallax = (int)fromlilToonMat.GetFloat("_UseParallax");
            if (_UseParallax == 1)
            {
                toNiloToonMat.SetFloat("_ParallaxMapEnable", 1);
                toNiloToonMat.EnableKeyword("_PARALLAXMAP");
            }
            else
            {
                toNiloToonMat.SetFloat("_ParallaxMapEnable", 0);
                toNiloToonMat.DisableKeyword("_PARALLAXMAP");
            }

            // (X) _UsePOM
            // (Same) _ParallaxMap
            // (Same) _Parallax
            // (X) _ParallaxOffset

            //----------------------------------------------------------------------------------------------------------------------
            // Distance Fade
            // (X) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // AudioLink
            // (X) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // Dissolve
            // (X) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // ID Mask
            // (X) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // Encryption
            // (X) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // Outline

            toNiloToonMat.SetColor("_OutlineTintColor", fromlilToonMat.GetColor("_OutlineColor"));

            // _OutlineWidth (same, no need to convert)

            // make 2 outline system's width matching
            float _OutlineWidth = fromlilToonMat.GetFloat("_OutlineWidth");
            const float niloToonWidthRelativeTolilToon = 4;
            toNiloToonMat.SetFloat("_OutlineWidthExtraMultiplier", niloToonWidthRelativeTolilToon);

            // extra check to limit any unexpected large outline width
            float maxFinalWidth = 0.6f; // 0.6 is NiloToon's default outline width
            float finalWidth = Mathf.Min(maxFinalWidth, _OutlineWidth * niloToonWidthRelativeTolilToon);
            toNiloToonMat.SetFloat("_OutlineWidth", finalWidth / niloToonWidthRelativeTolilToon);

            Texture2D _OutlineWidthMask = fromlilToonMat.GetTexture("_OutlineWidthMask") as Texture2D;
            if (_OutlineWidthMask)
            {
                toNiloToonMat.SetFloat("_UseOutlineWidthTex", 1);
                toNiloToonMat.EnableKeyword("_OUTLINEWIDTHMAP");
                toNiloToonMat.SetVector("_OutlineWidthTexChannelMask", new Vector4(1, 0, 0, 0));
                toNiloToonMat.SetTexture("_OutlineWidthTex", _OutlineWidthMask);
            }
            else
            {
                toNiloToonMat.SetFloat("_UseOutlineWidthTex", 0);
                toNiloToonMat.DisableKeyword("_OUTLINEWIDTHMAP");
                toNiloToonMat.SetVector("_OutlineWidthTexChannelMask", new Vector4(0, 1, 0, 0));
            }

            float _OutlineVertexR2Width = fromlilToonMat.GetFloat("_OutlineVertexR2Width");
            if (_OutlineVertexR2Width == 0)
            {
                // no vertex color used
                toNiloToonMat.SetFloat("_UseOutlineWidthMaskFromVertexColor", 0);
                toNiloToonMat.SetVector("_OutlineWidthMaskFromVertexColor", new Vector4(0, 1, 0, 0));
            }

            if (_OutlineVertexR2Width == 1)
            {
                // vertex color.r
                toNiloToonMat.SetFloat("_UseOutlineWidthMaskFromVertexColor", 1);
                toNiloToonMat.SetVector("_OutlineWidthMaskFromVertexColor", new Vector4(1, 0, 0, 0));
            }

            if (_OutlineVertexR2Width == 2)
            {
                // vertex color.a
                toNiloToonMat.SetFloat("_UseOutlineWidthMaskFromVertexColor", 1);
                toNiloToonMat.SetVector("_OutlineWidthMaskFromVertexColor", new Vector4(0, 0, 0, 1));
            }

            toNiloToonMat.SetFloat("_OutlineBaseZOffset", fromlilToonMat.GetFloat("_OutlineZBias"));

            //----------------------------------------------------------------------------------------------------------------------
            // Tessellation
            // (X) ...All...

            //----------------------------------------------------------------------------------------------------------------------
            // For Multi

            //----------------------------------------------------------------------------------------------------------------------
            // Save (Unused)

            //----------------------------------------------------------------------------------------------------------------------
            // Advanced

            //----------------------------------------------------------------------------------------------------------------------
            // Outline Advanced
        }

        static void SetMaterialAsIsSkin(Material m)
        {
            m.SetFloat(IsSkin, 1);

            // no keyword on/off is needed for _IsSkin
            // ...
        }

        static void SetMaterialAsIsFace(Material m)
        {
            m.SetFloat(IsFace, 1);
            m.EnableKeyword("_ISFACE");
        }

        static void SetMaterialAsNoOutline(Material m)
        {
            // TODO: update
            if (m.GetFloat(SurfaceTypePreset) == 0)
            {
                m.SetFloat(SurfaceTypePreset, 1);
            }

            m.SetFloat(RenderOutline, 0);

            // no keyword on/off is needed for _RenderOutline
            // ...
        }

        static void SetMaterialNiloToonSurfaceTypeAndProperties(Material m,
            NiloToonSurfaceTypePreset niloToonSurfaceTypePreset)
        {
            // fix render queue when it is out of expected range
            void ClampRenderQueueMinMax(Material m, int minRenderQueue, int maxRenderQueue)
            {
                if (m.renderQueue < minRenderQueue) m.renderQueue = minRenderQueue;
                if (m.renderQueue > maxRenderQueue) m.renderQueue = maxRenderQueue;
            }

            void ClampRenderQueueToGroup(Material m, RenderQueueGroup renderQueueGroup)
            {
                // from Unity's RenderQueue enum
                //public enum RenderQueue
                //{
                //    /// This render queue is rendered before any others.
                //    Background = 1000,
                //    /// Opaque geometry uses this queue.
                //    Geometry = 2000,
                //    /// Alpha tested geometry uses this queue.
                //    AlphaTest = 2450,
                //    /// Last render queue that is considered "opaque".
                //    GeometryLast = 2500,
                //    /// This render queue is rendered after Geometry and AlphaTest, in back-to-front order.
                //    Transparent = 3000,
                //    /// This render queue is meant for overlay effects.
                //    Overlay = 4000,
                //}

                switch (renderQueueGroup)
                {
                    case RenderQueueGroup.Opaque:
                        ClampRenderQueueMinMax(m, 2000, 2450 - 1);
                        break;

                    case RenderQueueGroup.Cutout:
                        ClampRenderQueueMinMax(m, 2450, 2500 - 1);
                        break;

                    // 2500 is SkyBox

                    case RenderQueueGroup.Transparent:
                        ClampRenderQueueMinMax(m, 2501, 4000 - 1);
                        break;
                }
            }

            // hardcode matching NiloToonCharacter_SurfaceType_LWGUI_ShaderPropertyPreset.asset
            switch (niloToonSurfaceTypePreset)
            {
                case NiloToonSurfaceTypePreset.Opaque_Outline:
                    m.SetFloat(SurfaceTypePreset, (int)NiloToonSurfaceTypePreset.Opaque_Outline);
                    m.SetFloat("_SrcBlend", 1);
                    m.SetFloat("_DstBlend", 0);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 1);

                    //m.renderQueue = 2000; // matching preset, not -1
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Opaque);

                    m.SetFloat("_AlphaClip", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.Opaque:
                    m.SetFloat(SurfaceTypePreset, (int)NiloToonSurfaceTypePreset.Opaque);
                    m.SetFloat("_SrcBlend", 1);
                    m.SetFloat("_DstBlend", 0);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 0);

                    //m.renderQueue = 2000; // matching preset, not -1
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Opaque);

                    m.SetFloat("_AlphaClip", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.Transparent_ZWrite_Outline:
                    m.SetFloat(SurfaceTypePreset, (int)NiloToonSurfaceTypePreset.Transparent_ZWrite_Outline);
                    m.SetFloat("_SrcBlend", 5);
                    m.SetFloat("_DstBlend", 10);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 1);

                    //m.renderQueue = 2499;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Cutout);

                    m.SetFloat("_AlphaClip", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.Transparent:
                    m.SetFloat(SurfaceTypePreset, (int)NiloToonSurfaceTypePreset.Transparent);
                    m.SetFloat("_SrcBlend", 5);
                    m.SetFloat("_DstBlend", 10);
                    m.SetFloat("_ZWrite", 0);
                    m.SetFloat("_RenderOutline", 0);

                    //m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Transparent);

                    m.SetFloat("_AlphaClip", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.Transparent_ZWrite:
                    m.SetFloat(SurfaceTypePreset, (int)NiloToonSurfaceTypePreset.Transparent_ZWrite);
                    m.SetFloat("_SrcBlend", 5);
                    m.SetFloat("_DstBlend", 10);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 0);

                    //m.renderQueue = 2499;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Cutout);

                    m.SetFloat("_AlphaClip", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.CutoutOpaque_Outline:
                    m.SetFloat(SurfaceTypePreset, (int)NiloToonSurfaceTypePreset.CutoutOpaque_Outline);
                    m.SetFloat("_SrcBlend", 1);
                    m.SetFloat("_DstBlend", 0);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 1);

                    //m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Cutout);

                    m.SetFloat("_AlphaClip", 1);
                    m.EnableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.CutoutOpaque:
                    m.SetFloat(SurfaceTypePreset, (int)NiloToonSurfaceTypePreset.CutoutOpaque);
                    m.SetFloat("_SrcBlend", 1);
                    m.SetFloat("_DstBlend", 0);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 0);

                    //m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Cutout);

                    m.SetFloat("_AlphaClip", 1);
                    m.EnableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.CutoutTransparent_ZWrite_Outline:
                    m.SetFloat(SurfaceTypePreset, (int)NiloToonSurfaceTypePreset.CutoutTransparent_ZWrite_Outline);
                    m.SetFloat("_SrcBlend", 5);
                    m.SetFloat("_DstBlend", 10);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 1);

                    //m.renderQueue = 2499;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Cutout);

                    m.SetFloat("_AlphaClip", 1);
                    m.EnableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.CutoutTransparent:
                    m.SetFloat(SurfaceTypePreset, (int)NiloToonSurfaceTypePreset.CutoutTransparent);
                    m.SetFloat("_SrcBlend", 5);
                    m.SetFloat("_DstBlend", 10);
                    m.SetFloat("_ZWrite", 0);
                    m.SetFloat("_RenderOutline", 0);

                    //m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Transparent);

                    m.SetFloat("_AlphaClip", 1);
                    m.EnableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.CutoutTransparent_ZWrite:
                    m.SetFloat(SurfaceTypePreset, (int)NiloToonSurfaceTypePreset.CutoutTransparent_ZWrite);
                    m.SetFloat("_SrcBlend", 5);
                    m.SetFloat("_DstBlend", 10);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 0);

                    //m.renderQueue = 2499;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Cutout);

                    m.SetFloat("_AlphaClip", 1);
                    m.EnableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.TransparentQueueTransparent_ZWrite_Outline:
                    m.SetFloat(SurfaceTypePreset,
                        (int)NiloToonSurfaceTypePreset.TransparentQueueTransparent_ZWrite_Outline);
                    m.SetFloat("_SrcBlend", 5);
                    m.SetFloat("_DstBlend", 10);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 1);

                    //m.renderQueue = 3000;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Transparent);

                    m.SetFloat("_AlphaClip", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.TransparentQueueTransparent_ZWrite:
                    m.SetFloat(SurfaceTypePreset, (int)NiloToonSurfaceTypePreset.TransparentQueueTransparent_ZWrite);
                    m.SetFloat("_SrcBlend", 5);
                    m.SetFloat("_DstBlend", 10);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 0);

                    //m.renderQueue = 3000;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Transparent);

                    m.SetFloat("_AlphaClip", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.CutoutTransparentQueueTransparent_ZWrite_Outline:
                    m.SetFloat(SurfaceTypePreset,
                        (int)NiloToonSurfaceTypePreset.CutoutTransparentQueueTransparent_ZWrite_Outline);
                    m.SetFloat("_SrcBlend", 5);
                    m.SetFloat("_DstBlend", 10);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 1);

                    //m.renderQueue = 3000;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Transparent);

                    m.SetFloat("_AlphaClip", 1);
                    m.EnableKeyword("_ALPHATEST_ON");
                    break;
                case NiloToonSurfaceTypePreset.CutoutTransparentQueueTransparent_ZWrite:
                    m.SetFloat(SurfaceTypePreset,
                        (int)NiloToonSurfaceTypePreset.CutoutTransparentQueueTransparent_ZWrite);
                    m.SetFloat("_SrcBlend", 5);
                    m.SetFloat("_DstBlend", 10);
                    m.SetFloat("_ZWrite", 1);
                    m.SetFloat("_RenderOutline", 0);

                    //m.renderQueue = 3000;
                    ClampRenderQueueToGroup(m, RenderQueueGroup.Transparent);

                    m.SetFloat("_AlphaClip", 1);
                    m.EnableKeyword("_ALPHATEST_ON");
                    break;
                default:
                    throw new NotImplementedException("Contact NiloToon's developer to support a new SurfaceType.");
            }
        }

        static bool TextureHasAlpha(Texture2D texture)
        {
            // Blit texture to a new RT to avoid non-Readable texture problem
            RenderTexture tempRT = RenderTexture.GetTemporary(texture.width, texture.height);
            Graphics.Blit(texture, tempRT);
            RenderTexture RTactiveCache = RenderTexture.active;
            RenderTexture.active = tempRT;

            // apply RT to our new Texture2D, so we can read it's per pixel alpha
            Texture2D readableTexture = new Texture2D(texture.width, texture.height);
            readableTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = RTactiveCache;
            RenderTexture.ReleaseTemporary(tempRT);

            Color[] pixels = readableTexture.GetPixels();

            // this loop maybe slow depending on Texture size
            foreach (Color pixel in pixels)
            {
                if (pixel.a < 1f)
                {
                    return true;
                }
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // copy from UniversalRenderPipelineMaterialUpgrader.cs
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        static void GetShaderNamesToIgnore(ref HashSet<string> shadersToIgnore)
        {
            // all URP shader don't need an upgrade, since it is URP shader already!
            shadersToIgnore.Add("Universal Render Pipeline/Baked Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Particles/Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Particles/Simple Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Particles/Unlit");
            shadersToIgnore.Add("Universal Render Pipeline/Simple Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Complex Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Nature/SpeedTree7");
            shadersToIgnore.Add("Universal Render Pipeline/Nature/SpeedTree7 Billboard");
            shadersToIgnore.Add("Universal Render Pipeline/Nature/SpeedTree8");
            shadersToIgnore.Add("Universal Render Pipeline/2D/Sprite-Lit-Default");
            shadersToIgnore.Add("Universal Render Pipeline/Terrain/Lit");
            shadersToIgnore.Add("Universal Render Pipeline/Unlit");
            shadersToIgnore.Add("Sprites/Default");

            // NiloToonURP added all character shader to ignore list:
            shadersToIgnore.Add("Universal Render Pipeline/NiloToon/NiloToon_Character");
            shadersToIgnore.Add("Universal Render Pipeline/NiloToon/NiloToon_Character Sticker(Multiply)");
            shadersToIgnore.Add("Universal Render Pipeline/NiloToon/NiloToon_Character Sticker(Additive)");

            // lilToon shader will be upgraded by NiloToon's script
            shadersToIgnore.Add("lilToon");
            shadersToIgnore.Add("Hidden/lilToonOutline");
            shadersToIgnore.Add("Hidden/lilToonTransparent");
            shadersToIgnore.Add("Hidden/lilToonCutoutOutline");
            shadersToIgnore.Add("Hidden/lilToonTransparentOutline");

            // vrm mtoon will be upgraded by NiloToon's script
            shadersToIgnore.Add("UniGLTF/UniUnlit");
            shadersToIgnore.Add("VRM/MToon");
            shadersToIgnore.Add("VRM10/MToon10");
            shadersToIgnore.Add("VRM10/Universal Render Pipeline/MToon10");
        }

        static void GetUpgraders(ref List<MaterialUpgrader> upgraders)
        {
            /////////////////////////////////////
            //     Unity Standard Upgraders    //
            /////////////////////////////////////
            upgraders.Add(new StandardUpgrader("Standard"));
            upgraders.Add(new StandardUpgrader("Standard (Specular setup)"));

            ////////////////////////////////////
            // Particle Upgraders             //
            ////////////////////////////////////
            upgraders.Add(new ParticleUpgrader("Particles/Standard Surface"));
            upgraders.Add(new ParticleUpgrader("Particles/Standard Unlit"));
            upgraders.Add(new ParticleUpgrader("Particles/VertexLit Blended"));

            ////////////////////////////////////
            // Autodesk Interactive           //
            ////////////////////////////////////
            upgraders.Add(new AutodeskInteractiveUpgrader("Autodesk Interactive"));
        }

#if UNITY_EDITOR
        [MenuItem("Window/NiloToonURP/[Material] Convert Selected Materials to NiloToon", priority = 2)]
        [MenuItem("Assets/NiloToon/[Material] Convert Selected Materials to NiloToon", priority = 1100 + 2)]
        public static void ConvertSelectedMaterialsToNiloToon_Character()
        {
            var allselectedObjects = Selection.objects;
            List<Material> allInputMaterials = new List<Material>();

            foreach (var selectedObject in allselectedObjects)
            {
                if (selectedObject is Material mat)
                {
                    if (mat != null)
                    {
                        allInputMaterials.Add(mat);
                    }
                }
            }

            AutoConvertMaterialsToNiloToon(allInputMaterials);
        }

        [MenuItem("Window/NiloToonURP/[Material] Convert Selected Materials to NiloToon", priority = 2, validate = true)]
        [MenuItem("Assets/NiloToon/[Material] Convert Selected Materials to NiloToon", priority = 1100 + 2, validate = true)]
        public static bool ValidateConvertSelectedMaterialsToNiloToon_Character()
        {
            var allselectedObjects = Selection.objects;
            return allselectedObjects.Any(selectedObject => selectedObject is Material);
        }
#endif
    }
}