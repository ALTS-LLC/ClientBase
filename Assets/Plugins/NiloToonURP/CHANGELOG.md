# Changelog
All notable changes to NiloToonURP & demo project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

- (Core) or no tag means the change is part of NiloToonURP_[version].unitypackage (files inside NiloToonURP folder)
- (Demo) means the change is only changing the demo project, without changing NiloToonURP.unitypackage
- (Doc) means the change is only affecting any .pdf documents
- (InternalCore) means Core, but if you are only using NiloToonURP as a tool, and don't need to read/edit NiloToonURP's source code/files, you can ignore this change.

----------------------------------------------
## [0.16.28] - 2024-12-04

### Changed
- NiloToonMotionBlurVolume: Improve the quality and performance by rewriting it based on KinoMotion
----------------------------------------------
## [0.16.27] - 2024-12-03

### Added
- Added NiloToonMotionBlurVolume in volume, it is an alternative to URP's motion blur

### Changed
- MotionBlurVideoBaker: improve UI, adding document links
- LWGUI: upgrade to 1.20.2

### Fixed
- NiloToon Character shader: fix a motion vector bug when _ALPHATEST_ON is used
- fix a bug that NiloToon can't import a mesh when MeshRenderer exists without MeshFilter
----------------------------------------------
## [0.16.26] - 2024-11-17

### Developer Note
- This is the first version that makes NiloToon support RenderGraph in a very basic way. Note that NiloToon's self shadowmap pass and all NiloToon postprocess will be disabled when enabling RenderGraph, we are working on it, it will take some time. (Unity2021.3 is still supported, we are trying to delay the remove of Unity2021.3 support as much as possible, due to Warudo using Unity2021.3)

### Breaking Change
- NiloToon Character shader: default _OutlineWidth changed from 0.6->0.5, since it is a more reasonable value for VTuber models

### Added
- NiloToon Character shader: added 'XRMotionVectors' pass, for experimental support of XR Application SpaceWarp for Unity6
- NiloEditorMotionBlurVideoBaker: add ProRes,h265 option to a new codec option, default to ProRes now to improve speed and quality (but bigger file size)
- NiloToonAverageShadowTestRTPass: add experimental RenderGraph support
- NiloToonCharSelfShadowMapRTPass: add WIP RenderGraph support, now self shadow will always be disabled when RenderGraph is enabled, we will unlock this again after we finish the implementation in the future

### Changed
- NiloToonToonOutlinePass.cs: improved RenderGraph support
- NiloToon Character shader: better 'MotionVectors' pass, it will now consider zoffset, perspective removal, dither, dissolve, enable renderings etc
- NiloEditorMotionBlurVideoBaker: improve log info, displaying important info like complete% and total duration, also requiring less memory now to reduce the chance of out of memory crash

### Fixed
- NiloToon Bloom: Add a very small bias to NiloToon's prepass, to avoid prepass problems similar to shadow acne
----------------------------------------------
## [0.16.25] - 2024-11-13

### Added
- NiloToon Character shader: +_ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCameraRemapStart,_ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCameraRemapEnd. For adding control to front hair semi-transparent fadeout
- NiloToonSetToonParamPass.cs: add experimental RenderGraph support
- NiloToonToonOutlinePass.cs: add experimental RenderGraph support

### Changed
- NiloToon Character shader: sync motion vector pass to Unity6LTS
- LWGUI: upgrade to latest version

### Fixed
- NiloEditorMotionBlurVideoBaker: support the latest ffmpeg, improve input video fps detection speed, improve UI info
----------------------------------------------
## [0.16.24] - 2024-11-02

### Added
- NiloToon Character shader: Add Adaptive Probe Volumes(APV) support for Unity6LTS
----------------------------------------------
## [0.16.23] - 2024-10-17

### Added
- Add menu item for "Assets/NiloToon/xxx", for easier menuitem usage by right clicking an asset within the project window, so user don't need to always click the "Window/NiloToon/xxx" menu item

### Changed
- NiloToonMaterialConvertor.cs: lilToon convertor now consider ZWrite option to select the correct SurfaceType

### Fixed
- NiloToonUberPostProcessPass.cs: remove the use of ShaderKeywordStrings.UseRGBM, it is a required fix to support Unity6 (2024/10/17 release version)
- NiloToonSetToonParamPass.cs: fix a bug when light number > max supported number of light
----------------------------------------------
## [0.16.22] - 2024-10-01

### Added
- Improve and released "Window/NiloToon/MotionBlur Video Baker", now out of experimental
- Added NiloToonEditor_AutoGeneratePrefabVariantAndAssets.cs, now user can use "Window/NiloToon/Create Nilo Prefab Variant and Materials" to setup NiloToon without modifying any original material and prefab
- NiloToonVolumePresetPicker: added Nilo027~Nilo029 volume presets
- NiloToonTonemapping volume: +Nilo Hybird ACES (experimental)

### Fixed
- NiloToonMaterialConvertor.cs: fixed name space problem
----------------------------------------------
## [0.16.21] - 2024-09-03

### Added
- NiloToon_Character shader: To allow using reflection probe as 3D rim light, added -> _EnvironmentReflectionShouldApplyToFaceArea,_EnvironmentReflectionApplyReplaceBlending,_EnvironmentReflectionApplyAddBlending,_EnvironmentReflectionFresnelPower,_EnvironmentReflectionFresnelRemapStart,_EnvironmentReflectionMFresnelRemapEnd
- NiloToonUberPostProcessPass.cs: to allow better renderer feature sorting, added -> renderPassEventTimingOffset, default 0 

### Fixed
- NiloToon_Character shader: fix SRP batching not correct in Unity6 due to motion vector pass
- NiloToonCharacterSticker_Additive.shader: fix Float data mismatch bug, now corrected to Integer
- NiloToonCharacterSticker_Multiply.shader: fix Float data mismatch bug, now corrected to Integer
- GenericStencilUnlit.shader: fix Float data mismatch bug, now corrected to Integer
----------------------------------------------
## [0.16.20] - 2024-08-19

### Added
- NiloToonTonemapping: + Khronos PBR Neutral, + Nilo True Color V1 (experimental)
- NiloToon_Character shader: add new group -> Face 3D rim light and shadow (experimental)
- Add Window/NiloToonURP/Experimental/MotionBlur Video Baker (experimental)

### Fixed
- NiloToonPerCharacterRenderController: fix SetInt() data mismatch bug, now corrected to SetInteger() API
- NiloToon Debug Window: change normalTS debug to match URP's rendering debugger color format
----------------------------------------------
## [0.16.19] - 2024-08-07

### Added
- NiloToon Character shader: +_AsUnlit, for multiply blending material like blush

