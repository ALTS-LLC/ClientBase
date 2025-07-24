using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Universal.ShaderGUI;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    // direct copy from URP10.5.1's LitShader.cs, with edit marked by [NiloToon] tag
    internal class NiloToonEnvironmentShaderGUI : BaseShaderGUI
    {
        private LitGUI.LitProperties litProperties;
        private LitDetailGUI.LitProperties litDetailProperties;
        private SavedBool m_DetailInputsFoldout;

        // NiloToon added:
        //=======================================================================
        private LitSplatGUI.LitProperties litSplatProperties;
        private LitScreenSpaceOutlineGUI.LitProperties litScreenSpaceOutlineProperties;
        private SavedBool m_SplatInputsFolder;
        private SavedBool m_ScreenSpaceOutlineInputsFolder;
        //=======================================================================

        // NiloToon added (to support URP 12):
        //=======================================================================
        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(LitDetailGUI.Styles.detailInputs, Expandable.Details, _ => LitDetailGUI.DoDetailArea(litDetailProperties, materialEditor));

            // NiloToon added:
            //=======================================================================
            materialScopesList.RegisterHeaderScope(LitSplatGUI.Styles.splatInputs, Expandable.Details, _ => LitSplatGUI.DoSplatArea(litSplatProperties, materialEditor));
            materialScopesList.RegisterHeaderScope(LitScreenSpaceOutlineGUI.Styles.outlineInputs, Expandable.Details, _ => LitScreenSpaceOutlineGUI.DoScreenSpaceOutlineArea(litScreenSpaceOutlineProperties, materialEditor));
            //=======================================================================
        }
        //=======================================================================

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new LitGUI.LitProperties(properties);
            litDetailProperties = new LitDetailGUI.LitProperties(properties);

            // NiloToon added:
            //=======================================================================
            litSplatProperties = new LitSplatGUI.LitProperties(properties);
            litScreenSpaceOutlineProperties = new LitScreenSpaceOutlineGUI.LitProperties(properties);
            //=======================================================================
        }

        // material changed check

        // NiloToon added:
        //=======================================================================
        public override void ValidateMaterial(Material material)
        {
            MaterialChange(material);
        }
        //=======================================================================

        public void MaterialChange(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, LitDetailGUI.SetMaterialKeywords);

            // NiloToon added:
            //=======================================================================
            LitSplatGUI.SetMaterialKeywords(material);
            LitScreenSpaceOutlineGUI.SetMaterialKeywords(material);
            //=======================================================================
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            if (litProperties.workflowMode != null)
            {
                DoPopup(LitGUI.Styles.workflowModeText, litProperties.workflowMode, Enum.GetNames(typeof(LitGUI.WorkflowMode)));
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendModeProp.targets)
                    MaterialChange((Material)obj);
            }
            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        // material main advanced options
        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(litProperties.highlights, LitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, LitGUI.Styles.reflectionsText);
                if (EditorGUI.EndChangeCheck())
                {
                    MaterialChange(material);
                }
            }

            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);

            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Specular);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
            else
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Metallic);
                Texture texture = material.GetTexture("_MetallicGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }

            MaterialChange(material);
        }
    }

    // NiloToonURP added:
    //=====================================================================================================================================
    // copy and edit of URP10.5.1's LitDetailGUI.cs, for Splat map section
    internal class LitSplatGUI
    {
        public static class Styles
        {
            public static readonly GUIContent splatMapOnOff = new GUIContent("On off Splat map",
                "");

            public static readonly GUIContent splatInputs = new GUIContent("Splat Inputs (experimental)(Only support URP10)",
                "These settings let you mix textures to the surface.");

            public static readonly GUIContent splatMaskText = new GUIContent("Splat RGBA Mask",
                "Select a RGBA mask for the Splat maps (please use sRGB = off for this texture)");

            public static readonly GUIContent splatMaskBlendingSoftnessText = new GUIContent("Blending softness",
                "Make Splat mask's blending sharper or softer");

            // R
            public static readonly GUIContent splatAlbedoMapRText = new GUIContent("Base Map (R)",
                "Select the texture containing the surface details. (smoothness data is in alpha)");
            public static readonly GUIContent splatNormalMapRText = new GUIContent("Normal Map (R)",
                "Select the texture containing the normal vector data.");
            public static readonly GUIContent splatAlbedoMapRTiling = new GUIContent("Tiling (R)",
                "UV tiling");
            public static readonly GUIContent splatNormalMapRIntensity = new GUIContent("Normal Intensity (R)",
                "normal map intensity for mask's R area");
            public static readonly GUIContent splatSmoothnessMultiplierR = new GUIContent("Smoothness (R)",
                "smoothness for mask's R area");
            public static readonly GUIContent splatPackedDataMapRText = new GUIContent("Packed Data Map (R)",
                "Select the texture containing the packed data. \n r = Metallic \n g = Occlusion \n b = Height \n a = Unused");
            public static readonly GUIContent splatHeightMultiplierRText = new GUIContent("Height Multiplier (R)",
                "affects height blending");

            // G
            public static readonly GUIContent splatAlbedoMapGText = new GUIContent("Base Map (G)",
                "Select the texture containing the surface details. (smoothness data is in alpha)");
            public static readonly GUIContent splatNormalMapGText = new GUIContent("Normal Map (G)",
                "Select the texture containing the normal vector data.");
            public static readonly GUIContent splatAlbedoMapGTiling = new GUIContent("Tiling (G)",
                "UV tiling");
            public static readonly GUIContent splatNormalMapGIntensity = new GUIContent("Normal Intensity (G)",
                "normal map intensity for mask's G area");
            public static readonly GUIContent splatSmoothnessMultiplierG = new GUIContent("Smoothness (G)",
                "smoothness for mask's G area");
            public static readonly GUIContent splatPackedDataMapGText = new GUIContent("Packed Data Map (G)",
                "Select the texture containing the packed data. \n r = Metallic \n g = Occlusion \n b = Height \n a = Unused");
            public static readonly GUIContent splatHeightMultiplierGText = new GUIContent("Height Multiplier (G)",
                "affects height blending");

            // B
            public static readonly GUIContent splatAlbedoMapBText = new GUIContent("Base Map (B)",
                "Select the texture containing the surface details. (smoothness data is in alpha)");
            public static readonly GUIContent splatNormalMapBText = new GUIContent("Normal Map (B)",
                "Select the texture containing the normal vector data.");
            public static readonly GUIContent splatAlbedoMapBTiling = new GUIContent("Tiling (B)",
                "UV tiling");
            public static readonly GUIContent splatNormalMapBIntensity = new GUIContent("Normal Intensity (B)",
                "normal map intensity for mask's B area");
            public static readonly GUIContent splatSmoothnessMultiplierB = new GUIContent("Smoothness (B)",
                "smoothness for mask's B area");
            public static readonly GUIContent splatPackedDataMapBText = new GUIContent("Packed Data Map (B)",
                "Select the texture containing the packed data. \n r = Metallic \n g = Occlusion \n b = Height \n a = Unused");
            public static readonly GUIContent splatHeightMultiplierBText = new GUIContent("Height Multiplier (B)",
                "affects height blending");

            // A
            public static readonly GUIContent splatAlbedoMapAText = new GUIContent("Base Map (A)",
                "Select the texture containing the surface details. (smoothness data is in alpha)");
            public static readonly GUIContent splatNormalMapAText = new GUIContent("Normal Map (A)",
                "Select the texture containing the normal vector data.");
            public static readonly GUIContent splatAlbedoMapATiling = new GUIContent("Tiling (A)",
                "UV tiling");
            public static readonly GUIContent splatNormalMapAIntensity = new GUIContent("Normal Intensity (A)",
                "normal map intensity for mask's A area");
            public static readonly GUIContent splatSmoothnessMultiplierA = new GUIContent("Smoothness (A)",
                "smoothness for mask's A area");
            public static readonly GUIContent splatPackedDataMapAText = new GUIContent("Packed Data Map (A)",
                "Select the texture containing the packed data. \n r = Metallic \n g = Occlusion \n b = Height \n a = Unused");
            public static readonly GUIContent splatHeightMultiplierAText = new GUIContent("Height Multiplier (A)",
                "affects height blending");
        }

        public struct LitProperties
        {
            public MaterialProperty splatMapFeatureOnOff;

            public MaterialProperty splatMaskMap;

            public MaterialProperty splatBlendingSoftness;

            ///////////////////////////////////////////////
            // R
            ///////////////////////////////////////////////
            // base map (albedo)
            public MaterialProperty splatAlbedoMapR;
            public MaterialProperty splatAlbedoMapRTintColor;
            public MaterialProperty splatAlbedoMapRTiling;
            // normal map
            public MaterialProperty splatNormalMapR;
            public MaterialProperty splatNormalMapIntensityR;
            // smoothness
            public MaterialProperty splatSmoothnessMultiplierR;
            // packed data map
            public MaterialProperty splatPackedDataMapR;
            public MaterialProperty splatHeightMultiplierR;

            ///////////////////////////////////////////////
            // G
            ///////////////////////////////////////////////
            // base map (albedo)
            public MaterialProperty splatAlbedoMapG;
            public MaterialProperty splatAlbedoMapGTintColor;
            public MaterialProperty splatAlbedoMapGTiling;
            // normal map
            public MaterialProperty splatNormalMapG;
            public MaterialProperty splatNormalMapIntensityG;
            // smoothness
            public MaterialProperty splatSmoothnessMultiplierG;
            // packed data map
            public MaterialProperty splatPackedDataMapG;
            public MaterialProperty splatHeightMultiplierG;

            ///////////////////////////////////////////////
            // B
            ///////////////////////////////////////////////
            // base map (albedo)
            public MaterialProperty splatAlbedoMapB;
            public MaterialProperty splatAlbedoMapBTintColor;
            public MaterialProperty splatAlbedoMapBTiling;
            // normal map
            public MaterialProperty splatNormalMapB;
            public MaterialProperty splatNormalMapIntensityB;
            // smoothness
            public MaterialProperty splatSmoothnessMultiplierB;
            // packed data map
            public MaterialProperty splatPackedDataMapB;
            public MaterialProperty splatHeightMultiplierB;

            ///////////////////////////////////////////////
            // A
            ///////////////////////////////////////////////
            // base map (albedo)
            public MaterialProperty splatAlbedoMapA;
            public MaterialProperty splatAlbedoMapATintColor;
            public MaterialProperty splatAlbedoMapATiling;
            // normal map
            public MaterialProperty splatNormalMapA;
            public MaterialProperty splatNormalMapIntensityA;
            // smoothness
            public MaterialProperty splatSmoothnessMultiplierA;
            // packed data map
            public MaterialProperty splatPackedDataMapA;
            public MaterialProperty splatHeightMultiplierA;

            public LitProperties(MaterialProperty[] properties)
            {
                splatMapFeatureOnOff = BaseShaderGUI.FindProperty("_SplatMapFeatureOnOff", properties, false);

                splatMaskMap = BaseShaderGUI.FindProperty("_SplatMaskMap", properties, false);

                splatBlendingSoftness = BaseShaderGUI.FindProperty("_SplatMaskBlendingSoftness", properties, false);

                // R
                splatAlbedoMapR = BaseShaderGUI.FindProperty("_SplatAlbedoMapR", properties, false);
                splatAlbedoMapRTintColor = BaseShaderGUI.FindProperty("_SplatAlbedoMapRTintColor", properties, false);
                splatAlbedoMapRTiling = BaseShaderGUI.FindProperty("_SplatAlbedoMapRTiling", properties, false);
                splatNormalMapR = BaseShaderGUI.FindProperty("_SplatNormalMapR", properties, false);
                splatNormalMapIntensityR = BaseShaderGUI.FindProperty("_SplatNormalMapIntensityR", properties, false);
                splatSmoothnessMultiplierR = BaseShaderGUI.FindProperty("_SplatSmoothnessMultiplierR", properties, false);
                splatPackedDataMapR = BaseShaderGUI.FindProperty("_SplatPackedDataMapR", properties, false);
                splatHeightMultiplierR = BaseShaderGUI.FindProperty("_SplatHeightMultiplierR", properties, false);

                // G
                splatAlbedoMapG = BaseShaderGUI.FindProperty("_SplatAlbedoMapG", properties, false);
                splatAlbedoMapGTintColor = BaseShaderGUI.FindProperty("_SplatAlbedoMapGTintColor", properties, false);
                splatAlbedoMapGTiling = BaseShaderGUI.FindProperty("_SplatAlbedoMapGTiling", properties, false);
                splatNormalMapG = BaseShaderGUI.FindProperty("_SplatNormalMapG", properties, false);
                splatNormalMapIntensityG = BaseShaderGUI.FindProperty("_SplatNormalMapIntensityG", properties, false);
                splatSmoothnessMultiplierG = BaseShaderGUI.FindProperty("_SplatSmoothnessMultiplierG", properties, false);
                splatPackedDataMapG = BaseShaderGUI.FindProperty("_SplatPackedDataMapG", properties, false);
                splatHeightMultiplierG = BaseShaderGUI.FindProperty("_SplatHeightMultiplierG", properties, false);

                // B
                splatAlbedoMapB = BaseShaderGUI.FindProperty("_SplatAlbedoMapB", properties, false);
                splatAlbedoMapBTintColor = BaseShaderGUI.FindProperty("_SplatAlbedoMapBTintColor", properties, false);
                splatAlbedoMapBTiling = BaseShaderGUI.FindProperty("_SplatAlbedoMapBTiling", properties, false);
                splatNormalMapB = BaseShaderGUI.FindProperty("_SplatNormalMapB", properties, false);
                splatNormalMapIntensityB = BaseShaderGUI.FindProperty("_SplatNormalMapIntensityB", properties, false);
                splatSmoothnessMultiplierB = BaseShaderGUI.FindProperty("_SplatSmoothnessMultiplierB", properties, false);
                splatPackedDataMapB = BaseShaderGUI.FindProperty("_SplatPackedDataMapB", properties, false);
                splatHeightMultiplierB = BaseShaderGUI.FindProperty("_SplatHeightMultiplierB", properties, false);

                // A
                splatAlbedoMapA = BaseShaderGUI.FindProperty("_SplatAlbedoMapA", properties, false);
                splatAlbedoMapATintColor = BaseShaderGUI.FindProperty("_SplatAlbedoMapATintColor", properties, false);
                splatAlbedoMapATiling = BaseShaderGUI.FindProperty("_SplatAlbedoMapATiling", properties, false);
                splatNormalMapA = BaseShaderGUI.FindProperty("_SplatNormalMapA", properties, false);
                splatNormalMapIntensityA = BaseShaderGUI.FindProperty("_SplatNormalMapIntensityA", properties, false);
                splatSmoothnessMultiplierA = BaseShaderGUI.FindProperty("_SplatSmoothnessMultiplierA", properties, false);
                splatPackedDataMapA = BaseShaderGUI.FindProperty("_SplatPackedDataMapA", properties, false);
                splatHeightMultiplierA = BaseShaderGUI.FindProperty("_SplatHeightMultiplierA", properties, false);
            }
        }

        public static void DoSplatArea(LitProperties properties, MaterialEditor materialEditor)
        {
            void DrawFloat01Slider(MaterialProperty property, GUIContent name)
            {
                // on off (slider)
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = property.hasMixedValue;
                var value = EditorGUILayout.Slider(name, property.floatValue, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                    property.floatValue = value;
                EditorGUI.showMixedValue = false;
            }

            // on off (slider)
            DrawFloat01Slider(properties.splatMapFeatureOnOff, Styles.splatMapOnOff);

            // mask
            materialEditor.TexturePropertySingleLine(Styles.splatMaskText, properties.splatMaskMap);

            // blending softness (slider)
            DrawFloat01Slider(properties.splatBlendingSoftness, Styles.splatMaskBlendingSoftnessText);

            EditorGUILayout.Space(20);

            // (R)
            EditorGUILayout.HelpBox("R", MessageType.None, true);
            materialEditor.TexturePropertyWithHDRColor(Styles.splatAlbedoMapRText, properties.splatAlbedoMapR, properties.splatAlbedoMapRTintColor, false);
            materialEditor.TexturePropertySingleLine(Styles.splatNormalMapRText, properties.splatNormalMapR);
            materialEditor.TexturePropertySingleLine(Styles.splatPackedDataMapRText, properties.splatPackedDataMapR);
            materialEditor.FloatProperty(properties.splatAlbedoMapRTiling, Styles.splatAlbedoMapRTiling.text);
            materialEditor.FloatProperty(properties.splatNormalMapIntensityR, Styles.splatNormalMapRIntensity.text);
            DrawFloat01Slider(properties.splatSmoothnessMultiplierR, Styles.splatSmoothnessMultiplierR);
            materialEditor.FloatProperty(properties.splatHeightMultiplierR, Styles.splatHeightMultiplierRText.text);
            EditorGUILayout.Space(20);

            // (G)
            EditorGUILayout.HelpBox("G", MessageType.None, true);
            materialEditor.TexturePropertyWithHDRColor(Styles.splatAlbedoMapGText, properties.splatAlbedoMapG, properties.splatAlbedoMapGTintColor, false);
            materialEditor.TexturePropertySingleLine(Styles.splatNormalMapGText, properties.splatNormalMapG);
            materialEditor.TexturePropertySingleLine(Styles.splatPackedDataMapGText, properties.splatPackedDataMapG);
            materialEditor.FloatProperty(properties.splatAlbedoMapGTiling, Styles.splatAlbedoMapGTiling.text);
            materialEditor.FloatProperty(properties.splatNormalMapIntensityG, Styles.splatNormalMapGIntensity.text);
            DrawFloat01Slider(properties.splatSmoothnessMultiplierG, Styles.splatSmoothnessMultiplierG);
            materialEditor.FloatProperty(properties.splatHeightMultiplierG, Styles.splatHeightMultiplierGText.text);
            EditorGUILayout.Space(20);

            // (B)
            EditorGUILayout.HelpBox("B", MessageType.None, true);
            materialEditor.TexturePropertyWithHDRColor(Styles.splatAlbedoMapBText, properties.splatAlbedoMapB, properties.splatAlbedoMapBTintColor, false);
            materialEditor.TexturePropertySingleLine(Styles.splatNormalMapBText, properties.splatNormalMapB);
            materialEditor.TexturePropertySingleLine(Styles.splatPackedDataMapBText, properties.splatPackedDataMapB);
            materialEditor.FloatProperty(properties.splatAlbedoMapBTiling, Styles.splatAlbedoMapBTiling.text);
            materialEditor.FloatProperty(properties.splatNormalMapIntensityB, Styles.splatNormalMapBIntensity.text);
            DrawFloat01Slider(properties.splatSmoothnessMultiplierB, Styles.splatSmoothnessMultiplierB);
            materialEditor.FloatProperty(properties.splatHeightMultiplierB, Styles.splatHeightMultiplierBText.text);
            EditorGUILayout.Space(20);

            // (A)
            EditorGUILayout.HelpBox("A", MessageType.None, true);
            materialEditor.TexturePropertyWithHDRColor(Styles.splatAlbedoMapAText, properties.splatAlbedoMapA, properties.splatAlbedoMapATintColor, false);
            materialEditor.TexturePropertySingleLine(Styles.splatNormalMapAText, properties.splatNormalMapA);
            materialEditor.TexturePropertySingleLine(Styles.splatPackedDataMapAText, properties.splatPackedDataMapA);
            materialEditor.FloatProperty(properties.splatAlbedoMapATiling, Styles.splatAlbedoMapATiling.text);
            materialEditor.FloatProperty(properties.splatNormalMapIntensityA, Styles.splatNormalMapAIntensity.text);
            DrawFloat01Slider(properties.splatSmoothnessMultiplierA, Styles.splatSmoothnessMultiplierA);
            materialEditor.FloatProperty(properties.splatHeightMultiplierA, Styles.splatHeightMultiplierAText.text);

            // code references
            /*
            if (properties.detailAlbedoMapScale.floatValue != 1.0f)
            {
                EditorGUILayout.HelpBox(Styles.detailAlbedoMapScaleInfo.text, MessageType.Info, true);
            }
            
            materialEditor.TextureScaleOffsetProperty(properties.detailAlbedoMap);
            */
        }

        public static void SetMaterialKeywords(Material material)
        {
            if (material.HasProperty("_SplatMapFeatureOnOff"))
            {
                CoreUtils.SetKeyword(material, "_SPLATMAP", material.GetFloat("_SplatMapFeatureOnOff") > 0f);
            }
        }
    }

    internal class LitScreenSpaceOutlineGUI
    {
        public static class Styles
        {
            public static readonly GUIContent outlineInputs = new GUIContent("Outline Inputs",
                "These settings let you add screen space outline to the surface.");

            public static readonly GUIContent outlineIntensity = new GUIContent("Outline Intensity",
                "");
            public static readonly GUIContent outlineColor = new GUIContent("Outline Color",
                "");
            public static readonly GUIContent outlineWidth = new GUIContent("Outline Width",
                "");
        }

        public struct LitProperties
        {
            public MaterialProperty outlineIntensity;

            public MaterialProperty outlineColor;
            public MaterialProperty outlineWidth;

            public LitProperties(MaterialProperty[] properties)
            {
                outlineIntensity = BaseShaderGUI.FindProperty("_ScreenSpaceOutlineIntensity", properties, false);
                outlineColor = BaseShaderGUI.FindProperty("_ScreenSpaceOutlineColor", properties, false);
                outlineWidth = BaseShaderGUI.FindProperty("_ScreenSpaceOutlineWidth", properties, false);
            }
        }

        public static void DoScreenSpaceOutlineArea(LitProperties properties, MaterialEditor materialEditor)
        {
            //TODO: extract all DrawFloat01Slider to a utility file
            void DrawFloat01Slider(MaterialProperty property, GUIContent name)
            {
                // on off (slider)
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = property.hasMixedValue;
                var value = EditorGUILayout.Slider(name, property.floatValue, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                    property.floatValue = value;
                EditorGUI.showMixedValue = false;
            }

            // on off (slider)
            DrawFloat01Slider(properties.outlineIntensity, Styles.outlineIntensity);

            // outline settings
            materialEditor.ColorProperty(properties.outlineColor, Styles.outlineColor.text);
            materialEditor.FloatProperty(properties.outlineWidth, Styles.outlineWidth.text);
        }

        public static void SetMaterialKeywords(Material material)
        {
            /*
            if (material.HasProperty("_SplatMapFeatureOnOff"))
            {
                CoreUtils.SetKeyword(material, "_SPLATMAP", material.GetFloat("_SplatMapFeatureOnOff") > 0f);
            }
            */
        }
    }
    //=====================================================================================================================================

    // direct copy from URP10.5.1's LitDetailGUI.cs (no edit)
    internal class LitDetailGUI
    {
        public static class Styles
        {
            public static readonly GUIContent detailInputs = new GUIContent("Detail Inputs",
                "These settings let you add details to the surface.");

            public static readonly GUIContent detailMaskText = new GUIContent("Mask",
                "Select a mask for the Detail maps. The mask uses the alpha channel of the selected texture. The __Tiling__ and __Offset__ settings have no effect on the mask.");

            public static readonly GUIContent detailAlbedoMapText = new GUIContent("Base Map",
                "Select the texture containing the surface details.");

            public static readonly GUIContent detailNormalMapText = new GUIContent("Normal Map",
                "Select the texture containing the normal vector data.");

            public static readonly GUIContent detailAlbedoMapScaleInfo = new GUIContent("Setting the scaling factor to a value other than 1 results in a less performant shader variant.");
        }

        public struct LitProperties
        {
            public MaterialProperty detailMask;
            public MaterialProperty detailAlbedoMapScale;
            public MaterialProperty detailAlbedoMap;
            public MaterialProperty detailNormalMapScale;
            public MaterialProperty detailNormalMap;

            public LitProperties(MaterialProperty[] properties)
            {
                detailMask = BaseShaderGUI.FindProperty("_DetailMask", properties, false);
                detailAlbedoMapScale = BaseShaderGUI.FindProperty("_DetailAlbedoMapScale", properties, false);
                detailAlbedoMap = BaseShaderGUI.FindProperty("_DetailAlbedoMap", properties, false);
                detailNormalMapScale = BaseShaderGUI.FindProperty("_DetailNormalMapScale", properties, false);
                detailNormalMap = BaseShaderGUI.FindProperty("_DetailNormalMap", properties, false);
            }
        }

        public static void DoDetailArea(LitProperties properties, MaterialEditor materialEditor)
        {
            materialEditor.TexturePropertySingleLine(Styles.detailMaskText, properties.detailMask);
            materialEditor.TexturePropertySingleLine(Styles.detailAlbedoMapText, properties.detailAlbedoMap,
                properties.detailAlbedoMap.textureValue != null ? properties.detailAlbedoMapScale : null);
            if (properties.detailAlbedoMapScale.floatValue != 1.0f)
            {
                EditorGUILayout.HelpBox(Styles.detailAlbedoMapScaleInfo.text, MessageType.Info, true);
            }
            materialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, properties.detailNormalMap,
                properties.detailNormalMap.textureValue != null ? properties.detailNormalMapScale : null);
            materialEditor.TextureScaleOffsetProperty(properties.detailAlbedoMap);
        }

        public static void SetMaterialKeywords(Material material)
        {
            if (material.HasProperty("_DetailAlbedoMap") && material.HasProperty("_DetailNormalMap") && material.HasProperty("_DetailAlbedoMapScale"))
            {
                bool isScaled = material.GetFloat("_DetailAlbedoMapScale") != 1.0f;
                bool hasDetailMap = material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap");
                CoreUtils.SetKeyword(material, "_DETAIL_MULX2", !isScaled && hasDetailMap);
                CoreUtils.SetKeyword(material, "_DETAIL_SCALED", isScaled && hasDetailMap);
            }
        }
    }

    // direct copy from URP10.5.1's SavedParameter.cs (no edit)
    class SavedParameter<T>
        where T : IEquatable<T>
    {
        internal delegate void SetParameter(string key, T value);
        internal delegate T GetParameter(string key, T defaultValue);

        readonly string m_Key;
        bool m_Loaded;
        T m_Value;

        readonly SetParameter m_Setter;
        readonly GetParameter m_Getter;

        public SavedParameter(string key, T value, GetParameter getter, SetParameter setter)
        {
            Assert.IsNotNull(setter);
            Assert.IsNotNull(getter);

            m_Key = key;
            m_Loaded = false;
            m_Value = value;
            m_Setter = setter;
            m_Getter = getter;
        }

        void Load()
        {
            if (m_Loaded)
                return;

            m_Loaded = true;
            m_Value = m_Getter(m_Key, m_Value);
        }

        public T value
        {
            get
            {
                Load();
                return m_Value;
            }
            set
            {
                Load();

                if (m_Value.Equals(value))
                    return;

                m_Value = value;
                m_Setter(m_Key, value);
            }
        }
    }

    // direct copy from URP10.5.1's SavedParameter.cs (no edit)
    sealed class SavedBool : SavedParameter<bool>
    {
        public SavedBool(string key, bool value)
            : base(key, value, EditorPrefs.GetBool, EditorPrefs.SetBool) { }
    }

    // direct copy from URP10.5.1's CoreUtil.cs's SetKeyword(...) (no edit)
    public static class CoreUtils
    {
        public static void SetKeyword(Material material, string keyword, bool state)
        {
            if (state)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }
    }
}
