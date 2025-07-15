using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace NiloToon.NiloToonURP
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class NiloToonPerCharacterRenderController : MonoBehaviour
    {
        static readonly string PlayerPrefsKey_NiloToonNeedPreserveEditorPlayModeMaterialChange = "NiloToonPreserveEditorPlayModeMaterialChange";

#if UNITY_EDITOR
        public static bool GetNiloToonNeedPreserveEditorPlayModeMaterialChange_EditorOnly()
        {
            return PlayerPrefs.GetInt(PlayerPrefsKey_NiloToonNeedPreserveEditorPlayModeMaterialChange) == 1;
        }
        public static void SetNiloToonNeedPreserveEditorPlayModeMaterialChange_EditorOnly(bool preserveEditorPlayModeMaterialChange)
        {
            PlayerPrefs.SetInt(PlayerPrefsKey_NiloToonNeedPreserveEditorPlayModeMaterialChange, preserveEditorPlayModeMaterialChange ? 1 : 0);
        }
#endif
        public enum RefillRenderersMode
        {
            Always,
            EditModeOnly,
            APIOnly
        }
        public enum RefillRenderersLogMode
        {
            Always,
            AlwaysEditorOnly,
            EditModeOnly,
            Never
        }
        public enum TransformDirection
        {
            X,
            Y,
            Z,
            negX,
            negY,
            negZ
        }
        public enum ExtraThickOutlineZWriteMode
        {
            Auto,
            On,
            Off
        }

        public enum DissolveMode
        {
            Default = 0,
            UV1 = 1,
            UV2 = 2,
            WorldSpaceNoise = 3,
            WorldSpaceVerticalUpward = 4,
            WorldSpaceVerticalDownward = 5,
            ScreenSpaceNoise = 6,
            ScreenSpaceVerticalUpward = 7,
            ScreenSpaceVerticalDownward = 8,
        }

        public enum UVOption
        {
            UV0 = 0,
            UV1 = 1,
            UV2 = 2,
            UV3 = 3,
            MatCapUV = 4,
            CharBoundUV = 5,
            ScreenSpaceUV = 6,
        }

        public enum ScreenSpaceUVOption
        {
            CharBoundUV = 5,
            ScreenSpaceUV = 6,
        }

        public enum BaseMapOverrideBlendMode
        {
            // 0 is not allowed to use, since it will edit alpha -> Normal(RGBA)
            Normal = 1,
            Add = 2,
            Screen = 3,
            Multiply = 4,
            None = 5,
        }

        public enum MaterialInstanceMode
        {
            PlayModeOnly,
            NeverGenerate,
        }
        //------------------------------------------------------------
        [Tooltip("Set to false if you want to stop the rendering of this character.\n\n" +
                 "Useful when you just want to stop the rendering, but not disabling the character's game object or renderer.")]
        public bool renderCharacter = true;
        //------------------------------------------------------------
        [Tooltip("Set to NeverGenerate if you don't want NiloToon to generate material instance in play mode or build, then all renderers will use the project's material directly without any cloning.\n\n" +
                 "When it is set to NeverGenerate, you will have these pros and cons:\n\n" +
                 "[Disadvantages]\n" +
                 "- Can't SRP Batch\n" +
                 "- Can't use features with (PlayMode) suffix\n\n" +
                 "[Possible Advantages]\n" +
                 "- Any material edit will be preserved in PlayMode\n" +
                 "- Likely less conflicts with other assets since there are no material instance")]
        public MaterialInstanceMode materialInstanceMode = MaterialInstanceMode.PlayModeOnly;
        //------------------------------------------------------------
        [Foldout("Character Renderers")]

        [Tooltip(
            "When any child transform structure changes (OnTransformChildrenChanged() is triggered), should this script discard allRenderers list and auto refill it?\n\n" +
            "- Always: Auto refill in PlayMode and EditMode (*slow in PlayMode)\n" +
            "- Edit Mode Only: Auto refill in EditMode only\n" +
            "- API Only: Never perform Auto refill due to child transform structure changes (OnTransformChildrenChanged() is triggered). Only perform Auto refill once when user calls API -> RefillAllRenderers()\n\n" +
            "Name: autoRefillRenderersMode\n" +
            "Default: Edit Mode Only")]
        [OverrideDisplayName("Auto Refill Mode")]
        public RefillRenderersMode autoRefillRenderersMode = RefillRenderersMode.EditModeOnly;

        [Tooltip(
           "Whenever this script discarded allRenderers list and auto refill it, should NiloToon output a Debug.Log() also?\n\n" +
           "- Always: Log in PlayMode and EditMode (*slow in PlayMode)\n" +
           "- Always Editor Only: if it is UnityEditor, Log in PlayMode and EditMode\n" +
           "- Edit Mode Only: Log in EditMode only\n" +
           "- Never: Never Log\n\n" +
           "Name: autoRefillRenderersLogMode\n" +
           "Default: Never")]
        [OverrideDisplayName("Log Mode")]
        public RefillRenderersLogMode autoRefillRenderersLogMode = RefillRenderersLogMode.Never;

        [Tooltip(
            "This script will constantly set material render settings to all materials found in this list via:\n" +
            "- Renderer.GetMaterials(play mode) or\n" +
            "- Renderer.SetPropertyBlock(edit mode).\n\n" +
            "If you want NiloToon to discard and auto refill this list correctly, you can pick one of the following:\n" +
            "- click \"Auto Setup this character\" button\n" +
            "- click \"Auto refill AllRenderers list\" button\n" +
            "- set the list count to 0.\n\n" +
            "* In most situations, you don't need to edit this list manually, as NiloToon will try to auto fill in this list for you.\n\n" +
            "Name: allRenderers\n" +
            "Default: Empty List")]
        public List<Renderer> allRenderers = new();
        //------------------------------------------------------------
        [Foldout("Attachment Renderers")]

        [Tooltip(
            "As an extension list to the above Character Renderers list, but only for attachment renderers that are NOT part of the character prefab and do not have a NiloToonPerCharacterRenderController script attached on them.\n" +
            "(e.g. weapon/microphone/any item/extra cloth/extra accessory...renderer)\n" +
            "You can assign attachment renderers here, which will make these attachment renderers render using this script's settings also.\n\n" +
            "Name: attachmentRendererList\n" +
            "Default: Empty List")]
        public List<Renderer> attachmentRendererList = new();
        //------------------------------------------------------------
        [Foldout("NiloToonRendererRedrawer")]

        [Tooltip(
            "A collection of all child NiloToonRendererRedrawer(s).\n" +
            "(e.g. usually for extra re-render of hair/eye using a special material(e.g. special ZOffset & Stencil in material))\n" +
            "* In most situations, you don't need to edit this list manually, this list will be automatically filled by any enabled child NiloToonRendererRedrawer.\n\n" +
            "Name: nilotoonRendererRedrawerList\n" +
            "Default: Empty List")]
        public List<NiloToonRendererRedrawer> nilotoonRendererRedrawerList = new();
        //------------------------------------------------------------
        [Foldout("Head & Face")]

        [Tooltip(
            "You should assign character's head bone transform (or any transform that is similar to head bone) here, it will affect face lighting/perspective removal.\n\n" +
            "Click \"Auto Setup this character\" button may fill this correctly.\n" +
            "If your model has a \"head\" bone with name \"head\", usually clicking \"Auto Setup this character\" button will assign it for you already, so you don't need to edit this.\n\n" +
            "Name: headBoneTransform\n" +
            "Default: Empty")]
        [OverrideDisplayName("Head")]
        public Transform headBoneTransform;

        [Tooltip(
            "Select a direction which makes scene view's BLUE gizmo arrow from head bone points to face's FORWARD direction.\n\n" +
            "Name: faceForwardDirection\n" +
            "Default: Z")]
        [OverrideDisplayName("Face Forward")]
        public TransformDirection faceForwardDirection = TransformDirection.Z;

        [Tooltip(
            "Enable this toggle to show 'Face Forward' BLUE gizmo arrow in scene view.\n\n" +
            "Name: showFaceForwardDirectionGizmo\n" +
            "Default: On")]
        [OverrideDisplayName("    Show Gizmo?")]
        public bool showFaceForwardDirectionGizmo = true;

        [Tooltip(
            "Select a direction which makes scene view's GREEN gizmo arrow from head bone points to face's UPWARD direction.\n\n" +
            "Name: faceUpDirection\n" +
            "Default: Y")]
        [OverrideDisplayName("Face Up")]
        public TransformDirection faceUpDirection = TransformDirection.Y;

        [Tooltip(
            "Enable this toggle to show 'Face Up' GREEN gizmo arrow in scene view.\n\n" +
            "Name: showFaceUpDirectionGizmo\n" +
            "Default: On")]
        [OverrideDisplayName("    Show Gizmo?")]
        public bool showFaceUpDirectionGizmo = true;

        [Tooltip(
            "Controls the amount(strength) of face normal edit by NiloToon. If headBoneTransform is none, this will treat as 0 (not edit face normal).\n" +
            "The face normal edit method is defined in the 'Method' slider below.\n" +
            "You can open NiloToon's Debug window, turn on debug 'Normal', and see how the face normals are edited.\n\n" +
            "Name: faceNormalFixAmount\n" +
            "Default: 1")]
        [RangeOverrideDisplayName("Face Normal fix",0, 1)]
        public float faceNormalFixAmount = 1;

        [Tooltip(
            "When set to 0, will use 'flatten face normal' method, which force all face normal's direction points to face forward direction.\n\n" +
            "When set to 1, will use 'proxy sphere face normal' method, where headBoneTransform's position will be proxy sphere's center, and the new normal direction will be a vector from headBoneTransform's position to vertex position.\n\n" +
            "Default is a mix of both methods.\n\n" + 
            "Name: fixFaceNormalUseFlattenOrProxySphereMethod\n" +
            "Default: 0.75")]
        [RangeOverrideDisplayName("    Method",0, 1)]
        public float fixFaceNormalUseFlattenOrProxySphereMethod = 0.75f;
        //------------------------------------------------------------
        [Foldout("Bounding Sphere")]

        [Tooltip(
            "If enabled, NiloToon will use 'Bound Pos' below as this character's custom bounding sphere center position.\n\n" +
            "If disabled, NiloToon will calculate Bounding sphere Center position per frame, which is the center of all bounding box from all renderers.\n\n" +
            "The result Bounding sphere Center position will affect NiloToon's average shadow's rendering and NiloToon's shadowmap culling and rendering.\n\n" +
            "Name: useCustomCharacterBoundCenter\n" +
            "Default: On")]
        [OverrideDisplayName("Custom Bound Pos?")]
        public bool useCustomCharacterBoundCenter = true;

        [Tooltip(
            "If 'Custom Bound Pos?' is true, you should assign hip / Pelvis bone (any transform that is center of character) into this slot.\n" +
            "Useful if the per frame calculated bounding sphere from all renderers is not working for your character, or realtime bounding sphere calculation is too slow.\n\n" +
            "Name: customCharacterBoundCenter\n" +
            "Default: None")]
        [OverrideDisplayName("    Bound Pos")]
        public Transform customCharacterBoundCenter;

        [Tooltip(
            "Set a number that is as small as possible, but still be able to always contain the whole character.\n" +
            "1.25m is usually a safe enough radius for any character < 2m height, if the red gizmo sphere is not big enough to contain the character, increase this radius until it does.\n\n" +
            "Name: characterBoundRadius\n" +
            "Default: 1.25m")]
        [OverrideDisplayName("Bound Radius")]
        public float characterBoundRadius = 1.25f;
       
        [Tooltip(
            "Enable to show a RED gizmo sphere that defines the bounding sphere of character (this red gizmo sphere is defined by all three settings above), please make sure the sphere is big enough to contain the whole character.\n\n" +
            "Name: showBoundingSphereGizmo\n" +
            "Default: Off")]
        [OverrideDisplayName("Show Gizmo?")]
        public bool showBoundingSphereGizmo = false;
        //------------------------------------------------------------
        [Foldout("Base Map")]

        [Tooltip(
            "Controls the brightness of Base Map. (Won't affect rim light and Specular highlight)\n\n" +
            "Name: perCharacterBaseColorMultiply\n" +
            "Default: 1")]
        [OverrideDisplayName("Brightness")]
        public float perCharacterBaseColorMultiply = 1;

        [Tooltip(
            "Controls the tint color of Base Map. (Won't affect rim light and Specular highlight)\n\n" +
            "Name: perCharacterBaseColorTint\n" +
            "Default: White")]
        [ColorUsageOverrideDisplayName("Tint Color",false, true)]
        public Color perCharacterBaseColorTint = Color.white;
        //------------------------------------------------------------
        [Foldout("Classic Outline")]

        [Tooltip(
            "Enable character's self outline?\n\n" +
            "Name: shouldRenderSelfOutline\n" +
            "Default: On")]
        [OverrideDisplayName("Enable?")]
        public bool shouldRenderSelfOutline = true;

        [Tooltip(
            "Controls the width of self outline.\n\n" +
            "Name: perCharacterOutlineWidthMultiply\n" +
            "Default: 1")]
        [OverrideDisplayName("    Width")]
        public float perCharacterOutlineWidthMultiply = 1;

        [Tooltip(
            "Controls the Tint Color of self outline.\n\n" +
            "Name: perCharacterOutlineColorTint\n" +
            "Default: White")]
        [ColorUsageOverrideDisplayName("    Tint Color",false, true)]
        public Color perCharacterOutlineColorTint = Color.white;

        [Tooltip(
            "Controls the Tint Color of self outline. (extra control)\n\n" +
            "Name: perCharacterOutlineEffectTintColor\n" +
            "Default: White")]
        [ColorUsageOverrideDisplayName("    Tint Color (extra)", false, true)]
        public Color perCharacterOutlineEffectTintColor = new Color(1, 1, 1);

        [Tooltip(
            "Controls the replace color of self outline.\n" +
            "Alpha controls this effect's strength, if you want to enable this effect, you need to increase this color's alpha.\n\n" +
            "Name: perCharacterOutlineEffectTintColor\n" +
            "Default: White with 0 alpha (1,1,1,0)")]
        [ColorUsageOverrideDisplayName("    Replace by Color", true, true)]
        public Color perCharacterOutlineEffectLerpColor = new Color(1, 1, 1, 0);
        //------------------------------------------------------------
        [Foldout("Perspective removal")]

        [Tooltip(
            "Increase to override character's projection matrix in vertex shader.\n" +
            "If you set this to 1, character's vertices around 'Head' will be rendered just like viewing by an orthographic camera.\n" +
            "Useful when camera FOV is high, and character is close to camera, where the head 3D shape is distorted and looks ugly.\n\n" +
            "Name: perspectiveRemovalAmount\n" +
            "Default: 0")]
        [RangeOverrideDisplayName("Strength",0, 1)]
        public float perspectiveRemovalAmount = 0;

        [Header("    Head Sphere Mask")]
        [Tooltip(
            "A sphere mask located at head's position that will fadeout 'Perspective removal' according to distance from head's position to vertex position.\n\n" +
            "Name: perspectiveRemovalRadius\n" +
            "Default: 1")]
        [OverrideDisplayName("    Radius")]
        public float perspectiveRemovalRadius = 1;

        [Header("    World Height Mask")]
        [Tooltip(
            "At this world space height, 'Perspective removal' will start to fadeout. It is usually set to 1m above ground's world space height for a 1.6m character. The purpose of 'World Height Mask' is to ensure character's foot is always on ground perfectly.\n\n" +
            "Name: perspectiveRemovalEndHeight\n" +
            "Default: 1")]
        [OverrideDisplayName("    Start height")]
        public float perspectiveRemovalEndHeight = 1;
        [Tooltip(
            "At this world space height, 'Perspective removal' will fadeout completely. It is usually set to the ground's world space height. The purpose of 'World Height Mask' is to ensure character's foot is always on ground perfectly.\n\n" +
            "Name: perspectiveRemovalStartHeight\n" +
            "Default: 0")]
        [OverrideDisplayName("    End height")]
        public float perspectiveRemovalStartHeight = 0;
        
        // TODO:
        // should convert to or add local height mask from GO's root position, world height maybe is a wrong design since character is not always standing on World Position.y = 0 plane.

        [Header("    XR")]
        [Tooltip(
            "It is maybe a bit weird to use 'Perspective Removal' in XR, you can enable this toggle to disable 'Perspective Removal' for XR if you feel that it is weird.\n\n" +
            "Name: disablePerspectiveRemovalInXR\n" +
            "Default: True")]
        [OverrideDisplayName("    Disable in XR?")]
        public bool disablePerspectiveRemovalInXR = true;
        //------------------------------------------------------------
        [Foldout("Receive Shadow Maps")]

        [Tooltip(
            "Controls the amount of Average URP Shadow that this character receives.\n\n" +
            "If you want NiloToon character to receive an average of URP shadow map(a special shadowing that is blurry across the whole character, darken the character uniformly), turn this on.\n"+
            "When turned on, character won't receive main directional light when occluded by shadow casters.\n\n" +
            "Name: receiveAverageURPShadowMap\n" + 
            "Default: 1")]
        [RangeOverrideDisplayName("Average URP Shadow", 0, 1)]
        public float receiveAverageURPShadowMap = 1;

        [Tooltip(
            "Controls the amount of Standard URP Shadow that this character receives.\n\n" +
            "Name: receiveStandardURPShadowMap\n" +
            "Default: 1")]
        [RangeOverrideDisplayName("Standard URP Shadow", 0, 1)]
        public float receiveStandardURPShadowMap = 1;

        [Tooltip(
            "Controls the amount of NiloToon Self Shadow that this character receives.\n\n" +
            "Name: receiveNiloToonSelfShadowMap\n" +
            "Default: 1")]
        [RangeOverrideDisplayName("NiloToon Self Shadow", 0, 1)]
        public float receiveNiloToonSelfShadowMap = 1;
        //------------------------------------------------------------
        [Foldout("Tint color")]

        [Tooltip(
            "Tint color of character. (affects rim light)\n\n" +
            "Name: perCharacterTintColor\n" +
            "Default: White")]
        [ColorUsageOverrideDisplayName("Color", false, true)]
        public Color perCharacterTintColor = Color.white; // default tint white = do nothing
        //-------------------------------------------------------------------------
        [Foldout("Add color")]

        [Tooltip(
            "Add an additive color to character.\n\n" +
            "Name: perCharacterAddColor\n" +
            "Default: Black")]
        [ColorUsageOverrideDisplayName("Color", false, true)]
        public Color perCharacterAddColor = Color.black; // default add black = do nothing
        //-------------------------------------------------------------------------
        [Foldout("Desaturate")]

        [Tooltip(
            "Increase to turn character into grayscale.\n\n" +
            "Name: perCharacterDesaturationUsage\n" +
            "Default: 0")]
        [RangeOverrideDisplayName("Strength", 0, 1f)]
        public float perCharacterDesaturationUsage = 0;
        //-------------------------------------------------------------------------
        [Foldout("Replace by color")]

        [Tooltip(
            "Increase to replace character to a single color.\n\n" +
            "Name: perCharacterLerpUsage\n" +
            "Default: 0")]
        [RangeOverrideDisplayName("Strength", 0, 1f)]
        public float perCharacterLerpUsage = 0;

        [Tooltip(
            "If the above 'Strength' is > 0, replace character to this color.\n\n" +
            "Name: perCharacterLerpColor\n" +
            "Default: White")]
        [ColorUsageOverrideDisplayName("    Color", false, true)]
        public Color perCharacterLerpColor = new Color(1, 1, 1);
        //-------------------------------------------------------------------------
        [Foldout("Rim Light")]

        [Tooltip(
            "Enable Rim light? (Face will not receive rim light)\n\n" +
            "Name: usePerCharacterRimLightIntensity\n" + 
            "Default: false")]
        [OverrideDisplayName("Enable?")]
        public bool usePerCharacterRimLightIntensity = false;

        [Tooltip(
            "Intensity of rim light.\n\n" +
            "Name: perCharacterRimLightIntensity\n" +
            "Default: 2")]
        [RangeOverrideDisplayName("    Intensity", 0, 100)]
        public float perCharacterRimLightIntensity = 2;

        [Tooltip(
            "Tint color of rim light.\n\n" +
            "Name: perCharacterRimLightColor\n" +
            "Default: White")]
        [ColorUsageOverrideDisplayName("    Color", false, true)]
        public Color perCharacterRimLightColor = new Color(1, 1, 1);

        [Tooltip(
            "Sharpness of rim light.\n\n" +
            "Name: perCharacterRimLightSharpnessPower\n" +
            "Default: 4")]
        [RangeOverrideDisplayName("    Sharpness", 1, 32)]
        public float perCharacterRimLightSharpnessPower = 4;
        //-------------------------------------------------------------------------
        [Foldout("Color Fill")]
        [Tooltip(
            "Enable to render a color fill for this character's area, usually for:\n" +
            "- Showing occluded character behind a wall\n\n" +
            "Name: shouldRenderCharacterAreaColorFill\n" +
            "Default: Off")]
        [OverrideDisplayName("Enable?")]
        public bool shouldRenderCharacterAreaColorFill = false;
        
        [Tooltip(
            "The Tint Color of Color Fill on character area.\n\n" +
            "Name: characterAreaColorFillColor\n" +
            "Default: White")]
        [ColorUsageOverrideDisplayName("    Color", true, true)]
        public Color characterAreaColorFillColor = Color.white;

        [Tooltip(
            "The Color Fill Texture to display.\n\n" +
            "Name: characterAreaColorFillTexture\n" +
            "Default: None (White texture)")]
        [OverrideDisplayName("    Texture")]
        public Texture2D characterAreaColorFillTexture = null;
        
        [Tooltip(
            "The UV type of Texture.\n\n" +
            "Name: characterAreaColorFillTextureUVIndex\n" +
            "Default: CharBoundUV")]
        [OverrideDisplayName("        UV")]
        public ScreenSpaceUVOption characterAreaColorFillTextureUVIndex = ScreenSpaceUVOption.CharBoundUV;
        
        [Tooltip(
            "The overall UV tiling of the texture.\n\n" +
            "Name: characterAreaColorFillTextureUVTiling\n" +
            "Default: 1")]
        [OverrideDisplayName("            Tiling")]
        public float characterAreaColorFillTextureUVTiling = 1;
        
        [Tooltip(
            "The UV tiling(XY) of the texture.\n\n" +
            "Name: characterAreaColorFillTextureUVTilingXY\n" +
            "Default: (1,1)")]
        [OverrideDisplayName("            Tiling(XY)")]
        public Vector2 characterAreaColorFillTextureUVTilingXY = new Vector2(1,1);
        
        [Tooltip(
            "The UV offset of the texture.\n\n" +
            "Name: characterAreaColorFillTextureUVOffset\n" +
            "Default: (0,0)")]
        [OverrideDisplayName("            Offset")]
        public Vector2 characterAreaColorFillTextureUVOffset = new Vector2(0,0);
        
        [Tooltip(
            "The UV scrolling direction and speed of Texture.\n\n" +
            "Name: characterAreaColorFillTextureUVScrollSpeed\n" +
            "Default: (0,0)")]
        [OverrideDisplayName("            Scroll Speed")]
        public Vector2 characterAreaColorFillTextureUVScrollSpeed = new Vector2(0,0);
        
        [Tooltip(
            "Should this color fill render on areas that the character is visible (e.g. not blocked by a wall).\n\n" +
            "Name: characterAreaColorFillRendersVisibleArea\n" +
            "Default: False")]
        [OverrideDisplayName("    Show Visible part")]
        public bool characterAreaColorFillRendersVisibleArea = false;
        
        [Tooltip(
            "Should this color fill render on areas that the character is not visible (e.g. blocked by a wall).\n\n" +
            "Name: characterAreaColorFillRendersBlockedArea\n" +
            "Default: True")]
        [OverrideDisplayName("    Show Blocked part")]
        public bool characterAreaColorFillRendersBlockedArea = true;
        
        //-------------------------------------------------------------------------
        [Foldout("Extra thick Outline")]
        [Tooltip(
            "Enable to render an extra thick outline for this character, usually for:\n" +
            "- selection outline in gameplay\n" +
            "- any artistic needs, like a thick white/black outline\n" +
            "- A fake 2D semi-transparent back shadow\n\n" +
            "Requires (PlayMode)\n\n" +
            "Name: shouldRenderExtraThickOutline\n" +
            "Default: Off")]
        [OverrideDisplayName("Enable?")]
        public bool shouldRenderExtraThickOutline = false;

        [Tooltip(
            "The Color of Extra thick Outline.\n\n" +
            "Name: extraThickOutlineColor\n" +
            "Default: White")]
        [ColorUsageOverrideDisplayName("    Color", true, true)]
        public Color extraThickOutlineColor = Color.white;
        [Tooltip(
            "The Width of Extra thick Outline.\n\n" +
            "Name: extraThickOutlineColor\n" +
            "Default: 3")]
        [RangeOverrideDisplayName("    Width", 0, 100)]
        public float extraThickOutlineWidth = 3;
        [Tooltip(
            "The Maximum final width of Extra thick Outline, since all outlines will be affected by distance from camera to vertex.\n" +
            "Default = 100 since we don't want this setting to do anything by default, you can reduce it if you find that extra thick outline is too thick when camera is far away from character.\n" +
            "Name: extraThickOutlineMaximumFinalWidth\n" +
            "Default: 100")]
        [RangeOverrideDisplayName("    Max final Width", 0, 100)]
        public float extraThickOutlineMaximumFinalWidth = 100;

        [Tooltip(
            "You can move this outline's position in View Space, useful to create a 2D fake drop shadow that is next to character.\n\n" +
            "Name: extraThickOutlineViewSpacePosOffset\n" +
            "Default: (0,0,0)")]
        [OverrideDisplayName("    Pos Offset(VS)")]
        public Vector3 extraThickOutlineViewSpacePosOffset = Vector3.zero;

        [Tooltip(
            "Should this outline render when character is visible (e.g. not blocked by a wall).\n\n" +
            "Name: extraThickOutlineRendersVisibleArea\n" +
            "Default: True")]
        [OverrideDisplayName("    Show Visible part (PlayMode)")]
        public bool extraThickOutlineRendersVisibleArea = true;

        [Tooltip(
            "Should this outline render when character is blocked (e.g. blocked by a wall).\n\n" +
            "Name: extraThickOutlineRendersBlockedArea\n" +
            "Default: False")]
        [OverrideDisplayName("    Show Blocked part (PlayMode)")]
        public bool extraThickOutlineRendersBlockedArea = false;

        [Tooltip(
            "Should this outline writes into the depth Buffer also? These options may affect how other transparent materials draw onto this outline's area.\n" +
            "- Auto = if this outline's alpha is 1, then it will write into the depth buffer also, acting same as an Opaque outline material. If alpha is < 1, then it will NOT write into the depth buffer\n" +
            "- On = it will always write into the depth buffer\n" +
            "- Off = it will never write into the depth buffer\n\n" +
            "Name: extraThickOutlineZWriteMode\n" +
            "Default: Auto")]
        [OverrideDisplayName("    ZWrite Mode (PlayMode)")]
        public ExtraThickOutlineZWriteMode extraThickOutlineZWriteMode = ExtraThickOutlineZWriteMode.Auto;

        [Tooltip(
           "Should this outline writes into the depth texture also? \n" +
           "Enable this will improve Depth of Field result, but will affect Depth Texture rim light and shadow's width\n\n" +
           "Name: extraThickOutlineWriteIntoDepthTexture\n" +
           "Default: false")]
        [OverrideDisplayName("    Write DepthTex?")]
        public bool extraThickOutlineWriteIntoDepthTexture = false;

        [Tooltip(
            "Decrease this to render a better outline when the outline is occluded by an opaque ground. To understand what it does you can set it to 0 and you will see the outline difference at area around the foot and ground.\n" +
            "Name: extraThickOutlineZOffset\n" +
            "Default: -0.1")]
        [OverrideDisplayName("    ZOffset")]
        public float extraThickOutlineZOffset = -0.1f;
        //-------------------------------------------------------------------------
        [Foldout("BaseMap Overlay (PlayMode)")]

        [Tooltip(
            "Useful for showing an overlay texture on character. (e.g. ice texture, rock texture, any pattern texture)\n" +
            "The higher the Strength, the higher the alpha(opacity) of Overlay Map.\n\n" +
            "Requires (PlayMode)\n\n" +
            "Name: perCharacterBaseMapOverrideAmount\n" +
            "Default: 0")]
        [RangeOverrideDisplayName("Strength", 0, 1)]
        [Range(0, 1)] public float perCharacterBaseMapOverrideAmount = 0;

        [Tooltip(
            "An extra tint color of Overlay Map.\n\n" +
            "Name: perCharacterBaseMapOverrideTintColor\n" +
            "Default: White")]
        [ColorUsageOverrideDisplayName("    Tint Color", false, true)]
        public Color perCharacterBaseMapOverrideTintColor = Color.white;
        
        [Tooltip(
            "Assign a texture that you want to show on character.\n\n" +
            "Name: perCharacterBaseMapOverrideMap\n" +
            "Default: None (white texture)")]
        [OverrideDisplayName("    Overlay Map")]
        public Texture2D perCharacterBaseMapOverrideMap = null;

        [OverrideDisplayName("        UV")]
        public UVOption perCharacterBaseMapOverrideUVIndex = 0;
        
        [Tooltip(
            "The tiling of Overlay Map.\n\n" +
            "Name: perCharacterBaseMapOverrideTiling\n" +
            "Default: 1")]
        [OverrideDisplayName("            Tiling")]
        public float perCharacterBaseMapOverrideTiling = 1;

        [Tooltip(
            "The UV tiling(XY) of the texture.\n\n" +
            "Name: perCharacterBaseMapOverrideUVTilingXY\n" +
            "Default: (1,1)")]
        [OverrideDisplayName("            Tiling(XY)")]
        public Vector2 perCharacterBaseMapOverrideUVTilingXY = new Vector2(1,1);
        
        [Tooltip(
            "The UV offset of the texture.\n\n" +
            "Name: perCharacterBaseMapOverrideUVOffset\n" +
            "Default: (0,0)")]
        [OverrideDisplayName("            Offset")]
        public Vector2 perCharacterBaseMapOverrideUVOffset = new Vector2(0,0);
        
        [Tooltip(
            "The UV scrolling direction and speed of Texture.\n\n" +
            "Name: perCharacterBaseMapOverrideUVScrollSpeed\n" +
            "Default: (0,0)")]
        [OverrideDisplayName("            Scroll Speed")]
        public Vector2 perCharacterBaseMapOverrideUVScrollSpeed = new Vector2(0,0);

        [OverrideDisplayName("    Blend Mode")]
        public BaseMapOverrideBlendMode perCharacterBaseMapOverrideBlendMode = BaseMapOverrideBlendMode.Normal; 
        //-------------------------------------------------------------------------
        [Foldout("Dither Fadeout (PlayMode)")]
        
        [Tooltip(
            "Controls the Overall Opacity of this character.\n" +
            "This slider will apply dither fadeout to URP's shadowmap also, which we recommend enabling URP's softshadow toggle to improve the final shadow result.\n" +
            "If you enabled SSAO in renderer feature, you should turn off SSAO's After Opaque, else SSAO will draw on top of NiloToon's character shader.\n" +
            "Keep it at 1 will improve performance a lot, since we can turn it off completely in shader.\n\n" +
            "Requires (PlayMode)\n\n" +
            "Name: ditherOpacity\n" +
            "Default: 1")]
        [RangeOverrideDisplayName("Opacity", 0, 1)]
        public float ditherOpacity = 1;

        [Tooltip(
            "In most cases, you do NOT need to edit this,\n" +
            "but if the SSAO result is not good in dither area, you can try edit it.\n\n" +
            "Name: ditherNormalScaleFix\n" +
            "Default: 1")]
        [RangeOverrideDisplayName("    NormalScaleFix", 0, 16)]
        public float ditherNormalScaleFix = 1;
        //-------------------------------------------------------------------------
        [Foldout("Dissolve (PlayMode)")]

        [Tooltip(
            "Drag this slider to control the dissolve threshold of this character.\n" +
            "Keep it at 0 means disable this effect, which will improve performance a lot, since we can turn it off completely in shader.\n\n" +
            "Requires (PlayMode)\n\n" +
            "Name: dissolveAmount\n" +
            "Default: 0")]
        [RangeOverrideDisplayName("Amount",0, 1)] 
        public float dissolveAmount = 0;

        [Tooltip(
            "Select a method to process dissolve.\n" +
            "We recommend try each of them in PlayMode to see which one is suitable.\n\n" +
            "*If Mode is Default, it will use WorldSpaceNoise Mode\n\n" +
            "*If Mode is any ScreenSpace method, shadowcaster pass's dissolve direction will rely on Directional Light's local rotation.z. It is a known limitation.\n\n" +
            "Name: dissolveMode\n" +
            "Default: Default")]
        [OverrideDisplayName("    Mode")]
        public DissolveMode dissolveMode = DissolveMode.Default;

        [Tooltip(
            "The map that defines the dissolve threshold / dissolve noise.\n" +
            "When it is None, it will use NiloToon's default noise texture(NiloToonDefaultDissolvePerlinNoise.png).\n\n" +
            "Name: dissolveThresholdMap\n" +
            "Default: None")]
        [OverrideDisplayName("    Threshold/Noise Map")]
        public Texture2D dissolveThresholdMap = null;

        [Tooltip(
           "The uv.x tiling when sampling Threshold/Noise Map.\n\n" +
           "Name: dissolveThresholdMapTilingX\n" +
           "Default: 1")]
        [RangeOverrideDisplayName("         Tiling(X)", 0.01f, 32f)]
        public float dissolveThresholdMapTilingX = 1;

        [Tooltip(
            "The uv.y tiling when sampling Threshold/Noise Map.\n\n" +
            "Name: dissolveThresholdMapTilingY\n" +
            "Default: 1")]
        [RangeOverrideDisplayName("         Tiling(Y)", 0.01f, 32f)]
        public float dissolveThresholdMapTilingY = 1;

        [Tooltip(
            "The noise strength for the following DissolveMode.\n\n" +
            "- WorldSpaceVerticalUpward\n" +
            "- WorldSpaceVerticalDownward\n" +
            "- ScreenSpaceVerticalUpward\n" +
            "- ScreenSpaceVerticalDownward\n" +
            "Name: dissolveNoiseStrength\n" +
            "Default: 1")]
        [RangeOverrideDisplayName("         Noise Strength", 0f, 4f)]
        public float dissolveNoiseStrength = 1;

        [Tooltip(
           "The width of the dissolve border.\n\n" +
           "Name: dissolveBorderRange\n" +
           "Default: 0.02f")]
        [RangeOverrideDisplayName("    Border Range", 0.001f, 1)] 
        public float dissolveBorderRange = 0.02f;

        [Tooltip(
           "The tint color of the dissolve border, you can use an HDR color to make the dissolve border glow due to bloom.\n\n" +
           "Name: dissolveBorderTintColor\n" +
           "Default: (1,3,6)")]
        [ColorUsageOverrideDisplayName("    Border Color", false, true)]
        public Color dissolveBorderTintColor = new Color(1, 3, 6);
        //-------------------------------------------------------------------------
        [Foldout("ZOffset")]

        [Tooltip(
           "Similar to NiloToon_Character material's ZOffset, \n" +
           "- A positive ZOffset will push character away from Camera. \n" +
           "- A negative ZOffset will push character towards Camera. \n\n" +
           "Useful for per character z sorting when characters intersect with each other, or when character is intersecting the environment. \n\n" +
           "*Using this ZOffset will allow you to see NiloToonSelfShadowMap or URP ShadowMap shadows that should not be visible in normal situation, it can look weird, so please consider disable these shadowmaps in NiloToonShadowMapVolume when you are using this ZOffset feature and the shadow is weird. \n\n" +
           "Name: perCharacterZOffset\n" +
           "Default: 0")]
        [OverrideDisplayName("ZOffset(meter)")]
        public float perCharacterZOffset = 0;
        //-------------------------------------------------------------------------
        [Foldout("Allowed Passes (PlayMode)")]

        [Tooltip(
            "Enable to allow this character renders into URP's shadow maps.\n\n" +
            "Requires (PlayMode)\n\n" +
            "Name: allowRenderShadowCasterPass\n" +
            "Default: On")]
        [OverrideDisplayName("URP ShadowCasterPass")]
        public bool allowRenderShadowCasterPass = true;

        [Tooltip(
            "Enable to allow this character renders into URP's Depth texture/Normal texture.\n\n" +
            "Requires (PlayMode)\n\n" +
            "Name: allowRenderDepthOnlyAndDepthNormalsPass\n" +
            "Default: On")]
        [OverrideDisplayName("DepthOnly/DepthNormals")]
        public bool allowRenderDepthOnlyAndDepthNormalsPass = true;

        [Tooltip(
            "Enable to allow this character renders into NiloToon's shadowmap.\n\n" +
            "Requires (PlayMode)\n\n" +
            "Name: allowRenderNiloToonSelfShadowCasterPass\n" +
            "Default: On")]
        [OverrideDisplayName("NiloToonSelfShadowCaster")]
        public bool allowRenderNiloToonSelfShadowCasterPass = true;

        [Tooltip(
            "Enable to allow this character renders into NiloToon's prepass render texture.\n\n" +
            "Requires (PlayMode)\n\n" +
            "Name: allowRenderNiloToonPrepassBufferPass\n" +
            "Default: On")]
        [OverrideDisplayName("NiloToonPrepassBuffer")]
        public bool allowRenderNiloToonPrepassBufferPass = true;
        //-------------------------------------------------------------------------
        /*
        [Foldout("Optimization")]
        [Tooltip(
            "If turn this on, will boost CPU performance a lot, but switching material in playmode will require you to call RequestForceMaterialUpdateOnce() in C#.\n\n" +
            "Requires (PlayMode)\n" +
            "Name: allowCacheSystem\n" +
            "Default: On")]
        [OverrideDisplayName("Use Cache?")]
        public bool allowCacheSystem = true;
        */
        
        //-------------------------------------------------------------------------
        [Foldout("Misc")]
        [Tooltip(
            "Enable to keep material instance's name in PlayMode same as the original material name.\n" +
            "It is required by VRMBlendShapeProxy in order to let VRMBlendShapeProxy's material control works, so it is default on.\n\n" +
            "For example,\n" +
            "- If enabled, the generated material instance name is 'm_MyMat', same as the original material\n" +
            "- If disabled, the generated material instance name is 'm_MyMat (Instance)', using Unity's default material instance's naming method\n\n" +
            "Requires (PlayMode).\n\n" +
            "Name: keepMaterialInstanceNameSameAsOriginal\n" +
            "Default: On")]
        [OverrideDisplayName("Support VRMBlendShapeProxy?")]
        public bool keepMaterialInstanceNameSameAsOriginal = true;

        [OverrideDisplayName("Generate Smoothed normal?")]
        [Tooltip(
            "When turned on,\n" +
            "- For .fbx, this option will be mostly useless, since UV8(smoothed normal data) should be generated already on .fbx's reimport right after this script is attached to the character\n" +
            "- For other mesh(e.g., vrm's mesh asset), this option will generate UV8(smoothed normal data) to the sharedMesh of all renderers, so a very similar outline result of fbx can be produced by other mesh(e.g., vrm's mesh asset)\n\n" +
            "*The only way to remove the generate UV8, is to turn off this toggle and restart Unity, since the data is saved into the sharedMesh asset.\n\n" +
            "Default: Off"
            )]
        public bool regenerateSmoothedNormalInUV8 = false;

        [OverrideDisplayName("Compatible with 0.13.8 shader")]
        [Tooltip(
            "When turned on,\n" +
            "Allows you to use NiloToon0.l6's C# together with NiloToon 0.13.8's shader\n" +
            "The only purpose is to let NiloToon0.16's C# support old Asset Bundle / Warudo built with NiloToon 0.13.8 shader\n\n" +
            "Default: Off"
        )]
        public bool compatibleWithNiloToon13Shader = false;
        
        /// <summary>
        /// API for user, to force update all material once
        /// </summary>
        public void RequestForceMaterialUpdateOnce()
        {
            requestForceMaterialUpdate = true;
            alreadyRenameMaterialToOriginalName = false;
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        // NiloToon renderer feature will set this, it is internal so user can't see/set this
        [NonSerialized]
        internal int characterID = -1;

        [NonSerialized]
        /// <summary>
        /// Should not be used by NiloToon user, this field is for preventing reimport Mesh(not .fbx) dead loop
        /// </summary>
        public bool reimportDone = false;
        [NonSerialized]
        public int requireUpdateCount = 2; // default require update twice (once is not enough, not sure why)

        // originally created for perspective removal override by an external script(NiloToonCharacterRenderOverrider)
        // to allow multiple character's hand holding, and everything having the same perspective removal settings
        [NonSerialized]
        internal NiloToonCharacterRenderOverrider ExternalRenderOverrider;

        MaterialPropertyBlock materialPropertyBlock;
        List<Material> tempMaterialList = new();
        Dictionary<int, Material[]> matListDictionary = new();
        bool? lastFrameShouldEditMaterial;
        bool forceMaterialIgnoreCacheAndUpdate;
        bool requestForceMaterialUpdate;
        bool refillAllRenderersRequired;
        bool alreadyRenameMaterialToOriginalName;

        // only to avoid GC per call
        List<Renderer> allRenderersFound = new();
        HashSet<NiloToonPerCharacterRenderController> clearedNiloControllers = new();

        static Texture2D _defaultWhiteTexture;
        Texture2D DefaultWhiteTexture
        {
            get
            {
                if (_defaultWhiteTexture == null)
                {
                    // this line (calling Texture2D.whiteTexture) will crash the Unity Editor if NDMF(Non-Destructive Modular Framework) is used in the project
                    //_defaultWhiteTexture = Texture2D.whiteTexture;
                    
                    // so to avoid the crash, we use our own white texture as a workaround
                    _defaultWhiteTexture = new Texture2D(1,1);
                    _defaultWhiteTexture.SetPixel(0,0,Color.white);
                }

                return _defaultWhiteTexture;
            }
        }
        
        Texture2D defaultDissolveThresholdMap;

        // optimization: cache of last frame's value, for remove useless material.SetXXX() calls
        bool? renderCharacter_Cache;
        bool? controlledByNiloToonPerCharacterRenderController_Cache;
        int? characterID_Cache;
        Color? perCharacterTintColor_Cache;
        Color? perCharacterAddColor_Cache;
        float? perCharacterDesaturationUsage_Cache;
        Vector4? PerCharEffectLerpColor_Cache;
        Vector4? PerCharEffectRimColor_Cache;
        float? PerCharEffectRimSharpnessPower_Cache;
        Vector3? FinalFaceForwardDirectionWS_Cache; // Converted to global array
        Vector3? FinalFaceUpDirectionWS_Cache; // Converted to global array
        float? faceNormalFixAmount_Cache;
        float? fixFaceNormalUseFlattenOrProxySphereMethod_Cache;
        bool? characterAreaColorFillEnabled_Cache;
        Color? characterAreaColorFillColor_Cache;
        Texture2D characterAreaColorFillTexture_Cache;
        ScreenSpaceUVOption? characterAreaColorFillTextureUVIndex_Cache;
        Vector4? characterAreaColorFillTextureUVTilingOffset_Cache;
        Vector2? characterAreaColorFillTextureUVScrollSpeed_Cache;
        bool? characterAreaColorFillRendersVisibleArea_Cache;
        bool? characterAreaColorFillRendersBlockedArea_Cache;
        bool? extraThickOutlineEnabled_Cache;
        Color? extraThickOutlineColor_Cache;
        float? extraThickOutlineWidth_Cache;
        float? extraThickOutlineMaximumFinalWidth_Cache;
        Vector3? extraThickOutlineViewSpacePosOffset_Cache;
        int? ExtraThickOutlineZTest_Cache;
        float? extraThickOutlineZOffset_Cache;
        float? extraThickOutlineZWrite_Cache;
        bool? extraThickOutlineWriteIntoDepthTexture_Cache;
        Vector3? CharacterBoundCenter_Cache; // Converted to global array
        float? CharacterBoundRadius_Cache;
        float? PerspectiveRemovalAmount_Cache;
        float? PerspectiveRemovalRadius_Cache;
        float? PerspectiveRemovalStartHeight_Cache;
        float? PerspectiveRemovalEndHeight_Cache;
        Vector3? PerspectiveRemovalCenter_Cache; // Converted to global array
        Color? PerCharBaseColorTint_Cache;
        float? perCharacterOutlineWidthMultiply_Cache;
        Color? PerCharOutlineColorTint_Cache;
        Color? perCharacterOutlineEffectLerpColor_Cache;
        float? DitherFadeoutAmount_Cache;
        float? DitherNormalScaleFix_Cache;
        float? DissolveAmount_Cache;
        int? DissolveMode_Cache;
        Texture2D DissolveThresholdMap_Cache;
        float? DissolveBorderRange_Cache;
        float? DissolveThresholdMapTilingX_Cache;
        float? DissolveThresholdMapTilingY_Cache;
        float? DissolveNoiseStrength_Cache;
        Color? DissolveBorderTintColor_Cache;
        float? PerCharacterBaseMapOverrideAmount_Cache;
        Texture2D PerCharacterBaseMapOverrideMap_Cache;
        Vector4? PerCharacterBaseMapOverrideTilingOffset_Cache;
        Vector2? perCharacterBaseMapOverrideUVScrollSpeed_Cache;
        Color? PerCharacterBaseMapOverrideTintColor_Cache;
        UVOption? perCharacterBaseMapOverrideUVIndex_Cache;
        BaseMapOverrideBlendMode? perCharacterBaseMapOverrideBlendMode_Cache;
        float? ReceiveAverageURPShadowMap_Cache;
        float? ReceiveStandardURPShadowMap_Cache;
        float? ReceiveNiloToonSelfShadowMap_Cache;
        float? perCharacterZOffset_Cache;
        bool? shouldRenderSelfOutline_Cache;
        bool? ShouldRenderNiloToonCharacterAreaColorFillPass_Cache;
        bool? ShouldRenderNiloToonExtraThickOutlinePass_Cache;
        bool? ShouldEnableDitherFadeOut_Cache;
        bool? ShouldEnableDissolve_Cache;
        bool? ShouldEnablePerCharacterBaseMapOverride_Cache;
        bool? ShouldEnableDepthTextureRimLightAndShadow_Cache;
        bool? allowRenderShadowCasterPass_Cache;
        bool? allowRenderDepthOnlyAndDepthNormalsPass_Cache;
        bool? allowRenderNiloToonSelfShadowCasterPass_Cache;
        bool? allowRenderNiloToonPrepassBufferPass_Cache;

        // RequireMaterialSet or RequireSetShaderPassEnabledCall
        bool renderCharacter_RequireMaterialSet;
        bool controlledByNiloToonPerCharacterRenderController_RequireMaterialSet;
        bool characterID_RequireMaterialSet;
        bool perCharacterTintColor_RequireMaterialSet;
        bool perCharacterAddColor_RequireMaterialSet;
        bool perCharacterDesaturationUsage_RequireMaterialSet;
        bool PerCharEffectLerpColor_RequireMaterialSet;
        bool PerCharEffectRimColor_RequireMaterialSet;
        bool PerCharEffectRimSharpnessPower_RequireMaterialSet;
        bool FinalFaceForwardDirectionWS_RequireMaterialSet; // Converted to global array
        bool FinalFaceUpDirectionWS_RequireMaterialSet; // Converted to global array
        bool faceNormalFixAmount_RequireMaterialSet;
        bool fixFaceNormalUseFlattenOrProxySphereMethod_RequireMaterialSet;
        bool characterAreaColorFillEnabled_RequireMaterialSet;
        bool characterAreaColorFillColor_RequireMaterialSet;
        bool characterAreaColorFillTexture_RequireMaterialSet;
        bool characterAreaColorFillTextureUVIndex_RequireMaterialSet;
        bool characterAreaColorFillTextureUVTilingOffset_RequireMaterialSet;
        bool characterAreaColorFillTextureUVScrollSpeed_RequireMaterialSet;
        bool characterAreaColorFillRendersVisibleArea_RequireMaterialSet;
        bool characterAreaColorFillRendersBlockedArea_RequireMaterialSet;
        bool extraThickOutlineEnabled_RequireMaterialSet;
        bool extraThickOutlineColor_RequireMaterialSet;
        bool extraThickOutlineWidth_RequireMaterialSet;
        bool extraThickOutlineMaximumFinalWidth_RequireMaterialSet;
        bool extraThickOutlineViewSpacePosOffset_RequireMaterialSet;
        bool ExtraThickOutlineZTest_RequireMaterialSet;
        bool extraThickOutlineZOffset_RequireMaterialSet;
        bool extraThickOutlineZWrite_RequireMaterialSet;
        bool extraThickOutlineWriteIntoDepthTexture_RequireMaterialSet;
        bool CharacterBoundCenter_RequireMaterialSet; // Converted to global array
        bool CharacterBoundRadius_RequireMaterialSet;
        bool PerspectiveRemovalAmount_RequireMaterialSet;
        bool PerspectiveRemovalRadius_RequireMaterialSet;
        bool PerspectiveRemovalStartHeight_RequireMaterialSet;
        bool PerspectiveRemovalEndHeight_RequireMaterialSet;
        bool PerspectiveRemovalCenter_RequireMaterialSet; // Converted to global array
        bool PerCharBaseColorTint_RequireMaterialSet;
        bool perCharacterOutlineWidthMultiply_RequireMaterialSet;
        bool PerCharOutlineColorTint_RequireMaterialSet;
        bool perCharacterOutlineEffectLerpColor_RequireMaterialSet;
        bool DitherFadeoutAmount_RequireMaterialSet;
        bool DitherNormalScaleFix_RequireMaterialSet;
        bool DissolveAmount_RequireMaterialSet;
        bool DissolveMode_RequireMaterialSet;
        bool DissolveThresholdMap_RequireMaterialSet;
        bool DissolveBorderRange_RequireMaterialSet;
        bool DissolveThresholdMapTilingX_RequireMaterialSet;
        bool DissolveThresholdMapTilingY_RequireMaterialSet;
        bool DissolveNoiseStrength_RequireMaterialSet;
        bool DissolveBorderTintColor_RequireMaterialSet;
        bool PerCharacterBaseMapOverrideAmount_RequireMaterialSet;
        bool PerCharacterBaseMapOverrideMap_RequireMaterialSet;
        bool PerCharacterBaseMapOverrideTilingOffset_RequireMaterialSet;
        bool perCharacterBaseMapOverrideUVScrollSpeed_RequireMaterialSet;
        bool PerCharacterBaseMapOverrideTintColor_RequireMaterialSet;
        bool perCharacterBaseMapOverrideUVIndex_RequireMaterialSet;
        bool perCharacterBaseMapOverrideBlendMode_RequireMaterialSet;
        bool ReceiveAverageURPShadowMap_RequireMaterialSet;
        bool ReceiveStandardURPShadowMap_RequireMaterialSet;
        bool ReceiveNiloToonSelfShadowMap_RequireMaterialSet;
        bool perCharacterZOffset_RequireMaterialSet;
        bool shouldRenderSelfOutline_RequireMaterialSet;
        bool shouldRenderSelfOutline_RequireSetShaderPassEnabledCall;
        bool ShouldRenderNiloToonCharacterAreaColorFillPass_RequireSetShaderPassEnabledCall;
        bool ShouldRenderNiloToonExtraThickOutlinePass_RequireSetShaderPassEnabledCall;
        bool ShouldEnableDitherFadeOut_RequireKeywordChangeCall;
        bool ShouldEnableDissolve_RequireKeywordChangeCall;
        bool ShouldEnablePerCharacterBaseMapOverride_RequireKeywordChangeCall;
        bool ShouldEnableDepthTextureRimLightAndShadow_RequireKeywordChangeCall;
        bool allowRenderShadowCasterPass_RequireSetShaderPassEnabledCall;
        bool allowRenderDepthOnlyAndDepthNormalsPass_RequireSetShaderPassEnabledCall;
        bool allowRenderNiloToonSelfShadowCasterPass_RequireSetShaderPassEnabledCall;
        bool allowRenderNiloToonPrepassBufferPass_RequireSetShaderPassEnabledCall;

        // build current frame data cache (for avoid duplicated method calls which returns the same result within the same frame)
        bool isHeadBoneTransformExist;

        internal Vector3 finalFaceDirectionWS_Forward { get; private set; }
        internal Vector3 finalFaceDirectionWS_Up { get; private set; }
        internal Vector3 characterBoundCenter { get; private set; }
        internal Vector3 perspectiveRemovalCenter { get; private set; }
        
        Vector4 perCharacterTintColorAsVector;
        Vector4 perCharacterAddColorAsVector;
        Vector4 PerCharEffectLerpColorAsVector;
        Vector4 PerCharEffectRimColorAsVector;
        Vector4 characterAreaColorFillColorAsVector;
        Vector4 extraThickOutlineColorAsVector;
        Vector4 PerCharBaseColorTintAsVector;
        Vector4 PerCharOutlineColorTintAsVector;
        Vector4 perCharacterOutlineEffectLerpColorAsVector;
        Vector4 PerCharacterBaseMapOverrideTintColorAsVector;

        void OnValidate()
        {
            // group: prevent inspector setting negative number
            characterBoundRadius = Mathf.Max(0, characterBoundRadius);
            perspectiveRemovalRadius = Mathf.Max(0, perspectiveRemovalRadius);
            perCharacterBaseColorMultiply = Mathf.Max(0, perCharacterBaseColorMultiply);
            perCharacterOutlineWidthMultiply = Mathf.Max(0, perCharacterOutlineWidthMultiply);

            if (regenerateSmoothedNormalInUV8)
            {
                NiloBakeSmoothNormalTSToMeshUv8.GenGOSmoothedNormalToUV8(gameObject);    
            }
            
            AutoFillInMissingProperties();
            
            // [Make the edited values in the Editor Inspector reflect in the Game window]
            // Note:
            // 1. Don't call LateUpdate() in edit mode, since it is not required and may trigger a crash when building the game (if headBoneTransform is null)
            // 2. check also this script's enable & activeInHierarchy, to ensure LateUpdate() only runs when this character is enabled in scene.
            // Not checking enable & activeInHierarchy will cause error when loading the character using asset bundle, since LateUpdate() will call Renderer.GetMaterials(),
            // which instantiate materials of a prefab before instantiate the character prefab.
            if (Application.isPlaying && this.enabled && this.gameObject.activeInHierarchy)
            {
                LateUpdate(); 
            }
        }

        void OnEnable()
        {
            RequestForceMaterialUpdateOnce();

            if (regenerateSmoothedNormalInUV8)
            {
                NiloBakeSmoothNormalTSToMeshUv8.GenGOSmoothedNormalToUV8(gameObject);    
            }
            
            AutoFillInMissingProperties();
            
            // To support VRMBlendShapeProxy, NiloToon now generates material instances on OnEnable(), which is before VRMBlendShapeProxy's Start()
            // Note:
            // - Don't call LateUpdate() in edit mode, since it is not required and may trigger a crash when building the game (if headBoneTransform is null)
            if (Application.isPlaying)
            {
                LateUpdate(); 
            }
        }

        void OnDisable()
        {
            NiloToonAllInOneRendererFeature.Remove(this);
            
            // reset MPB, since user can disable or remove this script
            foreach (var renderer in allRenderers)
            {
                if(!renderer) continue;
                
                renderer.SetPropertyBlock(null);
            }
        }

        void OnDestroy()
        {
            // TODO: should we just delete this Resources.UnloadAsset, and rely on Unity's asset GC?
            // unload Resources.Load<Texture2D>()
            if (defaultDissolveThresholdMap)
            {
                Resources.UnloadAsset(defaultDissolveThresholdMap);
                defaultDissolveThresholdMap = null;
            }
        }

        void OnDrawGizmosSelected()
        {
            // bounding sphere
            if (showBoundingSphereGizmo)
            {
                Gizmos.color = new Color(1, 0, 0, 0.5f);
                Gizmos.DrawSphere(characterBoundCenter, GetCharacterBoundRadius());
            }

            // face forward direction arrow
            if (showFaceForwardDirectionGizmo && headBoneTransform && isHeadBoneTransformExist)
            {
                DrawArrow(headBoneTransform.position, finalFaceDirectionWS_Forward, new Color(0, 0, 1, 1f)); // blue Z, same as unity's scene window
            }

            // face up direction arrow
            if (showFaceUpDirectionGizmo && headBoneTransform && isHeadBoneTransformExist)
            {
                DrawArrow(headBoneTransform.position, finalFaceDirectionWS_Up, new Color(0, 1, 0, 1f)); // green Y, same as unity's scene window
            }
        }

        void OnTransformChildrenChanged()
        {
            switch(autoRefillRenderersMode)
            {
                case RefillRenderersMode.Always:
                    refillAllRenderersRequired = true;
                    break;
                case RefillRenderersMode.EditModeOnly:
                    if(!Application.isPlaying)
                    {
                        refillAllRenderersRequired = true;
                    }
                    break;
                case RefillRenderersMode.APIOnly:
                    // do nothing
                    break;
            }
        }
        
        void LateUpdate()
        {
            // it is possible that the bone doesn't exist in the character, 
            // which will waste the CPU time checking per frame
            // so we only allow the check in editor 
            if (!Application.isPlaying)
            {
                AutoFillInMissingProperties();
            }

            // must call this first before others, because others rely on this
            CacheCurrentFrameCalculations();

            UpdateRequireMaterialSetFlags();

            // auto clear ExternalRenderOverrider to null, if not needed anymore
            if (ExternalRenderOverrider)
            {
                // if ExternalRenderOverrider removed this controller already
                if (!ExternalRenderOverrider.targets.Contains(this))
                    ExternalRenderOverrider = null;
            }

            // register this character into global character list
            NiloToonAllInOneRendererFeature.AddCharIfNotExist(this);

            RefillAllRenderersIfNeeded();

            // https://docs.unity3d.com/ScriptReference/ExecuteAlways.html
            // If a MonoBehaviour runs Play logic in Play Mode and fails to check if its GameObject is part of the playing world,
            // a Prefab being edited in Prefab Mode may incorrectly get modified and saved by logic intended only to be run as part of the game.
            bool shouldEditMaterial = Application.isPlaying && Application.IsPlaying(gameObject);
            
#if UNITY_EDITOR
            // shouldEditMaterial can be overridden if user enable "Keep mat edit in playmode?" in [MenuItem("Window/NiloToonURP/Debug Window")]
            // In build, NiloToon always use SRPBatching (make material instances and edit materials directly), because there is almost no reason to not use it in build

            //This option only works in editor.
            //- In build, it is always ON, which enables SRP batching = better performance.
            //- In editor, turn this OFF can   preserve material change in editor playmode, but breaks  SRP batching. Good for material editing (for artist).
            //- In editor, turn this ON  can't preserve material change in editor playmode, but enables SRP batching. Good for profiling and playtest(for programmer/QA).

            shouldEditMaterial &= !GetNiloToonNeedPreserveEditorPlayModeMaterialChange_EditorOnly();
#endif

            if (materialInstanceMode == MaterialInstanceMode.NeverGenerate)
            {
                shouldEditMaterial = false;
            }
            
            if (shouldEditMaterial)
            {
                ////////////////////////////////////////////////////////////////////////////////
                // In play mode,
                // NOT using material property block in order to make SRP batching works,
                // will produce material instances and edit materials directly
                ////////////////////////////////////////////////////////////////////////////////
                void workPerRenderer(Renderer renderer)
                {
                    if (!renderer) return;

                    if (lastFrameShouldEditMaterial.HasValue && lastFrameShouldEditMaterial != shouldEditMaterial)
                        renderer.SetPropertyBlock(null); // only clear MPB when "shouldEditMaterial" changes for the first time, to let other asset like cloth dynamics to set material value via Material property block correctly

                    // TODO: ideally we should only force update the required material, but not force update the whole character
                    bool forceUpdateMaterialNeeded = GetMaterialsInstances(renderer);
                    if (forceUpdateMaterialNeeded)
                    {
                        RequestForceMaterialUpdateOnce();
                        forceMaterialIgnoreCacheAndUpdate = true;
                        UpdateRequireMaterialSetFlags();
                    }
                    
                    foreach (var material in tempMaterialList)
                    {
                        UpdateMaterial(material);
                    }
                }
                void workPerNiloToonRendererRedrawer(NiloToonRendererRedrawer rendererRedrawer)
                {
                    if (!rendererRedrawer) return;

                    rendererRedrawer.GetMaterials(tempMaterialList);
                    foreach (var material in tempMaterialList)
                    {
                        UpdateMaterial(material);
                    }
                }

                foreach (var renderer in allRenderers)
                    workPerRenderer(renderer);
                foreach (var renderer in attachmentRendererList)
                    workPerRenderer(renderer);

                // for each NiloToonRendererRedrawer, update all of it's materials instance
                foreach (var redrawRendererRequest in nilotoonRendererRedrawerList)
                    workPerNiloToonRendererRedrawer(redrawRendererRequest);
                
                if(keepMaterialInstanceNameSameAsOriginal)
                    alreadyRenameMaterialToOriginalName = true;
            }
            else
            {
                ////////////////////////////////////////////////////////////////////////////////
                // using material property block to NOT edit renderer's material (edit mode)
                ////////////////////////////////////////////////////////////////////////////////
                void workPerRenderer(Renderer renderer)
                {
                    if (!renderer) return;

                    // get old block as base first, prevent destroy user's MPB
                    if (renderer.HasPropertyBlock())
                        renderer.GetPropertyBlock(materialPropertyBlock);

                    UpdateMaterialPropertyBlock(materialPropertyBlock);

                    // apply to renderer
                    renderer.SetPropertyBlock(materialPropertyBlock); // this will break SRP batching!
                }
                
                if (materialPropertyBlock == null)
                    materialPropertyBlock = new MaterialPropertyBlock();

                foreach (var renderer in allRenderers)
                    workPerRenderer(renderer);
                foreach (var renderer in attachmentRendererList)
                    workPerRenderer(renderer);

                // no need to handle NiloToonRendererRedrawer for material property block,
                // since NiloToonRendererRedrawer already use the material property block of the original Renderer
                // (X)
            }

            forceMaterialIgnoreCacheAndUpdate = false; // each frame reset, will turn on again when needed

            // if user edit nilotoon debug window's settings, force update once to ensure all material set
            if (lastFrameShouldEditMaterial != shouldEditMaterial)
                forceMaterialIgnoreCacheAndUpdate = true;

            lastFrameShouldEditMaterial = shouldEditMaterial;

            CacheProprtiesForNextFrameOptimizationCheck();

            // when allRenderers list changed, force update once to ensure all material set
            if (refillAllRenderersRequired || requestForceMaterialUpdate /*|| !allowCacheSystem*/)
            {
                forceMaterialIgnoreCacheAndUpdate = true;
            }

            requestForceMaterialUpdate = false; // each frame reset, will turn on by user when needed
        }

        /// <returns>bool forceUpdateMaterialNeeded</returns>
        bool GetMaterialsInstances(Renderer renderer)
        {
            // Obtain the current material instances from the renderer
            // this line will generate material instances if the material is not yet an instance
            // this line can be performance heavy if some external C#/animation keep assigning new material to the renderer
            renderer.GetMaterials(tempMaterialList);

            bool forceUpdateMaterialNeeded = false;

            // Check if the renderer already has a cached entry
            // use InstanceID as key since it is int(value type), it is much faster the Renderer(reference type) as key
            if (matListDictionary.TryGetValue(renderer.GetInstanceID(), out Material[] materialsCache))
            {
                if (materialsCache.Length == tempMaterialList.Count)
                {
                    // Update materials that have changed
                    for (var i = 0; i < materialsCache.Length; i++)
                    {
                        // If any cached material is different from the current one, replace it.
                        // It means some external logic switched a material (e.g., C#/animation)
                        if (!Equals(materialsCache[i], tempMaterialList[i]))
                        {
                            // Destroy the old material if it's not null
                            // this is an important step to prevent memory leak due to lost reference material instance(e.g., renderer's material changed by animation "material reference" key)
                            if (materialsCache[i] != null)
                            {
                                Destroy(materialsCache[i]);
                            }
                            // Replace with the new material from the tempMaterialList
                            materialsCache[i] = tempMaterialList[i];

                            forceUpdateMaterialNeeded = true;
                        }
                    } 
                }
                else
                {
                    // [If the size differs(which should be rare but still possible), update the dictionary entry with the new materials array]
                    
                    // Destroy all old materials from this renderer in the cache
                    foreach (var material in materialsCache)
                    {
                        if (material != null)
                        {
                            Destroy(material);
                        }
                    }
                    // Update the dictionary with the new array of materials
                    matListDictionary[renderer.GetInstanceID()] = tempMaterialList.ToArray();

                    forceUpdateMaterialNeeded = true;
                }
            }
            else
            {
                // If the renderer is not in the dictionary, add it with the current materials
                matListDictionary[renderer.GetInstanceID()] = tempMaterialList.ToArray();

                forceUpdateMaterialNeeded = true;
            }

            // Optionally update material names, the only reason of adding this is to support VRMBlendShapeProxy
            // TODO: alreadyRenameMaterialToOriginalName is not a perfect method, 
            if (keepMaterialInstanceNameSameAsOriginal && !alreadyRenameMaterialToOriginalName)
            {
                foreach (var material in tempMaterialList)
                {
                    if (material.name.EndsWith(" (Instance)"))
                    {
                        material.name = material.name.Substring(0, material.name.Length - " (Instance)".Length);
                    }
                }
            }

            return forceUpdateMaterialNeeded;
        }
        
        private void RefillAllRenderersIfNeeded()
        {
            // if user didn't assign allRenderers (count == 0), trigger auto refill
            refillAllRenderersRequired |= allRenderers.Count == 0;
            
            // if user updated the fbx, which makes some renderer becomes null, also trigger auto refill
            foreach (var r in allRenderers)
            {
                if (r == null)
                {
                    refillAllRenderersRequired = true;
                    break;
                }
            }

            if (refillAllRenderersRequired)
            {
                void LogRefillRenderers() =>
                    Debug.Log($"NiloToon: discard and refill AllRenderers list for ({this.gameObject.name})");

                switch (autoRefillRenderersLogMode)
                {
                    case RefillRenderersLogMode.Always:
                        LogRefillRenderers();
                        break;
                    case RefillRenderersLogMode.AlwaysEditorOnly:
#if UNITY_EDITOR
                        LogRefillRenderers();
#endif
                        break;
                    case RefillRenderersLogMode.EditModeOnly:
                        if (!Application.isPlaying)
                        {
                            LogRefillRenderers();
                        }

                        break;
                    case RefillRenderersLogMode.Never:
                        break;
                }

                RefillAllRenderers();
                
                // finished refill, so we reset the flag and wait for next refill request
                refillAllRenderersRequired = false;
            }
        }
        
        /// <summary>
        /// API for user, to force update "allRenderers" list once
        /// </summary>
        public void RefillAllRenderers()
        {
            // test of ~10 characters
            // first call = GC Alloc 0.7KB
            // extra call = GC Alloc 160B (List.AddWithResize Alloc GC)
            
            clearedNiloControllers.Clear();

            GetComponentsInChildren(true, allRenderersFound);

            // for each Renderer, search for the correct(closest) "parent Nilo controller script" to assign that renderer to it
            for (int i = 0; i < allRenderersFound.Count; i++)
            {
                Renderer renderer = allRenderersFound[i];
                var NiloToonPerCharacterRenderControllerFound = renderer.transform.GetComponentInParent<NiloToonPerCharacterRenderController>();
                if(NiloToonPerCharacterRenderControllerFound)
                {
                    // clear per char script's allRenderers first for once only, add to HashSet to prevent re-clear
                    if (!clearedNiloControllers.Contains(NiloToonPerCharacterRenderControllerFound))
                    {
                        NiloToonPerCharacterRenderControllerFound.allRenderers.Clear();
                        clearedNiloControllers.Add(NiloToonPerCharacterRenderControllerFound);
                    }
                    NiloToonPerCharacterRenderControllerFound.allRenderers.Add(renderer); // this line Alloc GC, but List's internal array should be large enough, why GC is still happening?
                }
            }
        }
        
        private void CacheCurrentFrameCalculations()
        {
            //////////////////////////////////////////////////////////////////////////////////////
            // calculate and cache value for current frame's repeated API calls
            // to improve performance
            //////////////////////////////////////////////////////////////////////////////////////
            isHeadBoneTransformExist = headBoneTransform;
            if (isHeadBoneTransformExist)
            {
                finalFaceDirectionWS_Forward = GetFinalFaceDirectionWS(faceForwardDirection);
                finalFaceDirectionWS_Up = GetFinalFaceDirectionWS(faceUpDirection);
            }
            characterBoundCenter = GetCharacterBoundCenter();
            perspectiveRemovalCenter = GetPerspectiveRemovalCenter();

            perCharacterTintColorAsVector = perCharacterTintColor;
            perCharacterAddColorAsVector = perCharacterAddColor;
            PerCharEffectLerpColorAsVector = GetPerCharEffectLerpColor();
            PerCharEffectRimColorAsVector = GetPerCharEffectRimColor();

            characterAreaColorFillColorAsVector = characterAreaColorFillColor;
            extraThickOutlineColorAsVector = extraThickOutlineColor;
            PerCharBaseColorTintAsVector = GetPerCharBaseColorTint();
            PerCharOutlineColorTintAsVector = GetPerCharOutlineColorTint();
            perCharacterOutlineEffectLerpColorAsVector = perCharacterOutlineEffectLerpColor;

            PerCharacterBaseMapOverrideTintColorAsVector = perCharacterBaseMapOverrideTintColor;
        }
        private void UpdateRequireMaterialSetFlags()
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // check if UpdateMaterial() or UpdateMaterialPropertyBlock() require to call expensive Material.SetXXX(), Material.EnableKeyword() or Material.SetShaderPassEnabled()
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            if (forceMaterialIgnoreCacheAndUpdate)
            {
                renderCharacter_RequireMaterialSet = true;
                controlledByNiloToonPerCharacterRenderController_RequireMaterialSet = true;
                characterID_RequireMaterialSet = true;
                perCharacterTintColor_RequireMaterialSet = true;
                perCharacterAddColor_RequireMaterialSet = true;
                perCharacterDesaturationUsage_RequireMaterialSet = true;
                PerCharEffectLerpColor_RequireMaterialSet = true;
                PerCharEffectRimColor_RequireMaterialSet = true;
                PerCharEffectRimSharpnessPower_RequireMaterialSet = true;
                FinalFaceForwardDirectionWS_RequireMaterialSet = true; // Converted to global array
                FinalFaceUpDirectionWS_RequireMaterialSet = true; // Converted to global array
                faceNormalFixAmount_RequireMaterialSet = true;
                fixFaceNormalUseFlattenOrProxySphereMethod_RequireMaterialSet = true;
                characterAreaColorFillEnabled_RequireMaterialSet = true;
                characterAreaColorFillColor_RequireMaterialSet = true;
                characterAreaColorFillTexture_RequireMaterialSet = true;
                characterAreaColorFillTextureUVIndex_RequireMaterialSet = true;
                characterAreaColorFillTextureUVTilingOffset_RequireMaterialSet = true;
                characterAreaColorFillTextureUVScrollSpeed_RequireMaterialSet = true;
                characterAreaColorFillRendersVisibleArea_RequireMaterialSet = true;
                characterAreaColorFillRendersBlockedArea_RequireMaterialSet = true;
                extraThickOutlineEnabled_RequireMaterialSet = true;
                extraThickOutlineColor_RequireMaterialSet = true;
                extraThickOutlineWidth_RequireMaterialSet = true;
                extraThickOutlineMaximumFinalWidth_RequireMaterialSet = true;
                extraThickOutlineViewSpacePosOffset_RequireMaterialSet = true;
                ExtraThickOutlineZTest_RequireMaterialSet = true;
                extraThickOutlineZOffset_RequireMaterialSet = true;
                extraThickOutlineZWrite_RequireMaterialSet = true;
                extraThickOutlineWriteIntoDepthTexture_RequireMaterialSet = true;
                CharacterBoundCenter_RequireMaterialSet = true; // Converted to global array
                CharacterBoundRadius_RequireMaterialSet = true;
                PerspectiveRemovalAmount_RequireMaterialSet = true;
                PerspectiveRemovalRadius_RequireMaterialSet = true;
                PerspectiveRemovalStartHeight_RequireMaterialSet = true;
                PerspectiveRemovalEndHeight_RequireMaterialSet = true;
                PerspectiveRemovalCenter_RequireMaterialSet = true; // Converted to global array
                PerCharBaseColorTint_RequireMaterialSet = true;
                perCharacterOutlineWidthMultiply_RequireMaterialSet = true;
                PerCharOutlineColorTint_RequireMaterialSet = true;
                perCharacterOutlineEffectLerpColor_RequireMaterialSet = true;
                DitherFadeoutAmount_RequireMaterialSet = true;
                DitherNormalScaleFix_RequireMaterialSet = true;
                DissolveAmount_RequireMaterialSet = true;
                DissolveMode_RequireMaterialSet = true;
                DissolveThresholdMap_RequireMaterialSet = true;
                DissolveBorderRange_RequireMaterialSet = true;
                DissolveThresholdMapTilingX_RequireMaterialSet = true;
                DissolveThresholdMapTilingY_RequireMaterialSet = true;
                DissolveNoiseStrength_RequireMaterialSet = true;
                DissolveBorderTintColor_RequireMaterialSet = true;
                PerCharacterBaseMapOverrideAmount_RequireMaterialSet = true;
                PerCharacterBaseMapOverrideMap_RequireMaterialSet = true;
                PerCharacterBaseMapOverrideTilingOffset_RequireMaterialSet = true;
                perCharacterBaseMapOverrideUVScrollSpeed_RequireMaterialSet = true;
                PerCharacterBaseMapOverrideTintColor_RequireMaterialSet = true;
                perCharacterBaseMapOverrideUVIndex_RequireMaterialSet = true;
                perCharacterBaseMapOverrideBlendMode_RequireMaterialSet = true;
                ReceiveAverageURPShadowMap_RequireMaterialSet = true;
                ReceiveStandardURPShadowMap_RequireMaterialSet = true;
                ReceiveNiloToonSelfShadowMap_RequireMaterialSet = true;
                perCharacterZOffset_RequireMaterialSet = true;
                shouldRenderSelfOutline_RequireMaterialSet = true;

                shouldRenderSelfOutline_RequireSetShaderPassEnabledCall = true;
                ShouldRenderNiloToonCharacterAreaColorFillPass_RequireSetShaderPassEnabledCall = true;
                ShouldRenderNiloToonExtraThickOutlinePass_RequireSetShaderPassEnabledCall = true;
                ShouldEnableDitherFadeOut_RequireKeywordChangeCall = true;
                ShouldEnableDissolve_RequireKeywordChangeCall = true;
                ShouldEnablePerCharacterBaseMapOverride_RequireKeywordChangeCall = true;
                ShouldEnableDepthTextureRimLightAndShadow_RequireKeywordChangeCall = true;
                allowRenderShadowCasterPass_RequireSetShaderPassEnabledCall = true;
                allowRenderDepthOnlyAndDepthNormalsPass_RequireSetShaderPassEnabledCall = true;
                allowRenderNiloToonSelfShadowCasterPass_RequireSetShaderPassEnabledCall = true;
                allowRenderNiloToonPrepassBufferPass_RequireSetShaderPassEnabledCall = true;

                // force update all and exit
                return;
            }

            // if no need to force update all property,
            // we find out which property is dirty and only do the expensive material update for these dirty property
            renderCharacter_RequireMaterialSet = renderCharacter_Cache != renderCharacter;
            controlledByNiloToonPerCharacterRenderController_RequireMaterialSet = !controlledByNiloToonPerCharacterRenderController_Cache.HasValue; // a special property, it is always true, unchanged after set
            characterID_RequireMaterialSet = characterID_Cache != characterID;
            perCharacterTintColor_RequireMaterialSet = perCharacterTintColor_Cache != perCharacterTintColor;
            perCharacterAddColor_RequireMaterialSet = perCharacterAddColor_Cache != perCharacterAddColor;
            perCharacterDesaturationUsage_RequireMaterialSet = perCharacterDesaturationUsage_Cache != perCharacterDesaturationUsage;
            PerCharEffectLerpColor_RequireMaterialSet = PerCharEffectLerpColor_Cache != PerCharEffectLerpColorAsVector;
            PerCharEffectRimColor_RequireMaterialSet = PerCharEffectRimColor_Cache != PerCharEffectRimColorAsVector;
            PerCharEffectRimSharpnessPower_RequireMaterialSet = PerCharEffectRimSharpnessPower_Cache != perCharacterRimLightSharpnessPower;
            
            // Converted to global array
            if (isHeadBoneTransformExist)
            {
                FinalFaceForwardDirectionWS_RequireMaterialSet = FinalFaceForwardDirectionWS_Cache != finalFaceDirectionWS_Forward;
                FinalFaceUpDirectionWS_RequireMaterialSet = FinalFaceUpDirectionWS_Cache != finalFaceDirectionWS_Up;
            }
            else
            {
                FinalFaceForwardDirectionWS_RequireMaterialSet = false;
                FinalFaceUpDirectionWS_RequireMaterialSet = false;
            }
            
            faceNormalFixAmount_RequireMaterialSet = faceNormalFixAmount_Cache != GetFaceNormalFixAmount();
            fixFaceNormalUseFlattenOrProxySphereMethod_RequireMaterialSet = fixFaceNormalUseFlattenOrProxySphereMethod_Cache != fixFaceNormalUseFlattenOrProxySphereMethod;
            characterAreaColorFillEnabled_RequireMaterialSet = characterAreaColorFillEnabled_Cache != GetShouldRenderNiloToonCharacterAreaColorFillPass();
            characterAreaColorFillColor_RequireMaterialSet = characterAreaColorFillColor_Cache != characterAreaColorFillColor;
            characterAreaColorFillTexture_RequireMaterialSet = characterAreaColorFillTexture_Cache != characterAreaColorFillTexture;
            characterAreaColorFillTextureUVIndex_RequireMaterialSet = characterAreaColorFillTextureUVIndex_Cache != characterAreaColorFillTextureUVIndex;
            characterAreaColorFillTextureUVTilingOffset_RequireMaterialSet = characterAreaColorFillTextureUVTilingOffset_Cache != GetCharacterAreaColorFillFinalUVTilingOffset();
            characterAreaColorFillTextureUVScrollSpeed_RequireMaterialSet = characterAreaColorFillTextureUVScrollSpeed_Cache != characterAreaColorFillTextureUVScrollSpeed;
            characterAreaColorFillRendersVisibleArea_RequireMaterialSet = characterAreaColorFillRendersVisibleArea_Cache != characterAreaColorFillRendersVisibleArea;
            characterAreaColorFillRendersBlockedArea_RequireMaterialSet = characterAreaColorFillRendersBlockedArea_Cache != characterAreaColorFillRendersBlockedArea;
            extraThickOutlineEnabled_RequireMaterialSet = extraThickOutlineEnabled_Cache != GetShouldRenderNiloToonExtraThickOutlinePass();
            extraThickOutlineColor_RequireMaterialSet = extraThickOutlineColor_Cache != extraThickOutlineColor;
            extraThickOutlineWidth_RequireMaterialSet = extraThickOutlineWidth_Cache != extraThickOutlineWidth;
            extraThickOutlineMaximumFinalWidth_RequireMaterialSet = extraThickOutlineMaximumFinalWidth_Cache != extraThickOutlineMaximumFinalWidth;
            extraThickOutlineViewSpacePosOffset_RequireMaterialSet = extraThickOutlineViewSpacePosOffset_Cache != extraThickOutlineViewSpacePosOffset;
            ExtraThickOutlineZTest_RequireMaterialSet = ExtraThickOutlineZTest_Cache != GetExtraThickOutlineZTest();
            extraThickOutlineZOffset_RequireMaterialSet = extraThickOutlineZOffset_Cache != extraThickOutlineZOffset;
            extraThickOutlineZWrite_RequireMaterialSet = extraThickOutlineZWrite_Cache != GetExtraThickOutlineZWrite();
            extraThickOutlineWriteIntoDepthTexture_RequireMaterialSet = extraThickOutlineWriteIntoDepthTexture_Cache != extraThickOutlineWriteIntoDepthTexture;
            CharacterBoundCenter_RequireMaterialSet = CharacterBoundCenter_Cache != characterBoundCenter; // Converted to global array
            CharacterBoundRadius_RequireMaterialSet = CharacterBoundRadius_Cache != GetCharacterBoundRadius();
            PerspectiveRemovalAmount_RequireMaterialSet = PerspectiveRemovalAmount_Cache != GetPerspectiveRemovalAmount();
            PerspectiveRemovalRadius_RequireMaterialSet = PerspectiveRemovalRadius_Cache != GetPerspectiveRemovalRadius();
            PerspectiveRemovalStartHeight_RequireMaterialSet = PerspectiveRemovalStartHeight_Cache != GetPerspectiveRemovalStartHeight();
            PerspectiveRemovalEndHeight_RequireMaterialSet = PerspectiveRemovalEndHeight_Cache != GetPerspectiveRemovalEndHeight();
            PerspectiveRemovalCenter_RequireMaterialSet = PerspectiveRemovalCenter_Cache != perspectiveRemovalCenter; // Converted to global array
            PerCharBaseColorTint_RequireMaterialSet = PerCharBaseColorTint_Cache != GetPerCharBaseColorTint();
            perCharacterOutlineWidthMultiply_RequireMaterialSet = perCharacterOutlineWidthMultiply_Cache != perCharacterOutlineWidthMultiply;
            PerCharOutlineColorTint_RequireMaterialSet = PerCharOutlineColorTint_Cache != GetPerCharOutlineColorTint();
            perCharacterOutlineEffectLerpColor_RequireMaterialSet = perCharacterOutlineEffectLerpColor_Cache != perCharacterOutlineEffectLerpColor;
            DitherFadeoutAmount_RequireMaterialSet = DitherFadeoutAmount_Cache != GetDitherFadeoutAmount();
            DitherNormalScaleFix_RequireMaterialSet = DitherNormalScaleFix_Cache != ditherNormalScaleFix;
            DissolveAmount_RequireMaterialSet = DissolveAmount_Cache != dissolveAmount;
            DissolveMode_RequireMaterialSet = DissolveMode_Cache != GetDissolveModeInt();
            DissolveThresholdMap_RequireMaterialSet = DissolveThresholdMap_Cache != dissolveThresholdMap;
            DissolveBorderRange_RequireMaterialSet = DissolveBorderRange_Cache != dissolveBorderRange;
            DissolveThresholdMapTilingX_RequireMaterialSet = DissolveThresholdMapTilingX_Cache != dissolveThresholdMapTilingX;
            DissolveThresholdMapTilingY_RequireMaterialSet = DissolveThresholdMapTilingY_Cache != dissolveThresholdMapTilingY;
            DissolveNoiseStrength_RequireMaterialSet = DissolveNoiseStrength_Cache != dissolveNoiseStrength;
            DissolveBorderTintColor_RequireMaterialSet = DissolveBorderTintColor_Cache != dissolveBorderTintColor;
            PerCharacterBaseMapOverrideAmount_RequireMaterialSet = PerCharacterBaseMapOverrideAmount_Cache != perCharacterBaseMapOverrideAmount;
            PerCharacterBaseMapOverrideMap_RequireMaterialSet = PerCharacterBaseMapOverrideMap_Cache != perCharacterBaseMapOverrideMap;
            PerCharacterBaseMapOverrideTilingOffset_RequireMaterialSet = PerCharacterBaseMapOverrideTilingOffset_Cache != GetPerCharacterBaseMapOverrideFinalUVTilingOffset();
            perCharacterBaseMapOverrideUVScrollSpeed_RequireMaterialSet = perCharacterBaseMapOverrideUVScrollSpeed_Cache != perCharacterBaseMapOverrideUVScrollSpeed;
            PerCharacterBaseMapOverrideTintColor_RequireMaterialSet = PerCharacterBaseMapOverrideTintColor_Cache != perCharacterBaseMapOverrideTintColor;
            perCharacterBaseMapOverrideUVIndex_RequireMaterialSet = perCharacterBaseMapOverrideUVIndex_Cache != perCharacterBaseMapOverrideUVIndex;
            perCharacterBaseMapOverrideBlendMode_RequireMaterialSet = perCharacterBaseMapOverrideBlendMode_Cache != perCharacterBaseMapOverrideBlendMode;
            ReceiveAverageURPShadowMap_RequireMaterialSet = ReceiveAverageURPShadowMap_Cache != receiveAverageURPShadowMap;
            ReceiveStandardURPShadowMap_RequireMaterialSet = ReceiveStandardURPShadowMap_Cache != receiveStandardURPShadowMap;
            ReceiveNiloToonSelfShadowMap_RequireMaterialSet = ReceiveNiloToonSelfShadowMap_Cache != receiveNiloToonSelfShadowMap;
            perCharacterZOffset_RequireMaterialSet = perCharacterZOffset_Cache != perCharacterZOffset;
            shouldRenderSelfOutline_RequireMaterialSet = shouldRenderSelfOutline_Cache != shouldRenderSelfOutline;

            shouldRenderSelfOutline_RequireSetShaderPassEnabledCall = shouldRenderSelfOutline_Cache != shouldRenderSelfOutline;
            ShouldRenderNiloToonCharacterAreaColorFillPass_RequireSetShaderPassEnabledCall = ShouldRenderNiloToonCharacterAreaColorFillPass_Cache != GetShouldRenderNiloToonCharacterAreaColorFillPass();
            ShouldRenderNiloToonExtraThickOutlinePass_RequireSetShaderPassEnabledCall = ShouldRenderNiloToonExtraThickOutlinePass_Cache != GetShouldRenderNiloToonExtraThickOutlinePass();
            ShouldEnableDitherFadeOut_RequireKeywordChangeCall = ShouldEnableDitherFadeOut_Cache != GetShouldEnableDitherFadeOut();
            ShouldEnableDissolve_RequireKeywordChangeCall = ShouldEnableDissolve_Cache != GetShouldEnableDissolve();
            ShouldEnablePerCharacterBaseMapOverride_RequireKeywordChangeCall = ShouldEnablePerCharacterBaseMapOverride_Cache != GetShouldEnablePerCharacterBaseMapOverride();
            ShouldEnableDepthTextureRimLightAndShadow_RequireKeywordChangeCall = ShouldEnableDepthTextureRimLightAndShadow_Cache != GetShouldEnableDepthTextureRimLightAndShadow();
            allowRenderShadowCasterPass_RequireSetShaderPassEnabledCall = allowRenderShadowCasterPass_Cache != allowRenderShadowCasterPass;
            allowRenderDepthOnlyAndDepthNormalsPass_RequireSetShaderPassEnabledCall = allowRenderDepthOnlyAndDepthNormalsPass_Cache != allowRenderDepthOnlyAndDepthNormalsPass;
            allowRenderNiloToonSelfShadowCasterPass_RequireSetShaderPassEnabledCall = allowRenderNiloToonSelfShadowCasterPass_Cache != allowRenderNiloToonSelfShadowCasterPass;
            allowRenderNiloToonPrepassBufferPass_RequireSetShaderPassEnabledCall = allowRenderNiloToonPrepassBufferPass_Cache != allowRenderNiloToonPrepassBufferPass;
        }
        private void CacheProprtiesForNextFrameOptimizationCheck()
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // save this frame's value in cache, for next frame's optimization
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            renderCharacter_Cache = renderCharacter;
            controlledByNiloToonPerCharacterRenderController_Cache = true;
            characterID_Cache = characterID;
            perCharacterTintColor_Cache = perCharacterTintColor;
            perCharacterAddColor_Cache = perCharacterAddColor;
            perCharacterDesaturationUsage_Cache = perCharacterDesaturationUsage;
            PerCharEffectLerpColor_Cache = GetPerCharEffectLerpColor();
            PerCharEffectRimColor_Cache = GetPerCharEffectRimColor();
            PerCharEffectRimSharpnessPower_Cache = perCharacterRimLightSharpnessPower;
            
            // Converted to global array
            if (isHeadBoneTransformExist)
            {
                FinalFaceForwardDirectionWS_Cache = finalFaceDirectionWS_Forward;
                FinalFaceUpDirectionWS_Cache = finalFaceDirectionWS_Up;
            }

            faceNormalFixAmount_Cache = GetFaceNormalFixAmount();
            fixFaceNormalUseFlattenOrProxySphereMethod_Cache = fixFaceNormalUseFlattenOrProxySphereMethod;
            characterAreaColorFillEnabled_Cache = GetShouldRenderNiloToonCharacterAreaColorFillPass();
            characterAreaColorFillColor_Cache = characterAreaColorFillColor;
            characterAreaColorFillTexture_Cache = characterAreaColorFillTexture;
            characterAreaColorFillTextureUVIndex_Cache = characterAreaColorFillTextureUVIndex;
            characterAreaColorFillTextureUVTilingOffset_Cache = GetCharacterAreaColorFillFinalUVTilingOffset();
            characterAreaColorFillTextureUVScrollSpeed_Cache = characterAreaColorFillTextureUVScrollSpeed;
            characterAreaColorFillRendersVisibleArea_Cache = characterAreaColorFillRendersVisibleArea;
            characterAreaColorFillRendersBlockedArea_Cache = characterAreaColorFillRendersBlockedArea;
            extraThickOutlineEnabled_Cache = GetShouldRenderNiloToonExtraThickOutlinePass();
            extraThickOutlineColor_Cache = extraThickOutlineColor;
            extraThickOutlineWidth_Cache = extraThickOutlineWidth;
            extraThickOutlineMaximumFinalWidth_Cache = extraThickOutlineMaximumFinalWidth;
            extraThickOutlineViewSpacePosOffset_Cache = extraThickOutlineViewSpacePosOffset;
            ExtraThickOutlineZTest_Cache = GetExtraThickOutlineZTest();
            extraThickOutlineZOffset_Cache = extraThickOutlineZOffset;
            extraThickOutlineZWrite_Cache = GetExtraThickOutlineZWrite();
            extraThickOutlineWriteIntoDepthTexture_Cache = extraThickOutlineWriteIntoDepthTexture;
            CharacterBoundCenter_Cache = characterBoundCenter; // Converted to global array
            CharacterBoundRadius_Cache = GetCharacterBoundRadius();
            PerspectiveRemovalAmount_Cache = GetPerspectiveRemovalAmount();
            PerspectiveRemovalRadius_Cache = GetPerspectiveRemovalRadius();
            PerspectiveRemovalStartHeight_Cache = GetPerspectiveRemovalStartHeight();
            PerspectiveRemovalEndHeight_Cache = GetPerspectiveRemovalEndHeight();
            PerspectiveRemovalCenter_Cache = perspectiveRemovalCenter; // Converted to global array
            PerCharBaseColorTint_Cache = GetPerCharBaseColorTint();
            perCharacterOutlineWidthMultiply_Cache = perCharacterOutlineWidthMultiply;
            PerCharOutlineColorTint_Cache = GetPerCharOutlineColorTint();
            perCharacterOutlineEffectLerpColor_Cache = perCharacterOutlineEffectLerpColor;
            DitherFadeoutAmount_Cache = GetDitherFadeoutAmount();
            DitherNormalScaleFix_Cache = ditherNormalScaleFix;
            DissolveAmount_Cache = dissolveAmount;
            DissolveMode_Cache = GetDissolveModeInt();
            DissolveThresholdMap_Cache = dissolveThresholdMap;
            DissolveBorderRange_Cache = dissolveBorderRange;
            DissolveThresholdMapTilingX_Cache = dissolveThresholdMapTilingX;
            DissolveThresholdMapTilingY_Cache = dissolveThresholdMapTilingY;
            DissolveNoiseStrength_Cache = dissolveNoiseStrength;
            DissolveBorderTintColor_Cache = dissolveBorderTintColor;
            PerCharacterBaseMapOverrideMap_Cache = perCharacterBaseMapOverrideMap;
            PerCharacterBaseMapOverrideAmount_Cache = perCharacterBaseMapOverrideAmount;
            PerCharacterBaseMapOverrideTilingOffset_Cache = GetPerCharacterBaseMapOverrideFinalUVTilingOffset();
            perCharacterBaseMapOverrideUVScrollSpeed_Cache = perCharacterBaseMapOverrideUVScrollSpeed;
            PerCharacterBaseMapOverrideTintColor_Cache = perCharacterBaseMapOverrideTintColor;
            perCharacterBaseMapOverrideUVIndex_Cache = perCharacterBaseMapOverrideUVIndex;
            perCharacterBaseMapOverrideBlendMode_Cache = perCharacterBaseMapOverrideBlendMode;
            ReceiveAverageURPShadowMap_Cache = receiveAverageURPShadowMap;
            ReceiveStandardURPShadowMap_Cache = receiveStandardURPShadowMap;
            ReceiveNiloToonSelfShadowMap_Cache = receiveNiloToonSelfShadowMap;
            perCharacterZOffset_Cache = perCharacterZOffset;

            shouldRenderSelfOutline_Cache = shouldRenderSelfOutline;
            ShouldRenderNiloToonCharacterAreaColorFillPass_Cache = GetShouldRenderNiloToonCharacterAreaColorFillPass();
            ShouldRenderNiloToonExtraThickOutlinePass_Cache = GetShouldRenderNiloToonExtraThickOutlinePass();
            ShouldEnableDitherFadeOut_Cache = GetShouldEnableDitherFadeOut();
            ShouldEnableDissolve_Cache = GetShouldEnableDissolve();
            ShouldEnablePerCharacterBaseMapOverride_Cache = GetShouldEnablePerCharacterBaseMapOverride();
            ShouldEnableDepthTextureRimLightAndShadow_Cache = GetShouldEnableDepthTextureRimLightAndShadow();
            allowRenderShadowCasterPass_Cache = allowRenderShadowCasterPass;
            allowRenderDepthOnlyAndDepthNormalsPass_Cache = allowRenderDepthOnlyAndDepthNormalsPass;
            allowRenderNiloToonSelfShadowCasterPass_Cache = allowRenderNiloToonSelfShadowCasterPass;
            allowRenderNiloToonPrepassBufferPass_Cache = allowRenderNiloToonPrepassBufferPass;
        }
        public float GetCharacterBoundRadius()
        {
            // static radius sphere is better than dyanmic calculated radius sphere
            // because it is visually more stable
            return characterBoundRadius;
        }

        public Vector3 GetPerspectiveRemovalCenter()
        {
            // overrider
            if (ExternalRenderOverrider && ExternalRenderOverrider.ShouldOverridePerspectiveRemoval())
            {
                return ExternalRenderOverrider.GetPerspectiveRemovalOverridedCenterPosWS();
            }

            return GetSelfPerspectiveRemovalCenter();
        }
        public Vector3 GetSelfPerspectiveRemovalCenter()
        {
            // we mainly want to remove face's perspective, so use head as center is the preferred choice
            if (isHeadBoneTransformExist)
                return headBoneTransform.position;

            // fallback
            return characterBoundCenter;
        }
        public Vector3 GetCharacterBoundCenter()
        {
            if (useCustomCharacterBoundCenter && customCharacterBoundCenter)
            {
                return customCharacterBoundCenter.position;
            }
            else
            {
                // when user click auto setup button, allRenderers will become empty for that frame, we call this to ensure allRenderers is correct within the same frame
                if (allRenderers.Count == 0)
                    RefillAllRenderers();

                if (allRenderers.Count == 0)
                {
                    // disabled warning log, due to warning spam when user is creating a procedual character
                    //Debug.LogWarning($"No NiloToon shader detected inside {gameObject.name}, Did you forget to change character's material to NiloToon's character shader?", this);
                    return transform.position + Vector3.up; // a guess position of character center when we don't have any info, assuming transform.position is between 2 foots(on ground center).
                }

                // find center in realtime as fallback method (very slow)
                Bounds bound = new Bounds();

                int startIndex = 0;
                while (startIndex < allRenderers.Count)
                {
                    Renderer firstRenderer = allRenderers[startIndex];
                    if(firstRenderer != null)
                    {
                        bound = new Bounds(firstRenderer.bounds.center, firstRenderer.bounds.size);
                        break;
                    }
                    startIndex++;
                }

                for (int i = startIndex + 1; i < allRenderers.Count; i++)
                {
                    Renderer renderer = allRenderers[i];
                    if(renderer != null)
                    {
                        bound.Encapsulate(renderer.bounds);
                    }
                }

                return bound.center;
            }
        }

        // copy and edit of https://forum.unity.com/threads/debug-drawarrow.85980/
        private static void DrawArrow(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.05f, float arrowHeadAngle = 22.5f)
        {
            direction *= 0.5f;
            Gizmos.color = color;
            Gizmos.DrawRay(pos, direction);

            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
            Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
            Gizmos.DrawRay(pos + direction, left * arrowHeadLength);

            Vector3 up = Quaternion.LookRotation(direction) * Quaternion.Euler(+arrowHeadAngle, 180, 0) * new Vector3(0, 0, 1);
            Vector3 down = Quaternion.LookRotation(direction) * Quaternion.Euler(-arrowHeadAngle, 180, 0) * new Vector3(0, 0, 1);
            Gizmos.DrawRay(pos + direction, up * arrowHeadLength);
            Gizmos.DrawRay(pos + direction, down * arrowHeadLength);
        }

        static readonly int _RenderCharacter = Shader.PropertyToID("_RenderCharacter");
        static readonly int _ControlledByNiloToonPerCharacterRenderController = Shader.PropertyToID("_ControlledByNiloToonPerCharacterRenderController");
        static readonly int _CharacterID = Shader.PropertyToID("_CharacterID");
        static readonly int _AverageShadowMapRTSampleIndex = Shader.PropertyToID("_AverageShadowMapRTSampleIndex"); // Only for NiloToon 0.13.8 shader support
        static readonly int _PerCharEffectTintColor = Shader.PropertyToID("_PerCharEffectTintColor");
        static readonly int _PerCharEffectAddColor = Shader.PropertyToID("_PerCharEffectAddColor");
        static readonly int _PerCharEffectDesaturatePercentage = Shader.PropertyToID("_PerCharEffectDesaturatePercentage");
        static readonly int _PerCharEffectLerpColor = Shader.PropertyToID("_PerCharEffectLerpColor");
        static readonly int _PerCharEffectRimColor = Shader.PropertyToID("_PerCharEffectRimColor");
        static readonly int _PerCharEffectRimSharpnessPower = Shader.PropertyToID("_PerCharEffectRimSharpnessPower");
        static readonly int _FaceForwardDirection = Shader.PropertyToID("_FaceForwardDirection"); // Converted to global array
        static readonly int _FaceUpDirection = Shader.PropertyToID("_FaceUpDirection"); // Converted to global array
        static readonly int _FixFaceNormalAmount = Shader.PropertyToID("_FixFaceNormalAmount");
        static readonly int _FixFaceNormalUseFlattenOrProxySphereMethod = Shader.PropertyToID("_FixFaceNormalUseFlattenOrProxySphereMethod");
        static readonly int _CharacterAreaColorFillEnabled = Shader.PropertyToID("_CharacterAreaColorFillEnabled");
        static readonly int _CharacterAreaColorFillColor = Shader.PropertyToID("_CharacterAreaColorFillColor");
        static readonly int _CharacterAreaColorFillTexture = Shader.PropertyToID("_CharacterAreaColorFillTexture");
        static readonly int _CharacterAreaColorFillTextureUVIndex = Shader.PropertyToID("_CharacterAreaColorFillTextureUVIndex");
        static readonly int _CharacterAreaColorFillTextureUVTilingOffset = Shader.PropertyToID("_CharacterAreaColorFillTextureUVTilingOffset");
        static readonly int _CharacterAreaColorFillTextureUVScrollSpeed = Shader.PropertyToID("_CharacterAreaColorFillTextureUVScrollSpeed");
        static readonly int _CharacterAreaColorFillRendersVisibleArea = Shader.PropertyToID("_CharacterAreaColorFillRendersVisibleArea");
        static readonly int _CharacterAreaColorFillRendersBlockedArea = Shader.PropertyToID("_CharacterAreaColorFillRendersBlockedArea");
        static readonly int _ExtraThickOutlineEnabled = Shader.PropertyToID("_ExtraThickOutlineEnabled");
        static readonly int _ExtraThickOutlineColor = Shader.PropertyToID("_ExtraThickOutlineColor");
        static readonly int _ExtraThickOutlineWidth = Shader.PropertyToID("_ExtraThickOutlineWidth");
        static readonly int _ExtraThickOutlineMaxFinalWidth = Shader.PropertyToID("_ExtraThickOutlineMaxFinalWidth");
        static readonly int _ExtraThickOutlineViewSpacePosOffset = Shader.PropertyToID("_ExtraThickOutlineViewSpacePosOffset");
        static readonly int _ExtraThickOutlineZTest = Shader.PropertyToID("_ExtraThickOutlineZTest");
        static readonly int _ExtraThickOutlineZOffset = Shader.PropertyToID("_ExtraThickOutlineZOffset");
        static readonly int _ExtraThickOutlineZWrite = Shader.PropertyToID("_ExtraThickOutlineZWrite");
        static readonly int _ExtraThickOutlineWriteIntoDepthTexture = Shader.PropertyToID("_ExtraThickOutlineWriteIntoDepthTexture");
        static readonly int _CharacterBoundCenterPosWS = Shader.PropertyToID("_CharacterBoundCenterPosWS"); // Converted to global array
        static readonly int _CharacterBoundRadius = Shader.PropertyToID("_CharacterBoundRadius");
        static readonly int _PerspectiveRemovalAmount = Shader.PropertyToID("_PerspectiveRemovalAmount");
        static readonly int _PerspectiveRemovalRadius = Shader.PropertyToID("_PerspectiveRemovalRadius");
        static readonly int _PerspectiveRemovalStartHeight = Shader.PropertyToID("_PerspectiveRemovalStartHeight");
        static readonly int _PerspectiveRemovalEndHeight = Shader.PropertyToID("_PerspectiveRemovalEndHeight");
        static readonly int _HeadBonePositionWS = Shader.PropertyToID("_HeadBonePositionWS"); // Converted to global array
        static readonly int _PerCharacterBaseColorTint = Shader.PropertyToID("_PerCharacterBaseColorTint");
        static readonly int _PerCharacterOutlineWidthMultiply = Shader.PropertyToID("_PerCharacterOutlineWidthMultiply");
        static readonly int _PerCharacterOutlineColorTint = Shader.PropertyToID("_PerCharacterOutlineColorTint");
        static readonly int _PerCharacterOutlineColorLerp = Shader.PropertyToID("_PerCharacterOutlineColorLerp");
        static readonly int _DitherFadeoutAmount = Shader.PropertyToID("_DitherFadeoutAmount");
        static readonly int _DitherFadeoutNormalScaleFix = Shader.PropertyToID("_DitherFadeoutNormalScaleFix");
        static readonly int _DissolveAmount = Shader.PropertyToID("_DissolveAmount");
        static readonly int _DissolveMode = Shader.PropertyToID("_DissolveMode");
        static readonly int _DissolveThresholdMap = Shader.PropertyToID("_DissolveThresholdMap");
        static readonly int _DissolveBorderRange = Shader.PropertyToID("_DissolveBorderRange");
        static readonly int _DissolveThresholdMapTilingX = Shader.PropertyToID("_DissolveThresholdMapTilingX");
        static readonly int _DissolveThresholdMapTilingY = Shader.PropertyToID("_DissolveThresholdMapTilingY");
        static readonly int _DissolveNoiseStrength = Shader.PropertyToID("_DissolveNoiseStrength");
        static readonly int _DissolveBorderTintColor = Shader.PropertyToID("_DissolveBorderTintColor");
        static readonly int _PerCharacterBaseMapOverrideAmount = Shader.PropertyToID("_PerCharacterBaseMapOverrideAmount");
        static readonly int _PerCharacterBaseMapOverrideMap = Shader.PropertyToID("_PerCharacterBaseMapOverrideMap");
        static readonly int _PerCharacterBaseMapOverrideTilingOffset = Shader.PropertyToID("_PerCharacterBaseMapOverrideTilingOffset");
        static readonly int _PerCharacterBaseMapOverrideUVScrollSpeed = Shader.PropertyToID("_PerCharacterBaseMapOverrideUVScrollSpeed");
        static readonly int _PerCharacterBaseMapOverrideTintColor = Shader.PropertyToID("_PerCharacterBaseMapOverrideTintColor");
        static readonly int _PerCharacterBaseMapOverrideUVIndex = Shader.PropertyToID("_PerCharacterBaseMapOverrideUVIndex");
        static readonly int _PerCharacterBaseMapOverrideBlendMode = Shader.PropertyToID("_PerCharacterBaseMapOverrideBlendMode");
        static readonly int _PerCharReceiveAverageURPShadowMap = Shader.PropertyToID("_PerCharReceiveAverageURPShadowMap");
        static readonly int _PerCharReceiveStandardURPShadowMap = Shader.PropertyToID("_PerCharReceiveStandardURPShadowMap");
        static readonly int _PerCharReceiveNiloToonSelfShadowMap = Shader.PropertyToID("_PerCharReceiveNiloToonSelfShadowMap");
        static readonly int _PerCharZOffset = Shader.PropertyToID("_PerCharZOffset");
        static readonly int _PerCharacterRenderOutline = Shader.PropertyToID("_PerCharacterRenderOutline");

        public Vector3 GetFinalFaceDirectionWS(TransformDirection direction)
        {
            switch (direction)
            {
                case TransformDirection.X:
                    return headBoneTransform.right;
                case TransformDirection.Y:
                    return headBoneTransform.up;
                case TransformDirection.Z:
                    return headBoneTransform.forward;
                case TransformDirection.negX:
                    return -headBoneTransform.right;
                case TransformDirection.negY:
                    return -headBoneTransform.up;
                case TransformDirection.negZ:
                    return -headBoneTransform.forward;
                default:
                    throw new NotImplementedException();
            }
        }

        Vector4 GetCharacterAreaColorFillFinalUVTilingOffset()
        {
            Vector2 tiling = characterAreaColorFillTextureUVTiling * characterAreaColorFillTextureUVTilingXY;

            return new Vector4(tiling.x, tiling.y, characterAreaColorFillTextureUVOffset.x, characterAreaColorFillTextureUVOffset.y);
        }

        Vector4 GetPerCharacterBaseMapOverrideFinalUVTilingOffset()
        {
            Vector2 tiling = perCharacterBaseMapOverrideTiling * perCharacterBaseMapOverrideUVTilingXY;

            return new Vector4(tiling.x, tiling.y, perCharacterBaseMapOverrideUVOffset.x, perCharacterBaseMapOverrideUVOffset.y);
        }
        
        float GetPerspectiveRemovalAmount()
        {
            // overrider
            if (ExternalRenderOverrider && ExternalRenderOverrider.ShouldOverridePerspectiveRemoval())
            {
                // overrider settings
                return ExternalRenderOverrider.GetPerspectiveRemovalOverridedAmount();
            }

            // self (XR check)
            if (disablePerspectiveRemovalInXR && XRSettings.isDeviceActive)
            {
                return 0; // disable in VR, because PerspectiveRemoval looks weird in VR when camera rotate a lot
            }

            // self
            return perspectiveRemovalAmount;
        }
        float GetPerspectiveRemovalRadius()
        {
            // overrider
            if (ExternalRenderOverrider && ExternalRenderOverrider.ShouldOverridePerspectiveRemoval())
                return Mathf.Max(0.01f, ExternalRenderOverrider.GetPerspectiveRemovalOverridedRadius()); // prevent /0

            // self
            return Mathf.Max(0.01f, perspectiveRemovalRadius); // prevent /0
        }
        float GetPerspectiveRemovalStartHeight()
        {
            // overrider
            if (ExternalRenderOverrider && ExternalRenderOverrider.ShouldOverridePerspectiveRemoval())
                return ExternalRenderOverrider.GetPerspectiveRemovalOverridedStartHeight();

            // self
            return perspectiveRemovalStartHeight;
        }
        float GetPerspectiveRemovalEndHeight()
        {
            // overrider
            if (ExternalRenderOverrider && ExternalRenderOverrider.ShouldOverridePerspectiveRemoval())
                return ExternalRenderOverrider.GetPerspectiveRemovalOverridedEndHeight();

            // self
            return perspectiveRemovalEndHeight;
        }

        Color GetPerCharEffectLerpColor()
        {
            return new Color(perCharacterLerpColor.r, perCharacterLerpColor.g, perCharacterLerpColor.b, perCharacterLerpUsage);
        }
        Color GetPerCharEffectRimColor()
        {
            return usePerCharacterRimLightIntensity ? perCharacterRimLightIntensity * perCharacterRimLightColor : Color.clear;
        }
        Color GetPerCharBaseColorTint()
        {
            return perCharacterBaseColorTint * perCharacterBaseColorMultiply;
        }
        Color GetPerCharOutlineColorTint()
        {
            return perCharacterOutlineColorTint * perCharacterOutlineEffectTintColor;
        }
        float GetDitherFadeoutAmount()
        {
            return 1 - ditherOpacity;
        }
        float GetFaceNormalFixAmount()
        {
            return isHeadBoneTransformExist ? faceNormalFixAmount : 0;
        }
        int GetDissolveModeInt()
        {
            int result = (int)dissolveMode;

            // decide what should be the default mode
            if(dissolveMode == DissolveMode.Default)
            {
                // use this as default because it has the closest type of result when compared to UV1. We don't want UV1 as default because it relys on model's UV
                result = (int)DissolveMode.WorldSpaceNoise; 
            }

            return result;
        }
        bool GetShouldRenderNiloToonCharacterAreaColorFillPass()
        {
            return shouldRenderCharacterAreaColorFill && characterAreaColorFillColor.a > 0;
        }
        bool GetShouldRenderNiloToonExtraThickOutlinePass()
        {
            return shouldRenderExtraThickOutline && extraThickOutlineColor.a > 0;
        }
        bool GetShouldEnableDitherFadeOut()
        {
            return ditherOpacity < 0.99f;
        }
        bool GetShouldEnableDissolve()
        {
            return dissolveAmount > 0.01f;
        }

        bool GetShouldEnablePerCharacterBaseMapOverride()
        {
            return perCharacterBaseMapOverrideAmount > 0.01f;
        }
        bool GetShouldEnableDepthTextureRimLightAndShadow()
        {
            return allowRenderDepthOnlyAndDepthNormalsPass;
        }

        int GetExtraThickOutlineZTest()
        {
            if (extraThickOutlineRendersVisibleArea && extraThickOutlineRendersBlockedArea)
                return (int)CompareFunction.Always;
            if (extraThickOutlineRendersVisibleArea && !extraThickOutlineRendersBlockedArea)
                return (int)CompareFunction.LessEqual;
            if (!extraThickOutlineRendersVisibleArea && extraThickOutlineRendersBlockedArea)
                return (int)CompareFunction.Greater;
            if (!extraThickOutlineRendersVisibleArea && !extraThickOutlineRendersBlockedArea)
                return (int)CompareFunction.Never;
            throw new NotImplementedException();
        }
        float GetExtraThickOutlineZWrite()
        {
            switch (extraThickOutlineZWriteMode)
            {
                case ExtraThickOutlineZWriteMode.Auto:
                    return extraThickOutlineColor.a >= 0.999f ? 1 : 0;
                case ExtraThickOutlineZWriteMode.On:
                    return 1;
                case ExtraThickOutlineZWriteMode.Off:
                    return 0;
                default:
                    throw new NotImplementedException();
            }      
        }

        Texture2D GetFinalSetTexturePerCharacterBaseMapOverrideMap()
        {
            if (!perCharacterBaseMapOverrideMap)
            {
                return DefaultWhiteTexture;
            }

            return perCharacterBaseMapOverrideMap;
        }
        Texture2D GetFinalSetTextureCharacterAreaColorFillTexture()
        {
            if (!characterAreaColorFillTexture)
            {
                return DefaultWhiteTexture;
            }

            return characterAreaColorFillTexture;
        }
        Texture2D GetFinalSetTextureDissolveThresholdMap()
        {
            if (!dissolveThresholdMap)
            {
                if (!defaultDissolveThresholdMap)
                {
                    // if user provide null texture, then we use our default texture.
                    // *Similar to UGUI's Image's default white texture
                    defaultDissolveThresholdMap = Resources.Load<Texture2D>("NiloToonDefaultDissolvePerlinNoise");
                }

                return defaultDissolveThresholdMap;
            }
            return dissolveThresholdMap;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // UpdateMaterial() & UpdateMaterialPropertyBlock() are two almost identical functions
        // *it is possible to use "dynamic" to make these 2 methods share identical code, but it is not a strong type
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void UpdateMaterial(Material input)
        {
            // if it is not a NiloToon material, we should not modify them
            //if (!input.shader.name.Contains("NiloToon")) return;

            // Please copy this section to UpdateMaterialPropertyBlock()
            //================================================================================================================================================
            // DONT IGNORE THIS: for material but not MPB, use SetVector instead of SetColor to make edit/play mode the same (don't want unity's auto gamma correction different for this Color)
            if (renderCharacter_RequireMaterialSet) input.SetFloat(_RenderCharacter, renderCharacter ? 1 : 0);
            if (controlledByNiloToonPerCharacterRenderController_RequireMaterialSet) input.SetFloat(_ControlledByNiloToonPerCharacterRenderController, 1);
            if (characterID_RequireMaterialSet)
            {
                input.SetInteger(_CharacterID, characterID);
                
                if(compatibleWithNiloToon13Shader)
                    input.SetInteger(_AverageShadowMapRTSampleIndex, characterID);
            }
            if (perCharacterTintColor_RequireMaterialSet) input.SetVector(_PerCharEffectTintColor, perCharacterTintColorAsVector);
            if (perCharacterAddColor_RequireMaterialSet) input.SetVector(_PerCharEffectAddColor, perCharacterAddColorAsVector);
            if (perCharacterDesaturationUsage_RequireMaterialSet) input.SetFloat(_PerCharEffectDesaturatePercentage, perCharacterDesaturationUsage);
            if (PerCharEffectLerpColor_RequireMaterialSet) input.SetVector(_PerCharEffectLerpColor, PerCharEffectLerpColorAsVector);
            if (PerCharEffectRimColor_RequireMaterialSet) input.SetVector(_PerCharEffectRimColor, PerCharEffectRimColorAsVector);
            if (PerCharEffectRimSharpnessPower_RequireMaterialSet) input.SetFloat(_PerCharEffectRimSharpnessPower, perCharacterRimLightSharpnessPower);
            if (isHeadBoneTransformExist)
            {
                if (compatibleWithNiloToon13Shader && FinalFaceForwardDirectionWS_RequireMaterialSet) input.SetVector(_FaceForwardDirection, finalFaceDirectionWS_Forward); // Converted to global array!
                if (compatibleWithNiloToon13Shader && FinalFaceUpDirectionWS_RequireMaterialSet) input.SetVector(_FaceUpDirection, finalFaceDirectionWS_Up); // Converted to global array!
                if (faceNormalFixAmount_RequireMaterialSet) input.SetFloat(_FixFaceNormalAmount, faceNormalFixAmount);
                if (fixFaceNormalUseFlattenOrProxySphereMethod_RequireMaterialSet) input.SetFloat(_FixFaceNormalUseFlattenOrProxySphereMethod, fixFaceNormalUseFlattenOrProxySphereMethod);
            }
            else
            {
                input.SetFloat(_FixFaceNormalAmount, 0);
            }
            if (characterAreaColorFillEnabled_RequireMaterialSet) input.SetFloat(_CharacterAreaColorFillEnabled, GetShouldRenderNiloToonCharacterAreaColorFillPass() ? 1 : 0);
            if (characterAreaColorFillColor_RequireMaterialSet) input.SetVector(_CharacterAreaColorFillColor, characterAreaColorFillColorAsVector);
            if (characterAreaColorFillTexture_RequireMaterialSet) input.SetTexture(_CharacterAreaColorFillTexture, GetFinalSetTextureCharacterAreaColorFillTexture());
            if (characterAreaColorFillTextureUVIndex_RequireMaterialSet) input.SetFloat(_CharacterAreaColorFillTextureUVIndex,(uint)characterAreaColorFillTextureUVIndex);
            if (characterAreaColorFillTextureUVTilingOffset_RequireMaterialSet) input.SetVector(_CharacterAreaColorFillTextureUVTilingOffset, GetCharacterAreaColorFillFinalUVTilingOffset());
            if (characterAreaColorFillTextureUVScrollSpeed_RequireMaterialSet) input.SetVector(_CharacterAreaColorFillTextureUVScrollSpeed, characterAreaColorFillTextureUVScrollSpeed);
            if (characterAreaColorFillRendersVisibleArea_RequireMaterialSet) input.SetFloat(_CharacterAreaColorFillRendersVisibleArea, characterAreaColorFillRendersVisibleArea ? 1 : 0);
            if (characterAreaColorFillRendersBlockedArea_RequireMaterialSet) input.SetFloat(_CharacterAreaColorFillRendersBlockedArea, characterAreaColorFillRendersBlockedArea ? 1 : 0);
            if (extraThickOutlineEnabled_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineEnabled, GetShouldRenderNiloToonExtraThickOutlinePass() ? 1 : 0);
            if (extraThickOutlineColor_RequireMaterialSet) input.SetVector(_ExtraThickOutlineColor, extraThickOutlineColorAsVector);
            if (extraThickOutlineWidth_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineWidth, extraThickOutlineWidth);
            if (extraThickOutlineMaximumFinalWidth_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineMaxFinalWidth, extraThickOutlineMaximumFinalWidth);
            if (extraThickOutlineViewSpacePosOffset_RequireMaterialSet) input.SetVector(_ExtraThickOutlineViewSpacePosOffset, extraThickOutlineViewSpacePosOffset);
            if (ExtraThickOutlineZTest_RequireMaterialSet) input.SetInteger(_ExtraThickOutlineZTest, GetExtraThickOutlineZTest());
            if (extraThickOutlineZOffset_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineZOffset, extraThickOutlineZOffset);
            if (extraThickOutlineZWrite_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineZWrite, GetExtraThickOutlineZWrite());
            if (extraThickOutlineWriteIntoDepthTexture_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineWriteIntoDepthTexture, extraThickOutlineWriteIntoDepthTexture ? 1 : 0);
            if (compatibleWithNiloToon13Shader && CharacterBoundCenter_RequireMaterialSet) input.SetVector(_CharacterBoundCenterPosWS, characterBoundCenter); // Converted to global array!
            if (CharacterBoundRadius_RequireMaterialSet) input.SetFloat(_CharacterBoundRadius, GetCharacterBoundRadius());
            if (PerspectiveRemovalAmount_RequireMaterialSet) input.SetFloat(_PerspectiveRemovalAmount, GetPerspectiveRemovalAmount());
            if (PerspectiveRemovalRadius_RequireMaterialSet) input.SetFloat(_PerspectiveRemovalRadius, GetPerspectiveRemovalRadius());
            if (PerspectiveRemovalStartHeight_RequireMaterialSet) input.SetFloat(_PerspectiveRemovalStartHeight, GetPerspectiveRemovalStartHeight());
            if (PerspectiveRemovalEndHeight_RequireMaterialSet) input.SetFloat(_PerspectiveRemovalEndHeight, GetPerspectiveRemovalEndHeight());
            if (compatibleWithNiloToon13Shader && PerspectiveRemovalCenter_RequireMaterialSet) input.SetVector(_HeadBonePositionWS, perspectiveRemovalCenter); // Converted to global array!
            if (PerCharBaseColorTint_RequireMaterialSet) input.SetVector(_PerCharacterBaseColorTint, PerCharBaseColorTintAsVector); // DONT IGNORE THIS: for material, use SetVector instead of SetColor to make edit/play mode the same (don't want unity's auto gamma correction different for this Color)
            if (perCharacterOutlineWidthMultiply_RequireMaterialSet) input.SetFloat(_PerCharacterOutlineWidthMultiply, perCharacterOutlineWidthMultiply);
            if (PerCharOutlineColorTint_RequireMaterialSet) input.SetVector(_PerCharacterOutlineColorTint, PerCharOutlineColorTintAsVector);
            if (perCharacterOutlineEffectLerpColor_RequireMaterialSet) input.SetVector(_PerCharacterOutlineColorLerp, perCharacterOutlineEffectLerpColorAsVector);
            if (DitherFadeoutAmount_RequireMaterialSet) input.SetFloat(_DitherFadeoutAmount, GetDitherFadeoutAmount()); // need to always set, no matter keyword "_NILOTOON_DITHER_FADEOUT" is on or off (pass 1-x to shader to make preview window still render when shader is using 0 as _DitherFadeoutAmount's default value)
            if (DitherNormalScaleFix_RequireMaterialSet) input.SetFloat(_DitherFadeoutNormalScaleFix, ditherNormalScaleFix);
            if (DissolveAmount_RequireMaterialSet) input.SetFloat(_DissolveAmount, dissolveAmount);
            if (DissolveMode_RequireMaterialSet) input.SetInteger(_DissolveMode, GetDissolveModeInt());
            if (DissolveThresholdMap_RequireMaterialSet) input.SetTexture(_DissolveThresholdMap, GetFinalSetTextureDissolveThresholdMap());
            if (DissolveBorderRange_RequireMaterialSet) input.SetFloat(_DissolveBorderRange, dissolveBorderRange);
            if (DissolveThresholdMapTilingX_RequireMaterialSet) input.SetFloat(_DissolveThresholdMapTilingX, dissolveThresholdMapTilingX);
            if (DissolveThresholdMapTilingY_RequireMaterialSet) input.SetFloat(_DissolveThresholdMapTilingY, dissolveThresholdMapTilingY);
            if (DissolveNoiseStrength_RequireMaterialSet) input.SetFloat(_DissolveNoiseStrength, dissolveNoiseStrength);
            if (DissolveBorderTintColor_RequireMaterialSet) input.SetVector(_DissolveBorderTintColor, dissolveBorderTintColor);
            if (PerCharacterBaseMapOverrideAmount_RequireMaterialSet) input.SetFloat(_PerCharacterBaseMapOverrideAmount, perCharacterBaseMapOverrideAmount);
            if (PerCharacterBaseMapOverrideMap_RequireMaterialSet) input.SetTexture(_PerCharacterBaseMapOverrideMap, GetFinalSetTexturePerCharacterBaseMapOverrideMap());
            if (PerCharacterBaseMapOverrideTilingOffset_RequireMaterialSet) input.SetVector(_PerCharacterBaseMapOverrideTilingOffset, GetPerCharacterBaseMapOverrideFinalUVTilingOffset());
            if (perCharacterBaseMapOverrideUVScrollSpeed_RequireMaterialSet) input.SetVector(_PerCharacterBaseMapOverrideUVScrollSpeed, perCharacterBaseMapOverrideUVScrollSpeed);
            if (PerCharacterBaseMapOverrideTintColor_RequireMaterialSet) input.SetVector(_PerCharacterBaseMapOverrideTintColor, PerCharacterBaseMapOverrideTintColorAsVector);
            if (perCharacterBaseMapOverrideUVIndex_RequireMaterialSet) input.SetFloat(_PerCharacterBaseMapOverrideUVIndex, (uint)perCharacterBaseMapOverrideUVIndex);
            if (perCharacterBaseMapOverrideBlendMode_RequireMaterialSet) input.SetFloat(_PerCharacterBaseMapOverrideBlendMode, (uint)perCharacterBaseMapOverrideBlendMode);
            if (ReceiveAverageURPShadowMap_RequireMaterialSet) input.SetFloat(_PerCharReceiveAverageURPShadowMap, receiveAverageURPShadowMap);
            if (ReceiveStandardURPShadowMap_RequireMaterialSet) input.SetFloat(_PerCharReceiveStandardURPShadowMap, receiveStandardURPShadowMap);
            if (ReceiveNiloToonSelfShadowMap_RequireMaterialSet) input.SetFloat(_PerCharReceiveNiloToonSelfShadowMap, receiveNiloToonSelfShadowMap);
            if (perCharacterZOffset_RequireMaterialSet) input.SetFloat(_PerCharZOffset, perCharacterZOffset);
            if (shouldRenderSelfOutline_RequireMaterialSet) input.SetFloat(_PerCharacterRenderOutline, shouldRenderSelfOutline ? 1 : 0);
            if (ShouldEnableDepthTextureRimLightAndShadow_RequireKeywordChangeCall) input.SetFloat("_NiloToonEnableDepthTextureRimLightAndShadow", GetShouldEnableDepthTextureRimLightAndShadow() ? 1 : 0);
            //================================================================================================================================================


            // API SetShaderPassEnabled(...) only exists in Material class, but not MaterialPropertyBlock class
            // so the following section will NOT exist in UpdateMaterialPropertyBlock(...) function
            //-------------------------------------------------------------------------------------------------------------
            // What is SetShaderPassEnabled()?
            // this API will find "LightMode" = "XXX" in your shader, and disable that pass if param "enabled" is false
            // https://docs.unity3d.com/ScriptReference/Material.SetShaderPassEnabled.html
            //-------------------------------------------------------------------------------------------------------------
            if (shouldRenderSelfOutline_RequireSetShaderPassEnabledCall) input.SetShaderPassEnabled("NiloToonOutline", shouldRenderSelfOutline);

            // optimization: auto skip rendering if we know that it will not affecting rendering result
            if (ShouldRenderNiloToonExtraThickOutlinePass_RequireSetShaderPassEnabledCall || ShouldRenderNiloToonCharacterAreaColorFillPass_RequireSetShaderPassEnabledCall)
            {
                bool shouldStencilFillPassEnable = GetShouldRenderNiloToonExtraThickOutlinePass() || GetShouldRenderNiloToonCharacterAreaColorFillPass();
                input.SetShaderPassEnabled("NiloToonCharacterAreaStencilBufferFill", shouldStencilFillPassEnable);
                
                input.SetShaderPassEnabled("NiloToonCharacterAreaColorFill", GetShouldRenderNiloToonCharacterAreaColorFillPass());
                input.SetShaderPassEnabled("NiloToonExtraThickOutline", GetShouldRenderNiloToonExtraThickOutlinePass());
            }
            if (ShouldEnableDitherFadeOut_RequireKeywordChangeCall) CoreUtils.SetKeyword(input, "_NILOTOON_DITHER_FADEOUT", GetShouldEnableDitherFadeOut());
            if (ShouldEnableDissolve_RequireKeywordChangeCall) CoreUtils.SetKeyword(input, "_NILOTOON_DISSOLVE", GetShouldEnableDissolve());
            if (ShouldEnablePerCharacterBaseMapOverride_RequireKeywordChangeCall) CoreUtils.SetKeyword(input, "_NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE", GetShouldEnablePerCharacterBaseMapOverride());

            // optimization: allow user to control render these pass or not, so user can turn these off for non-important/far-away characters
            // the first param equals to "LightMode" = "XXX"
            if (allowRenderShadowCasterPass_RequireSetShaderPassEnabledCall) input.SetShaderPassEnabled("ShadowCaster", allowRenderShadowCasterPass);
            if (allowRenderDepthOnlyAndDepthNormalsPass_RequireSetShaderPassEnabledCall) input.SetShaderPassEnabled("DepthOnly", allowRenderDepthOnlyAndDepthNormalsPass);
            if (allowRenderDepthOnlyAndDepthNormalsPass_RequireSetShaderPassEnabledCall) input.SetShaderPassEnabled("DepthNormals", allowRenderDepthOnlyAndDepthNormalsPass);
            if (allowRenderNiloToonSelfShadowCasterPass_RequireSetShaderPassEnabledCall) input.SetShaderPassEnabled("NiloToonSelfShadowCaster", allowRenderNiloToonSelfShadowCasterPass);
            if (allowRenderNiloToonPrepassBufferPass_RequireSetShaderPassEnabledCall) input.SetShaderPassEnabled("NiloToonPrepassBuffer", allowRenderNiloToonPrepassBufferPass);
        }
        void UpdateMaterialPropertyBlock(MaterialPropertyBlock input)
        {
            // Please copy from UpdateMaterial(), but keep "SetVector() for material" and "SetColor() for MPB" difference
            //================================================================================================================================================
            if (renderCharacter_RequireMaterialSet) input.SetFloat(_RenderCharacter, renderCharacter ? 1 : 0);
            if (controlledByNiloToonPerCharacterRenderController_RequireMaterialSet) input.SetFloat(_ControlledByNiloToonPerCharacterRenderController, 1);
            if (characterID_RequireMaterialSet)
            {
                input.SetInteger(_CharacterID, characterID);
                
                if(compatibleWithNiloToon13Shader)
                    input.SetInteger(_AverageShadowMapRTSampleIndex, characterID);
            }
            if (perCharacterTintColor_RequireMaterialSet) input.SetColor(_PerCharEffectTintColor, perCharacterTintColorAsVector); // DONT IGNORE THIS: for MPB, use SetColor instead of SetVector to make edit/play mode the same (don't want unity's auto gamma correction different for this Color)
            if (perCharacterAddColor_RequireMaterialSet) input.SetColor(_PerCharEffectAddColor, perCharacterAddColorAsVector); // DONT IGNORE THIS: for MPB, use SetColor instead of SetVector to make edit/play mode the same (don't want unity's auto gamma correction different for this Color)
            if (perCharacterDesaturationUsage_RequireMaterialSet) input.SetFloat(_PerCharEffectDesaturatePercentage, perCharacterDesaturationUsage);
            if (PerCharEffectLerpColor_RequireMaterialSet) input.SetColor(_PerCharEffectLerpColor, PerCharEffectLerpColorAsVector); // DONT IGNORE THIS: for MPB, use SetColor instead of SetVector to make edit/play mode the same (don't want unity's auto gamma correction different for this Color)
            if (PerCharEffectRimColor_RequireMaterialSet) input.SetColor(_PerCharEffectRimColor, PerCharEffectRimColorAsVector); // DONT IGNORE THIS: for MPB, use SetColor instead of SetVector to make edit/play mode the same (don't want unity's auto gamma correction different for this Color)
            if (PerCharEffectRimSharpnessPower_RequireMaterialSet) input.SetFloat(_PerCharEffectRimSharpnessPower, perCharacterRimLightSharpnessPower);
            if (isHeadBoneTransformExist)
            {
                if (compatibleWithNiloToon13Shader && FinalFaceForwardDirectionWS_RequireMaterialSet) input.SetVector(_FaceForwardDirection, finalFaceDirectionWS_Forward); // Converted to global array!
                if (compatibleWithNiloToon13Shader && FinalFaceUpDirectionWS_RequireMaterialSet) input.SetVector(_FaceUpDirection, finalFaceDirectionWS_Up); // Converted to global array!
                if (faceNormalFixAmount_RequireMaterialSet) input.SetFloat(_FixFaceNormalAmount, faceNormalFixAmount);
                if (fixFaceNormalUseFlattenOrProxySphereMethod_RequireMaterialSet) input.SetFloat(_FixFaceNormalUseFlattenOrProxySphereMethod, fixFaceNormalUseFlattenOrProxySphereMethod);
            }
            else
            {
                input.SetFloat(_FixFaceNormalAmount, 0);
            }
            if (characterAreaColorFillEnabled_RequireMaterialSet) input.SetFloat(_CharacterAreaColorFillEnabled, GetShouldRenderNiloToonCharacterAreaColorFillPass() ? 1 : 0);
            if (characterAreaColorFillColor_RequireMaterialSet) input.SetColor(_CharacterAreaColorFillColor, characterAreaColorFillColorAsVector); // DONT IGNORE THIS: for MPB, use SetColor instead of SetVector to make edit/play mode the same (don't want unity's auto gamma correction different for this Color)
            if (characterAreaColorFillTexture_RequireMaterialSet) input.SetTexture(_CharacterAreaColorFillTexture, GetFinalSetTextureCharacterAreaColorFillTexture());
            if (characterAreaColorFillTextureUVIndex_RequireMaterialSet) input.SetFloat(_CharacterAreaColorFillTextureUVIndex,(uint)characterAreaColorFillTextureUVIndex);
            if (characterAreaColorFillTextureUVTilingOffset_RequireMaterialSet) input.SetVector(_CharacterAreaColorFillTextureUVTilingOffset, GetCharacterAreaColorFillFinalUVTilingOffset());
            if (characterAreaColorFillTextureUVScrollSpeed_RequireMaterialSet) input.SetVector(_CharacterAreaColorFillTextureUVScrollSpeed, characterAreaColorFillTextureUVScrollSpeed);
            if (characterAreaColorFillRendersVisibleArea_RequireMaterialSet) input.SetFloat(_CharacterAreaColorFillRendersVisibleArea, characterAreaColorFillRendersVisibleArea ? 1 : 0);
            if (characterAreaColorFillRendersBlockedArea_RequireMaterialSet) input.SetFloat(_CharacterAreaColorFillRendersBlockedArea, characterAreaColorFillRendersBlockedArea ? 1 : 0);
            if (extraThickOutlineEnabled_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineEnabled, GetShouldRenderNiloToonExtraThickOutlinePass() ? 1 : 0);
            if (extraThickOutlineColor_RequireMaterialSet) input.SetColor(_ExtraThickOutlineColor, extraThickOutlineColorAsVector); // DONT IGNORE THIS: for MPB, use SetColor instead of SetVector to make edit/play mode the same (don't want unity's auto gamma correction different for this Color)
            if (extraThickOutlineWidth_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineWidth, extraThickOutlineWidth);
            if (extraThickOutlineMaximumFinalWidth_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineMaxFinalWidth, extraThickOutlineMaximumFinalWidth);
            if (extraThickOutlineViewSpacePosOffset_RequireMaterialSet) input.SetVector(_ExtraThickOutlineViewSpacePosOffset, extraThickOutlineViewSpacePosOffset);
            if (ExtraThickOutlineZTest_RequireMaterialSet) input.SetInteger(_ExtraThickOutlineZTest, GetExtraThickOutlineZTest());
            if (extraThickOutlineZOffset_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineZOffset, extraThickOutlineZOffset);
            if (extraThickOutlineZWrite_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineZWrite, GetExtraThickOutlineZWrite());
            if (extraThickOutlineWriteIntoDepthTexture_RequireMaterialSet) input.SetFloat(_ExtraThickOutlineWriteIntoDepthTexture, extraThickOutlineWriteIntoDepthTexture ? 1 : 0);
            if (compatibleWithNiloToon13Shader && CharacterBoundCenter_RequireMaterialSet) input.SetVector(_CharacterBoundCenterPosWS, characterBoundCenter); // Converted to global array!
            if (CharacterBoundRadius_RequireMaterialSet) input.SetFloat(_CharacterBoundRadius, GetCharacterBoundRadius());
            if (PerspectiveRemovalAmount_RequireMaterialSet) input.SetFloat(_PerspectiveRemovalAmount, GetPerspectiveRemovalAmount());
            if (PerspectiveRemovalRadius_RequireMaterialSet) input.SetFloat(_PerspectiveRemovalRadius, GetPerspectiveRemovalRadius());
            if (PerspectiveRemovalStartHeight_RequireMaterialSet) input.SetFloat(_PerspectiveRemovalStartHeight, GetPerspectiveRemovalStartHeight());
            if (PerspectiveRemovalEndHeight_RequireMaterialSet) input.SetFloat(_PerspectiveRemovalEndHeight, GetPerspectiveRemovalEndHeight());
            if (compatibleWithNiloToon13Shader && PerspectiveRemovalCenter_RequireMaterialSet) input.SetVector(_HeadBonePositionWS, perspectiveRemovalCenter); // Converted to global array!
            if (PerCharBaseColorTint_RequireMaterialSet) input.SetColor(_PerCharacterBaseColorTint, PerCharBaseColorTintAsVector); // DONT IGNORE THIS: for MPB, use SetColor instead of SetVector to make edit/play mode the same (don't want unity's auto gamma correction different for this Color)
            if (perCharacterOutlineWidthMultiply_RequireMaterialSet) input.SetFloat(_PerCharacterOutlineWidthMultiply, perCharacterOutlineWidthMultiply);
            if (PerCharOutlineColorTint_RequireMaterialSet) input.SetColor(_PerCharacterOutlineColorTint, PerCharOutlineColorTintAsVector); // DONT IGNORE THIS: for MPB, use SetColor instead of SetVector to make edit/play mode the same (don't want unity's auto gamma correction different for this Color)
            if (perCharacterOutlineEffectLerpColor_RequireMaterialSet) input.SetColor(_PerCharacterOutlineColorLerp, perCharacterOutlineEffectLerpColorAsVector); // DONT IGNORE THIS: for MPB, use SetColor instead of SetVector to make edit/play mode the same (don't want unity's auto gamma correction different for this Color)
            if (DitherFadeoutAmount_RequireMaterialSet) input.SetFloat(_DitherFadeoutAmount, GetDitherFadeoutAmount()); // need to always set, no matter keyword "_NILOTOON_DITHER_FADEOUT" is on or off (pass 1-x to shader to make preview window still render when shader is using 0 as _DitherFadeoutAmount's default value)
            if (DitherNormalScaleFix_RequireMaterialSet) input.SetFloat(_DitherFadeoutNormalScaleFix, ditherNormalScaleFix);
            if (DissolveAmount_RequireMaterialSet) input.SetFloat(_DissolveAmount, dissolveAmount);
            if (DissolveMode_RequireMaterialSet) input.SetInteger(_DissolveMode, GetDissolveModeInt());
            if (DissolveThresholdMap_RequireMaterialSet) input.SetTexture(_DissolveThresholdMap, GetFinalSetTextureDissolveThresholdMap());
            if (DissolveBorderRange_RequireMaterialSet) input.SetFloat(_DissolveBorderRange, dissolveBorderRange);
            if (DissolveThresholdMapTilingX_RequireMaterialSet) input.SetFloat(_DissolveThresholdMapTilingX, dissolveThresholdMapTilingX);
            if (DissolveThresholdMapTilingY_RequireMaterialSet) input.SetFloat(_DissolveThresholdMapTilingY, dissolveThresholdMapTilingY);
            if (DissolveNoiseStrength_RequireMaterialSet) input.SetFloat(_DissolveNoiseStrength, dissolveNoiseStrength);
            if (DissolveBorderTintColor_RequireMaterialSet) input.SetVector(_DissolveBorderTintColor, dissolveBorderTintColor);
            if (PerCharacterBaseMapOverrideAmount_RequireMaterialSet) input.SetFloat(_PerCharacterBaseMapOverrideAmount, perCharacterBaseMapOverrideAmount);
            if (PerCharacterBaseMapOverrideMap_RequireMaterialSet) input.SetTexture(_PerCharacterBaseMapOverrideMap, GetFinalSetTexturePerCharacterBaseMapOverrideMap());
            if (PerCharacterBaseMapOverrideTilingOffset_RequireMaterialSet) input.SetVector(_PerCharacterBaseMapOverrideTilingOffset, GetPerCharacterBaseMapOverrideFinalUVTilingOffset());
            if (perCharacterBaseMapOverrideUVScrollSpeed_RequireMaterialSet) input.SetVector(_PerCharacterBaseMapOverrideUVScrollSpeed, perCharacterBaseMapOverrideUVScrollSpeed);
            if (PerCharacterBaseMapOverrideTintColor_RequireMaterialSet) input.SetVector(_PerCharacterBaseMapOverrideTintColor, PerCharacterBaseMapOverrideTintColorAsVector);
            if (perCharacterBaseMapOverrideUVIndex_RequireMaterialSet) input.SetFloat(_PerCharacterBaseMapOverrideUVIndex, (uint)perCharacterBaseMapOverrideUVIndex);
            if (perCharacterBaseMapOverrideBlendMode_RequireMaterialSet) input.SetFloat(_PerCharacterBaseMapOverrideBlendMode, (uint)perCharacterBaseMapOverrideBlendMode);
            if (ReceiveAverageURPShadowMap_RequireMaterialSet) input.SetFloat(_PerCharReceiveAverageURPShadowMap, receiveAverageURPShadowMap);
            if (ReceiveStandardURPShadowMap_RequireMaterialSet) input.SetFloat(_PerCharReceiveStandardURPShadowMap, receiveStandardURPShadowMap);
            if (ReceiveNiloToonSelfShadowMap_RequireMaterialSet) input.SetFloat(_PerCharReceiveNiloToonSelfShadowMap, receiveNiloToonSelfShadowMap);
            if (perCharacterZOffset_RequireMaterialSet) input.SetFloat(_PerCharZOffset, perCharacterZOffset);
            if (shouldRenderSelfOutline_RequireMaterialSet) input.SetFloat(_PerCharacterRenderOutline, shouldRenderSelfOutline ? 1 : 0);

            if (ShouldEnableDepthTextureRimLightAndShadow_RequireKeywordChangeCall) input.SetFloat("_NiloToonEnableDepthTextureRimLightAndShadow", GetShouldEnableDepthTextureRimLightAndShadow() ? 1 : 0);
            //================================================================================================================================================

            // API EnableKeyword(...)/SetShaderPassEnabled(...) only works for Material class, so it only works in play mode
            //(X)
        }
        
        public void AutoFillInMissingProperties()
        {
            void SearchHipBone(Transform hipSearchStartTransform)
            {
                // [spine]
                if (!customCharacterBoundCenter)
                {
                    customCharacterBoundCenter =
                        NiloToonUtils.DepthSearch(hipSearchStartTransform, "spine", new string[] { "left", "right" });
                }
                
                // [hip]
                // find hip bone(with many ban words to avoid getting non-hip bone)
                if (!customCharacterBoundCenter)
                {
                    customCharacterBoundCenter = NiloToonUtils.DepthSearch(hipSearchStartTransform, "hip",
                        new string[]
                        {
                            "left", "right", "hipster", "hugger", "belt", "boot", "bag", "flask", "brace", "wrap", "chain",
                            "sling"
                        });
                }

                // if we find nothing due to ban words, find again(this time without ban words except LR)
                if (!customCharacterBoundCenter)
                {
                    customCharacterBoundCenter =
                        NiloToonUtils.DepthSearch(hipSearchStartTransform, "hip", new string[] { "left", "right" });
                }

                // [alternative: pelvis]
                // if confirmed that we can't find any hip bone, try pelvis instead(with many ban words to avoid getting non-pelvis bone)
                if (!customCharacterBoundCenter)
                {
                    customCharacterBoundCenter = NiloToonUtils.DepthSearch(hipSearchStartTransform, "pelvis",
                        new string[] { "left", "right", "blet", "binder", "support", "floor", "protector", "brace" });
                }

                // if we find nothing due to ban words, find again(this time without ban words except LR)
                if (!customCharacterBoundCenter)
                {
                    customCharacterBoundCenter =
                        NiloToonUtils.DepthSearch(hipSearchStartTransform, "pelvis", new string[] { "left", "right" });
                }

                // Should we produce a warning here if we can't find any hip/pelvis/spine bone?
                // = NO, since this function can run on non-characters
            }

            void SearchHeadBone(Transform headSearchStartTransform)
            {
                // [head]
                // find head bone(with many ban words to avoid getting non-head bone)
                if (!headBoneTransform)
                {
                    headBoneTransform = NiloToonUtils.DepthSearch(headSearchStartTransform, "head",
                        new string[]
                        {
                            "left", "right", "band", "scarf", "gear", "phone", "piece", "set", "wrap", "cover", "net", "lamp"
                        });
                }

                // if we find nothing due to ban words, find again(this time without ban words except LR)
                if (!headBoneTransform)
                {
                    headBoneTransform =
                        NiloToonUtils.DepthSearch(headSearchStartTransform, "head", new string[] { "left", "right" });
                }

                // [alternative: neck]
                // if confirmed that we can't find any head bone, try neck instead(with many ban words to avoid getting non-neck bone)
                if (!headBoneTransform)
                {
                    headBoneTransform = NiloToonUtils.DepthSearch(headSearchStartTransform, "neck",
                        new string[]
                            { "left", "right", "lace", "tie", "turtle", "chief", "crew", "brace", "strap", "gaiter", "guard" });
                }

                // if we find nothing due to ban words, find again(this time without ban words except LR)
                if (!headBoneTransform)
                {
                    headBoneTransform =
                        NiloToonUtils.DepthSearch(headSearchStartTransform, "neck", new string[] { "left", "right" });
                }

                // should we produce a warning here if we can't find any head/neck bone?
                // = no, since this function can run on non-characters
            }

            // 1. find hip/pelvis bone first, full search from script transform
            SearchHipBone(transform);

            // 2a. search head, using hip/pelvis bone as start transform to avoid getting non-bone transform
            if (customCharacterBoundCenter)
            {
                // usually head bone is a child of hip bone's parent transform
                Transform startTransform = customCharacterBoundCenter.parent;
                if (startTransform == null)
                {
                    startTransform = customCharacterBoundCenter;
                }

                if (!headBoneTransform)
                {
                    SearchHeadBone(startTransform);

                    // after headbone set, try auto set FaceForwardDirection and FaceUpDirection
                    AutoFillInFaceForwardDirAndFaceUpDir();
                }
            }

            // 2b. if we can't find any head bone, do a full search from script transform instead
            if (!headBoneTransform)
            {
                SearchHeadBone(transform);
                
                // after headbone set, try auto set FaceForwardDirection and FaceUpDirection
                AutoFillInFaceForwardDirAndFaceUpDir();
            }
            
            // Don't always call AutoFillInFaceForwardDirAndFaceUpDir(),
            // only call in auto setup, or when headbone is auto assigned
            // it will produce wrong result when character is in motion, where character GO root forward is not always face forward
        }

        public void AutoFillInFaceForwardDirAndFaceUpDir()
        {
            // early exit if auto set is not possible
            if (!headBoneTransform) return;

            // [try finding the best faceUpDirection and faceForwardDirection, so user don't need to set them manually]
            // within 6 possible vectors:
            // - find a vector that is closest to Vector3.Up
            // - find a vector that is closest to perCharScript.gameobject.transform.forward
            float bestFaceUpDotProductScore = float.NegativeInfinity;
            float bestFaceForwardDotProductScore = float.NegativeInfinity;
            TransformDirection bestFaceUpDirection = faceUpDirection; // default value doesn't matter
            TransformDirection bestFaceForwardDirection = faceForwardDirection; // default value doesn't matter

            for (int i = 0; i < 6; i++)
            {
                TransformDirection currentTestDir = (TransformDirection)i;
                Vector3 currentTestVector = GetFinalFaceDirectionWS(currentTestDir);

                // Dot product = how similar two vectors are, the highest score is 1 (when 2 vectors are 100% same)
                // all test are in world space
                float currentFaceUpTestDotProductScore = Vector3.Dot(currentTestVector, Vector3.up);
                float currentFaceForwardTestDotProductScore = Vector3.Dot(currentTestVector, transform.forward);
                if (currentFaceUpTestDotProductScore > bestFaceUpDotProductScore)
                {
                    bestFaceUpDotProductScore = currentFaceUpTestDotProductScore;
                    bestFaceUpDirection = currentTestDir;
                }
                if (currentFaceForwardTestDotProductScore > bestFaceForwardDotProductScore)
                {
                    bestFaceForwardDotProductScore = currentFaceForwardTestDotProductScore;
                    bestFaceForwardDirection = currentTestDir;
                }
            }

            // after all 6 test, the best direction will be found, and we apply them here
            faceUpDirection = bestFaceUpDirection;
            faceForwardDirection = bestFaceForwardDirection;
        }
    }
}