### Fixed
- NiloToon Character shader: change array index from Float to integer, to avoid graphics bug on Adreno740 GPU (https://issuetracker.unity3d.com/issues/android-shader-has-incorrect-uvs-when-built-on-android-devices-with-an-adreno-740-gpu)
----------------------------------------------
## [0.16.18] - 2024-07-30

### Added
- NiloToon Character shader: +_PreMultiplyAlphaIntoRGBOutput, _ColorRenderStatesGroupPreset

### Changed
- NiloToon Character shader: move "Color Buffer" group to Normal UI Display group (now show in default material setting)
----------------------------------------------
## [0.16.17] - 2024-07-28

### Breaking Change
- NiloToon Character shader: reduce max char(maximum number of NiloToonPerCharacterRenderController) to improve mobile GPU performance, max char for opengles = 32->16, max char for vulkan/switch = 64->32, max char for PC = still 128(unchanged)
- NiloToon Character shader: apply basemapstacking layer to all passes, because basemapstacking layer will now affect alpha value of all passes

### Changed
- NiloToon Character shader: _DepthTexRimLight3DRimMaskThreshold change default value from 0.075 -> 0.5, remove experimental tag

### Added
- NiloToon Character shader: +_DepthTexRimLight3DFallbackMidPoint,_DepthTexRimLight3DFallbackSoftness, _DepthTexRimLight3DFallbackRemoveFlatPolygonRimLight, for letting user control 3D rim light fallback's visual, can be used as soft fresnel rim light
- NiloToon Character shader: + _AlphaOverrideTexUVIndex

### Fixed
- NiloToon Character shader: average shadow now supports URP's screen space shadow renderer feature
- NiloToon Character shader: optimize GetUV() method GPU performance by "replacing dynamic array UV index access by if chains with const index"
- NiloToonPerCharacterRenderController: avoid calling Texture2D.whiteTexture, which will avoid triggering NDMF(Non-Destructive Modular Framework)'s crash bug
----------------------------------------------
## [0.16.16] - 2024-07-25

### Fixed
- fix environment shader can't compile in Unity6(6000.0.11f1), by calling the correct OUTPUT_SH4 function overload
- fix average shadow not working correctly for multiple characters in Unity2022.3 or later
- (InternalCore) change GraphicsSettings.renderPipelineAsset -> GraphicsSettings.defaultRenderPipeline
----------------------------------------------
## [0.16.15] - 2024-07-05

### Fixed
- Fix NiloToon renderer feature's runtime error in Unity2022.3, this error will trigger when PIDI 5 planar reflection is used together with NiloToon in Unity2022.3
----------------------------------------------
## [0.16.14] - 2024-07-03

### Fixed
- error handle when bake smooth normal's mesh doesn't have tangent
- fix a basemap stacking layer NormalRGBA mode bug when the material is opaque cutout
----------------------------------------------
## [0.16.13] - 2024-06-21

### Added
- NiloToon Character shader: +zoffset presets
- NiloToon Character shader: apply LWGUI ramp map to spceular ramp
- NiloToonVolumePicker: +ID 026 night soft bloom

### Changed
- Auto setup: eye keyword + "sclera", "cornea", "limbus"
- Auto setup: ban "headset","phone" for face keyword
- NiloToon Character shader: specular ramp now affected by ramp alpha

### Fixed
- fix reimport deadlloop when mesh has 0 vertex
- NiloToon Character shader: fix a bug where toon specular can't use _SpecularReactToLightDirMode
----------------------------------------------
## [0.16.12] - 2024-05-28

### Breaking Changes
- NiloToonLightSourceModifier: remove penetration, add "back light occlusion(2D) & (3D)"
- NiloToonLightSourceModifier: when light list is empty, it will not be considered as a global modifier anymore
- NiloToonLightSourceModifier: now always auto re-fill empty light list, instead of auto fill in OnValidate() only
- Auto material convertor: don't force reset render queue anymore, unless the original material's render queue is out of expected render queue range. This change can help preserve the render queue from the original material (more correct draw order/ stencil order), while still not breaking NiloToon's material render queue requirements

### Added
- NiloToon Character shader: +_MultiplyBRPColor, expose _Color
- NiloToon Character shader: Add "invert?" to many mask map -> _MatCapOcclusionMaskMapInvert, _MatCapAdditiveMaskMapInvertColor, _DepthTexRimLightMaskTexInvertColor, _OverrideShadowColorMaskMapInvertColor, _ZOffsetMaskMapInvertColor, _MatCapAlphaBlendMaskMapInvertColor, _DetailMaskInvertColor, _FaceShadowGradientMaskMapInvertColor, _EmissionMaskMapInvertColor, _EnvironmentReflectionMaskMapInvertColor
- NiloToon Character shader: +_DepthTexRimLight3DRimMaskEnable, _DepthTexRimLight3DRimMaskThreshold (experimental) for try fixing 2D rimlight problem on finger or small objects like ear ring
- NiloToon Character shader: +_SpecularReactToLightDirMode dropdown menu(Use settings from volume, Readct to light, Follow camera) for controlling how specular light react to light direction in material
- NiloToon Character shader: +RimHard(LightFromTop) matcap add preset
- NiloToonPerCharacterRenderController: +renderCharacter toggle, allow you to on/off the rendering of a character with just 1 click or 1 API call
- NiloToonPerCharacterRenderController: +MaterialInstanceMode  toggle, for prevent generating material instance when required
- Auto material convert: Rewrite univrm0.x auto material convert, porting more properties from "VRM/MToon", now _Color in VRM blend shape proxy clips can be reused without editing any clips
- Auto setup character can now select multiple characters and process them together. For example, you can select multiple vrm character prefabs and process them together
- Added a new menu item 'Window > NiloToonURP > Convert Selected Materials to NiloToon_Character', so you can convert material to NiloToon without selecting any gameobject/prefab
- Auto material convert: + lilToon emission's alpha porting
- Auto material convert: +Stencil preset matching
- +Matcap_Upward_HardEdge.png
-(InternalCore) + NiloAAUtil.hlsl
-(InternalCore) + NiloToonMaterialConvertor.cs, extracted all NiloToonEditorPerCharacterRenderControllerCustomEditor's material auto convert logic into this script

### Changed
- NiloToon Character shader: convert "Use GGX Direct Specular?" toggle to a "Specular Method" dropdown menu with 2 modes(Toon, PBR GGX)
- NiloToon Character shader: move 'UV Edit' and 'Lighting style' group
- Auto material convert: + 'karada' as skin material keyword, +'head' as face keyword, +'canthus' as eye keyword
- Auto material convert: auto disable '2D rimlight and shadow' if material renderqueue > 2500 (doesn't draw depth texture)
- Update LWGUI to the latest 2.x version (2024-05-29)
- Improve menuitem ordering in 'Window > NiloToon > ...'
- (Doc) Improve doc about improving rendering results in section 'AA(outline,rim light,shadow map)','DLSS | FSR3 (better fps & AA)','Cam output char’s alpha'

### Fixed
- NiloToonVolumePresetPickerEditor: fix a critical bug that NiloToonVolumePresetPickerEditor's ID didn't save correctly when saving the scene, where the ID will become 0 when the scene reload
- NiloToonCharacter_LightingEquation: fix a critical rim light result breaking change bug due to wrong code implementation, where rim light artifact will appear in version 0.16.0~0.16.11
- NiloBlendEquationUtil.hlsl: fix a basemap stacking layer NormalRGBA mode rgb blending bug when alpha is 0 and the material is opaque
- NiloToonEditor_AssetLabelAssetPostProcessor: fix Model import bake UV8's 'HashMap is full' error log
- NiloToonPerCharacterRenderController: fix a dissolve bug where pixels are not dissolve correctly when the current dissolve texture pixel is pure white/black 
----------------------------------------------
## [0.16.11] - 2024-04-11

### Added
- NiloToonTonemappingVolume: add a new tonemap mode -> ACES (Custom SR)
- Added 5 Skin template materials
- NiloToonLightSourceModifier: add main light "Penetration" param, set it to 0 for blocking backlight(e.g., spot light)
----------------------------------------------
## [0.16.10] - 2024-03-29

### Changed
- frame debugger and profiler: better display group & names for all NiloToon pass
- auto setup now treats material with name "matuge" as eye material now

### Fixed
- NiloToonPerCharacterRenderController: fix a critical bug that makes FaceForward & FaceUp can't be edited (incorrectly locked to "auto value" every frame), if you saw that face shadow / face direction is incorrect in previous NiloToon version when character is playing animation(e.g., timeline), try to upgrade to this version. 
----------------------------------------------
## [0.16.9] - 2024-03-23

### Breaking Changes
- NiloToon Character shader: clamp ggx specular roughness's minimum to 0.04 instead of 0, to prevent bad bloom result when smoothness is 1(roughness is 0)
----------------------------------------------
## [0.16.8] - 2024-03-23

### Breaking Changes
- NiloToon Character shader: _SelfShadowAreaSaturationBoost's default value changed from 0.5 -> 0.2, to reduce the default hue artifact due to texture compression

### Added
- NiloToon Character shader: + WIP motion vector pass for URP16(Unity2023.2) or later
- NiloToonEditorPerCharacterRenderControllerCustomEditor: auto setup character now converts lilToon's _ShadowBorderMask -> NiloToon's _OcclusionMap
- NiloToonEditorPerCharacterRenderControllerCustomEditor: auto setup character now converts lilToon's parallax -> NiloToon
- NiloToonAnimePostProcessPass: add new toggle -> affectedByCameraPostprocessToggle
- Auto install renderer feature: now consider Graphics tab's URP asset also
- Add new Window>NiloToon>... [MenuItem] buttons: Update to latest version, Open document, Change log, contact support 

### Changed
- LWGUI: updated to latest 2.x
- Auto install renderer feature: if the setup is correct already and nothing to do, now also show a pop up window

### Fixed
- NiloToon environment shader: fix a Unity6 compile error about SETUP_DEBUG_TEXTURE_DATA(...)
- NiloToonEditorPerCharacterRenderControllerCustomEditor: AutoSetupCharacterGameObject(...) prevent user calling from fbx generated prefab
- NiloToonPerCharacterRenderController: fix a null reference exception bug on OnDisable()
- NiloToonSetToonParamPass: fix a possible GPU global array wrong length when switching platform
- NiloToonEditorPerCharacterRenderControllerCustomEditor: fix lilToon->NiloToon "_BackFaceBaseMapReplaceColor" wrong port
- NiloToonEditorPerCharacterRenderControllerCustomEditor: fix lilToon->NiloToon "_Main2ndTexAngle" wrong port
- NiloToon Character shader: add [HideInInspector] _MainTex & _Color, to prevent vrm0.x output error 
- NiloBlendEquationUtil.hlsl: fix NormalRGBA's 0 alpha bug on mobile
----------------------------------------------
## [0.16.7] - 2024-03-12

### Added
- NiloToonSetToonParamPass: add new API -> public static bool ForceMinimumShader, to enable ForceMinimumShader using code
- NiloToonCharacter shader: add _SkinShadowBrightness, _FaceShadowBrightness

### Changed
- NiloToonCharacter shader: _MultiplyBaseColorToSpecularColor's default value changed from 0 -> 0.5
- NiloToonCharacter shader: moved Parallax Map from "Show All" group to "Expert" group 
- NiloToonPerCharacterRenderController: remove the (Experimental) tag of "Generate Smoothed normal?"
- NiloToonShaderStrippingSettingSO: better tooltip

### Fixed
- Auto setup character: fix material wrongly set to renderQueue -1, now it is correctly set to 2000
- NiloDefineURPGlobalTextures.hlsl: fix _CameraDepthTexture_TexelSize macro condition bug, which makes it fail to compile in Unity2022.3.21f1
- NiloToonPerCharacterRenderController: fix a possible OnDisable() null reference exception when building the player
----------------------------------------------
## [0.16.6] - 2024-03-05

### Changed
- Volume profile presets: specular react to light dir revert to default value(not override)
- Volume profile presets: outline width revert to default value(not override)

### Fixed
- NiloToonCharSelfShadowMapRTPass: fix XR multi pass mode's rendering bug due to wrong view projection matrix 
- NiloToon Character shader: fix debug shading can't compile when _PARALLAXMAP enabled
----------------------------------------------
## [0.16.5] - 2024-02-26

### Fixed
- LWGUI: upgraded to the latest 2.x version, fixing ramp tex format ARGB32 error and helpbox width problem 
- NiloToon Character shader: Fix Opaque SurfaceType not setting the correct render queue (-1) due to LWGUI, now Opaque SurfaceType preset set render queue to 2000 instead to prevent this problem
----------------------------------------------
## [0.16.4] - 2024-02-25

### Breaking Change
- (InternalCore) VideoPlayerForceSyncWithRecorder: move from NO namespace to NiloToon.NiloToonURP.MiscUtil
- (InternalCore) DoFAutoFocusTransform.cs: move from namespace NiloToon.NiloToonURP to NiloToon.NiloToonURP.MiscUtil
- (InternalCore) rename global shader float _NiloToonGlobalSelfShadowDepthBias to _NiloToonGlobalSelfShadowCasterDepthBias, to make support of 0.13.8 shader possible
- (InternalCore) rename global shader float _NiloToonGlobalSelfShadowNormalBias to _NiloToonGlobalSelfShadowCasterNormalBias, to make support of 0.13.8 shader possible

### Added
- NiloToonPerCharacterRenderController.cs: +compatibleWithNiloToon13Shader("Compatible with 0.13.8 shader") toggle in Misc group, for a special situation where NiloToon 0.16.x C# + 0.13.8 shader need to work together (e.g., to support old Warudo files/Asset bundles built with NiloToon 0.13.8 in the past in a NiloToon 0.16.x C# build)
- +SelfRotate.cs, for model 360 debug use
- +UnlockFPS.cs, for android build fps checking use
----------------------------------------------
## [0.16.3] - 2024-02-22

### Added
- NiloToon Character shader: _DepthTexShadowFixedDirectionForFace, for a stable hair shadow on face

### Changed
- Auto Setup Character now search 'bound center' bone in the following order: spine>hip>pelvis instead of hip>pelvis>spine
----------------------------------------------
## [0.16.2] - 2024-02-21

### Fixed
- fix LWGUI null material problem (material inspector can't show, with error log)
- (InternalCore)fix shader compile warning about pow(f/e) potential negative f value
----------------------------------------------
## [0.16.1] - 2024-02-20

### Breaking Change
- low-end OpenGL mobile limited maximum characters from 128 -> 32, due to shader compile limitation & performance
- mobile/OpenGL limited maximum characters from 128 -> 64, due to shader compile limitation & performance

### Added
- NiloToon Character shader: +BaseMapStackingLayer 1-10 Mask's UVIndex, Remap, Extract from ID
- NiloToon Character shader: +MatCapp Add & MatCap Replace's new presets
- +Matcap_BlackSmoothSpecular01.png

### Changed
- LWGUI upgraded from 1.16 to 2.0, you can use "Show Only Modified Properties by Group" option now, it is an important option for adjusting material easily and safely

### Fixed
- fix all BaseMapStackingLayer's mask UV bug
- fix shader can't compile in OpenGL mode on a Window PC's editor due to "error C6020: Constant register limit exceeded at _____; more than 1024 registers needed to compile program", see [Breaking Change]
----------------------------------------------
## [0.16.0] - 2024-02-19

### Version Highlights
In short, compared to NiloToon 0.15.15, NiloToon 0.16.0 introduces these significant changes:
- [breaking change] Rewrote the code for additional lights; all additional lights are now treated as the main light by default, allowing you to light a character nicely with just point/spot lights (great for interiors!)
- Added a new 'NiloToonCharacterLightController' script, which allows you to control how each character receives lighting(e.g, tint or add color to main light for a few target characters)
- Added a new 'NiloToonAdditionalLightStyleVolume' volume, which enables you to control how all lights affects all characters globally
- Added a new 'NiloToonLightSourceModifier' script, which enables you to control how each light affects all characters. For example, you can create a few point lights with different roles -> (1)a normal point light (2)a point light without rim light (3)a point light with only rim light (4)a point light that doesn't affect nilotoon characters...For (4), it is especially handy if you'd rather not use Unity URP's rendering masks, since URP's rendering layers mandate individual settings for each renderer's layer, this can be time-consuming and clutters your scene's game objects and prefab files.
- Many new options in the menu "GameObject > Create Other > NiloToon"
- Many new options in the menu "GameObject > Light > NiloToon" 
- Introduced 'soft shadow' for shadow maps to reduce aliasing, enabled by default (medium quality)
- Updated the NiloToon Character shader: Basemap Stacking Layer 1-10 now includes +7 UVs (UV1-4, MatcapUV, CharBoundUV, ScreenSpaceUV) and 4 blend mode options (normal, add, screen, multiply). For example, you can create a basemap stacking layer using matcapUV with multiply or add blend methods.
- Updated NiloToonPerCharacterRenderController's' 'Basemap Override' group: +7 UVs (UV1-4, MatcapUV, CharBoundUV, ScreenSpaceUV), and 4 blend mode options (normal, add, screen, multiply)
- Added a new 'Color Fill' group to NiloToonPerCharacterRenderController, a utility function to redraw the character with different effects (e.g., x-ray vision, show characters that are behind a wall similar to CS2/Valorant)
- Added a new 'Generate Smoothed normal?' toogle to NiloToonPerCharacterRenderController, you can enable it for non-fbx prefabs(e.g., vrm character), so those non-fbx character prefabs can still have a similar outline quality as fbx characters
- Fixed all memory leaks and GC allocation issues that were found
- Significant CPU optimization when multiple characters are visible (approximately 1.72x CPU speed up for a test scene with 30 unique characters and 800 unique characters materials)
- HLSL code is now much easier to read in Rider

### Note from the Developer (Unity6/RenderGraph/Warudo)
- NiloToon 0.16.x is confirmed to be the last version that still supports Unity2021.3.
- In NiloToon 0.17.0, we will completely remove support for Unity2021.3 to begin working on Unity6 (RenderGraph) support.
- After a few more 0.16.x releases, we will provide a stable NiloToon 0.16.x version to Warudo to be used as the official NiloToon version for Warudo(Unity2021.3).

### Note from the Developer (Additional Light Rewrite)
**Skip this message if you do not need to light characters using point/spot lights.**
NiloToon 0.16.0 includes a major rewrite of the code for additional lights!

In the previous version, NiloToon 0.15.15, the treatment of additional lights was similar to Unity's method, where the lights' colors were additively blended onto the character. As a Unity URP shader, our original design aimed to align with Unity's URP additional light behavior as closely as possible, although we know that it is not the best for toon shader.

However, the additive light method only looked good under very carefully set up conditions, like this [video](https://youtu.be/ST9qNEfPrmY?si=98mqByySiRE7kcTO). In most regular use cases, NiloToon 0.15.15's point/spot lights could easily make the character appear overly bright or look strange.

Therefore, in NiloToon 0.16.0, we decided it was time to make a significant breaking change to ensure additional lights provide good results by default. Now, any additional lights are treated the same as the main light, so no matter how you light the character and regardless of the light type used, the lighting result will always maintain the same quality as the main directional light. This means scenes that mainly use point/spot lights, such as interiors, rooms, caves, etc., will now be much easier to light characters nicely and consistently with NiloToon 0.16.0.

*If you want to produce the old additional light result, you can:
- In Nilotoon Additional Light Style Volume: set color & direction = 0
- (optional)In Nilotoon Cinematic Rim Light Volume: disable “auto fix unsafe style”

### Breaking Change
- NiloToon Character shader: "face normal fix" now only affects materials that are controlled by NiloToonPerCharacterRenderController
- NiloToon Character shader: fix a wrong MatCapUV & CharBoundUV code that wrongly flip these UV's x, now after the fix, MatCapUV & CharBoundUV will look the same as the Texture2D on a 3D sphere mesh correctly, not having the uv x flip anymore. However, it is a bug fix that lead to a breaking change, so if you want to keep the old MatCapUV result where uv x is flipped, go to material's "UV Edit" group, set MatCapUV's tiling to (-1,1,_,_).
- NiloToon Character shader: _BackFaceTintColor will not affect outline pass anymore, since it will make the outline darken which is not correct
- NiloToon Character shader: normalize depth rim uv offset direction vector, to make rim light width more consistent under all light condition and direction
- NiloToon Character shader: disable NiloToonDepthOnlyOrDepthNormalPass's face normal edit, since we don't want to edit the face normal of URP's _CameraNormalTexture
- NiloToonPerCharacterRenderController: BaseMap Override group's UV tiling is now using center(0.5,0.5) as tiling center instead of (0,0)
- NiloToonPerCharacterRenderController: BaseMap Override's Map, when it is null, NiloToon will now use a default white opaque tex instead of gray & semi-transparent(50%) texture
- NiloToonPerCharacterRenderController: no longer force update/reimport all active characters in scene, since it is too slow when a scene contains too many characters
- NiloToonPerCharacterRenderController: make StencilFill, ExtraThickOutline, ColorFill pass only draw opaque queue (<2500) materials
- NiloToonCinematicRimLightVolume: +"Auto fix unsafe Style?" toggle, default enabled, which will force at least 1 cinematic rim light style on
- NiloToonCharRenderingControlVolume: specularReactToLightDirectionChange now default = true. If you want to reproduce the old result, you can override it and set it back to false
- All NiloToonVolumePreset assets: reset charOutlineWidthMultiplier to 1

### Breaking Change(hlsl programming only)
- (InternalCore) NiloToon Character shader: rename ToonLightingData.PerPixelNormalizedPolygonNormalWS to normalWS_NoNormalMap
- (InternalCore) NiloToon Character shader: rename ToonLightingData.PolygonNdotV to NdotV_NoNormalMap
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader: changed function param -> void ApplyCustomUserLogicToBaseColor(inout half4 baseColor, Varyings input, UVData uvData, float facing)
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader: changed function param -> void ApplyCustomUserLogicBeforeFog(inout half3 color, ToonSurfaceData surfaceData, ToonLightingData lightingData, Varyings input, UVData uvData)
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader: changed function param -> void ApplyCustomUserLogicAfterFog(inout half3 color, ToonSurfaceData surfaceData, ToonLightingData lightingData, Varyings input, UVData uvData)
- (InternalCore) NiloToon Character shader: move indirect light to main light injection phase

### Added
- + new volume "NiloToonAdditionalLightStyleVolume"
- + new script "NiloToonCharacterLightController", with a create gameobject method in "GameObject > ... > NiloToon" menu
- + new script "NiloToonLightSourceModifier", with a few create gameobject methods in "GameObject > ... > NiloToon" menu
- NiloToonAllInOneRendererFeature: + soft shadow (default enable, medium quality)
- NiloToonAnimePostProcessVolume: + topLightMultiplyLightColor
- NiloToonCharacterMainLightOverrider: + overrideTiming
- NiloToonPerCharacterRenderController: + new "Color Fill" group
- NiloToonPerCharacterRenderController: + auto fill missing properties in LateUpdate
- NiloToonPerCharacterRenderController: +"Generate Smoothed normal?" toggle, great for non-fbx character to also generate a similar quality baked outline at runtime
- NiloToonPerCharacterRenderController: "Auto setup character" now support PhysicalMaterial3DsMax ArnoldStandardSurface's auto setup
- NiloToonPerCharacterRenderController: BaseMapOverride group + UVIndex {UV1-4, MatCapUV, CharBoundUV, ScreenSpaceUV}
- NiloToonPerCharacterRenderController: BaseMapOverride group + BlendMode {Normal,Add,Screen,Multiply}
- NiloToon Character shader: + _DepthTexRimLightIgnoreLightDir(360 rim) and _DepthTexShadowIgnoreLightDir(360 shadow)
- NiloToon Character shader: + _SelfShadowAreaHSVStrength 
- NiloToon Character shader: + _SkinShadowTintColor2, _FaceShadowTintColor2
- NiloToon Character shader: "Normal map" group + UVIndex,UVScaleOffset,UVScrollSpeed
- NiloToon Character shader: + new "UV Edit" group, allow you can add tiling,rotation,animation.... to UV1~4 & MatCapUV
- NiloToon Character shader: all "Basemap stacking layers" group + master strength slider
- NiloToon Character shader: all "Basemap stacking layers" group + BlendMode {Normal,Add,Screen,Multiply}
- NiloToon Character shader: all "Basemap stacking layers" group + UVIndex {UV1-4, MatCapUV, CharBoundUV, ScreenSpaceUV}
- NiloToon Character shader: all "Basemap stacking layers" group + UVscale,position,angle,rotate options
- NiloToon Character shader: update basemap stacking layer 4-10, now they have the same quality & options as basemap stacking layer 1-3
- NiloToon Character shader: normal map will affect MatCapUV now
- NiloToon Character shader: +_BackFaceForceShadow, _BackFaceReplaceBaseMapColor (including liltoon auto setup convertion)
- NiloToon Character shader: +_ReceiveURPShadowMappingAmountForFace, _ReceiveURPShadowMappingAmountForNonFace
- NiloToon Character shader: + new group "URP Additional Light ShadowMap"
- NiloToon Character shader: + "Light Style" group's preset
- NiloToon Character shader: +_SkinMaskMapAsIDMap, _SkinMaskMapExtractFromID
- NiloToon Character shader: +_SpecularMapAsIDMap, _SpecularMapInvertColor
- NiloToon Character shader: +_DepthTexRimLightMaskTex
- + Normalmap_Skin.png, NormalMap_MetalBump.png  in Textures folder, as util textures for detailmap group
- more liltoon properties can port to NiloToon via "Auto setup character" (WIP)
- (InternalCore) Enhanced support for Rider. You can read and edit NiloToon source code in Rider, just like reading C#. Utilize Rider functions like 'Go To' (F12), 'Go Back' (Ctrl+-), and 'Find All Usages' (Shift+F12).

### Changed
- LWGUI: upgrade 1.4.3 -> 1.6.0
- NiloToonPerCharacterRenderController: remove allowCacheSystem toggle, now NiloToon will auto detect material reference change and perform auto update (no API call due to material reference change is needed by user now, all update is automated)
- NiloToon Character shader: various UI inprovements like helpbox, tooltip, display names....
- Remove console Debug.Log(s) that are not too helpful, so NiloToon will try to keep the console window as clean as possible

### Fixed
- NiloToonPerCharacterRenderController: Significant CPU optimization has been achieved when multiple characters use the NiloToon per character script. In tests with 30 characters and 800 materials, the time improved from 19ms to 11ms (speed up = x1.72). The speed up is due to NiloToon stopped a few per frame material updates via NiloToonPerCharacterRenderControllerm, which lead to SRP batcher using a fast path to upload data from CPU to GPU, since no material properties are edited per frame now
- NiloToonPerCharacterRenderController: fix all memory leaks due to material reference changed by user in playmode(e.g., due to playing an animator animation with any material reference key)
- NiloToonPerCharacterRenderController: fix a bug that clicking "don't edit material" in character auto-setup will wrongly skip updating the script's field also. Now after the fix, script's field will always update correctly
- NiloToonUberPostProcessPass: fix a GC alloc bug due to wrong shader name string
- NiloToonBloomVolume: fix bloom RT over max size due to fixedHeight option with a very wide screen, it now cap at the GPU allowed max size, usually it is 16384 width for PC
- package.json: changed "type" from "module" -> "tool", allow NiloToonURP to show up in project window even when installed via package manager(UPM)
- fix a few null reference exception found when entering/exiting playmode or when building the game

---------------------
### バージョンのハイライト
簡単に言うと、NiloToon 0.15.15に比べて、NiloToon 0.16.0は以下の重要な変更を導入しました：
- [破壊的変更] 追加ライトのコードを書き直しました。すべての追加ライトは、デフォルトでメインライトとして扱われるようになり、ポイントライト/スポットライトだけでキャラクターをきれいに照らすことができます（インテリアに最適！）
- 新しい「NiloToonCharacterLightController」スクリプトを追加しました。これにより、各キャラクターがライティングをどのように受け取るかを制御できます（例えば、いくつかのターゲットキャラクターに対してメインライトの色を調整したり、色を追加したりすることができます）
- 新しい「NiloToonAdditionalLightStyleVolume」ボリュームを追加しました。これにより、すべてのライトがすべてのキャラクターに対してグローバルにどのように影響するかを制御できます
- 新しい「NiloToonLightSourceModifier」スクリプトを追加しました。これにより、各ライトがすべてのキャラクターにどのように影響するかを制御できます。例えば、（1）通常のポイントライト（2）リムライトなしのポイントライト（3）リムライトのみのポイントライト（4）nilotoonキャラクターに影響を与えないポイントライト...（4）は特に便利です。Unity URPのレンダリングマスクを使用したくない場合に役立ちます。URPのレンダリングレイヤーは、各レンダラーのレイヤーに個別の設定が必要であり、これは時間がかかり、シーンのゲームオブジェクトやプレハブファイルを散らかす可能性があります。
- メニュー「GameObject > Create Other > NiloToon」に多くの新しいオプションを追加
- メニュー「GameObject > Light > NiloToon」に多くの新しいオプションを追加
- エイリアシングを減らすために「ソフトシャドウ」をシャドウマップに導入し、デフォルトで有効になっています（中品質）
- NiloToonキャラクターシェーダーを更新しました：Basemap Stacking Layer 1-10には+7 UVs（UV1-4、MatcapUV、CharBoundUV、ScreenSpaceUV）と4つのブレンドモードオプション（ノーマル、アド、スクリーン、マルチプライ）が含まれるようになりました。例えば、matcapUVを使ってmultiplyまたはaddブレンド方法でbasemap stacking layerを作成することができます。
- NiloToonPerCharacterRenderControllerの「Basemap Override」グループを更新：+7 UVs（UV1-4、MatcapUV、CharBoundUV、ScreenSpaceUV）、および4つのブレンドモードオプション（ノーマル、アド、スクリーン、マルチプライ）
- NiloToonPerCharacterRenderControllerに新しい「Color Fill」グループを追加しました。これは、異なる効果（例えば、X線ビジョン、壁の後ろにいるキャラクターをCS2/Valorantのように表示するなど）でキャラクターを再描画するためのユーティリティ機能です。
- NiloToonPerCharacterRenderControllerに「Generate Smoothed normal?」トグルを新たに追加しました。これを有効にすると、非fbxプレハブ（例えば、vrmキャラクター）でも、fbxキャラクターと同様のアウトライン品質を持つことができます。
- 発見されたすべてのメモリリークとGC割り当ての問題を修正しました。
- 複数のキャラクターが可視状態のときのCPU最適化が大幅に改善されました（約1.72倍のCPUスピードアップ、30個のユニークなキャラクターと800個のユニークなキャラクターマテリアルを含むテストシーンで）。
- HLSLコードがRiderで読みやすくなりました。

### 開発者からの注意（Unity6/RenderGraph/Warudo）
- NiloToon 0.16.xはUnity2021.3をサポートする最後のバージョンであることが確認されています。
- NiloToon 0.17.0では、Unity2021.3のサポートを完全に削除して、Unity6（RenderGraph）サポートに取り組むことになります。
- あと数回の0.16.xリリースの後、Warudo（Unity2021.3）で公式のNiloToonバージョンとして使用される安定したNiloToon 0.16.xバージョンを提供します。

### 開発者からの注意（追加ライトの書き換え）
**ポイントライト/スポットライトを使用してキャラクターを照らす必要がない場合は、このメッセージをスキップしてください。**
NiloToon 0.16.0には、追加ライトのためのコードの大幅な書き換えが含まれています！

前バージョンのNiloToon 0.15.15では、追加ライトの扱いはUnityの方法に似ており、ライトの色はキャラクターに加算的にブレンドされていました。Unity URPシェーダーとして、私たちの元のデザインはできるだけUnityのURP追加ライトの動作に沿うことを目指していましたが、トゥーンシェーダーには最適でないことは知っていました。

しかし、加算ライトの方法は、[このビデオ](https://youtu.be/ST9qNEfPrmY?si=98mqByySiRE7kcTO)のように非常に慎重にセットアップされた条件下でのみ見栄えが良かったです。ほとんどの通常の使用例では、NiloToon 0.15.15のポイントライト/スポットライトはキャラクターを過度に明るくしたり、奇妙に見せたりすることが容易でした。

したがって、NiloToon 0.16.0では、追加ライトがデフォルトで良い結果を提供するようにするための重要な変更を加える時が来たと判断しました。これで、追加ライトはメインライトと同じように扱われるので、キャラクターを照らす方法や使用するライトの種類に関係なく、照明結果は常にメインの方向光と同じ品質を維持します。つまり、インテリア、部屋、洞窟など、主にポイントライト/スポットライトを使用するシーンは、NiloToon 0.16.0を使ってキャラクターを簡単かつ一貫して綺麗に照らすことができるようになりました。

*以前の追加ライトの結果を出力したい場合は、以下の操作を行うことができます：
- NiloToon Additional Light Style Volumeで、色と方向を0に設定します。
- （オプション）NiloToon Cinematic Rim Light Volumeで、「auto fix unsafe style」を無効にします。

### 破壊的変更
- NiloToon Characterシェーダー：「face normal fix」は、NiloToonPerCharacterRenderControllerによって制御されるマテリアルのみに影響します。
- NiloToon Characterシェーダー：MatCapUVとCharBoundUVの間違ったコードを修正しました。これにより、これらのUVのxが間違って反転するのを修正しました。修正後、MatCapUVとCharBoundUVは、3D球メッシュのTexture2Dと同じように正しく見えるようになり、もうuv xが反転することはありません。ただし、これは破壊的変更をもたらすバグ修正ですので、古いMatCapUVの結果（uv xが反転している）を維持したい場合は、マテリアルの「UV Edit」グループで、MatCapUVのタイリングを(-1,1,_,_)に設定してください。
- NiloToon Characterシェーダー：_BackFaceTintColorはもうアウトラインパスには影響しません。なぜなら、アウトラインが暗くなるのは正しくないからです。
- NiloToon Characterシェーダー：リムライトのuvオフセット方向ベクトルを正規化しました。これにより、すべての照明条件と方向の下でリムライトの幅を一貫させます。
- NiloToon Characterシェーダー：NiloToonDepthOnlyOrDepthNormalPassの顔のノーマル編集を無効にしました。なぜなら、URPの_CameraNormalTextureの顔のノーマルを編集したくないからです。
- NiloToonPerCharacterRenderController：BaseMap OverrideグループのUVタイリングは、中心(0.5,0.5)をタイリングの中心として使用するようになりました。
- NiloToonPerCharacterRenderController：BaseMap Overrideのマップがnullの場合、NiloToonはデフォルトの白い不透明なテクスチャを使用するようになりました。以前は灰色の半透明（50％）テクスチャを使用していました。
- NiloToonPerCharacterRenderController：シーンに多くのキャラクターが含まれている場合、すべてのアクティブなキャラクターを強制的に更新/再インポートすることはなくなりました。なぜなら、それはシーンが多すぎる場合には遅すぎるからです。
- NiloToonPerCharacterRenderController：StencilFill、ExtraThickOutline、ColorFillパスは、不透明なキュー（<2500）のマテリアルのみを描画するようになりました。
- NiloToonCinematicRimLightVolume：「Auto fix unsafe Style?」トグルを追加しました。デフォルトで有効になっており、少なくとも1つのシネマティックリムライトスタイルが強制されます。
- NiloToonCharRenderingControlVolume：specularReactToLightDirectionChangeのデフォルト値をtrueに設定しました。古い結果を再現したい場合は、これを上書きしてfalseに戻すことができます。
- NiloToonVolumePresetアセットのすべて：charOutlineWidthMultiplierを1にリセットしました。

### 破壊的変更（HLSLプログラミングのみ）
- (InternalCore) NiloToon Characterシェーダー：ToonLightingData.PerPixelNormalizedPolygonNormalWSをnormalWS_NoNormalMapに変更しました。
- (InternalCore) NiloToon Characterシェーダー：ToonLightingData.PolygonNdotVをNdotV_NoNormalMapに変更しました。
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader：関数のパラメータを変更しました -> void ApplyCustomUserLogicToBaseColor(inout half4 baseColor, Varyings input, UVData uvData, float facing)。
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader：関数のパラメータを変更しました -> void ApplyCustomUserLogicBeforeFog(inout half3 color, ToonSurfaceData surfaceData, ToonLightingData lightingData, Varyings input, UVData uvData)。
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader：関数のパラメータを変更しました -> void ApplyCustomUserLogicAfterFog(inout half3 color, ToonSurfaceData surfaceData, ToonLightingData lightingData, Varyings input, UVData uvData)。
- (InternalCore) NiloToon Characterシェーダー：間接照明をメインライト注入フェーズに移動しました。

### 追加された機能
- 新しいボリューム「NiloToonAdditionalLightStyleVolume」
- 「GameObject > ... > NiloToon」メニューの「NiloToonCharacterLightController」新しいスクリプト、およびゲームオブジェクト作成方法を追加
- 「GameObject > ... > NiloToon」メニューの「NiloToonLightSourceModifier」新しいスクリプト、およびいくつかのゲームオブジェクト作成方法を追加
- NiloToonAllInOneRendererFeature：ソフトシャドウを追加（デフォルト有効、中品質）
- NiloToonAnimePostProcessVolume：topLightMultiplyLightColorを追加
- NiloToonCharacterMainLightOverrider：overrideTimingを追加
- NiloToonPerCharacterRenderController：新しい「Color Fill」グループを追加
- NiloToonPerCharacterRenderController：LateUpdateで不足しているプロパティを自動的に埋める機能を追加
- NiloToonPerCharacterRenderController：「Generate Smoothed normal?」トグルを追加。非fbxキャラクターに対してもランタイムで同様の品質のアウトラインを生成できるようになりました。
- NiloToonPerCharacterRenderController：「Auto setup character」はPhysicalMaterial3DsMax ArnoldStandardSurfaceの自動設定もサポートするようになりました。
- NiloToonPerCharacterRenderController：BaseMapOverrideグループにUVIndex {UV1-4, MatCapUV, CharBoundUV, ScreenSpaceUV}を追加
- NiloToonPerCharacterRenderController：BaseMapOverrideグループにBlendMode {Normal,Add,Screen,Multiply}を追加
- NiloToon Characterシェーダー：_DepthTexRimLightIgnoreLightDir（360リム）と_DepthTexShadowIgnoreLightDir（360シャドウ）を追加しました。
- NiloToon Characterシェーダー：_SelfShadowAreaHSVStrength を追加しました。
- NiloToon Characterシェーダー：_SkinShadowTintColor2、_FaceShadowTintColor2 を追加しました。
- NiloToon Characterシェーダー：「ノーマルマップ」グループにUVIndex, UVScaleOffset, UVScrollSpeedを追加しました。
- NiloToon Characterシェーダー：新しい「UV Edit」グループを追加し、UV1〜4 & MatCapUVにタイリング、回転、アニメーションなどを追加できるようにしました。
- NiloToon Characterシェーダー：すべての「Basemap stacking layers」グループにマスターストレングススライダーを追加しました。
- NiloToon Characterシェーダー：すべての「Basemap stacking layers」グループにBlendMode {Normal,Add,Screen,Multiply}を追加しました。
- NiloToon Characterシェーダー：すべての「Basemap stacking layers」グループにUVIndex {UV1-4, MatcapUV, CharBoundUV, ScreenSpaceUV}を追加しました。
- NiloToon Characterシェーダー：すべての「Basemap stacking layers」グループにUVscale, position, angle, rotateオプションを追加しました。
- NiloToon Characterシェーダー：ベースマップスタッキングレイヤー4-10を更新し、今ではベースマップスタッキングレイヤー1-3と同じ品質とオプションを持っています。
- NiloToon Characterシェーダー：ノーマルマップは、MatCapUVにも影響を与えるようになりました。
- NiloToon Characterシェーダー：_BackFaceForceShadow、_BackFaceReplaceBaseMapColor を追加しました（liltoon自動セットアップ変換も含む）。
- NiloToon Characterシェーダー：_ReceiveURPShadowMappingAmountForFace、_ReceiveURPShadowMappingAmountForNonFaceを追加しました。
- NiloToon Characterシェーダー：新しいグループ「URP Additional Light ShadowMap」を追加しました。
- NiloToon Characterシェーダー：「Light Style」グループのプリセットを追加しました。
- NiloToon Characterシェーダー：_SkinMaskMapAsIDMap, _SkinMaskMapExtractFromIDを追加しました。
- NiloToon Characterシェーダー：_SpecularMapAsIDMap, _SpecularMapInvertColorを追加しました。
- NiloToon Characterシェーダー：_DepthTexRimLightMaskTexを追加しました。
- TexturesフォルダーにNormalmap_Skin.png, NormalMap_MetalBump.pngを追加し、detailmapグループのユーティリティテクスチャとして使用できます。
- 「Auto setup character」を介して、より多くのliltoonプロパティをNiloToonに移植できるようになりました（作業中）。
- (InternalCore) Riderのサポートを強化しました。RiderでNiloToonのソースコードをC#のように読んだり編集したりできます。'Go To' (F12)、'Go Back' (Ctrl+-)、'Find All Usages' (Shift+F12)などのRider機能を利用できます。

### 変更点
- LWGUI: 1.4.3から1.6.0へアップグレードしました。
- NiloToonPerCharacterRenderController：allowCacheSystemトグルを削除し、NiloToonはマテリアルの参照の変更を自動的に検出し、自動更新を実行するようになりました（ユーザーによるAPI呼び出しは不要で、すべての更新は自動化されています）。
- NiloToonキャラクターシェーダー：ヘルプボックス、ツールチップ、表示名などのUIの改善を行いました。
- あまり役に立たないコンソールのDebug.Log(s)を削除し、NiloToonができるだけコンソールウィンドウをクリーンに保つようにしました。

### 修正点
- NiloToonPerCharacterRenderController：複数のキャラクターがNiloToonのキャラクターごとのスクリプトを使用する場合、CPUの最適化が大幅に向上しました。30キャラクターと800マテリアルでのテストでは、19msから11msに改善しました（スピードアップ = x1.72）。スピードアップは、NiloToonがNiloToonPerCharacterRenderControllerを介してフレームごとのマテリアル更新をいくつか停止したためであり、これによりSRPバッチャーがCPUからGPUへのデータのアップロードを高速パスで使用するようになりました。
- NiloToonPerCharacterRenderController：ユーザーがプレイモードでマテリアルの参照を変更したために発生するメモリリークを修正しました（例：マテリアル参照キーを含むアニメーターアニメーションを再生した場合）。
- NiloToonPerCharacterRenderController：キャラクターの自動セットアップで「マテリアルを編集しない」をクリックすると、スクリプトのフィールドの更新も誤ってスキップされるバグを修正しました。修正後は、スクリプトのフィールドが常に正しく更新されるようになりました。
- NiloToonUberPostProcessPass：誤ったシェーダー名の文字列によるGC割り当てのバグを修正しました。
- NiloToonBloomVolume：非常に広い画面でfixedHeightオプションを使った場合にbloom RTが最大サイズを超えるバグを修正しました。現在はGPUが許可する最大サイズ（通常はPCで16384幅）でキャップされます。
- package.json：「type」を「module」から「tool」に変更し、パッケージマネージャ（UPM）を介してインストールされた場合でもNiloToonURPがプロジェクトウィンドウに表示されるようにしました。
- プレイモードの開始/終了時やゲームのビルド時に発生するいくつかのnull参照例外を修正しました。

---------------------
### 版本亮点
简而言之，与 NiloToon 0.15.15 相比，NiloToon 0.16.0 引入了以下重大变化：
- [重大变更] 重写了额外灯光的代码；现在所有额外的灯光默认被当作主灯光处理，只需使用点光源/聚光灯就能很好地照亮角色（非常适合室内！）
- 添加了一个新的 `NiloToonCharacterLightController` 脚本，允许你控制每个角色接收光照的方式（例如，为几个目标角色给主光源着色或添加颜色）
- 添加了一个新的 `NiloToonAdditionalLightStyleVolume` 体积，使你能够全局控制所有灯光如何影响所有角色
- 添加了一个新的 `NiloToonLightSourceModifier` 脚本，使你能够控制每个灯光如何影响所有角色。例如，你可以创建几个具有不同角色的点光源 -> （1）一个普通点光源 （2）一个没有边缘光的点光源 （3）一个只有边缘光的点光源 （4）一个不影响 nilotoon 角色的点光源...对于（4），如果你不愿意使用 Unity URP 的渲染遮罩，这特别方便，因为 URP 的渲染层要求为每个渲染器的层进行单独设置，这可能既费时又会使你的场景的游戏对象和预制文件变得杂乱无章。
- 菜单 "GameObject > Create Other > NiloToon" 中增加了许多新选项
- 菜单 "GameObject > Light > NiloToon" 中增加了许多新选项
- 引入了 'soft shadow'（柔和阴影）用于阴影图，以减少走样，默认启用（中等质量）
- 更新了 NiloToon 角色着色器：基础图堆叠层 1-10 现在包括 +7 UVs（UV1-4, MatcapUV, CharBoundUV, ScreenSpaceUV）和 4 种混合模式选项（正常，添加，屏幕，乘法）。例如，你可以使用 matcapUV 与乘法或添加混合方法创建基础图堆叠层。
- 更新了 NiloToonPerCharacterRenderController 的 'Basemap Override' 组：+7 UVs（UV1-4, MatcapUV, CharBoundUV, ScreenSpaceUV），和 4 种混合模式选项（正常，添加，屏幕，乘法）
- 在 NiloToonPerCharacterRenderController 中添加了新的 'Color Fill' 组，一个实用功能，用于用不同效果重新绘制角色（例如，X光视觉，显示墙后的角色，类似 CS2/Valorant）
- 在 NiloToonPerCharacterRenderController 中添加了新的 'Generate Smoothed normal?' 开关，你可以为非 fbx 预制件启用它（例如，vrm 角色），以便这些非 fbx 角色预制件仍然可以拥有与 fbx 角色相似的轮廓质量
- 修复了发现的所有内存泄漏和 GC 分配问题
- 当多个角色可见时进行了显著的 CPU 优化（在一个测试场景中，有 30 个独特角色和 800 个独特角色材质，大约提速了 1.72 倍）
- 现在在 Rider 中 HLSL 代码更易于阅读

### 开发者说明（Unity6/RenderGraph/Warudo）
- 确认 NiloToon 0.16.x 是最后一个仍然支持 Unity2021.3 的版本。
- 在 NiloToon 0.17.0 中，我们将完全移除对 Unity2021.3 的支持，以开始支持 Unity6（RenderGraph）。
- 在再发布几个 0.16.x 版本后，我们将提供一个稳定的 NiloToon 0.16.x 版本给 Warudo，作为 Warudo（Unity2021.3）的官方 NiloToon 版本。

### 开发者说明（附加灯光重写）
**如果你不需要使用点光源/聚光灯来照亮角色，请跳过此消息。**
NiloToon 0.16.0 包含对附加灯光代码的重大重写！

在之前的版本 NiloToon 0.15.15 中，附加灯光的处理方式类似于 Unity 的方法，灯光的颜色会加性地混合到角色上。作为一个 Unity URP 着色器，我们的原始设计旨在尽可能地与 Unity 的 URP 附加灯光行为保持一致，尽管我们知道这对卡通着色器来说并不是最好的。

然而，加性光方法只有在非常仔细设置的条件下才看起来不错，就像这个[视频](https://youtu.be/ST9qNEfPrmY?si=98mqByySiRE7kcTO)。在大多数常规用例中，NiloToon 0.15.15 的点光源/聚光灯可能会很容易使角色显得过分明亮或看起来奇怪。

因此，在 NiloToon 0.16.0 中，我们决定是时候做出重大的突破性变化，以确保附加灯光默认提供良好的结果。现在，任何附加灯光都被视为与主灯光相同，所以无论你如何照亮角色，无论使用哪种类型的灯光，照明结果始终会保持与主方向光相同的质量。这意味着主要使用点光源/聚光灯的场景，如室内、房间、洞穴等，现在使用 NiloToon 0.16.0 很容易且一致地很好地照亮角色。

*如果你想产生旧的附加光结果，你可以：
- 在 NiloToon Additional Light Style Volume 中：设置颜色和方向 = 0
- （可选）在 NiloToon Cinematic Rim Light Volume 中：禁用 “auto fix unsafe style”

### 破坏性变更
- NiloToon 角色着色器："face normal fix" 现在只影响由 NiloToonPerCharacterRenderController 控制的材料
- NiloToon 角色着色器：修复了错误的 MatCapUV 和 CharBoundUV 代码，使这些 UV 的 x 错误翻转，现在修复后，MatCapUV 和 CharBoundUV 将正确地与 3D 球体网格上的 Texture2D 看起来相同，不再有 uv x 翻转的问题。但这是一个导致破坏性变更的错误修复，所以如果你想保持旧的 MatCapUV 结果，其中 uv x 翻转了，请转到材料的 "UV Edit" 组，将 MatCapUV 的平铺设置为 (-1,1,_,_)。
- NiloToon 角色着色器：_BackFaceTintColor 不再影响轮廓通过，因为它会使轮廓变暗，这是不正确的
- NiloToon 角色着色器：标准化深度轮廓 uv 偏移方向向量，使得在所有光照条件和方向下边缘光宽度更一致
- NiloToon 角色着色器：禁用了 NiloToonDepthOnlyOrDepthNormalPass 的面法线编辑，因为我们不想编辑 URP 的 _CameraNormalTexture 的面法线
- NiloToonPerCharacterRenderController：BaseMap Override 组的 UV 平铺现在使用中心(0.5,0.5) 作为平铺中心而不是 (0,0)
- NiloToonPerCharacterRenderController：当 BaseMap Override 的 Map 为空时，NiloToon 现在将使用默认的白色不透明纹理而不是灰色和半透明(50%)的纹理
- NiloToonPerCharacterRenderController：不再强制更新/重新导入场景中的所有活动角色，因为当场景中包含太多角色时这会非常慢
- NiloToonPerCharacterRenderController：使得 StencilFill、ExtraThickOutline、ColorFill pass 仅绘制不透明队列（<2500）的材质
- NiloToonCinematicRimLightVolume：添加了 "Auto fix unsafe Style?" 开关，默认启用，这将强制至少开启一个电影边缘光风格
- NiloToonCharRenderingControlVolume：specularReactToLightDirectionChange 现在默认为 true。如果你想重现旧的结果，你可以覆盖它然后将其设置回 false
- 所有 NiloToonVolumePreset 资产：将 charOutlineWidthMultiplier 重置为 1

### 破坏性变更（仅限 HLSL 编程）
- (InternalCore) NiloToon 角色着色器：将 ToonLightingData.PerPixelNormalizedPolygonNormalWS 重命名为 normalWS_NoNormalMap
- (InternalCore) NiloToon 角色着色器：将 ToonLightingData.PolygonNdotV 重命名为 NdotV_NoNormalMap
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader：更改了函数参数 -> void ApplyCustomUserLogicToBaseColor(inout half4 baseColor, Varyings input, UVData uvData, float facing)
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader：更改了函数参数 -> void ApplyCustomUserLogicBeforeFog(inout half3 color, ToonSurfaceData surfaceData, ToonLightingData lightingData, Varyings input, UVData uvData)
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader：更改了函数参数 -> void ApplyCustomUserLogicAfterFog(inout half3 color, ToonSurfaceData surfaceData, ToonLightingData lightingData, Varyings input, UVData uvData)
- (InternalCore) NiloToon 角色着色器：将间接光移至主光注入阶段

### 添加
- + 新的体积 "NiloToonAdditionalLightStyleVolume"
- + 新的脚本 "NiloToonCharacterLightController"，在 "GameObject > ... > NiloToon" 菜单中有创建游戏对象的方法
- + 新的脚本 "NiloToonLightSourceModifier"，在 "GameObject > ... > NiloToon" 菜单中有几种创建游戏对象的方法
- NiloToonAllInOneRendererFeature：+ 柔和阴影（默认启用，中等质量）
- NiloToonAnimePostProcessVolume：+ topLightMultiplyLightColor
- NiloToonCharacterMainLightOverrider：+ overrideTiming
- NiloToonPerCharacterRenderController：+ 新的 "Color Fill" 组
- NiloToonPerCharacterRenderController：+ 在 LateUpdate 中自动填充缺失属性
- NiloToonPerCharacterRenderController：+ "Generate Smoothed normal?" 开关，适用于非 fbx 角色，以便在运行时生成类似质量的轮廓
- NiloToonPerCharacterRenderController：现在支持 "Auto setup character" 自动设置 PhysicalMaterial3DsMax ArnoldStandardSurface
- NiloToonPerCharacterRenderController：BaseMapOverride 组 + UVIndex {UV1-4, MatCapUV, CharBoundUV, ScreenSpaceUV}
- NiloToonPerCharacterRenderController：BaseMapOverride 组 + BlendMode {Normal, Add, Screen, Multiply}
- NiloToon 角色着色器：+ _DepthTexRimLightIgnoreLightDir(360度边缘光) 和 _DepthTexShadowIgnoreLightDir(360度阴影)
- NiloToon 角色着色器：+ _SelfShadowAreaHSVStrength
- NiloToon 角色着色器：+ _SkinShadowTintColor2, _FaceShadowTintColor2
- NiloToon 角色着色器："Normal map" 组 + UVIndex, UVScaleOffset, UVScrollSpeed
- NiloToon 角色着色器：+ 新的 "UV Edit" 组，允许你为 UV1~4 & MatCapUV 添加平铺、旋转、动画......
- NiloToon 角色着色器：所有 "Basemap stacking layers" 组 + 主强度滑块
- NiloToon 角色着色器：所有 "Basemap stacking layers" 组 + BlendMode {Normal, Add, Screen, Multiply}
- NiloToon 角色着色器：所有 "Basemap stacking layers" 组 + UVIndex {UV1-4, MatCapUV, CharBoundUV, ScreenSpaceUV}
- NiloToon 角色着色器：所有 "Basemap stacking layers" 组 + UVscale, position, angle, rotate 选项
- NiloToon 角色着色器：更新了 basemap stacking layer 4-10，现在它们具有与 basemap stacking layer 1-3 相同的质量和选项
- NiloToon 角色着色器：现在 normal map 会影响 MatCapUV
- NiloToon 角色着色器：+_BackFaceForceShadow, _BackFaceReplaceBaseMapColor（包括 liltoon 自动设置转换）
- NiloToon 角色着色器：+_ReceiveURPShadowMappingAmountForFace, _ReceiveURPShadowMappingAmountForNonFace
- NiloToon 角色着色器：+ 新的组 "URP Additional Light ShadowMap"
- NiloToon 角色着色器：+ "Light Style" 组的预设
- NiloToon 角色着色器：+_SkinMaskMapAsIDMap, _SkinMaskMapExtractFromID
- NiloToon 角色着色器：+_SpecularMapAsIDMap, _SpecularMapInvertColor
- NiloToon 角色着色器：+_DepthTexRimLightMaskTex
- + 在 Textures 文件夹中的 Normalmap_Skin.png, NormalMap_MetalBump.png 作为 detailmap 组的工具纹理
- 通过 "Auto setup character" 可以将更多 liltoon 属性移植到 NiloToon（正在进行中）
- (InternalCore) 增强了对 Rider 的支持。你现在可以在 Rider 中阅读和编辑 NiloToon 源码，就像阅读 C# 一样。利用 Rider 的功能，如 'Go To' (F12)，'Go Back' (Ctrl+-)，和 'Find All Usages' (Shift+F12)。

### 变更
- LWGUI：从 1.4.3 升级到 1.6.0
- NiloToonPerCharacterRenderController：移除了 allowCacheSystem 开关，现在 NiloToon 会自动检测材质引用变化并执行自动更新（用户现在不再需要因材质引用变化而进行 API 调用，所有更新都是自动的）
- NiloToon 角色着色器：各种 UI 改进，如帮助框、工具提示、显示名称......
- 移除了那些没太大帮助的控制台 Debug.Log(s)，以便 NiloToon 尽可能保持控制台窗口的清洁

### 修复
- NiloToonPerCharacterRenderController：当多个角色使用 NiloToon 每个角色的脚本时，实现了显著的 CPU 优化。在测试中，包含 30 个角色和 800 个材料的场景，从 19ms 改善到 11ms（速度提升约为 1.72 倍）。这种提速是因为 NiloToon 停止了通过 NiloToonPerCharacterRenderController 进行的每帧材料更新，从而使得 SRP batcher 使用快速路径从 CPU 上传数据到 GPU，因为现在不再每帧编辑材料属性
- NiloToonPerCharacterRenderController：修复了用户在播放模式下更改材料引用（例如，播放包含任何材料引用键的动画器动画）时导致的所有内存泄漏
- NiloToonPerCharacterRenderController：修复了在角色自动设置中点击“不编辑材料”会错误地跳过更新脚本字段的错误。现在修复后，脚本字段将始终正确更新
- NiloToonUberPostProcessPass：由于错误的着色器名称字符串导致的 GC 分配错误已修复
- NiloToonBloomVolume：由于 fixedHeight 选项在非常宽的屏幕上导致 bloom RT 超过最大尺寸，现在在 GPU 允许的最大尺寸上限制，通常 PC 的宽度为 16384
- package.json：将“type”从“module”更改为“tool”，即使通过包管理器（UPM）安装，也允许 NiloToonURP 在项目窗口中显示
- 修复了进入/退出播放模式或构建游戏时发现的几个空引用异常

---------------------
### 버전 하이라이트
간단히 말해서, NiloToon 0.15.15에 비해 NiloToon 0.16.0은 이러한 중요한 변경 사항을 소개합니다:
- [주요 변경] 추가 라이트 코드를 재작성했습니다; 이제 모든 추가 라이트는 기본적으로 메인 라이트로 처리되어, 점/스팟 라이트만으로도 캐릭터를 멋지게 조명할 수 있게 되었습니다(인테리어에 적합!).
- 새로운 'NiloToonCharacterLightController' 스크립트를 추가했습니다. 이 스크립트를 통해 각 캐릭터가 조명을 받는 방식을 제어할 수 있습니다(예: 몇몇 타겟 캐릭터들에게 메인 라이트 색상을 틴트하거나 추가하는 등).
- 새로운 'NiloToonAdditionalLightStyleVolume' 볼륨을 추가하여, 전체 캐릭터에 대한 모든 라이트의 영향을 전역적으로 제어할 수 있게 되었습니다.
- 새로운 'NiloToonLightSourceModifier' 스크립트를 추가하여, 각 라이트가 모든 캐릭터에 미치는 영향을 제어할 수 있게 되었습니다. 예를 들어, 다양한 역할을 가진 몇 개의 포인트 라이트를 생성할 수 있습니다 -> (1)일반 포인트 라이트 (2)림 라이트가 없는 포인트 라이트 (3)림 라이트만 있는 포인트 라이트 (4)nilotoon 캐릭터에 영향을 주지 않는 포인트 라이트... (4)는 특히 Unity URP의 렌더링 마스크를 사용하지 않으려는 경우 유용합니다. URP의 렌더링 레이어는 각 렌더러의 레이어에 대한 개별 설정을 요구하기 때문에, 이는 시간이 많이 걸리고 장면의 게임 오브젝트 및 프리팹 파일을 복잡하게 만들 수 있습니다.
- 메뉴 "GameObject > Create Other > NiloToon"에 많은 새로운 옵션을 추가했습니다.
- 메뉴 "GameObject > Light > NiloToon"에 많은 새로운 옵션을 추가했습니다.
- 앨리어싱을 줄이기 위해 'soft shadow'를 그림자 맵에 도입했으며, 기본적으로 활성화되어 있습니다(중간 품질).
- NiloToon 캐릭터 쉐이더를 업데이트했습니다: 베이스맵 스택 레이어 1-10에 이제 +7 UV(UV1-4, MatcapUV, CharBoundUV, ScreenSpaceUV)와 4가지 블렌드 모드 옵션(노멀, 애드, 스크린, 멀티플라이)이 포함되어 있습니다. 예를 들어, matcapUV를 사용하여 멀티플라이나 애드 블렌드 방식으로 베이스맵 스택 레이어를 만들 수 있습니다.
- NiloToonPerCharacterRenderController의 'Basemap Override' 그룹을 업데이트했습니다: +7 UV(UV1-4, MatcapUV, CharBoundUV, ScreenSpaceUV)와 4가지 블렌드 모드 옵션(노멀, 애드, 스크린, 멀티플라이).
- NiloToonPerCharacterRenderController에 새로운 'Color Fill' 그룹을 추가했습니다. 다른 효과로 캐릭터를 다시 그릴 수 있는 유틸리티 기능입니다(예: X레이 비전, CS2/Valorant와 유사하게 벽 뒤에 있는 캐릭터를 보여줌).
- NiloToonPerCharacterRenderController에 새로운 'Generate Smoothed normal?' 토글을 추가했습니다. 이를 비fbx 프리팹(예: vrm 캐릭터)에 활성화할 수 있어, 이러한 비fbx 캐릭터 프리팹도 fbx 캐릭터와 유사한 퀄리티의 윤곽을 유지할 수 있습니다.
- 발견된 모든 메모리 누수와 GC 할당 문제를 수정했습니다.
- 여러 캐릭터가 보일 때 상당한 CPU 최적화를 달성했습니다(30개의 고유 캐릭터와 800개의 고유 캐릭터 재질을 사용한 테스트 장면에서 약 1.72배의 CPU 속도 향상).
- 이제 HLSL 코드가 Rider에서 훨씬 쉽게 읽을 수 있습니다.

### 개발자의 메모 (Unity6/RenderGraph/Warudo)
- NiloToon 0.16.x는 여전히 Unity2021.3을 지원하는 마지막 버전임이 확인되었습니다.
- NiloToon 0.17.0에서는 Unity2021.3에 대한 지원을 완전히 중단하고 Unity6 (RenderGraph) 지원 작업을 시작합니다.
- 0.16.x 버전을 몇 개 더 출시한 후, Warudo에 사용될 공식 NiloToon 버전으로 안정적인 NiloToon 0.16.x 버전을 제공할 예정입니다(Unity2021.3용).

### 개발자의 메모 (추가 조명 재작성)
**점/스팟 라이트를 사용하여 캐릭터에 조명을 제공할 필요가 없다면 이 메시지는 건너뛰세요.**
NiloToon 0.16.0은 추가 라이트 코드에 대한 주요 재작성을 포함하고 있습니다!

이전 버전인 NiloToon 0.15.15에서는 추가 라이트의 처리가 Unity의 방식과 유사했으며, 라이트 색상이 캐릭터에 가산적으로 혼합되었습니다. Unity URP 쉐이더로서, 우리의 원래 디자인은 가능한 한 Unity의 URP 추가 조명 동작과 일치하려고 했지만, 툰 쉐이더에는 최적이 아니라는 것을 알고 있었습니다.

그러나 가산 조명 방법은 [이 비디오](https://youtu.be/ST9qNEfPrmY?si=98mqByySiRE7kcTO)와 같이 매우 신중하게 설정된 조건 하에서만 좋아 보였습니다. 대부분의 일반적인 사용 사례에서 NiloToon 0.15.15의 점/스팟 라이트는 쉽게 캐릭터를 과도하게 밝게 만들거나 이상하게 보이게 했습니다.

따라서 NiloToon 0.16.0에서는 추가 라이트가 기본적으로 좋은 결과를 제공하도록 중요한 변경을 결정할 때라고 생각했습니다. 이제 모든 추가 라이트는 주 라이트와 동일하게 처리되므로, 캐릭터에 어떻게 조명을 하든, 사용하는 라이트의 유형에 상관없이, 조명 결과는 항상 메인 방향성 라이트와 동일한 품질을 유지할 것입니다. 이것은 내부, 방, 동굴 등 주로 점/스팟 라이트를 사용하는 장면들이 이제 NiloToon 0.16.0으로 캐릭터를 아름답고 일관성 있게 조명하는 것이 훨씬 쉬워졌음을 의미합니다.

*예전의 추가 라이트 결과를 만들고 싶다면, 다음과 같이 할 수 있습니다:
- Nilotoon Additional Light Style Volume에서: 색상 & 방향 = 0으로 설정
- (선택사항) Nilotoon Cinematic Rim Light Volume에서: "auto fix unsafe style"을 비활성화

### 변경 사항
- NiloToon Character shader: "face normal fix"는 이제 NiloToonPerCharacterRenderController에 의해 제어되는 재질에만 영향을 줍니다.
- NiloToon Character shader: MatCapUV & CharBoundUV의 잘못된 코드를 수정하여 이 UV들의 x축을 잘못 뒤집는 문제를 해결했습니다. 이제 수정 후에 MatCapUV & CharBoundUV는 3D 구체 메쉬의 Texture2D와 정확히 같은 모습을 하게 되며, 더 이상 UV x축이 뒤집히지 않습니다. 그러나 이는 버그 수정으로 인한 주요 변경 사항이므로, uv x축이 뒤집힌 예전의 MatCapUV 결과를 유지하고 싶다면, 재질의 "UV Edit" 그룹으로 가서 MatCapUV의 타일링을 (-1,1,_,_)로 설정하세요.
- NiloToon Character shader: _BackFaceTintColor는 이제 윤곽선 패스에 영향을 주지 않으며, 이것은 윤곽선을 어둡게 만들어 올바르지 않기 때문입니다.
- NiloToon Character shader: 모든 조명 조건과 방향에서 림 라이트 너비를 일관되게 만들기 위해 깊이 림 UV 오프셋 방향 벡터를 정규화합니다.
- NiloToon Character shader: URP의 _CameraNormalTexture의 얼굴 법선을 편집하지 않으려고 하므로 NiloToonDepthOnlyOrDepthNormalPass의 얼굴 법선 편집을 비활성화합니다.
- NiloToonPerCharacterRenderController: BaseMap Override 그룹의 UV 타일링은 이제 (0,0) 대신 중심(0.5,0.5)을 타일링 센터로 사용합니다.
- NiloToonPerCharacterRenderController: BaseMap Override의 Map이 null일 때, NiloToon은 이제 회색 & 반투명(50%) 텍스처 대신 기본 흰색 불투명 텍스처를 사용합니다.
- NiloToonPerCharacterRenderController: 장면에 너무 많은 캐릭터가 포함되어 있을 때 너무 느려지기 때문에 더 이상 장면에 있는 모든 활성 캐릭터를 강제로 업데이트/재가져오기하지 않습니다.
- NiloToonPerCharacterRenderController: StencilFill, ExtraThickOutline, ColorFill 패스가 이제 불투명 큐(<2500) 재질만 그리도록 만듭니다.
- NiloToonCinematicRimLightVolume: "Auto fix unsafe Style?" 토글을 추가하여 기본적으로 활성화되며, 적어도 하나의 시네마틱 림 라이트 스타일을 강제로 적용합니다.
- NiloToonCharRenderingControlVolume: specularReactToLightDirectionChange의 기본값이 이제 true입니다. 옛 결과를 재현하고 싶다면 재정의하여 false로 다시 설정할 수 있습니다.
- 모든 NiloToonVolumePreset 자산: charOutlineWidthMultiplier을 1로 리셋

### 주요 변경 사항(HLSL 프로그래밍 전용)
- (InternalCore) NiloToon 캐릭터 쉐이더: ToonLightingData.PerPixelNormalizedPolygonNormalWS의 이름을 normalWS_NoNormalMap으로 변경
- (InternalCore) NiloToon 캐릭터 쉐이더: ToonLightingData.PolygonNdotV의 이름을 NdotV_NoNormalMap으로 변경
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader: 함수 매개변수 변경 -> void ApplyCustomUserLogicToBaseColor(inout half4 baseColor, Varyings input, UVData uvData, float facing)
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader: 함수 매개변수 변경 -> void ApplyCustomUserLogicBeforeFog(inout half3 color, ToonSurfaceData surfaceData, ToonLightingData lightingData, Varyings input, UVData uvData)
- (InternalCore) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.shader: 함수 매개변수 변경 -> void ApplyCustomUserLogicAfterFog(inout half3 color, ToonSurfaceData surfaceData, ToonLightingData lightingData, Varyings input, UVData uvData)
- (InternalCore) NiloToon 캐릭터 쉐이더: 간접광을 메인 라이트 주입 단계로 이동

### 추가된 기능
- + 새로운 볼륨 "NiloToonAdditionalLightStyleVolume"
- + 새로운 스크립트 "NiloToonCharacterLightController"는 "GameObject > ... > NiloToon" 메뉴에서 게임 오브젝트를 생성하는 방법을 제공합니다.
- + 새로운 스크립트 "NiloToonLightSourceModifier"는 "GameObject > ... > NiloToon" 메뉴에서 몇 가지 게임 오브젝트를 생성하는 방법을 제공합니다.
- NiloToonAllInOneRendererFeature: + 소프트 쉐도우(기본 활성화, 중간 품질)
- NiloToonAnimePostProcessVolume: + topLightMultiplyLightColor
- NiloToonCharacterMainLightOverrider: + overrideTiming
- NiloToonPerCharacterRenderController: + 새로운 "Color Fill" 그룹
- NiloToonPerCharacterRenderController: + LateUpdate에서 누락된 속성을 자동으로 채움
- NiloToonPerCharacterRenderController: +"Generate Smoothed normal?" 토글, non-fbx 캐릭터에도 실행 시 유사한 품질의 베이크된 윤곽을 생성할 수 있습니다.
- NiloToonPerCharacterRenderController: "Auto setup character"는 이제 PhysicalMaterial3DsMax ArnoldStandardSurface의 자동 설정을 지원합니다.
- NiloToonPerCharacterRenderController: BaseMapOverride 그룹 + UVIndex {UV1-4, MatcapUV, CharBoundUV, ScreenSpaceUV}
- NiloToonPerCharacterRenderController: BaseMapOverride 그룹 + BlendMode {Normal, Add, Screen, Multiply}
- NiloToon 캐릭터 쉐이더: + _DepthTexRimLightIgnoreLightDir(360도 림) 및 _DepthTexShadowIgnoreLightDir(360도 쉐도우)
- NiloToon 캐릭터 쉐이더: + _SelfShadowAreaHSVStrength
- NiloToon 캐릭터 쉐이더: + _SkinShadowTintColor2, _FaceShadowTintColor2
- NiloToon 캐릭터 쉐이더: "Normal map" 그룹 + UVIndex, UVScaleOffset, UVScrollSpeed
- NiloToon 캐릭터 쉐이더: 새로운 "UV Edit" 그룹을 추가하여 UV1~4 및 MatcapUV에 타일링, 회전, 애니메이션 등을 추가할 수 있습니다.
- NiloToon 캐릭터 쉐이더: 모든 "Basemap stacking layers" 그룹에 주 강도 슬라이더를 추가
- NiloToon 캐릭터 쉐이더: 모든 "Basemap stacking layers" 그룹에 BlendMode {Normal,Add,Screen,Multiply}를 추가
- NiloToon 캐릭터 쉐이더: 모든 "Basemap stacking layers" 그룹에 UVIndex {UV1-4, MatcapUV, CharBoundUV, ScreenSpaceUV}를 추가
- NiloToon 캐릭터 쉐이더: 모든 "Basemap stacking layers" 그룹에 UVscale, position, angle, rotate 옵션을 추가
- NiloToon 캐릭터 쉐이더: basemap stacking layer 4-10을 업데이트하여 이제 basemap stacking layer 1-3과 동일한 품질 및 옵션을 가짐
- NiloToon 캐릭터 쉐이더: 이제 normal map은 MatCapUV에 영향을 줍니다.
- NiloToon 캐릭터 쉐이더: +_BackFaceForceShadow, _BackFaceReplaceBaseMapColor (liltoon 자동 설정 변환 포함)
- NiloToon 캐릭터 쉐이더: +_ReceiveURPShadowMappingAmountForFace, _ReceiveURPShadowMappingAmountForNonFace
- NiloToon 캐릭터 쉐이더: 새 그룹 "URP Additional Light ShadowMap" 추가
- NiloToon 캐릭터 쉐이더: "Light Style" 그룹의 프리셋 추가
- NiloToon 캐릭터 쉐이더: +_SkinMaskMapAsIDMap, _SkinMaskMapExtractFromID
- NiloToon 캐릭터 쉐이더: +_SpecularMapAsIDMap, _SpecularMapInvertColor
- NiloToon 캐릭터 쉐이더: +_DepthTexRimLightMaskTex
- Textures 폴더에 Normalmap_Skin.png, NormalMap_MetalBump.png를 유틸리티 텍스처로 추가하여 detailmap 그룹에 사용
- "Auto setup character"를 통해 더 많은 liltoon 속성을 NiloToon으로 이전할 수 있음 (작업 중)
- (InternalCore) Rider 지원 향상. 이제 Rider에서 NiloToon 소스 코드를 C#처럼 읽고 편집할 수 있습니다. 'Go To' (F12), 'Go Back' (Ctrl+-), 'Find All Usages' (Shift+F12) 같은 Rider 기능을 활용할 수 있습니다.

### 변경된 사항
- LWGUI: 버전 1.4.3에서 1.6.0으로 업그레이드
- NiloToonPerCharacterRenderController: allowCacheSystem 토글을 제거하고, 이제 NiloToon은 재질 참조 변경을 자동으로 감지하고 자동 업데이트를 수행합니다(사용자는 이제 재질 참조 변경으로 인한 API 호출이 필요 없으며, 모든 업데이트가 자동화됨).
- NiloToon 캐릭터 쉐이더: 도움말 상자, 툴팁, 표시 이름 등과 같은 다양한 UI 개선 사항.
- 유용하지 않은 콘솔 Debug.Log(s)를 제거하여 NiloToon이 콘솔 창을 가능한 한 깨끗하게 유지하려고 합니다.

### 수정된 사항
-NiloToonPerCharacterRenderController: 여러 캐릭터가 NiloToon 개별 캐릭터 스크립트를 사용할 때 상당한 CPU 최적화를 달성했습니다. 30개 캐릭터와 800개의 재질을 사용한 테스트에서 시간이 19ms에서 11ms로 개선되었습니다(속도 향상 = x1.72). 이 속도 향상은 NiloToonPerCharacterRenderController를 통한 몇몇 프레임당 재질 업데이트를 중단함으로써 이루어졌으며, 이는 SRP 배처가 CPU에서 GPU로 데이터를 업로드하는 빠른 경로를 사용하게 만들었기 때문입니다. 이제 재질 속성이 프레임당 수정되지 않습니다.
- NiloToonPerCharacterRenderController: 플레이 모드에서 사용자가 재질 참조를 변경함으로써 발생한 모든 메모리 누수를 수정했습니다(예: 재질 참조 키가 포함된 애니메이터 애니메이션을 재생하는 경우).
- NiloToonPerCharacterRenderController: 캐릭터 자동 설정에서 "재질을 편집하지 않음"을 클릭하면 스크립트 필드의 업데이트가 잘못 건너뛰어지는 버그를 수정했습니다. 이제 수정 후에는 스크립트 필드가 항상 올바르게 업데이트됩니다.
- NiloToonUberPostProcessPass: 잘못된 쉐이더 이름 문자열로 인해 발생한 GC 할당 버그를 수정했습니다.
- NiloToonBloomVolume: 매우 넓은 화면에서 fixedHeight 옵션으로 인해 블룸 RT가 최대 크기를 초과하는 문제를 수정했습니다. 이제 GPU가 허용하는 최대 크기에 맞춰져 있으며, 보통 PC의 경우 너비는 16384입니다.
- package.json: "type"을 "module"에서 "tool"로 변경하여, 패키지 관리자(UPM)를 통해 설치되었을 때에도 NiloToonURP가 프로젝트 창에 표시되도록 했습니다.
- 플레이 모드에 들어가거나 나올 때 또는 게임을 빌드할 때 발견된 몇 가지 null 참조 예외를 수정했습니다.

----------------------------------------------
## [0.15.15] - 2023-12-13

### Fixed
- NiloToonUberPostProcessPass.cs: Unity 2022.3 or above's code compile error
----------------------------------------------
## [0.15.14] - 2023-12-13

### Added
- Tonemapping: +GT Tonemap mode (GranTurismo Tonemap, usually a better mode for NPR than ACES/Neutral)
- Tonemapping: + more settings and improve volume UI
- Bloom: + Post-Bloom Result brightness control

### Changed
- All script's with LateUpdate has a later ExecuteOrder than default
----------------------------------------------
## [0.15.13] - 2023-12-11

### Note to user
- Changelog will try to be as short as possible from now on, to allow artist(non programmer) to read it easier

### Breaking Change
- Character shader: reduce top rim light width fadeout range from 10% to 5%

### Added
- VolumePresetPicker: +024,025 preset

### Changed
- Character shader: move "Lighting Style" to normal UI group, so it can be edited using default UI
- LWGUI: upgrade from 1.13.6 -> 1.14.3
- Auto Install: hide DebugLog on recompile or editor start

### Fixed 
- Shaders & ShaderLibrary folders: + asmdef files, to allow .cs generate for IDE
- VolumePresetPicker: has it's own editor script now, to allow better asembly structure
----------------------------------------------
## [0.15.12] - 2023-12-06

### Breaking Change
- (Core) NiloToonVolumePresetPicker: rewrite the whole script, delete all sub volume prefabs. Now NiloToonVolumePresetPicker.prefab is much simplier to understand, check volume values and use. 
- (Core) changes NiloToonVolumePreset 002,004,006,009,010,011,012,018. Make them more conservative to fit user's usecase

### Added
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: Auto setup character button will now handle the material conversion of "Complex Lit", "Simple Lit", "Unlit" also
- (Core) + 019~023 NiloToonVolumePreset profile
- (InternalCore) +Runtime\Utility\NiloToonRenderingUtils.cs

### Changed
- (Core) NiloToonCharacter.shader: rename title "Draw Outline in Depth Texture" -> "Support Depth of Field"

### Fixed
- (Core) NiloToonUberPostProcessPass.cs: fix null exception on editor first startup or NiloToon's first install
- (Core) NiloToonAnimePostProcessPass.cs: fix enderingUtils.fullscreenMesh obsolete warning
----------------------------------------------
## [0.15.11] - 2023-11-24

### Added
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: +lilToon converter, when "auto setup character" from lilToon(1.4.1) characters, NiloToon will now try to port properties from lilToon material to NiloToon material (still WIP, can only port basic properties now)
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: new menuitem "Window/NiloToonURP/Setup selected character GameObject", it is just adding a "NiloToonPerCharacterRenderController" script on that gameobject, and click the "auto setup" button for you 
- (Core) NiloToonCharacter.shader: _AlphaOverrideTexInvertColor, _AlphaOverrideTexValueScale, _AlphaOverrideTexValueOffset 
- (Core) NiloToonCharacter.shader: _AlphaOverrideMode added subtract type at index 3 
- (Core) NiloToonAnimePostProcessPass.cs: expose _TopLightSunTintColor
- (InternalCore) +[DisableIf()] Attribute

### Changed
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: For Unity2022.3 or higher, auto setup character will not reimport inactive character in scene anymore, for setup speed optimization
- (Core) NiloToonCharacterMainLightOverrider.cs: disable property edit if the override toggle is off
- (Core) Auto setup character will now consider material with "body" as IsSkin material

### Fixed
- (Core) NiloToonPerCharacterRenderController.cs: fix bound null exception, when character has null renderers
- (Core) NiloToonUberPostProcessPass.cs: fix NiloToonTonemapping bug -> when NiloToonTonemapping is off, the character becomes pure black when NiloToonBloom is on
- (Core) NiloToonUberPostProcessPass.cs: NiloToonTonemapping will try to run on HDR Grading mode also, even if the result is not be accurate
- (Core) NiloToonRendererFeatureAutoAdd.cs: fix null exception when project contains BIRP/HDRP RPasset
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: fix null exception when auto setup a character with null renderer/mesh
- (Core) NiloToonCharacter_Shared.hlsl: fix "HQ continuous outline" toggle not working, fix "classic outline" disappear when extrathick outline is on
----------------------------------------------
## [0.15.10] - 2023-11-21

### Added
- + NiloToonTonemappingVolume.cs, a new volume component that is similar to URP's tonemapping, but will not apply to character pixels. Using this instead of URP's tonemapping will keep the character area's color unaffected, which is usually good for NPR characters
- (Core) + many NiloToon volume presets (008~018) 
- (Core) NiloToonRendererRedrawer.cs: +deactivateWhenRendererIsNotVisible (default true) to save CPU time from offscreen characters
- (Core) NiloToonCharacterMainLightOverrider.cs: +[CanEditMultipleObjects]
- (Core) NiloToonRendererRedrawer.cs: +[CanEditMultipleObjects]

### Changed
- [MenuItem("Window/NiloToonURP/Utility/Select all Materials using NiloToon_Character")] to [MenuItem("Window/NiloToonURP/Select all NiloToon_Character Materials", priority = 20)]
- Shaders\NiloToonCharacter.shader: improve stencil UI, + preset
- Runtime\Volume\NiloToonCharRenderingControlVolume.cs: charIndirectLightMaxColor default white->gray
----------------------------------------------
## [0.15.9] - 2023-11-19

### Breaking Change
- (Core) move VolumePrefabs folder into Runtime folder, so NiloToonURP can be installed from disk in package manager
- (Core) rename folder of VolumePrefabs to VolumePresetPicker
- (Core) NiloToon Character shader: +_UIDisplayMode (default Normal), it will hide uncommon groups from the material inspector, to help new user focus on important groups only. User can decide hiding how many groups. This option is at the top position in the material inspector.

### Added
- (Core) NiloToon Character shader: _SkinFaceShadowColorPreset, _StencilPreset, _EnableShadowColor, _BaseMapBrightness

### Changed
- (Core) NiloToonEditorCreateObjectMenu.cs: use full name of NiloToonCharacterMainLightOverrider in MenuItem to help user search it in editor
- (Core) NiloToonShaderStrippingSettingSO.cs: CreateAssetMenu use a shorter name = CreateNiloToonShaderStrippingSettingSO -> NiloToonShaderStrippingSetting
- (Core) NiloToonCharacter.shader: merge group "Shadow Color Map" into group "Shadow Color"
- (Core) upgrade LWGUI from 1.13.5 -> 1.13.6
----------------------------------------------
## [0.15.8] - 2023-11-15

### Added
- (Core) All NiloToon volumes: + 'Show Help Box?', 'Hide non-override?' toggles
- (Core) All NiloToon volumes: some properties editor display name renamed to shorter names
- (Core) All NiloToon volumes: some properties + tooltips

### Fixed
- (Core) Runtime\NiloToonRendererRedrawer.cs: fix a bug that the draw doesn't match MeshRenderer in scale 
----------------------------------------------
## [0.15.7] - 2023-11-14

### Breaking Change
- (Core) NiloToon will try to automatically add NiloToon's renderer features to all active renderers of user's project, if user allow it via a pop up window. If this new auto install pop up window is very annoying, let us know your use case, contact us (nilotoon@gmail.com)

### Added
- (Core) + Editor\RendererFeatureAutoAdd\NiloToonRendererFeatureAutoAdd.cs, this script will handle all "auto install NiloToon renderer feature" logic
- (Core) RendererFeatureAutoAdd\NiloToonRendererFeatureAutoAdd.cs: allow user to add NiloToon's renderer feature via [MenuItem("Window/NiloToonURP/Auto Install RendererFeatures", priority = 10)] 
- (Core) NiloToonCharRenderingControlVolume: + "Hide non-override?" toggle
- (Core) NiloToonCharRenderingControlVolume: + helpbox

### Changed
- (Core) LWGUI upgraded from 1.13.5(dev) to 1.13.5(release)

### Fixed
- (Core) Editor\NiloToonEditorShaderStripping.cs: fix ShaderVariantLogLevel compile error in Unity2022.3
----------------------------------------------
## [0.15.6] - 2023-11-13

### Added
- (Core) + NiloToonVolumePresetPicker.cs and NiloToonVolumePresetPicker.prefab, allow user to use some volume profile preset provided by NiloToon
- (Core) + able to create NiloToonVolumePresetPicker.prefab from GameObject menu "GameObject/Volume/NiloToon/NiloToonVolumePresetPicker"
- (Core) + Nilo000, Nilo005, Nilo006 volume prefabs and their volume profiles
- (Core) + NiloToonVolumeComponentEditor.cs, allow better GUI for VolumeComponent
- (Core) + NiloToonCharRenderingControlVolumeEditor.cs, for improve GUI using NiloToonVolumeComponentEditor

### Changed
- (Core) NiloToonCharRenderingControlVolume.cs: rename the display name and header of all properties, improve user experience a lot due to new display name

### Fixed
- (Core) NiloToonRendererRedrawer.cs: fix rendering not sync with parent lossy scale bug
- (Core) NiloToonRendererRedrawer.cs: fix a bug that user can't switch to another material in runtime
- (InternalCore) NiloToon.NiloToonURP.Editor.asmdef: +Unity.RenderPipelines.Core.Runtime
----------------------------------------------
## [0.15.5] - 2023-11-12

### Fixed
- (Core) NiloToonRendererRedrawer.cs: use SkinnedMeshRenderer.BakeMesh() to ensure Graphics.RenderMesh() 100% sync with SkinnedMeshRenderer
- (Core) NiloToonPerCharacterRenderController.cs: fix asset bundle load error due to generate material instance in OnValidate()
----------------------------------------------
## [0.15.4] - 2023-11-11

### Breaking Change
- (Core) NiloToonRendererRedrawer.cs: Completely rewrite NiloToonRendererRedrawer! It is now a much more practical script, you can use it like an extension renderer on top of Unity's Renderer, allow you to add extra materials to a Unity's Renderer, and you can decide which SubMesh to draw. The material to draw will be the same as rendered by any Unity's Renderer (material's render queue, shader passes, lighting & shadow, batching... will all work as expected, due to the use of Graphics.RenderMesh() in LateUpdate() instead of cmd.DrawRenderer() in renderer feature). Now this script works in build & editor(edit mode, play mode, prefab mode). 
- (InternalCore) Deprecate NiloToonRendererRedrawerPass.cs, due to NiloToonRendererRedrawer's complete rewrite

### Added
- (InternalCore) + Shaders\NiloToonEnvironment_HLSL\NiloToonEnvironment_ExtendFunctionsForUserCustomLogic.hlsl, allow you to inject hlsl code into the nilotoon environment shader
- (Core) Added 2 more example NiloToon volume preset prefabs, for user copy or use it (NiloToonURP/ExampleAssets/VolumePrefabs)
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: also find "spine" as extra alternative bone if no "customCharacterBoundCenter" found

### Changed
- (InternalCore) NiloToonEnvironment_LitForwardPass.hlsl: apply NiloToonEnvironment_ExtendFunctionsForUserCustomLogic.hlsl
- (Core) NiloToonCinematicRimLightVolume.cs: strengthRimMask3D_ClassicStyle removed experimental tag
- (Core) NiloToonPerCharacterRenderController: OnValidate() always call LateUpdate() in playmode, to ensure game window correctness when user editing inspector value

### Fixed
- (Core) NiloToonPerCharacterRenderController.cs: fix an important build crash bug when headBoneTransform is null
- (Core) NiloToonCharacterMainLightOverrider.cs: +[DisallowMultipleComponent]
----------------------------------------------
## [0.15.3] - 2023-11-06

### Breaking Change
- (Core) Added new shadow receiver depth bias & norma bias, default ON. These new shadow receiver bias are not the same as the already existed shadow caster's bias. These new shadow receiver bias should solve most of the shadow acne problem, in any shadowmap size and zooming scale(shadow range).
- (Core) NiloToonPerCharacterRenderController.cs: Added support to VRMBlendShapeProxy's material control in playmode, but now NiloToonPerCharacterRenderController will generate material instances on OnEnabled() instead of LateUpdate(), to make sure material instances are generated before VRMBlendShapeProxy's Start().
- (Core) NiloToonPerCharacterRenderController.cs: + toggle keepMaterialInstanceNameSameAsOriginal (default ON) in a new "Misc" group, for supporting VRMBlendShapeProxy 
- (Core) NiloToonCharacter_RenderFace_LWGUI_ShaderPropertyPreset: + control to property _CullNiloToonSelfShadowCaster. To avoid nilotoon character produce bad shadow on double face polygons, now the preset is "when rendering front face, only back face polygon will be renderered into NiloToon's character self shadowmap".

### Added
- (Core) NiloToonAllInOne renderer feature: + receiverDepthBias,receiverNormalBias  
- (Core) NiloToon Character shader: +_NiloToonSelfShadowIntensity
- (Core) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.hlsl: added new method ApplyCustomUserLogicToVertexShaderOutputAtVertexShaderEnd(...)
- (Core) NiloToonCharacterMainLightOverrider.cs: added helpbox to hint user "main directional light's intensity must be > 0"

### Changed
- (Core) NiloToonCharacter.shader: Apply more LWGUI new feature to UI, e.g.,"Receive Nilo ShadowMap?"/"Is Skin?"/"Is Face?" sections
- (Core) NiloToonCharSelfShadowMapRTPass.cs: default shadowMapSize 4096->8192
- (Core) NiloToonCharSelfShadowMapRTPass.cs: defualt normalBias 1->0.5

### Fixed
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: add more null material/renderer check when doing "Auto setup character"
- (Core) Update LWGUI from 1.13.4 to 1.13.5, which will fix more material UI null exception bugs
----------------------------------------------
## [0.15.2] - 2023-10-31

### Added
- (Core) Added 1 more example NiloToon volume preset prefabs, for user copy or use it (NiloToonURP/ExampleAssets/VolumePrefabs)

### Fixed
- (Core) Update LWGUI from 1.13.2 to 1.13.4, which will fix most of the material UI bugs
----------------------------------------------
## [0.15.1] - 2023-10-30

### Added
- (Core) Added 2 example NiloToon volume preset prefabs, let user copy or use it (NiloToonURP/ExampleAssets/VolumePrefabs)

### Changed
- (Core) Update LWGUI from 1.13.0 to 1.13.2, which will fix some material UI bugs
- (Core) NiloToonCharacter.shader: rename Face Shadow Gradient Map -> Face Shadow Gradient Map (SDF)
- (Core) NiloToonPerCharacterRenderController.cs: better log when NiloToon renderer feature is not yet setup
- (Core) NiloToon character shader: remove face additional light when cinematic rim light is on
- (Core) NiloToon character shader: add a fix to "2D rim light is weird at screen edge" problem (e.g., hair 2D rim light touching the top side of the screen)

### Fixed
- (Core) NiloToonAllInOneRendererFeature: fix a possible missing/null pass
----------------------------------------------
## [0.15.0] - 2023-10-24

### Version Highlight
- Started a UI rework on NiloToon character shader and NiloToonShaderStrippingSettingSO
- Tested with all 5 available Unity versions from the UnityHub(Unity2021.3.31f1 ~ Unity2023.3.0a10)
- Solved all GC Alloc and RTHandle performance problem
- Build is now faster, smaller, and has lower shader memory usage if user rely on the default stripping settings
- We expect NiloToon 0.15.x will become the next stable version, replacing 0.13.6 within the year 2023

### Known Issue
- For Unity 2022.3.x, please use version 2022.3.11f1 or later to avoid the URP RenderTexture memory leak issue found in earlier versions (e.g., Unity 2022.3.9f1). Although this is not a NiloToon bug, the severity of this URP issue necessitates user notification.
- In Unity 2022.3 or later, the material UI for NiloToon_Character might disappear from the Material Inspector when two conditions are met: 1) in PlayMode, and 2) when the material becomes an instance due to the C# Renderer.materials call. This issue can be reproduced when viewing the material instance from a Renderer component. We are currently awaiting a resolution from LWGUI.

### Breaking change
- (Core) NiloToonShaderStrippingSettingSO.cs: In previous versions, NiloToonShaderStrippingSettingSO attempted to include most of the possible shader keywords in the build to ensure correct keywords by default. However, this resulted in high default build time, size, and shader memory usage. To mitigate this, the default setting for NiloToon shader stripping will now be more aggressive, significantly reducing these metrics. To ensure all required keywords of your project are correctly included in build, you should configure NiloToonShaderStrippingSettingSO for each project to achieve the desired build stripping.
- (Core) NiloToonShaderStrippingSettingSO.cs: +WebGL stripping override (same default setting as mobile), it is default enabled so it is a breaking change
- (Core) NiloToonCharRenderingControlVolume: + charIndirectLightMaxColor, default clamp at white. If you have bright(intensity > 1) light probes baked, this change will affect you
- (Core) NiloToon Character shader: change the apply order of matcap (color replace), environment reflection. Now environment reflection is applied after matcap (color replace)
- (Core) NiloToon Character shader: fix the apply order of BackFaceColorTint and PerCharacterBaseMapOverride. Now PerCharacterBaseMapOverride is applied after BackFaceColorTint
- (InternalCore) NiloToonSetToonParamPass.cs: remove the public static access of EnableDepthTextureRimLigthAndShadow

### Added
- (Core) NiloToon Character shader: +_OverrideShadowColorByTexMode,_OverrideShadowColorMaskMap
- (Core) NiloToon Character shader: +_EmissionMapUVScrollSpeed
- (Core) NiloToon Character shader: +_AlphaOverrideMode
- (Core) NiloToon Character shader: NiloToon self shadow map now supports XR
- (Core) + Textures\PureWhite4x4.png
- (Core) + more Tooltips to NiloToon renderer features properties
- (InternalCore) NiloToon Character shader: + WIP ScreenSpace Outline V2 code, it is still in a very early stage WIP, so user can't use it now.
- (InternalCore) NiloToon Character shader: + Rider resharper support, the support is WIP and not fully complete, but now you can see some nice hlsl code highlight and autopcomplete when reading the hlsl in Rider.

### Changed
- (Core) NiloToonCharacter.shader: initiated a UI rework(without breaking change), with further improvements scheduled for release in subsequent 0.15.x versions. The overhaul will include features like "Group sorted based on importance", "Better Display Name", "Improved Property Folding", "Hiding Unused Groups and Properties by Default", "UI display mode(Simple/Advanced)" among others. The primary objective is to streamline the material UI, reducing its vertical UI length and learning curve by default. This way, users can work more efficiently without the need for excessive mouse wheel scrolling or group searching by text.
- (Core) Upgrade to LWGUI 1.13.0
- (Core) NiloToonShaderStrippingSettingSO.cs: rewrite the UI display
- (Core) NiloToonCharacter.shader: rename matcap (alpha blend) to (replace)
- (Core) NiloToonCharacter.shader: rename matcap (additive) to matcap(specular highlight/rim light)
- (Core) NiloToonCharacter.shader: _UnityCameraDepthTextureWriteOutlineExtrudedPosition is now default off, since it may produce 2D rim light artifact too often on some model
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: limit MToon to NiloToon's converted outline width to a maximum of 0.6
- (InternalCore) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: ban "surface" for face's mat name keyword
- (InternalCore) NiloHSVRGBConvert.hlsl: rgb2hsv code now use SRP Core library's function, instead of custom code
- (InternalCore) any camera name containing 'reflect' will be treated as planar reflection camera now, before it is 'reflection' instead of 'reflect'
- (InternalCore) NiloToonPrepassBufferRTPass.cs: rename NiloToonPrepassBufferColor->_NiloToonPrepassBufferColor, and, NiloToonPrepassBufferDepth->_NiloToonPrepassBufferDepth

### Fixed
- (Core) Correctly fix all GC/RTHandle problem in Unity2022.3 ~ Unity2023.3, pass and RTHandle dispose logic now follows URP 2022.3.11f1's RTHandle style
- (Core) NiloToon Character shader: fix bug -> OpenGLES's depth tex 2D rim light produced in opposite direction
- (Core) NiloToon Character shader: fix bug -> camera normal texture not having correct normal for Unity2021.3
- (Core) NiloToon Character shader: fix bug -> enableDepthTextureRimLightAndShadow always false when material is not controlled by pechar script
- (Core) NiloToon Character shader: EnableDepthTextureRimLigthAndShadow now set by renderer feature instead of per char script, to ensure value is always updated correctly every frame in edit mode
- (Core) NiloToonPerCharacterRenderController.cs: fix bug -> renderer's material lost reference when editing prefab in prefab mode while running in playmode
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: 'auto setup this char' button now supports all 14 SurfaceType of NiloToon Character material
- (Core) NiloToonAverageShadowTestRTPass.cs: fix bug -> some samsung mobile device not supporting R16_UNorm
- (InternalCore) NiloScaledScreenParamUtil.hlsl: fix bug -> GetScaledScreenTexelSize() return incorrect result
----------------------------------------------
## [0.14.5] - 2023-08-26

### Fixed
- (Core) NiloToonCharSelfShadowMapRTPass.cs: fix NiloToonCharacterMainLightOverrider didn't override direction of this self shadow pass
----------------------------------------------
## [0.14.4] - 2023-08-25

### Fixed
- (Core) NiloToonAllInOneRendererFeature.cs: Correctly fix all RT memory leak by calling extra Dispose(), revert temp fix in 0.14.3
----------------------------------------------
## [0.14.3] - 2023-08-25

### Known Issue
- (Core) NiloToonCharSelfShadowMapRTPass will have RT memory leak when user edit NiloToon's renderer feature(e.g. dragging the slider), now for Unity2022.3 or lower we converted back to old RenderTargetHandle system as a temp fix, but the memory leak will still exist for Unity2023.1 or higher

### Changed
- (Core) rewrite NiloToonAllInOneRendererFeature's UI completely
- (Core) NiloToonCharacter.shader: _ReceiveSelfShadowMappingPosOffset's default value revert from 1 back to 0
- (Core) NiloToonAllInOneRendererFeature: URPShadowDepthBias's default value changed 0.1 -> 0, URPShadowNormalBiasMultiplier's default value changed 0 -> 1, so now the 'receive URP shadow map' section will match URP shadow's shadow result completely by default

### Fixed
- (Core) NiloToonCharSelfShadowMapRTPass will have RT memory leak when user edit NiloToon's renderer feature(e.g. dragging the slider), now for Unity2022.3 or lower we converted back to old RenderTargetHandle system as a temp fix, but the memory leak will still exist for Unity2023.1 or higher
----------------------------------------------
## [0.14.2] - 2023-08-24

### Fixed
- (Core) NiloToonEditorShaderStripping.cs: Stop stripping any URP keywords, this will make URP's stripping perform correctly, which produce correct stripping result of NiloToonCharacter shader (e.g. additional light and additional light shadow)
- (InternalCore) NiloToonCharSelfShadowMapRTPass.cs: fix validTRS matrix check bug
- (InternalCore) NiloToonEditor_AssetLabelAssetPostProcessor.cs: remove AssetDatabase.Refresh() when importing asset
----------------------------------------------
## [0.14.1] - 2023-08-23

### Fixed
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor: fix a bug that Unity version lower than Unity2021.3.29 using a non exist API FindObjectsByType. Now all Unity2021.3 will revert to use FindObjectsOfType(not FindObjectsByType)
----------------------------------------------
## [0.14.0] - 2023-08-23

### Version Highlight (English)
This version is the largest update when compared to any previous update.
This version now supports Unity2021.3 to Unity2023.2, and supporting all URP's new features.
Feedback and bug report are appreciated! (email: nilotoon@gmail.com)(discord: kuronekoshaderlab)

[NiloToon now supports new URP features since Unity2021.3LTS]
A1. Point Light Shadows
A2. Deferred Rendering Support (NiloToon shaders will still render as Forward in Deferred Rendering with 8 light per renderer limitation)
A3. Decals
A4. Light Cookies
A5. Reflection Probe Blending
A6. Reflection Probe Box Projection

[NiloToon now supports new URP features since Unity2022.3LTS]
B1. Forward+ Support (Forward+ can render a maximum of 256 lights per camera, instead of Forward's maximum of 8 lights per renderer)
B2. Rendering Layer
B3. Light layer
B4. Decal layer
B5. LOD Crossfade (Only NiloToonEnvironment shader has this feature supported, since it is not very useful to apply LOD Crossfade to characters)
B6. TAA support (alternative of MSAA when MSAA is too slow in high resolution render)
B7. All types of light cookies

[NiloToon added feature]
C1. a new volume - NiloCinematicRimLightVolume, which will turn all additional lights into rim light for NiloToonCharacters materials
C2. a new MonoBehaviour - NiloToonCharacterMainLightOverrider, you can override character's mainlight color and direction, without editing any URP's directional light
C3. NiloToonCharacter material can now be used independently without a NiloToonPerCharacterRenderController script attached to the prefab(but when used without having a NiloToonPerCharacterRenderController to control that material, that NiloToonCharacter material's NiloToon self shadow map and average URP shadow will not be rendered)
C4. NiloToonCharacter material's Classic outline can now be rendered for transparent queue(2501-5000) materials. You can use "Transparent(ZWrite)/Outline" and "Opaque/Outline" surface type preset materials together now, outlines from both type of materials will be displayed correctly and not be blocked by each other anymore
C5. Rewrite NiloToon self shadow map algorithm, it will not show shadow artifact easily now when multiple characters are visible and far to each other.
C6. Auto update NiloToonPerCharacterRenderController's allRenderers，any child transform change will trigger an update in edit mode. The update will consider child NiloToonPerCharacterRenderController also
C7. NiloToonBloom added HSV control, high saturation bloom can produce another art style 
C8. Rewrite NiloToonCharacter's surfaceType preset, allow you to setup a material more easily with a better dropmenu
C9. NiloToonCharacter material UI update (upgrade to LWGUI 1.11.1), including a new toolbar and group search mode

[NiloToon important bug fix]
D1. fix all possible crash that we know when building the player(compiling shader variants)
D2. now NiloToon will have 0 GC alloc per frame(unless using "NiloToonRendererRedrawer" or "Terrain Crash Safe Guard" enabled)

### 版本重點 (中文)
- 這版本(0.14.0)會是所有版本之中改動最大的版本
- 支援Unity2021.3~2023.2
- 歡迎意見和bug report (email: nilotoon@gmail.com)(discord: kuronekoshaderlab)(QQ/Wechat)

[支援所有URP(Unity2021.3)版本的新功能]
A1. Point Light Shadows
A2. Deferred Rendering Support (NiloToon shaders 在 Deferred Rendering 仍然會以 Forward 渲染(有8 light per renderer 的限制))
A3. Decals
A4. Light Cookies
A5. Reflection Probe Blending
A6. Reflection Probe Box Projection

[支援所有URP(Unity2022.3)版本的新功能]
B1. 支援Forward+，可以令角色收到256個additional light
B2. 支援URP rendering layer
B3. 支援light的rendering layer
B4. 支援decal的rendering layer
B5. LOD crossfade(只有NiloToonEnvironment shader有包括支持，因為角色的LOD crossfade似乎不太常用)
B6. 支援TAA (當你不想使用MSAA(例如在4K渲染時太慢)，可以試試TAA)
B7. 支援所有light cookie

[NiloToon重要新功能]
C1. 增加了一個「令additional light變rim light」的volume - NiloCinematicRimLightVolume，理論上角色可以隨便接受大量spotlight/point light但仍會好看不會過亮
C2. 增加 「NiloToonCharacterMainLightOverrider」 MonoBehaviour，可以使用新的gameobject來改變角色的main light燈光方向和顏色，而不需要改動URP的任何light
C3. NiloToonCharacter material可以獨立使用，不需要依賴nilotoon per character script (但會NiloToon的average shadow如selfshadowmap不會渲染)
C4. 內層Opauqe+Outline物體 和 外層半透明+Outline物體，都能同時出現描邊，不會互相檔住
C5. 重寫了NiloToon self shadow mapping，在多角色而且各自距離比較遠時都不會再出現難看的影子問題
C6. 自動設置NiloToonPerCharacterRenderController的allRenderers，在child transform有任何變動時會在edit mode自動更新，會考慮child NiloToonPerCharacterRenderController
C7. NiloToonBloom 增加 HSV 控制，例如高saturation的bloom可以做出新的bloom風格
C8. 重寫 NiloToonCharacter的surfaceType preset，新的dropmenu使用上會更直觀
C9. NiloToonCharacter material UI 更新 (使用 LWGUI 1.11.1), 增加新的 toolbar 和 search mode

[NiloToon重要修正]
D1. 修正所有build時會令unity crash的問題
D2. 修正所有C# GC，現在是0 GC Alloc per frame(除非使用 "NiloToonRendererRedrawer" 或 "Terrain Crash Safe Guard")

### Known Issue
- (Core) NiloToonRendererRedrawer's rendering will not handle lighting correctly in build since it's first release(released in 0.13.1), we are fixing it and we do not recommend user using it right now other than "editor only rendering" purposes.
- (Core) When Deferred rendering + TAA + NiloToon’s character self shadowmap are all enabled, rendering will be wrong(random white flicker). Consider using Forward+ if possible

### Breaking change
- (Core) NiloToonPerCharacterRenderController: add autoRefillRenderersMode and autoRefillRenderersLogMode, now NiloToonPerCharacterRenderController will auto refill allRenderers list if any child transform structure changed in editor. And when performing auto refill allRenderers, it will assign renderers correctly to all child NiloToonPerCharacterRenderController(s)'s allRenderers list
- (Core) NiloToonAnimePostProcessVolume: this volume will now be affected by Camera's PostProcessing toggle, since it should be treated as postprocess similar to Bloom/Vignette
- (Core) NiloToonBloomVolume: this volume will now be affected by Camera's PostProcessing toggle, since it should be treated as postprocess similar to URP's Bloom
- (Core) NiloToonStickerAdditive shader: this shader will now receive _GlobalVolumeMulColor from NiloToonCharRenderControlVolume
- (Core) NiloToonSetToonParamPass: EnableSkyboxDrawBeforeOpaque is now default true, it can solve Opaque queue(<2500) semi-transparent draw problems. Disable it if you don't need it can improve performance
- (Core) NiloToon Self shadow mapping: Rewrite depth bias and normal bias, it will match URP16's depth bias and normal bias design. 
- (Core) NiloToon Self shadow mapping: Rewrite shadow caster CPU culling, now override main light dir from volume or script will also override NiloToon Self shadow's shadow casting direction
- (Core) NiloToon self shadow mapping: Rewrite shadow caster CPU culling, now shadowRange start from the closest visible character, not from camera, it can help low FOV camera with large distance between camera and character to stil render shadow map correctly
- (Core) NiloToon self shadow mapping: default shadowRange -> 10m
- (Core) NiloToon self shadow mapping: shadowRange clamp to 10m ~ 100m
- (Core) NiloToon self shadow mapping: fadeTotalDistance 2m->1m, same as URP's default default
- (Core) NiloToonCharacter.shader: +_EnableNiloToonSelfShadowMappingDepthBias,+_EnableNiloToonSelfShadowMappingNormalBias (default is false, which will disable all material depth and normal bias that was used in the past)
- (Core) NiloToonCharacter.shader: clamp emission light color mul, light received will not exceed intensity 1
- (Core) NiloToonCharacter.shader: remove min rimlight and specular, now rendering result between main light intensity 0 and 0.0001 is the same
- (Core) NiloToonCharacter.shader: emission, when tint by light, will receive URP shadowmap if allowed
- (Core) NiloToonCharacter.shader: make isFace's mask sampling using fragment shader quality instead of vertex
- (InternalCore)NiloToonPerCharacterRenderController.cs: In C#, we're changing shadowTestIndex and ExternalRenderOverrider from public to internal. This change ensures users won't modify these by accident.

### Added
- (Core) Support all versions of Unity starting from Unity2021.3 to Unity 2023.2
- (Core) all shaders: support Forward+ (require Unity2022.2 or higher)
- (Core) all shaders: support RenderingLayer (e.g. Light and Decal 's rendering layer)
- (Core) all shaders: support all light's lightcookie (including directional light's light cookie)
- (Core) all shaders: support Unity2022.2's new depthnormal pass design
- (Core) +NiloToonCharacterMainLightOverrider.cs
- (Core) NiloToonCharacter shader: NiloToonCharacter material can now be used independently without a NiloToonPerCharacterRenderController script attached to the prefab(but when used without having a NiloToonPerCharacterRenderController to control that material, that NiloToon_Character material's self shadow map and average URP shadow will not be rendered)
- (Core) NiloToonCharacter shader: NiloToon_Character material's Classic outline can now be rendered for transparent queue(2501-5000) materials. You can use "Transparent(ZWrite)/Outline" and "Opaque/Outline" surface type preset materials together now, outlines from both type of materials will be displayed correctly and not be blocked by each other anymore.
- (Core) NiloToonCharacter shader: surface type preset + 4 new cases: Transparent(ZWrite)/Outline, Transparent(ZWrite)/No Outline, Transparent(ZWrite)/Outline (Cutout), Transparent(ZWrite)/No Outline(Cutout)
- (Core) NiloToonCharacter shader: +_FaceShadowGradientMapUVIndex,_FaceShadowGradientMaskMapUVIndex
- (Core) NiloToonCharacter shader: improve the auto check of ShouldOutlineUseBakedSmoothNormal() method, added unit vector length check, this will ensure outline is still correct when no smoothed normal baked data inside mesh's uv8
- (Core) NiloToonCharacter shader: +_OutlineBaseZOffset,_UseOutlineZOffsetMaskFromVertexColor,_OutlineZOffsetMaskFromVertexColor
- (Core) NiloToonPerCharacterRenderController: + "Auto refill AllRenderers list" button
- (Core) NiloToonPerCharacterRenderController: + RefillAllRenderers() API
- (Core) NiloToonPerCharacterRenderController: + ZOffset (similar to material's ZOffset, but this is per character)
- (Core) NiloToonPerCharacterRenderController: "Select all Nilo materials of this char" button will include materirals from AttachmentRendererList & NiloToonRendererRedrawerList also
- (Core) NiloToonAllInOneRendererFeature: + toggle "allowRenderNiloToonBloom"(default on), turn it off when you want to disable NiloToonBloom in low quality settings for performance
- (Core) NiloToonAllInOneRendererFeature: add warning console log when depth priming is enabled, since face materials need depth priming = off
- (Core) NiloToon/Bloom: +downscale,maxIterations (matching URP14's bloom design, require Unity2022.3)
- (Core) NiloToonEditorSelectAllMaterialsWithNiloToonShader.cs: + new API -> SelectAllMaterialsWithNiloToonShader() , GetAllNiloToonCharacterShaderMaterials()
- (Core) NiloToonBloomVolume:+ characterBaseColorBrightness,characterBrightness
- (Core) NiloToonDebugWindow: + Show SelfShadow frustum? toggle in debug window
- (Core) NiloToonShadowControlVolume.cs: + charSelfShadowStrength 
- (Core) NiloToonCharRenderingControlVolume.cs: + addLightColor and desaturateLightColor
- (Core) NiloToonBloom: +HSV control
- (Core) NiloToonAllInOneRendererFeature: +auto check terrain crash fix (terrainCrashSafeGuard)
- (InternalCore) + Runtime\Utility\NiloToonPlanarReflectionHelper.cs
- (InternalCore) + NiloToonEditorCreateObjectMenu.cs
- (Doc) Improve the document pdf, adding more section about NiloToon 0.14.x's new feature

### Changed
- (Core) NiloToonCharacter.shader: changed the default URP shadow depth bias to 1 for non-face also. Having a default 1m depth bias is because usually you do not want character to receive low resolution ugly URP shadow that was casted by self (e.g. hand,hair,hat), but still wants to receive URP shadow that was casted by far environment objects(e.g. tree, building).
- (Core) NiloToonAllInOneRendererFeature: added allowClassicOutlineInPlanarReflection (default false). When this is off, NiloToon will disable classic outline if any camera has "mirror"/"planar"/"reflection" in camera gameobject's name (ignore cases)
- (Core) NiloToonSetToonParamPass.cs: optimize C# GC to 0 by caching C# Reflection calls like GetProperty()
- (Core) NiloToonPerCharacterRenderController: Extra thick outline now works in edit mode
- (Core) stop rendering NiloToonExtraThickToonOutlinePass,NiloToonPrepassBufferRTPass,NiloToonRendererRedrawerPass,NiloToonAverageShadowTestRTPass in preview window
- (Core) NiloZOffsetUtil.hlsl: zoffset improve cam near plane clamp method, will avoid zfight when vertex is pushed beyound camera near plane
- (Core) NiloToonCharacter.shader: _OutlineWidth default 0.7->0.6, in many cases a smaller outline looks better
- (Core) NiloToonCharacter.shader: rename all surface preset to shorter names and put them into a folder structure drop menu
- (Core) Upgrade LWGUI to 1.11.1
- (Core) improve all NiloToon passes's frame debugger naming and foldout structure
- (Core) NiloToon_Character.shader: make _MainLightIgnoreCelShadeForFaceArea default 0, this will make fade and body skin brightness closer in shadow area
- (Core) NiloToon_Character.shader: rename Smoothness-> Smoothness/Roughness, to make it more easier for group search
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: rewrite auto set up character, will improve the chance of setting the correct bone and set up the material correctly by identity material and bone names 
- (Core) NiloToon_Character.shader: _MatCapAdditiveMixWithBaseMapColor,_HairStrandSpecularMixWithBaseMapColor default is now 0.5 (50%)
- (Internal)NiloToonEditorShaderStripping.cs: matching URP12.1.10's shader stripping more
- (Internal)NiloToonCharacter_Shared.hlsl: make shader structure matching URP14 LitForwardPass more(Remove viewDirectionWS in Varyings, recalculate in fragment shader)(fog always calculate in fragment shader)
- (InternalCore)For URP14 or later, nilotoon's all render passes and renderer feature are all converted to "RTHandle+RenderingUtils.ReAllocateIfNeeded+Dispose" style, the "RenderTargetHandle" or "RenderTargetIdentifier" style code will remain for Unity2021.3 only. We will need to make a big change again when Unity2023.4LTS's "render graph system + URP" is released
- (InternalCore) change NiloToonSetToonParamPass's render pass event to BeforeRenderingPrePasses
- (InternalCore) NiloToonEnvironment shader: make envi shader matching URP16.0.3 complexlit 100%(keep all NiloToon changes and 2021.3+2022.3 support), now only 1 SubShader will remain instead of 2

### Fixed
- (Core) fix -> extra thick outline will not correctly render when "Keep PlayMode mat edit" is enabled
- (Core) NiloToonCharacter_Shared.hlsl: fix a bug about NiloToonCharacterAreaStencilBufferFillPass. The bug will draw StencillBuffer incorrectly on Classic outline area even Classic outline has been disabled
- (Core) NiloToonSetToonParamPass.cs: fix a possible null exception when getting NiloToonRenderingPerformanceControlVolume instance in constructor
- (Core) Runtime\RendererFeatures\NiloToonAllInOneRendererFeature.cs: turn char list to static list, this will solve all build crash
- (Core) Runtime\NiloToonPerCharacterRenderController.cs: enable faceNormalFix only if head bone exists
- (Core) fix -> In Unity2022.3 LTS, NiloToon/Bloom is not working
- (Core) fix -> In Unity2022.3 LTS, when NiloToon/AnimePostProcess is enabled and it's DrawBeforePostProcess = off, that anime postprocess effect will not work when camera is using fxaa
- (InternalCore) Singleton of NiloToonAllInOneRendererFeature.Instance will now set to the last Create() instance renderer's renderer feature
- (InternalCore) Runtime\RendererFeatures\Passes\NiloToonAnimePostProcessPass.cs: make anime postprocess from RenderPassEvent.AfterRendering + 2 to AfterRenderingPostProcessing
- (InternalCore) Editor\NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: replace FindObjectsOfType to FindObjectsByType
- (InternalCore) ShaderLibrary\NiloUtilityHLSL\NiloHSVRGBConvert.hlsl: fix wrong function declare bug
- (InternalCore) remove NiloToon's _ADDITIONAL_LIGHT_SHADOWS stripping for Unity2022.2 or up, due to Forward+ this stripping is not safe to perform
- (InternalCore) ban NiloToon average shadow in WebGL, since the large iteration forloop in shader can't be compiled correctly in WebGL anyway
- (InternalCore) NiloToonEnvironmentShaderGUI.cs remove "MaterialChanged" override, now it use "ValidateMaterial" override
----------------------------------------------
## [0.13.5] - 2023-06-28

### Known Issue
- (Core) NiloToonRendererRedrawer's rendering will not handle lighting correctly on build since it's first release(released in 0.13.1), we are fixing it and we do not recommend user using it right now other than "editor only rendering" purposes.
- (Core) In Unity2022.3 LTS, NiloToon/Bloom is not working
- (Core) In Unity2022.3 LTS, when NiloToon/AnimePostProcess is enabled and it's DrawBeforePostProcess = off, that anime postprocess effect will not work when camera is using fxaa

### Added
- (Core) NiloToonBloomVolume.cs: +_NiloToonBloomCharacterAreaBloomEmitMultiplier and more toolip to other settings
- (Core) NiloToonSetToonParamPass.cs: +auto detect any possible planar reflection camera by name and handle these camera rendering using NiloToonPlanarReflectionHelper (for support PIDI: planar reflection 5 automatically)
- (Core) NiloToon_Character.shader: +_BumpMapApplytoFaces,_OcclusionMapApplytoFaces, _EnvironmentReflectionApplytoFaces

### Changed
- (Core) NiloToonBloomVolume.cs: NiloToonBloom IsActive() logic is more strict for better optimization chance
- (Core) NiloToonBloomVolume.cs: default height of renderTextureOverridedToFixedHeight changed from 540->1080
- (Core) NiloToon_Character.shader: rename Mat Cap (occlusion)'s display name to a more intuitive name "Mat Cap (shadow)"

### Fixed
- (Core) NiloToonPrepassBufferRTPass.cs: fix prepass RT depth and MSAA not match to cameraTargetDescriptor bug, this bug fix will make NiloToon/Bloom render 100% correct now.
- (Core) NiloToonCharacter_Shared.hlsl: fix an important bug of typo "NiloToonPrepassBuffer", the correct one is "NiloToonPrepassBufferPass", this will solve many NiloToon/Bloom artifact
----------------------------------------------
## [0.13.4] - 2023-06-26

### Known Issue
- (Core) NiloToonRendererRedrawer's rendering will not handle lighting correctly on build since it's first release(released in 0.13.1), we are fixing it and we do not recommend user using it right now other than "editor only rendering" purposes.
- (Core) In Unity2022.3 LTS, NiloToon/Bloom is not working
- (Core) In Unity2022.3 LTS, when NiloToon/AnimePostProcess is enabled and it's DrawBeforePostProcess = off, that anime postprocess effect will not work when camera is using fxaa

### Breaking Change
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: Rewrite "Auto setup this character" button, it will now convert MToon10(VRM1 only)/UniUnlit/Lit material properties to NiloToon_Character material. If you need to convert VRM1 generated MToon10 materials to NiloToon shader, the "Auto setup this character" button is the best option now
- (Core) NiloToonCharacter_RenderFace_LWGUI_ShaderPropertyPreset: edit element order, new order doesn't not match to old material properties

### Added
- (Core) NiloToonCharacter_SurfaceType_LWGUI_ShaderPropertyPreset: +CutoutTransparent(ZWrite)+Outline,CutoutTransparent,CutoutTransparent(ZWrite) surface type preset
- (Core) NiloToonEditorDebugWindow.cs: +more debug case (to a total of 32 cases)
- (Core) NiloToonCharacter.shader: +_CullNiloToonSelfShadowCaster
- (Core) NiloToonCharacter.shader: +_MultiplyLightColorToEmissionColor,+_EmissionMaskMap
- (Core) NiloToonCharacter.shader: _OutlineWidthExtraMultiplier slider range increase to 2048 for accepting properties from MToon10 shader

### Changed
- (Core) LWGUI: upgraded 1.5.3 -> 1.7.0

### Fixed
- (Core) NiloToonCharSelfShadowMapRTPass.cs: (VERY Important bug fix)fix nilotoon shadowmap flicker when more than 1 camera is active(including game+scene window's camera)
- (Core) NiloToonCharacter.shader: fix _StencilRef Int Slider
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: fix a null reference exception related to headBoneTransform
- (Core) NiloToonCharacter.shader: fix debug case's alpha output 
----------------------------------------------
## [0.13.3] - 2023-06-08

### Known Issue
- (Core) NiloToonRendererRedrawer's rendering will not handle lighting correctly on build since it's first release(released in 0.13.1), we are fixing it and we do not recommend user using it right now other than "editor only rendering" purposes.
- (Core) In Unity2022.3 LTS, NiloToon/Bloom is not working
- (Core) In Unity2022.3 LTS, when NiloToon/AnimePostProcess is enabled and it's DrawBeforePostProcess = off, that anime postprocess effect will not work when camera is using fxaa
- (Core) NiloToonCharacter.shader: _StencilRef's IntRange is not active correctly, it is now a float slider instead of int slider

### Breaking Change
- (Core) NiloToonCharacter.shader: +_DepthTexRimLightAndShadowReduceWidthWhenCameraIsClose, this will reduce the depth tex rimlight&shadow's width when camera distance is < 1, inorder to keep the width not too big when view closely. If you want the old result, you can edit this back to 0

### Added
- (Core) added Textures\Matcap_Rainbow.png
- (Core) NiloToonCharacter.shader: +_AllowPerCharacterDissolve,_AllowPerCharacterDitherFadeout
- (Core) NiloToonCharacter.shader: +_CullOutline, _RenderFacePreset, Render Face group
- (Core) NiloToonCharacter.shader: +_DepthTexRimLightBlockByShadow, _DepthTexRimLightAndShadowWidthExtraMultiplier
- (Core) NiloToonPerCharacterRenderController.cs: +extraThickOutlineRendersVisibleArea 
- (Core) NiloToonAllInOneRendererFeature.cs: +EnableSkyboxDrawBeforeOpaque, for solving semi-transparent material + Skybox flicker bug
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor: auto disable depth tex shadow if it is an eye white material

### Changed
- (Core) LWGUI: upgrade from 1.4.3 to 1.5.3
- (Core) NiloToonCharacter.shader: better matcap preset grouping
- (Core) NiloToonCharacter.shader: increase _FaceAreaCameraDepthTextureZWriteOffset's default value 0.03->0.04
- (Core) NiloToonCharacter.shader: reduce _DepthTexRimLightAndShadowWidthMultiplier's default value 0.7->0.6
- (Core) NiloToonCharacter.shader: move Render states settings into Collapsible Group (Color Buffer, Depth Buffer, Stencil, Render Face)
- (Core) NiloToonPerCharacterRenderController.cs: disable a few warning spam
- (Core) NiloToonPerCharacterRenderController.cs: fixFaceNormalUseFlattenOrProxySphereMethod default value 0->0.75f
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor: Auto setup char button only change _SurfaceTypePreset if _SurfaceTypePreset is the default preset, to avoid editing "Already edited" materials
----------------------------------------------
## [0.13.2] - 2023-05-07

### Known Issue
- (Core) NiloToonRendererRedrawer's rendering will not handle lighting correctly since it's inititial release(released in 0.13.1), we are fixing it and we do not recommend any user using it right now.

### Breaking Change
- (Core) NiloToonCharacter_LightingEquation.hlsl: update the algorithm of calculating the width of depth texture rim light and shadow, it is different to the old algorithm, but it will produce a more "character relative" consistant width between different camera distance and fov.

### Added
- (Core) NiloToonCharacter.shader: add 3 new SurfaceType preset: Transparent(ZWrite), CutoutOpaque+Outline, CutoutOpaque
- (Core) NiloToonCharacter.shader: add _MatCapOcclusionPreset, _MatCapAdditivePreset, _MatCapAdditiveExtractBrightArea
- (Core) Added a new Textures\Matcap_Glossy01.png texture

### Changed
- (Core) NiloToonRendererRedrawerPass.cs: remove a useless ExecuteCommandBuffer() call
- (Core) NiloToonPrepassBufferRTPass.cs: Now this Prepass will show correctly as a group in Frame Debugger, due to an added ExecuteCommandBuffer() call
- (Core) NiloToonAllInOneRendererFeature.cs: optimize the renderPassEvent of all pass
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: added "oral", "brow", "mayu", "express" to IsFaceTargetNames and IsNoOutlineNames array. So material that contains these keywords in their name, will enable "IsFace?" and disable "Classic Outline" when user click the "Auto setup this character" button
- (Core) NiloToonCharacter.shader: Update properies default value. _OutlineWidth 0.8->0.7, _ApplyDepthTexRimLightFixDottedLineArtifacts 0 -> 1, _DepthTexRimLightMixWithBaseMapColor 0 -> 0.5, _DepthTexRimLightIntensity 1 -> 1.5    
- (Core) NiloToonCharacter_Shared.hlsl: improve matcap uv using bgolus's method
- (Core) In all NiloToon's .hlsl, turn all unity_OrthoParams.w to !IsPerspectiveProjection() or similar code, to improve readability

### Fixed
- (Core) NiloToonEnvironment.shader: update NiloToonEnvironment.shader to match URP12.1.10, to display shadow mask baked lightmap correctly
- (Core) NiloToonCharacter.shader: CharacterAreaStencilBufferFill pass now defines NiloToonCharacterAreaStencilBufferFillPass instead of NiloToonSelfOutlinePass. This will help NiloToonCharacter_Shared.hlsl to solve a bug about "ExtraThick outline pass not correctly render"
- (Core) NiloToonCharacter_Shared.hlsl: fix "ExtraThick outline pass not correctly render" when Classic Outline is off
----------------------------------------------
## [0.13.1] - 2023-04-23

### Breaking Change
- (Core) NiloToonPerCharacterRenderController.cs: remove extraThickOutlineStencilID, now extra thick outline only relies on the 1st bit of the stencil buffer. The 2nd-8th bit can be controlled by NiloToonCharacter.shader material's new _StencilRef,_StencilComp,_StencilPass settings freely.
- (Core) NiloToonPerCharacterRenderController.cs: now "Extra Thick Outline" will draw only the outline area when "Show Blocked" is enabled
- (Core) NiloToonCharacter_ExtendFunctionsForUserCustomLogic.hlsl: fix typo attrubute -> attribute

### Added
- (Core) Added NiloToonRendererRedrawer.cs, this script can be attached to any NiloToonPerCharacterRenderController's child renderer, to redraw that renderer's target SubMesh again. Usually for drawing semi-transparent eye or hair again, using Stencil setting of the material.
- (Core) Added NiloToonRendererRedrawerPass.cs, this is the pass for drawing all NiloToonRendererRedrawer component that is enabled in scene
- (Core) NiloToonAllInOneRendererFeature.cs: enqueue NiloToonRendererRedrawerPass
- (Core) NiloToonPerCharacterRenderController.cs: added nilotoonRendererRedrawerList, to update the materials of every child NiloToonRendererRedrawer
- (Core) NiloToonCharacter.shader: add _StencilRef,_StencilComp,_StencilPass. It is usually for drawing eye on top of hair. User can control 2nd~8th bit of Stencil Read/Write freely per material, while the 1st bit is always reserved for NiloToon's ExtraThickOutline.
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: More auto setting by material name. Now if material name contains "lash", then "Is Face?" will be turned on. If material name contains "body", then "Is Skin?" will be turned on. Now if material name contains "facial"/"eye"/"iris"/"pupil"/"mouth"/"teeth"/"tongue"/"lash", then "Classic Outline" will turned off, and "Surface Type Preset" will be set to "Opaque" 
- (Core) add PureBlack4x4.png
- (Core) NiloToonCharacter.shader: add a "Require Depth Priming Mode equals Disabled" note to "Is Face?" section

### Changed
- (Core) NiloToonEnvironment.shader: now SRP batching is working. Every pass will share the same CBUFFER_START(UnityPerMaterial).
- (Core) NiloToonEnvironment.shader: upgrade to use URP12.1.9 ComplexLit shader as base code, so ClearCoat can be used now, and you can expect the render result will be the same as URP12.1.9's ComplexLit shader but with all NiloToon functions applied to it.
- (Doc) rewrite "How to make eyebrows render on top of hair(in a semi transparent way)?" section, to use new feature(NiloToonRendererRedrawer) of this update 

### Fixed
- (Core) NiloToonCharacter.shader: fix a bug that uv4 is not using the 4th uv(TEXCOORD3), in the past it is using the 5th uv(TEXCOORD4)
- (Core) FoldoutEditor.cs: MeshFilter is not showing correctly due to this class, so we changed FoldoutEditor.cs to not affect classes other than 'NiloToonPerCharacterRendeController' now
- (Core) NiloToonPerCharacterRenderController.cs: fix Extra Thick Outline's ZOffset is not work bug
- (Core) NiloToonCharSelfShadowMapRTPass.cs: optimize NiloToonCharSelfShadowMapRTPass memory allocation, by calling to a non-allocate version of GeometryUtility.CalculateFrustumPlanes()
----------------------------------------------
## [0.12.3] - 2023-03-29

### Known Issue
- (Core) NiloToonEnvironment.shader: will show error log -> "Instancing: Property 'unity_RenderingLayer' shares the same constant buffer offset with 'unity_LODFade'. Ignoring."

### Changed
- (Demo) Upgrade demo project to Unity2021.3.20f1, remove demo project support for Unity2020.3LTS
- (Demo) CRS SampleScene URP (NiloToon control).asset: change charBaseColorMultiply from 1->0.95 and charBaseColorTintColor to a redish value
- (Demo) NiloToonSampleSceneProfile.asset: change charBaseColorMultiply from 1->0.95
- (Demo) UniversalRP-5HighestQuality.asset: enable m_ReflectionProbeBlending, m_ReflectionProbeBoxProjection, m_SupportsLightLayers
- (Demo) UniversalRP-5HighestQuality.asset: change m_MSAA from 8 to 4, to avoid performance problem in editor
- (Demo) UniversalRP-5HighestQuality.asset: enable m_RequireOpaqueTexture
- (Demo) ForwardRenderer 5HighestQuality.asset: remove an unused renderer feature "GTToneMapping"
- (Demo) ForwardRenderer 5HighestQuality.asset: adding a renderer feature "Decal", enabled "dbuffer" technique
- (Demo) pidi reflection upgrade from 4.0.7 to 4.2.0
- (Demo) Magica Cloth: replace NativeMultiHashMap to NativeParallelMultiHashMap
- (Demo) UnityPackageManager(UPM): remove jobs, install collections
- (Demo) ProjectSettings\ProjectSettings.asset: m_BuildTargetDefaultTextureCompressionFormat now use ASTC compression

### Fixed
- (Core) Upgrade LWGUI to 1.4.3
- (Demo) fix advancedFPSCounter font bug for Unity2022.2
- (Demo) NiloToonDemoSampleScene_UI.cs: fix UI typo enviroment -> environment
----------------------------------------------
## [0.12.2] - 2023-03-19

### Fixed
- (Core) temp fix a bug that LWGUI1.4.2 failed to show tooltip and helpbox inside a group. 
----------------------------------------------
## [0.12.1] - 2023-03-19

### Breaking change
- (Core) Since Unity will stop Unity2020.3 LTS support soon, NiloToon 0.12.1 removed the support of Unity2020.3, and merged URP12's support branch to NiloToon's main branch. Now Unity2021.3 is the minimum and recommended Unity version for NiloToon. If after upgrading to 0.12.1, you encountered Unity editor crash when building the game, please close Unity -> delete library folder -> restart unity to rebuild the shader cache.
- (Core) Removed NiloToonShaderPatch(for URP12 or newer).unitypackage. If you still see this package in your project, please delete it.
- (Core) NiloToonPerCharacterRenderController: run desaturate before tint color and add color, since this will allow more color combination results.

### Added
- (Core) NiloToonUPMImporterCollections.cs: Added this [InitializeOnLoad] editor script to auto install required UPM package when user imported NiloToonURP's .unitypackage to a project (e.g. install Collections package), user don't need to open UPM and install anything manually now.
- (Core) NiloToonPerCharacterRenderController: added Dissolve Mode, noise TilingX&Y and noise Strength. This will allow user to create different style of dissolve.
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: added "facial", "iris", "pupil", "tongue" to IsFaceTargetNames, material with these name will be auto processed when user clicked "Auto setup this character" button
- (Core) NiloToonCharacter.shader: added _FaceShadowGradientMapFaceMidPoint, this will allow FaceShadowGradientMap to support any non-horizontally mirrored texture. When using any non-horizontally mirrored texture user need to set _FaceShadowGradientMapFaceMidPoint to the mid point of their _FaceShadowGradientMap texture
- (Core) NiloToonCharacter.shader: "Environment Reflection" added Reflection probe blending & reflection probe box projection support
- (Core) Added Textures\Matcap_Metal4Gold.png

### Changed
- (Core) Assembly definitions: Removed the requirement of Jobs package, now NiloToon only rely on Collections package
- (Core) NiloToonCharacter.shader: added _fragment & _vertex suffix to shader_feature & multi_compile, this will reduce build time, build size, and build's runtime shader memory usage, but user may need to delete their Library folder to rebuild the shader-cache, else crash may happen
- (Core) NiloToonCharacter.shader: edit the display name of _EnvironmentReflectionUsage from Intensity to Strength
- (Core) NiloToonCharacter.shader: DepthNormalsOnly pass added normal related shader_feature
- (Core) NiloToonCharacter.shader: re-enable _NILOTOON_DITHER_FADEOUT for DepthOnly and DepthNormalsOnly pass
- (Core) NiloToonCharacter.shader: enable _NILOTOON_DITHER_FADEOUT for prepass, prepass remove shader_feature _NORMALMAP
- (Core) NiloToonCharacter.shader: changed _OutlineTintColorSkinAreaOverride default color from (0.623,0.238,0.289,0) -> (0.4,0.2,0.2,1), which is enabled by default now
- (Core) NiloToonCharacter.shader: changed _IgnoreDefaultMainLightFaceShadow default value from 0 -> 1
- (Core) NiloToonCharacter.shader: removed _REFLECTION_PROBE_BLENDING/_REFLECTION_PROBE_BOX_PROJECTION for Outline pass, to reduce shader variant count
- (Core) NiloToonCharacter.shader: outline pass + _BASEMAP_STACKING_LAYER1-10, _MATCAP_BLEND shader_feature, to make outline pass's color matching lit pass
- (Core) NiloToonCharacter.shader: changed _FaceAreaCameraDepthTextureZWriteOffset default value from 0.04 -> 0.03, this will produce better Classic Outline for face
- (Core) NiloToonCharacter.shader: Adding separation line to shader GUI
- (Core) NiloToonCharacter.shader: improved shader GUI
- (Core) NiloToonCharacter.shader: _OutlineWidth's default value change from 1 -> 0.8, since most of the time a thinner outline is preferred
- (Core) NiloToonAllInOneRendererFeature.cs: edit prepass timing from AfterRenderingPrePasses to BeforeRenderingOpaques. since character shader's NiloToonPrepassBuffer pass needs _CameraDepthTexture. (AfterRenderingPrePasses is still too early because _CameraDepthTexture is not ready at this timing)
- (Core) NiloToonCharSelfShadowMapRTPass.cs & NiloToonShadowControlVolume.cs: useMainLightAsCastShadowDirection's default value changed from false->true, since many user expect it to be true by default.
- (InternalCore) remove all UNITY_2021_3_OR_NEWER macro in C#, since the minimum Unity version is 2021.3 already
- (InternalCore) remove all !UNITY_2021_3_OR_NEWER or below's macro and content in C#, since the minimum Unity version is 2021.3 already
- (InternalCore) NiloToonCharacter.shader: removed all URP10(Unity2020.3) support code
- (InternalCore) NiloToonCharacter.shader: In shader, convert all SHADER_LIBRARY_VERSION_MAJOR to UNITY_VERSION, since SHADER_LIBRARY_VERSION_MAJOR is deprecated for Unity2022.2 or later
- (InternalCore) NiloToonCharacter_Shared.hlsl: replace raw type to FRONT_FACE_TYPE / FRONT_FACE_SEMANTIC / IS_FRONT_VFACE(), this will let Unity handle the per platform conversion
- (InternalCore) NiloToonCharacter.shader: change "UniversalMaterialType" = "Lit" -> "ComplexLit"
- (InternalCore) Replace all UnsafeHashMap -> UnsafeParallelHashMap
- (InternalCore) Upgrade LWGUI to 1.4.2
- (Doc) improved NiloToonURP user document.pdf, adding jump links, removed old data and replace all pictures to an updated version.

### Fixed
- (Core) NiloToonCharacter.shader: NiloToonPrepassBuffer now follow depth pass outline extrude logic, so NiloToon/Bloom's character prepass mask will have better accuracy
- (Core) NiloToonCharacter.shader: NiloToonIsAnyOutlinePass don't flip normal on back face anymore, this fix will make outline color matching the original surface color
- (Core) NiloToonCharacter.shader: _IgnoreDefaultMainLightFaceShadow is now only effective when _FACE_SHADOW_GRADIENTMAP is enabled
- (Core) NiloToonCharacter.shader: only enableDepthTextureRimLightAndShadow when _DitherFadeoutAmount == 0, since rendering enableDepthTextureRimLightAndShadow when character has dither holes will not produce good result
- (Core) NiloToonCharacter.shader: fix typo of _SkinMaskMapRemapStart, _SkinMaskMapRemapEnd, this function is not working for a long time, now it is working correctly.
- (Core) NiloToonToonOutlinePass.cs: fix outline pass flickering when mouse move across game view and material inspector
- (Core) NiloToonEditor_AssetLabelAssetPostProcessor.cs: fix multiple renderer with same name in same hierarchy = can't generate uv8 bug
- (InternalCore) NiloToonCharacter.shader: rename reflectionVectorVS to reflectionVectorWS, it is a typo.
----------------------------------------------
## [0.11.9] - 2023-02-01

### Changed
- (Core) NiloToonCharacter.shader: _OverrideShadowColorTexTintColor + [HDR]
- (Core) VideoPlayerForceSyncWithRecorder.cs: update this utility script to the latest version
- (Doc) Improve document pdf
- (Demo) Update demo project to the latest version, adding more models, edit model materials using NiloToon 0.11.9. This maybe the last demo project version to support Unity2020.3. In the future only Unity2021.3 or above's demo project will be provided.
----------------------------------------------
## [0.11.8] - 2023-01-19

### Fixed
- (Core) NiloToonUberPost.shader: compile support for Unity 2022.2(URP14), this version can now build correctly without compile error.
- (Doc) Improve document pdf
----------------------------------------------
## [0.11.7] - 2023-01-19

### Fixed
- (Core) NiloToonCharacter_Shared.hlsl: compile support for Unity 2022.2(URP14)
- (Core) NiloToonAverageShadowTestRT.shader: compile support for Unity 2022.2(URP14)
- (Core) NiloToonBloom.shader: compile support for Unity 2022.2(URP14). Although the shader compiles, NiloToon's Bloom will not show in the game window in Unity2022.2.
----------------------------------------------
## [0.11.6] - 2023-01-19

### Added
- (Core) NiloToonCharacter.shader: added sample UV channel and apply UV channel option to "Parallax Map"

### Changed
- (InternalCore) Upgrade LWGUI to 1.4.0

### Fixed
- (Core) NiloToonCharacter.shader: fix a bug that "Parallax code" can not compile on iOS platform / iOS Editor
----------------------------------------------
## [0.11.5] - 2022-12-28

### Added
- (Core) NiloToonCharacter.shader: added additional light (point/spot)'s light layer support for URP12 or above

### Changed
- (Core) NiloToonCharSelfShadowMapRTPass.cs: NiloToon's shadowmap default size changed from 2048->4096, max limit changed from 8192->16384
- (Core) NiloToonShadowControlVolume.cs: NiloToon's shadowmap default size changed from 2048->4096, max limit changed from 8192->16384
----------------------------------------------
## [0.11.4] - 2022-12-21

### Added
- (Core) NiloToonCharacter.shader: added face shadow gradient map preset, _FaceShadowGradientMapUVCenterPivotScalePos
- (Core) NiloToonCharacter.shader: matcap additive added render face option
- (Core) NiloToonCharacter.shader: specular highlight added render face option
- (Core) NiloToonCharRenderingControlVolume.cs: + _GlobalAdditionalLightRimMaskSoftness

### Fixed
- (Core) NiloToonCharacter.shader: temp disable soft shadow due to filter bug in 2021.3 or later
----------------------------------------------
## [0.11.3] - 2022-12-20

### Breaking change
- (Core) NiloToonCharacter.shader: Now "Override Shadow Color" feature will accept BaseMap tint color from material, NiloToonPerCharacterRenderController and NiloToonCharRenderingControlVolume. By design the "Override Shadow Color" should not be affected by any tint color, but most of the NiloToon user will assume "Override Shadow Color" should be affected by all tint color, so we make this breaking change now to meet user's assumption. If anyone wants the old behavior back, tell us and we will add a toggle for that.

### Changed
- (Core) NiloToonCharacter.shader: rewrite all material UI (except BaseMap stacking layer 4-10 are not yet finished), now all UI have shorter name, and some of them have tooltip also.

### Added
- (Core) NiloToonCharacter.shader: some textures can select UV0~UV3 now.
- (Core) NiloToonCharacter.shader: add _BaseMapStackingLayer1TexIgnoreAlpha,_BaseMapStackingLayer1ApplytoFaces. (also for layer2,3)
- (Core) NiloToonPerCharacterRenderController.cs: add extraThickOutlineWriteIntoDepthTexture
----------------------------------------------
## [0.11.2] - 2022-12-06

### Added
- (Core) added FoldoutAttribute.cs and FoldoutEditor.cs
- (Core) NiloToonPerCharacterRenderController.cs: now use FoldoutEditor, and added Foldout group to inspector UI. This will change the inspector UI greatly.
- (Doc) adding table of content in document
- (Doc) adding preview and download link in document 

### Changed
- (Core) NiloToonPerCharacterRenderController.cs: now 'Classic Outline' will always have effect in play mode or edit mode. In the past it requires in play mode.
----------------------------------------------
## [0.11.1] - 2022-12-05

### Breaking change
- (Core) NiloToonPerCharacterRenderController.cs & NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: completely rewrite the inspector UI, now use short names and adding helpbox and tooltip to explain what each setting does. Our goal is to make it easy to understand and use without the need to read a lengthy document.
- (Core) LWGUI: update to 1.3.0
- (Core) NiloToonCharacter.shader: rewriting the inspector UI (still WIP), now use short names and adding helpbox and tooltip to explain what each setting does. Part of the rewrite UI are now released in this version to gather feedback and bug report. Our goal is to make it easy to understand and use without the need to read a lengthy document.
- (Core) Completely removed Unity 2019.4 support, now the oldest Unity version that can run NiloToon is Unity 2020.3LTS
- (Core) NiloToonCharacter_Shared.hlsl: prepass pass now use a dynamic bias in fragment shader based on camera far plane value. (when camera far plane is higher than 1000, we want to increase the bias, else we will return wrong pixels as black, same as any shadow map artifact)
- (Core) NiloToonCharSelfShadowMapRTPass.cs: disable scene view nilotoon shadowmap pass's render if UseMainLightAsCastShadowDirection is false.
- (Core) NiloToonCharSelfShadowMapRTPass.cs: When rendererfeature's enableCharSelfShadow is false, no matter what override value on volume is, nilotoon shadowmap pass will not render. So user can control performance using renderer feature more easier.
- (Core) NiloToonAverageShadowTestRTPass.cs: When rendererfeature's enableAverageShadow is false, no matter what override value on volume is, nilotoon average shadow will not render. So user can control performance using renderer feature more easier.
- (Core) NiloToonCharacter.shader: change HSV's Saturation apply method, now extremely low saturation pixels will still be low saturation after HSV edit, it will avoid random hue pixels appears due to texture compression (e.g. ETC/ASTC)
- (Core) NiloToonCharacter.shader: _LowSaturationFallbackColor now default off(alpha = 0), because it is not needed after we change HSV 's S apply method

### Added
- (Core) NiloToonCharacter.shader: +SurfaceType preset, now you can switch between Opaque+Outline/Opaque/Transparent+Outline/Transparent presets easily
- (Core) NiloToonCharacter.shader: +Dynamic Ramp Map, you can edit ramp map (lighting)'s ramp map inside material inspector in real-time
- (Core) NiloToonCharacter.shader: +_SkinMaskMapInvertColor
- (Core) NiloToonCharacter.shader: +_OutlineZOffsetMaskTexChannelMask, +_OutlineZOffsetMaskTexInvertColor
- (Core) NiloToonCharacter.shader: + auto ShouldOutlineUseBakedSmoothNormal() check in vertex shader, now .vrm character will auto NOT use baked smoothed outline automatically
- (Core) NiloToonCharacter.shader: +_OcclusionMapInvertColor, _OcclusionMapStylePreset
- (Core) NiloToonCharacter.shader: +_DepthTexShadowColorStyleForNonFacePreset, _DepthTexShadowColorStyleForFacePreset
- (Core) NiloToonCharacter.shader: +_ZTest
- (Core) NiloToonCharacter.shader: _ColorMask added A and None options
- (Core) added VideoPlayerForceSyncWithRecorder.cs and DoFAutoFocusTransform.cs
- (Core) NiloToonAllInOneRendererFeature.cs: added [DisallowMultipleRendererFeature]
- (core) NiloToonScreenSpaceOutlinePass.cs: +AllowRenderScreenSpaceOutlineInSceneView toggle
- (Core) NiloToonPerCharacterRenderController.cs: +useCustomCharacterBoundCenter toggle
- (Core) NiloToonPerCharacterRenderController.cs: +extraThickOutlineZWriteMode, to solve extra thick outline ZWrite problem
- (Core) NiloToonToonOutlinePass.cs: +extraThickOutlineRenderTiming

### Change
- (Core) improve NiloToon's VolumeComponentMenu, now you can search all NiloToon's volume by typing "Nilo"
- (Core) NiloToonCharacter.shader: _DepthTexRimLightAndShadowWidthMultiplier default value 1->0.7, the default width is too large in the past
- (Core) NiloToonCharacter.shader: URP shadow depth bias(face) default 0.2->1
- (Core) Don't auto add NiloToon tag for .vrm and .asset mesh files anymore, which reduced reimport time for .vrm characters by a lot.

### Fixed
- (Core) convert all VFACE to SV_IsFrontFace in shader, to make DX12 shader compile works correctly for XBox One GameCore
- (Core) NiloToonCharSelfShadowMapRTPass.cs: fixed a NiloToon shadowmap flicker bug when mouse is moving into unity editor's material inspector
- (Core) NiloToonScreenSpaceOutlinePass.cs.cs: fixed a NiloToon Screen space outline flicker bug when mouse is moving into unity editor's material inspector
- (Core) NiloToon.NiloToonURP.Runtime.asmdef now includes any platform
- (Core) NiloToonAnimePostProcessPass.cs: anime postprocess will skip drawing if shader is yet not ready, it will remove an error log after 'reimport all'.
- (Core) NiloToonEditorShaderStripping.cs: In Unity2021.3's build, NiloToon will incorrectly strip some keyword(e.g. _MATCAP_ADD _MATCAP_BLEND _RAMP_LIGHTING_SAMPLE_UVY_TEX _SKIN_MASK_ON _SPECULARHIGHLIGHTS), we now disable some stripping logic for "Unity 2021.3 or later" temporary until bug is fixed.
- (Core) NiloToonPerCharacterRenderController.cs: fix a bug that bounding sphere center pos per frame's calculation is wrong
----------------------------------------------
## [0.10.18] - 2022-10-24

### Fixed
- (Core) NiloToonCharSelfShadowMapRTPass.cs: skip execute if it is processing a preview camera. Skip preview camera can solve a bug that cause shadowmap flicker when mouse is moving into unity editor's material inspector region
----------------------------------------------
## [0.10.17] - 2022-10-24

### Breaking change
- (Core) NiloToonBloomVolume.cs: rename VolumeComponentMenu from "NiloToon/Bloom" to "NiloToon/Bloom(NiloToon)", to avoid having the same name as URP's Bloom inside volume's menu
- (Core) NiloToonCharSelfShadowMapRTPass.cs: add normalBias for nilotoon's self shadowmapping, with default value = 1, it is enabled by default, which will change the result of nilotoon's self shadowmapping. Keep depthBias and normalBias to 1 if you are not sure about the correct value
- (Core) NiloToonCharacter.shader: rewrite depthbias implementation method, added normalbias(enabled by default). 

### Added
- (Core) NiloToonCharSelfShadowMapRTPass.cs: add normalBias for nilotoon's self shadowmapping, with default value = 1
- (Core) NiloToonShadowControlVolume.cs: add normalBias, you can use it if you want to override it in volume
- (Core) NiloToonCharacter.shader: Expose NormalBias offset in nilotoon char material

### changed
- (Core) NiloToonCharacter.shader: improve "Can Receive NiloToon ShadowMap?" material GUI's naming,tooltip,helpbox

### Fixed
- (Core) NiloToonCharacter.shader: fix Unity2021.3(URP12) additional light shadowmap's sampling bug, now nilotoon's character shader should receive point light/spot light shadowmap similar to the result of URP's Lit shader shadowmap sampling
----------------------------------------------
## [0.10.16] - 2022-09-30

### Added
- (Core) NiloToonPerCharacterRenderController.cs: added ditherNormalScaleFix, used to hide SSAO problem when dither opacity is close to 0. Default is 1(enabled) 

### changed
- (Core) LWGUI: upgrade to 1.2.5 (added search bar, tooltip, property default value)
- (Core) NiloToonEnvironment.shader: support more URP12 keywords (e.g. Decal)

### Fixed
- (Core) NiloToonEnvironmentShaderGUI.cs: fix Unity2021.3 or above nilotoon environment shader GUI not always update bug
----------------------------------------------
## [0.10.15] - 2022-09-17

### Added
- (Core) NiloToonAnimePostProcessVolume.cs: add rotation,topLightExtraRotation,bottomDarkenExtraRotation
- (Core) NiloToonCharRenderingControlVolume.cs: add additionalLightIntensityMultiplierForFaceArea,additionalLightIntensityMultiplierForOutlineArea,additionalLightApplyRimMask,additionalLightRimMaskPower

### changed
- (Core) LWGUI: upgrade to 1.1.2

### Fixed
- (Core) NiloToonCharacter.shader: optimize additional light code, now additional light code will only enable if keyword _ADDITIONAL_LIGHTS || _ADDITIONAL_LIGHTS_VERTEX is enabled  
----------------------------------------------
## [0.10.14] - 2022-09-07

### Fixed
- (Core) NiloToonCharacter.shader: fix _DBUFFER's tempMetallic typo in code, which makes decal section failed to compile
- (Core) NiloToonCharacter.shader: try to solve Unity2022.2 NiloToon self shadow (soft shadow) compile error
- (Core) NiloToonCharacter.shader: fix _DepthTexRimLightUsage & _DepthTexRimLightIntensity can't completely remove rim light when set to 0
----------------------------------------------
## [0.10.13] - 2022-09-04

### Breaking Changes
- (Core) Replace all old LWGUI related script to the latest LWGUI 1.0.1, you MUST delete NiloToonURP/Editor/ShaderGUI folder before upgrade to NiloToonURP 0.10.13, or just delete NiloToonURP folder completely before import NiloToonURP 0.10.13
- (Core) NiloToonAnimePostProcess.shader: use ColorMask RGB instead of default RGBA, to keep RT's alpha

### Added
- (Core) NiloToonCharacter.shader: Due to upgrade to LWGUI 1.0.1, now material property and group has a "reset/revert to default value" button at the right side of each property or group, it can be used to show all the edited values too

### Changed
- (Core) NiloToonCharacter.shader: Due to upgrade to LWGUI 1.0.1, merge all texture channelMask option into the same line of that texture using [Tex(,)]
- (Core) NiloToonCharacter.shader: Due to upgrade to LWGUI 1.0.1, remove (Default XXX) in group name
- (Core) NiloToonCharacter.shader: Due to upgrade to LWGUI 1.0.1, rename "Calculate Shadow Color of BaseMap" to "Shadow Color"   
- (InternalCore) NiloToonCharacter.shader: Due to upgrade to LWGUI 1.0.1, all [Header()] changed to [Title()]
- (InternalCore) NiloToonCharacter.shader: Due to upgrade to LWGUI 1.0.1, remove all [Space()]
- (InternalCore) NiloToonCharacter.shader: Due to upgrade to LWGUI 1.0.1, turn all [Main(x,_,2)] to [Main(x,_,off,off)]

### Fixed
- (Core) NiloToonCharacter.shader: fix Unity2020.3 additional light shadow not appear in build, but this fix will increase NiloToon_Character shader memory usage
----------------------------------------------
## [0.10.12] - 2022-08-21

### Fixed
- (Core) NiloToonSetToonParamPass.cs: fix a bug that is introduced in 0.10.11(refactor code)
- (Demo) vrm prefab will have their own material copy as override in prefab variant, these materials will not be overwrite by univrm after reimport all
- (Doc) added section "After deleting Library folder or Reimport All, all vrm material are reset to mtoon original material(purple in URP), How to stop it?"
----------------------------------------------
## [0.10.11] - 2022-08-20

### Important! Future NiloToonURP version 0.11.x will use Unity2021.3 as the minimum version
- (About NiloToonURP 0.11.x's Unity2019.4 support) Since Unity has stopped Unity2019.4's support on 01 Jun 2022(https://endoflife.date/unity), now Unity2019.4.40f1 is the last Unity2019 version available, we will remove NiloToonURP 0.11.x's Unity2019.4 support completely. If possible, please upgrade your project to Unity2021.3 before you upgrade to future NiloToonURP 0.11.x versions
- (About NiloToonURP 0.11.x's Unity2020.3 support) Since Unity2021.3LTS has been released for a while already, our testing found that Unity2021.3 is a better version than Unity2020.3, we will soon use Unity2021.3.x as the minimum version for using NiloToonURP in the near future, you can expect NiloToonURP 0.11.x's Unity2020.3 support will be removed also.
- If you must stay on Unity2019.4 or Unity2020.3, you should keep using NiloToonURP 0.10.x and not upgrading to 0.11.x

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it
- (Core) For URP12(Unity2021.3) or newer, if depthPriming is enabled, character shader's face will not render correctly, we are fixing it
- (Core) For URP13(Unity2022.1) or newer, if depthPriming is enabled, character will not render correctly, we are fixing it
- (Core) For URP13(Unity2022.1) or newer, NiloToon/Bloom will not render correctly, we are fixing it

### Added
- (Core) (Important) NiloToonChar.shader: added support for URP12(Unity2021.3LTS) Point and Spot light cookies, decal, reflection probe blending, reflection probe box projection, correct point light shadow
- (Core) NiloToonCharRenderingControlVolume.cs: added depthTexRimLightDepthDiffThresholdOffset
- (Core) NiloToonCharRenderingControlVolume.cs: added charRimLightMultiplierForOutlineArea && charRimLightTintColorForOutlineArea 
- (Demo) Added more testing models
- (Demo) +Assets\ThirdParty(GitHub)
- (Demo) +Assets\ThirdParty(GitHub)\URP_BlitRenderFeature-master
- (Demo) +Assets\ThirdParty(GitHub)\GT-ToneMapping-main
- (Demo) Packages\manifest.json: added .abc support

### Changed
- (Core) NiloToonCharRenderingControlVolume.cs: charRimLightCameraDistanceFadeoutStartDistance default value increased from 50 to 1000, because we want it to have no effect by default
- (Core) NiloToonCharRenderingControlVolume.cs: charRimLightCameraDistanceFadeoutEndDistance default value increased from 100 to 2000 because we want it to have no effect by default
- (Core) NiloToonShadowControlVolume.cs: changed depthBias's default value from 1 to 0.5
- (Core) NiloToonCharSelfShadowMapRTPass.cs:  changed depthBias's default value from 1 to 0.5
- (InternalCore) NiloToonSetToonParamPass.cs: refactor code
- (Demo) update project from Unity2020.3.33f1 to Unity2020.3.38f1
- (Demo) NiloToonShaderStrippingSettingSO.asset: demo project's default stripping now match mobile setting, to reduce build time(shader compile) and runtime shader memory
- (Demo) update luxiya cloth material
- (Demo) update Klee face material
- (Demo) update Barbara all materials
- (Demo) update MaGirl2 materials
- (Demo) Project Settings/Editor -> EnterPlayModeOption changed from off to on, to allow faster playmode testing in editor
- (Demo) Player Settings -> use il2cpp for build instead of mono, to allow better CPU performance in build
- (Demo) ForwardRenderer 5HighestQuality: add GT-ToneMapping renderer feature(default inactive), for tonemapping style testing
- (Demo) Assets\ThirdParty(VRM): update univrm to 0.99
- (Demo) ForwardRenderer 5HighestQuality: shadow range 10->200
- (Demo) ForwardRenderer 4ExtremeHighQuality: shadow range 10->100
- (Demo) ForwardRenderer 3VHighQuality: shadow range 10->50
- (Demo) ForwardRenderer 2HighQuality: shadow range 10->25

### Fixed
- (Core) NiloToonSetToonParamPass.cs: correctly support depth/normal texture request when deferred rendering is enabled. Now if you switch between ForwardRendering and DeferredRendering, NiloToon character shader's rendering result should be very similar
- (Core) NiloToonCharacter.shader: shader stripping will work correctly on URP12 also
- (Core) NiloToonEditor_AssetLabelAssetPostProcessor.cs: increase maxOverlapVertices from 10 to 100, this change will improve uv8's result (usually affect hair's tip outline)
- (Core) NiloToonCharacter_Shared.hlsl: now specular debug will correctly show masked specular area
- (Core) NiloToonCharacter_Shared.hlsl: optimize BaseColorAlphaClipTest_AndNiloToonPrepassBufferColorOutput clip() to return 0
----------------------------------------------
## [0.10.10] - 2022-06-28

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it
- (Core) For URP12(Unity2021.3) or newer, if depthPriming is enabled, character shader's face will not render correctly, we are fixing it
- (Core) For URP13(Unity2022.1) or newer, if deferred rendering is enabled, character shader will not render correctly, we are fixing it
- (Core) For URP13(Unity2022.1) or newer, NiloToon/Bloom will not render correctly, we are fixing it

### Breaking Changes
- (Core) NiloToon/bloom: will now affected by camera's post processing toggle

### Added
- (Core) NiloToonCharacter.shader: Added _UseHairStrandSpecularTintMap, _HairStrandSpecularTintMap, _HairStrandSpecularTintMapUsage, _HairStrandSpecularTintMapTilingXyOffsetZw

### Changed
- (Core) NiloToonShadowControlVolume.cs & NiloToonCharSelfShadowMapRTPass.cs: increase nilotoon self shadow map default shadowRange from 10 to 100

### Fixed
- (Core) NiloToonSetToonParamPass.cs: Make NiloToonURP running in Unity2022.1(URP13) possible
----------------------------------------------
## [0.10.9] - 2022-06-23

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it
- (Core) For URP12 or newer, if depthPriming is enabled, character shader's face will not render correctly, we are fixing it

### Fixed
- (Core) NiloToonEditor_AssetLabelAssetPostProcessor.cs: Fixed a critical bug -> "if MeshRenderer/SkinnedMeshRenderer is attached on .fbx GameObject's root, uv8 smooth normal can not be generated" 
----------------------------------------------
## [0.10.8] - 2022-06-20

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it
- (Core) For URP12 or newer, if depthPriming is enabled, character shader's face will not render correctly, we are fixing it

### Breaking Changes
- (Core) NiloToonEditorShaderStripping .cs: rewrite shader stripping, now will correctly strip all possible keywords, result in much smaller shader memory if you use NiloToonShaderStrippingSettingSO. If you find that a feature is missing in build after this update, make sure to include that feature in your NiloToonShaderStrippingSettingSO
- (Core) NiloToonCharacter.shader: make "specular hightlight" and "matcap(additive)" add their additive Luminance to alpha value, to simulate correct reflection on "very low or zero alpha" semi-transparent material. If you find that after this update, some semi-transparent's reflection is too bright, you need to reduce their intensity to a normal level 
- (Core) NiloToonCharacter.shader: _EditFinalOutputAlphaEnable & _ZOffsetEnable are now default off, since in most cases they are not needed
- (Core) NiloToonCharacter.shader: for all opauqe material(SrcBlend = One, DstBlend = Zero), shader will force outputAlpha = 1, to produce a correct alpha value on RenderTexture

### Added
- (Core) NiloToonCharacter.shader: + _BaseColor2
- (Core) NiloToonCharacter.shader: + _SpecularColorTintMapTilingXyOffsetZw,_SpecularColorTintMapUseSecondUv
- (Core) NiloToonPerCharacterRenderController.cs: +receiveAverageURPShadowMap,receiveStandardURPShadowMap,receiveNiloToonSelfShadowMap
----------------------------------------------
## [0.10.7] - 2022-06-13

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it
- (Core) For URP12 or newer, if depthPriming is enabled, character shader's face will not render correctly, we are fixing it

### Added
- (Core) NiloToonCharacter.shader: + _SpecularUseReplaceBlending

## Changed
- (InternalCore) NiloToonCharacter.shader: move ZOffsetFinalSum != 0 check to NiloZOffsetUtil.hlsl

### Fixed
- (Core) NiloToonCharacter.shader: Fixed "physical camera makes depth texture rim light in shader not correct" bug
- (Core) Fixed NiloToonCharacter.shader's shader warning
----------------------------------------------
## [0.10.6] - 2022-05-31

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it
- (Core) For URP12 or newer, if depthPriming is enabled, character shader's face will not render correctly, we are fixing it

### Added
- (Core) NiloToonCharacter.shader: + _OutlineUniformLengthInViewSpace
- (Core) NiloToonCharacter.shader: + _SkinMaskMapRemapStart,_SkinMaskMapRemapEnd
- (Core) NiloToonCharacter.shader: + _EnvironmentReflectionTintAlbedo
- (Core) added some Metal & stocking matcap textures
- (Doc) + section "How to set metal/reflective/fresnel rim light/specular material?" 
- (Doc) + section "How to set black/white stocking material?" 

### Changed
- (Core) NiloToonCharacter.shader: section "Environment Reflections" rewrite blending method, remove (Experimental tag)
----------------------------------------------
## [0.10.5] - 2022-05-26

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it
- (Core) For URP12 or newer, if depthPriming is enabled, character shader's face will not render correctly, we are fixing it

### Added
- (Core) support URP12 deferred rendering path
- (Core) support URP12 depth priming mode, but face is not rendering correctly in this version 
- (Core) added NiloToonShaderPatch(for URP12 or newer).unitypackage, user must import it when using URP12 or newer 

### Changed
- (Core) NiloToon debug window: default debug case is "JustBaseMap" now

### Fixed
- (Core) NiloToonAverageShadowTestRT.shader: fix a bug that make this shader not able to compile in Unity2019.4(URP7) 
----------------------------------------------
## [0.10.4] - 2022-05-23

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it

### Added
- (Core) NiloToon debug window: added "JustBaseMap" case, which is different to albedo
- (Core) NiloToonCharacter.shader: smoothness section added note "Make sure _Smoothness is not 0 in order to see specular result"

### Changes
- (Core) NiloToonShadowControlVolume.cs: increase shadowRange max value to 1000, from 100
- (Core) NiloToonCharacter.shader: _MatCapAlphaBlendTintColor added [HDR] tag

### Fixed
- (Core) NiloToonCharacter.shader: fix LoadDepthTextureLinearDepthVS() not clamping out of range loadTexPos, character depth tex rim light will render correctly on edge of screen now
- (Core) NiloToonCharacter.shader: fix some harmless shader warning
----------------------------------------------
## [0.10.3] - 2022-05-16

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it

### Breaking Changes
- (Core) NiloToonCharacter.shader: Rewrite Kajiya-Kay Specular (for hair) section, in old versions we hardcoded light direction as world space up, which is a wrong implementation, we now replace it with head up direction, we also rewrite NiloStrandSpecular.hlsl to use a better method. Old material settings of Kajiya-Kay Specular (for hair) section may need to adjust by user
- (Core) NiloToonCharacter.shader: _FaceShadowGradientMapChannel's default value change from g to r, because the recommend texture format is changed to R8 now, which use less memory
- (Core) FaceShadowGradientMaps change format from auto(rgba32) to r8
- (Demo) upgrade demo project to 2020.3.33f1

### Added
- (Core) support Unity2022.1.0f1, added "NiloScaledScreenParamUtil.hlsl" for supporting Unity2022.1.0f1(URP13.x)
- (Core) NiloToonCharacter.shader: section "Depth Texture Rim Light and Shadow" added _ApplyDepthTexRimLightFixDottedLineArtifacts and _DepthTexRimLightFixDottedLineArtifactsExtendMultiplier, user can enable it to solve DepthTex rim light's weird white line/dot artifacts on uv seams
- (Core) NiloToonCharacter.shader: section "FaceShadowGradientMap" added note about using texture R8 format and None compression
- (Core) NiloToonCharacter.shader: added _NiloToonSelfShadowMappingTintColor
- (Core) NiloToonCharacter.shader: added _FaceShadowGradientThresholdMin,_FaceShadowGradientThresholdMax
- (Core) NiloToonCharacter.shader: added _FaceShadowGradientMapInvertColor
- (Core) NiloToonCharacter.shader: added _FaceShadowGradientMapUVxInvert
- (Core) NiloToonCharacter.shader: added _IgnoreDefaultMainLightFaceShadow
- (Core) NiloToonCharacter.shader: added _FaceMaskMapRemapMinMaxSlider
- (Core) NiloToonCharacter.shader: added _FaceMaskMapInvertColor
- (Core) NiloToonCharacter.shader: added _RampLightingNdotLRemapMinMaxSlider
- (Core) NiloToonCharacter.shader: added _RampLightingSampleUvYTexChannelMask
- (Core) NiloToonCharacter.shader: added _RampLightingSampleUvYTexInvertColor, _RampLightingUvYRemapMinMaxSlider
- (Core) NiloToonCharacter.shader: added _RampLightingFaceAreaRemoveEffect
- (Core) NiloToonCharacter.shader: added _BaseMapStackingLayer1MaskInvertColor
- (Core) NiloToonCharacter.shader: matcap sections added note about usable textures in NiloToonURP folder
- (Core) NiloToonCharacter.shader: Outline pass added multi_compile _ADDITIONAL_LIGHT_SHADOWS
- (Core) NiloToonEditorShaderStripping: added _ADDITIONAL_LIGHT_SHADOWS stripping
- (Demo) Support Unity2022.1.0f1
- (Demo) Added and updated a few new demo characters

### Changes
- (Core) [IMPORTANT]NiloToonCharacter.shader: due to vulkan constant buffer size limitation, we disabled srp batching in vulkan, all shader features are now supported correctly on vulkan again, but batching performance is reduced 
- (Core) update NiloToonAverageShadowTestRT.shader algorithm
- (InternalCore) apply "NiloScaledScreenParamUtil.hlsl"'s GetScaledScreenWidthHeight() to replace all _CameraDepthTexture_TexelSize.zw calls
- (InternalCore) NiloToonCharacter.shader Lit pass alpha blend write = One OneMinusSrcAlpha (alpha set to One OneMinusSrcAlpha, due to semi-transparent overwrite wrong value to RT.a, see https://twitter.com/MuRo_CG/status/1511543317883863045)
- (InternalCore)Outline pass always render as opaque colors, so we force output alpha always equals 1
- (Demo) PC quality settings + SSAO
- (Doc) updated document about how to install Jobs in package manager

### Fixed
- (Core) NiloToonCharacter.shader: _RampLightingTex force LOD 0 sample in shader, to fix sampling mipmap artifact
- (Core) NiloToonCharacter.shader: write a correct cameraBackwardDir method for _ApplyAlphaOverrideOnlyWhenFaceForward
- (Core) NiloToonCharacter.shader: fix a bug that ramp lighting doesn't consider shading grade map
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor: fix a bug that after clicking auto setup character, uv8 is not generated
- (Core) NiloStrandSpecular.hlsl: fix dirAtten missing multiply bug
- (Core) FaceShadowGradientMap textures use repeat instead of clamp
- (Demo) fix 4ExtremeHighQuality.asset use wrong rendererdata bug
- (Demo) jp envi scene disable character screen space outline
----------------------------------------------
## [0.10.2] - 2022-04-27

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it

### Added
- (Core) NiloToonCharacter.shader: section "BaseMap Alpha Override" added _ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCamera

### Fixed
- (Core) revert this change due to invalid chinese char in .cs->　NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: ｗhen user click "Auto setup this character" button, we added more material name keywords, to help NiloToonURP auto setup more materials to correct settings
----------------------------------------------
## [0.10.1] - 2022-04-25

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it

### Added
- (Core) NiloToonPerCharacterRenderController.cs: added perCharacterRimLightSharpnessPower, it will let user control the sharpness of perCharacterRimLight
- (Core) NiloToonPerCharacterRenderController.cs: added "PerCharacter BaseMap override" section, user can use it to apply effect to a character(e.g. character convert to ice texture when freeze)
- (Core) NiloToonShaderStrippingSettingSO.cs: added include_NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE
- (Core) NiloToonCharacter.shader: section "Screen Space Outline" added note about "Not supported on Vulkan"
- (Core) NiloToonCharacter.shader: section "Shading Grade Map" added _ShadingGradeMapInvertColor, _ShadingGradeMapApplyRange
- (Core) NiloToonCharacter.shader: section "Face Shadow Gradient Map" added _FaceShadowGradientResultSoftness, _FaceShadowGradientMapChannel
- (Core) NiloToonCharacter.shader: section "BaseMapAlphaOverride" added _AlphaOverrideStrength
- (Core) NiloToonCharacter.shader: section "Traditional Outline" added _OutlineTintColorSkinAreaOverride
- (Core) NiloToonShadowControlVolume.cs: added URPShadowblurriness,URPShadowAsDirectLightTintIgnoreMaterialURPUsageSetting
- (Core) added textures: FaceShadowGradientMap_StyleA,FaceShadowGradientMap_StyleB,FaceShadowGradientMap_StyleC
- (Core) added NiloToonEnvironment(for URP12.x).unitypackage, user can install it to support URP12
- (InternalCore) ShaderDrawer.cs: TitleDecorator added "SpaceLine" keyword, for supporting Rider  
- (InternalCore) NiloToonCharacter.shader: now support Rider highlight (replace [Title(X, )] to keyword SpaceLine)
- (Demo) Added more test characters
- (Demo) Added new scene: full body shoot scene
- (Demo) upgrade Magica cloth to latest version, fix Magica cloth not working in 2021.2 bug

### Breaking Change
- (Core) NiloToonCharacter.shader: reorder and rewrite UI, added header & space to group functions into categories. (This is a very big change to NiloToonCharacter.shader, so we consider it as Breaking Change even it should not break anything unless you edit NiloToonCharacter.shader's code before)

### Changed
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: when user click "Auto setup this character" button, it will now auto find the best faceUpDirection and faceForwardDirection, it is an important improvement because most user did not not setup faceUpDirection and faceForwardDirection correctly
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: hen user click "Auto setup this character" button, we added more material name keywords, to help NiloToonURP auto setup more materials to correct settings
- (Core) NiloToonCharacter.shader: _HairStrandSpecularShapePositionOffset range increase from (-0.5,0.5) to (-1,1)
- (Core) NiloToonCharacter.shader: if _NILOTOON_DEBUG_SHADING is active, now we only render NiloToonForwardLitPass to show better debug result
- (Core) NiloToonCharacter.shader: Convert "Depth Tex Rim Light and Shadow" section to a toggle feature
- (Core) NiloToonPerCharacterRenderController.cs: convert GetFinalFaceDirectionWS(...) to a public function

### Fixed
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: fix a critical performance problem which NiloToonPerCharacterRenderController is extremely laggy when user focus it
- (Core) NiloToonCharacter.shader: section "Shading Grade Map" fixed _ShadingGradeMap's wrong description
- (Core) NiloToonCharacter.shader: fix a bug where _BackFaceTintColor is not working
- (Core) NiloToonCharacter.shader: _ZOffsetMultiplierForTraditionalOutlinePass fixed wrong default value in UI(0 is wrong, 1 is correct)
- (Core) NiloToonBloomVolume.cs: it will now correctly support URP12, user don't need to do anything
- (Core) NiloToonCharacter.shader: outline pass added shader_feature_local _RECEIVE_URP_SHADOW, else outline is not correctly darken when character is within URP shadow
- (Demo) Demo project will now strip include_NILOTOON_DISSOLVE and include_NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE in NiloToonShaderStrippingSettingSO.asset
----------------------------------------------
## [0.9.1] - 2022-03-21

### Known Issue
- (Core) NiloToonEnvironment.shader: this shader is not SRP-batcher compatible since 0.8.20(0.8.19 is safe), we are fixing it

### Added
- (Core) NiloToonCharacter.shader: section "Face Shadow Gradient Map" added _FaceShadowGradientMaskMapChannel
- (Core) NiloToonCharacter.shader: section "Depth Texture Rim Light and Shadow" added _DepthTexRimLightIntensity,_DepthTexRimLightMixWithBaseMapColor
- (Core) NiloToonCharacter.shader: section "Mat Cap (additive)" added _MatCapAdditiveMixWithBaseMapColor
- (Core) NiloToonCharacter.shader: section "Kajiya-Kay Specular (for hair)" added _HairStrandSpecularMixWithBaseMapColor,_HairStrandSpecularOverallIntensity,_HairStrandSpecularMainIntensity,_HairStrandSpecularSecondIntensity,_HairStrandSpecularShapeFrequency,_HairStrandSpecularShapeShift,_HairStrandSpecularShapePositionOffset
- (Core) NiloToonCharRenderingControlVolume.cs: added characterOverallShadowTintColor
- (Core) NiloToonCharRenderingControlVolume.cs: added characterOverallShadowStrength
- (Core) NiloToonCharRenderingControlVolume.cs: added specularTintColor
- (Core) NiloToonCharRenderingControlVolume.cs: added charOutlineWidthAutoAdjustToCameraDistanceAndFOV
- (Core) NiloToonToonOutlinePass.cs: added charOutlineWidthAutoAdjustToCameraDistanceAndFOV (you can find it in NiloToonAllInOneRendererFeature's UI)
- (Demo) added new character prefab: CH0066_Mesh Variant.prefab, suz.prefab, yu.prefab, miku.prefab, Miko (NiloToon Variant).prefab

### Breaking Change
- (Core) fix "NiloToonCharacter.shader not working on Vulkan platform(android Mali G-** GPU)", but now we disabled basemap stacking layer 3-10 & screen space outline on vulkan as a work around to avoid Vulkan platform(android Mali G-** GPU) constant buffer size limitation
- (Core) NiloToonCharacter.shader: base map stacking layer 3,5,7,9 will now use sampler_linear_clamp, to reduce sampler count
- (Core) NiloToonCharacter.shader: base map stacking layer 4,6,8,10 will now use sampler_linear_repeat, to reduce sampler count
- (Core) NiloToonCharacter.shader: 5 mask maps -> _MatCapAlphaBlendMaskMap,_MatCapAdditiveMaskMap,_MatCapOcclusionMaskMap,_SkinMaskMap,_EnvironmentReflectionMaskMap use sampler_BaseMap instead of their own texture sampler, in order to reduce sampler count
- (InternalCore) NiloOutlineUtil.hlsl->GetOutlineCameraFovAndDistanceFixMultiplier(...) added "applyPercentage" param, used by NiloToonCharRenderingControlVolume/NiloToonToonOutlinePass's charOutlineWidthAutoAdjustToCameraDistanceAndFOV
- (InternalCore) NiloStrandSpecular.hlsl->ShiftTangent(...) added frequency,shift,offset param
- (Demo) Demo project -> android player setting -> default Graphics API = Vulkan+OpenGLES now, instead of OpenGLES only

### Changed
- (Core) better notes in character shader material UI
- (Core) remove _StencilRef and _StencilComp because they are not being used for a long time
- (Core) rename "Alpha Override" to "BaseMap Alpha Override" (affects UI display only)
- (Core) rename "KajiyaKaySpecular" to "Kajiya-Kay Specular (for hair)" (affects UI display only)
- (InternalCore) Improve NiloToonCharacter.shader comments in all related .hlsl files
- (InternalCore) NiloToonCharacter_Shared.hlsl: rename positionWS to positionWS_ZOffsetFinalSum because it is a packed float4(xyz = positionWS, w = ZOffsetFinalSum)
- (InternalCore) NiloToonCharacter_Shared.hlsl: change _GlobalSpecularIntensityMultiplier to _GlobalSpecularTintColor to combine specularTintColor in volume
- (Doc) Improve document pdf
- (Demo) Upgrade project to Unity2020.3.30f1
- (Demo) Optimize NiloToonDancingDemo.unity
- (Demo) Optimize NiloToon_CRS_SampleScene.unity
- (Demo) Optimize NiloToon_CloseShot_SampleScene.unity (remove rendering URP shadow and NiloToon's average shadow rendering)
- (Demo) Force NiloToon_CloseShot_SampleScene.unity's NiloToon shadowmap resolution = 2048, even for mobile quality
- (Demo) Optimize RenderScale and shadow resolution for mobile quality
- (Demo) Force android max screen resolution height = 900, instead of 1080
- (Demo) Highest quality turn on additional light shadow, and allow 4096 resolution
- (Demo) MaGirl materials allow Screen space outline now
- (Demo) Improve Sienna skin material setting
- (Demo) Improve Luxiya, Klee material setting
- (Demo) NiloToonDancingDemo.unity update camera default active state, user should be able to see character before entering play mode

### Fixed
- (Core) NiloToonAverageShadowTestRTPass.cs: fix "camera anime postprocess average shadow not working"
- (Core) NiloToonAverageShadowTestRTPass.cs: fix "camera anime postprocess average shadow -> shadow test position out of camera frustrum bound" problem
- (Core) NiloToonCharacter.shader: fix shader compile warning
- (Doc) Fix typo
----------------------------------------------
## [0.8.35] - 2021-12-29

### Breaking Change
- (Core) NiloToonCharacter.shader: set _ZOffsetMultiplierForTraditionalOutlinePass's default value to 1, which enable ZOffset for outline pass by default now (e.g. 3D eyebrow with ZOffset will show outline by default now)

### Added
- (Core) NiloToonPerCharacterRenderController.cs: Add "Dissolve" section
- (Core) NiloToonCharacter.shader: Add "Pass On/Off" section, allow you to disable target pass(e.g. disable URP shadow caster) per material
- (InternalCore) Added "NiloToonURP7Support.hlsl"
- (Core) Added "NiloToonDefaultDissolvePerlinNoise" in Resources folder
- (Core) NiloToonShaderStrippingSettingSO.cs: + _NILOTOON_DISSOLVE stripping

### Fixed
- (Core) NiloToonCharacter.shader: support 2019.4.33f1(URP7.7.1)
- (Core) NiloToonCharacter.shader: support OpenGLES3.0 & WebGL by removing #pragma target 4.5
- (Core) NiloToonCharacter.shader: fix debug shading can't compile for face area
- (Core) NiloToonCharacter.shader: try to fix a bug introduced in 0.8.22, which makes mobile (vivo Y73s) (TAS_AN00) not rendering corerctly
- (Core) NiloToonCharacter.shader: added if() check to make planar reflection correct by default, when ZOffsetFinalSum is 0
- (Core) NiloToonCharacter.shader: ZOffset will not affect depth texture rim light and shadow's result now. In the past, ZOffset will make material's depth texture rim light become white wrongly. (e.g. eyebrow with ZOffset)
----------------------------------------------
## [0.8.34] - 2021-12-14

### Added
- (Core) NiloToonCharacter.shader: add "Parallax Map" feature, same as URP's Lit.shader
- (Core) NiloToonCharacter.shader: add "Shading Grade Map" feature, similar to UTS2's "Shading Grade Map"
- (Core) NiloToonCharacter.shader: add "Enable Rendering" toggle
- (Core) NiloToonCharacter.shader: add "Back face color tint"

### Changed
- (Core) NiloToonCharacter.shader: improve additional light shading for face area
- (InternalCore) NiloToonCharacter.shader: viewDirectionWS is now calculate in vertex shader using GetWorldSpaceViewDir(), and will take care of orthographic camera
- (InternalCore) NiloToonCharacter.shader: change some NormalizeNormalPerPixel() to normalize() to ensure normalize in all platform
- (InternalCore) NiloToonCharacter.shader: refactor additional light code
- (InternalCore) NiloToonCharacter.shader: refactor "disable rendering" section

### Fixed
- (Core) NiloToonCharacter.shader: remove wrong unity_WorldTransformParams.w multiply when calculating bitangent in fragment shader
- (Core) NiloToonCharacter.shader: fixed a bug that Perspective removal doesn't affect NiloToonPrepassBufferPass correctly
----------------------------------------------
## [0.8.33] - 2021-12-6

### Added
- (Core) Added NiloToonDepthTexRimLightAndShadowGlobalOnOff.cs, and will now called by NiloToonPlanarReflectionHelper.cs
- (Core) NiloToonPlanarReflectionHelper.cs: will now call NiloToonDepthTexRimLightAndShadowGlobalOnOff's functions also
- (Core) NiloToonPlanarReflectionHelper.cs: added command buffer version functions, for user that renders planar reflection using renderer feature(command buffer)
- (Doc) Added "NiloToon’s character shader is broken in build, it only works correctly in editor"

### Changed
- (Core) NiloToonCharacter.shader: now _BASEMAP_STACKING_LAYER2 will use it's own texture sampler sampler_BaseMapStackingLayer2Tex, to allow more wrap mode control from user
----------------------------------------------
## [0.8.32] - 2021-12-4

### Fixed
- (Core) NiloToonEnvironment.shader: added _CASTING_PUNCTUAL_LIGHT_SHADOW multi_compile, which means support URP11 point light shadow
----------------------------------------------
## [0.8.31] - 2021-12-1

### Added
- (Core) NiloToonEnvironment.shader: added "Screen Space Outline" section
----------------------------------------------
## [0.8.30] - 2021-12-1

### Added
- (Core) NiloToonEnvironment.shader: "splatmap" section added PackedDataMap(b for height) and a height multiplier
----------------------------------------------
## [0.8.29] - 2021-11-30

### Changed
- (Core) NiloToonCharacter.shader: BaseMap Stacking Layer 1's texture allow user to edit wrap mode
----------------------------------------------
## [0.8.28] - 2021-11-26

### Fixed
- (Core) NiloToonEnvironment.shader: fix "splatmap normalmap not enabled" bug

### Changed
- (Demo) Upgrade project to Unity2020.3.23f1
----------------------------------------------
## [0.8.27] - 2021-11-26

### Fixed
- (Core) support URP7.6.0 (Unity2019.4.32f1)
----------------------------------------------
## [0.8.26] - 2021-11-25

### Added
- (Core) NiloToonCharacter.shader: +_FixFaceNormalUseFlattenOrProxySphereMethod, to let user choose how to fix face normal (flat or round sphere)

### Fixed
- (Core) NiloToonEnvironmentShaderGUI.cs: +temp support URP12
----------------------------------------------
## [0.8.25] - 2021-11-24

### Fixed
- (Core) NiloToonEnvironment.shader: use a much better splat map blending method

### Changed
- (InternalCore) NiloToonCharacter_Shared.hlsl: change Varying struct's data layout
- (InternalCore) NiloToonCharacter_Shared.hlsl: cleanup and refactor code, improve code notes
----------------------------------------------
## [0.8.24] - 2021-11-22

### Fixed
- (Core) NiloToonCharacter.shader: fix main light culling mask bug when more than 1 directional light exist in scene
----------------------------------------------
## [0.8.23] - 2021-11-17

### Added
- (Core) NiloToonEnvironment.shader: +splatmap's smoothness control
- (Core) NiloToonEnvironment.shader: +splatmap's normal intensity control

### Changed
- (Core) NiloToonEnvironment.shader: improve splatmap's mateiral UI

### Fixed
- (Core) NiloToonEnvironment.shader: fix blending softness's bug
----------------------------------------------
## [0.8.22] - 2021-11-14

### Added
- (Core) NiloToonCharacter.shader: increase BaseMapStackingLayer count from 6 to 10, each layer added mask texture, uv scale offset, uv anim speed
- (Core) NiloToonCharacter.shader: "ZOffset" section now affect NiloToonSelfOutlinePass also
- (Core) NiloToonCharacter.shader: "ZOffset" section added _ZOffsetMultiplierForTraditionalOutlinePass

### Changed
- (InternalCore) NiloToonCharacter.shader: BaseMapStackingLayer now use a shared sampler to prevent max 16 sampler limit
- (InternalCore) NiloToonCharacter.shader: unify all ZOffset to 1 NiloGetNewClipPosWithZOffsetVS() call
----------------------------------------------
## [0.8.21] - 2021-11-12

### Changed
- (Core) NiloToonEnvironment.shader: splat map section now controlled by keyword shader_feature _SPLATMAP
----------------------------------------------
## [0.8.20] - 2021-11-11

### Changed
- (Core) NiloToonCharacter.shader: "Face Shadow Gradient Map" section is not experimental now 
- (Core) NiloToonEnvironment.shader: add working splat map section

### Fixed 
- (Core) NiloToonCharacter.shader: "Face Shadow Gradient Map" will only render on face area
- (Core) NiloToonCharacter.shader: "Face Shadow Gradient Map"'s mask texture uv will now use basemap's uv
----------------------------------------------
## [0.8.19] - 2021-11-10

### Added
- (Core) NiloToonEnvironment.shader: added WIP splat map section, it has no effect in this version
- (Core) NiloToonCharRenderingControlVolume.cs: added overrideLightDirectionIntensity, overridedLightUpDownAngle, overridedLightLRAngle

### Changed 
- (Core) NiloToonCharacter.shader: _FaceShadowGradientOffset default value is now 0.1, instead of 0
- (InternalCore) NiloToonEnvironment.shader: now GUI is controlled by custom material UI code
----------------------------------------------
## [0.8.18] - 2021-11-04

### Breaking Changes
- (Core) NiloToonCharacter.shader: much better additional light default value in "Lighting style" section

### Fixed
- (Core) NiloToonCharacter.shader: fix point spot light's shader bug about URP's additional light shadow
----------------------------------------------
## [0.8.17] - 2021-11-03

### Breaking Changes
- (Core) NiloToonCharacter.shader: better additional light default value in "Lighting style" section

### Added
- (Core) NiloToonCharacter.shader: +_AdditionalLightIgnoreOcclusion,+_AdditionalLightDistanceAttenuationClamp

### Fixed
- (Core) NiloToonCharacter.shader: fix point spot light's shader bug when shadow is off
----------------------------------------------
## [0.8.16] - 2021-11-02

### Breaking Changes
- (Core) NiloToonCharacter.shader: completely rewrite additional light!!! it now support shadow map, fragment quality shading, adjustable cel shade param, but much slower.
- (Core) NiloToonCharacter.shader: add _AdditionalLightCelShadeMidPoint,_AdditionalLightCelShadeSoftness,_AdditionalLightIgnoreCelShade
- (Core) NiloToonCharacter.shader: add _OverrideAdditionalLightCelShadeParamForFaceArea,_AdditionalLightCelShadeMidPointForFaceArea,_AdditionalLightCelShadeSoftnessForFaceArea,_AdditionalLightIgnoreCelShadeForFaceArea
----------------------------------------------
## [0.8.15] - 2021-10-31

### Breaking Changes
- (Core) NiloToonCharacter.shader: add "_SpecularShowInShadowArea" in "Specular Highlights" section, which is default 0. Before this version specular result will show in shadow area also. User will need to edit this slider if they want to show specular result in shadow area.
- (Core) NiloToonCharacter.shader: "Ramp Texture(Specular)" section affected by _SpecularMap mask now
----------------------------------------------
## [0.8.14] - 2021-10-29

### Added
- (Core) NiloToonEditorDebugWindow.cs: added "Force all windows update in edit mode" section, to allow user force update all view(e.g. game view) in edit mode, but enable it will make editor slower due to extra repaint
- (Core) NiloToonCharacter.shader: add "Matcap (occlusion)" section
- (Core) NiloToonCharacter.shader: added _FaceShadowGradientIntensity & _FaceShadowGradientMaskMap for "Face Shadow Gradient Map" section

### Fixed
- (Core) NiloToonEditorDebugWindow.cs: fix "editing debug shading param can not instantly see result in game view" bug, now we call  UnityEditorInternal.InternalEditorUtility.RepaintAllViews() when changes are made by user
- (Core) NiloToonEditorDebugWindow.cs: fix "if there are more than 1 NiloToon renderer feature enabled, debug window is not working" bug, solved by using static var instead of Singleton Instance var in NiloToonSetToonParamPass.cs
----------------------------------------------
## [0.8.13] - 2021-10-15

### Fixed
- (Core) NiloToonToonOutlinePass.cs: fix "when user edit NiloToon renderer feature's value, screen will flickering" bug
----------------------------------------------
## [0.8.12] - 2021-10-15

### Added
- (Core) NiloToonCharacter.shader: add _FaceAreaCameraDepthTextureZWriteOffset, which replaced a hardcode const value
----------------------------------------------
## [0.8.11] - 2021-10-14

### Fixed
- (Core) NiloToonSetToonParamPass.cs: fix "when user edit NiloToon renderer feature's value, screen will flickering" bug
----------------------------------------------
## [0.8.10] - 2021-10-13

### Changed
- (Demo) manifest.json: remove com.unity.toolchain.win-x86_64-linux-x86_64
----------------------------------------------
## [0.8.9] - 2021-10-11

### Added
- (Core) NiloToonCharacter.shader: added _FaceShadowGradientMapUVScaleOffset & _DebugFaceShadowGradientMap for "Face Shadow Gradient Map" section
----------------------------------------------
## [0.8.8] - 2021-10-10

### Added
- (Core) NiloToonCharacter.shader: add more tips and information for "Face Shadow Gradient Map" section

### Fixed
- (Core) NiloToonToonOutlinePass.cs: added isPreviewCamera check for screen space outline, to solve "material preview window makes outline flicker" bug
----------------------------------------------
## [0.8.7] - 2021-10-8

### Breaking Changes
- (Core) NiloToonCharacter.shader: fix a bug "normalmap can not affect lighting result"
----------------------------------------------
## [0.8.6] - 2021-10-4

### Breaking Changes
- (Core) NiloToonPerCharacterRenderController.cs: fix a gamma/linear color bug that cause play/edit mode color is not the same
----------------------------------------------
## [0.8.5] - 2021-9-29

### Added
- (Core) NiloToonCharacter.shader: added _DepthTexRimLightAndShadowWidthTexChannelMask and _OutlineWidthTexChannelMask option

### Fixed
- (Core) NiloToonEditorPerCharacterRenderControllerCustomEditor.cs: + null check
----------------------------------------------
## [0.8.4] - 2021-9-28

### Added
- (Core) NiloToonPerCharacterRenderController.cs: added allowCacheSystem, to let user control CPU optimization option
- (Core) NiloToonPerCharacterRenderController.cs: added API RequestForceMaterialUpdateOnce(), for user to call after changing material in playmode
- (Core) NiloToonPerCharacterRenderController.cs: OnEnable will now call RequestForceMaterialUpdateOnce()
----------------------------------------------
## [0.8.3] - 2021-9-27

### Breaking Changes
- (Core) NiloToonPerCharacterRenderController.cs: combine allowRenderDepthOnlyPass and allowRenderDepthNormalsPass into 1 public bool allowRenderDepthOnlyAndDepthNormalsPass

### Added
- (Core) NiloToonCharacter.shader: BaseMap Alpha Blending Layer 1 added UVScaleOffset, UVAnimSpeed, and optional mask texture. If it is useful, we will add these feature to other layers

### Changed
- (Core) remove keyword _NILOTOON_ENABLE_DEPTH_TEXTURE_RIMLIGHT_AND_SHADOW in C# and shader, replaced by a new material float _NiloToonEnableDepthTextureRimLightAndShadow. In order to trade a small amount of fragment performance for 50% lower shader memory usage
- (Core) NiloToonPerCharacterRenderController.cs: _NiloToonEnableDepthTextureRimLightAndShadow will not be enabled if allowRenderDepthOnlyAndDepthNormalsPass is false, to prevent wrong rim light result when user disabled allowRenderDepthOnlyAndDepthNormalsPass
- (Core) NiloToonPerCharacterRenderController.cs: auto set up button will not upgrade particle and sprite material anymore

### Fixed
- (Core) NiloToonPerCharacterRenderController.cs: now script will not miss material update(shadowTestIndex_RequireMaterialSet) due to cache logic when setting up a new character 
- (Core) fixed a bug that triggers useless Debug.warning when setting up a new character using auto button
----------------------------------------------
## [0.8.2] - 2021-9-23

### Added
- (Core) NiloToonCharacter.shader: added _DepthTexRimLightThresholdOffset and _DepthTexRimLightFadeoutRange, to let user control rim light area
----------------------------------------------
## [0.8.1] - 2021-9-21

### Added
- (Core) NiloToonCharacter.shader: added "Face shadow gradient map" section, allow user to control face shadow's visual by a special threshold gradient grayscale texture
- (Core) NiloToonPerCharacterRenderController.cs: added "Face Up Direction", for user to setup. (will affect material's "Face shadow gradient map" result)
- (Core) NiloToonEditorShaderStripping.cs will strip _FACE_SHADOW_GRADIENTMAP if _ISFACE is off also
- (Core) NiloToonPerCharacterRenderController: added allowRenderShadowCasterPass,allowRenderDepthOnlyPass,allowRenderDepthNormalsPass,allowRenderNiloToonSelfShadowCasterPass,allowRenderNiloToonPrepassBufferPass for user side optimization option
- (Demo) Update Ganyu.prefab and Klee.prefab's face rendering(using new "Face shadow gradient map" feature)

### Changed
- (Demo) upgrade URP from 10.5.0 to 10.5.1
- (Demo) optimize close shot scene's camera near far

### Fixed
- (Core) optimize NiloToonPerCharacterRenderController.LateUpdate()'s CPU time cost (only call material.SetXXX when value changed) and "IEnumerable<Renderer> AllRenderersIncludeAttachments()"'s GC
- (Demo) fix bug: "multiple incorrect renderer feature data in forward renderers"
----------------------------------------------
## [0.7.3] - 2021-9-12

### Changed
- (Core) NiloToonPerCharacterRenderController will not clear material property block every frame now

### Fixed
- (Core) correctly support 'ClothDynamics' asset
----------------------------------------------
## [0.7.2] - 2021-9-10

### Added
- (Core) NiloToonCharacter.shader: Emission section added _EmissionMapUseSingleChannelOnly and _EmissionMapSingleChannelMask
- (Core) NiloToonCharacter.shader: added "Support 'ClothDynamics' asset" section(just an on/off toggle)
- (InternalCore) Added NiloToonFullscreen_Shared.hlsl, to provide FullscreenVert() function for fullscreen shaders for URP9 or lower

### Changed
- (Core) When EnableDepthTextureRimLightAndShadow is off, low quality fallback rim light will NOT consider normalmap contribution anymore, which can produce rim light that is much more similar to depth texture rim light

### Fixed
- (Core) Fix shader bug to correctly support Unity 2019  
----------------------------------------------
## [0.7.1] - 2021-9-6

### Added
- (Core) Added NiloToonBloomVolume.cs, a bloom volume that is similar to URP's offical Bloom, but with more controls
- (Core) Added NiloToonRenderingPerformanceControlVolume .cs, a control volume that let you control performance per volume
- (Core) NiloToonCharacter.shader: Added "Allow NiloToonBloom Override?" section, and all required functions
- (InternalCore) NiloToonCharacter_Shared.shader: Added "Allow NiloToonBloom Override?" section required MACRO, uniforms and functions
- (InternalCore) Added new files NiloToonPrepassBufferRTPass.cs, NiloToonUberPostProcessPass.cs, NiloToonBloom.shader & NiloToonUberPost.shader
- (InternalCore) NiloToonAllInOneRendererFeature.cs now enqueue NiloToonPrepassBufferRTPass and NiloToonUberPostProcessPass
- (InternalCore) NiloToonCharacter.shader: Added NiloToonPrepassBuffer pass

### Changed
- (Core) NiloToonCharacter.shader: _CelShadeSoftness minimum value is now 0.001, instead of 0, to avoid lighting direction flipped.
- (Core) Remove large amount of useless Debug.Log of NiloToonEditor_AssetLabelAssetPostProcessor.cs
- (InternalCore) Delete files that are not currently using: MathUtility.cs and NiloToonShaderStrippingSettingSO.cs(a duplicated file)

### Fixed
- (Core) all character shader: all _DitherOpacity are now correctly replaced by_DitherFadeoutAmount, shader can compile correctly(material not pink anymore)
- (Core) fix NiloToonEditor_AssetLabelAssetPostProcessor's memory leak, which will produce fatal error when importing large amount of character
- (InternalCore) add namespace to NiloToonEditor_EditorLoopCleanUpTempAssetsGenerated.cs and NiloToonEditorSelectAllMaterialsWithNiloToonShader.cs

### TODO
- (Core) Optimize NiloToonPerCharacterRenderController.LateUpdate() (only call material.SetXXX when value changed)
----------------------------------------------
## [0.6.3] - 2021-8-27

### Added
- (Core) NiloToonCharacter.shader: Added a "BaseMap Stacking Layer 4-6" section
----------------------------------------------
## [0.6.2] - 2021-8-25

### Changed
- (Core) change _DitherOpacity to _DitherFadeoutAmount (not just rename, but with logic change). This will fix a bug -> preview window can't render nilotoon character shader
----------------------------------------------
## [0.6.1] - 2021-8-24

### Added
- (Core) NiloToonPerCharacterRenderController: Added a "Select all NiloToon_Character materials of this character" button
- (Core) Added NiloToonShaderStrippingSettingSO.cs, a scriptable object storing per platform NiloToon shader stripping settings
- (Core) NiloToonAllInOneRendererFeature: Added shaderStrippingSettingSO slot, user can assign a NiloToonShaderStrippingSettingSO to control NiloToon's shader stripping per platform
- (Core) NiloToonCharacter.shader: Added BaseMapStackingLayer section (1-3)
- (Core) NiloToonCharRenderingControlVolume: Added bool specularReactToLightDirectionChange
- (Core) NiloToonCharacter.shader: Added _NiloToonSelfShadowIntensityMultiplierTex in "Can Receive NiloToon Shadow?" section, useful if you want to control nilo self shadow intensity by a texture
- (Demo) Added NiloToonShaderStrippingSettingSO to project, which reduce all platform's shader memory usage by 50%, and android/iOS shader memory usage by 75%, this change will let demo apk run on low memory phones(1-2GB).
- (Demo) Added "Close Shot" scene, showing a close shot male character.
- (DOC) Added "How to separate IsFace, because I combine everything into 1 renderer/material" section
- (DOC) Added "Some DepthTexture shadow is missing if shadow caster is very close to shadow receiving surface"
- (DOC) Added a few section's about how to use volume to edit visual globally
- (DOC) Added "How to make specular results react to light direction change?" section

### Changed
- (Core) delete all NiloToonURPFirstTimeOpenProject folder, scripts and files
- (Doc) rewrite "NiloToon shader is using too much memory, how to reduce it?" section, to explain how to use NiloToonShaderStrippingSettingSO 

### Fixed
- (Core) Fixed a bug -> when dither fadeout is 0% (completely invisible), depth/depthNormal texture pass will not render
----------------------------------------------
## [0.5.1] - 2021-8-16

### Added
- (Core) NiloToonPerCharacterRenderController's dither fade out will now affect URP's shadowmap, will rely on URP's soft shadow filter to improve the final result.

### Changed
- (Demo) Now demo project will dynamic load character from Resources folder, instead of just placing every character in scene. This will reduce memory usage a lot, which helps android build(.apk) to prevent out of memory crash.
- (Demo) keepLoadedShadersAlive in PlayerSetting is now false, to save some memory usage on scene change

### Fixed
- (Core) Fix an important bug which NiloToonPerCharacterRenderController wrongly edit URP's default Lit material's shader. If any user were affected by version 0.4.1, please update to 0.5.1 and delete URP's Lit.material in the project's Library folder, doing this will trigger URP's regenerate default asset, which reset URP's Lit material and fixes the problem.
- (Core) Environment shader will NOT receive screen space outline if SurfaceType is Transparent
----------------------------------------------
## [0.4.1] - 2021-8-10

### Breaking Changes
- (Core) NiloToonPerCharacterRenderController.cs: rename extraThickOutlineMaxiumuFinalWidth -> extraThickOutlineMaximumFinalWidth

### Added
- (Core) NiloToonPerCharacterRenderController: Added "Auto setup this character" button, user can use click this button to quickly setup any character that is not using nilotoon
- (Demo) Added 4 new demo model, set up using NiloToon
- (Doc) Added section about "Auto setup this character" button
----------------------------------------------
## [0.3.5] - 2021-8-04

### Added
- (Core) NiloToonCharRenderingControlVolume: +rim light distance fadeout params, you can use it if you want to hide rim light flicker artifact due to not enough pixel count for character(resolution low / character far away from camera)

### Fixed
- (Core) now NiloToon will NOT leave any generated character .fbx in project (in older versions, NiloToon will generate .fbx that has (you can ignore or delete safely) prefix, polluting version control history)
----------------------------------------------
## [0.3.4] - 2021-8-02

### Added
- (Core) character shader: Occlusion section added _OcclusionStrengthIndirectMultiplier
- (Core) character shader: depthtex rim light and shadow width can now optionally controlled by texture and vertex color
- (Doc) added "How to enable character shader’s screen space outline? section

### Changed
- (Core) Now FrameDebugger will show NiloToon's passes correctly (Profiling scope)
- (Core) Now screen space outline can run on mobile platforms(android/iOS)
- (Core) screen space outline now support OpenGLES

### Fixed
- (Core) NiloToonPerCharacterRenderController will now auto re-find allRenderers, if any null renderer is detected in allRenderers list
- (Core) LWGUI will now correctly not saving _ keyword in material, which will also fix screen space outline constantly flicking in scene window unless user's mouse is moving on GUI
- (Core) fix _GlobalIndirectLightMinColor not applied correctly, which ignored occlusion map
----------------------------------------------
## [0.3.3] - 2021-7-19

### Added
- (Core) Environment shader/volume: +_NiloToonGlobalEnviSurfaceColorResultOverrideColor
- (Core) provide a new option in NiloToonAllInOneRendererFeature, "Perfect Culling For Shadow Casters", to fix terrain crash Unity bug
- (Doc) added section for "Perfect Culling For Shadow Casters (how to prevent terrain crash)"

### Changed
- (Core) character shader: rewrite "Ramp texture (specular)" section
- (Core) update SpecularRampTexForEroSkin.psd
----------------------------------------------
## [0.3.2] - 2021-7-11

### Added
- (Core) Improve XR rendering a lot! (by adding correct depth texture rim light and shadow in XR)
- (Core) NiloToonScreenSpaceOutlineControlVolume: add an extra outline width multiplier for XR
- (Core) NiloToonScreenSpaceOutlineControlVolume: add separated control for environment and character
- (Core) NiloToonCharacter shader: _LowSaturationFallbackColor's alpha can now control the intensity of _LowSaturationFallbackColor
- (Core) NiloToonCharacter shader: rewrite specular ramp method
- (Core) NiloToonPerCharacterRenderController: + _PerCharacterOutlineColorLerp
- (Core) NiloToonEnvironmentControlVolume: +_NiloToonGlobalEnviGITintColor , _NiloToonGlobalEnviGIAddColor , _NiloToonGlobalEnviShadowBorderTintColor
- (Demo) NiloToonEnviJPStreetScene now support playing in XR
- (Demo) Some example character models + MatCap(Additive) for metal reflection
- (Demo) Add version number on OnGUI (bottom right of the screen when playing)
- (Demo) Add UI text background transparent black quad, to make OnGUI text easier to read

### Fixed
- (Core) Fixed an environmment shader bug, NiloToonURP can support 2019.4 (URP 7.6.0) now
- (Core) Fixed a bug "After switching platform or reimport character model .fbx assets, baked smooth normal outline data in character model's uv8 will sometimes disappear"
- (Core) Fixed a bug "focusing on NiloToonPerCharacterRenderController is very slow due to wrongly call to AssetDataBase.Refresh()""
- (Core) Fixed a bug "NiloToonPerCharacterRenderController running very slow and allocate huge GC"
----------------------------------------------
## [0.3.1] - 2021-7-05

### Breaking Changes
- (Core) NiloToonCharacter_Shared.hlsl: rename struct LigthingData to ToonLightingData, so now NiloToonURP can also run on Unity2021.1 (URP11) or Unity2021.2 (URP12)
- (Core) NiloToonPerCharacterRenderController.cs: remove useCustomCharacterBoundCenter, now assigning customCharacterBoundCenter transform will treat as enable
- (Core) All screen space outline related feature, will be auto-disabled if running on mobile platform(android / iOS) due to performance and memory impact is high

### Known Issues
- (Core) when inspector is focusing on any NiloToon_Character materials, screen space outline in editor will always keep flickering in scene/game window
- [Done in 0.3.2] (Core) (this bug already exist in 0.2.4) After switching platform or reimport character model assets, baked smooth outline data in character model's uv8 will sometimes disappear. You will need to click on that character's NiloToonPerCharacterRenderController script to trigger a rebake, or click "Windows/NiloToonUrp/Model Label/Re-fix whole project!" button to rebake every character
- (Core) (this bug already exist in 0.2.4) Sometime (you can ignore or delete it safely)xxx.fbx will be generated in project, and not correctly deleted by NiloToonURP automatically    

### Added
- (Core) Added NiloToonEnvironment.shader (Universal Render Pipeline/NiloToon/NiloToon_Environment), you can switch any URP's Lit shader to this shader, they are early stage proof of concept toon shader for environment (Don't use it for production! still WIP/in proof of concept stage, will change a lot in future). Added these envi shader because lots of customers request to include it first(even the shader is far from complete)
- (Core) Added NiloToonEnvironmentControlVolume.cs, to provide extra per volume control for NiloToonEnvironment.shader
- (Core) Added NiloToonScreenSpaceOutlineControlVolume.cs, for controlling NiloToonCharacter.shader and NiloToonEnvironment.shader's screen space outline's settings
- (Core) NiloToonToonOutlinePass.cs added "AllowRenderScreenSpaceOutline" option, you can enable it in NiloToonAllInOne renderer feature if you need to render any screen space outline(default is off)
- (Core) Character shader: Added a WIP and experimental "Ramp Texture (Specular) section"
- (Demo) Add NiloToonEnviJPStreetScene.scene (and all related assets), it is a new scene to demo the new environment shader
- (Doc) Added CustomCharacterBoundCenter section
- (Doc) Added a simple environment shader's document

### Changed
- (Core) now support URP11.0 and URP12.0, because all NiloToon shader renamed struct LightingData to ToonLightingData (solving Shader error in URP11 -> 'Universal Render Pipeline/NiloToon/NiloToon_Character': redefinition of 'LightingData')
- (Core) Character shader: screen space outline(experimental) section completely rewrite (new algorithm, adding depthSensitifity texture, normalsSensitifity, normalsSensitifity texture, extra control for face....)
- (Core) Character shader: screen space outline use multi_compile now(to allow on off by different quality setting using renderer feature)
- (Core) Character shader: rename Outline to Traditional Outline, to better separate it from Screen space outline
- (Core) make screen space outline not affected by RenderScale,screen resolution (scale outline size to match resoltion)
- (Core) global uniform "_GlobalAspectFix" now use camera RT width height as input, instead of screen width height
- (Core) extract screen space outline's code to a new .hlsl (now shared by character shader and environment shader)
- (Core) NiloToonPerCharacterRenderController: optimize shader memory and build time for mobile, but increase GPU shading pressure a bit
- (Core) NiloToonPerCharacterRenderController: optimize C# cpu time spent, and reduce GC 
- (Core) NiloToonPerCharacterRenderController: better warning in Status section, if something not set up correctly 
- (Core) NiloToonEditorShaderStripping.cs: screen space outline will get stripped when building for mobile(android / iOS)
- (Demo) upgrade demo project to Unity 2020.3.12f1
- (Demo) Upgrade demo project to URP 10.5

### Fixed
- (Core) NiloToonEditor_AssetLabelAssetPostProcessor: handle KeyNotFoundException: The given key was not present in the dictionary. when reimporting a model. now will produce a warning message for tracking which model will produce this error
----------------------------------------------
## [0.2.4] - 2021-6-23

### Added
- (Core)NiloToonPerCharacterRenderController: Add "attachmentRendererList" for user to attach any other renderer to the current character, these attachment renderers will use that NiloToonPerCharacterRenderController script's setting(e.g. sync weapon/microphone's perspective removal with a character) 

### Changed
- (Core)NiloToonPerCharacterRenderController: better ToolTip
- (Core)GenericStencilUnlit shader: rewrite to support attachmentRendererList and SRP batching

### Fixed
- (Core)NiloToonPerCharacterRenderController: handle null renderer

----------------------------------------------
## [0.2.3] - 2021-6-21

### Added
- (Core)Character shader: Add "Override Shadow Color by texture" section
- (Core)Character shader: Add "Override Outline Color by texture" section

### Changed
- (Core)Character shader: better material GUI

### Fixed
- (Core)Character shader: GGX specular use float instead of half to avoid precision problem on mobile platform
- GenericStencilUnlit.shader support SRP batching and VR
----------------------------------------------
## [0.2.2] - 2021-6-16

### Changed
- (Core)Character shader: better material GUI's note
### Fixed
- (Core)Character shader: fixed a bug which makes SRP batching not working correctly
- (Core)Character shader: fixed a bug which makes Detail albedo not used by specular amd emission

----------------------------------------------
## [0.2.1] - 2021-6-15

### Breaking Changes
- (Core)(IMPORTANT)Character shader: not using all roughness related settings in "Specular" section anymore, add a new shared "Smoothness" section. This change is made to make NiloToon character shader matches URP Lit.shader's smoothness data convention, and now "Smoothness" section's data can be shared/used by multiple features such as "Environment reflection" and "Specular(GGX)". ***If you are using "Specular" section's roughness setting in your project's materials already, after this update you will have to set up it again in the new "Smoothness" section***
- (Core)(IMPORTANT)Character shader: "Matcap Mask" section merged into "MatCap(alpha blend)" and "MatCap(additive)" section. This change is made due to the old design will force user to combine 2 mask textures into 1 texture, which is not flexible enough. This change will break old materials's "Matcap Mask" section. ***If you are using "MatCap mask" section's settings in your project's materials already, you will have to set up it again in the "MatCap(alpha blend)" and "MatCap(additive)" section's Optional mask setting***
- (Core)rename class NiloToonPerCharacterRenderControllerOverrider to NiloToonCharacterRenderOverrider, to avoid confusion when adding a NiloToonCharacterRenderOverrider script to GameObject
- (Core)rename shader internal MACRO NiloToonIsOutlinePass to NiloToonIsAnyOutlinePass, you can ignore this change if you didn't edit NiloToon's shader code
- (Core)rename shader internal MACRO NiloToonIsColorPass to NiloToonIsAnyLitColorPass, you can ignore this change if you didn't edit NiloToon's shader code

### Added
- (Core)(IMPORTANT): NiloToonAllInOneRendererFeature and NiloToonShadowControlVolume added a new toggle "useMainLightAsCastShadowDirection", you can enable it if you want NiloToon's self shadow system use scene's MainLight direction to cast shadow(same shadow casting direction as regular URP main light shadow, which means shadow result will NOT be affected by camera rotation/movement) 
- (Core)Add GenericStencilUnlit.shader, useful if you want to apply stencil effects that uses drawn character pixels as a stencil mask
- (Core)Character shader: in "Specular" section, add _MultiplyBaseColorToSpecularColor slider, useful if you want to mix base color into specular result
- (Core)Character shader: in "Specular" section, add "Extra Tint by Texture" option, useful if you want to mix any texture into specular result
- (Core)Character shader: in "Emission" section, add _EmissionIntensity, _MultiplyBaseColorToEmissionColor, to allow more Emission color control
- (Core)Character shader: add ColorMask option (RGBA or RGB_), useful if you don't want to pollute RenderTexture's alpha channel for semi-transparent materials
- (Core)Character shader: MatCap(additive) add _MatCapAdditiveMaskMapChannelMask and _MatCapAdditiveMaskMap's remap minmax slider
- (Core)Character shader: MatCap(alpha blend) add _MatCapAlphaBlendMaskMapChannelMask and _MatCapAlphaBlendMaskMap's remap minmax slider
- (Core)Character shader: MatCap(alpha blend) add _MatCapAlphaBlendTintColor and _MatCapAlphaBlendMapAlphaAsMask
- (Core)Character shader: add a new "Environment Reflections" section
- (Core)Character shader: in "Lighting Style" section,added _IndirectLightFlatten, to allow more control on how to display lightprobe result
- (Core)Character shader: in "Outline" section, added _UnityCameraDepthTextureWriteOutlineExtrudedPosition, you can disable it to help removing weird 2D white line artifact on material
- (Core)Character shader: in "Can receive NiloToon Shadow?" section, add _NiloToonSelfShadowIntensityForNonFace(Default 1) and _NiloToonSelfShadowIntensityForFace(Default 0). Face can receive NiloToon's self shadow map now.
- (DEMO)Add MMD4Mecanim folder
- (DEMO)Add some MMD model for testing(only exist in project window)
- (DEMO)Bike Mb1MotorMd000001 add a prefab variant for showing "Environment Reflection" material

### Changed
- (Core)Character shader: hide stencil option (stencil options are not being used in all versions)
- (Core)Sticker shader: use ColorMask RGB now, to not pollute RT's alpha channel
- (Core)NiloToonAnimePostProcessPass set ScriptableRenderPass.renderPassEvent = XXX directly, internally NiloToonAllInOneRendererFeature don't need to create 2 renderpass now 
- (Core)Internal shader code big refactor without changing visual result, if you didn't edit NiloToon's shader source code, you can ignore this change
- (DEMO)Update model materials (IsSkin and Smoothness)
- (DEMO)CRS scene use main light as NiloToon self shadow casting direction (enable useMainLightAsCastShadowDirection in NiloToonSelfShadowVolume)

### Fixed
- (Core)Character shader: fixed a bug where _OcclusionStrength and _GlobalOcclusionStrength is not applied in a correct order
- (Core)Debug window: Fix a bug where NiloToon Debug window always wrongly focus project/scene window
- (Core)Anime postProcess shader: Fix Hidden/NiloToon/AnimePostProcess produce "framgent shader output doesn't have enough component" error in Metal graphics API
----------------------------------------------
## [0.1.3] - 2021-5-19

### Breaking Changes
- (Core)Change namespace of all NiloToonURP C# script to "using NiloToon.NiloToonURP;", please delete your old NiloToonURP folder first before importing the updated NiloToonURP.unitypackage
- (Core)Change shader path of Character shader to "Universal Render Pipeline/NiloToon/NiloToon_Character"
- (Core)Change shader path of Sticker shader to "Universal Render Pipeline/NiloToon/xxx"

### Known Issues
- (Demo)Planar reflection in CRS scene (VR mode) is not correct

### Added
- (Core)Character Shader: Add "IsSkin?" toggle in material, you can enable this toggle if a material is skin(hand/leg/body...), enable it will make the shader use an optional overrided shadow color for skin, optional skin mask can be enabled also if your material is a mix of skin and cloth
- (Core)Character Shader: Add _OverrideByFaceShadowTintColor,_OverrideBySkinShadowTintColor,_OutlineWidthExtraMultiplier
- (Core)Character Shader: Add _ZOffsetEnable to allow on off ZOffset by a toggle
- (Core)Character Shader: Add _EditFinalOutputAlphaEnable to allow on off EditFinalAlphaOuput by a toggle
- (Core)Character Shader: Add _EnableNiloToonSelfShadowMapping to allow on off NiloToonSelfShadowMapping by a toggle
- (Core)Character Shader: Add _NiloToonSelfShadowMappingDepthBias to allow edit NiloToonSelfShadowMapping's depth bias per material
- (Core)Character Shader: Add RGBAverage and RGBLuminance to RGBAChannelMaskToVec4Drawer, you will see it if you click the RGBA channel drop list in material UI
- (Core)Sticker Shader: Add override alpha by a texture (optional)
- (Core)Correctly support Unity 2019.4.0f1 or above (need to use URP 7.4.1 or above, NOT URP 7.3.1, you can upgrade URP to 7.4.1 in the package manager)
- (Core)Add NiloToonPlanarReflectionHelper.cs, for planar reflection camera support, you need to call NiloToonPlanarReflectionHelper.cs's function in C# when rendering your planar reflection camera (see MirrorReflection.cs in CRS demo scene), user document has a new section about it, see "When rendering NiloToon shader in planar reflection camera, some part of the model disappeared"
- (Core)Add NiloToonPerCharacterRenderControllerOverrider.cs, to sync perspective removal result from a source to a group of characters
- (Core)Add SimpleLit debug option in NiloToon debug window
- (Core)NiloToonAllInOneRendererFeature: Add useNdotLFix to hide self shadowmap artifact, you can turn it off if you don't like it
- (Core)NiloToonCharRenderingControlVolume: + depthTextureRimLightAndShadowWidthMultiplier
- (Core)NiloToonPerCharacterRenderController: + perCharacterOutlineWidthMultiply , perCharacterOutlineColorTint
- (Demo)Add CRS(Candy rock star) demo scene, with a planar reflection script(MirrorReflection.cs)
- (Demo)Add more demo models
- (Demo)Add .vrm file auto prefab generation editor script, inside ThirdParty(VRM) folder. You can drag .vrm files into demo project and prefab will be generated
- (Demo)Add 4ExtremeHighQuality and 5HighestQuality quality settings, PC build now default use 4ExtremeHighQuality
- (Doc)Add section for how to correctly use NiloToonURP in 2019.4 (need to enable URP's depth texture manually and install URP 7.4.1 or above) 
- (Doc)Add section for setting up semi-transparent alpha blending material
- (Doc)Add section for changing VRM(MToon)/RealToon material to NiloToon material

### Changed
- (Core)remove _vertex and _fragment suffixes in multi_compile and shader_compile, in order to support 2019.4 correctly
- (Core)Character Shader: Now the minimum supported OpenGLES version is 3.1, not 3.0, due to support SRP Batching correctly
- (Core)package.json: Now the minimum supported URP version is 7.4.1, not 7.6.0
- (Core)package.json: Now the minimum supported editor version is 2019.4.0f1, not 2019.4.25f1
- (Core)NiloToonPerCharacterRenderController.cs: now auto disable perspective removal in XR
- (Core)Smooth normal editor baking: Will skip baking if model don't have correct tangent, and better import error message if model don't have tangent data
- (Core)if _ZWrite = 0, disable all _CameraDepthTexture related effects (e.g.auto disable depth texture 2D rim light of a ZOffset enabled eyebrow material)
- (Core)now use global keyword SHOULD_STRIP_FORCE_MINIMUM_SHADER in demo's enable NiloToon toggle
- (Demo)change demo script to use namespace using NiloToon.NiloToonURP;
- (Demo)now limit Screen height to maximum 1080, to increase fps in 4k monitors
- (Demo)optimize some model's texture max resolution for android, to avoid using too much memory in .apk
- (Doc)improve user document with more FAQ

### Fixed
- (Core)fix SRP batcher mode linear gamma bug
- (Core)fixed NiloToonCharacterSticker shaders return alpha not equals 1 problem, these shader will not pollute RT's alpha channel anymore (always return alpha == 1) 
- (Core)fixed some shader and C# warning (no harm)
- (Core)fixed a bug that make SRP batcher not working (add _PerCharacterBaseColorTint in NiloToonCharacter.shader's Properties section)
----------------------------------------------
## [0.1.2] - 2021-5-3

### Added
- (Core)Add NiloToonShadowControlVolume for Volume, you can use it to control and override shadow settings per volume instead of editing NiloToonAllInOneRendererFeature directly
- (Core)All shaders add basic XR support, but many shader features are now auto disabled in XR in this version temporarily, because we are fixing them
- (Core)In XR, due to high fov, outline width is default 50% (only in XR), you can change this number in NiloToonCharRenderingControlVolume or NiloToonAllInOneRendererFeature
- (Core)character shader: Add DepthNormal pass, URP's SSAO renderer feature(DepthNormals mode) will work correctly now
- (Core)character shader: Add "screen space outline" feature in material (experimental, need URP's _CameraDepthTexture enabled), enable it in material UI will add more detail outline to character, useful for alphaclip materials where the default outline looks bad
- (Core)character shader: Add "Dynamic eye" feature in material, for users who need circular dynamic eye pupil control
- (Core)character shader: Add VertExmotion support, you can enable it in NiloToonCharacter_ExtendDefinesForExternalAsset.hlsl
- (Core)character shader: Add _ZOffsetMaskMapChannelMask,_ExtraThickOutlineMaxFinalWidth,_DepthTexShadowThresholdOffset,_DepthTexShadowFadeoutRange,_GlobalMainLightURPShadowAsDirectResultTintColor in material
- (Core)NiloToonAnimePostProcessVolume: add anime postprocess effect draw height and draw timing control
- (Core)Add per character and per volume extra BaseColor tint
- (Core)Can install from PackageManager(install from disk) using NiloToonURP folder's package.json file
- (Core)Add auto reimport and message box when using NiloToonURP the first time
- (Demo)All demo shader add XR support
- (Demo)All scene add steam XR support, see user document pdf for instruction on how to try it in editor play mode (steam PCVR)
- (Demo)Add NiloToonDemoDanceScene_UI.cs, adding more user control in NiloToonDancingDemo.unity

### Changed
- (Core)NiloToonAnimePostProcess: Reduce default top light intensity from 100% to 75%
- (Core)improve UI display and tooltips
- (Demo)player settings remove Vulkan API for Android, now always use OpenGLES3 to support testing on  more devices
- (Demo)UnityChan edit skin material to have better shadow color(in 0.1.1 shadow color is too dirty and grey) 
- (Doc)improve user document with more FAQ

### Fixed
- (Core) Fixed bake smooth normal UV8 index out of bound exception if .fbx has no tangent, will now produce error log only
- (Core) Fixed NoV rimlight V matrix not corerct bug (now use UNITY_MATRIX_V instead of unity_CameraToWorld)
- (Core) Fixed a missing abs() bug in NiloZOffsetUtil.hlsl
- (Demo) Fixed multi AdvanceFPS script in scene bug
----------------------------------------------
## [0.1.1] - 2021-4-19

### Added
- (Core)Add change log file (this file)
- (Core)character shader: Add "MatCap (blend,add and mask)" section, which mix MatCap textures to Base map
- (Core)character shader: Add "Ramp lighting texture" section, which override most lighting params by a ramp texture
- (Core)character shader: Add "Final output alpha" section, to allow user render opaque character to custom RT without alpha problem
- (Core)character shader: "Detail Maps" section add uv2 toggle and albedo texture white point slider
- (Core)character shader: "Calculate Shadow Color" section add _FaceShadowTintColor
- (Core)character shader: "Calculate Shadow Color" section add _LitToShadowTransitionAreaTintColor, LitToShadowTransitionArea HSV edit 
- (Core)character shader: "Outline" section add _OutlineOcclusionAreaTintColor, _OutlineReplaceColor
- (Core)character shader: "Depth texture shadow" section add _DepthTexShadowUsage
- (Core)character shader: NiloToonCharacter_ExtendFunctionsForUserCustomLogic.hlsl add more empty functions for user
- (Core)character shader: Add support to VertExmotion asset (no change to NiloToon shader name) (add NiloToonCharacter_ExtendDefinesForExternalAsset.hlsl for user to enable VertExmotion support if they need it)
- (Core)volume: NiloToonCharRenderingControlVolume.cs add Directional Light, Additional Light, Specular's volume control to allow better control light intensity on character
- (Core)character root script: NiloToonPerCharacterRenderController.cs Add perCharacterDesaturation 
- (Core)character root script: NiloToonPerCharacterRenderController.cs Add extra think outline view space pos offset, usually for stylized color 2D drop shadow
- (Demo)Add GBVS Narmaya model and setup her using NiloToon (face material not ready for 360 light rotation)
- (Demo)Add NiloToonCutinScene.unity to show GBVS Narmaya model
- (Demo)Add bike (using NiloToonURP) in NiloToonSampleScene.unity
- (Demo)Include XR related files, preparing for future XR support
- (Demo)Add 4HighestQuality, it will now ignore performance limit, using the best possible setting (only for PC build, WebGL and editor)

### Changed
- (Core)NiloToonAllInOneRendererFeature: Unlock self shadow's shadow map resolution limit from 4096 to 8192
- (Core)NiloToonAverageShadowTestRT.shader: add sampling pos offset to prevent over generating average shadow due to near by objects/characters
- (Core)revert indirectLightMultiplier's default setting from 2 to 1
- (Demo)Upgraded dmeo project to 2020.3.4f1
- (Demo)Optimize 0~3 quality settings for mobile, while allow best quality(4) for PC/Editor/WebGL
- (Demo)Allow every scene to switch to another in play mode, no matter user start playing in which scene
- (Demo)Refactor and rename some files and folder

### Fixed
- (Core)character root script: NiloToonPerCharacterRenderController.cs fix can't edit material in pause play mode bug

### Know Issues
- (Core)Depth texture rim light is not correct in PCVR Editor
- (Core)Shader variant(memory usage) too much in mobile build
- (Core)Outline render incorrectly if camera is too close
- [DONE in 0.3.2] (Core)per character scirpt's editor script is too slow 
- (Demo)Switching scene in editor play mode takes a very long time, due to editor script

### TODO
- (Core)Finish detail document on material properties, NiloToon UI params
- (Core)Charcter shader can't receive point or spot light shadow (need to support URP11 first, since point light shadow only exist in URP11)
- [DONE in 0.1.2] (Core)depth texture sobel outline shader_feature
- [DONE in 0.1.2] (Core)control URP shadow intensity remove directional light contribution (as volume)
- (Demo)Add moving tree shadowmap demo scene
- (Demo)GBVS Narmaya's face material not yet setup correctly to support all light direction
----------------------------------------------
## [0.0.3] - 2021-4-06
### Added
- (Demo)Add MaGirlHeadJsonTransformSetter.cs
- (Demo)Add random face & ear animation for MaGirl using MaGirlHeadJsonTransformSetter.cs 
----------------------------------------------
## [0.0.2] - 2021-4-03
### Added
- (Demo)Add MaGirl3.prefab and it's variants, setup her using NiloToon
- (Demo)Add MaGirl2.prefab and it's variants, setup her using NiloToon
- (Demo)Add NiloToonDancingDemo.unity scene
----------------------------------------------
## [0.0.1] - 2021-3-28
### Added
- (Core)First version to record in change log.
- (Demo)First version to record in change log.