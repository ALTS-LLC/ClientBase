// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

/*
[Extend this shader]
You can extend this shader by writing additional code inside "NiloToonCharacter_ExtendFunctionsForUserCustomLogic.hlsl",
without worrying about merge conflict in future updates, 
because "NiloToonCharacter_ExtendFunctionsForUserCustomLogic.hlsl" 
is just an almost empty .hlsl file with empty functions for you to fill in extra code.
You can use those empty functions to apply your global effect, similar to character-only postprocess (e.g. add fog of war/scan line...).
If you need us to expose more empty functions at another shading timing, please contact nilotoon@gmail.com

If you really need to edit the shader, usually just by editing "NiloToonCharacter_LightingEquation.hlsl" alone can control most of the visual result,
while editing this .shader file directly may make your future update(merge) difficult.

[All Passes of this .shader]
This shader includes 9 passes, each pass will be activated if needed:
0.ForwardLit                        (LightMode:UniversalForwardOnly)                    (same as URP Lit shader, always render, a regular color pass that will always render to URP's _CameraColorTexture RT)
1.Outline                           (LightMode:NiloToonOutline)                         (only render if user turn it on in NiloToonAllInOneRendererFeature(default on). If render is needed, this pass will always render to URP's _CameraColorTexture RT)
2.CharacterAreaStencilBufferFill    (LightMode:NiloToonCharacterAreaStencilBufferFill)  (only render if user turn "Extra Thick Outline" on in NiloToonPerCharacterRenderController(default off). If render is needed, this pass will always render to URP's _CameraColorTexture RT)
3.ExtraThickOutline                 (LightMode:NiloToonExtraThickOutline)               (only render if user turn "Extra Thick Outline" on in NiloToonPerCharacterRenderController(default off). If render is needed, this pass will always render to URP's _CameraColorTexture RT)
4.ShadowCaster                      (LightMode:ShadowCaster)                            (same as URP Lit shader, only for rendering to URP's shadow map RT's depth buffer, this pass won't be used if all your characters don't cast URP shadow)
5.DepthOnly                         (LightMode:DepthOnly)                               (same as URP Lit shader, only for rendering to URP's depth texture RT _CameraDepthTexture's depth buffer, this pass won't be used if your project don't render URP's offscreen depth prepass)
6.DepthNormalsOnly                  (LightMode:DepthNormalsOnly)                        (same as URP Lit shader, only for rendering to URP's normal texture RT _CameraNormalsTexture's rendering (color+depth), this pass won't be used if your project don't render URP's offscreen depth normal prepass)
7.NiloToonSelfShadowCaster          (LightMode:NiloToonSelfShadowCaster)                (only for rendering NiloToon's _NiloToonCharSelfShadowMapRT's depth buffer, this pass won't be used if your project don't enable NiloToon's character self shadow in NiloToonAllInOneRendererFeature)
8.NiloToonPrepassBuffer             (LightMode:NiloToonPrepassBuffer)                   (Only for rendering NiloToon's _NiloToonPrepassBufferRT)

[SRP Batcher]
Because most of the time, user use NiloToon's character shader for unique characters or dynamic objects like character weapons, so all lightmap-related code is removed for simplicity.
For batching, we only rely on SRP batcher, which is the most practical batching method in URP for rendering lots of unique material + SkinnedMeshRenderer(unique mesh) characters.
The followiing are 4 possible batching method, and only SRP Batcher is useful.
- (X)Static batching (characters are dynamic)
- (X)Dynamic batching (characters have many vertices)
- (X)GPU instancing (characters have unique animation(unique mesh))
- (O)SRP Batcher

[GPU Instancing]
GPU instancing is not included (due to adding more multi_compile for not much gain), 
because in most cases, we don't need GPU instancing unless you are rendering lots of lowpoly characters with the same material + same animation(same mesh) or need to support VR's Single Pass Stereo rendering.

[If() performance]
In this shader, sometimes we choose "conditional move (a?b:c)" or "static uniform branching (if(_Uniform))" over "shader_feature & multi_compile" for some of the togglable ALU only(pure calculation) features, 
because:
    - we want to avoid this shader's build time takes too long (2^n)
    - we want to avoid shader size and memory usage becomes too large easily (2^n), 2GB memory iOS mobile will crash easily if you use too much multi_compile
    - we want to avoid rendering spike/hiccup when a new shader variant was seen by the camera first time ("create GPU program" in profiler, when the shader compile at the first time)
    - we want to avoid increasing ShaderVarientCollection's keyword combination complexity
    - we want to avoid breaking SRP batcher's batching because SRP batcher is per shader variant batching, not per shader

    All modern GPU(include the latest high-end mobile devices since 2022) can handle "static uniform branching" with reasonable performance cost (if Register Pressure or Instruction Cache Pressure is not the bottleneck).
    Usually, there exist 4 cases of branching, here we sorted them by cost, from lowest cost to highest cost,
    and you usually only need to worry about the "case 4" only!

    case 1 - compile time constant if():
        // absolutely 0 performance cost for any platform, 
        // - if condition is false, unity's shader compiler will treat the whole if() block as dead code and remove everything completely
        // - if condition is true, unity's shader compiler will simply remove the if() line, keeping only the logic body
        // shader compiler is very good at dead code removal for any compile time constant code
        #define SHOULD_RUN_FANCY_CODE 0
        if(SHOULD_RUN_FANCY_CODE) {...}

        #define A_CONSTANT_NUMBER 7
        if(A_CONSTANT_NUMBER + 3 < 10) {...}
        if(A_CONSTANT_NUMBER + 24 > 1) {...}

    case 2 - static uniform branching if():
        // Reasonable cost, quite fast! (except OpenGLES2, OpenGLES2 doesn't have branching and will run both paths and discard the false path)
        // since OpenGLES2 is not the main focus anymore in 2024, we will ignore OpenGLES2 and use static uniform branching if() in NiloToonURP when suitable
        CBUFFER_START(UnityPerMaterial)
            // usually controlled by a [Toggle] in material inspector, material.SetFloat() or Shader.SetGlobalFloat() in C#
            float _ShouldRunFancyCode; 
            float _ShouldRunSuperFancyCode;
        CBUFFER_END
        if(_ShouldRunFancyCode) {...}
        if(!_ShouldRunFancyCode) {...}
        if(_ShouldRunFancyCode && _ShouldRunSuperFancyCode) {...}
        if(!_ShouldRunFancyCode && !_ShouldRunSuperFancyCode) {...}

    case 3 - dynamic branching if() without divergence inside a warp(NVIDIA)/wavefront(AMD):
        // all pixels inside a warp/wavefront(imagine it is a group of 8x4=32(NVIDIA) pixels or 8x8=64(AMD) pixels) all goes into the same path, it is a "no divergence" case.
        // it is still fast, but not as fast as case (2)
        bool shouldRunFancyCode = (any non-uniform shader calculation that involve texture samples, vertex data via Varyings struct.....);
        if(shouldRunFancyCode) {...}

    case 4 - dynamic branching if() WITH divergence inside a warp/wavefront: 
        // this is the only case that will make GPU slow! You will want to avoid it as much as possible (e.g., when shouldRunFancyCode is some random white noise(0/1) on screen, it is likely a divergence inside a warp/wavefront)
        bool shouldRunFancyCode = (some shader calculation); // pixels inside a warp/wavefront goes into a different path, even it is 63 vs 1 within a 64 thread group, it is still a "divergence" case!
        if(shouldRunFancyCode) {...} 

    If you want to understand the difference between case 1-4,
    Here are some resources about the cost of if() / branching / divergence in shader:
    - https://stackoverflow.com/questions/37827216/do-conditional-statements-slow-down-shaders
    - https://stackoverflow.com/questions/5340237/how-much-performance-do-conditionals-and-unused-samplers-textures-add-to-sm2-3-p/5362006#5362006
    - https://twitter.com/bgolus/status/1235351597816795136
    - https://twitter.com/bgolus/status/1235254923819802626?s=20
    - https://www.shadertoy.com/view/wlsGDl?fbclid=IwAR1ByDhQBck8VO0AMPS5XpbtBPSzSN9Mh8clW4itRgDIpy5ROcXW1Iyf86g
    - https://medium.com/@jasonbooth_86226/branching-on-a-gpu-18bfc83694f2
    - https://medium.com/@jasonbooth_86226/stalling-a-gpu-7faac66b11b9
    - https://forum.unity.com/threads/branching-in-shaders.1231695/

    [TLDR] 
    Just remember(even for mobile platform): 
    - if() itself is not evil, you CAN use it if you know there is no divergence inside a warp/wavefront, still, it is not free on mobile(that's why shader_feature/multi_compile are still the popular choice to skip things in hlsl).
    - "a ? b : c" is just a conditional move(movc / cmov) in assembly code, don't worry using it if you have calculated b and c already
    - Don't try to optimize if() or "a ? b : c" by replacing them by lerp(b,c,step())..., because "a ? b : c" is always faster if you have calculated b and c already
    - branching is not evil, still it is not free. Sometimes we can use branching to help GPU run faster if the skipped task is heavy! (e.g, skipping a big for loop inside an if(){...heavy loop...})
    - but, divergence is evil! If you want to use if(condition){...}else{...}, make sure the "condition" is the same within as many groups of 32 or 64 pixels as possible

    [Note from the developer (1)]
    Using shader permutation(multi_compile/shader_feature) is still the fastest way to skip shader calculation,
    because once the code doesn't exist, it will enable many compiler optimizations. 
    If you need the best GPU performance, and you can accept long build time and huge runtime shader memory usage, you can use multi_compile/shader_feature more.

    NiloToon's character shader will always prefer shader permutation if it can skip any texture read or sampler, 
    because GPU hardware has very strong ALU(pure calculation/math) power growth since 2015 (including mobile), 
    but relatively weak growth in memory bandwidth(usually means buffer/texture read).
    (https://community.arm.com/developer/tools-software/graphics/b/blog/posts/moving-mobile-graphics#siggraph2015)

    And when GPU is waiting for receiving texture fetch, it won't become idle, 
    GPU will still continue any other available ALU work(latency hiding) until there is 100% nothing to calculate anymore (when shader is stall due to waiting texture read, look for keyword "long scoreboard" in Nsight's shader profiler, 
    also bandwidth is the biggest source of heat generation (especially on mobile without active cooling = easier overheat/thermal throttling). 
    So we should try our best to keep memory bandwidth usage low (since more ALU(math) is ok, but more texture/buffer/off-chip/cache miss memory read is not ok),
    the easiest way is to remove the texture read hlsl code using shader permutation.

    But if the code is ALU only(pure calculation/math), and calculation is simple on both paths on the if & else side, NiloToonURP will prefer "a ? b : c". 
    The rest will be:
    - static uniform branching (usually means heavy ALU only code inside an if(_Uniform))
    - or dynamic branching (try to reduce divergency as much as possible) 

    [Note from the developer (2)]
    If you are working on a game project, not a generic tool like NiloToonURP, you will always want to pack 
    - 4data (e.g., occlusion/specular/smoothness/metallic/any mask.....) into 1 RGBA texture(for fragment shader), 
    - 4data (outlineWidth/ZOffset/face area mask....) into another RGBA texture or even vertex color, 
    to reduce the number of texture read without changing visual result(if we ignore texture compression artifacts).
    But since NiloToonURP is a generic tool that is used by different person/team/company, 
    we know it is VERY important for all users to be able to apply this shader to any model easily/fast/without effort,
    and we know that it is almost not practical if we force regular user to pack their texture into a special format just for this shader,
    so we decided we will keep every texture separated, even it is VERY slow compared to the packed texture method.
    That is a sad decision in terms of performance, but a good decision for ease of use. 
    If user don't need the best performance, this decision is actually a plus to them since it is much more flexible when using this shader.  

    [About multi_compile or shader_feature's _vertex and _fragment suffixes]
    In unity 2020.3, unity added _vertex, _fragment suffixes to multi_compile and shader_feature
    https://docs.unity3d.com/2020.3/Documentation/Manual/SL-MultipleProgramVariants.html (Using stage-specific keyword directives)
    
   
  
    The only disadvantage of NOT using _vertex and _fragment suffixes is only compilation time, not build size/memory usage:
    https://docs.unity3d.com/2020.3/Documentation/Manual/SL-MultipleProgramVariants.html (Stage-specific keyword directives)
    "Unity identifies and removes duplicates afterwards, so this redundant work does not affect build sizes or runtime performance; 
    however, if you have a lot of stages and/or variants, the time wasted during shader compilation can be significant."

    ---------------------------------------------------------------------------
    More information about mobile GPU optimization can be found here, most of the best practice can apply both GPU(Mali & Adreno):
    https://developer.arm.com/solutions/graphics-and-gaming/arm-mali-gpu-training

[Shader build time and memory]
https://blog.unity.com/engine-platform/2021-lts-improvements-to-shader-build-times-and-memory-usage

[Support SinglePassInstancing]
https://docs.unity3d.com/2022.2/Documentation/Manual/SinglePassInstancing.html

[Conditionals can affect #pragma directives]
preprocessor conditionals can be used to influence, which #pragma directives are selected.
https://forum.unity.com/threads/new-shader-preprocessor.790328/
https://docs.unity3d.com/Manual/shader-variant-stripping.html
Example code:
{
    #ifdef SHADER_API_DESKTOP
        #pragma multi_compile _ RED GREEN BLUE WHITE
    #else
       #pragma shader_feature RED GREEN BLUE WHITE
    #endif
}
{
    #if SHADER_API_DESKTOP
        #pragma geometry ForwardPassGeometry
    #endif
}
{
    #if SHADER_API_DESKTOP
        #pragma vertex DesktopVert
    #else
        #pragma vertex MobileVert
    #endif
}
{
    #if SHADER_API_DESKTOP
       #pragma multi_compile SHADOWS_LOW SHADOWS_HIGH
       #pragma multi_compile REFLECTIONS_LOW REFLECTIONS_HIGH
       #pragma multi_compile CAUSTICS_LOW CAUSTICS_HIGH
    #elif SHADER_API_MOBILE
       #pragma multi_compile QUALITY_LOW QUALITY_HIGH
       #pragma shader_feature CAUSTICS // Uses shader_feature, so Unity strips variants that use CAUSTICS if there are no Materials that use the keyword at build time.
    #endif
}
But this will not work (Keywords coming from pragmas (shader_feature, multi_compile and variations) will not affect other pragmas.):
{
    #pragma shader_feature WIREFRAME_MODE_ON
 
    #ifdef WIREFRAME_MODE_ON
        #pragma geometry ForwardPassGeometry
    #endif
}

[Write .shader and .hlsl using an IDE]
Rider is the best IDE for writing shader in Unity, there should be no other better tool than Rider for editing .shader / .hlsl files.
If you never used Rider to write hlsl before, we highly recommend trying it for a month for free.
https://www.jetbrains.com/rider/

[hlsl code is inactive in Rider]  
You may encounter an issue that some hlsl code is inactive within the #if #endif section, so Rider's "auto complete" and "systax highlightd" is not active, 
see NiloToonCharacter_RiderSupport.hlsl for how to solve it

[shader profiling in PC NVIDIA Nsight]
1. search [NVIDIA Nsight shader profiling] in this .shader, enable 2 #pragma lines
2. use DX12 to build a PC build
3. use Nsight to launch that PC build with Nsight's frame debugger
4. capture a frame 
5. start shader profiler, click "hot spot"
then you can see the cost(stall reason) of this shader for each used hlsl function from NiloToon/URP/Core's hlsl
*/ 
Shader "Universal Render Pipeline/NiloToon/NiloToon_Character"
{
    Properties
    {
        // [about naming]
        // All properties will try to follow URP Lit shader's naming convention if possible,
        // so switching your URP lit material's shader to NiloToon shader will preserve most of the original properties if defined in this shader.
        // For URP Lit shader's naming convention, see URP's Lit.shader (search "Lit t:shader" in the project window, use "Search: In Packages")

        // [about HDR color picker]
        // All color properties in this shader are HDR, because we don't want to limit user's creativity, even if HDR color makes no sense at all.
        // If user want to make a 100% non-sense color choice, for example, an emissive shadow, just let them do it! 
        // because why not? It is NPR, unlike PBR, there is no right or wrong, everything is permitted and valid if it looks good.
        // *Adding [HDR] will force unity to treat the color as linear, be careful.
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Group UI Display Mode
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Group UI Display)]
        [Tooltip(To show users the most commonly used groups by default, some less commonly used groups will only be displayed in certain mode or above.)]
        [Tooltip(This option only affects how many groups you see in this Editor material inspector, it will not affect any rendering.)]
        [Tooltip(If you enabled a group, and hide it using this option, that group will still render.)]
        [Tooltip()]
        [Tooltip(The following groups will only show in Normal or above,)]
        //[Tooltip(.    Base Map Stacking Layer 2)]
        //[Tooltip(.    Base Map Stacking Layer 3)]
        [Tooltip(.    MatCap (Color Replace))]
        [Tooltip(.    Normal Map)]
        [Tooltip(.    Detail Map)]
        //[Tooltip(.    Lighting Style)]
        //[Tooltip(.    Lighting Style (face))]
        [Tooltip(.    Occlusion Map)]
        [Tooltip(.    MatCap (Shadow))]
        //[Tooltip(.    MatCap (Add|Specular|RimLight))]
        [Tooltip(.    Smoothness Roughness)]
        [Tooltip(.    Specular Highlights)]
        [Tooltip()]
        [Tooltip(The following groups will only show in Expert or above,)]
        [Tooltip(.    Color Buffer)]
        [Tooltip(.    Depth Buffer)]
        [Tooltip(.    Parallax Map)]
        [Tooltip(.    Face Shadow Gradient Map (SDF))]
        [Tooltip(.    Ramp Map (Diffuse Lighting))]
        [Tooltip(.    Environment Reflections)]
        [Tooltip(.    Allowed Pass)]
        [Tooltip()]
        [Tooltip(The following groups will only show in ShowAll mode,)]
        [Tooltip(.    Shading Grade Map)]
        [Tooltip(.    Receive URP ShadowMap)]
        [Tooltip(.    Kajiya Kay Specular (hair))]
        [Tooltip(.    Screen Space Outline)]
        [Tooltip(.    Override Outline Color)]
        [Tooltip(.    Allow Per Character effect)]
        [Tooltip(.    Allow Nilo Bloom Override)]
        [Tooltip(.    Support Cloth Dynamics)]
        [Enum(Basic,0,Normal,100,Expert,200,ShowAll,10000)]_UIDisplayMode("Mode", Float) = 100
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Surface Type Preset
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Top Level Control)]

        [Tooltip(This is the first option that you want to edit.)]
        [Tooltip(This correlates to the Surface Type in the URP Lit shader.)]
        [Tooltip()]
        [Tooltip(There are 4 first level options,)]
        [Tooltip(.     Opaque means the same as URP Lit shader Opaque.)]
        [Tooltip(.     Opaque(Alpha) means Opaque(but with alpha blending supported). (RenderQueue 2499))]
        [Tooltip(.     Transparent means the same as URP Lit shader Transparent. (RenderQueue 3000))]
        [Tooltip(.     Transparent(ZWrite) means Transparent(but enabled ZWrite and allow Outline). (RenderQueue 3000))]
        [Tooltip()]
        
        [Tooltip(After selecting a preset, the following properties will update their current and default value to match the selected preset.)]
        [Tooltip()]
        [Tooltip(.     _SrcBlend)]
        [Tooltip(.     _DstBlend)]
        [Tooltip(.     _ZWrite)]
        [Tooltip(.     _AlphaClip)]
        [Tooltip(.     _RenderOutline)]
        [Tooltip(.     renderqueue)]
        [Tooltip()]
        [Tooltip(For details about all properties that this preset controls, you can search NiloToonCharacter_SurfaceType_LWGUI_ShaderPropertyPreset inside NiloToonURP folder.)]
        [Preset(NiloToonCharacter_SurfaceType_LWGUI_ShaderPropertyPreset)] _SurfaceTypePreset ("Surface Type", float) = 0 

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Rendering On Off
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Tooltip(.     If enabled, this material will render as usual.)]
        [Tooltip(.     If disabled, this material will still render, but it will not affect any render results (e.g., it becomes invisible).)]
        [Tooltip()]
        [Tooltip(Also, you can turn this toggle on and off to quickly identify where this material is located on a character.)]
        [SubToggle(,_)]_EnableRendering("Render material?", Float) = 1

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Base Map and Color
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Base Map and Color)]
        
        [Tooltip(This color will be multiplied to Base Map (RGBA multiply).)]
        [Tooltip(You can use this color to tint color(RGB) and reduce alpha(A) of Base Map.)]
        [HDR][MainColor]_BaseColor("     Base Color", Color) = (1,1,1,1)
        
        [AdvancedHeaderProperty]
        [Tooltip(The MainTexture of this material,)]
        [Tooltip(.    RGB channel affects color)]
        [Tooltip(.    A channel affects alpha)]
        [Tooltip()]
        [Tooltip(a.k.a Albedo texture.)]
        [MainTexture]_BaseMap("Base Map", 2D) = "white" {} // Not using [Tex], in order to preserve tiling and offset GUI
        
        [Advanced]
        [Tooltip(Select which UV channel should be used to sample Base Map.)]
        [Tooltip()]
        [Tooltip(In most cases, you can keep it as default value UV0, unless your mesh has multiple UV groups.)]
        [Enum(UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapUVIndex("     UV", Float) = 0

        [Advanced]
        [Tooltip(This color will also be multiplied to Base Map (RGBA multiply).)]
        [Tooltip(You can use this color to tint color(RGB) and reduce alpha(A) of Base Map.)]
        [Tooltip()]
        [Tooltip(This is an additional control for convenience, it has the same function as Base Color.)]
        [Tooltip()]
        [Tooltip(For example, you can use this for animation keyframe, so animation will not affect the original Base Color.)]
        [HDR]_BaseColor2("     Extra tint", Color) = (1,1,1,1)
        
        [Advanced]
        _BaseMapBrightness("     Brightness", Range(0,10)) = 1
        
        [Title(Built in RP Color (VRM))]
        [Tooltip(Enable this option if you want UniVRM Blend Shape Proxy...Blend Shape Clip...Material List...Color control still works in this shader.)]
        [Tooltip()]
        [Tooltip(We added this option so user can still use VRM Blend Shape Proxy as usual after converting to NiloToon without editing any blend shape clips.)]
        [Tooltip()]
        [Tooltip(For example, blend shape clips may use alpha of this Color to control the material on off state.)]
        [SubToggle(,_)]_MultiplyBRPColor("     Multiply Built-in RP _Color?", Float) = 0
        [ShowIf(_MultiplyBRPColor, Equal, 1)]
        _Color("         _Color", Color) = (1,1,1,1)

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Core Settings)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Is Skin?
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // note: 
        // no shader_feature keyword is used for _IsSkin, 
        // since the increase in ALU and register pressure is very small in Skin section, 
        // not worth adding a new shader_feature.
        [Tooltip(We recommend turning this on for any skin materials,)]
        [Tooltip(so the skin area can automatically use a better overridden tint color for shadows and outlines.)]
        [Tooltip()]
        [Tooltip(You can search for (skin) in the search bar while in property mode to find settings related to the skin.)]
        [Main(_IsSkinGroup,_)]_IsSkin("Is Skin?", Float) = 0 

        [Helpbox(Please enable IsSkin group for skin materials.)]
        [Helpbox()]
        [Helpbox(.    It will override the shadow color of skin and allow you to edit the skin shadow color easily in Shadow Color group.)]

        [Tooltip(If this material contains skin and other non skin parts together,)]
        [Tooltip(you can enable this toggle to make this group masked by Mask Map.)]
        [SubToggle(_IsSkinGroup,_SKIN_MASK_ON)]_UseSkinMaskMap("Enable Mask?", Float) = 0

        [ShowIf(_UseSkinMaskMap, Equal, 1)]
        [AdvancedHeaderProperty]
        [Tooltip(Shader will convert Mask Map to a grayscale mask, so you need to select a texture channel or a convert method.)]
        [Tooltip()]
        [Tooltip(The converted grayscale mask value should be,)]
        [Tooltip(.    White for Skin area.)]
        [Tooltip(.    Black for Non Skin area.)]
        [Tooltip()]
        [Tooltip(If no texture is assigned, it will use a default White texture.)]
        [Tooltip()]
        [Tooltip(Use G as the default texture read value because if the Mask Map is an RGB grayscale texture, the G channel provides better quality with some compression formats.)]
        [Tex(_IsSkinGroup,_SkinMaskMapChannelMask)][NoScaleOffset]_SkinMaskMap("Mask Map", 2D) = "white" {}
        [Advanced][HideInInspector]_SkinMaskMapChannelMask("", Vector) = (0,1,0,0)

        [Advanced]
        [SubToggle(_IsSkinGroup, _)]_SkinMaskMapAsIDMap("               Extract from ID?", Float) = 0
        [Advanced]
        [ShowIf(_SkinMaskMapAsIDMap, Equal, 1)]
        [SubIntRange(_IsSkinGroup)]_SkinMaskMapExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [Advanced]
        [Tooltip(If your Mask Map is painted in an inverted way,)]
        [Tooltip(.    Painted White in Non Skin area)]
        [Tooltip(.    Painted Black in Skin area)]
        [Tooltip(In this situation, you can enable this toggle, which will invert the mask value inside the shader, so you do not have to spend time editing the texture in an external software like PhotoShop.)]
        [SubToggle(_IsSkinGroup, _)]_SkinMaskMapInvertColor("               Invert?", Float) = 0

        [Advanced]
        [Tooltip(Apply a linear value remap to pixels of Mask Map.)]
        [Tooltip()]
        [Tooltip(For scripting,)] 
        [Tooltip()]
        [Tooltip(you should call Material.SetFloat())]
        [Tooltip(.    _SkinMaskMapRemapStart)]
        [Tooltip(.    _SkinMaskMapRemapEnd)]
        [Tooltip()]
        [Tooltip(You should NOT call Material.SetFloat())]
        [Tooltip(.    _SkinMaskMapRemapMinMaxSlider)]
        [MinMaxSlider(_IsSkinGroup,_SkinMaskMapRemapStart,_SkinMaskMapRemapEnd)]_SkinMaskMapRemapMinMaxSlider("               Remap", Range(0,1)) = 1
        [Advanced]
        [HideInInspector]_SkinMaskMapRemapStart("", Range(0,1)) = 0
        [Advanced]
        [HideInInspector]_SkinMaskMapRemapEnd("", Range(0,1)) = 1
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Is Face?
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Tooltip(We recommend turning this on for any face materials,)]
        [Tooltip(so face area can automatically use a better lighting, shadow and outline settings.)]
        [Tooltip()]
        [Tooltip(.    Please enable IsFace group for face,eye,eyebrow,mouth,teeth materials)]
        [Tooltip(.    Please set Depth Priming Mode to Disabled in URP renderer)]
        [Tooltip()]
        [Tooltip(You can search for (face) in the search bar while in property mode to find settings related to the face.)]
        [Main(_IsFaceGroup,_ISFACE)]_IsFace("Is Face?", Float) = 0

        [Helpbox(Please enable IsFace group for face,eye,eyebrow,mouth,teeth materials.)]
        [Helpbox()]
        [Helpbox(.    It will override the shadow color of face and allow you to edit the face shadow color easily in Shadow Color group.)]
        [Helpbox(.    It will override the face normals to flat normals also.)]
        [Helpbox()]
        [Helpbox(Please set Depth Priming Mode to Disabled in Universal Renderer.)]

        //[Title(_IsFaceGroup, Mask)]
        [Tooltip(If this material contains face and other non face parts together,)]
        [Tooltip(you can enable this toggle to make this section masked by Mask Map.)]
        [SubToggle(_IsFaceGroup,_FACE_MASK_ON)]_UseFaceMaskMap("Enable Mask?", Float) = 0

        [ShowIf(_UseFaceMaskMap, Equal, 1)]
        [AdvancedHeaderProperty]
        [Tooltip(Shader will convert Mask Map to a grayscale mask, so you need to select a texture channel or a convert method.)]
        [Tooltip()]
        [Tooltip(The converted grayscale mask value should be,)]
        [Tooltip(.    White for Face area.)]
        [Tooltip(.    Black for Non Face area.)]
        [Tooltip()]
        [Tooltip(If no texture is assigned, it will use a default White texture, which means the whole material is face.)]
        [Tex(_IsFaceGroup,_FaceMaskMapChannelMask)][NoScaleOffset]_FaceMaskMap("Mask Map", 2D) = "white" {}
        [Advanced][HideInInspector]_FaceMaskMapChannelMask("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is usually 1 bit better when texture is compressed
        
        [Advanced]
        [Tooltip(If your Mask Map is painted in an inverted way,)]
        [Tooltip(.    Painted White in Non Face area)]
        [Tooltip(.    Painted Black in Face area)]
        [Tooltip(In this situation, you can enable this toggle, which will invert the mask value inside the shader, so you do not have to spend time editing the texture.)]
        [SubToggle(_IsFaceGroup, _)]_FaceMaskMapInvertColor("               Invert?", Float) = 0
        
        [Advanced]
        [Tooltip(Apply a linear value remap to pixels of Mask Map.)]
        [Tooltip()]
        [Tooltip(For scripting,)] 
        [Tooltip()]
        [Tooltip(you should call Material.SetFloat())]
        [Tooltip(.    _FaceMaskMapRemapStart)]
        [Tooltip(.    _FaceMaskMapRemapEnd)]
        [Tooltip()]
        [Tooltip(You should NOT call Material.SetFloat())]
        [Tooltip(.    _FaceMaskMapRemapMinMaxSlider)]
        [MinMaxSlider(_IsFaceGroup,_FaceMaskMapRemapStart,_FaceMaskMapRemapEnd)]_FaceMaskMapRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_FaceMaskMapRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_FaceMaskMapRemapEnd("", Range(0.0,1.0)) = 1.0
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Render Face(Cull)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Tooltip(Similar to the Render Face of URP Lit shader. Decide which polygon face to render, it is not related to IsFace settings.)]
        [Main(_RenderFaceGroup,_,off,off)]_RenderFaceGroup("Render Face(Cull)", Float) = 0

        [Helpbox(Please set Preset to Both for double face material, usually for displaying inner polygons of cloths like skirt.)]
        
        [AdvancedHeaderProperty]
        [Title(_RenderFaceGroup,Preset)]
        [Tooltip(Unless the mesh has a wrong polygon facing, you only need to use Front or Both, and you can ignore Back and Both(Flip).)]
        [Tooltip()]
        [Tooltip(This preset controls the following params,)]
        [Tooltip(.     Lit pass (_Cull))]
        [Tooltip(.     Outline pass (_CullOutline))]
        [Tooltip(.     NiloToonSelfShadowCaster pass (_CullNiloToonSelfShadowCaster))]
        [Preset(_RenderFaceGroup,NiloToonCharacter_RenderFace_LWGUI_ShaderPropertyPreset)] _RenderFacePreset ("Preset", float) = 0 

        [Advanced]
        [Title(_RenderFaceGroup,. Render Face)]
        // https://docs.unity3d.com/ScriptReference/Rendering.CullMode.html
        [Tooltip(Same as URP Lit shader Render Face.)]
        [Tooltip()]
        [Tooltip(Use this dropdown to determine which sides of your geometry to render.)]
        [Tooltip()]
        [Tooltip(Options are,)]
        [Tooltip(.    Front    . Renders the front face of your geometry and culls the back face. This is the default setting.)]
        [Tooltip(.    Back     . Renders the back face of your geometry and culls the front face.)]
        [Tooltip(.    Both     . Renders both faces of the geometry, this is good for flat objects with single face polygon, like cloth and hair, where you might want both sides visible.)]
        [SubEnum(_RenderFaceGroup,Front,2,Back,1,Both,0)]_Cull("  Lit pass", Float) = 2

        [Advanced]
        [Tooltip(Use this dropdown to determine which sides of the outline geometry to render.)]
        [Tooltip(Outline pass should always renders the opposite face of surface color pass)]
        [Tooltip()]
        [Tooltip(Options are,)]
        [Tooltip(.    Front    . Renders the front face of your geometry and culls the back face.)]
        [Tooltip(.    Back     . Renders the back face of your geometry and culls the front face. This is the default setting.)]
        [SubEnum(_RenderFaceGroup,Front,2,Back,1)]_CullOutline("  Outline pass", Float) = 1

        [Advanced]
        [Tooltip(Use this dropdown to determine which sides of your geometry to render in NiloToonSelfShadowCaster pass.)]
        [Tooltip()]
        [Tooltip(Options are,)]
        [Tooltip(.    Front    . Renders the front face of your geometry and culls the back face.)]
        [Tooltip(.    Back     . Renders the back face of your geometry and culls the front face.)]
        [Tooltip(.    Both     . Renders both faces of the geometry, this is good for flat objects with single face polygon, like cloth and hair, where you might want both sides visible. Using this can avoid all shadowmap hole artifacts, but may produce more shadow acne problems.)]
        [SubEnum(_RenderFaceGroup,Front,2,Back,1,Both,0)]_CullNiloToonSelfShadowCaster("  NiloToonSelfShadowCaster pass", Float) = 1

        [Advanced]
        [Title(_RenderFaceGroup, . Backface edit)]
        [Tooltip(Back Face albedo will be replaced to this color, where alpha is the replace strength)]
        [Sub(_RenderFaceGroup)]_BackFaceBaseMapReplaceColor("  Replace to Color", Color) = (0,0,0,0)
        [Advanced]
        [Tooltip(Color will be multiplied to Base Map (affects Back Face polygon only) (rgb only multiply, not affecting alpha).)]
        [HDR][Sub(_RenderFaceGroup)]_BackFaceTintColor("  Tint Color", Color) = (1,1,1,1)
        [Advanced]
        [Sub(_RenderFaceGroup)]_BackFaceForceShadow("  Force Shadow", Range(0,1)) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Classic Outline
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        [Tooltip(Enable to redraw the back face of the mesh again, and adding a little vertex extrusion along normal or smoothed normal direction, which makes this redraw looks like an outline.)]
        [Tooltip(This outline method does not require any postprocessing or screen space render pass, so performance is usually good, unless vertex count is too high.)]
        [Tooltip()]
        [Tooltip(We call this Classic Outline, since it is the fastest and mostly used realtime outline method to draw character outline since the year 2000(Jet Set Radio).)] 
        [Tooltip(Many realtime application and games use this classic outline method also (e.g. Guilty Gear Xrd, Genshin Impact, VRM MToon))]
        [Tooltip()]
        [Tooltip(NiloToon improve this outline method by using an auto generated smoothed normal for extrude direction instead of lighting normal, and adding outline Z Offset, which will produce a much better outline result.)]
        [Main(_RENDER_OUTLINE,_)]_RenderOutline("Classic Outline", Float) = 1
        
        [Helpbox(When enabled, this material will produce a classic outline by redrawing the mesh with inverted culling and a slight extrusion based on the outline width.)]

        [Title(_RENDER_OUTLINE, Style)]
        [Tooltip(Enable to normalize outline width in view space xy plane, this will make outline width more uniform, but it will not always improve the outline result, you can turn it ON and OFF to check the difference, and keep the good looking option depending on the style you want.)]
        [SubToggle(_RENDER_OUTLINE,_)]_OutlineUniformLengthInViewSpace("Uniform screen width", Float) = 0
        
        [Advanced]
        [Title(_RENDER_OUTLINE, HQ Continuous Outline)]
        [Tooltip(Enable to use Smoothed Normal instead of lighting normal for outline extrude direction if smoothed normal can be generated by NiloToon, which will produce a much higher quality outline than using regular lighting normal.)]
        [Tooltip()]
        [Tooltip(.     For .fbx character, you should keep this turn ON, since NiloToon will generate Smoothed Normal for .fbx meshs (store in UV8).)]
        [Tooltip(.     For .vrm character, you can ignore this option (or turn this OFF if keeping it ON produced undesired result), since NiloToon does not generate Smoothed Normal for .vrm meshs.)]
        [Tooltip()]
        [Tooltip(If you find that disabling this toggle will produce better result for you, it is ok to turn this off, it depends on the outline style you want.)]
        [SubToggle(_RENDER_OUTLINE,_)]_OutlineUseBakedSmoothNormal("Allow?", Float) = 1
        
        [Advanced]
        [Title(_RENDER_OUTLINE, Support Depth of Field)]
        [Tooltip(If enabled, when drawing URP Depth Texture, NiloToon will draw Outline area into Depth Texture also.)]
        [Tooltip(For effects that rely on outline depth (e.g. Depth of Field), you may enable this toggle to provide a better Depth Texture for those effects.)]
        [Tooltip()]
        [Tooltip(Keep this turn off if you are seeing 2D Rim Light artifacts, or other depth related artifacts.)]
        [SubToggle(_RENDER_OUTLINE,_)]_UnityCameraDepthTextureWriteOutlineExtrudedPosition("Draw in Depth Texture?", Float) = 0

        [Title(_RENDER_OUTLINE, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_RENDER_OUTLINE, Width)]
        [Tooltip(The width of outline, usually it is between 0 to 1.)]
        [Tooltip()]
        [Tooltip(If you convert a material from VRM MToon or lilToon to NiloToon, NiloToon will keep the outline width from previous shader here. Then you should keep this Width untouched, and edit the below Width(extra) instead. This way you can preserve the outline width from previous shader.)]
        [Sub(_RENDER_OUTLINE)]_OutlineWidth("Width", Range(0,32)) = 0.5 // default 0.5, a conservative outline width for VTuber model
        [Tooltip(Just like the Width above, this width serves as an additional multiplier control for convenience.)]
        [Tooltip(Sometimes when you convert a material from another shader (e.g. VRM MToon) to NiloToon, the width from previous shader is stored in the above Width, then you can use this slider to do any extra width multiplier control without editing the above Width(_OutlineWidth).)]
        [Tooltip(.    For VRM0.x MToon converted materials, 12 to 18 is a good starting point)]
        [Tooltip(.    For VRM1.0 MToon converted materials, 512 is a good starting point)]
        [Sub(_RENDER_OUTLINE)]_OutlineWidthExtraMultiplier("Width(extra)", Range(0,2048)) = 1
        
        [Title(_RENDER_OUTLINE, Width Mask(Texture))]
        [Tooltip(Enable to let outline width multiply by a 0 to 1 value extracted from Mask Map.)]
        [SubToggle(_RENDER_OUTLINE,_OUTLINEWIDTHMAP)]_UseOutlineWidthTex("Enable Mask?", Float) = 0
        [Tooltip(Similar to an Outline width Map.)]
        [Tooltip(.    white means keeping width unchange)]
        [Tooltip(.    darker means reduce width)]
        [Tooltip(.    black means 0 width (e.g. hiding the outline))]
        [Tooltip(You need to pick a channel or a method to extract the final 0 to 1 value for outline width multiply.)]
        [Tooltip()]
        [Tooltip(To paint this mask map, you start with a white texture, and paint darker or black value to areas that should reduce outline width, like)]
        [Tooltip(.    eye)]
        [Tooltip(.    mouth)]
        [Tooltip(.    hair)]
        [Tooltip(.    finger and nails)]
        [Tooltip(.    small accessory)]
        [Tooltip()]
        [Tooltip(Typically, if you have a white face texture with the eye and mouth areas painted black, you can use it here.)]
        [Tooltip()]
        [Tooltip(Expected sRGB off, Mipmap off)]
        [Tex(_RENDER_OUTLINE,_OutlineWidthTexChannelMask)][NoScaleOffset]_OutlineWidthTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_OutlineWidthTexChannelMask("", Vector) = (0,1,0,0)
        
        [Title(_RENDER_OUTLINE, Width Mask(Vertex Color))]
        [Tooltip(Enable to let outline width multiply with a 0 to 1 value extracted from vertex color.)]
        [Advanced][SubToggle(_RENDER_OUTLINE,_)]_UseOutlineWidthMaskFromVertexColor("Enable Mask?", Float) = 0
        [Tooltip(Select which vertex color channel should be extracted to multiply with outline width, or pick a convertion method to extract a 0 to 1 value to multiply with outline width.)]
        [Advanced][ChannelDrawer(_RENDER_OUTLINE)]_OutlineWidthMaskFromVertexColor("    Use channel", Vector) = (0,1,0,0)
        
        [Title(_RENDER_OUTLINE, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_RENDER_OUTLINE, Color)]
        [Tooltip(The resulting outline color is derived from the Base Map multiplied by this outline Tint Color.)]
        [Sub(_RENDER_OUTLINE)][HDR]_OutlineTintColor("Tint Color", Color) = (0.25,0.25,0.25,1)
        [Tooltip(You can override outline Tint Color for Skin area.)]
        [Tooltip(Alpha of this color is the override strength.)]
        [Tooltip()]
        [Tooltip(The default alpha is 1, which means the outline color of Skin will be overridden to this color by default.)]
        [Sub(_RENDER_OUTLINE)][HDR]_OutlineTintColorSkinAreaOverride("Tint Color(Skin Override)", Color) = (0.4,0.2,0.2,1)
        
        [Advanced]
        [Title(Extra Tint Color)]
        [Tooltip(An extra outline tint color for occlusion area.)]
        [Sub(_RENDER_OUTLINE)][HDR]_OutlineOcclusionAreaTintColor("Occlusion area Tint Color", Color) = (1,1,1,1)

        [Title(_RENDER_OUTLINE, Replace Color(Pre Lighting))]
        [Tooltip(You can replace outline color to a single color, and this outline color will be affected by lighting.)]
        [Tooltip(This slider controls the Replace Strength.)]
        [Advanced][Sub(_RENDER_OUTLINE)]_OutlineUsePreLightingReplaceColor("Replace Strength", Range(0,1)) = 0
        [Tooltip(You can replace outline color to a single color, and this outline color will be affected by lighting.)]
        [Tooltip(This Color is the outline color that you want to replace to.)]
        [Advanced][Sub(_RENDER_OUTLINE)][HDR]_OutlinePreLightingReplaceColor("    Replace to Color", Color) = (1,1,1,1)

        [Title(_RENDER_OUTLINE, Replace Color(Final Color))]
        [Tooltip(You can replace outline result color to a single color, and this outline color will NOT be affected by lighting, it is the final outline color.)]
        [Tooltip(This slider controls the Replace Strength.)]
        [Advanced][Sub(_RENDER_OUTLINE)]_OutlineUseReplaceColor("Replace Strength", Range(0,1)) = 0
        [Tooltip(You can replace outline result color to a single color, and this outline color will NOT be affected by lighting, it is the final outline color.)]
        [Tooltip(This Color is the final outline color that you want to replace to.)]
        [Advanced][Sub(_RENDER_OUTLINE)][HDR]_OutlineReplaceColor("    Replace to Color", Color) = (1,1,1,1)
        
        [Title(_RENDER_OUTLINE, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_RENDER_OUTLINE, Base Z Offset)]
        [Tooltip(Outline Z Offset is used to hide inner outline. The higher the outline Z Offset, the less inner outline can appear.)]
        [Tooltip(Increasing this a bit can hide most of the ugly inner outline.)]
        [Tooltip()]
        [Tooltip(Outline Z Offset is the View Space unit that pushs outline away from camera (1 Z Offset is 1m).)]
        [Tooltip()]
        [Tooltip(Outline Z Offset will only modify clip space position.z of the outline vertex, so modify Z Offset will not change the outline vertex XY position on screen, it will only affects depth buffer Z value (affects per pixel depth sorting).)]
        [Tooltip()]
        [Tooltip(This is the Base Z Offset, so it will NOT be affected by any Z Offset Mask.)]
        [Sub(_RENDER_OUTLINE)]_OutlineBaseZOffset("Base Z Offset", Range(0,1)) = 0

        [Title(_RENDER_OUTLINE, ........................................................................................................................................................................................................................................................................................................................................................................)]
        [Title(_RENDER_OUTLINE, Maskable Z Offset)]
        [Tooltip(Outline Z Offset is used to hide inner outline. The higher the outline Z Offset, the less inner outline can appear.)]
        [Tooltip(Increasing this a bit can hide most of the ugly inner outline, so the default value is not 0.)]
        [Tooltip()]
        [Tooltip(Outline Z Offset is the View Space unit that pushs outline away from camera (1 Z Offset is 1m).)]
        [Tooltip()]
        [Tooltip(Outline Z Offset will only modify clip space position.z of the outline vertex, so modify Z Offset will not change the outline vertex XY position on screen, it will only affects depth buffer Z value (affects per pixel depth sorting).)]
        [Tooltip()]
        [Tooltip(This is a Maskable Z Offset, so it will be affected by Z Offset Mask.)]
        [Sub(_RENDER_OUTLINE)]_OutlineZOffset("Z Offset", Range(0,1)) = 0.0001
        [Tooltip(This is the Outline Z Offset for the face area, similar to the Z Offset mentioned above.)]
        [Tooltip()]
        [Tooltip(For faces, a larger default Outline Z Offset is used to hide unattractive outlines around the eyes and mouth by default, as users may not have an optimal outline width mask map. However, this could also mistakenly hide desirable outlines of the jaw and cheek.)]
        [Tooltip()]
        [Tooltip(If you have already provided a good outline width mask map, you should reduce this ZOffset (Face) to reveal more pleasing outlines of the face.)]
        [Tooltip()]
        [Toltip(Default 0.02 is a conservative safe value to hide bad outline around eye and mouth (view in any camera direction), 0.01 is usually not enough)]
        [Sub(_RENDER_OUTLINE)]_OutlineZOffsetForFaceArea("Z Offset (Face)", Range(0,1)) = 0.02

        [Title(_RENDER_OUTLINE,Z Offset Mask(Texture))]
        [Tooltip(Enable to let outline Z Offset multiply by an inverted float value extracted from Mask Map.)]
        [Tooltip()]
        [Tooltip(Usually if you have a white face texture that painted black in eye and mouth area, you can use it here.)]
        [Tooltip(See the tooltip of Mask Map for details.)]
        [SubToggle(_RENDER_OUTLINE,_OUTLINEZOFFSETMAP)]_UseOutlineZOffsetTex("Enable Mask?", Float) = 0
        [Tooltip(The Mask Map of Outline Z Offset.)]
        [Tooltip(Due to the convention of artist painting black in areas that wants to hide ugly outline (e.g. eye and mouth), this mask map is using an inverted mask rule to coop with artist convention, so be careful.)]
        [Tooltip()]
        [Tooltip(This Mask Map expects the following data, similar to the data of outline width Mask Map, where you paint the area that has ugly outline as Black, and keep other area as White.)]
        [Tooltip(.    Black is apply Z Offset as usual)]
        [Tooltip(.    Brighter is reduce Z Offset)]
        [Tooltip(.    White is not apply Z Offset)]
        [AdvancedHeaderProperty][Tex(_RENDER_OUTLINE,_OutlineZOffsetMaskTexChannelMask)][NoScaleOffset]_OutlineZOffsetMaskTex("Mask Map", 2D) = "black" {}
        [Advanced][HideInInspector]_OutlineZOffsetMaskTexChannelMask("", Vector) = (0,1,0,0)
        [Tooltip(If you want to use a Z Offset Mask map that is,)]
        [Tooltip(.    White is apply Z Offset as usual)]
        [Tooltip(.    Darker is reduce Z Offset)]
        [Tooltip(.    Black is not apply Z Offset)]
        [Tooltip(then you need to enable this toggle.)]
        [Advanced][SubToggle(_RENDER_OUTLINE, _)]_OutlineZOffsetMaskTexInvertColor("               Invert?", Float) = 0

        [Tooltip(Apply a linear value remap to pixels of Mask Map.)]
        [Tooltip()]
        [Tooltip(For scripting,)] 
        [Tooltip()]
        [Tooltip(you should call Material.SetFloat())]
        [Tooltip(.    _OutlineZOffsetMaskRemapStart)]
        [Tooltip(.    _OutlineZOffsetMaskRemapEnd)]
        [Tooltip()]
        [Tooltip(You should NOT call Material.SetFloat())]
        [Tooltip(.    _OutlineZOffsetMaskRemapMinMaxSlider)]
        [Advanced][MinMaxSlider(_RENDER_OUTLINE,_OutlineZOffsetMaskRemapStart,_OutlineZOffsetMaskRemapEnd)]_OutlineZOffsetMaskRemapMinMaxSlider("               Remap", Range(0,1)) = 1
        [HideInInspector]_OutlineZOffsetMaskRemapStart("", Range(0,1)) = 0
        [HideInInspector]_OutlineZOffsetMaskRemapEnd("", Range(0,1)) = 1
        
        
        [Title(_RENDER_OUTLINE, Z Offset Mask(Vertex Color))]
        [Tooltip(Enable to let outline Z Offset multiply by a float value extracted from vertex color.)]
        [Advanced][SubToggle(_RENDER_OUTLINE,_)]_UseOutlineZOffsetMaskFromVertexColor("Enable Mask?", Float) = 0
        [Tooltip(Select which vertex color channel should be extracted to multiply with outline Z Offset, or pick a convertion method to extract a float value to multiply with outline Z Offset.)]
        [Advanced][ChannelDrawer(_RENDER_OUTLINE)]_OutlineZOffsetMaskFromVertexColor("    Use channel", Vector) = (0,1,0,0)
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // RimLight+Shadow 2D
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Tooltip(When enabled, shader will render two effects together,)]
        [Tooltip(.    A screen space 2D rim light that relies on Depth Texture)]
        [Tooltip(.    A screen space 2D shadow that relies on Depth Texture)]
        [Tooltip()]
        [Tooltip(When disabled,)]
        [Tooltip(.    screen space 2D rim light will fallback to a traditional 3D fresnel rim light that looks as similar as possbile)]
        [Tooltip(.    screen space 2D shadow will disappear completely, there is no fallback)]
        [Main(_DepthTextureRimLightAndShadowGroup,_)]_PerMaterialEnableDepthTextureRimLightAndShadow("RimLight+Shadow 2D", Float) = 1

        [Helpbox(When enabled, this material will render two effects together,)]
        [Helpbox(.    screen space 2D rim light)]
        [Helpbox(.    screen space 2D shadow)]
        [Helpbox()]
        [Helpbox(When disabled,)]
        [Helpbox(.    screen space rim light will fallback to a traditional 3D fresnel rim light)]
        [Helpbox(.    screen space shadow will disappear without fallback)]
        
        [Title(_DepthTextureRimLightAndShadowGroup, Width)]
        [Tooltip(This width slider will control the width of both Depth Texture rim light and shadow together)]
        [AdvancedHeaderProperty][Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightAndShadowWidthMultiplier("Width Multiplier", Range(0,4)) = 0.6
        [Tooltip(This width slider serves as an additional multiplier, same as the Width Multiplier above, but it is provided for convenience.)]
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightAndShadowWidthExtraMultiplier("Width Multiplier(extra)", Range(0,4)) = 1
        [Tooltip(When pixel to camera distance is less than 1,)] 
        [Tooltip(increase this value towards 1 to reduce the width of rim light and shadow more.)]
        [Tooltip(reduce this value towards 0 to not reduce the width of rim light and shadow.)]
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightAndShadowReduceWidthWhenCameraIsClose("Near Cam Reduce Width", Range(0,1)) = 1

        [Title(_DepthTextureRimLightAndShadowGroup,Width Mask (Texture))]
        [Tooltip(Enable to multiply the width of rim light and shadow by a texture)]
        [SubToggle(_DepthTextureRimLightAndShadowGroup,_DEPTHTEX_RIMLIGHT_SHADOW_WIDTHMAP)]_UseDepthTexRimLightAndShadowWidthTex("Enable Width Mask?", Float) = 0
        [Tooltip(. white is full width)]
        [Tooltip(. darker is reduce width)]
        [Tooltip(. black is 0 width)]
        [Tooltip((Default white))]
        [Tex(_DepthTextureRimLightAndShadowGroup,_DepthTexRimLightAndShadowWidthTexChannelMask)][NoScaleOffset]_DepthTexRimLightAndShadowWidthTex("   Mask Map ", 2D) = "white" {}
        [HideInInspector]_DepthTexRimLightAndShadowWidthTexChannelMask("", Vector) = (0,1,0,0)
        
        [Title(_DepthTextureRimLightAndShadowGroup, Width Mask (Vertex Color))]
        [Tooltip(Enable to multiply the width of rim light and shadow by vertex color)]
        [Advanced][SubToggle(_DepthTextureRimLightAndShadowGroup,_)]_UseDepthTexRimLightAndShadowWidthMultiplierFromVertexColor("Enable Mask?", Float) = 0
        [Advanced][ChannelDrawer(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightAndShadowWidthMultiplierFromVertexColorChannelMask("    use channel", Vector) = (0,1,0,0)
        
        [Title(_DepthTextureRimLightAndShadowGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_DepthTextureRimLightAndShadowGroup, Style)]
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightIgnoreLightDir("360 Rim light", Range(0,1)) = 0
        
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexShadowIgnoreLightDir("360 Shadow (deprecated)", Range(0,1)) = 0
        
        [Title(_DepthTextureRimLightAndShadowGroup, Style (face))]
        [Tooltip(When set to 1, the face 2D shadow will be static on the face, ignoring light direction)]
        [Tooltip()]
        [Tooltip(When set to 0, the face 2D shadow direction will follow the final main light direction)]
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexShadowFixedDirectionForFace("Fixed face (hair) Shadow", Range(0,1)) = 0
        
        [Title(_DepthTextureRimLightAndShadowGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_DepthTextureRimLightAndShadowGroup, Rim Light 2D)]
        [Tooltip(The total opacity control of this rim light feature.)]
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightUsage("Opacity", Range(0,1)) = 1
        
        [Tooltip(Enable to multiply the opacity of rim light 2D by a texture.)]
        [SubToggle(_DepthTextureRimLightAndShadowGroup,_DEPTHTEX_RIMLIGHT_OPACITY_MASKMAP)]_UseDepthTexRimLightMaskTex("    Mask?", Float) = 0
        [Tooltip(. white is full opacity)]
        [Tooltip(. darker is reduce opacity)]
        [Tooltip(. black is 0 opacity)]
        [Tooltip((Default white))]
        [ShowIf(_UseDepthTexRimLightMaskTex,Equal,1)]
        [AdvancedHeaderProperty]
        [Tex(_DepthTextureRimLightAndShadowGroup,_DepthTexRimLightMaskTexChannelMask)][NoScaleOffset]_DepthTexRimLightMaskTex("   Mask Map ", 2D) = "white" {}
        [Advanced]
        [HideInInspector]_DepthTexRimLightMaskTexChannelMask("", Vector) = (0,1,0,0)
        [Advanced]
        [SubToggle(_DepthTextureRimLightAndShadowGroup, _)]_DepthTexRimLightMaskTexInvertColor("              Invert?", Float) = 0
        
        [Title(.   Setting)]
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightWidthMultiplier("    Width", Range(0,10)) = 1
        [Tooltip(The intensity or brightness of rim light.)]
        [Tooltip(In HDR mode, a high value will produce bloom at rim light area, usually you will control this in NiloToonCharRenderingControlVolume instead of material.)]
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightIntensity("    Brightness", Range(0,32)) = 1.5
        [Tooltip(An optional tint color of the rim light result.)]       
        [Sub(_DepthTextureRimLightAndShadowGroup)][HDR]_DepthTexRimLightTintColor("    Tint Color", Color) = (1,1,1)
        [Tooltip(The amount of Base Map color multiply into rim light color)]
        [Tooltip(.     0 means ignore BaseMap color, keep rim light color white)]
        [Tooltip(.     1 means fully multiply BaseMap color into rim light color)]
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightMixWithBaseMapColor("    Mix with Base Map", Range(0,1)) = 0.5

        [Title(_DepthTextureRimLightAndShadowGroup, .   Rim Light 3D Rim mask)]
        [Tooltip(Mask the rim light result by a classic 3D fresnel rim light mask.)]
        [Tooltip(Enable this may produce better rim light result for small objects like finger or accessory.)]
        [SubToggle(_DepthTextureRimLightAndShadowGroup,_)]_DepthTexRimLight3DRimMaskEnable("    Enable?", Float) = 0
        [Helpbox(Recommend to enable for small objects like finger or accessory)]
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLight3DRimMaskThreshold("        threshold", Range(0,1)) = 0.5
        
        [Title(_DepthTextureRimLightAndShadowGroup, Rim Light Area)]
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightBlockByShadow("Block by Shadow", Range(0,1)) = 0
        [Tooltip(A positive bias will make 2D rim light occlude by nearby objects easier (removing more inner 2D rim light), great for producing a clean silhouette only rim light)]
        [Tooltip()]
        [Tooltip(A negative bias will produce more inner 2D rim light, which may not be desired)]
        [Tooltip()]
        [Tooltip(Recommend increase it to 0.5 for hair material to produce a cleaner hair 2D rim light.)]
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightThresholdOffset("Occlusion bias", Range(-1,1)) = 0
        [Tooltip(Rim light will fadeout softly when a nearby occulder appears, you can control the fadeout softness.)]
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightFadeoutRange("Fadeout Softness", Range(0.01,10)) = 1

        [Title(_DepthTextureRimLightAndShadowGroup, Rim Light artifacts fix)]
        [Tooltip(. Turn on will remove most of the rim light artifacts, but it is much slower to render.)]
        [Tooltip(. Turn off will allow rim light artifacts to appear, but it is much faster to render.)]
        [Advanced][SubToggle(_DepthTextureRimLightAndShadowGroup,_DEPTHTEX_RIMLIGHT_FIX_DOTTED_LINE_ARTIFACTS)]_ApplyDepthTexRimLightFixDottedLineArtifacts("Enable fix?", Float) = 1
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLightFixDottedLineArtifactsExtendMultiplier("    Fix Range", Range(0,1)) = 0.1
        
        [Title(_DepthTextureRimLightAndShadowGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]
        [Title(RimLight 3D Fallback (experimental))]
        [Tooltip(3D Fallback values will be used only when the whole .RimLight Shadow 2D. group is disabled)]
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLight3DFallbackMidPoint("MidPoint", Range(0,1)) = 0.7
        [Tooltip(3D Fallback values will be used only when the whole .RimLight Shadow 2D. group is disabled)]
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexRimLight3DFallbackSoftness("Softness", Range(0,1)) = 0.02
        [Tooltip(3D Fallback values will be used only when the whole .RimLight Shadow 2D. group is disabled)]
        [SubToggle(_DepthTextureRimLightAndShadowGroup,_)]_DepthTexRimLight3DFallbackRemoveFlatPolygonRimLight("Remove Flat polygon rim?", Float) = 1        
        
        [Title(_DepthTextureRimLightAndShadowGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_DepthTextureRimLightAndShadowGroup, Shadow 2D)]
        [Tooltip(The final opacity control of this shadow feature.)]
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexShadowUsage("Opacity", Range(0,1)) = 1
        [Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexShadowWidthMultiplier("    Width", Range(0,10)) = 1

        [Title(_DepthTextureRimLightAndShadowGroup, Shadow Color)]
        [Title(_DepthTextureRimLightAndShadowGroup, .   Non Face)]
        [AdvancedHeaderProperty][Preset(_DepthTextureRimLightAndShadowGroup,NiloToonCharacter_DepthTexShadowColorStyleForNonFace_LWGUI_ShaderPropertyPreset)] _DepthTexShadowColorStyleForNonFacePreset ("    Color Style", float) = 0 
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexShadowBrightness("    Brightness", Range(0,1)) = 0.85
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)][HDR]_DepthTexShadowTintColor("    Tint Color", Color) = (1,1,1)

        [Title(_DepthTextureRimLightAndShadowGroup, .   Face)]
        [AdvancedHeaderProperty][Preset(_DepthTextureRimLightAndShadowGroup,NiloToonCharacter_DepthTexShadowColorStyleForFace_LWGUI_ShaderPropertyPreset)] _DepthTexShadowColorStyleForFacePreset ("    Color Style", float) = 0 
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexShadowBrightnessForFace("    Brightness", Range(0,1)) = 1       
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)][HDR]_DepthTexShadowTintColorForFace("    Tint Color", Color) = (1,0.85,0.85)

        [Title(_DepthTextureRimLightAndShadowGroup, Shadow Area)]
        [Tooltip(If you are seeing shadow artifact due to using this material on a big object, try adjust this settings.)]
        [Advanced(Advanced)][Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexShadowThresholdOffset("Threshold Offset", Range(-1,1)) = 0
        [Tooltip(If you are seeing shadow artifact due to using this material on a big object, try adjust this settings.)]
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)]_DepthTexShadowFadeoutRange("Fadeout Range", Range(0.01,10)) = 1

        [Title(_DepthTextureRimLightAndShadowGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_DepthTextureRimLightAndShadowGroup, Face Area Param)]
        [Tooltip(If you are seeing a wierd depth tex shadow on the neck, try adjust this value.)]
        [Advanced][Sub(_DepthTextureRimLightAndShadowGroup)]_FaceAreaCameraDepthTextureZWriteOffset("DepthTex ZWrite Offset", Range(0,0.1)) = 0.04 // minimum required is 0.04. (0.03 is still too small, mouth area may show unwanted shadow when viewed by side camera angle)

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Shadow Color
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Tooltip(This section controls the shadow color.)]
        [Tooltip(You can read the document pdf for solution if ugly random hue color artifact appears in shadow area)]
        [Main(_ShadowColorGroup,_)]_EnableShadowColor("Shadow Color", Float) = 1

        [Helpbox(Calculate the light independent shadow color from BaseMap, similar to using color adjustment layer in Photoshop to find out the shadow color.)]
        [Helpbox(Shadow color will only be displayed in shadow region.)]
        
        [Title(_ShadowColorGroup, Shadow Color Style)]
        [Sub(_ShadowColorGroup)][HDR]_SelfShadowTintColor("Tint Color", Color) = (1,1,1)
        [Sub(_ShadowColorGroup)]_SelfShadowAreaHSVStrength("HSV Stregnth", Range(0,1)) = 1
        
        [Helpbox(HSV requires texture compression equals High or None for Saturation Boost to work nicely.)]
        [Helpbox(If shadow color artifact appears, try to use a better texture compression or lower Saturation Boost)]
        
        // in this section, default values were set by the author's art experience, you can edit it freely if these don't work for your project
        [ShowIf(_SelfShadowAreaHSVStrength,Greater,0)]
        [Sub(_ShadowColorGroup)]_SelfShadowAreaHueOffset("    (H) Hue Offset", Range(-1,1)) = 0
        [ShowIf(_SelfShadowAreaHSVStrength,Greater,0)]
        [Sub(_ShadowColorGroup)]_SelfShadowAreaSaturationBoost("    (S) Saturation Boost", Range(0,1)) = 0.2 // the default value is 0.5 in version <= NiloToon 0.16.7. Now it is 0.2 as a safer default to reduce the default hue artifact
        [ShowIf(_SelfShadowAreaHSVStrength,Greater,0)]
        [Sub(_ShadowColorGroup)]_SelfShadowAreaValueMul("    (V) Value Multiply", Range(0,1)) = 0.7
        
        [Header(....................................................................................................................................................................................)]
        [AdvancedHeaderProperty]
        [Title(_ShadowColorGroup, Lit To Shadow Transition Area Style)]
        [Sub(_ShadowColorGroup)]_LitToShadowTransitionAreaIntensity("Intensity", Range(0,32)) = 1
        [Advanced]
        [ShowIf(_LitToShadowTransitionAreaIntensity,Greater,0)]
        [Sub(_ShadowColorGroup)][HDR]_LitToShadowTransitionAreaTintColor("    Tint Color", Color) = (1,1,1)

        [Advanced]
        [ShowIf(_LitToShadowTransitionAreaIntensity,Greater,0)]
        [Sub(_ShadowColorGroup)]_LitToShadowTransitionAreaHueOffset("    (H) Hue Offset", Range(-1,1)) = 0.01
        [Advanced]
        [ShowIf(_LitToShadowTransitionAreaIntensity,Greater,0)]
        [Sub(_ShadowColorGroup)]_LitToShadowTransitionAreaSaturationBoost("    (S) Saturation Boost", Range(0,1)) = 0.5
        [Advanced]
        [ShowIf(_LitToShadowTransitionAreaIntensity,Greater,0)]
        [Sub(_ShadowColorGroup)]_LitToShadowTransitionAreaValueMul("    (V) Value Multiply", Range(0,1)) = 1
        
        
        [Header(....................................................................................................................................................................................)]
        [Title(_ShadowColorGroup, Skin and Face Override)]
        [Tooltip(Select all materials of a character, then pick a Color Preset that looks good on your model.)]
        [AdvancedHeaderProperty]
        [Preset(_ShadowColorGroup,NiloToonCharacter_SkinFaceShadowColor_LWGUI_ShaderPropertyPreset)] _SkinFaceShadowColorPreset ("Color Preset", float) = 0 

        [Title(_ShadowColorGroup, .   Skin Override)]
        [Tooltip(If you enabled IsSkin toggle, you can optionally override skin shadow color to _SkinShadowTintColor)]
        [Tooltip(Controls if this material should use an override Skin Shadow Tint Color)]
        [Tooltip(.     When this is 1, will use default settings, and use Skin Shadow Tint Color as shadow color.)]
        [Tooltip(.     When this is 0, will not override skin shadow color.)]
        [Advanced]
        [Sub(_ShadowColorGroup)]_OverrideBySkinShadowTintColor("    Strength", Range(0,1)) = 1
        [Advanced]
        [Sub(_ShadowColorGroup)]_SkinShadowTintColor("        Shadow Color", Color) = (1,0.8,0.8) // TODO: if we want to unify face & skin shadow color = try (1,0.856,0.856)
        [Advanced]
        [Sub(_ShadowColorGroup)]_SkinShadowBrightness("        Brightness", Range(0,1)) = 1
        [Advanced]
        [Sub(_ShadowColorGroup)]_SkinShadowTintColor2("        Custom Tint", Color) = (1,1,1)
        
        [Title(_ShadowColorGroup, .   Face Override)]
        [Tooltip(If you enabled IsFace toggle, you can optionally override face shadow color to _FaceShadowTintColor)]
        [Tooltip(Controls if this material should use an override Skin Shadow Tint Color)]
        [Tooltip(.     When this is 1, will use default settings, and use Skin Shadow Tint Color as shadow color.)]
        [Tooltip(.     When this is 0, will not override skin shadow color.)]
        [Advanced]
        [Sub(_ShadowColorGroup)]_OverrideByFaceShadowTintColor("    Strength", Range(0,1)) = 1
        [Advanced]
        [Sub(_ShadowColorGroup)]_FaceShadowTintColor("        Shadow Color", Color) = (1,0.9,0.9) // TODO: if we want to unify face & skin shadow color = try (1,0.856,0.856)
        [Advanced]
        [Sub(_ShadowColorGroup)]_FaceShadowBrightness("        Brightness", Range(0,1)) = 1
        [Advanced]
        [Sub(_ShadowColorGroup)]_FaceShadowTintColor2("        Custom Tint", Color) = (1,1,1)
        
        [Hidden]
        [Advanced]
        [Title(_ShadowColorGroup, Low Saturation Fallback Color)]
        [Tooltip(Alpha is intensity, default is OFF.)]
        [Sub(_ShadowColorGroup)][HDR]_LowSaturationFallbackColor("Low Saturation Fallback Color", Color) = (0.3764706,0.4141177,0.5019608,0)
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Shadow Color > Shadow Color Map
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // TODO: highlight this, make it looks like a group
        [AdvancedHeaderProperty]
        [Header(....................................................................................................................................................................................)]
        [Header(Shadow Color Map)]
        [Tooltip(If you dont like the shadow color of the above section,)]
        [Tooltip(you can modify the shadow color by a Shadow Color Map.)]
        [SubToggle(_ShadowColorGroup,_OVERRIDE_SHADOWCOLOR_BY_TEXTURE)]_UseOverrideShadowColorByTexture("Enable?", Float) = 0        

        [Advanced]
        [Title(_ShadowColorGroup, .   Strength)]
        [Sub(_ShadowColorGroup)]_OverrideShadowColorByTexIntensity("    Intensity", Range(0,1)) = 1
        
        [Advanced]
        [Title(_OverrideShadowColorByTextureGroup, .   Mode)]
        [Tooltip(.  Replace, In this mode, Shadow Color Map defines the final shadow color, overriding the BaseMap. Draw the desired shadow color incorporating BaseMap information directly onto this map.)]
        [Tooltip()]
        [Tooltip(.  Multiply, In this mode, BaseMap is multiplied by Shadow Color Map, resulting the final shadow color. You draw only the shadow Tint Color in Shadow Color Map without any BaseMap information.)]
        [SubEnum(_ShadowColorGroup,Replace,0,Multiply,1)]_OverrideShadowColorByTexMode("    Mode", Float) = 0

        [Advanced]
        [Title(_ShadowColorGroup, .   Shadow Color Map)]
        [Tooltip(rgb is shadow color information, a is mask.)]
        [Tex(_ShadowColorGroup,_OverrideShadowColorTexTintColor)][NoScaleOffset]_OverrideShadowColorTex("Shadow Color Map", 2D) = "white" {}
        [Advanced]
        [HideInInspector][HDR]_OverrideShadowColorTexTintColor("", Color) = (1,1,1,1)
        [Advanced]
        [Tooltip(alpha of Shadow Color Map is a mask, but you can ignore the alpha by setting this to 1.)]
        [Sub(_ShadowColorGroup)]_OverrideShadowColorTexIgnoreAlphaChannel("              Ignore Alpha", Range(0,1)) = 0
        
        [Advanced]
        [Title(_ShadowColorGroup, .   Mask)]
        [Tooltip(white is apply override, black is do nothing.)]
        [Tex(_ShadowColorGroup,_OverrideShadowColorMaskMapChannelMask)][NoScaleOffset]_OverrideShadowColorMaskMap("Mask Map", 2D) = "white" {}
        [Advanced]
        [HideInInspector]_OverrideShadowColorMaskMapChannelMask("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better   
        [Advanced]
        [SubToggle(_ShadowColorGroup, _)]_OverrideShadowColorMaskMapInvertColor("              Invert?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Lighting Style
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //[ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Controls the shape,softness,intensity of local dot(N,L) diffuse shadow (not shadowmap))]
        [Main(_LightingStyleGroup,_,off,off)]_LightingStyleGroup("Lighting Style", Float) = 0

        [Title(_LightingStyleGroup, Receive lighting)]
        [Sub(_LightingStyleGroup)]_AsUnlit("As unlit", Range(0.0,1)) = 0
        
        [Title(_LightingStyleGroup,Main Light Shadow Style)]
        [Helpbox(Controls the shape,softness,intensity of local NdotL shadow (not shadowmap))]
        [Helpbox()]
        [Helpbox(If the default values does not fit your target style, you can try another Preset)]
        [Preset(_LightingStyleGroup,NiloToonCharacter_LightingStyleDirectionalLight_LWGUI_ShaderPropertyPreset)] _LightingStyleDirectionalLightRenderFacePreset ("Preset", float) = 0 
        [Tooltip(Offsets Main light NdotL mid point.)]
        [Sub(_LightingStyleGroup)]_CelShadeMidPoint("Mid Point", Range(-1,1)) = 0
        [Tooltip(Controls the blending range(softness) between Lit and Shadow area.)]
        [Sub(_LightingStyleGroup)]_CelShadeSoftness("Softness", Range(0.001,1)) = 0.05 // avoid 0
        
        [Title(_LightingStyleGroup,Main Light Shadow remove)]
        [Tooltip(Increase this a bit can produce fake SSS, or fake indirect light.)]
        [Sub(_LightingStyleGroup)]_MainLightIgnoreCelShade("Remove Shadow", Range(0,1)) = 0

        [Title(_LightingStyleGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_LightingStyleGroup,Indirect Light)]
        [Tooltip(Increase this will produce a more 2D indirect light effect from baked light probe.)]
        [Tooltip(Decrease this will produce a more 3D indirect light effect from baked light probe.)]
        [Sub(_LightingStyleGroup)]_IndirectLightFlatten("Light Flatten", Range(0,1)) = 1

        [Title(_LightingStyleGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_LightingStyleGroup,Additional Light)]
        // in this section, default values were set by the author's art experience, you can edit it freely if these don't work for your project
        [Sub(_LightingStyleGroup)]_AdditionalLightCelShadeMidPoint("Mid Point", Range(-1,1)) = 0
        [Sub(_LightingStyleGroup)]_AdditionalLightCelShadeSoftness("Softness", Range(0.001,1)) = 0.05 // avoid 0
        [Tooltip(Increase this a bit can produce fake SSS, or fake indirect light.)]
        [Sub(_LightingStyleGroup)]_AdditionalLightIgnoreCelShade("Remove Shadow", Range(0,1)) = 0.2
        [Sub(_LightingStyleGroup)]_AdditionalLightIgnoreOcclusion("Remove Occlusion", Range(0,1)) = 0.2
        [Tooltip(When light is too close to character,)]
        [Tooltip(it can become too bright.)]
        [Tooltip(This option allows you to set a max brightness when light is extremely close to character.)]
        [Sub(_LightingStyleGroup)]_AdditionalLightDistanceAttenuationClamp("Distance Attenuation Clamp", Range(0,32)) = 2

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Lighting Style (face)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //[ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Similar to the above Lighting Style.)]
        [Tooltip(Override Lighting Style for Face.)]
        [Main(_LightingStyleFaceOverrideGroup,_,off,off)]_LightingStyleFaceOverrideGroup("Lighting Style (face)", Float) = 0
        
        [Helpbox(Similar to the above Lighting Style.)]
        [Helpbox(Override Lighting Style for Face.)]

        [Title(_LightingStyleFaceOverrideGroup, Main light Override for IsFace area)]
        [SubToggle(_LightingStyleFaceOverrideGroup,_)]_OverrideCelShadeParamForFaceArea("Override?", Float) = 1
        // in this section, default values were set by the author's art experience, you can edit it freely if these don't work for your project
        [Sub(_LightingStyleFaceOverrideGroup)]_CelShadeMidPointForFaceArea("    Mid Point", Range(-1,1)) = -0.3
        [Sub(_LightingStyleFaceOverrideGroup)]_CelShadeSoftnessForFaceArea("    Softness", Range(0.001,1)) = 0.15 // avoid 0
        [Tooltip(Increase this a bit can produce fake SSS, or fake indirect light.)]
        [Sub(_LightingStyleFaceOverrideGroup)]_MainLightIgnoreCelShadeForFaceArea("    Remove shadow", Range(0,1)) = 0.0 // default 0 to make face and body brightness in shadow similar

        [Title(_LightingStyleFaceOverrideGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_LightingStyleFaceOverrideGroup, Additional light Override for IsFace area)]
        [SubToggle(_LightingStyleFaceOverrideGroup,_)]_OverrideAdditionalLightCelShadeParamForFaceArea("Override?", Float) = 1
        // in this section, default values were set by the author's art experience, you can edit it freely if these don't work for your project
        [Sub(_LightingStyleFaceOverrideGroup)]_AdditionalLightCelShadeMidPointForFaceArea("    Mid Point", Range(-1,1)) = 0
        [Sub(_LightingStyleFaceOverrideGroup)]_AdditionalLightCelShadeSoftnessForFaceArea("    Softness", Range(0.001,1)) = 0.15 // avoid 0
        [Tooltip(Increase this a bit can produce fake SSS, or fake indirect light.)]
        [Sub(_LightingStyleFaceOverrideGroup)]_AdditionalLightIgnoreCelShadeForFaceArea("    Remove shadow", Range(0,1)) = 0.2 // face can be bright, but too much  dark = dirty face
        
        /*
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Core Settings (Optional))]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        */

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(UV)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // UV Edit
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Main(_UVEditGroup,_)]_EnableUVEditGroup("UV Edit", Float) = 0
        [Helpbox(Any features using UV0 will be affected)]
        
        [Advanced(UV0)]
        [Title(_UVEditGroup, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled by this UV.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled by this UV.)]
        [Sub(_UVEditGroup)]_UV0CenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV0)]
        [Title(_UVEditGroup, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the UV Tiling in a classic Unity way.)]
        [Tooltip((z,w) controls the UV Offset in a classic Unity way.)]
        [Sub(_UVEditGroup)]_UV0ScaleOffset("", Vector) = (1,1,0,0)

        [Advanced(UV0)]
        [Title(_UVEditGroup, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scroll speed, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_UVEditGroup)]_UV0ScrollSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV0)]
        [Title(_UVEditGroup, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_UVEditGroup)]_UV0RotatedAngle("Angle", Float) = 0
        [Advanced(UV0)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_UVEditGroup)]_UV0RotateSpeed("Speed", Float) = 0
        
        //------------------------------
        [Helpbox(Any features using UV1 will be affected)]
        
        [Advanced(UV1)]
        [Title(_UVEditGroup, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled by this UV.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled by this UV.)]
        [Sub(_UVEditGroup)]_UV1CenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV1)]
        [Title(_UVEditGroup, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the UV Tiling in a classic Unity way.)]
        [Tooltip((z,w) controls the UV Offset in a classic Unity way.)]
        [Sub(_UVEditGroup)]_UV1ScaleOffset("", Vector) = (1,1,0,0)
        
        [Advanced(UV1)]
        [Title(_UVEditGroup, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scroll speed, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_UVEditGroup)]_UV1ScrollSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV1)]
        [Title(_UVEditGroup, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_UVEditGroup)]_UV1RotatedAngle("Angle", Float) = 0
        [Advanced(UV1)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_UVEditGroup)]_UV1RotateSpeed("Speed", Float) = 0
        
        //------------------------------
        [Helpbox(Any features using UV2 will be affected)]
        
        [Advanced(UV2)]
        [Title(_UVEditGroup, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled by this UV.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled by this UV.)]
        [Sub(_UVEditGroup)]_UV2CenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV2)]
        [Title(_UVEditGroup, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the UV Tiling in a classic Unity way.)]
        [Tooltip((z,w) controls the UV Offset in a classic Unity way.)]
        [Sub(_UVEditGroup)]_UV2ScaleOffset("", Vector) = (1,1,0,0)
        
        [Advanced(UV2)]
        [Title(_UVEditGroup, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scroll speed, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_UVEditGroup)]_UV2ScrollSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV2)]
        [Title(_UVEditGroup, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_UVEditGroup)]_UV2RotatedAngle("Angle", Float) = 0
        [Advanced(UV2)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_UVEditGroup)]_UV2RotateSpeed("Speed", Float) = 0
        
        //------------------------------
        [Helpbox(Any features using UV3 will be affected)]
        
        [Advanced(UV3)]
        [Title(_UVEditGroup, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled by this UV.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled by this UV.)]
        [Sub(_UVEditGroup)]_UV3CenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV3)]
        [Title(_UVEditGroup, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the UV Tiling in a classic Unity way.)]
        [Tooltip((z,w) controls the UV Offset in a classic Unity way.)]
        [Sub(_UVEditGroup)]_UV3ScaleOffset("", Vector) = (1,1,0,0)
        
        [Advanced(UV3)]
        [Title(_UVEditGroup, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scroll speed, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_UVEditGroup)]_UV3ScrollSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV3)]
        [Title(_UVEditGroup, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_UVEditGroup)]_UV3RotatedAngle("Angle", Float) = 0
        [Advanced(UV3)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_UVEditGroup)]_UV3RotateSpeed("Speed", Float) = 0
        
        //------------------------------
        [Helpbox(Any features using MatCapUV will be affected)]
        
        [Advanced(MatCapUV)]
        [Title(_UVEditGroup, Tiling(xy)))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled by this UV.)]
        [Tooltip(e.g., set to negative will flip the MatCapUV)]
        [Sub(_UVEditGroup)]_MatCapUVTiling("", Vector) = (1,1,0,0)
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Render States)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Stencil Buffer
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Tooltip(Stencil Settings of all color pass(Lit pass and Outline pass).)]
        [Main(_StencilGroup,_,off,off)]_StencilGroup("Stencil Buffer", Float) = 0

        [Helpbox(Most of the time, the use of Stencil is to make the eye render on top of hair.)]
        [Helpbox(You have to make sure the renderqueues are all correctly ordered between all related materials.)]
        
        [Title(Comp and Pass)]
        [Tooltip(In most cases, user use Stencil only for drawing eye on hair, so we provided 3 common presets for you to handle the rendering of eye and hair.)]
        [Preset(_StencilGroup, NiloToonCharacter_StencilPreset_LWGUI_ShaderPropertyPreset)] _StencilPreset ("Preset", float) = 0 
        // https://docs.unity3d.com/Manual/SL-Stencil.html
        // https://docs.unity3d.com/ScriptReference/Rendering.CompareFunction.html
        // https://docs.unity3d.com/ScriptReference/Rendering.StencilOp.html
        
        [Tooltip(The operation that the GPU performs for the stencil test for all pixels.)]
        [Tooltip(This defines the operation for all pixels, regardless of facing.)]
        [SubEnumDrawer(_StencilGroup,UnityEngine.Rendering.CompareFunction)]_StencilComp(".    Comp", Float) = 0 // Default = Disabled
        
        [Tooltip(The operation that the GPU performs on the stencil buffer when a pixel pases both the stencil test and the depth test.)]
        [Tooltip(This defines the operation for all pixels, regardless of facing.)]
        [SubEnumDrawer(_StencilGroup,UnityEngine.Rendering.StencilOp)]_StencilPass(".    Pass", Float) = 0 // Default = Keep
        
        [Title(Ref ID)]
        [Tooltip(The reference value (Must be an integer).)]
        [Tooltip(The GPU compares the current contents of the stencil buffer against this value, using the operation defined in Comp.)]
        [Tooltip(The GPU can also write this value to the stencil buffer, if Pass, Fail or ZFail have a value of Replace.)]
        [SubIntRange(_StencilGroup)]_StencilRef("Ref", Range(0,127)) = 0
        
        // We currently don't see any great value of adding a separated stencil control for "Classic Outline" pass, 
        // other than breaking existing materials, making trouble to the user and adding too much complexity to the usage of stencil,
        // so it is not exposed to user now.
        // *If you absolutely need this function, you can re-enable it and switch the stencil param of "Classic Outline" pass to the params below.
        /*
        [Title(Stencil(Outline Override))]
        [Tooltip(The reference value.)]
        [Tooltip(The GPU compares the current contents of the stencil buffer against this value, using the operation defined in Comp.)]
        [Tooltip(The GPU can also write this value to the stencil buffer, if Pass, Fail or ZFail have a value of Replace.)]
        [SubIntRange(_StencilGroup)](_StencilRefOutline("Ref", Range(0,127)) = 0
        [Tooltip(The operation that the GPU performs for the stencil test for all pixels.)]
        [Tooltip(This defines the operation for all pixels, regardless of facing.)]
        [Enum(UnityEngine.Rendering.CompareFunction)]_StencilCompOutline("Comp", Float) = 0 // Default = Disabled
        [Tooltip(The operation that the GPU performs on the stencil buffer when a pixel pases both the stencil test and the depth test.)]
        [Tooltip(This defines the operation for all pixels, regardless of facing.)]
        [Enum(UnityEngine.Rendering.StencilOp)]_StencilPassOutline("Pass", Float) = 0 // Default = Keep
        */
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Color buffer
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Controls Render States related to color buffer, like Blending and ColorMask.)]
        [Main(_ColorRenderStatesGroup,_,off,off)]_ColorRenderStatesGroup("Color Buffer", Float) = 0

        [Title(_ColorRenderStatesGroup, Blend Settings)]
        [Preset(_ColorRenderStatesGroup, NiloToonCharacter_ColorBufferBlendingPreset_LWGUI_ShaderPropertyPreset)] _ColorRenderStatesGroupPreset ("Preset", float) = 0 

        // https://docs.unity3d.com/ScriptReference/Rendering.BlendMode.html
        // https://docs.unity3d.com/Manual/SL-Blend.html
        // Source Factor
        [Tooltip(In most cases, you do NOT need to edit this, since selecting a Surface Type Preset will handle this for you already.)]
        [Tooltip()]
        [Tooltip(Determines how the GPU combines the output of the fragment shader with the render target.)]
        [Tooltip(This section will only affect ForwardLit color pass.)]
        [Tooltip()]
        [Tooltip(For example, you can use)]
        [Tooltip(.    One,Zero for Opaque.)]
        [Tooltip(.    SrcAlpha,OneMinusSrcAlpha for Semi Transparency.)]
        [Tooltip(.    One,One for Additive.)]
        [SubEnumDrawer(_ColorRenderStatesGroup,UnityEngine.Rendering.BlendMode)]_SrcBlend(".    Source Factor", Float) = 1
        // Destination Factor
        [Tooltip(In most cases, you do NOT need to edit this, since selecting a Surface Type Preset will handle this for you already.)]
        [Tooltip()]
        [Tooltip(Determines how the GPU combines the output of the fragment shader with the render target.)]
        [Tooltip(This section will only affect ForwardLit color pass.)]
        [Tooltip()]
        [Tooltip(For example, you can use)]
        [Tooltip(.    One,Zero for Opaque.)]
        [Tooltip(.    SrcAlpha,OneMinusSrcAlpha for Semi Transparency.)]
        [Tooltip(.    One,One for Additive.)]
        [SubEnumDrawer(_ColorRenderStatesGroup,UnityEngine.Rendering.BlendMode)]_DstBlend(".    Destination Factor", Float) = 0
        
        [SubToggle(_ColorRenderStatesGroup, _)]_PreMultiplyAlphaIntoRGBOutput(".    Pre Multiply Alpha to RGB?", Float) = 0

        [Title(_ColorRenderStatesGroup,Color Mask)]
        // https://docs.unity3d.com/Manual/SL-ColorMask.html
        // not using [Enum(UnityEngine.Rendering.ColorWriteMask)]
        // https://docs.unity3d.com/ScriptReference/Rendering.ColorWriteMask.html,
        // because we can't select RGB if we use Unity's ColorWriteMask.
        // So here we define a few custom enum
        // 15 = binary 1111 (RGBA)
        // 14 = binary 1110 (RGB_)
        // 01 = binary 0001 (___A)
        // 00 = binary 0000 (____)
        [Tooltip(In most cases, you do NOT need to edit this, but we still provide Color Mask control in case you need it.)]
        [Tooltip()]
        [Tooltip(Sets the color channel writing mask, which prevents the GPU from writing to channels in the render target.)]
        [Tooltip()]
        [Tooltip(You can set Color Mask to)]
        [Tooltip(.    RGBA for most cases.)]
        [Tooltip(.    RGB if you do not want to write to alpha channel of the drawing render target.)]
        [Tooltip(.    A if you only want to write to alpha channel of the drawing render target.)]
        [Tooltip(.    None if you do not want to write to any rgba channels of the drawing render target, for example, when you only want to write to depth buffer and keeping the color buffer (rgba) unchange.)]
        [SubEnumDrawer(_ColorRenderStatesGroup,RGBA,15,RGB,14,A,1,None,0)]_ColorMask("Write to which Channel?", Float) = 15 // 15 is RGBA (binary 1111)

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Depth Buffer
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(Controls Render States related to depth buffer, like ZWrite and ZTest.)]
        [Main(_DepthRenderStatesGroup,_,off,off)]_DepthRenderStatesGroup("Depth Buffer", Float) = 0

        [Title(_DepthRenderStatesGroup,ZWrite)]
        // https://docs.unity3d.com/Manual/SL-ZWrite.html
        [Tooltip(In most cases, you do NOT need to edit this, since selecting a Surface Type Preset will handle this for you already.)]
        [Tooltip()]
        [Tooltip(Sets whether the depth buffer contents are updated during rendering.)]
        [Tooltip()]
        [Tooltip(.    If enabled, this material writes into the depth buffer.)]
        [Tooltip(.    If disabled, this material does NOT write into the depth buffer.)]
        [Tooltip()]
        [Tooltip(Changing Surface Type Preset will turn this off if the selected preset is Transparent (without ZWrite).)]
        [SubToggle(_DepthRenderStatesGroup,_)]_ZWrite("Write to depth buffer?", Float) = 1

        [Title(_DepthRenderStatesGroup,ZTest)]
        // https://docs.unity3d.com/Manual/SL-ZTest.html
        // https://docs.unity3d.com/ScriptReference/Rendering.CompareFunction.html
        [Tooltip(In most cases, you do NOT need to edit this, but we still provide ZTest control in case you need it.)]
        [Tooltip()]
        [Tooltip(Sets the conditions under which geometry passes or fails depth testing.)]
        [Tooltip()]
        [Tooltip(Default LessEqual.)] // LWGUI can't show the correct default value, so we write it again.
        [SubEnumDrawer(_DepthRenderStatesGroup,UnityEngine.Rendering.CompareFunction)]_ZTest("Depth test pass condition", Float) = 4 // 4 is LEqual

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Edit Alpha channel and Alpha Clipping)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // BaseMap Alpha Modify
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Tooltip(When enabled, will modify the alpha channel of Base Map by a texture.)]
        [Main(_BaseMapAlphaOverrideGroup, _ALPHAOVERRIDEMAP)]_UseAlphaOverrideTex("BaseMap Alpha Modify", Float) = 0

        [Helpbox(Commonly used for adding a bit of transparancy for front hair material.)]
        [Helpbox(Or use it when the RGB and alpha are separated textures.)]
        
        [Header(Blend Mode)]
        [Tooltip(How Alpha Modfiy Tex should be used to modify the Alpha channel of BaseMap.)]
        [Tooltip(.     Replace, replace the content of BaseMap Alpha. Useful when you want to override the alpha of BaseMap.)]
        [Tooltip(.     Multiply, multiply with the content of BaseMap Alpha. Useful when you want to create semi transparent hair or cloth.)]
        [Tooltip(.     Add, add to the content of BaseMap Alpha. Can be useful if you want to create a more Opaque alpha.)]
        [SubEnum(_BaseMapAlphaOverrideGroup,Replace,0,Multiply,1,Add,2,Subtract,3)]_AlphaOverrideMode("Mode", Float) = 0
        
        [Title(_BaseMapAlphaOverrideGroup, Overall Strength)]
        [Tooltip(Controls the strength of this section(BaseMap Alpha Modify). (e.g. How much should the BaseMap alpha be overridden or modified by the Alpha Modify Tex))]
        [Tooltip(.     0 means NOT modified at all)]
        [Tooltip(.     1 means apply the modify fully)]
        [Sub(_BaseMapAlphaOverrideGroup)]_AlphaOverrideStrength("Strength", Range(0.0, 1.0)) = 1
        
        [AdvancedHeaderProperty]
        [Title(_BaseMapAlphaOverrideGroup, Alpha Modify Tex)]
        [Tooltip(Depending on the selected conversion method, the shader will first convert the Alpha Modify Tex into a grayscale value. Then, it will use this value to modify the BaseMap alpha based on the selected Mode.)]
        [Tex(_BaseMapAlphaOverrideGroup,_AlphaOverrideTexChannelMask)][NoScaleOffset]_AlphaOverrideTex("Alpha Modify Tex", 2D) = "white" {}
        [Advanced][HideInInspector]_AlphaOverrideTexChannelMask("_AlphaOverrideTexChannelMask", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is usually 1 bit better when texture is compressed
        
        [Advanced]
        [Tooltip(Select which UV channel should be used to sample Alpha Modify Tex.)]
        [Tooltip()]
        [Tooltip(In most cases, you can keep it as default value UV0, unless your mesh has multiple UV groups.)]
        [SubEnum(_BaseMapAlphaOverrideGroup,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_AlphaOverrideTexUVIndex("            UV", Float) = 0

        [Advanced]
        [SubToggle(_BaseMapAlphaOverrideGroup, _)]_AlphaOverrideTexInvertColor("            Invert?", Float) = 0
        [Title(.           Advanced Remap)]
        [Advanced]
        [Sub(_BaseMapAlphaOverrideGroup)]_AlphaOverrideTexValueScale("            Scale", Float) = 1
        [Advanced]
        [Sub(_BaseMapAlphaOverrideGroup)]_AlphaOverrideTexValueOffset("            Offset", Float) = 0
        
        [Advanced(Semi transparent front hair)]
        [Title(_BaseMapAlphaOverrideGroup, Semi transparent front hair)]
        [Tooltip(Controls if this group (alpha modify) should only apply when Face Forward is pointing to camera.)]
        [Tooltip(Usually set to 1 if this alpha override group is for semi transparent front hair.)]
        [Tooltip()]
        [Tooltip(Setting to 1 may prevent the problem of see through all hairs inside semi transparent front hair, while viewed at some non front camera angle.)]
        [Sub(_BaseMapAlphaOverrideGroup)]_ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCamera("Is front hair? (see Tooltip)", Range(0.0, 1.0)) = 0
        
        [MinMaxSlider(_BaseMapAlphaOverrideGroup,_ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCameraRemapStart,_ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCameraRemapEnd)]_ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCameraRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCameraRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_ApplyAlphaOverrideOnlyWhenFaceForwardIsPointingToCameraRemapEnd("", Range(0.0,1.0)) = 1.0
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Alpha Clipping
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Tooltip(Same as the Alpha Clipping of URP Lit shader.)]
        [Tooltip()]
        [Tooltip(Enable this will make your Material act like a Cutout Shader. Use this to create a transparent effect with hard edges between the opaque and transparent areas.)]
        [Tooltip()]
        [Tooltip(For example, adding lace holes or cutout to an Opaque cloth material.)]
        [Main(_AlphaClippingGroup,_ALPHATEST_ON)]_AlphaClip("Alpha Clipping", Float) = 0

        [Helpbox(Same as the Alpha Clipping of URP Lit shader.)]
        
        [Title(_AlphaClippingGroup, Cutout Threshold)]
        [Tooltip(All pixels above your threshold will render as usual, and all pixels below your threshold are invisible.)]
        [Tooltip(For example, a threshold of 0.1 means that this material does not render when alpha values of that pixel is below 0.1.)]
        [Sub(_AlphaClippingGroup)]_Cutoff("Threshold", Range(0.0, 1.0)) = 0.5

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Override Alpha Output
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Hidden]
        [Tooltip(In most cases, you do NOT need to enable this.)]
        [Tooltip(Because when _SrcBlend is One and _DstBlend is Zero, shader will assume the material is Opaque and override the alpha output of fragment shader to 1 automatically.)]
        [Tooltip()]
        [Tooltip(If enabled, the Alpha Output value of the fragment shader to RenderTarget(RenderTexture) can be overridden by the options in this section.)]
        [Tooltip()]
        [Tooltip(If not enabled, the Alpha Output value of the fragment shader to RenderTarget(RenderTexture) will be decided by the shader, which is,)]
        [Tooltip(.     1 for Opauqe)] 
        [Tooltip(.     value of Base Map alpha (with any other alpha modifications you added) for Transparent.)]
        [Main(_FinalOutputAlphaGroup,_)]_EditFinalOutputAlphaEnable("Override Alpha Output", Float) = 0

        [Title(_FinalOutputAlphaGroup,Override Alpha Output)]
        [Tooltip(If this material meets all of the following conditions,)] 
        [Tooltip(.    expected to be Opaque)]
        [Tooltip(.    is using a custom Blending State that is not One,Zero,)]
        [Tooltip(.    is rendering into a RerderTarget(RenderTexture), where the alpha of RerderTarget(RenderTexture) is used as Character Mask.)]
        [Tooltip()]
        [Tooltip(you can enable this toggle to force alpha write to that RerderTarget(RenderTexture) always equals 1.)]
        [Tooltip()]
        [Tooltip(Useful if you need to use the alpha channel of RerderTarget(RenderTexture) as UGUI RawImage alpha or mask,)]
        [Tooltip(or outputing a masked character result to OBS directly.)]
        [SubToggle(_FinalOutputAlphaGroup, _)]_ForceFinalOutputAlphaEqualsOne("Force Alpha Output = 1?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Offset vertex Z sorting)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Z Offset (eyebrow)
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        [Tooltip(Offset vertex shader Clip space depth value (SV_POSITION.z).)] 
        [Tooltip(Useful for pushing eyebrow on top of hair or pushing special face expression over cheek.)]
        [Main(_ZOffsetGroup,_)]_ZOffsetEnable("Z Offset (eyebrow)", Float) = 0

        [Helpbox(Usually for pushing eyebrow on top of the hair.)]
        
        [Title(_ZOffsetGroup, Preset)]
        [Preset(_ZOffsetGroup, NiloToonCharacter_ZOffsetPreset_LWGUI_ShaderPropertyPreset)]_ZOffsetPreset("Preset", Float) = 0

        [Title(_ZOffsetGroup, ZOffset)]
        [Tooltip(Offset vertex shader Clip space depth value (SV_POSITION.z), 1 ZOffset is 1m in view space.)]
        [Tooltip(The slider range is 2m, which should be more than enough for character.)] 
        [Tooltip(Z Offset will only modify clip space position.z of the vertex, so modify Z Offset will not change the vertex XY position on screen, it will only affect depth buffer Z value (affects per pixel depth sorting).)]
        [Tooltip()]
        [Tooltip(Negative 0.025 to Negative 0.1 is a good range for eyebrow rendering on top of hair.)]
        [Sub(_ZOffsetGroup)]_ZOffset("Z Offset", Range(-2,2)) = 0.0

        [Tooltip(You can control the Z Offset multipler for Classic Outline pass.)]
        [Sub(_ZOffsetGroup)]_ZOffsetMultiplierForTraditionalOutlinePass("    Multiplier(Outline)", Range(0,2)) = 1

        [Title(_ZOffsetGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_ZOffsetGroup, Mask)]
        [Tooltip(Enable a mask to select which vertices to apply ZOffset.)]
        [SubToggle(_ZOffsetGroup,_ZOFFSETMAP)]_UseZOffsetMaskTex("Enable Mask?", Float) = 0
        
        [Tooltip(Controls Z Offset by a mask map.)]
        [Tooltip(.    White is apply Z Offset)]
        [Tooltip(.    Darker is apply less Z Offset)]
        [Tooltip(.    Black is ignore Z Offset)]
        [Tex(_ZOffsetGroup,_ZOffsetMaskMapChannelMask)][NoScaleOffset]_ZOffsetMaskTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_ZOffsetMaskMapChannelMask("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is usually 1 bit better when texture is compressed
        [SubToggle(_ZOffsetGroup, _)]_ZOffsetMaskMapInvertColor("               Invert?", Float) = 0
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Adding Base Map Stacking Layers)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Base Map Stacking Layer 1
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //[ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Just like adding a Photoshop layer on top of Base Map.)]
        [Tooltip(Useful if you need to overlay a texture on top of Base Map. For example, if you want to add)]
        [Tooltip(.    makeup on face)]
        [Tooltip(.    decal)]
        [Tooltip(.    logo)]
        [Tooltip(.    tattoo)]
        [Tooltip(.    texture uv animation loop effect)]
        [Tooltip(.    (Any effect that is suitable))]
        [Main(_BaseMapStackingLayer1Group, _BASEMAP_STACKING_LAYER1)]_BaseMapStackingLayer1Enable("Base Map Stacking Layer 1", Float) = 0
        
        [Helpbox(Wrap mode of Layer Map is controlled by its texture import settings.)]
                
        [Title(_BaseMapStackingLayer1Group, Master Control)]
        [Sub(_BaseMapStackingLayer1Group)]_BaseMapStackingLayer1MasterStrength("Strength", Range(0,1)) = 1

        [Title(_BaseMapStackingLayer1Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer1Group, Blending Mode)]
        [Tooltip(Same as the blending mode in Photoshop.)]
        [Tooltip(Decide how this layer should combine with Base Map.)]
        [Tooltip(.     Normal RGBA, same as the normal blending layer in Photoshop, will modify the alpha of Base Map also.)]
        [Tooltip(.     Normal RGB, replace the RGB of Base Map, will not modify the alpha of the Base Map.)]
        [Tooltip(.     Add RGB, add to the RGB color of Base Map. Great for emissive.)]
        [Tooltip(.     Screen RGB, soft additive with the RGB color of Base Map. Only works if the blending colors are both within LDR 0 to 1 range.)]
        [Tooltip(.     Multiply RGB, multiply with the RGB color of Base Map.)]
        [SubEnum(_BaseMapStackingLayer1Group,Normal RGBA,0,Normal RGB,1,Add RGB,2,Screen RGB,3,Multiply RGB,4,None,5)]_BaseMapStackingLayer1ColorBlendMode("Blending Mode", Float) = 0
        
        [Title(_BaseMapStackingLayer1Group, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_BaseMapStackingLayer1Group, Layer Content)]
        [Tooltip(A map that contains the content of this layer, same as the content of a Photoshop layer.)]
        [Tex(_BaseMapStackingLayer1Group,_BaseMapStackingLayer1TintColor)][NoScaleOffset]_BaseMapStackingLayer1Tex("Layer Map", 2D) = "white" {}
        [HideInInspector][HDR]_BaseMapStackingLayer1TintColor("", Color) = (1,1,1,1)
        [Tooltip(Which UV channel should be used for sampling Layer Map.)]
        [SubEnum(_BaseMapStackingLayer1Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer1TexUVIndex("               UV Mode", Float) = 0
        [Tooltip(Enable to ignore the alpha channel of Layer Map. (Treat the alpha channel of Layer Map as 1))]
        [SubToggle(_BaseMapStackingLayer1Group, _)]_BaseMapStackingLayer1TexIgnoreAlpha("               Ignore Map Alpha?", Float) = 0
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer1Group, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled.)]
        [Sub(_BaseMapStackingLayer1Group)]_BaseMapStackingLayer1TexUVCenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer1Group, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the classic UV Tiling of Layer Map.)]
        [Tooltip((z,w) controls the classic UV Offset of Layer Map.)]
        [Sub(_BaseMapStackingLayer1Group)]_BaseMapStackingLayer1TexUVScaleOffset("", Vector) = (1,1,0,0)

        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer1Group, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scrolling speed of Layer Map, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_BaseMapStackingLayer1Group)]_BaseMapStackingLayer1TexUVAnimSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer1Group, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_BaseMapStackingLayer1Group)]_BaseMapStackingLayer1TexUVRotatedAngle("Angle", Float) = 0
        [Advanced(UV Edit)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_BaseMapStackingLayer1Group)]_BaseMapStackingLayer1TexUVRotateSpeed("Speed", Float) = 0
        
        [Title(_BaseMapStackingLayer1Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer1Group, Mask)]
        [Tooltip(Controls which area this layer is allowed to show, similar to the layer mask of a Photoshop layer.)]
        [Tooltip(.    White is show)]
        [Tooltip(.    Gray is show(with 50 percent alpha))]
        [Tooltip(.    Black is hide)]
        [Tex(_BaseMapStackingLayer1Group,_BaseMapStackingLayer1MaskTexChannel)][NoScaleOffset]_BaseMapStackingLayer1MaskTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_BaseMapStackingLayer1MaskTexChannel("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubEnum(_BaseMapStackingLayer1Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer1MaskUVIndex("               UV Mode", Float) = 0
        
        [SubToggle(_BaseMapStackingLayer1Group, _)]_BaseMapStackingLayer1MaskTexAsIDMap("               Extract from ID?", Float) = 0
        [ShowIf(_BaseMapStackingLayer1MaskTexAsIDMap, Equal, 1)]
        [SubIntRange(_BaseMapStackingLayer1Group)]_BaseMapStackingLayer1MaskTexExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [Tooltip(Enable to invert the value of Mask Map.)]
        [SubToggle(_BaseMapStackingLayer1Group, _)]_BaseMapStackingLayer1MaskInvertColor("               Invert?", Float) = 0

        [MinMaxSlider(_BaseMapStackingLayer1Group,_BaseMapStackingLayer1MaskRemapStart,_BaseMapStackingLayer1MaskRemapEnd)]_BaseMapStackingLayer1MaskRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_BaseMapStackingLayer1MaskRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_BaseMapStackingLayer1MaskRemapEnd("", Range(0.0,1.0)) = 1.0
        
        [Title(_BaseMapStackingLayer1Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer1Group, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show this layer.)]
        [SubEnum(_BaseMapStackingLayer1Group,Both,0,Front,2,Back,1)]_BaseMapStackingLayer1ApplytoFaces("Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Base Map Stacking Layer 2
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //[ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Just like adding a Photoshop layer on top of Base Map.)]
        [Tooltip(Useful if you need to overlay a texture on top of Base Map. For example, if you want to add)]
        [Tooltip(.    makeup on face)]
        [Tooltip(.    decal)]
        [Tooltip(.    logo)]
        [Tooltip(.    tattoo)]
        [Tooltip(.    texture uv animation loop effect)]
        [Tooltip(.    (Any effect that is suitable))]
        [Main(_BaseMapStackingLayer2Group, _BASEMAP_STACKING_LAYER2)]_BaseMapStackingLayer2Enable("Base Map Stacking Layer 2", Float) = 0
        
        [Helpbox(Wrap mode of Layer Map is controlled by its texture import settings.)]
                
        [Title(_BaseMapStackingLayer2Group, Master Control)]
        [Sub(_BaseMapStackingLayer2Group)]_BaseMapStackingLayer2MasterStrength("Strength", Range(0,1)) = 1

        [Title(_BaseMapStackingLayer2Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer2Group, Blending Mode)]
        [Tooltip(Same as the blending mode in Photoshop.)]
        [Tooltip(Decide how this layer should combine with Base Map.)]
        [Tooltip(.     Normal RGBA, same as the normal blending layer in Photoshop, will modify the alpha of Base Map also.)]
        [Tooltip(.     Normal RGB, replace the RGB of Base Map, will not modify the alpha of the Base Map.)]
        [Tooltip(.     Add RGB, add to the RGB color of Base Map. Great for emissive.)]
        [Tooltip(.     Screen RGB, soft additive with the RGB color of Base Map. Only works if the blending colors are both within LDR 0 to 1 range.)]
        [Tooltip(.     Multiply RGB, multiply with the RGB color of Base Map.)]
        [SubEnum(_BaseMapStackingLayer2Group,Normal RGBA,0,Normal RGB,1,Add RGB,2,Screen RGB,3,Multiply RGB,4,None,5)]_BaseMapStackingLayer2ColorBlendMode("Blending Mode", Float) = 0
        
        [Title(_BaseMapStackingLayer2Group, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_BaseMapStackingLayer2Group, Layer Content)]
        [Tooltip(A map that contains the content of this layer, same as the content of a Photoshop layer.)]
        [Tex(_BaseMapStackingLayer2Group,_BaseMapStackingLayer2TintColor)][NoScaleOffset]_BaseMapStackingLayer2Tex("Layer Map", 2D) = "white" {}
        [HideInInspector][HDR]_BaseMapStackingLayer2TintColor("", Color) = (1,1,1,1)
        [Tooltip(Which UV channel should be used for sampling Layer Map.)]
        [SubEnum(_BaseMapStackingLayer2Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer2TexUVIndex("               UV Mode", Float) = 0
        [Tooltip(Enable to ignore the alpha channel of Layer Map. (Treat the alpha channel of Layer Map as 1))]
        [SubToggle(_BaseMapStackingLayer2Group, _)]_BaseMapStackingLayer2TexIgnoreAlpha("               Ignore Map Alpha?", Float) = 0
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer2Group, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled.)]
        [Sub(_BaseMapStackingLayer2Group)]_BaseMapStackingLayer2TexUVCenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer2Group, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the classic UV Tiling of Layer Map.)]
        [Tooltip((z,w) controls the classic UV Offset of Layer Map.)]
        [Sub(_BaseMapStackingLayer2Group)]_BaseMapStackingLayer2TexUVScaleOffset("", Vector) = (1,1,0,0)

        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer2Group, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scrolling speed of Layer Map, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_BaseMapStackingLayer2Group)]_BaseMapStackingLayer2TexUVAnimSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer2Group, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_BaseMapStackingLayer2Group)]_BaseMapStackingLayer2TexUVRotatedAngle("Angle", Float) = 0
        [Advanced(UV Edit)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_BaseMapStackingLayer2Group)]_BaseMapStackingLayer2TexUVRotateSpeed("Speed", Float) = 0
        
        [Title(_BaseMapStackingLayer2Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer2Group, Mask)]
        [Tooltip(Controls which area this layer is allowed to show, similar to the layer mask of a Photoshop layer.)]
        [Tooltip(.    White is show)]
        [Tooltip(.    Gray is show(with 50 percent alpha))]
        [Tooltip(.    Black is hide)]
        [Tex(_BaseMapStackingLayer2Group,_BaseMapStackingLayer2MaskTexChannel)][NoScaleOffset]_BaseMapStackingLayer2MaskTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_BaseMapStackingLayer2MaskTexChannel("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubEnum(_BaseMapStackingLayer2Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer2MaskUVIndex("               UV Mode", Float) = 0
        
        [SubToggle(_BaseMapStackingLayer2Group, _)]_BaseMapStackingLayer2MaskTexAsIDMap("               Extract from ID?", Float) = 0
        [ShowIf(_BaseMapStackingLayer2MaskTexAsIDMap, Equal, 1)]
        [SubIntRange(_BaseMapStackingLayer2Group)]_BaseMapStackingLayer2MaskTexExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [Tooltip(Enable to invert the value of Mask Map.)]
        [SubToggle(_BaseMapStackingLayer2Group, _)]_BaseMapStackingLayer2MaskInvertColor("               Invert?", Float) = 0
        
        [MinMaxSlider(_BaseMapStackingLayer2Group,_BaseMapStackingLayer2MaskRemapStart,_BaseMapStackingLayer2MaskRemapEnd)]_BaseMapStackingLayer2MaskRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_BaseMapStackingLayer2MaskRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_BaseMapStackingLayer2MaskRemapEnd("", Range(0.0,1.0)) = 1.0

        [Title(_BaseMapStackingLayer2Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer2Group, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show this layer.)]
        [SubEnum(_BaseMapStackingLayer2Group,Both,0,Front,2,Back,1)]_BaseMapStackingLayer2ApplytoFaces("Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Base Map Stacking Layer 3
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //[ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Just like adding a Photoshop layer on top of Base Map.)]
        [Tooltip(Useful if you need to overlay a texture on top of Base Map. For example, if you want to add)]
        [Tooltip(.    makeup on face)]
        [Tooltip(.    decal)]
        [Tooltip(.    logo)]
        [Tooltip(.    tattoo)]
        [Tooltip(.    texture uv animation loop effect)]
        [Tooltip(.    (Any effect that is suitable))]
        [Main(_BaseMapStackingLayer3Group, _BASEMAP_STACKING_LAYER3)]_BaseMapStackingLayer3Enable("Base Map Stacking Layer 3", Float) = 0
        
        [Helpbox(Layer Map Wrap mode is linear clamp (not texture import settings).)]
                
        [Title(_BaseMapStackingLayer3Group, Master Control)]
        [Sub(_BaseMapStackingLayer3Group)]_BaseMapStackingLayer3MasterStrength("Strength", Range(0,1)) = 1

        [Title(_BaseMapStackingLayer3Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer3Group, Blending Mode)]
        [Tooltip(Same as the blending mode in Photoshop.)]
        [Tooltip(Decide how this layer should combine with Base Map.)]
        [Tooltip(.     Normal RGBA, same as the normal blending layer in Photoshop, will modify the alpha of Base Map also.)]
        [Tooltip(.     Normal RGB, replace the RGB of Base Map, will not modify the alpha of the Base Map.)]
        [Tooltip(.     Add RGB, add to the RGB color of Base Map. Great for emissive.)]
        [Tooltip(.     Screen RGB, soft additive with the RGB color of Base Map. Only works if the blending colors are both within LDR 0 to 1 range.)]
        [Tooltip(.     Multiply RGB, multiply with the RGB color of Base Map.)]
        [SubEnum(_BaseMapStackingLayer3Group,Normal RGBA,0,Normal RGB,1,Add RGB,2,Screen RGB,3,Multiply RGB,4,None,5)]_BaseMapStackingLayer3ColorBlendMode("Blending Mode", Float) = 0
        
        [Title(_BaseMapStackingLayer3Group, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_BaseMapStackingLayer3Group, Layer Content)]
        [Tooltip(A map that contains the content of this layer, same as the content of a Photoshop layer.)]
        [Tex(_BaseMapStackingLayer3Group,_BaseMapStackingLayer3TintColor)][NoScaleOffset]_BaseMapStackingLayer3Tex("Layer Map", 2D) = "white" {}
        [HideInInspector][HDR]_BaseMapStackingLayer3TintColor("", Color) = (1,1,1,1)
        [Tooltip(Which UV channel should be used for sampling Layer Map.)]
        [SubEnum(_BaseMapStackingLayer3Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer3TexUVIndex("               UV Mode", Float) = 0
        [Tooltip(Enable to ignore the alpha channel of Layer Map. (Treat the alpha channel of Layer Map as 1))]
        [SubToggle(_BaseMapStackingLayer3Group, _)]_BaseMapStackingLayer3TexIgnoreAlpha("               Ignore Map Alpha?", Float) = 0
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer3Group, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled.)]
        [Sub(_BaseMapStackingLayer3Group)]_BaseMapStackingLayer3TexUVCenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer3Group, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the classic UV Tiling of Layer Map.)]
        [Tooltip((z,w) controls the classic UV Offset of Layer Map.)]
        [Sub(_BaseMapStackingLayer3Group)]_BaseMapStackingLayer3TexUVScaleOffset("", Vector) = (1,1,0,0)

        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer3Group, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scrolling speed of Layer Map, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_BaseMapStackingLayer3Group)]_BaseMapStackingLayer3TexUVAnimSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer3Group, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_BaseMapStackingLayer3Group)]_BaseMapStackingLayer3TexUVRotatedAngle("Angle", Float) = 0
        [Advanced(UV Edit)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_BaseMapStackingLayer3Group)]_BaseMapStackingLayer3TexUVRotateSpeed("Speed", Float) = 0
        
        [Title(_BaseMapStackingLayer3Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer3Group, Mask)]
        [Tooltip(Controls which area this layer is allowed to show, similar to the layer mask of a Photoshop layer.)]
        [Tooltip(.    White is show)]
        [Tooltip(.    Gray is show(with 50 percent alpha))]
        [Tooltip(.    Black is hide)]
        [Tex(_BaseMapStackingLayer3Group,_BaseMapStackingLayer3MaskTexChannel)][NoScaleOffset]_BaseMapStackingLayer3MaskTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_BaseMapStackingLayer3MaskTexChannel("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubEnum(_BaseMapStackingLayer3Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer3MaskUVIndex("               UV Mode", Float) = 0
        
        [SubToggle(_BaseMapStackingLayer3Group, _)]_BaseMapStackingLayer3MaskTexAsIDMap("               Extract from ID?", Float) = 0
        [ShowIf(_BaseMapStackingLayer3MaskTexAsIDMap, Equal, 1)]
        [SubIntRange(_BaseMapStackingLayer3Group)]_BaseMapStackingLayer3MaskTexExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [Tooltip(Enable to invert the value of Mask Map.)]
        [SubToggle(_BaseMapStackingLayer3Group, _)]_BaseMapStackingLayer3MaskInvertColor("               Invert?", Float) = 0

        [MinMaxSlider(_BaseMapStackingLayer3Group,_BaseMapStackingLayer3MaskRemapStart,_BaseMapStackingLayer3MaskRemapEnd)]_BaseMapStackingLayer3MaskRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_BaseMapStackingLayer3MaskRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_BaseMapStackingLayer3MaskRemapEnd("", Range(0.0,1.0)) = 1.0
        
        [Title(_BaseMapStackingLayer3Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer3Group, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show this layer.)]
        [SubEnum(_BaseMapStackingLayer3Group,Both,0,Front,2,Back,1)]_BaseMapStackingLayer3ApplytoFaces("Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Base Map Stacking Layer 4
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //[ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Just like adding a Photoshop layer on top of Base Map.)]
        [Tooltip(Useful if you need to overlay a texture on top of Base Map. For example, if you want to add)]
        [Tooltip(.    makeup on face)]
        [Tooltip(.    decal)]
        [Tooltip(.    logo)]
        [Tooltip(.    tattoo)]
        [Tooltip(.    texture uv animation loop effect)]
        [Tooltip(.    (Any effect that is suitable))]
        [Main(_BaseMapStackingLayer4Group, _BASEMAP_STACKING_LAYER4)]_BaseMapStackingLayer4Enable("Base Map Stacking Layer 4", Float) = 0
        
        [Helpbox(Layer Map Wrap mode is repeat clamp (not texture import settings).)]
                
        [Title(_BaseMapStackingLayer4Group, Master Control)]
        [Sub(_BaseMapStackingLayer4Group)]_BaseMapStackingLayer4MasterStrength("Strength", Range(0,1)) = 1

        [Title(_BaseMapStackingLayer4Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer4Group, Blending Mode)]
        [Tooltip(Same as the blending mode in Photoshop.)]
        [Tooltip(Decide how this layer should combine with Base Map.)]
        [Tooltip(.     Normal RGBA, same as the normal blending layer in Photoshop, will modify the alpha of Base Map also.)]
        [Tooltip(.     Normal RGB, replace the RGB of Base Map, will not modify the alpha of the Base Map.)]
        [Tooltip(.     Add RGB, add to the RGB color of Base Map. Great for emissive.)]
        [Tooltip(.     Screen RGB, soft additive with the RGB color of Base Map. Only works if the blending colors are both within LDR 0 to 1 range.)]
        [Tooltip(.     Multiply RGB, multiply with the RGB color of Base Map.)]
        [SubEnum(_BaseMapStackingLayer4Group,Normal RGBA,0,Normal RGB,1,Add RGB,2,Screen RGB,3,Multiply RGB,4,None,5)]_BaseMapStackingLayer4ColorBlendMode("Blending Mode", Float) = 0
        
        [Title(_BaseMapStackingLayer4Group, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_BaseMapStackingLayer4Group, Layer Content)]
        [Tooltip(A map that contains the content of this layer, same as the content of a Photoshop layer.)]
        [Tex(_BaseMapStackingLayer4Group,_BaseMapStackingLayer4TintColor)][NoScaleOffset]_BaseMapStackingLayer4Tex("Layer Map", 2D) = "white" {}
        [HideInInspector][HDR]_BaseMapStackingLayer4TintColor("", Color) = (1,1,1,1)
        [Tooltip(Which UV channel should be used for sampling Layer Map.)]
        [SubEnum(_BaseMapStackingLayer4Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer4TexUVIndex("               UV Mode", Float) = 0
        [Tooltip(Enable to ignore the alpha channel of Layer Map. (Treat the alpha channel of Layer Map as 1))]
        [SubToggle(_BaseMapStackingLayer4Group, _)]_BaseMapStackingLayer4TexIgnoreAlpha("               Ignore Map Alpha?", Float) = 0
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer4Group, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled.)]
        [Sub(_BaseMapStackingLayer4Group)]_BaseMapStackingLayer4TexUVCenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer4Group, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the classic UV Tiling of Layer Map.)]
        [Tooltip((z,w) controls the classic UV Offset of Layer Map.)]
        [Sub(_BaseMapStackingLayer4Group)]_BaseMapStackingLayer4TexUVScaleOffset("", Vector) = (1,1,0,0)

        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer4Group, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scrolling speed of Layer Map, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_BaseMapStackingLayer4Group)]_BaseMapStackingLayer4TexUVAnimSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer4Group, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_BaseMapStackingLayer4Group)]_BaseMapStackingLayer4TexUVRotatedAngle("Angle", Float) = 0
        [Advanced(UV Edit)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_BaseMapStackingLayer4Group)]_BaseMapStackingLayer4TexUVRotateSpeed("Speed", Float) = 0
        
        [Title(_BaseMapStackingLayer4Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer4Group, Mask)]
        [Tooltip(Controls which area this layer is allowed to show, similar to the layer mask of a Photoshop layer.)]
        [Tooltip(.    White is show)]
        [Tooltip(.    Gray is show(with 50 percent alpha))]
        [Tooltip(.    Black is hide)]
        [Tex(_BaseMapStackingLayer4Group,_BaseMapStackingLayer4MaskTexChannel)][NoScaleOffset]_BaseMapStackingLayer4MaskTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_BaseMapStackingLayer4MaskTexChannel("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubEnum(_BaseMapStackingLayer4Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer4MaskUVIndex("               UV Mode", Float) = 0
        
        [SubToggle(_BaseMapStackingLayer4Group, _)]_BaseMapStackingLayer4MaskTexAsIDMap("               Extract from ID?", Float) = 0
        [ShowIf(_BaseMapStackingLayer4MaskTexAsIDMap, Equal, 1)]
        [SubIntRange(_BaseMapStackingLayer4Group)]_BaseMapStackingLayer4MaskTexExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [Tooltip(Enable to invert the value of Mask Map.)]
        [SubToggle(_BaseMapStackingLayer4Group, _)]_BaseMapStackingLayer4MaskInvertColor("               Invert?", Float) = 0

        [MinMaxSlider(_BaseMapStackingLayer4Group,_BaseMapStackingLayer4MaskRemapStart,_BaseMapStackingLayer4MaskRemapEnd)]_BaseMapStackingLayer4MaskRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_BaseMapStackingLayer4MaskRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_BaseMapStackingLayer4MaskRemapEnd("", Range(0.0,1.0)) = 1.0
        
        [Title(_BaseMapStackingLayer4Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer4Group, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show this layer.)]
        [SubEnum(_BaseMapStackingLayer4Group,Both,0,Front,2,Back,1)]_BaseMapStackingLayer4ApplytoFaces("Show in which faces?", Float) = 0
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Base Map Stacking Layer 5
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(Just like adding a Photoshop layer on top of Base Map.)]
        [Tooltip(Useful if you need to overlay a texture on top of Base Map. For example, if you want to add)]
        [Tooltip(.    makeup on face)]
        [Tooltip(.    decal)]
        [Tooltip(.    logo)]
        [Tooltip(.    tattoo)]
        [Tooltip(.    texture uv animation loop effect)]
        [Tooltip(.    (Any effect that is suitable))]
        [Main(_BaseMapStackingLayer5Group, _BASEMAP_STACKING_LAYER5)]_BaseMapStackingLayer5Enable("Base Map Stacking Layer 5", Float) = 0
        
        [Helpbox(Layer Map Wrap mode is linear clamp (not texture import settings).)]
                
        [Title(_BaseMapStackingLayer5Group, Master Control)]
        [Sub(_BaseMapStackingLayer5Group)]_BaseMapStackingLayer5MasterStrength("Strength", Range(0,1)) = 1

        [Title(_BaseMapStackingLayer5Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer5Group, Blending Mode)]
        [Tooltip(Same as the blending mode in Photoshop.)]
        [Tooltip(Decide how this layer should combine with Base Map.)]
        [Tooltip(.     Normal RGBA, same as the normal blending layer in Photoshop, will modify the alpha of Base Map also.)]
        [Tooltip(.     Normal RGB, replace the RGB of Base Map, will not modify the alpha of the Base Map.)]
        [Tooltip(.     Add RGB, add to the RGB color of Base Map. Great for emissive.)]
        [Tooltip(.     Screen RGB, soft additive with the RGB color of Base Map. Only works if the blending colors are both within LDR 0 to 1 range.)]
        [Tooltip(.     Multiply RGB, multiply with the RGB color of Base Map.)]
        [SubEnum(_BaseMapStackingLayer5Group,Normal RGBA,0,Normal RGB,1,Add RGB,2,Screen RGB,3,Multiply RGB,4,None,5)]_BaseMapStackingLayer5ColorBlendMode("Blending Mode", Float) = 0
        
        [Title(_BaseMapStackingLayer5Group, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_BaseMapStackingLayer5Group, Layer Content)]
        [Tooltip(A map that contains the content of this layer, same as the content of a Photoshop layer.)]
        [Tex(_BaseMapStackingLayer5Group,_BaseMapStackingLayer5TintColor)][NoScaleOffset]_BaseMapStackingLayer5Tex("Layer Map", 2D) = "white" {}
        [HideInInspector][HDR]_BaseMapStackingLayer5TintColor("", Color) = (1,1,1,1)
        [Tooltip(Which UV channel should be used for sampling Layer Map.)]
        [SubEnum(_BaseMapStackingLayer5Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer5TexUVIndex("               UV Mode", Float) = 0
        [Tooltip(Enable to ignore the alpha channel of Layer Map. (Treat the alpha channel of Layer Map as 1))]
        [SubToggle(_BaseMapStackingLayer5Group, _)]_BaseMapStackingLayer5TexIgnoreAlpha("               Ignore Map Alpha?", Float) = 0
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer5Group, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled.)]
        [Sub(_BaseMapStackingLayer5Group)]_BaseMapStackingLayer5TexUVCenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer5Group, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the classic UV Tiling of Layer Map.)]
        [Tooltip((z,w) controls the classic UV Offset of Layer Map.)]
        [Sub(_BaseMapStackingLayer5Group)]_BaseMapStackingLayer5TexUVScaleOffset("", Vector) = (1,1,0,0)

        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer5Group, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scrolling speed of Layer Map, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_BaseMapStackingLayer5Group)]_BaseMapStackingLayer5TexUVAnimSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer5Group, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_BaseMapStackingLayer5Group)]_BaseMapStackingLayer5TexUVRotatedAngle("Angle", Float) = 0
        [Advanced(UV Edit)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_BaseMapStackingLayer5Group)]_BaseMapStackingLayer5TexUVRotateSpeed("Speed", Float) = 0
        
        [Title(_BaseMapStackingLayer5Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer5Group, Mask)]
        [Tooltip(Controls which area this layer is allowed to show, similar to the layer mask of a Photoshop layer.)]
        [Tooltip(.    White is show)]
        [Tooltip(.    Gray is show(with 50 percent alpha))]
        [Tooltip(.    Black is hide)]
        [Tex(_BaseMapStackingLayer5Group,_BaseMapStackingLayer5MaskTexChannel)][NoScaleOffset]_BaseMapStackingLayer5MaskTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_BaseMapStackingLayer5MaskTexChannel("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubEnum(_BaseMapStackingLayer5Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer5MaskUVIndex("               UV Mode", Float) = 0
        
        [SubToggle(_BaseMapStackingLayer5Group, _)]_BaseMapStackingLayer5MaskTexAsIDMap("               Extract from ID?", Float) = 0
        [ShowIf(_BaseMapStackingLayer5MaskTexAsIDMap, Equal, 1)]
        [SubIntRange(_BaseMapStackingLayer5Group)]_BaseMapStackingLayer5MaskTexExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [Tooltip(Enable to invert the value of Mask Map.)]
        [SubToggle(_BaseMapStackingLayer5Group, _)]_BaseMapStackingLayer5MaskInvertColor("               Invert?", Float) = 0

        [MinMaxSlider(_BaseMapStackingLayer5Group,_BaseMapStackingLayer5MaskRemapStart,_BaseMapStackingLayer5MaskRemapEnd)]_BaseMapStackingLayer5MaskRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_BaseMapStackingLayer5MaskRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_BaseMapStackingLayer5MaskRemapEnd("", Range(0.0,1.0)) = 1.0
        
        [Title(_BaseMapStackingLayer5Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer5Group, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show this layer.)]
        [SubEnum(_BaseMapStackingLayer5Group,Both,0,Front,2,Back,1)]_BaseMapStackingLayer5ApplytoFaces("Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Base Map Stacking Layer 6
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(Just like adding a Photoshop layer on top of Base Map.)]
        [Tooltip(Useful if you need to overlay a texture on top of Base Map. For example, if you want to add)]
        [Tooltip(.    makeup on face)]
        [Tooltip(.    decal)]
        [Tooltip(.    logo)]
        [Tooltip(.    tattoo)]
        [Tooltip(.    texture uv animation loop effect)]
        [Tooltip(.    (Any effect that is suitable))]
        [Main(_BaseMapStackingLayer6Group, _BASEMAP_STACKING_LAYER6)]_BaseMapStackingLayer6Enable("Base Map Stacking Layer 6", Float) = 0
        
        [Helpbox(Layer Map Wrap mode is repeat clamp (not texture import settings).)]
                
        [Title(_BaseMapStackingLayer6Group, Master Control)]
        [Sub(_BaseMapStackingLayer6Group)]_BaseMapStackingLayer6MasterStrength("Strength", Range(0,1)) = 1

        [Title(_BaseMapStackingLayer6Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer6Group, Blending Mode)]
        [Tooltip(Same as the blending mode in Photoshop.)]
        [Tooltip(Decide how this layer should combine with Base Map.)]
        [Tooltip(.     Normal RGBA, same as the normal blending layer in Photoshop, will modify the alpha of Base Map also.)]
        [Tooltip(.     Normal RGB, replace the RGB of Base Map, will not modify the alpha of the Base Map.)]
        [Tooltip(.     Add RGB, add to the RGB color of Base Map. Great for emissive.)]
        [Tooltip(.     Screen RGB, soft additive with the RGB color of Base Map. Only works if the blending colors are both within LDR 0 to 1 range.)]
        [Tooltip(.     Multiply RGB, multiply with the RGB color of Base Map.)]
        [SubEnum(_BaseMapStackingLayer6Group,Normal RGBA,0,Normal RGB,1,Add RGB,2,Screen RGB,3,Multiply RGB,4,None,5)]_BaseMapStackingLayer6ColorBlendMode("Blending Mode", Float) = 0
        
        [Title(_BaseMapStackingLayer6Group, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_BaseMapStackingLayer6Group, Layer Content)]
        [Tooltip(A map that contains the content of this layer, same as the content of a Photoshop layer.)]
        [Tex(_BaseMapStackingLayer6Group,_BaseMapStackingLayer6TintColor)][NoScaleOffset]_BaseMapStackingLayer6Tex("Layer Map", 2D) = "white" {}
        [HideInInspector][HDR]_BaseMapStackingLayer6TintColor("", Color) = (1,1,1,1)
        [Tooltip(Which UV channel should be used for sampling Layer Map.)]
        [SubEnum(_BaseMapStackingLayer6Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer6TexUVIndex("               UV Mode", Float) = 0
        [Tooltip(Enable to ignore the alpha channel of Layer Map. (Treat the alpha channel of Layer Map as 1))]
        [SubToggle(_BaseMapStackingLayer6Group, _)]_BaseMapStackingLayer6TexIgnoreAlpha("               Ignore Map Alpha?", Float) = 0
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer6Group, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled.)]
        [Sub(_BaseMapStackingLayer6Group)]_BaseMapStackingLayer6TexUVCenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer6Group, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the classic UV Tiling of Layer Map.)]
        [Tooltip((z,w) controls the classic UV Offset of Layer Map.)]
        [Sub(_BaseMapStackingLayer6Group)]_BaseMapStackingLayer6TexUVScaleOffset("", Vector) = (1,1,0,0)

        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer6Group, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scrolling speed of Layer Map, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_BaseMapStackingLayer6Group)]_BaseMapStackingLayer6TexUVAnimSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer6Group, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_BaseMapStackingLayer6Group)]_BaseMapStackingLayer6TexUVRotatedAngle("Angle", Float) = 0
        [Advanced(UV Edit)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_BaseMapStackingLayer6Group)]_BaseMapStackingLayer6TexUVRotateSpeed("Speed", Float) = 0
        
        [Title(_BaseMapStackingLayer6Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer6Group, Mask)]
        [Tooltip(Controls which area this layer is allowed to show, similar to the layer mask of a Photoshop layer.)]
        [Tooltip(.    White is show)]
        [Tooltip(.    Gray is show(with 50 percent alpha))]
        [Tooltip(.    Black is hide)]
        [Tex(_BaseMapStackingLayer6Group,_BaseMapStackingLayer6MaskTexChannel)][NoScaleOffset]_BaseMapStackingLayer6MaskTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_BaseMapStackingLayer6MaskTexChannel("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubEnum(_BaseMapStackingLayer6Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer6MaskUVIndex("               UV Mode", Float) = 0
        
        [SubToggle(_BaseMapStackingLayer6Group, _)]_BaseMapStackingLayer6MaskTexAsIDMap("               Extract from ID?", Float) = 0
        [ShowIf(_BaseMapStackingLayer6MaskTexAsIDMap, Equal, 1)]
        [SubIntRange(_BaseMapStackingLayer6Group)]_BaseMapStackingLayer6MaskTexExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [Tooltip(Enable to invert the value of Mask Map.)]
        [SubToggle(_BaseMapStackingLayer6Group, _)]_BaseMapStackingLayer6MaskInvertColor("               Invert?", Float) = 0

        [MinMaxSlider(_BaseMapStackingLayer6Group,_BaseMapStackingLayer6MaskRemapStart,_BaseMapStackingLayer6MaskRemapEnd)]_BaseMapStackingLayer6MaskRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_BaseMapStackingLayer6MaskRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_BaseMapStackingLayer6MaskRemapEnd("", Range(0.0,1.0)) = 1.0
        
        [Title(_BaseMapStackingLayer6Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer6Group, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show this layer.)]
        [SubEnum(_BaseMapStackingLayer6Group,Both,0,Front,2,Back,1)]_BaseMapStackingLayer6ApplytoFaces("Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Base Map Stacking Layer 7
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(Just like adding a Photoshop layer on top of Base Map.)]
        [Tooltip(Useful if you need to overlay a texture on top of Base Map. For example, if you want to add)]
        [Tooltip(.    makeup on face)]
        [Tooltip(.    decal)]
        [Tooltip(.    logo)]
        [Tooltip(.    tattoo)]
        [Tooltip(.    texture uv animation loop effect)]
        [Tooltip(.    (Any effect that is suitable))]
        [Main(_BaseMapStackingLayer7Group, _BASEMAP_STACKING_LAYER7)]_BaseMapStackingLayer7Enable("Base Map Stacking Layer 7", Float) = 0
        
        [Helpbox(Layer Map Wrap mode is linear clamp (not texture import settings).)]
                
        [Title(_BaseMapStackingLayer7Group, Master Control)]
        [Sub(_BaseMapStackingLayer7Group)]_BaseMapStackingLayer7MasterStrength("Strength", Range(0,1)) = 1

        [Title(_BaseMapStackingLayer7Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer7Group, Blending Mode)]
        [Tooltip(Same as the blending mode in Photoshop.)]
        [Tooltip(Decide how this layer should combine with Base Map.)]
        [Tooltip(.     Normal RGBA, same as the normal blending layer in Photoshop, will modify the alpha of Base Map also.)]
        [Tooltip(.     Normal RGB, replace the RGB of Base Map, will not modify the alpha of the Base Map.)]
        [Tooltip(.     Add RGB, add to the RGB color of Base Map. Great for emissive.)]
        [Tooltip(.     Screen RGB, soft additive with the RGB color of Base Map. Only works if the blending colors are both within LDR 0 to 1 range.)]
        [Tooltip(.     Multiply RGB, multiply with the RGB color of Base Map.)]
        [SubEnum(_BaseMapStackingLayer7Group,Normal RGBA,0,Normal RGB,1,Add RGB,2,Screen RGB,3,Multiply RGB,4,None,5)]_BaseMapStackingLayer7ColorBlendMode("Blending Mode", Float) = 0
        
        [Title(_BaseMapStackingLayer7Group, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_BaseMapStackingLayer7Group, Layer Content)]
        [Tooltip(A map that contains the content of this layer, same as the content of a Photoshop layer.)]
        [Tex(_BaseMapStackingLayer7Group,_BaseMapStackingLayer7TintColor)][NoScaleOffset]_BaseMapStackingLayer7Tex("Layer Map", 2D) = "white" {}
        [HideInInspector][HDR]_BaseMapStackingLayer7TintColor("", Color) = (1,1,1,1)
        [Tooltip(Which UV channel should be used for sampling Layer Map.)]
        [SubEnum(_BaseMapStackingLayer7Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer7TexUVIndex("               UV Mode", Float) = 0
        [Tooltip(Enable to ignore the alpha channel of Layer Map. (Treat the alpha channel of Layer Map as 1))]
        [SubToggle(_BaseMapStackingLayer7Group, _)]_BaseMapStackingLayer7TexIgnoreAlpha("               Ignore Map Alpha?", Float) = 0
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer7Group, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled.)]
        [Sub(_BaseMapStackingLayer7Group)]_BaseMapStackingLayer7TexUVCenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer7Group, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the classic UV Tiling of Layer Map.)]
        [Tooltip((z,w) controls the classic UV Offset of Layer Map.)]
        [Sub(_BaseMapStackingLayer7Group)]_BaseMapStackingLayer7TexUVScaleOffset("", Vector) = (1,1,0,0)

        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer7Group, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scrolling speed of Layer Map, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_BaseMapStackingLayer7Group)]_BaseMapStackingLayer7TexUVAnimSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer7Group, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_BaseMapStackingLayer7Group)]_BaseMapStackingLayer7TexUVRotatedAngle("Angle", Float) = 0
        [Advanced(UV Edit)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_BaseMapStackingLayer7Group)]_BaseMapStackingLayer7TexUVRotateSpeed("Speed", Float) = 0
        
        [Title(_BaseMapStackingLayer7Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer7Group, Mask)]
        [Tooltip(Controls which area this layer is allowed to show, similar to the layer mask of a Photoshop layer.)]
        [Tooltip(.    White is show)]
        [Tooltip(.    Gray is show(with 50 percent alpha))]
        [Tooltip(.    Black is hide)]
        [Tex(_BaseMapStackingLayer7Group,_BaseMapStackingLayer7MaskTexChannel)][NoScaleOffset]_BaseMapStackingLayer7MaskTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_BaseMapStackingLayer7MaskTexChannel("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubEnum(_BaseMapStackingLayer7Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer7MaskUVIndex("               UV Mode", Float) = 0

        [SubToggle(_BaseMapStackingLayer7Group, _)]_BaseMapStackingLayer7MaskTexAsIDMap("               Extract from ID?", Float) = 0
        [ShowIf(_BaseMapStackingLayer7MaskTexAsIDMap, Equal, 1)]
        [SubIntRange(_BaseMapStackingLayer7Group)]_BaseMapStackingLayer7MaskTexExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [Tooltip(Enable to invert the value of Mask Map.)]
        [SubToggle(_BaseMapStackingLayer7Group, _)]_BaseMapStackingLayer7MaskInvertColor("               Invert?", Float) = 0

        [MinMaxSlider(_BaseMapStackingLayer7Group,_BaseMapStackingLayer7MaskRemapStart,_BaseMapStackingLayer7MaskRemapEnd)]_BaseMapStackingLayer7MaskRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_BaseMapStackingLayer7MaskRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_BaseMapStackingLayer7MaskRemapEnd("", Range(0.0,1.0)) = 1.0
        
        [Title(_BaseMapStackingLayer7Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer7Group, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show this layer.)]
        [SubEnum(_BaseMapStackingLayer7Group,Both,0,Front,2,Back,1)]_BaseMapStackingLayer7ApplytoFaces("Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Base Map Stacking Layer 8
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(Just like adding a Photoshop layer on top of Base Map.)]
        [Tooltip(Useful if you need to overlay a texture on top of Base Map. For example, if you want to add)]
        [Tooltip(.    makeup on face)]
        [Tooltip(.    decal)]
        [Tooltip(.    logo)]
        [Tooltip(.    tattoo)]
        [Tooltip(.    texture uv animation loop effect)]
        [Tooltip(.    (Any effect that is suitable))]
        [Main(_BaseMapStackingLayer8Group, _BASEMAP_STACKING_LAYER8)]_BaseMapStackingLayer8Enable("Base Map Stacking Layer 8", Float) = 0
        
        [Helpbox(Layer Map Wrap mode is repeat clamp (not texture import settings).)]
                
        [Title(_BaseMapStackingLayer8Group, Master Control)]
        [Sub(_BaseMapStackingLayer8Group)]_BaseMapStackingLayer8MasterStrength("Strength", Range(0,1)) = 1

        [Title(_BaseMapStackingLayer8Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer8Group, Blending Mode)]
        [Tooltip(Same as the blending mode in Photoshop.)]
        [Tooltip(Decide how this layer should combine with Base Map.)]
        [Tooltip(.     Normal RGBA, same as the normal blending layer in Photoshop, will modify the alpha of Base Map also.)]
        [Tooltip(.     Normal RGB, replace the RGB of Base Map, will not modify the alpha of the Base Map.)]
        [Tooltip(.     Add RGB, add to the RGB color of Base Map. Great for emissive.)]
        [Tooltip(.     Screen RGB, soft additive with the RGB color of Base Map. Only works if the blending colors are both within LDR 0 to 1 range.)]
        [Tooltip(.     Multiply RGB, multiply with the RGB color of Base Map.)]
        [SubEnum(_BaseMapStackingLayer8Group,Normal RGBA,0,Normal RGB,1,Add RGB,2,Screen RGB,3,Multiply RGB,4,None,5)]_BaseMapStackingLayer8ColorBlendMode("Blending Mode", Float) = 0
        
        [Title(_BaseMapStackingLayer8Group, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_BaseMapStackingLayer8Group, Layer Content)]
        [Tooltip(A map that contains the content of this layer, same as the content of a Photoshop layer.)]
        [Tex(_BaseMapStackingLayer8Group,_BaseMapStackingLayer8TintColor)][NoScaleOffset]_BaseMapStackingLayer8Tex("Layer Map", 2D) = "white" {}
        [HideInInspector][HDR]_BaseMapStackingLayer8TintColor("", Color) = (1,1,1,1)
        [Tooltip(Which UV channel should be used for sampling Layer Map.)]
        [SubEnum(_BaseMapStackingLayer8Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer8TexUVIndex("               UV Mode", Float) = 0
        [Tooltip(Enable to ignore the alpha channel of Layer Map. (Treat the alpha channel of Layer Map as 1))]
        [SubToggle(_BaseMapStackingLayer8Group, _)]_BaseMapStackingLayer8TexIgnoreAlpha("               Ignore Map Alpha?", Float) = 0
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer8Group, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled.)]
        [Sub(_BaseMapStackingLayer8Group)]_BaseMapStackingLayer8TexUVCenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer8Group, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the classic UV Tiling of Layer Map.)]
        [Tooltip((z,w) controls the classic UV Offset of Layer Map.)]
        [Sub(_BaseMapStackingLayer8Group)]_BaseMapStackingLayer8TexUVScaleOffset("", Vector) = (1,1,0,0)

        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer8Group, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scrolling speed of Layer Map, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_BaseMapStackingLayer8Group)]_BaseMapStackingLayer8TexUVAnimSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer8Group, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_BaseMapStackingLayer8Group)]_BaseMapStackingLayer8TexUVRotatedAngle("Angle", Float) = 0
        [Advanced(UV Edit)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_BaseMapStackingLayer8Group)]_BaseMapStackingLayer8TexUVRotateSpeed("Speed", Float) = 0
        
        [Title(_BaseMapStackingLayer8Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer8Group, Mask)]
        [Tooltip(Controls which area this layer is allowed to show, similar to the layer mask of a Photoshop layer.)]
        [Tooltip(.    White is show)]
        [Tooltip(.    Gray is show(with 50 percent alpha))]
        [Tooltip(.    Black is hide)]
        [Tex(_BaseMapStackingLayer8Group,_BaseMapStackingLayer8MaskTexChannel)][NoScaleOffset]_BaseMapStackingLayer8MaskTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_BaseMapStackingLayer8MaskTexChannel("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubEnum(_BaseMapStackingLayer8Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer8MaskUVIndex("               UV Mode", Float) = 0
        
        [SubToggle(_BaseMapStackingLayer8Group, _)]_BaseMapStackingLayer8MaskTexAsIDMap("               Extract from ID?", Float) = 0
        [ShowIf(_BaseMapStackingLayer8MaskTexAsIDMap, Equal, 1)]
        [SubIntRange(_BaseMapStackingLayer8Group)]_BaseMapStackingLayer8MaskTexExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [Tooltip(Enable to invert the value of Mask Map.)]
        [SubToggle(_BaseMapStackingLayer8Group, _)]_BaseMapStackingLayer8MaskInvertColor("               Invert?", Float) = 0

        [MinMaxSlider(_BaseMapStackingLayer8Group,_BaseMapStackingLayer8MaskRemapStart,_BaseMapStackingLayer8MaskRemapEnd)]_BaseMapStackingLayer8MaskRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_BaseMapStackingLayer8MaskRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_BaseMapStackingLayer8MaskRemapEnd("", Range(0.0,1.0)) = 1.0
        
        [Title(_BaseMapStackingLayer8Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer8Group, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show this layer.)]
        [SubEnum(_BaseMapStackingLayer8Group,Both,0,Front,2,Back,1)]_BaseMapStackingLayer8ApplytoFaces("Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Base Map Stacking Layer 9
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(Just like adding a Photoshop layer on top of Base Map.)]
        [Tooltip(Useful if you need to overlay a texture on top of Base Map. For example, if you want to add)]
        [Tooltip(.    makeup on face)]
        [Tooltip(.    decal)]
        [Tooltip(.    logo)]
        [Tooltip(.    tattoo)]
        [Tooltip(.    texture uv animation loop effect)]
        [Tooltip(.    (Any effect that is suitable))]
        [Main(_BaseMapStackingLayer9Group, _BASEMAP_STACKING_LAYER9)]_BaseMapStackingLayer9Enable("Base Map Stacking Layer 9", Float) = 0
        
        [Helpbox(Layer Map Wrap mode is linear clamp (not texture import settings).)]
                
        [Title(_BaseMapStackingLayer9Group, Master Control)]
        [Sub(_BaseMapStackingLayer9Group)]_BaseMapStackingLayer9MasterStrength("Strength", Range(0,1)) = 1

        [Title(_BaseMapStackingLayer9Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer9Group, Blending Mode)]
        [Tooltip(Same as the blending mode in Photoshop.)]
        [Tooltip(Decide how this layer should combine with Base Map.)]
        [Tooltip(.     Normal RGBA, same as the normal blending layer in Photoshop, will modify the alpha of Base Map also.)]
        [Tooltip(.     Normal RGB, replace the RGB of Base Map, will not modify the alpha of the Base Map.)]
        [Tooltip(.     Add RGB, add to the RGB color of Base Map. Great for emissive.)]
        [Tooltip(.     Screen RGB, soft additive with the RGB color of Base Map. Only works if the blending colors are both within LDR 0 to 1 range.)]
        [Tooltip(.     Multiply RGB, multiply with the RGB color of Base Map.)]
        [SubEnum(_BaseMapStackingLayer9Group,Normal RGBA,0,Normal RGB,1,Add RGB,2,Screen RGB,3,Multiply RGB,4,None,5)]_BaseMapStackingLayer9ColorBlendMode("Blending Mode", Float) = 0
        
        [Title(_BaseMapStackingLayer9Group, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_BaseMapStackingLayer9Group, Layer Content)]
        [Tooltip(A map that contains the content of this layer, same as the content of a Photoshop layer.)]
        [Tex(_BaseMapStackingLayer9Group,_BaseMapStackingLayer9TintColor)][NoScaleOffset]_BaseMapStackingLayer9Tex("Layer Map", 2D) = "white" {}
        [HideInInspector][HDR]_BaseMapStackingLayer9TintColor("", Color) = (1,1,1,1)
        [Tooltip(Which UV channel should be used for sampling Layer Map.)]
        [SubEnum(_BaseMapStackingLayer9Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer9TexUVIndex("               UV Mode", Float) = 0
        [Tooltip(Enable to ignore the alpha channel of Layer Map. (Treat the alpha channel of Layer Map as 1))]
        [SubToggle(_BaseMapStackingLayer9Group, _)]_BaseMapStackingLayer9TexIgnoreAlpha("               Ignore Map Alpha?", Float) = 0
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer9Group, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled.)]
        [Sub(_BaseMapStackingLayer9Group)]_BaseMapStackingLayer9TexUVCenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer9Group, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the classic UV Tiling of Layer Map.)]
        [Tooltip((z,w) controls the classic UV Offset of Layer Map.)]
        [Sub(_BaseMapStackingLayer9Group)]_BaseMapStackingLayer9TexUVScaleOffset("", Vector) = (1,1,0,0)

        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer9Group, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scrolling speed of Layer Map, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_BaseMapStackingLayer9Group)]_BaseMapStackingLayer9TexUVAnimSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer9Group, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_BaseMapStackingLayer9Group)]_BaseMapStackingLayer9TexUVRotatedAngle("Angle", Float) = 0
        [Advanced(UV Edit)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_BaseMapStackingLayer9Group)]_BaseMapStackingLayer9TexUVRotateSpeed("Speed", Float) = 0
        
        [Title(_BaseMapStackingLayer9Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer9Group, Mask)]
        [Tooltip(Controls which area this layer is allowed to show, similar to the layer mask of a Photoshop layer.)]
        [Tooltip(.    White is show)]
        [Tooltip(.    Gray is show(with 50 percent alpha))]
        [Tooltip(.    Black is hide)]
        [Tex(_BaseMapStackingLayer9Group,_BaseMapStackingLayer9MaskTexChannel)][NoScaleOffset]_BaseMapStackingLayer9MaskTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_BaseMapStackingLayer9MaskTexChannel("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubEnum(_BaseMapStackingLayer9Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer9MaskUVIndex("               UV Mode", Float) = 0
        
        [SubToggle(_BaseMapStackingLayer9Group, _)]_BaseMapStackingLayer9MaskTexAsIDMap("               Extract from ID?", Float) = 0
        [ShowIf(_BaseMapStackingLayer9MaskTexAsIDMap, Equal, 1)]
        [SubIntRange(_BaseMapStackingLayer9Group)]_BaseMapStackingLayer9MaskTexExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [Tooltip(Enable to invert the value of Mask Map.)]
        [SubToggle(_BaseMapStackingLayer9Group, _)]_BaseMapStackingLayer9MaskInvertColor("               Invert?", Float) = 0

        [MinMaxSlider(_BaseMapStackingLayer9Group,_BaseMapStackingLayer9MaskRemapStart,_BaseMapStackingLayer9MaskRemapEnd)]_BaseMapStackingLayer9MaskRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_BaseMapStackingLayer9MaskRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_BaseMapStackingLayer9MaskRemapEnd("", Range(0.0,1.0)) = 1.0
        
        [Title(_BaseMapStackingLayer9Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer9Group, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show this layer.)]
        [SubEnum(_BaseMapStackingLayer9Group,Both,0,Front,2,Back,1)]_BaseMapStackingLayer9ApplytoFaces("Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Base Map Stacking Layer 10
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(Just like adding a Photoshop layer on top of Base Map.)]
        [Tooltip(Useful if you need to overlay a texture on top of Base Map. For example, if you want to add)]
        [Tooltip(.    makeup on face)]
        [Tooltip(.    decal)]
        [Tooltip(.    logo)]
        [Tooltip(.    tattoo)]
        [Tooltip(.    texture uv animation loop effect)]
        [Tooltip(.    (Any effect that is suitable))]
        [Main(_BaseMapStackingLayer10Group, _BASEMAP_STACKING_LAYER10)]_BaseMapStackingLayer10Enable("Base Map Stacking Layer 10", Float) = 0
        
        [Helpbox(Layer Map Wrap mode is repeat clamp (not texture import settings).)]
                
        [Title(_BaseMapStackingLayer10Group, Master Control)]
        [Sub(_BaseMapStackingLayer10Group)]_BaseMapStackingLayer10MasterStrength("Strength", Range(0,1)) = 1

        [Title(_BaseMapStackingLayer10Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer10Group, Blending Mode)]
        [Tooltip(Same as the blending mode in Photoshop.)]
        [Tooltip(Decide how this layer should combine with Base Map.)]
        [Tooltip(.     Normal RGBA, same as the normal blending layer in Photoshop, will modify the alpha of Base Map also.)]
        [Tooltip(.     Normal RGB, replace the RGB of Base Map, will not modify the alpha of the Base Map.)]
        [Tooltip(.     Add RGB, add to the RGB color of Base Map. Great for emissive.)]
        [Tooltip(.     Screen RGB, soft additive with the RGB color of Base Map. Only works if the blending colors are both within LDR 0 to 1 range.)]
        [Tooltip(.     Multiply RGB, multiply with the RGB color of Base Map.)]
        [SubEnum(_BaseMapStackingLayer10Group,Normal RGBA,0,Normal RGB,1,Add RGB,2,Screen RGB,3,Multiply RGB,4,None,5)]_BaseMapStackingLayer10ColorBlendMode("Blending Mode", Float) = 0
        
        [Title(_BaseMapStackingLayer10Group, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_BaseMapStackingLayer10Group, Layer Content)]
        [Tooltip(A map that contains the content of this layer, same as the content of a Photoshop layer.)]
        [Tex(_BaseMapStackingLayer10Group,_BaseMapStackingLayer10TintColor)][NoScaleOffset]_BaseMapStackingLayer10Tex("Layer Map", 2D) = "white" {}
        [HideInInspector][HDR]_BaseMapStackingLayer10TintColor("", Color) = (1,1,1,1)
        [Tooltip(Which UV channel should be used for sampling Layer Map.)]
        [SubEnum(_BaseMapStackingLayer10Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer10TexUVIndex("               UV Mode", Float) = 0
        [Tooltip(Enable to ignore the alpha channel of Layer Map. (Treat the alpha channel of Layer Map as 1))]
        [SubToggle(_BaseMapStackingLayer10Group, _)]_BaseMapStackingLayer10TexIgnoreAlpha("               Ignore Map Alpha?", Float) = 0
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer10Group, Scale(xy) Pos(zw))]
        [Tooltip((x,y) controls the perceptual Scale of texture sampled.)]
        [Tooltip((z,w) controls the perceptual Position of texture sampled.)]
        [Sub(_BaseMapStackingLayer10Group)]_BaseMapStackingLayer10TexUVCenterPivotScalePos("", Vector) = (1,1,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer10Group, Tiling(xy) Offset(zw))]
        [Tooltip((x,y) controls the classic UV Tiling of Layer Map.)]
        [Tooltip((z,w) controls the classic UV Offset of Layer Map.)]
        [Sub(_BaseMapStackingLayer10Group)]_BaseMapStackingLayer10TexUVScaleOffset("", Vector) = (1,1,0,0)

        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer10Group, Scroll Speed(xy))]
        [Tooltip((x,y) controls the UV scrolling speed of Layer Map, similar to a river panning texture.)]
        [Tooltip((z,w) unused.)]
        [Sub(_BaseMapStackingLayer10Group)]_BaseMapStackingLayer10TexUVAnimSpeed("", Vector) = (0,0,0,0)
        
        [Advanced(UV Edit)]
        [Title(_BaseMapStackingLayer10Group, Rotation in Degree)]
        [Tooltip(Controls the fixed angle rotation, good for static logos or decals.)]
        [Sub(_BaseMapStackingLayer10Group)]_BaseMapStackingLayer10TexUVRotatedAngle("Angle", Float) = 0
        [Advanced(UV Edit)]
        [Tooltip(Controls the animation angle rotation per second, similar to a rotating fan.)]
        [Sub(_BaseMapStackingLayer10Group)]_BaseMapStackingLayer10TexUVRotateSpeed("Speed", Float) = 0
        
        [Title(_BaseMapStackingLayer10Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer10Group, Mask)]
        [Tooltip(Controls which area this layer is allowed to show, similar to the layer mask of a Photoshop layer.)]
        [Tooltip(.    White is show)]
        [Tooltip(.    Gray is show(with 50 percent alpha))]
        [Tooltip(.    Black is hide)]
        [Tex(_BaseMapStackingLayer10Group,_BaseMapStackingLayer10MaskTexChannel)][NoScaleOffset]_BaseMapStackingLayer10MaskTex("Mask Map", 2D) = "white" {}
        [HideInInspector]_BaseMapStackingLayer10MaskTexChannel("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubEnum(_BaseMapStackingLayer10Group,UV0,0,UV1,1,UV2,2,UV3,3,MatCapUV,4,CharBoundUV,5,ScreenSpaceUV,6)]_BaseMapStackingLayer10MaskUVIndex("               UV Mode", Float) = 0
        
        [SubToggle(_BaseMapStackingLayer10Group, _)]_BaseMapStackingLayer10MaskTexAsIDMap("               Extract from ID?", Float) = 0
        [ShowIf(_BaseMapStackingLayer10MaskTexAsIDMap, Equal, 1)]
        [SubIntRange(_BaseMapStackingLayer10Group)]_BaseMapStackingLayer10MaskTexExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [Tooltip(Enable to invert the value of Mask Map.)]
        [SubToggle(_BaseMapStackingLayer10Group, _)]_BaseMapStackingLayer10MaskInvertColor("               Invert?", Float) = 0

        [MinMaxSlider(_BaseMapStackingLayer10Group,_BaseMapStackingLayer10MaskRemapStart,_BaseMapStackingLayer10MaskRemapEnd)]_BaseMapStackingLayer10MaskRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_BaseMapStackingLayer10MaskRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_BaseMapStackingLayer10MaskRemapEnd("", Range(0.0,1.0)) = 1.0
        
        [Title(_BaseMapStackingLayer10Group, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_BaseMapStackingLayer10Group, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show this layer.)]
        [SubEnum(_BaseMapStackingLayer10Group,Both,0,Front,2,Back,1)]_BaseMapStackingLayer10ApplytoFaces("Show in which faces?", Float) = 0
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Replacing BaseMap Result)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // MatCap (Color Replace)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Useful for replacing material visual to any target matcap texture)]
        [Main(_MatCapAlphaBlendGroup,_MATCAP_BLEND)]_UseMatCapAlphaBlend("MatCap (Color Replace)", Float) = 0

        [Title(_MatCapAlphaBlendGroup,Preset)]
        [Preset(_MatCapAlphaBlendGroup, NiloToonCharacter_MatCapAlphaBlendPreset_LWGUI_ShaderPropertyPreset)] _MatCapAlphaBlendPreset("Preset", Float) = 0

        [Title(_MatCapAlphaBlendGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]
                
        [Title(_MatCapAlphaBlendGroup,Opacity control)]
        [Sub(_MatCapAlphaBlendGroup)]_MatCapAlphaBlendUsage("Strength", Range(0,1)) = 1.0

        [Title(_MatCapAlphaBlendGroup,MatCap Map)]
        [AdvancedHeaderProperty]
        [Tex(_MatCapAlphaBlendGroup,_MatCapAlphaBlendTintColor)][NoScaleOffset]_MatCapAlphaBlendMap("MatCap Map", 2D) = "white" {}
        [Advanced][HideInInspector][HDR]_MatCapAlphaBlendTintColor("TintColor (Can edit alpha)", Color) = (1,1,1,1)

        [Advanced]
        [Sub(_MatCapAlphaBlendGroup)]_MatCapAlphaBlendUvScale("              UV Tiling", Range(0,8)) = 1
        [Advanced]
        [Sub(_MatCapAlphaBlendGroup)]_MatCapAlphaBlendMapAlphaAsMask("              Alpha as Mask?", Range(0,1)) = 0

        [Title(_MatCapAlphaBlendGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        // don't have a separated shader_feature for matcap's mask texture, because usually a matcap mask texture is needed anyway
        [Title(_MatCapAlphaBlendGroup, Mask)]
        [Tooltip(White is show, Black is hide)]
        [Tex(_MatCapAlphaBlendGroup,_MatCapAlphaBlendMaskMapChannelMask)][NoScaleOffset]_MatCapAlphaBlendMaskMap("Mask Map", 2D) = "white" {}
        [HideInInspector]_MatCapAlphaBlendMaskMapChannelMask("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubToggle(_MatCapAlphaBlendGroup, _)]_MatCapAlphaBlendMaskMapInvertColor("               Invert?", Float) = 0   
        [MinMaxSlider(_MatCapAlphaBlendGroup,_MatCapAlphaBlendMaskMapRemapStart,_MatCapAlphaBlendMaskMapRemapEnd)]_MatCapAlphaBlendMaskMapRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_MatCapAlphaBlendMaskMapRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_MatCapAlphaBlendMaskMapRemapEnd("", Range(0.0,1.0)) = 1.0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Sphere Eye Ball (legacy)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Hidden]
        [Tooltip(This function will not update anymore, but we will still keep it.)]
        [Tooltip()]
        [Tooltip(Useful if material is realistic sphere eye ball.)]
        [Tooltip()]
        [Tooltip(You can ignore this section if this material is not realistic sphere eye ball.)]
        [Main(_DynamicEyeGroup,_DYNAMIC_EYE)]_EnableDynamicEyeFeature("Sphere Eye Ball (legacy)", Float) = 0

        [Title(_DynamicEyeGroup, Overall)]
        [Sub(_DynamicEyeGroup)]_DynamicEyeSize("Size", Range(0.1,8)) = 2.2
        [Sub(_DynamicEyeGroup)]_DynamicEyeFinalBrightness("Final Brightness", Range(0,8)) = 2
        [Sub(_DynamicEyeGroup)]_DynamicEyeFinalTintColor("Final Tint Color", Color) = (1,1,1)

        [Title(_DynamicEyeGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_DynamicEyeGroup, Eye pupil)]
        [Tex(_DynamicEyeGroup)]_DynamicEyePupilMap("Pupil Map", 2D) = "white" {}
        [Tex(_DynamicEyeGroup,_DynamicEyePupilMaskTexChannelMask)]_DynamicEyePupilMaskTex("Pupil Mask Map", 2D) = "white" {}
        [HideInInspector]_DynamicEyePupilMaskTexChannelMask("Pupil Mask Tex Channel Mask", Vector) = (0,0,0,1)
        [Sub(_DynamicEyeGroup)]_DynamicEyePupilColor("Pupil Color", Color) = (1,1,1)
        [Sub(_DynamicEyeGroup)]_DynamicEyePupilDepthScale("Pupil Depth Scale", Range(0,1)) = 0.4
        [Sub(_DynamicEyeGroup)]_DynamicEyePupilSize("Pupil Size", Range(-1,1)) = -0.384
        [Sub(_DynamicEyeGroup)]_DynamicEyePupilMaskSoftness("Pupil Mask Softness", Range(0,1)) = 0.216

        [Title(_DynamicEyeGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_DynamicEyeGroup, Eye white)]
        [Tex(_DynamicEyeGroup)]_DynamicEyeWhiteMap("Eye White Map", 2D) = "white" {}

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Surface define note
        //
        // URP's channel packing rule:
        // (https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/lit-shader.html#channel-packing)
        // - Red    = Metallic
        // - Green  = Occlusion
        // - Blue   = None
        // - Alpha  = Smoothness
        //
        // But in this NiloToon character shader, all texture sampling are isolated, for ease of use reason (but harms performance)
        // search [Note from the developer (2)] for a more detail explaination
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(URP Lit Shader Maps)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Normal Map (Bump Map)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Same as the Tangent space normal map of URP Lit shader)]        
        [Main(_NormalMapGroup,_NORMALMAP)]_UseNormalMap("Normal Map", Float) = 0

        [Title(_NormalMapGroup, Bump Map)]
        [Tooltip(a.k.a NormalMap)]
        [AdvancedHeaderProperty]
        [Tex(_NormalMapGroup)][NoScaleOffset][Normal]_BumpMap("Bump Map ", 2D) = "bump" {}
        [Advanced]
        [Title(_NormalMapGroup, . Strength)]
        [Sub(_NormalMapGroup)]_BumpScale("  Bump Scale", Float) = 1.0
        [Advanced]
        [Title(_NormalMapGroup, . UV)]
        [SubEnum(_NormalMapGroup,UV0,0,UV1,1,UV2,2,UV3,3)]_BumpMapUVIndex("  UV Channel", Float) = 0
        [Advanced]
        [Sub(_NormalMapGroup)]_BumpMapUVScaleOffset("  Tiling(xy)|Offset(zw)", Vector) = (1,1,0,0)
        [Advanced]
        [Sub(_NormalMapGroup)]_BumpMapUVScrollSpeed("  Scroll speed(xy)", Vector) = (0,0,0,0)
        [Advanced]
        [Title(_NormalMapGroup, . Front and Back Face)]
        [SubEnum(_NormalMapGroup,Both,0,Front,2,Back,1)]_BumpMapApplytoFaces("  Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Detail map
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // (all following URP 10 Lit.shader naming)
        [ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Mix another base color texture and normal texture into result.)] 
        [Main(_DETAIL,_DETAIL)]_UseDetailMap("Detail Maps", Float) = 0

        [Title(_DETAIL, Albedo Map)]
        [Tooltip((only use rgb channel) (Default linearGrey 0.5))]
        [Tex(_DETAIL)][NoScaleOffset]_DetailAlbedoMap("Albedo Map", 2D) = "linearGrey" {}
        [Sub(_DETAIL)]_DetailAlbedoWhitePoint("              WhitePoint", Range(0.01,1)) = 0.5
        [Sub(_DETAIL)]_DetailAlbedoMapScale("              Scale", Range(0.0, 20)) = 1.0

        [Title(_DETAIL, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_DETAIL, Normal Map)]
        [Tex(_DETAIL)][NoScaleOffset][Normal]_DetailNormalMap("Normal Map", 2D) = "bump" {}
        [Sub(_DETAIL)]_DetailNormalMapScale("              Scale", Range(0.0, 20)) = 1.0

        [Title(_DETAIL, ........................................................................................................................................................................................................................................................................................................................................................................)]


        [Title(_DETAIL, Mask)]
        [Tooltip((White is apply, Black is no effect) (Default White))]
        [Tex(_DETAIL,_DetailMaskChannelMask)][NoScaleOffset]_DetailMask("Mask Map", 2D) = "white" {}
        [HideInInspector]_DetailMaskChannelMask("", Vector) = (1,0,0,0)
        [SubToggle(_DETAIL, _)]_DetailMaskInvertColor("               Invert?", Float) = 0


        [Title(_DETAIL, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_DETAIL, UV)]
        [SubToggle(_DETAIL, _)]_DetailUseSecondUv("Use Second UV", Float) = 0

        [Title(_DETAIL, UV Tiling(xy) and Offset(zw))]
        [Tooltip(UV are shared between Albedo Map and Normal Map)]
        [Sub(_DETAIL)]_DetailMapsScaleTiling("", Vector) = (1,1,0,0)       

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Parallax Map
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(Similar to the Parallax map of URP Lit shader)]
        [Tooltip(Usually it is used for eye material to give some fake depth inside the eye)]
        [Main(_ParallaxMapGroup, _PARALLAXMAP)]_ParallaxMapEnable("Parallax Map", Float) = 0

        [Title(_ParallaxMapGroup,Parallax)]

        // Copy from URP's Lit.shader's Parallax code
        [Sub(_ParallaxMapGroup)]_Parallax("Parallax (Scale)", Range(0.005, 0.08)) = 0.005
        [Tooltip(Default Black.)]
        [Tex(_ParallaxMapGroup)]_ParallaxMap("Parallax Map (Height Map)", 2D) = "black" {}
        [SubEnum(_ParallaxMapGroup,UV0,0,UV1,1,UV2,2,UV3,3)]_ParallaxSampleUVIndex("               UV", Float) = 0

        [Title(_ParallaxMapGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_ParallaxMapGroup,Apply to which UV)]
        [SubEnum(_ParallaxMapGroup,UV0,0,UV1,1,UV2,2,UV3,3)]_ParallaxApplyToUVIndex("Apply to UV", Float) = 0
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Local Lighting and Shadow Style)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Shading Grade Map
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 300)]
        [Tooltip(Similar to UTS2 Shading Grade Map.)]
        [Tooltip(Offset Lighting Style _CelShadeMidPoint by a texture.)]
        [Main(_ShadingGradeMapGroup,_SHADING_GRADEMAP)]_UseShadingGradeMap("Shading Grade Map", Float) = 0

        [Title(_ShadingGradeMapGroup,Define)]
        [Tooltip(You should set SRGB (Color Texture) to OFF for this map.)]
        [Tooltip(.    Grey is no effect)]
        [Tooltip(.    Darker is less shadow)]
        [Tooltip(.    Brighter is more shadow)]
        [Tooltip(Default is linearGrey.)]
        [Tex(_ShadingGradeMapGroup,_ShadingGradeMapChannelMask)][NoScaleOffset]_ShadingGradeMap("Shading Grade Map ", 2D) = "linearGrey" {}
        [HideInInspector]_ShadingGradeMapChannelMask("", Vector) = (0,1,0,0) //  default accept g channel
        
        [SubToggle(_ShadingGradeMapGroup, _)]_ShadingGradeMapInvertColor("        Invert?", Float) = 0
        [MinMaxSlider(_ShadingGradeMapGroup,_ShadingGradeMapRemapStart,_ShadingGradeMapRemapEnd)]_ShadingGradeMapRemapMinMaxSlider("        Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_ShadingGradeMapRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_ShadingGradeMapRemapEnd("", Range(0.0,1.0)) = 1.0

        [Title(_ShadingGradeMapGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_ShadingGradeMapGroup,Apply)]
        [Sub(_ShadingGradeMapGroup)]_ShadingGradeMapStrength("Strength", Range(0.0, 1.0)) = 1.0
        [Sub(_ShadingGradeMapGroup)]_ShadingGradeMapApplyRange("Apply Range", Range(0.0,4.0)) = 1.0
        [Sub(_ShadingGradeMapGroup)]_ShadingGradeMapMidPointOffset("Mid-Point Offset", Range(-1,1)) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Adding extra local Shadow)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Occlusion Map
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Same as URP Lit shader Occlusion Map.)]
        [Tooltip()]
        [Tooltip(If an area was painted in black inside the Occlusion Map, that black area will always become in shadow.)]
        [Tooltip(Usually those area are self shadow areas, e.g.)]
        [Tooltip(.    upper section of Neck)]
        [Tooltip(.    area under the Jaw)]
        [Tooltip(.    occluded area of Hair)]
        [Tooltip(.    folded section of cloth)]
        [Tooltip(.    force hair shadow on face]
        [Tooltip(.    etc)]
        [Main(_OcclusionMapGroup,_OCCLUSIONMAP)]_UseOcclusion("Occlusion Map (Shadow)", Float) = 0

        [Title(_OcclusionMapGroup,Preset)]
        [Tooltip(If you want a sharp split shadow, use Cel Shader mode to split the shadow into black or white only.)]
        [Tooltip(After selecting a preset, the following properties will update their current and default value to match the selected preset.)]
        [Tooltip(.     _OcclusionRemapStart)]
        [Tooltip(.     _OcclusionRemapEnd)]
        [Tooltip()]
        [Tooltip(For details about all properties that this preset controls, you can search NiloToonCharacter_OcclusionMapStyle_LWGUI_ShaderPropertyPreset inside NiloToonURP folder.)]
        [Preset(_OcclusionMapGroup,NiloToonCharacter_OcclusionMapStyle_LWGUI_ShaderPropertyPreset)] _OcclusionMapStylePreset ("Style", float) = 0 

        [Title(_OcclusionMapGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_OcclusionMapGroup,Map)]
        [Tooltip(Same as URP Lit shader Occlusion Map.)]
        [Tooltip(.    White is no effect.)]
        [Tooltip(.    Gray is in shadow (half intensity).)]
        [Tooltip(.    Black is always in shadow(always fully occluded).)]
        [Tooltip()]
        [Tooltip(Occlusion Map will default using (G) channel, same as URP.)]
        [Tex(_OcclusionMapGroup,_OcclusionMapChannelMask)][NoScaleOffset]_OcclusionMap("Occlusion Map", 2D) = "white" {}
        [HideInInspector]_OcclusionMapChannelMask("", Vector) = (0,1,0,0)
        [SubEnum(_OcclusionMapGroup,UV0,0,UV1,1,UV2,2,UV3,3)]_OcclusionMapUVIndex("               UV", Float) = 0

        [Tooltip(If your Occlusion Map is painted in an inverted way,)]
        [Tooltip(.    Painted White for shadow area)]
        [Tooltip(.    Painted Black for non shadow area)]
        [Tooltip(In this situation, you can enable this toggle, which will invert the Occlusion Map value inside the shader, so you do not have to spend time editing the texture.)]
        [SubToggle(_OcclusionMapGroup, _)]_OcclusionMapInvertColor("               Invert?", Float) = 0
        
        [MinMaxSlider(_OcclusionMapGroup,_OcclusionRemapStart,_OcclusionRemapEnd)]_OcclusionRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_OcclusionRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_OcclusionRemapEnd("", Range(0.0,1.0)) = 1.0

        [Title(_OcclusionMapGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_OcclusionMapGroup, Strength)]
        [Tooltip(Controls how much Occlusion Map should the applied.)]
        [Sub(_OcclusionMapGroup)]_OcclusionStrength("Overall", Range(0.0, 1.0)) = 1.0
        //[Tooltip(The Occlusion map strength multipler for indirect lighting.)]
        //[Sub(_OcclusionMapGroup)]_OcclusionStrengthIndirectMultiplier("Indirect Multiplier", Range(0.0, 1.0)) = 0.5 // 0.5 instead of 1, because we don't want pure black result from light probe's contribution when main light is off

        [Title(_OcclusionMapGroup, Front and Back Face)]
        [SubEnum(_OcclusionMapGroup,Both,0,Front,2,Back,1)]_OcclusionMapApplytoFaces("Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // MatCap (Shadow)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Similar to Occlusion Map,)]
        [Tooltip(but it use a matcap texture to define how the extra shadow are added.)]
        [Tooltip(Useful for adding matcap shadow to skin or cloth that shows only at grazing angle, just like a shadow version of matcap fresnel rim light.)]
        [Tooltip(Try using the ShadowSoft preset on a skin material to see what this feature does.)]
        [Main(_MatCapOcclusionGroup,_MATCAP_OCCLUSION)]_UseMatCapOcclusion("MatCap (Shadow)", Float) = 0
        
        [Title(_MatCapOcclusionGroup, Preset)]
        [Preset(_MatCapOcclusionGroup, NiloToonCharacter_MatCapOcclusionPreset_LWGUI_ShaderPropertyPreset)]_MatCapOcclusionPreset("Preset", Float) = 0

        [Title(_MatCapOcclusionGroup, Matcap Map)]
        [Tooltip(Preset will assign texture to this slot. It is usually a white matcap texture with a dark surrounding edge.)]
        [Tooltip(Default White.)]      
        [Tex(_MatCapOcclusionGroup,_MatCapOcclusionMapChannelMask)][NoScaleOffset]_MatCapOcclusionMap("Matcap (Shadow) Map", 2D) = "white" {}
        [HideInInspector]_MatCapOcclusionMapChannelMask("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better   
        [MinMaxSlider(_MatCapOcclusionGroup,_MatCapOcclusionMapRemapStart,_MatCapOcclusionMapRemapEnd)]_MatCapOcclusionMapRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_MatCapOcclusionMapRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_MatCapOcclusionMapRemapEnd("", Range(0.0,1.0)) = 1.0

        [Title(_MatCapOcclusionGroup, Intensity)]
        [Sub(_MatCapOcclusionGroup)]_MatCapOcclusionIntensity("Intensity", Range(0,10)) = 1

        [Title(_MatCapOcclusionGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_MatCapOcclusionGroup, MatCapOcclusionMap Setting)]
        [Sub(_MatCapOcclusionGroup)]_MatCapOcclusionMapAlphaAsMask("Alpha As Mask", Range(0,1)) = 0
        [Sub(_MatCapOcclusionGroup)]_MatCapOcclusionUvScale("UV Tiling", Range(0,8)) = 1

        [Title(_MatCapOcclusionGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        // don't have a separated shader_feature for matcap's mask texture, because usually a matcap mask texture is needed anyway
        [Title(_MatCapOcclusionGroup, Mask)]
        [Tooltip(white is show, black is hide.Default White.)]
        [Tex(_MatCapOcclusionGroup,_MatCapOcclusionMaskMapChannelMask)][NoScaleOffset]_MatCapOcclusionMaskMap("Mask Map ", 2D) = "white" {}
        [HideInInspector]_MatCapOcclusionMaskMapChannelMask("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better   
        [SubToggle(_MatCapOcclusionGroup, _)]_MatCapOcclusionMaskMapInvert("               Invert", Float) = 0
        [MinMaxSlider(_MatCapOcclusionGroup,_MatCapOcclusionMaskMapRemapStart,_MatCapOcclusionMaskMapRemapEnd)]_MatCapOcclusionMaskMapRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_MatCapOcclusionMaskMapRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_MatCapOcclusionMaskMapRemapEnd("", Range(0.0,1.0)) = 1.0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Face 3D rim light and shadow 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(Add 3D rim light and shadow to face.)]
        [Tooltip(Only effective if IsFace is on.)]
        [Tooltip(Only correct if character script FaceForwardDirection and FaceUpDirection are both correctly set up.)]
        [Main(_Face3DRimLightAndShadowGroup,_FACE_3D_RIMLIGHT_AND_SHADOW)]_EnableFace3DRimLightAndShadow("Face 3D rim light and shadow (experimental)", Float) = 0
        
        [Title(Cheek rim light)]
        [Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_CheekRimLightIntensity("Intensity", Range(0,1)) = 1
        [HDR][Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_CheekRimLightTintColor("Tint Color", Color) = (1,1,1)
        [Tex(_Face3DRimLightAndShadowGroup,_Face3DRimLightAndShadow_CheekRimLightMaskMapChannel)][NoScaleOffset]_Face3DRimLightAndShadow_CheekRimLightMaskMap("Mask Map", 2D) = "white" {}
        [HideInInspector]_Face3DRimLightAndShadow_CheekRimLightMaskMapChannel("", Vector) = (1,0,0,0)
        [Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_CheekRimLightThreshold("Threshold", Range(0,1)) = 0.7
        [Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_CheekRimLightSoftness("Softness", Range(0,1)) = 0.1
        
        [Title(Cheek shadow)]
        [Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_CheekShadowIntensity("Intensity", Range(0,1)) = 1
        [Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_CheekShadowTintColor("Tint Color", Color) = (1,1,1)
        [Tex(_Face3DRimLightAndShadowGroup,_Face3DRimLightAndShadow_CheekShadowMaskMapChannel)][NoScaleOffset]_Face3DRimLightAndShadow_CheekShadowMaskMap("Mask Map", 2D) = "white" {}
        [HideInInspector]_Face3DRimLightAndShadow_CheekShadowMaskMapChannel("", Vector) = (1,0,0,0)
        [Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_CheekShadowThreshold("Threshold", Range(0,1)) = 0.7
        [Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_CheekShadowSoftness("Softness", Range(0,1)) = 0.1
        
        [Title(Nose rim light)]
        [Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_NoseRimLightIntensity("Intensity", Range(0,1)) = 1
        [HDR][Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_NoseRimLightTintColor("Tint Color", Color) = (1,1,1)
        [Tex(_Face3DRimLightAndShadowGroup,_Face3DRimLightAndShadow_NoseRimLightMaskMapChannel)][NoScaleOffset]_Face3DRimLightAndShadow_NoseRimLightMaskMap("Mask Map", 2D) = "black" {}
        [HideInInspector]_Face3DRimLightAndShadow_NoseRimLightMaskMapChannel("", Vector) = (1,0,0,0)
        
        [Title(Nose shadow)]
        [Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_NoseShadowIntensity("Intensity", Range(0,1)) = 1
        [Sub(_Face3DRimLightAndShadowGroup)]_Face3DRimLightAndShadow_NoseShadowTintColor("Tint Color", Color) = (1,1,1)
        [Tex(_Face3DRimLightAndShadowGroup,_Face3DRimLightAndShadow_NoseShadowMaskMapChannel)][NoScaleOffset]_Face3DRimLightAndShadow_NoseShadowMaskMap("Mask Map", 2D) = "black" {}
        [HideInInspector]_Face3DRimLightAndShadow_NoseShadowMaskMapChannel("", Vector) = (1,0,0,0)
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Face Shadow Gradient Map (SDF)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(User can provide a gradient texture to define artist controlled shadow result of 0 to 90 degrees light rotation.)]
        [Tooltip(Only effective if IsFace is on.)]
        [Tooltip(Only correct if character script FaceForwardDirection and FaceUpDirection are both correctly set up.)]
        [Main(_FaceShadowGradientMapGroup,_FACE_SHADOW_GRADIENTMAP)]_UseFaceShadowGradientMap("Face Shadow Gradient Map (SDF)", Float) = 0

        [Title(_FaceShadowGradientMapGroup, Preset)]
        [Tooltip(For Default preset, Gradient Map is expected to assign FaceShadowGradientMap textures included in the NiloToonURP package.)]
        [Tooltip()]
        [Tooltip(For Genshin Impact preset, Gradient Map is expected to assign Genshin Impact FaceLightmap textures, which is not included in the NiloToonURP package.)]
        [Preset(_FaceShadowGradientMapGroup, NiloToonCharacter_FaceShadowGradientMapPreset_LWGUI_ShaderPropertyPreset)] _FaceShadowGradientMapPreset ("Preset", float) = 0 

        [Title(_FaceShadowGradientMapGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_FaceShadowGradientMapGroup, Face Shadow Gradient Map)]
        [Helpbox(Search FaceShadowGradientMap for a Gradient Map)]
        [Tooltip(a.k.a Face SDF map)]
        [Tooltip()]
        [Tooltip(Use 1 channel, White is light from front, Black is light from right, Default gray)]
        [Tooltip(You can search FaceShadowGradientMap in NiloToonURP folder to find some style textures.)]
        [Tooltip(Make sure this map is R8 format (Not compressed) to avoid artifact.)]
        [Tooltip(Make sure this map sRGB is Off.)]
        [Tooltip(Default is R channel because R8 format texture is expected.)]
        [Tooltip(It is expected the nose is at the center of the texture,)]
        [Tooltip(if it is not, you need to edit Face mid line uv.x to match the nose position on the texture.)]
        [Tex(_FaceShadowGradientMapGroup,_FaceShadowGradientMapChannel)][NoScaleOffset]_FaceShadowGradientMap("Gradient Map", 2D) = "gray" {}
        [HideInInspector]_FaceShadowGradientMapChannel("", Vector) = (1,0,0,0)
        [SubEnum(_FaceShadowGradientMapGroup,UV0,0,UV1,1,UV2,2,UV3,3)]_FaceShadowGradientMapUVIndex("                 UV", Float) = 0
        [Tooltip(Shader will assume Gradient Map face is centered at the middle of the texture.)]
        [Tooltip(If it is not, you need to edit this value to match the center of the face.)]
        [Tooltip(For example, if the face is placed at the left side of the texture, set this number to around 0.25, where this value matches the uv x position of the nose.)]
        [Sub(_FaceShadowGradientMapGroup)]_FaceShadowGradientMapFaceMidPoint("             Face mid line uv.x", Range(0,1)) = 0.5
        [Tooltip(Enable to invert the sampled value in shader.)]
        [Tooltip(In most cases, it should not be turned on unless the texture color is inverted already.)]
        [SubToggle(_FaceShadowGradientMapGroup, _)]_FaceShadowGradientMapInvertColor("             Invert Color?", Float) = 0
        [Tooltip(Enable to flip the whole Gradient Map horizontally along Face mid line uv.x.)]
        [Tooltip(Usually, if you see bright color at the right side of the face texture, disable this toggle.)]
        [Tooltip(Usually, if you see bright color at the left side of the face texture, enable this toggle.)]
        [Tooltip(But it all depends on how the uv is, so you should rotate the main light to see if enabling this is good or not.)]
        [SubToggle(_FaceShadowGradientMapGroup, _)]_FaceShadowGradientMapUVxInvert("             Mirror L/R?", Float) = 1
        [Title(_FaceShadowGradientMapGroup,UV Scale(XY) Pos(ZW))]
        [Tooltip(A more intuitive way to fit the map on the face(edit UV) compared to the UV Tiling Offset method below.)]
        [Tooltip(The Scale is using texture center as pivot.)]
        [Tooltip(The Pos is having an opposite direction compared to Offset, which should be more intuitive when editing.)]
        [Sub(_FaceShadowGradientMapGroup)]_FaceShadowGradientMapUVCenterPivotScalePos("", Vector) = (1,1,0,0)
        [Title(_FaceShadowGradientMapGroup,UV Tiling(XY) Offset(ZW))]
        [Tooltip(A more simple way to edit UV.)]
        [Sub(_FaceShadowGradientMapGroup)]_FaceShadowGradientMapUVScaleOffset("", Vector) = (1,1,0,0)

        [Title(_FaceShadowGradientMapGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]
        
        [Title(_FaceShadowGradientMapGroup, Intensity)]
        [Sub(_FaceShadowGradientMapGroup)]_FaceShadowGradientIntensity("Intensity", Range(0,1)) = 1

        [Title(_FaceShadowGradientMapGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_FaceShadowGradientMapGroup, Style)]
        [Tooltip(A higher value will delay the appear of shadow, usually it is 0 to 0.1)]
        [Sub(_FaceShadowGradientMapGroup)]_FaceShadowGradientOffset("Offset", Range(-1,1)) = 0.1
        [Sub(_FaceShadowGradientMapGroup)]_FaceShadowGradientResultSoftness("Softness", Range(0,1)) = 0.005
        
        [MinMaxSlider(_FaceShadowGradientMapGroup,_FaceShadowGradientThresholdMin,_FaceShadowGradientThresholdMax)]_FaceShadowGradientThresholdMinMax("Clamp", Range(0.0,1.0)) = 1.0
        [HideInInspector]_FaceShadowGradientThresholdMin("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_FaceShadowGradientThresholdMax("", Range(0.0,1.0)) = 1.0

        [Title(_FaceShadowGradientMapGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_FaceShadowGradientMapGroup, Remove default shadow)]
        [Sub(_FaceShadowGradientMapGroup)]_IgnoreDefaultMainLightFaceShadow("Remove default Shadow", Range(0,1)) = 1

        [Title(_FaceShadowGradientMapGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_FaceShadowGradientMapGroup, Mask)]
        [Tooltip((Use 1 channel) (White is full shadow, Black is hide shadow) (Default white))]
        [Tex(_FaceShadowGradientMapGroup,_FaceShadowGradientMaskMapChannel)][NoScaleOffset]_FaceShadowGradientMaskMap("Mask Map ", 2D) = "white" {}
        [HideInInspector]_FaceShadowGradientMaskMapChannel("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubEnum(_FaceShadowGradientMapGroup,UV0,0,UV1,1,UV2,2,UV3,3)]_FaceShadowGradientMaskMapUVIndex("               UV", Float) = 0
        [SubToggle(_FaceShadowGradientMapGroup, _)]_FaceShadowGradientMaskMapInvertColor("               Invert?", Float) = 0


        [Title(_FaceShadowGradientMapGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_FaceShadowGradientMapGroup, Debug)]
        [SubToggle(_FaceShadowGradientMapGroup, _)]_DebugFaceShadowGradientMap("Debug?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Receiving ShadowMap Shadow)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Nilo Self ShadowMap
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Tooltip(When enabled, this material will receive NiloToon ShadowMap (all renderers using NiloToon_Character shader can cast high quality shadows onto this material).)]
        [Tooltip]
        [Tooltip(NiloToon shadowmap is not URP shadow map.)]
        [Main(_NiloToonSelfShadowMappingSettingGroup,_)]_EnableNiloToonSelfShadowMapping("Nilo Self ShadowMap", Float) = 1
        
        [AdvancedHeaderProperty]
        [Tooltip(Controls intensity of NiloToon ShadowMap received on this material.)]
        [Sub(_NiloToonSelfShadowMappingSettingGroup)]_NiloToonSelfShadowIntensity("Intensity", Range(0,1)) = 1
        
        [Advanced]
        [Tooltip(Controls intensity of NiloToon ShadowMap received on this material (only for Non Face area).)]
        [Sub(_NiloToonSelfShadowMappingSettingGroup)]_NiloToonSelfShadowIntensityForNonFace("     Non-Face", Range(0,1)) = 1
        [Advanced]
        [Tooltip(Controls intensity of NiloToon ShadowMap received on this material (only for Face area).)]
        [Sub(_NiloToonSelfShadowMappingSettingGroup)]_NiloToonSelfShadowIntensityForFace("     Face", Range(0,1)) = 0

        [Advanced]
        [Tooltip(Enable to activate Mask Map. Useful if you want to remove NiloToon ShadowMap only on some area.)]
        [Tooltip]
        [Tooltip(Turn off will give better performance.)]
        [SubToggle(_NiloToonSelfShadowMappingSettingGroup,_NILOTOON_SELFSHADOW_INTENSITY_MAP)]_UseNiloToonSelfShadowIntensityMultiplierTex("     Mask?", Float) = 0
        
        [Advanced]
        [ShowIf(_UseNiloToonSelfShadowIntensityMultiplierTex, Equal, 1)]
        [Tooltip(You can control NiloToon ShadowMap intensity by this Mask Map.)]
        [Tooltip(For example, drawing black on eye white area will remove hair shadow that was casted on eye white.)]
        [Tooltip(.    White is full intensity shadow.)]
        [Tooltip(.    Darker is reduced intensity shadow.)]
        [Tooltip(.    Black is no shadow.)]
        [Tex(_NiloToonSelfShadowMappingSettingGroup)][NoScaleOffset]_NiloToonSelfShadowIntensityMultiplierTex("Mask Map", 2D) = "white" {}
        
        [Tooltip(An extra Tint Color for NiloToon ShadowMap area.)]
        [Sub(_NiloToonSelfShadowMappingSettingGroup)]_NiloToonSelfShadowMappingTintColor("     Tint Color", Color) = (1,1,1)

        [Helpbox(If you see small holes in shadow area (e.g. neck shadow), instead of editing the bias,)]
        [Helpbox(make sure the Render Face(NiloToonSelfShadowCaster pass) of shadow casting material is Both first)]
        [Helpbox((e.g. set Face,Eye,Mouth materials Render Face to Both))]
        
        [Advanced(ShadowCaster Bias)]
        [Tooltip(In most cases, you do NOT need to enable this. You can try to enable this only if this material is having shadow artifact.)]
        [SubToggle(_NiloToonSelfShadowMappingSettingGroup,_)]_EnableNiloToonSelfShadowMappingDepthBias("Add DepthBias?", Float) = 0
        
        [Advanced(ShadowCaster Bias)]
        [ShowIf(_EnableNiloToonSelfShadowMappingDepthBias, Equal, 1)]
        [Tooltip(In most cases, you do NOT need to edit this. You can try to increase this a bit only if this material is having shadow artifact.)]
        [Sub(_NiloToonSelfShadowMappingSettingGroup)]_NiloToonSelfShadowMappingDepthBias("  DepthBias offset", Range(0,2)) = 0

        [Advanced(ShadowCaster Bias)]
        [Tooltip(In most cases, you do NOT need to enable this. You can try to enable this only if this material is having shadow artifact.)]
        [SubToggle(_NiloToonSelfShadowMappingSettingGroup,_)]_EnableNiloToonSelfShadowMappingNormalBias("Add NormalBias?", Float) = 0
        
        [Advanced(ShadowCaster Bias)]
        [ShowIf(_EnableNiloToonSelfShadowMappingNormalBias, Equal, 1)]
        [Tooltip(In most cases, you do NOT need to edit this. You can try to edit this only if this material is having shadow artifact.)]
        [Sub(_NiloToonSelfShadowMappingSettingGroup)]_NiloToonSelfShadowMappingNormalBias("  NormalBias offset", Range(-2,2)) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // URP Directional Light ShadowMap
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 300)]
        // same as !_RECEIVE_SHADOWS_OFF
        [Tooltip(When enabled, all shadow casting renderers can cast URP directional light shadows onto this material.)]
        [Main(_RECEIVE_URP_SHADOW,_RECEIVE_URP_SHADOW)]_ReceiveURPShadowMapping("URP Directional Light ShadowMap", Float) = 1 

        [Helpbox(Only controls URP directional light shadow map (not additional light shadow map).)]

        [Title(_RECEIVE_URP_SHADOW, Intesity)]
        [Tooltip(Controls intensity of URP ShadowMap casted on this material.)]
        [Sub(_RECEIVE_URP_SHADOW)]_ReceiveURPShadowMappingAmount("Intensity", Range(0,1)) = 1
        [Sub(_RECEIVE_URP_SHADOW)]_ReceiveURPShadowMappingAmountForFace("     Face", Range(0,1)) = 1
        [Sub(_RECEIVE_URP_SHADOW)]_ReceiveURPShadowMappingAmountForNonFace("     Non-Face", Range(0,1)) = 1

        [Title(_RECEIVE_URP_SHADOW, Tint Color)]
        [Tooltip(An extra Tint Color for URP ShadowMap.)]
        [Sub(_RECEIVE_URP_SHADOW)][HDR]_URPShadowMappingTintColor("Tint Color", Color) = (1,1,1)

        [Title(_RECEIVE_URP_SHADOW, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_RECEIVE_URP_SHADOW, Depth Bias)]
        [Tooltip(Increase to hide URP shadow artifact if needed.)]
        [Sub(_RECEIVE_URP_SHADOW)]_ReceiveSelfShadowMappingPosOffset("Depth Bias", Range(0,4)) = 0

        [Tooltip(Increase to hide ugly URP shadow artifact if needed.)]
        [Tooltip(Default is 1m, which means the shadow caster need to be atleast 1m far away from the shadow receiving pixel in order to produce URP shadow.)]
        [Tooltip(Having a default 1m depth bias is because usually you do not want character to receive low resolution ugly URP shadow that was casted by self (e.g. hand,hair,hat), but still wants to receive URP shadow that was casted by far environment objects(e.g. tree, building).)]        
        [Sub(_RECEIVE_URP_SHADOW)]_ReceiveSelfShadowMappingPosOffsetForFaceArea("Depth Bias (Face)", Range(0,4)) = 1

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // URP Additional Light ShadowMap
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 300)]
        [Tooltip(When enabled, all shadow casting renderers can cast URP additional light shadows onto this material.)]
        [Main(_RECEIVE_URP_ADDITIONAL_LIGHT_SHADOW,_)]_ReceiveURPAdditionalLightShadowMapping("URP Additional Light ShadowMap", Float) = 1
        
        [Helpbox(Only controls URP additional light shadow map (not directional light shadow map).)]
        
        [Title(_RECEIVE_URP_ADDITIONAL_LIGHT_SHADOW, Intesity)]
        [Tooltip(Controls intensity of URP additional light ShadowMap casted on this material.)]
        [Sub(_RECEIVE_URP_ADDITIONAL_LIGHT_SHADOW)]_ReceiveURPAdditionalLightShadowMappingAmount("Intensity", Range(0,1)) = 1
        [Sub(_RECEIVE_URP_ADDITIONAL_LIGHT_SHADOW)]_ReceiveURPAdditionalLightShadowMappingAmountForFace("     Face", Range(0,1)) = 1
        [Sub(_RECEIVE_URP_ADDITIONAL_LIGHT_SHADOW)]_ReceiveURPAdditionalLightShadowMappingAmountForNonFace("     Non-Face", Range(0,1)) = 1 
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Ramp Override Shadow Style and Color)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Ramp Map (Diffuse / Shadow)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(If enabled, ignores all Lighting Style and Shadow Color settings, and use a ramp map to define lighting (diffuse) and shadow color instead.)]
        [Main(_RampTextureLightingGroup,_RAMP_LIGHTING)]_UseRampLightingTex("Ramp Map (diffuse / Shadow)", Float) = 0

        [Helpbox(If enabled, ignores all Lighting Style and Shadow Color settings, and use a ramp map to define lighting (diffuse) and shadow color instead.)]
        [Helpbox()]
        [Helpbox(Recommend using Dynamic Mode if you do not have a Static Ramp Map.)]

        [Title(_RampTextureLightingGroup, Mode)]
        [Tooltip(We recommend using Dynamic Ramp Map, because it will allow you to see your edit in realtime, which will increase productivity a lot.)]
        [Tooltip(Default is Static only due to Backward compatibility.)]
        [SubEnum(_RampTextureLightingGroup, Dynamic,0,Static,1)]_RampLightTexMode("Use which Ramp Map?", float) = 1

        [Title(_RampTextureLightingGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [ShowIf(_RampLightTexMode, Equal, 0)]
        [Title(_RampTextureLightingGroup, Dynamic Ramp Map)]
        [Tooltip(Click the Edit button to try different Ramp Map, click Save when you are happy about the result.)]
        [Tooltip(Will output an error log when an external ramp texture is assign. If you want to use a ramp map that is not created by this NiloToon UI, please switch to Static Ramp Map Mode.)]
        [Ramp(_RampTextureLightingGroup)][NoScaleOffset]_DynamicRampLightingTex("Ramp Map", 2D) = "white" {}

        [Title(_RampTextureLightingGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [ShowIf(_RampLightTexMode, Equal, 1)]
        [Title(_RampTextureLightingGroup, Static Ramp Map)]
        [Tooltip(only use rgb channel.)]
        [Tex(_RampTextureLightingGroup)][NoScaleOffset]_RampLightingTex("Ramp Map", 2D) = "white" {}
        [ShowIf(_RampLightTexMode, Equal, 1)]
        [Sub(_RampTextureLightingGroup)]_RampLightingTexSampleUvY("              UV.Y", Range(0,1)) = 0.5

        [Title(_RampTextureLightingGroup,.             ........................................................................................................................................................................................................................................................................................................................................................................)]

        [ShowIf(_RampLightTexMode, Equal, 1)]
        [Title(_RampTextureLightingGroup, .             UV.Y Map)]
        [Tooltip(Override _RampLightingTexSampleUvY by a texture.)]
        [Tooltip(Useful if vertically packed different ramp tex into a single ramp tex atla.)]
        [SubToggle(_RampTextureLightingGroup,_RAMP_LIGHTING_SAMPLE_UVY_TEX)]_UseRampLightingSampleUvYTex("               Enable?", Float) = 0
        
        [ShowIf(_RampLightTexMode, Equal, 1)]
        [Tooltip(Default linear Gray, which means UV.y is  0.5.)]
        [Tex(_RampTextureLightingGroup,_RampLightingSampleUvYTexChannelMask)][NoScaleOffset]_RampLightingSampleUvYTex("     UV.Y Map", 2D) = "Gray" {}
        [HideInInspector]_RampLightingSampleUvYTexChannelMask("", Vector) = (0,1,0,0)
        
        [ShowIf(_RampLightTexMode, Equal, 1)]
        [SubToggle(_RampTextureLightingGroup,_)]_RampLightingSampleUvYTexInvertColor("                    Invert?", Float) = 0
        
        [ShowIf(_RampLightTexMode, Equal, 1)]
        [MinMaxSlider(_RampTextureLightingGroup,_RampLightingUvYRemapStart,_RampLightingUvYRemapEnd)]_RampLightingUvYRemapMinMaxSlider("                    Range", Range(0.0,1.0)) = 1.0
        [HideInInspector]_RampLightingUvYRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_RampLightingUvYRemapEnd("", Range(0.0,1.0)) = 1.0

        [Title(_RampTextureLightingGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_RampTextureLightingGroup, Remap NdotL)]
        [MinMaxSlider(_RampTextureLightingGroup,_RampLightingNdotLRemapStart,_RampLightingNdotLRemapEnd)]_RampLightingNdotLRemapMinMaxSlider("Remap NdotL", Range(0.0,1.0)) = 1.0
        [HideInInspector]_RampLightingNdotLRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_RampLightingNdotLRemapEnd("", Range(0.0,1.0)) = 1.0

        [Title(_RampTextureLightingGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_RampTextureLightingGroup, Face setting)]
        [Sub(_RampTextureLightingGroup)]_RampLightingFaceAreaRemoveEffect("Remove for Face", Range(0,1)) = 1

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Adding Specular Reflection Highlight)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // MatCap (Add/Specular/RimLight)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //[ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Useful for adding specular highlight or rim light using a matcap texture.)]
        [Tooltip()]
        [Tooltip(The reflection will always follow the camera view and will not be affected by light direction.)]
        [Main(_MatCapAdditiveGroup,_MATCAP_ADD)]_UseMatCapAdditive("MatCap (Add/Specular/RimLight)", Float) = 0

        [Helpbox(Useful for adding specular highlight or rim light using a matcap texture.)]
        [Helpbox()]
        [Helpbox(The reflection will always follow the camera view and will not be affected by light direction.)]
        
        [Title(_MatCapAdditiveGroup, Preset)]
        [Preset(_MatCapAdditiveGroup, NiloToonCharacter_MatCapAdditivePreset_LWGUI_ShaderPropertyPreset)]_MatCapAdditivePreset("Preset", Float) = 0

        [Title(_MatCapAdditiveGroup, Matcap Map)]
        [Tooltip(Preset will assign matcap texture to this slot.)]
        [Tex(_MatCapAdditiveGroup,_MatCapAdditiveColor)][NoScaleOffset]_MatCapAdditiveMap("Map", 2D) = "white" {}
        [Title(_MatCapAdditiveGroup, Intensity and Color)]
        [Sub(_MatCapAdditiveGroup)]_MatCapAdditiveIntensity("Intensity", Range(0,100)) = 1
        [Sub(_MatCapAdditiveGroup)]_MatCapAdditiveExtractBrightArea("Extract bright area", Range(0,16)) = 0
        [HideInInspector][HDR]_MatCapAdditiveColor("Color", Color) = (1,1,1,1)
        [Sub(_MatCapAdditiveGroup)]_MatCapAdditiveMixWithBaseMapColor("Mix with Base Map", Range(0,1)) = 0.5

        [Title(_MatCapAdditiveGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_MatCapAdditiveGroup, Visual)]
        [Sub(_MatCapAdditiveGroup)]_MatCapAdditiveMapAlphaAsMask("Alpha As Mask", Range(0,1)) = 0
        [Sub(_MatCapAdditiveGroup)]_MatCapAdditiveUvScale("UV Tiling", Range(0,8)) = 1

        [Title(_MatCapAdditiveGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        // don't have a separated shader_feature for matcap's mask texture, because usually a matcap mask texture is needed anyway
        [Title(_MatCapAdditiveGroup, Mask)]
        [Tooltip(white is show, black is hide.)]
        [Tex(_MatCapAdditiveGroup,_MatCapAdditiveMaskMapChannelMask)][NoScaleOffset]_MatCapAdditiveMaskMap("Mask Map ", 2D) = "white" {}
        [HideInInspector]_MatCapAdditiveMaskMapChannelMask("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubToggle(_MatCapAdditiveGroup, _)]_MatCapAdditiveMaskMapInvertColor("               Invert?", Float) = 0
        [MinMaxSlider(_MatCapAdditiveGroup,_MatCapAdditiveMaskMapRemapStart,_MatCapAdditiveMaskMapRemapEnd)]_MatCapAdditiveMaskMapRemapMinMaxSlider("              Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_MatCapAdditiveMaskMapRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_MatCapAdditiveMaskMapRemapEnd("", Range(0.0,1.0)) = 1.0

        [Title(_MatCapAdditiveGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_MatCapAdditiveGroup, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show MatCap effect.)]
        [SubEnum(_MatCapAdditiveGroup,Both,0,Front,2,Back,1)]_MatCapAdditiveApplytoFaces("Show in which faces?", Float) = 0
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Smoothness/Roughness 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Similar to URP Lit shader Smoothness, affects the sharpness of the following groups,)]
        [Tooltip(.    Specular Highlights (PBR GGX only, not Toon))]
        [Tooltip(.    Environment Reflections)]
        [Main(_SmoothnessGroup,_,off,off)]_SmoothnessGroup("Smoothness/Roughness (Specular)", Float) = 0

        [Helpbox(Similar to URP Lit shader Smoothness, affects the sharpness of the following groups,)]
        [Helpbox(.    Specular Highlights (PBR GGX only, not Toon))]
        [Helpbox(.    Environment Reflections)]
        
        [Title(_SmoothnessGroup, Smoothness Slider)]
        [Tooltip(You can use the Smoothness slider to control the spread of highlights on the surface. 0 gives a wide, rough highlight. 1 gives a small, sharp highlight like glass. Values in between produce semi glossy looks. For example, 0.5 produces a plastic like glossiness.)]
        [Tooltip()]
        [Tooltip(Smoothness equals One minus Roughness.)]
        [Sub(_SmoothnessGroup)]_Smoothness("Smoothness", Range(0,1)) = 0.5

        [Title(_SmoothnessGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_SmoothnessGroup, Smoothness or Roughness Map)]
        [Tooltip(Enable this toggle to activate Smoothness or Roughness Map.)]
        [Tooltip(You need to select the correct Map Type.)]
        [SubToggle(_SmoothnessGroup,_SMOOTHNESSMAP)]_UseSmoothnessMap("Enable Map?", Float) = 0

        [Tooltip(If enabled, Smoothness value will be multiplied with the extracted float value of this map.)]
        [Tooltip()]
        [Tooltip(For Smoothness Map,)]
        [Tooltip(.    White is smooth surface)]
        [Tooltip(.    Black is rough surface)]
        [Tooltip()]
        [Tooltip(For Roughness Map,)]
        [Tooltip(.    White is rough surface)]
        [Tooltip(.    Black is smooth surface)]
        [Tooltip()]
        [Tooltip(By default NiloToon will extract from A channel, same as URP Lit shader Smoothness channel convention.)]
        [Tex(_SmoothnessGroup,_SmoothnessMapChannelMask)][NoScaleOffset]_SmoothnessMap("Map", 2D) = "white" {}
        [HideInInspector]_SmoothnessMapChannelMask("", Vector) = (0,0,0,1) // use A as default value because it is URP Lit.shader's default smoothness channel (https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/lit-shader.html)
        
        [Tooltip(.    Smoothness Map, use the map without modification, as Perceptual Smoothness)]
        [Tooltip(.    Roughness Map, first apply Invert to the Map, then use it as Perceptual Smoothness. Since Perceptual Smoothness equals One minus Perceptual Roughness.)]
        [SubEnum(_SmoothnessGroup,Smoothness Map,0,Roughness Map,1)]_SmoothnessMapInputIsRoughnessMap("             Map Type", Float) = 0
        
        [Tooltip(Apply a linear value remap to pixels of Smoothness Map.)]
        [Tooltip()]
        [Tooltip(For scripting,)] 
        [Tooltip()]
        [Tooltip(you should call Material.SetFloat())]
        [Tooltip(.    _SmoothnessMapRemapStart)]
        [Tooltip(.    _SmoothnessMapRemapEnd)]
        [Tooltip()]
        [Tooltip(You should NOT call Material.SetFloat())]
        [Tooltip(.    _SmoothnessMapRemapMinMaxSlider)]
        [MinMaxSlider(_SmoothnessGroup,_SmoothnessMapRemapStart,_SmoothnessMapRemapEnd)]_SmoothnessMapRemapMinMaxSlider("             Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_SmoothnessMapRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_SmoothnessMapRemapEnd("", Range(0.0,1.0)) = 1.0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Specular Highlights
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Adding PBR or NPR direct specular highlights to result)]
        [Tooltip(You can use the Smoothness section above to control roughness)]
        [Tooltip(Make sure _Smoothness is not 0 in order to see specular result)]
        [Tooltip(Enable specularReactToLightDirectionChange in NiloToonCharRenderingControlVolume will make specular react to light direction change)]

        [Main(_SPECULARHIGHLIGHTS,_SPECULARHIGHLIGHTS)]_UseSpecular("Specular Highlights", Float) = 0

        [Title(_SPECULARHIGHLIGHTS,Additive or Replace)]
        [SubToggle(_SPECULARHIGHLIGHTS, _)]_SpecularUseReplaceBlending("Use Replace Blending", Float) = 0

        [Title(_SPECULARHIGHLIGHTS, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_SPECULARHIGHLIGHTS,Define Specular Area Mask)]
        [Tooltip(White is full specular, Black is no specular)]
        [Tex(_SPECULARHIGHLIGHTS,_SpecularMapChannelMask)][NoScaleOffset]_SpecularMap("Specular Map", 2D) = "white" {}
        [HideInInspector]_SpecularMapChannelMask("", Vector) = (0,0,1,0)
        [SubEnum(_SPECULARHIGHLIGHTS,UV0,0,UV1,1,UV2,2,UV3,3)]_SpecularMapUVIndex("               UV", Float) = 0
        
        [SubToggle(_SPECULARHIGHLIGHTS, _)]_SpecularMapAsIDMap("               Extract from ID?", Float) = 0
        [ShowIf(_SpecularMapAsIDMap, Equal, 1)]
        [SubIntRange(_SPECULARHIGHLIGHTS)]_SpecularMapExtractFromID("                   ID", Range(0,255)) = 255 // default white = pass
        
        [SubToggle(_SPECULARHIGHLIGHTS, _)]_SpecularMapInvertColor("               Invert?", Float) = 0
        
        [MinMaxSlider(_SPECULARHIGHLIGHTS,_SpecularMapRemapStart,_SpecularMapRemapEnd)]_SpecularMapRemapMinMaxSlider("               Remap", Range(0,1)) = 1
        [HideInInspector]_SpecularMapRemapStart("", Range(0,1)) = 0
        [HideInInspector]_SpecularMapRemapEnd("", Range(0,1)) = 1

        [Title(_SPECULARHIGHLIGHTS, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_SPECULARHIGHLIGHTS,Define Specular Method)]
        [Tooltip(. Use Toon for hair highlight)]
        [Tooltip(. Use PBR GGX for realistic specular like metal,plastic,cloth,etc)]
        [SubEnum(_SPECULARHIGHLIGHTS,Toon,0,PBR GGX,1)]_UseGGXDirectSpecular("Specular Method", Float) = 1
        [ShowIf(_UseGGXDirectSpecular, Equal, 1)]
        [Sub(_SPECULARHIGHLIGHTS)]_GGXDirectSpecularSmoothnessMultiplier("    Smoothness Multiplier", Range(0,4)) = 1

        [Title(_SPECULARHIGHLIGHTS, ........................................................................................................................................................................................................................................................................................................................................................................)]
        [Title(_SPECULARHIGHLIGHTS,React to light dir)]
        [SubEnum(_SPECULARHIGHLIGHTS,Use settings from volume,0,React to light,1,Follow camera,2)]_SpecularReactToLightDirMode("Mode", Float) = 0
        
        [Title(_SPECULARHIGHLIGHTS, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_SPECULARHIGHLIGHTS,Intensity and Color)]
        [Sub(_SPECULARHIGHLIGHTS)]_SpecularIntensity("Intensity", Range(0,100)) = 1
        [Sub(_SPECULARHIGHLIGHTS)][HDR]_SpecularColor("Color", Color) = (1,1,1) // URP Lit.shader use _SpecColor, we ignore it and use our own name to avoid confusion
        [Sub(_SPECULARHIGHLIGHTS)]_MultiplyBaseColorToSpecularColor("Multiply Base Map", Range(0,1)) = 0.5    

        [Title(_SPECULARHIGHLIGHTS,Color Tint Texture)]
        [SubToggle(_SPECULARHIGHLIGHTS,_SPECULARHIGHLIGHTS_TEX_TINT)]_UseSpecularColorTintMap("Use Color Tint Map?", Float) = 0
        [Tex(_SPECULARHIGHLIGHTS)][NoScaleOffset]_SpecularColorTintMap("    Color Tint Map", 2D) = "white" {}
        [Sub(_SPECULARHIGHLIGHTS)]_SpecularColorTintMapUsage("    Strength", Range(0,1)) = 1
        [SubToggle(_SPECULARHIGHLIGHTS, _)]_SpecularColorTintMapUseSecondUv("    Use Second UV", Float) = 0
        [Title(_SPECULARHIGHLIGHTS,.    Tiling(XY)Offset(ZW))]
        [Sub(_SPECULARHIGHLIGHTS)]_SpecularColorTintMapTilingXyOffsetZw("", Vector) = (1,1,0,0)

        [Title(_SPECULARHIGHLIGHTS, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_SPECULARHIGHLIGHTS, Style Remap)]
        [Sub(_SPECULARHIGHLIGHTS)]_SpecularAreaRemapUsage("Area Remap Strength", Range(0,1)) = 0.0
        [Sub(_SPECULARHIGHLIGHTS)]_SpecularAreaRemapMidPoint("    MidPoint", Range(0,1)) = 0.1
        [Sub(_SPECULARHIGHLIGHTS)]_SpecularAreaRemapRange("    Softness", Range(0,1)) = 0.05

        [Title(_SPECULARHIGHLIGHTS, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_SPECULARHIGHLIGHTS,Show Specular In Shadow Area)]
        [Sub(_SPECULARHIGHLIGHTS)]_SpecularShowInShadowArea("Show In Shadow Area", Range(0,1)) = 0.0

        [Title(_SPECULARHIGHLIGHTS, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_SPECULARHIGHLIGHTS, Front and Back Face)]
        [Tooltip(Select the render faces that you want to show Specular Highlights.)]
        [SubEnum(_SPECULARHIGHLIGHTS,Both,0,Front,2,Back,1)]_SpecularApplytoFaces("Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Environment Reflections
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(Display scene Reflection Probe result.)]
        [Tooltip(If the reflection is black, you can try rebake reflection probe in scene.)]
        [Tooltip(Usually used for Metal reflection, Glass or Clearcoat.)]
        [Main(_EnvironmentReflectionGroup,_ENVIRONMENTREFLECTIONS)]_ReceiveEnvironmentReflection("Environment Reflections", Float) = 0

        [Helpbox(Display the Reflection Probe of the current scene.)]
        [Helpbox()]    
        [Helpbox(.    Smoothness(Roughness) will affect the reflection probe sharpness.)]
        [Helpbox(.    If the reflection is black, you can try rebake reflection probe in scene.)]
        [Helpbox()]        
        [Helpbox(Usually used for Metal reflection, Glass or Clearcoat.)]
        
        [Title(_EnvironmentReflectionGroup,Apply Strength)]
        [Sub(_EnvironmentReflectionGroup)]_EnvironmentReflectionUsage("Strength", Range(0,1)) = 1
        [Sub(_EnvironmentReflectionGroup)]_EnvironmentReflectionShouldApplyToFaceArea(".    Face area", Range(0,1)) = 0

        [Title(_EnvironmentReflectionGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_EnvironmentReflectionGroup,Color and Brightness)]
        [Sub(_EnvironmentReflectionGroup)]_EnvironmentReflectionBrightness("Brightness", Range(0,128)) = 1
        [Sub(_EnvironmentReflectionGroup)][HDR]_EnvironmentReflectionColor("Color", Color) = (1,1,1)
        [Sub(_EnvironmentReflectionGroup)]_EnvironmentReflectionTintAlbedo("Tint Albedo", Range(0,1)) = 1

        [Title(_EnvironmentReflectionGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]
        [Title(_EnvironmentReflectionGroup,Blend Method)]
        [Sub(_EnvironmentReflectionGroup)]_EnvironmentReflectionApplyReplaceBlending("Replace", Range(0,1)) = 1
        [Sub(_EnvironmentReflectionGroup)]_EnvironmentReflectionApplyAddBlending("Add", Range(0,1)) = 0
        
        [Title(_EnvironmentReflectionGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_EnvironmentReflectionGroup,Roughness)]
        [Sub(_EnvironmentReflectionGroup)]_EnvironmentReflectionSmoothnessMultiplier("Smoothness Multiplier", Range(0,4)) = 1
  
        [Title(_EnvironmentReflectionGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]
      
        [Title(_EnvironmentReflectionGroup,Style)]
        [Sub(_EnvironmentReflectionGroup)]_EnvironmentReflectionFresnelEffect("Fresnel Effect", Range(0,1)) = 0
        [Sub(_EnvironmentReflectionGroup)]_EnvironmentReflectionFresnelPower(".    Power", Range(0.1,8)) = 1
        [MinMaxSlider(_EnvironmentReflectionGroup,_EnvironmentReflectionFresnelRemapStart,_EnvironmentReflectionFresnelRemapEnd)]_EnvironmentReflectionFresnelRemapMinMaxSlider("              Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_EnvironmentReflectionFresnelRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_EnvironmentReflectionFresnelRemapEnd("", Range(0.0,1.0)) = 1.0

        [Title(_EnvironmentReflectionGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        // don't have a separated shader_feature for Environment Reflections's mask texture, because usually a Environment Reflections mask texture is needed anyway
        [Title(_EnvironmentReflectionGroup, Mask)]
        [Tooltip(white is show, black is hide.)]
        [Tex(_EnvironmentReflectionGroup,_EnvironmentReflectionMaskMapChannelMask)][NoScaleOffset]_EnvironmentReflectionMaskMap("Mask Map", 2D) = "white" {}
        [HideInInspector]_EnvironmentReflectionMaskMapChannelMask("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better
        [SubToggle(_EnvironmentReflectionGroup, _)]_EnvironmentReflectionMaskMapInvertColor("              Invert?", Float) = 0   
        [MinMaxSlider(_EnvironmentReflectionGroup,_EnvironmentReflectionMaskMapRemapStart,_EnvironmentReflectionMaskMapRemapEnd)]_EnvironmentReflectionMaskMapRemapMinMaxSlider("              Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_EnvironmentReflectionMaskMapRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_EnvironmentReflectionMaskMapRemapEnd("", Range(0.0,1.0)) = 1.0

        [Title(_EnvironmentReflectionGroup, Front and Back Face)]
        [SubEnum(_EnvironmentReflectionGroup,Both,0,Front,2,Back,1)]_EnvironmentReflectionApplytoFaces("Show in which faces?", Float) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Kajiya-Kay Specular (hair)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 300)]
        [Tooltip(Useful if you want dynamic hair reflection.)]
        [Tooltip(Require each hair strip UV all rotated to the same direction.)]
        [Main(_KAJIYAKAY_SPECULAR,_KAJIYAKAY_SPECULAR)]_UseKajiyaKaySpecular("Kajiya-Kay Specular (hair)", Float) = 0

        [Helpbox(Useful if you want dynamic hair reflection.)]
        [Helpbox(Require each hair strip UV all rotated to the same direction.)]
        
        [Title(_KAJIYAKAY_SPECULAR, Color)]
        [Sub(_KAJIYAKAY_SPECULAR)]_HairStrandSpecularOverallIntensity("Intensity", Float) = 1
        [Sub(_KAJIYAKAY_SPECULAR)]_HairStrandSpecularMixWithBaseMapColor("Multiply BaseMap", Range(0,1)) = 0.5

        [Title(_KAJIYAKAY_SPECULAR, Color Tint by texture)]
        [SubToggle(_KAJIYAKAY_SPECULAR,_KAJIYAKAY_SPECULAR_TEX_TINT)]_UseHairStrandSpecularTintMap("Enable?", Float) = 0
        [Tex(_KAJIYAKAY_SPECULAR)][NoScaleOffset]_HairStrandSpecularTintMap("    Tint Map", 2D) = "white" {}
        [Sub(_KAJIYAKAY_SPECULAR)]_HairStrandSpecularTintMapUsage("                 Strength", Range(0,1)) = 1
        [Title(_KAJIYAKAY_SPECULAR,.                UV Tiling(XY) Offset(ZW))]
        [Sub(_KAJIYAKAY_SPECULAR)]_HairStrandSpecularTintMapTilingXyOffsetZw("", Vector) = (1,1,0,0)

        [Title(_KAJIYAKAY_SPECULAR, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_KAJIYAKAY_SPECULAR, Shape)]
        [Sub(_KAJIYAKAY_SPECULAR)]_HairStrandSpecularShapeFrequency("Frequency", Range(0,2000)) = 750
        [Sub(_KAJIYAKAY_SPECULAR)]_HairStrandSpecularShapeShift("Shift ", Range(0,0.1)) = 0.015
        [Sub(_KAJIYAKAY_SPECULAR)]_HairStrandSpecularShapePositionOffset("Position Offset", Range(-1.0,1.0)) = 0

        [Title(_KAJIYAKAY_SPECULAR, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_KAJIYAKAY_SPECULAR, Main group)]
        [Sub(_KAJIYAKAY_SPECULAR)]_HairStrandSpecularMainIntensity("Intensity", Float) = 1
        [Sub(_KAJIYAKAY_SPECULAR)][HDR]_HairStrandSpecularMainColor("Color", Color) = (1,1,1)
        [Tooltip(It is the pow() exponent in shader)]
        [Sub(_KAJIYAKAY_SPECULAR)]_HairStrandSpecularMainExponent("Sharpness", Range(0,1000)) = 256

        [Title(_KAJIYAKAY_SPECULAR, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_KAJIYAKAY_SPECULAR, Second group)]
        [Sub(_KAJIYAKAY_SPECULAR)]_HairStrandSpecularSecondIntensity("Intensity", Float) = 1
        [Sub(_KAJIYAKAY_SPECULAR)][HDR]_HairStrandSpecularSecondColor("Color", Color) = (1,1,1)
        [Tooltip(It is the pow() exponent in shader)]
        [Sub(_KAJIYAKAY_SPECULAR)]_HairStrandSpecularSecondExponent("Sharpness", Range(0,1000)) = 128

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Ramp override Specular Style and Color)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Ramp Map (specular)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Hidden]
        
        [Main(_RampTextureSpecularGroup,_RAMP_SPECULAR)]_UseRampSpecularTex("Ramp Map (Specular)", Float) = 0 

        [Helpbox(Remap grayscale specular brightness value to a specular color ramp texture.)]
        [Helpbox(So you can define specular result by editing rgb and alpha of the ramp texture easily.)]
        
        [Title(_RampTextureSpecularGroup,Dynamic Ramp Map)]
        [Tooltip(only use rgb channel, ignoring a channel.)]
        [Ramp(_RampTextureSpecularGroup)][NoScaleOffset]_RampSpecularTex("Ramp Map", 2D) = "white" {}
        [Sub(_RampTextureSpecularGroup)]_RampSpecularTexSampleUvY("             UV.Y", Range(0,1)) = 0.5
        [Sub(_RampTextureSpecularGroup)]_RampSpecularWhitePoint("             White Point", Range(0.01,1)) = 0.5

        [Title(_RampTextureSpecularGroup,.            ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_RampTextureSpecularGroup,.             UV.Y Map)]
        [Tooltip(useful if vertically packed different ramp tex into a single ramp tex atla)]
        [SubToggle(_RampTextureSpecularGroup,_RAMP_SPECULAR_SAMPLE_UVY_TEX)]_UseRampSpecularSampleUvYTex("              Enable?", Float) = 0
        [Tooltip(Use G channel.)]
        [Tex(_RampTextureSpecularGroup)][NoScaleOffset]_RampSpecularSampleUvYTex("        UV.Y Map", 2D) = "Gray" {}

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Adding Self Glow Emission)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Emission
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //[ShowIf(_UIDisplayMode, GEqual, 100)]
        [Tooltip(Same as URP Lit shader Emission, for adding self Illumination or glow.)]
        [Tooltip(Especially useful if URP bloom or NiloToonBloom postprocess is active in volume.)]  
        [Tooltip(Besides regular emission use case,)] 
        [Tooltip(you can also enable it for eye highlight and let bloom postprocess do the diffusion filter similar to Japan anime.)] 
        [Main(_EMISSION,_EMISSION)]_UseEmission("Emission", Float) = 0
        
        [Helpbox(Same as URP Lit shader Emission, for adding self Illumination or glow.)]
        [Helpbox(Especially useful if URP bloom or NiloToonBloom postprocess is active in volume.)]  

        [Title(_EMISSION, Emission Map)]
        [Tooltip(Same as URP Lit shader _EmissionMap and _EmissionColor.)]
        [AdvancedHeaderProperty][Tex(_EMISSION, _EmissionColor)][NoScaleOffset]_EmissionMap("Emission Map", 2D) = "white" {}

        [Title(_EMISSION,UV Tiling(XY) Offset(ZW))]
        [Advanced][Sub(_EMISSION)]_EmissionMapTilingXyOffsetZw("", Vector) = (1,1,0,0)
        [Title(_EMISSION,UV Scroll Speed(XY))]
        [Advanced][Sub(_EMISSION)]_EmissionMapUVScrollSpeed("", Vector) = (0,0,0,0)

        [Title(_EMISSION, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_EMISSION, Intensity and Color)]
        [Tooltip(Controls the overall intensity of Emission, a higher value will affect HDR Bloom result.)]
        [Sub(_EMISSION)]_EmissionIntensity("Intensity ", Range(0,100)) = 1
        [HideInInspector][HDR]_EmissionColor("Color", Color) = (0,0,0)
        [Tooltip(Enable to tint the emission result by Base Map, usually you will turn this on when Emission Map is a grayscale mask.)]
        [Sub(_EMISSION)]_MultiplyBaseColorToEmissionColor("    Multiply Base Map", Range(0,1)) = 0
        [Tooltip(Enable to tint the emission result by Main Directional Light Color, usually you will turn this on when you want to make emission result acts like a non self glow or reflective material.)]
        [Sub(_EMISSION)]_MultiplyLightColorToEmissionColor("    Multiply Light Color", Range(0,1)) = 0

        [Title(_EMISSION, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_EMISSION, Emission Map as Mask)]
        [Tooltip(If enabled, shader will extract 1 float data from Emission Map, and not treating Emission Map as Color texture anymore.)]
        [Tooltip(Usually you should enable this if you packed a grayscale emission mask into 1 channel of your multi channel packed texture.)]
        [AdvancedHeaderProperty][SubToggle(_EMISSION, _)]_EmissionMapUseSingleChannelOnly("Is Emission Map a Mask?", Float) = 0
        [Tooltip(If the above toggle is enabled, pick a channel or method to extract 1 float data from Emission Map.)]
        [Advanced][ChannelDrawer(_EMISSION)]_EmissionMapSingleChannelMask("    Extract from", Vector) = (0,1,0,0)

        [Title(_EMISSION, Mask Map)]
        [Tooltip(white is show, black is hide.)]
        [AdvancedHeaderProperty][Tex(_EMISSION,_EmissionMaskMapChannelMask)][NoScaleOffset]_EmissionMaskMap("Mask Map", 2D) = "white" {}
        [Advanced][HideInInspector]_EmissionMaskMapChannelMask("", Vector) = (0,1,0,0) // use G as default value because if input is rgb grayscale texture, g is 1 bit better   
        [Advanced][SubToggle(_EMISSION, _)]_EmissionMaskMapInvertColor("               Invert?", Float) = 0
        [Advanced][MinMaxSlider(_EMISSION,_EmissionMaskMapRemapStart,_EmissionMaskMapRemapEnd)]_EmissionMaskMapRemapMinMaxSlider("               Remap", Range(0.0,1.0)) = 1.0
        [HideInInspector]_EmissionMaskMapRemapStart("", Range(0.0,1.0)) = 0.0
        [HideInInspector]_EmissionMaskMapRemapEnd("", Range(0.0,1.0)) = 1.0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Adding Outline)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Screen Space Outline
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 300)]
        [Main(_SCREENSPACE_OUTLINE,_SCREENSPACE_OUTLINE)]_RenderScreenSpaceOutline("Screen Space Outline", Float) = 0
        
        [Helpbox(You need to enable AllowRenderScreenSpaceOutline in NiloToonAllInOneRendererFeature.)]
        [Helpbox(You also need to turn on Intensity in NiloToonScreenSpaceOutlineControlVolume.)]

        [Title(_SCREENSPACE_OUTLINE, Width)]
        [Tooltip(The width of screen space outline. Setting it too high may produce wierd outline.)]
        [Sub(_SCREENSPACE_OUTLINE)]_ScreenSpaceOutlineWidth("Width", Range(0,10)) = 1
        [Tooltip(Same as the above Width, but for face area.)]
        [Tooltip(It is default 0, since screen space outline on face usually looks horrible.)]
        [Sub(_SCREENSPACE_OUTLINE)]_ScreenSpaceOutlineWidthIfFace("Width(face)", Range(0,10)) = 0

        [Title(_SCREENSPACE_OUTLINE, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_SCREENSPACE_OUTLINE, Depth Sensitivity)]
        [Tooltip(The higher the sensitivity, the easier for outline to appear on areas that has screen space Depth difference.)]
        [Sub(_SCREENSPACE_OUTLINE)]_ScreenSpaceOutlineDepthSensitivity("Sensitivity", Range(0,10)) = 1
        [Tooltip(Same as the above Sensitivity, but for face area.)]
        [Sub(_SCREENSPACE_OUTLINE)]_ScreenSpaceOutlineDepthSensitivityIfFace("Sensitivity(face)", Range(0,10)) = 1

        // not writing shader_feature for this section, because there are many shader_feature already
        [Title(_SCREENSPACE_OUTLINE, Depth Sensitivity Multiplier Map)]
        [Tooltip(You can multiply Depth Sensitivity by a float value extracted from Sensitivity Map.)]
        [Tex(_SCREENSPACE_OUTLINE,_ScreenSpaceOutlineDepthSensitivityTexChannelMask)][NoScaleOffset]_ScreenSpaceOutlineDepthSensitivityTex("Sensitivity Map", 2D) = "white" {}
        [HideInInspector]_ScreenSpaceOutlineDepthSensitivityTexChannelMask("", Vector) = (0,1,0,0)
        [MinMaxSlider(_SCREENSPACE_OUTLINE,_ScreenSpaceOutlineDepthSensitivityTexRemapStart,_ScreenSpaceOutlineDepthSensitivityTexRemapEnd)]_ScreenSpaceOutlineDepthSensitivityTexRemapMinMaxSlider("              Remap", Range(0,1)) = 1
        [HideInInspector]_ScreenSpaceOutlineDepthSensitivityTexRemapStart("", Range(0,1)) = 0
        [HideInInspector]_ScreenSpaceOutlineDepthSensitivityTexRemapEnd("", Range(0,1)) = 1

        [Title(_SCREENSPACE_OUTLINE, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_SCREENSPACE_OUTLINE, Normals Sensitivity)]
        [Tooltip(The higher the sensitivity, the easier for outline to appear on areas that has screen space Normals difference.)]
        [Sub(_SCREENSPACE_OUTLINE)]_ScreenSpaceOutlineNormalsSensitivity("Sensitivity", Range(0,10)) = 1
        [Tooltip(Same as the above Sensitivity, but for face area.)]
        [Sub(_SCREENSPACE_OUTLINE)]_ScreenSpaceOutlineNormalsSensitivityIfFace("Sensitivity(face)", Range(0,10)) = 1

        // not writing shader_feature for this section, because there are many shader_feature already
        [Title(_SCREENSPACE_OUTLINE,Normals Sensitivity Multiplier Map)]
        [Tooltip(You can multiply Normals Sensitivity by a float value extracted from Sensitivity Map.)]
        [Tex(_SCREENSPACE_OUTLINE,_ScreenSpaceOutlineNormalsSensitivityTexChannelMask)][NoScaleOffset]_ScreenSpaceOutlineNormalsSensitivityTex("Sensitivity Map", 2D) = "white" {}
        [HideInInspector]_ScreenSpaceOutlineNormalsSensitivityTexChannelMask("", Vector) = (0,1,0,0)
        [MinMaxSlider(_SCREENSPACE_OUTLINE,_ScreenSpaceOutlineNormalsSensitivityTexRemapStart,_ScreenSpaceOutlineNormalsSensitivityTexRemapEnd)]_ScreenSpaceOutlineNormalsSensitivityTexRemapMinMaxSlider("              Remap", Range(0,1)) = 1
        [HideInInspector]_ScreenSpaceOutlineNormalsSensitivityTexRemapStart("", Range(0,1)) = 0
        [HideInInspector]_ScreenSpaceOutlineNormalsSensitivityTexRemapEnd("", Range(0,1)) = 1

        [Title(_SCREENSPACE_OUTLINE, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_SCREENSPACE_OUTLINE, Tint Color)]
        [Tooltip(The tint color of screen space outline.)]
        [Sub(_SCREENSPACE_OUTLINE)][HDR]_ScreenSpaceOutlineTintColor("Tint Color", Color) = (0.1,0.1,0.1,1)
        [Tooltip(An extra tint color of screen space outline, for occlusion area.)]
        [Sub(_SCREENSPACE_OUTLINE)][HDR]_ScreenSpaceOutlineOcclusionAreaTintColor("Occlusion Tint Color", Color) = (1,1,1)

        [Title(_SCREENSPACE_OUTLINE,Replace Color)]
        [Tooltip(You can replace outline result color to a single color.)]
        [Tooltip(This slider controls the Replace Strength.)]
        [Sub(_SCREENSPACE_OUTLINE)]_ScreenSpaceOutlineUseReplaceColor("Replace strength", Range(0,1)) = 0
        [Tooltip(You can replace outline result color to a single color.)]
        [Tooltip(This Color is the final outline color that you want to replace to.)]
        [Sub(_SCREENSPACE_OUTLINE)][HDR]_ScreenSpaceOutlineReplaceColor("    Replace to Color", Color) = (1,1,1,1)

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Screen Space Outline V2
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Hidden]
        [Main(_SCREENSPACE_OUTLINE_V2,_SCREENSPACE_OUTLINE_V2)]_RenderScreenSpaceOutlineV2("Screen Space Outline V2(Experimental)", Float) = 0
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Override Outline Color by texture
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 300)]
        [Tooltip(For overriding the final outline color by a texture.)]
        [Main(_OverrideOutlineColorByTextureGroup,_OVERRIDE_OUTLINECOLOR_BY_TEXTURE)]_UseOverrideOutlineColorByTexture("Override Outline Color", Float) = 0        
        
        [Helpbox(For overriding the final outline color by a texture.)]
        
        [Title(_OverrideOutlineColorByTextureGroup,Strength)]
        [Tooltip(How much should the outline color be overridden.)]
        [Sub(_OverrideOutlineColorByTextureGroup)]_OverrideOutlineColorByTexIntensity("Intensity", Range(0,1)) = 1

        [Title(_OverrideOutlineColorByTextureGroup,New Override Color)]
        [Tooltip(A texture that defines the overridden color of outline.)]
        [Tooltip(. RGB is the new overridden outline color)]
        [Tooltip(. A is mask)]
        [Tex(_OverrideOutlineColorByTextureGroup,_OverrideOutlineColorTexTintColor)][NoScaleOffset]_OverrideOutlineColorTex("Color Map", 2D) = "white" {}
        [HideInInspector][HDR]_OverrideOutlineColorTexTintColor("", Color) = (1,1,1,1)
        [Tooltip(Enable this toggle to force Color Map equals 1.)]
        [Sub(_OverrideOutlineColorByTextureGroup)]_OverrideOutlineColorTexIgnoreAlphaChannel("             Ignore Alpha?", Range(0,1)) = 0

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Per Character effect options)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Per Character effect mask
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 300)]
        [Tooltip(If you enabled effects in NiloToonPerCharacterRenderController, you can control if this material should receive that effect.)]
        [Main(_PerCharacterEffectGroup,_,off,off)]_PerCharacterEffectGroup("Allowed Per Character effects", Float) = 0

        [Title(_PerCharacterEffectGroup, Dither Fadeout)]
        [Tooltip(Useful when you want only some materials to apply the dither fadeout effect from NiloToonPerCharacterRenderController.)]
        [Tooltip()]
        [Tooltip(For example, when you only want to apply dither fadeout on an accessory material or a cloth material,)]
        [Tooltip(you can disable this toggle for every other materials, and only enable this toggle for the target materials.)]
        [SubToggle(_PerCharacterEffectGroup,_)]_AllowPerCharacterDitherFadeout("Allow Dither Fadeout?", Float) = 1

        [Title(_PerCharacterEffectGroup, Dissolve)]
        [Tooltip(Useful when you want only some materials to apply the dissolve effect from NiloToonPerCharacterRenderController.)]
        [Tooltip()]
        [Tooltip(For example, when you only want to apply dissolve on an accessory material or a cloth material,)]
        [Tooltip(you can disable this toggle for every other materials, and only enable this toggle for the target materials.)]
        [SubToggle(_PerCharacterEffectGroup,_)]_AllowPerCharacterDissolve("Allow Dissolve?", Float) = 1
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(NiloToon Bloom options)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Allow NiloToonBloom Character Area Override
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 300)]
        [Tooltip(If you enabled NiloToonBloomVolume, enabling this toggle means this material will use character area overridden params in NiloToonBloomVolume.)]
        [Main(_AllowNiloToonBloomCharacterAreaOverrideGroup,_)]_AllowNiloToonBloomCharacterAreaOverride("Allow Nilo Bloom Override?", Float) = 1

        [Title(_AllowNiloToonBloomCharacterAreaOverrideGroup, Override settings)]
        [Tooltip(How much character area override should be applied when rendering NiloToonBloomVolume for this material.)]
        [Tooltip(.    When set to 1, NiloToonBloom will apply character area override for this material.)]
        [Tooltip(.    When set to 0, NiloToonBloom will not apply character area override for this material.)]
        [Sub(_AllowNiloToonBloomCharacterAreaOverrideGroup)]_AllowedNiloToonBloomOverrideStrength("Override Strength", Range(0,1)) = 1

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Unity Asset Store asset support)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Cloth Dynamics
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 300)]
        [Tooltip(Enable this toggle will allow Cloth Dynamics(an AssetStore asset) to replace vertex data in vertex shader according to vertex ID.)]
        [Tooltip(Vertex data like)]
        [Tooltip(.    Position)]
        [Tooltip(.    Normal)]
        [Tooltip(.    Tangent)]
        [Tooltip(will be replaced to the resulting vertex data of GPU cloth simulation.)]
        [Tooltip()]
        [Tooltip(Please note that using this feature will require you to append a string to the shader name inside the shader file.)]
        [Tooltip(.    append cloth, e.g., NiloToon_Character_cloth for cloth material)]
        [Tooltip(.    append skinning, e.g., NiloToon_Character_skinning for GPU skinning material)]    
        [Tooltip(If you do not append these to the shader name, Cloth Dynamics will replace the shader in playmode.)]
        [Main(_ClothDynamicsGroup,_NILOTOON_SUPPORT_CLOTHDYNAMICS)]_SupportClothDynamics("Support 'Cloth Dynamics'", Float) = 0
        [Title(_ClothDynamicsGroup, No options for this group.)]

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [Title(Pass On Off)]
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Allowed Passes
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ShowIf(_UIDisplayMode, GEqual, 200)]
        [Tooltip(In most cases, you do NOT need to edit this section.)] 
        [Tooltip(But in some situations, you may want to disable rendering a shader pass of this material for performance or visual reason.)] 
        [Main(_PassOnOffGroup,_,off,off)]_PassOnOffGroup("Allowed Pass", Float) = 0

        [Title(_PassOnOffGroup, URP pass)]
        [Tooltip(Enable to allow drawing shader pass with LightMode ShadowCaster.)]
        [Tooltip()]
        [Tooltip(If you turn this off, NiloToon will not draw this material into any URP shadowmaps.)]
        [SubToggle(_PassOnOffGroup,_)]_AllowRenderURPShadowCasterPass("ShadowCaster", Float) = 1
        [Tooltip(Enable to allow drawing shader pass with LightMode DepthOnly or DepthNormals.)]
        [Tooltip()]
        [Tooltip(If you turn this off, NiloToon will not draw this material into any URP _CameraDepthTexture or _CameraNormalTexture.)]
        [SubToggle(_PassOnOffGroup,_)]_AllowRenderDepthOnlyOrDepthNormalsPass("DepthOnly/DepthNormals", Float) = 1

        [Title(_PassOnOffGroup, ........................................................................................................................................................................................................................................................................................................................................................................)]

        [Title(_PassOnOffGroup, NiloToon pass)]
        [Tooltip(Enable to allow drawing shader pass with LightMode NiloToonSelfShadowCaster.)]
        [Tooltip()]
        [Tooltip(If you turn this off, NiloToon will not draw this material into any NiloToon self shadowmaps.)]
        [SubToggle(_PassOnOffGroup,_)]_AllowRenderNiloToonSelfShadowPass("NiloToonSelfShadowCaster", Float) = 1
        [Tooltip(Enable to allow drawing shader pass with LightMode NiloToonExtraThickOutline.)]
        [Tooltip()]
        [Tooltip(If you turn this off, NiloToon will not draw this material when drawing extra thick outline.)]
        [SubToggle(_PassOnOffGroup,_)]_AllowRenderExtraThickOutlinePass("NiloToonExtraThickOutline", Float) = 1

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Set by script
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // everything inside your CBUFFER_START(UnityPerMaterial) must exist in this Properties{} section in order to make SRP batcher works
        // even properties that are only set by script and [HideInInspector]!
        // You can try comment out any line here and reimport the shader,
        // doing this will make SRP batching fail, you can check it by viewing .shader file in your Unity Editor inspector

        [HideInInspector]_RenderCharacter("_RenderCharacter", Float) = 1
        
        // identity if material is currently controlled by NiloToonPerCharacterRenderController script
        [HideInInspector]_ControlledByNiloToonPerCharacterRenderController("_ControlledByNiloToonPerCharacterRenderController", Float) = 0

        // Character ID for read global 1D array or load 1D texture
        // use Integer instead of Float, to avoid bug in adreno740 gpu when accessing array using _CharacterID, see https://issuetracker.unity3d.com/issues/android-shader-has-incorrect-uvs-when-built-on-android-devices-with-an-adreno-740-gpu
        // For Int vs Integer, see https://docs.unity3d.com/Manual/SL-Properties.html
        [HideInInspector]_CharacterID("_CharacterID", Integer) = 0 // use 0 instead of -1, to avoid array or texture read out of bound

        // per character gameplay effect
        [HideInInspector]_PerCharEffectTintColor("_PerCharEffectTintColor", Color) = (1,1,1)
        [HideInInspector]_PerCharEffectAddColor("_PerCharEffectAddColor", Color) = (0,0,0)
        [HideInInspector]_PerCharEffectDesaturatePercentage("_PerCharEffectDesaturatePercentage", Range(0,1)) = 0
        [HideInInspector]_PerCharEffectLerpColor("_PerCharEffectLerpColor", Color) = (1,1,0,0)
        [HideInInspector]_PerCharEffectRimColor("_PerCharEffectRimColor", Color) = (0,0,0)
        [HideInInspector]_PerCharEffectRimSharpnessPower("_PerCharEffectRimSharpnessPower", Float) = 4
        [HideInInspector]_PerCharacterOutlineColorLerp("_PerCharacterOutlineColorLerp", Color) = (1,1,1,0)

        // per character set up
        [HideInInspector]_PerCharacterBaseColorTint("_PerCharacterBaseColorTint", Color) = (1,1,1)
        [HideInInspector]_PerCharacterOutlineWidthMultiply("_PerCharacterOutlineWidthMultiply", Float) = 1
        [HideInInspector]_PerCharacterOutlineColorTint("_PerCharacterOutlineColorTint", Color) = (1,1,1)

        // per character outline
        [HideInInspector]_PerCharacterRenderOutline("_PerCharacterRenderOutline", Float) = 1

        // color fill
        [HideInInspector]_CharacterAreaColorFillEnabled("_CharacterAreaColorFillEnabled", Float) = 0
        [HideInInspector]_CharacterAreaColorFillColor("_CharacterAreaColorFillColor", Color) = (1,1,1,0.5)
        [HideInInspector]_CharacterAreaColorFillTexture("_CharacterAreaColorFillTexture", 2D) = "White" {}
        [HideInInspector]_CharacterAreaColorFillTextureUVIndex("_CharacterAreaColorFillTextureUVIndex", Float) = 5 // 5 = CharBoundUV
        [HideInInspector]_CharacterAreaColorFillTextureUVTilingOffset("_CharacterAreaColorFillTextureUVTilingOffset", Vector) = (1,1,0,0)
        [HideInInspector]_CharacterAreaColorFillTextureUVScrollSpeed("_CharacterAreaColorFillTextureUVScrollSpeed", Vector) = (0,0,0)
        [HideInInspector]_CharacterAreaColorFillRendersVisibleArea("_CharacterAreaColorFillRendersVisibleArea", Float) = 0
        [HideInInspector]_CharacterAreaColorFillRendersBlockedArea("_CharacterAreaColorFillRendersBlockedArea", Float) = 0
        
        // extra thick outline
        [HideInInspector]_ExtraThickOutlineEnabled("_ExtraThickOutlineEnabled", Float) = 0
        [HideInInspector]_ExtraThickOutlineZWrite("_ExtraThickOutlineZWrite", Float) = 0
        [HideInInspector]_ExtraThickOutlineWriteIntoDepthTexture("_ExtraThickOutlineWriteIntoDepthTexture", Float) = 0
        [HideInInspector]_ExtraThickOutlineWidth("_ExtraThickOutlineWidth", Range(0,100)) = 4
        [HideInInspector]_ExtraThickOutlineViewSpacePosOffset("_ExtraThickOutlineViewSpacePosOffset", Vector) = (0,0,0)
        [HideInInspector]_ExtraThickOutlineColor("_ExtraThickOutlineColor", Color) = (1,1,1,1)
        [HideInInspector]_ExtraThickOutlineZOffset("_ExtraThickOutlineZOffset", Float) = -0.1
        [HideInInspector]_ExtraThickOutlineMaxFinalWidth("_ExtraThickOutlineMaxFinalWidth", Float) = 100

        // face
        [HideInInspector]_FaceForwardDirection("_FaceForwardDirection", Vector) = (0,0,1)
        [HideInInspector]_FaceUpDirection("_FaceUpDirection", Vector) = (0,1,0)
        [HideInInspector]_FixFaceNormalAmount("_FixFaceNormalAmount", Range(0,1)) = 1
        [HideInInspector]_FixFaceNormalUseFlattenOrProxySphereMethod("_FixFaceNormalUseFlattenOrProxySphereMethod", Range(0,1)) = 0

        // per char center
        [HideInInspector]_CharacterBoundCenterPosWS("_CharacterBoundCenterPosWS", Vector) = (0,0,0)
        [HideInInspector]_CharacterBoundRadius("_CharacterBoundRadius", Float) = 1.25

        // EnableDepthTextureRimLightAndShadow
        [HideInInspector]_NiloToonEnableDepthTextureRimLightAndShadow("_NiloToonEnableDepthTextureRimLightAndShadow", Float) = 0
        
        // dither fadeout
        [HideInInspector]_DitherFadeoutAmount("_DitherFadeoutAmount", Range(0,1)) = 0
        [HideInInspector]_DitherFadeoutNormalScaleFix("_DitherFadeoutNormalScaleFix", Range(0,16)) = 1
        // dissolve
        [HideInInspector]_DissolveAmount("_DissolveAmount", Range(0,1)) = 0
        [HideInInspector]_DissolveMode("_DissolveMode", Integer) = 0 // if C# uses Material.SetInt(), then shader must use Integer instead of Float in shader
        [HideInInspector]_DissolveThresholdMap("_DissolveThresholdMap", 2D) = "linearGrey" {}
        [HideInInspector]_DissolveThresholdMapTilingX("_DissolveThresholdMapTilingX", Float) = 1
        [HideInInspector]_DissolveThresholdMapTilingY("_DissolveThresholdMapTilingY", Float) = 1
        [HideInInspector]_DissolveNoiseStrength("_DissolveNoiseStrength", Float) = 1
        [HideInInspector]_DissolveBorderRange("_DissolveBorderRange", Range(0,1)) = 0.02
        [HideInInspector]_DissolveBorderTintColor("_DissolveBorderTintColor", Color) = (0,4,4,1)

        // BaseMap Override
        [HideInInspector]_PerCharacterBaseMapOverrideAmount("_PerCharacterBaseMapOverrideAmount", Range(0,1)) = 0
        [HideInInspector]_PerCharacterBaseMapOverrideMap("_PerCharacterBaseMapOverrideMap", 2D) = "White" {}
        [HideInInspector]_PerCharacterBaseMapOverrideTilingOffset("_PerCharacterBaseMapOverrideTilingOffset", Vector) = (1,1,0,0)
        [HideInInspector]_PerCharacterBaseMapOverrideUVScrollSpeed("_PerCharacterBaseMapOverrideUVScrollSpeed", Vector) = (0,0,0,0)
        [HideInInspector]_PerCharacterBaseMapOverrideTintColor("_PerCharacterBaseMapOverrideTintColor", Color) = (1,1,1)
        [HideInInspector]_PerCharacterBaseMapOverrideUVIndex("_PerCharacterBaseMapOverrideUVIndex", Float) = 0
        [HideInInspector]_PerCharacterBaseMapOverrideBlendMode("_PerCharacterBaseMapOverrideBlendMode", Float) = 0

        // receive shadowMaps
        [HideInInspector]_PerCharReceiveAverageURPShadowMap("_PerCharReceiveAverageURPShadowMap", Range(0,1)) = 1
        [HideInInspector]_PerCharReceiveStandardURPShadowMap("_PerCharReceiveStandardURPShadowMap", Range(0,1)) = 1
        [HideInInspector]_PerCharReceiveNiloToonSelfShadowMap("_PerCharReceiveNiloToonSelfShadowMap", Range(0,1)) = 1

        // perspective removal
        [HideInInspector]_PerspectiveRemovalAmount("_PerspectiveRemovalAmount", Range(0,1)) = 0
        [HideInInspector]_PerspectiveRemovalRadius("_PerspectiveRemovalRadius", Float) = 1
        [HideInInspector]_HeadBonePositionWS("_HeadBonePositionWS", Vector) = (0,0,0)
        [HideInInspector]_PerspectiveRemovalStartHeight("_PerspectiveRemovalStartHeight", Float) = 0 // ground
        [HideInInspector]_PerspectiveRemovalEndHeight("_PerspectiveRemovalEndHeight", Float) = 1 // a point above ground and below character head

        // Per Char ZOffset
        [HideInInspector]_PerCharZOffset("_PerCharZOffset", Float) = 0
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Per char script's Passes  
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        // These lines are disabled, which means let these passes = default on, in order to make using these features in edit mode possible.
        // But comes with a disadvantage of "always drawing these passes" in edit mode(non-material instanced mode) even user don't need it,
        // disadvantages are:
        // - edit mode performance is slower due to drawing useless pass
        // - edit mode frame debugger is polluted by these passes due to drawing useless pass
        //[PassSwitch(NiloToonCharacterAreaStencilBufferFill)]_EnableNiloToonCharacterAreaStencilBufferFill("", Float) = 0
        //[PassSwitch(NiloToonExtraThickOutline)]_EnableNiloToonExtraThickOutline("", Float) = 0
        //[PassSwitch(NiloToonCharacterAreaColorFill)]_EnableNiloToonCharacterAreaColorFill("", Float) = 0
                
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Unsupported BRP properties
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [HideInInspector]_MainTex("_MainTex", 2D) = "White" {}
    }
    SubShader
    {       
        Tags 
        {
            // SRP introduced a new "RenderPipeline" tag in Subshader. This allows you to create shaders
            // that can match multiple render pipelines. If a RenderPipeline tag is not set it will match
            // any render pipeline. In case you want your SubShader to only run in URP, set the tag to
            // "UniversalPipeline"

            // Universal Pipeline tag is required. because we only want this shader to run in URP.
            // If Universal render pipeline is not set in the graphics settings, this SubShader will fail.

            // One can add a SubShader below or fallback to Standard built-in to make this
            // material work with both Universal Render Pipeline and Builtin Unity Pipeline

            // The tag value is "UniversalPipeline", not "UniversalRenderPipeline", be careful!
            // see -> https://github.com/Unity-Technologies/Graphics/pull/1431/
            "RenderPipeline" = "UniversalPipeline"

            // [need ShaderModel 4.5 because of SRP-batcher]
            // URP's ComplexLit.shader also require ShaderModel 4.5
            // ShaderModel 4.5 = OpenGL ES 3.1 capabilities (DX11 SM5.0 on D3D platforms, just without tessellation shaders)
            // Not supported on DX11 before SM5.0, OpenGL before 4.3 (i.e. Mac), OpenGL ES 2.0/3.0.
            // Supported on DX11+ SM5.0, OpenGL 4.3+, OpenGL ES 3.1, Metal, Vulkan, PS4/XB1 consoles.
            // Has compute shaders, random access texture writes, atomics and so on. No geometry or tessellation shaders.
            // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
            "ShaderModel"="4.5" 

            // explicit SubShader tag to avoid confusion
            "RenderType"="Opaque"
            "UniversalMaterialType" = "ComplexLit" // "UniversalMaterialType" is only used in deferred rendering, where each type of material are marked by stencil. If this tag is not set in a Pass, Unity uses the Lit value.
            
            "IgnoreProjector" = "True"
            "Queue"="Geometry"
        }

        // No LOD defined
        //LOD 300
    
        // We can extract duplicated hlsl code from all passes into this HLSLINCLUDE section to reduce duplicated code.
        // All Passes will use these keywords
        HLSLINCLUDE

        // these can affact alpha, which affects alpha clipping result, so they are required by all passes
        #pragma shader_feature_local_fragment _ALPHAOVERRIDEMAP
        #pragma shader_feature_local _ALPHATEST_ON // MotionVector pass need this in vertex shader, so we can't use shader_feature_local_fragment here

        #pragma shader_feature_local_fragment _BASEMAP_STACKING_LAYER1
        #pragma shader_feature_local_fragment _BASEMAP_STACKING_LAYER2
        #pragma shader_feature_local_fragment _BASEMAP_STACKING_LAYER3
        #pragma shader_feature_local_fragment _BASEMAP_STACKING_LAYER4
        #pragma shader_feature_local_fragment _BASEMAP_STACKING_LAYER5
        #pragma shader_feature_local_fragment _BASEMAP_STACKING_LAYER6
        #pragma shader_feature_local_fragment _BASEMAP_STACKING_LAYER7
        #pragma shader_feature_local_fragment _BASEMAP_STACKING_LAYER8
        #pragma shader_feature_local_fragment _BASEMAP_STACKING_LAYER9
        #pragma shader_feature_local_fragment _BASEMAP_STACKING_LAYER10

        // can be strip by user in NiloToonAllInOneRendererFeature's stripping setting - NiloToonShaderStrippingSettingSO
        #pragma multi_compile _ _NILOTOON_FORCE_MINIMUM_SHADER 
        #pragma multi_compile_local _ _NILOTOON_DISSOLVE

        // to support Unity AssetStore asset "ClothDynamics"
        #pragma shader_feature_local _NILOTOON_SUPPORT_CLOTHDYNAMICS
        #pragma shader_feature_local USE_BUFFERS // set by Unity AssetStore asset "ClothDynamics"'s GPUClothDynamics.cs script

        // [NVIDIA Nsight shader profiling]
        // you can enable these 2 lines when you want to use shader profiler in NVIDIA Nsight Graphics
        // WARNING: DO NOT enable these on any public release! Doing that will leak the hlsl source code completely
        //#pragma enable_d3d11_debug_symbols
        //#pragma use_dxc
        
        ENDHLSL

        // [#0 Pass - ForwardLit]
        // Forward only pass.
        // Acts also as an opaque forward fallback for deferred rendering.

        // Shades GI, all lights, emission, and fog in a single pass.
        // Compared to the Builtin pipeline forward renderer, URP forward renderer will
        // render a scene with multiple lights(additional light) with fewer draw calls and less overdraw.
        Pass
        {               
            Name "ForwardLit"
            Tags
            {
                // "Lightmode" matches the "ShaderPassName" set in UniversalRenderPipeline.cs. 
                // SRPDefaultUnlit and passes with no LightMode tag are also rendered by Universal Render Pipeline

                // In this pass, 
                // -For Unity2020.3 or below(Support of Unity2020.3 was removed since NiloToon 0.12.1), "Lightmode" tag must be "UniversalForward" in order to render lit objects by URP's ForwardRenderer.
                // -For Unity2021.3 or above, "Lightmode" tag must be "UniversalForwardOnly" in order to render lit objects by URP's ForwardRendering.
                // see "ShaderLab Pass tags" in https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@12.0/manual/rendering/deferred-rendering-path.html
                "LightMode" = "UniversalForwardOnly"
            }

            // Render state (URP's ComplexLit.shader)
            Blend [_SrcBlend] [_DstBlend] , One OneMinusSrcAlpha // Alpha Op set to "One OneMinusSrcAlpha" (or "OneMinusDstAlpha One" is also correct), so semi-transparent material can update RenderTarget's a correctly, see https://twitter.com/MuRo_CG/status/1511543317883863045
            ZWrite [_ZWrite]
            Cull [_Cull]

            // Render state (NiloToon extra added)
            ColorMask [_ColorMask]
            ZTest [_ZTest]
            
            // TODO: if you enabled MSAA, it is possible to have Anti-aliased Alpha Test result by using "Alpha To Coverage(AlphaToMask)" .
            // read "Anti-aliased Alpha Test: The Esoteric Alpha To Coverage" by Ben Golus
            // https://bgolus.medium.com/anti-aliased-alpha-test-the-esoteric-alpha-to-coverage-8b177335ae4f
            // https://developer.arm.com/documentation/102073/0100/Alpha-compositing
            // https://docs.unity3d.com/Manual/SL-AlphaToMask.html
            
            // If you use AlphaToMask, it will require the following fix:
            // 1. turn AlphaToMask On (This command is intended for use with MSAA. If you enable alpha-to-coverage mode when you are not using MSAA, the results can be unpredictable; different graphics APIs and GPUs handle this differently.)
            // 2. remove clip() / discard() function calls
            // 3. fragment shader's output alpha -> col.a = (col.a - _Cutoff) / max(fwidth(col.a), 0.0001) + 0.5;
            // 4. turn on "Mip Maps Preserve Coverage" in Unity's texture importer
            
            // If you DO NOT use AlphaToMask, you can use these alternatives:
            // 1. Traditional alpha test - This is the default alternative and what is most commonly used. However, it causes noticeable aliasing as the article shows. It also does not work well at a distance.
            // 2. Alpha blending - This provides smooth edges, but has depth sorting issues if transparent objects overlap.
            // 3. Two-pass rendering - Render the object once with alpha test for correct sorting, and again with alpha blending for smooth edges. This adds some performance overhead.
            // 4. Order independent transparency - Approximation techniques like depth peeling that correctly sort overlapping transparent objects. Can be expensive.
            // 5. Temporal anti-aliasing (TAA) - A post-process effect that filters aliased edges over multiple frames. Works well but adds latency.
            // 6. Supersampling - Rendering at a higher resolution and downsampling can smooth aliased edges. Adds significant performance cost.
            
            // Let user fully control the 2nd-8th bit of stencil buffer per material, 
            // but leaving the 1st bit for NiloToon to write "Character or Classic Outline" area
            // https://docs.unity3d.com/Manual/SL-Stencil.html
            Stencil 
            {
                Ref [_StencilRef] // user fully control
                ReadMask 127 // 01111111 (only allow 2nd-8th bit for user fully control)
                WriteMask 127 // 01111111 (only allow 2nd-8th bit for user fully control)
                Comp [_StencilComp] // user fully control
                Pass [_StencilPass] // user fully control
            }

            HLSLPROGRAM

            //#pragma exclude_renderers gles gles3 glcore // no need to exclude any renderers, all platforms will run the same Pass

            // no need to target 4.5, all platforms will run the same Pass(including WebGL = maximum 3.5) 
            // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
            //#pragma target 4.5

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Material Keywords (similar to URP's ComplexLit.shader's Material Keywords)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma shader_feature_local _NORMALMAP // need TangentWS from vertex shader also, so shader_feature_local_fragment is not enough
            #pragma shader_feature_local _PARALLAXMAP // need TangentWS from vertex shader also, so shader_feature_local_fragment is not enough
            #pragma shader_feature_local_fragment _RECEIVE_URP_SHADOW // URP ComplexLit.shader use _RECEIVE_SHADOWS_OFF to save a keyword, here we use an inverted keyword
            
            // URP ComplexLit.shader use _ _DETAIL_MULX2 _DETAIL_SCALED, here we simplify them into a single keyword _DETAIL to reduce variant.
            #pragma shader_feature_local _DETAIL // need TangentWS from vertex shader also, so shader_feature_local_fragment is not enough

            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _SMOOTHNESSMAP
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS // URP ComplexLit.shader use _SPECULARHIGHLIGHTS_OFF to save a keyword, here we use an inverted keyword since it is default off
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_TEX_TINT
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS // URP ComplexLit.shader use _ENVIRONMENTREFLECTIONS_OFF to save a keyword, here we use an inverted keyword, here we use an inverted keyword since it is default off
            
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Material keywords (NiloToon specific shader_feature)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma shader_feature_local _ISFACE // can affect vertex shader, so shader_feature_local_fragment is not enough 
            #pragma shader_feature_local _FACE_MASK_ON // can affect vertex shader, so shader_feature_local_fragment is not enough
            #pragma shader_feature_local_fragment _SKIN_MASK_ON

            // TODO: recheck if it is still true in 2021.3LTS:
            // can't use shader_feature_local_vertex, 
            // else mobile build will not include this varient, not sure why. 
            // Now use shader_feature_local as a workaround          
            #pragma shader_feature_local _ZOFFSETMAP 

            #pragma shader_feature_local_fragment _MATCAP_BLEND
            #pragma shader_feature_local_fragment _MATCAP_ADD
            #pragma shader_feature_local_fragment _MATCAP_OCCLUSION

            #pragma shader_feature_local_fragment _RAMP_LIGHTING
            #pragma shader_feature_local_fragment _RAMP_LIGHTING_SAMPLE_UVY_TEX
            #pragma shader_feature_local_fragment _RAMP_SPECULAR
            #pragma shader_feature_local_fragment _RAMP_SPECULAR_SAMPLE_UVY_TEX

            #pragma shader_feature_local_fragment _SHADING_GRADEMAP

            #pragma shader_feature_local _DYNAMIC_EYE // need TangentWS from vertex shader also, so shader_feature_local_fragment is not enough

            #pragma shader_feature_local _KAJIYAKAY_SPECULAR // need TangentWS from vertex shader also, so shader_feature_local_fragment is not enough
            #pragma shader_feature_local_fragment _KAJIYAKAY_SPECULAR_TEX_TINT

            #pragma shader_feature_local_fragment _SCREENSPACE_OUTLINE
            #pragma shader_feature_local_fragment _SCREENSPACE_OUTLINE_V2

            #pragma shader_feature_local_fragment _OVERRIDE_SHADOWCOLOR_BY_TEXTURE

            #pragma shader_feature_local_fragment _OVERRIDE_OUTLINECOLOR_BY_TEXTURE // we still need this if ForwardLit pass want to produce outline color

            #pragma shader_feature_local_fragment _DEPTHTEX_RIMLIGHT_SHADOW_WIDTHMAP
            #pragma shader_feature_local_fragment _DEPTHTEX_RIMLIGHT_OPACITY_MASKMAP
            #pragma shader_feature_local_fragment _DEPTHTEX_RIMLIGHT_FIX_DOTTED_LINE_ARTIFACTS

            #pragma shader_feature_local_fragment _NILOTOON_SELFSHADOW_INTENSITY_MAP

            #pragma shader_feature_local_fragment _FACE_SHADOW_GRADIENTMAP
            #pragma shader_feature_local_fragment _FACE_3D_RIMLIGHT_AND_SHADOW

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Material keywords (NiloToon specific multi_compile)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma multi_compile_fragment _ _NILOTOON_RECEIVE_URP_SHADOWMAPPING
            #pragma multi_compile_fragment _ _NILOTOON_RECEIVE_SELF_SHADOW
            #pragma multi_compile_local_fragment _ _NILOTOON_DITHER_FADEOUT
            #pragma multi_compile_local_fragment _ _NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE

            #pragma multi_compile_fragment _ _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE
            //#pragma multi_compile_fragment _ _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2 //WIP, temp disabled

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // NiloToon Material debug keywords (can strip when build)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma multi_compile _ _NILOTOON_DEBUG_SHADING // multi_compile_fragment is not enough, because we need to transfer uv8 in vertex shader
   
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Universal Pipeline keywords (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // These multi_compile variants are stripped from the build depending on:
            // 1) Settings in the URP Asset assigned in the GraphicsSettings at build time
            // e.g If you disabled AdditionalLights in the asset then all _ADDITIONA_LIGHTS variants
            // will be stripped from build
            // 2) Invalid combinations are stripped. e.g variants with _MAIN_LIGHT_SHADOWS_CASCADE
            // but not _MAIN_LIGHT_SHADOWS are invalid and therefore stripped.
            // You can read URP's ShaderPreprocessor.cs (which implements interface IPreprocessShaders) to view URP's shader stripping logic

            // [directly copied from URP14 ComplexLit.shader]
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            // [Hardcode define _SHADOWS_SOFT base on SHADER_API, instead of multi_compile, in order to reduce shader variant by 50%]
            // https://docs.unity3d.com/ScriptReference/Rendering.BuiltinShaderDefine.html
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT // original code
            #ifndef SHADER_API_MOBILE 
                #define _SHADOWS_SOFT 1
            #endif

            // [Character won't need lightmap related logic]
            //#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // TODO: not supported now, it may ruin visual style if we included SSAO in lighting, and will double the shader memory usage
            //#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            // [directly copied from URP14 ComplexLit.shader]
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #if UNITY_VERSION >= 202220
            #pragma multi_compile _ _FORWARD_PLUS
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #endif

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Unity defined keywords (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // [Character won't need lightmap related logic]
            //#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING 
            //#pragma multi_compile _ SHADOWS_SHADOWMASK
            //#pragma multi_compile _ DIRLIGHTMAP_COMBINED
            //#pragma multi_compile _ LIGHTMAP_ON
            //#pragma multi_compile _ DYNAMICLIGHTMAP_ON

            //#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE // Not worth to support this since it cost 2x shader memory
            #pragma multi_compile_fog
            //#pragma multi_compile_fragment _ DEBUG_DISPLAY // NiloToon didn't implement URP's debug display

            // for APV support
            #if UNITY_VERSION >= 60000011
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #endif
            
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // GPU Instancing (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // to support GPU instancing and Single Pass Stereo rendering(VR), you should add the following section
            // but here we disabled them because they are not worth the increase of memory usage and build time, 
            // and this shader didn't have the concept of instancing anyway
            //------------------------------------------------------------------------------------------------------------------------------
            //#pragma multi_compile_instancing
            //#pragma instancing_options renderinglayer
            //#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            //------------------------------------------------------------------------------------------------------------------------------

            #pragma vertex VertexShaderAllWork
            #pragma fragment FragmentShaderAllWork

            // because this pass is a ForwardLit pass
            // define "NiloToonForwardLitPass" to inject code into VertexShaderAllWork()
            #define NiloToonForwardLitPass 1

            // all shader logic written inside this .hlsl, remember to write all #define BEFORE writing #include
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"

            ENDHLSL
        }
        
        // [#1 Pass - Outline]
        // Very similar to the above "ForwardLit" pass, but: 
        // -vertex position are pushed(extrude) out a bit base on lighting normal / smoothed normal direction
        // -also color is tinted
        // -Cull Front instead of Cull Back because Cull Front is a must for all extrude mesh outline method
        Pass 
        {
            Name "Outline"
            Tags 
            {
                // [IMPORTANT note(Unity2020.3LTS)] 
                // If you don't have a RendererFeature to render your custom pass, DON'T write
                //"LightMode" = "UniversalForward"
                // in your custom pass! else your custom pass will not be rendered by URP!

                // [Important CPU performance note]
                // If you need to add a custom pass to your shader (extra outline pass, planar shadow pass, XRay pass when blocked....etc),
                // Please do the following:

                // For shader:
                // (1) Add a new Pass{} in your .shader file, just like this Pass{} section
                // (2) Write "LightMode" = "YourAwesomeCustomPassLightModeTag" inside that new Pass's Tags{} section
                "LightMode" = "NiloToonOutline" // here we set "NiloToonOutline" as custom pass's "LightMode" value, but it can be any string as long as it is matching the value in renderer feature

                // For RendererFeature(C#):
                // (1) Create a new RendererFeature C# script (right click in project window -> Create/Rendering/Universal Render Pipeline/Renderer Feature)
                // (2) Add that new RendererFeature to your ForwardRenderer.asset
                // (3) Write context.DrawRenderers() with ShaderTagId = "YourAwesomeCustomPassLightModeTag" in RendererPass's Execute() method

                // If done correctly, URP will render your new Pass{} for your shader, in a SRP-batcher friendly way (usually in 1 big SRP batch that containing lots of draw call)
            }

            // Render state (URP's ComplexLit.shader)
            Blend One Zero // TODO: should we expose the blending param for outline pass? are there any good use of a semi-transparent outline?
            ZWrite On
            Cull [_CullOutline]
            
            // Render state (NiloToon extra added)
            ColorMask [_ColorMask]
            ZTest [_ZTest]

            // Let user fully control the 2nd-8th bit of stencil buffer per material, 
            // but leaving the 1st bit for NiloToon to write "Character or Classic Outline" area
            // https://docs.unity3d.com/Manual/SL-Stencil.html
            Stencil 
            {
                Ref [_StencilRef] // user fully control
                ReadMask 127 // 01111111 (only allow 2nd-8th bit for user fully control)
                WriteMask 127 // 01111111 (only allow 2nd-8th bit for user fully control)
                Comp [_StencilComp] // user fully control
                Pass [_StencilPass] // user fully control
            }

            HLSLPROGRAM

            //#pragma exclude_renderers gles gles3 glcore // no need to exclude any renderers, all platforms will run the same Pass

            // no need to target 4.5, all platforms will run the same Pass(including WebGL = maximum 3.5) 
            // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
            //#pragma target 4.5

            // [For shader_feature and multi_compile]
            // it is similar but not the same when comparing to all keywords from the above "ForwardLit" pass,
            // this outline pass will ignore some keywords if it is not that important for this outline pass, to reduce compiled shader variant count
            // for notes, please see the above "ForwardLit" pass
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Material Keywords (similar to URP's ComplexLit.shader's Material Keywords)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////     
            // Note: outline need to receive URP shadow, else outline is not correctly darken when character is within URP shadow
            #pragma shader_feature_local_fragment _RECEIVE_URP_SHADOW

            #pragma shader_feature_local_fragment _OCCLUSIONMAP // needs to affect outline color

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Material keywords (NiloToon Outline pass specific)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma shader_feature_local_vertex _OUTLINEWIDTHMAP
            #pragma shader_feature_local_vertex _OUTLINEZOFFSETMAP

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Material keywords (NiloToon specific shader_feature)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma shader_feature_local _ISFACE
            #pragma shader_feature_local _FACE_MASK_ON
            #pragma shader_feature_local_fragment _SKIN_MASK_ON // _OutlineTintColorSkinAreaOverride need this

            #pragma shader_feature_local _ZOFFSETMAP

            #pragma shader_feature_local_fragment _MATCAP_BLEND

            #pragma shader_feature_local_fragment _SHADING_GRADEMAP // needs to affect outline color

            #pragma shader_feature_local_fragment _OVERRIDE_OUTLINECOLOR_BY_TEXTURE
            
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Material keywords (NiloToon specific multi_compile)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma multi_compile_fragment _ _NILOTOON_RECEIVE_URP_SHADOWMAPPING
            #pragma multi_compile_local_fragment _ _NILOTOON_DITHER_FADEOUT
            #pragma multi_compile_local_fragment _ _NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // NiloToon Material debug keywords (can strip when build)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma multi_compile _ _NILOTOON_DEBUG_SHADING
            
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Universal Pipeline keywords (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // [directly copied from URP14 ComplexLit.shader]
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            // for outline pass,
            // 100% correct reflection is not that important for mobile, 
            // we prefer smaller shader memory usage on mobile
            #ifndef SHADER_API_MOBILE 
                #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
                #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #endif

            // [Hardcode define _SHADOWS_SOFT base on SHADER_API, instead of multi_compile, in order to reduce shader variant by 50%]
            // https://docs.unity3d.com/ScriptReference/Rendering.BuiltinShaderDefine.html
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT // original code
            #ifndef SHADER_API_MOBILE 
                #define _SHADOWS_SOFT 1
            #endif

            // [Character won't need lightmap related logic]
            //#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // TODO: not supported now, it may ruin visual style if we included SSAO in lighting, and will double the shader memory usage
            //#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            // [directly copied from URP14 ComplexLit.shader]
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #if UNITY_VERSION >= 202220
            #pragma multi_compile _ _FORWARD_PLUS
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #endif

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Unity defined keywords (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma multi_compile_fog
            // (except fog, all others removed)

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // GPU Instancing (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // (all removed)

            #pragma vertex VertexShaderAllWork
            #pragma fragment FragmentShaderAllWork

            // because this is an Outline pass, 
            // define "NiloToonSelfOutlinePass" to inject outline related code into both VertexShaderAllWork() and FragmentShaderAllWork()
            #define NiloToonSelfOutlinePass 1

            // all shader logic written inside this .hlsl, remember to write all #define BEFORE writing #include
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"

            ENDHLSL
        }

        // [#2 Pass - CharacterAreaStencilBufferFill, a stencil pre-pass for extra thick Outline]
        // Draw Character(including classic outline area) again, but only write 1 into the 1st bit of stencil buffer
        Pass 
        {
            Name "CharacterAreaStencilBufferFill"
            Tags 
            {
                // for notes on "LightMode" = "xxx", please see the above "Outline" Pass{}
                "LightMode" = "NiloToonCharacterAreaStencilBufferFill"
            }

            // Render state (This pass's sole purpose is to fill in the 1st bit of the stencil buffer, and it will not modify the color or depth buffer.)
            ZTest Always
            ZWrite Off
            Cull off // Cull off is the best choice, it is slower but it can cover as many character pixels as possible 
            ColorMask 0 

            // The stencil test is set to always pass.
            // The stencil buffer's write result value will be used by extra thick outline pass later,
            // the only goal of this pass is to prevent drawing extra thick outline on top of any character area pixels.
            // What this pass does is -> Always Draw 1 into the 1st bit of stencil buffer (e.g. 1*******), ignoring all depth test or stencil test
            // https://docs.unity3d.com/Manual/SL-Stencil.html
            Stencil 
            {
                Ref 128 // (10000000)
                WriteMask 128 // (10000000)
                Comp Always
                Pass Replace
            }          

            HLSLPROGRAM

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Material keywords (NiloToon Outline pass specific)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma shader_feature_local_vertex _OUTLINEWIDTHMAP
            
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Material keywords (NiloToon specific multi_compile)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma multi_compile_local_fragment _ _NILOTOON_DITHER_FADEOUT

            #pragma vertex VertexShaderAllWork
            #pragma fragment ExtraThickOutlineFragmentFunction // because code is too simple, we define the method within this Pass{} section

            // because this is an Outline pass, 
            // define "NiloToonCharacterAreaStencilBufferFillPass" to inject outline related code into VertexShaderAllWork()
            #define NiloToonCharacterAreaStencilBufferFillPass 1

            // all shader logic written inside this .hlsl, remember to write all #define BEFORE writing #include
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"

            half4 ExtraThickOutlineFragmentFunction(Varyings input, FRONT_FACE_TYPE IsFrontFace : FRONT_FACE_SEMANTIC) : SV_TARGET
            {
                BaseColorAlphaClipTest(input, IsFrontFace);

                return 0;
            }

            ENDHLSL
        }

        // [#3 Pass - extra thick Outline]
        // Same as "Outline"(Classic outline) pass, but extra thick and output only a single color(ignore fog)
        
        // TODO: it is possible to use Jump Flood Algorithm(JFA) to improve quality (https://bgolus.medium.com/the-quest-for-very-wide-outlines-ba82ed442cd9),
        // but 
        // - is it worth the performance cost and implementation cost?
        // - how to solve antialiasing?
        // - how to solve depth occlusion of the outline?
        Pass 
        {
            Name "ExtraThickOutline"
            Tags 
            {
                // for notes on "LightMode" = "xxx", please see the above "Outline" Pass{}
                "LightMode" = "NiloToonExtraThickOutline"
            }

            // Render state
            ZTest [_ExtraThickOutlineZTest]
            ZWrite [_ExtraThickOutlineZWrite]
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off // Cull off is the best choice, it is slower but it can cover as many character pixels as possible 
            ColorMask [_ColorMask]

            // Only draw if the 1st bit of stencil value is NOT 1(1 means it was character area and filled by #2 Pass)
            // https://docs.unity3d.com/Manual/SL-Stencil.html
            Stencil 
            {
                Ref 128
                ReadMask 128 // 10000000, use the 1st bit as mask for stencil test
                WriteMask 128
                Comp NotEqual
                Pass Replace // why replace is needed? because we need to write 1 to prevent later overdraw fragment shader redrawing the same pixel again, because extra thick outline color can be semi-transparent
            }          

            HLSLPROGRAM

            //#pragma exclude_renderers gles gles3 glcore // no need to exclude any renderers, all platforms will run the same Pass

            // no need to target 4.5, all platforms will run the same Pass(including WebGL = maximum 3.5) 
            // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
            //#pragma target 4.5

            #pragma multi_compile_local_fragment _ _NILOTOON_DITHER_FADEOUT

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // GPU Instancing (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // to support GPU instancing and Single Pass Stereo rendering(VR), you should add the following section
            // but here we disabled them because they are not worth the memory usage and build time increase, 
            // and the shader didn't have the concept of instancing anyway
            //------------------------------------------------------------------------------------------------------------------------------
            //#pragma multi_compile_instancing
            //#pragma instancing_options renderinglayer
            //#pragma multi_compile _ DOTS_INSTANCING_ON
            //------------------------------------------------------------------------------------------------------------------------------

            #pragma vertex VertexShaderAllWork
            #pragma fragment ExtraThickOutlineFragmentFunction // because code is too simple, we define the method within this Pass{} section

            // because this is an Outline pass, 
            // define "NiloToonExtraThickOutlinePass" to inject outline related code into VertexShaderAllWork()
            #define NiloToonExtraThickOutlinePass 1

            // all shader logic written inside this .hlsl, remember to write all #define BEFORE writing #include
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"

            half4 ExtraThickOutlineFragmentFunction(Varyings input, FRONT_FACE_TYPE IsFrontFace : FRONT_FACE_SEMANTIC) : SV_TARGET
            {
                BaseColorAlphaClipTest(input, IsFrontFace);
                
                return _ExtraThickOutlineColor;
            }

            ENDHLSL
        }

        // [#4 Pass - NiloToonCharacterAreaColorFill pass]
        // Draw Character(including classic outline area) again, but output only a single color(ignore fog)
        Pass 
        {
            Name "NiloToonCharacterAreaColorFill"
            Tags 
            {
                // for notes on "LightMode" = "xxx", please see the above "Outline" Pass{}
                "LightMode" = "NiloToonCharacterAreaColorFill"
            }

            // Render state
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off // Cull off is the best choice, it is slower but it can cover as many character pixels as possible 
            ColorMask RGB

            // Only draw if the 1st bit of stencil value is 1(1 means it was character area and filled by #2 Pass)
            // https://docs.unity3d.com/Manual/SL-Stencil.html
            Stencil 
            {
                Ref 128
                ReadMask 128 // 10000000, use the 1st bit as mask for stencil test
                WriteMask 128
                Comp Equal
                Pass Invert // why replace is needed? because we need to write 1 to prevent later overdraw fragment shader redrawing the same pixel again, because extra thick outline color can be semi-transparent
            }          

            HLSLPROGRAM

            //#pragma exclude_renderers gles gles3 glcore // no need to exclude any renderers, all platforms will run the same Pass

            // no need to target 4.5, all platforms will run the same Pass(including WebGL = maximum 3.5) 
            // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
            //#pragma target 4.5

            #pragma multi_compile_local_fragment _ _NILOTOON_DITHER_FADEOUT

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // GPU Instancing (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // to support GPU instancing and Single Pass Stereo rendering(VR), you should add the following section
            // but here we disabled them because they are not worth the memory usage and build time increase, 
            // and the shader didn't have the concept of instancing anyway
            //------------------------------------------------------------------------------------------------------------------------------
            //#pragma multi_compile_instancing
            //#pragma instancing_options renderinglayer
            //#pragma multi_compile _ DOTS_INSTANCING_ON
            //------------------------------------------------------------------------------------------------------------------------------

            #pragma vertex VertexShaderAllWork
            #pragma fragment NiloToonCharacterAreaColorFillFragmentFunction // because code is too simple, we define the method within this Pass{} section

            // because this is an Outline pass, 
            // define "NiloToonExtraThickOutlinePass" to inject outline related code into VertexShaderAllWork()
            #define NiloToonCharacterAreaColorFillPass 1

            // all shader logic written inside this .hlsl, remember to write all #define BEFORE writing #include
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"

            half4 NiloToonCharacterAreaColorFillFragmentFunction(Varyings input, FRONT_FACE_TYPE IsFrontFace : FRONT_FACE_SEMANTIC) : SV_TARGET
            {
                BaseColorAlphaClipTest(input, IsFrontFace);

                //////////////////////////////////////////////////////////////////////////////////////////
                // flip normalWS by IS_FRONT_VFACE()
                //////////////////////////////////////////////////////////////////////////////////////////
                float facing = IS_FRONT_VFACE(IsFrontFace, 1.0, -1.0);
                
                //////////////////////////////////////////////////////////////////////////////////////////
                // Init all data structs
                //////////////////////////////////////////////////////////////////////////////////////////
                UVData uvData;
                ToonLightingData lightingData;
                ToonSurfaceData surfaceData;
                InitAllData(input, facing, uvData, lightingData, surfaceData);
                
                half charVisibleArea = SAMPLE_TEXTURE2D_X(_NiloToonPrepassBufferTex, sampler_PointClamp, lightingData.normalizedScreenSpaceUV).g;

                float2 uv = uvData.GetUV(_CharacterAreaColorFillTextureUVIndex);
                uv -= 0.5;
                uv = CalcUV(uv,_CharacterAreaColorFillTextureUVTilingOffset,_Time.y,_CharacterAreaColorFillTextureUVScrollSpeed);
                uv += 0.5;
                half4 resultColor = _CharacterAreaColorFillColor * SAMPLE_TEXTURE2D_X(_CharacterAreaColorFillTexture, sampler_linear_repeat, uv);

                half renderArea = 0;
                if(_CharacterAreaColorFillRendersVisibleArea)
                {
                    renderArea += charVisibleArea;
                }
                if(_CharacterAreaColorFillRendersBlockedArea)
                {
                    renderArea += (1-charVisibleArea);
                }
                resultColor.a *= _CharacterAreaColorFillEnabled * saturate(renderArea);
                
                return resultColor;
            }

            ENDHLSL
        }
 
        // ShadowCaster pass. Used for rendering URP's shadow maps (see URP ComplexLit.shader's "ShadowCaster" pass for reference)
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            // Explicit render state to avoid confusion
            ZWrite On // the only goal of this pass is to write depth (shadow map's light space depth)!
            ZTest LEqual // early exit at Early-Z stage if possible (only possible if clip() does not exist)            
            ColorMask 0 // we don't care about color, we just want to write depth, ColorMask 0 will save some write bandwidth
            Cull [_Cull]

            HLSLPROGRAM

            //#pragma exclude_renderers gles gles3 glcore // no need to exclude any renderers, all platforms will run the same Pass

            // no need to target 4.5, all platforms will run the same Pass(including WebGL = maximum 3.5) 
            // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
            //#pragma target 4.5

            // we need clip() related keywords in this pass, which is already defined inside the HLSLINCLUDE block in SubShader level
            // ...

            // let dither fadeout affect URP's shadowmap(depth from light's view), then let URP's softshadow filter to average the dither holes in shadowmap, 
            // doing this can let character cast shadows that looks like "semi-transparent" shadow
            #pragma multi_compile_local_fragment _ _NILOTOON_DITHER_FADEOUT

            // -------------------------------------
            // Universal Pipeline keywords
            // (X)

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // GPU Instancing (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // to support GPU instancing and Single Pass Stereo rendering(VR), you should add the following section
            // but here we disabled them because they are not worth the memory usage and build time increase, 
            // and the shader didn't have the concept of instancing anyway
            //------------------------------------------------------------------------------------------------------------------------------
            //#pragma multi_compile_instancing
            //#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            //------------------------------------------------------------------------------------------------------------------------------

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex VertexShaderAllWork
            #pragma fragment BaseColorAlphaClipTest // we only need to do Clip(), we do not need rgba color result for shading

            // because it is a ShadowCaster pass, define "NiloToonShadowCasterPass" to inject "remove shadow mapping artifact" code into VertexShaderAllWork().
            // We don't want to do outline extrude here because if we do it, the whole mesh will always receive self shadow.
            #define NiloToonShadowCasterPass 1

            // all shader logic written inside this .hlsl, remember to write all #define BEFORE writing #include
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"

            ENDHLSL
        }

        // (X)No GBuffer Pass, this shader is designed for forward lighting only.
        // Even project is in deferred mode, the actually rendering of this shader is still in Forward.
        // {...}

        // DepthOnly pass. Used for rendering URP's offscreen depth prepass _CameraDepthTexture (you can search DepthOnlyPass.cs in URP package)
        // When URP's depth texture is required, and if CopyDepthPass is not possible due to MSAA or renderer feature, 
        // URP will perform this offscreen depth prepass for this shader to draw character depth into _CameraDepthTexture
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            // Explicit render state to avoid confusion
            ZWrite On // the only goal of this pass is to write depth!
            ZTest LEqual // early exit at Early-Z stage if possible (only possible if clip() does not exist)
            ColorMask R // (URP13.1.8 or later changed from 0 to R, we will follow it) normally, we don't care about color, we just want to write depth, ColorMask 0 will save some write bandwidth
            Cull [_Cull]

            HLSLPROGRAM

            //#pragma exclude_renderers gles gles3 glcore // no need to exclude any renderers, all platforms will run the same Pass

            // no need to target 4.5, all platforms will run the same Pass(including WebGL = maximum 3.5) 
            // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
            //#pragma target 4.5

            // we need clip() related keywords in this pass, which is already defined inside the HLSLINCLUDE block in SubShader level
            // ...

            #pragma shader_feature_local_vertex _OUTLINEWIDTHMAP

            // for push back zoffset depth write of face vertices, to hide face depth texture self shadow artifact
            #pragma shader_feature_local _ISFACE
            #pragma shader_feature_local _FACE_MASK_ON

            // DITHER_FADEOUT is needed for this pass, do not disable it
            #pragma multi_compile_local_fragment _ _NILOTOON_DITHER_FADEOUT

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // GPU Instancing (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // to support GPU instancing and Single Pass Stereo rendering(VR), you should add the following section
            // but here we disabled them because they are not worth the memory usage and build time increase, 
            // and the shader didn't have the concept of instancing anyway
            //------------------------------------------------------------------------------------------------------------------------------
            //#pragma multi_compile_instancing
            //#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            //------------------------------------------------------------------------------------------------------------------------------

            #pragma vertex VertexShaderAllWork
            #pragma fragment BaseColorAlphaClipTest // we only need to do Clip(), no need color shading

            // because Outline area should write to depth also, define "NiloToonDepthOnlyOrDepthNormalPass" to inject outline related code into VertexShaderAllWork()
            // if depth write is correct, outline area will process depth of field correctly also.
            #define NiloToonDepthOnlyOrDepthNormalPass 1

            // all shader logic written inside this .hlsl, remember to write all #define BEFORE writing #include
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"

            ENDHLSL
        }

        // [This pass is used when drawing to _CameraNormalsTexture texture]
        // This pass is used when drawing to a _CameraNormalsTexture texture with the forward renderer or the depthNormal prepass with the deferred renderer.
        // Starting from URP 10, URP can generate a normal texture called _CameraNormalsTexture when renderer feature(e.g. SSAO or NiloToon's screen space outline) requires it. 
        // To render to this texture in your custom shader in URP 12 or later, add a Pass{} with the name "DepthNormalsOnly" and "LightMode" = "DepthNormalsOnly". 
        // For example, see the implementation in URP's ComplexLit.shader
        // *this pass is almost a direct copy of the above "DepthOnly" pass, but with the following changes:
        // - Name changed to DepthNormalsOnly
        // - LightMode changed to DepthNormalsOnly
        // - removed ColorMask R, because we need color rgba output
        // - #pragma fragment point to a function returning PackNormalOctRectEncode()
        // - include _NORMALMAP keyword
        Pass
        {
            Name "DepthNormalsOnly"

            // In this pass, 
            // For Unity2020.3 or below, "Lightmode" tag must be "DepthNormals" in order to render lit objects by URP's ForwardRenderer.
            // For Unity2021.3 or above, "Lightmode" tag must be "DepthNormalsOnly" in order to render lit objects by URP's ForwardRendering.
            // see "ShaderLab Pass tags" in https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@12.0/manual/rendering/deferred-rendering-path.html
            Tags{"LightMode" = "DepthNormalsOnly"}

            // Explicit render state to avoid confusion
            ZWrite On // one of the goal of this pass is to write depth into _CameraDepthTexture!
            ZTest LEqual // early exit at Early-Z stage if possible (only possible if clip() does not exist)            
            //ColorMask R // we NEED to write PackNormalOctRectEncode()'s result rg color data into _CameraNormalTexture color buffer, so don't write ColorMask R
            Cull [_Cull]

            HLSLPROGRAM

            //#pragma exclude_renderers gles gles3 glcore // no need to exclude any renderers, all platforms will run the same Pass

            // no need to target 4.5, all platforms will run the same Pass(including WebGL = maximum 3.5) 
            // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
            //#pragma target 4.5

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Material Keywords (similar to ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // these shader_feature will affect normal result, so we need to include them
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL

            // we need clip() related keywords in this pass, which is already defined inside the HLSLINCLUDE block in SubShader level
            // ...

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Material Keywords (NiloToon specific)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma shader_feature_local_vertex _OUTLINEWIDTHMAP

            // for push back zoffset depth write of face vertices, to hide face depth texture self shadow artifact
            #pragma shader_feature_local _ISFACE
            #pragma shader_feature_local _FACE_MASK_ON

            // DITHER_FADEOUT is needed for this pass, do not disable it
            #pragma multi_compile_local_fragment _ _NILOTOON_DITHER_FADEOUT

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Unity defined keywords
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT // forward-only variant
            #if UNITY_VERSION >= 202220
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #endif
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // GPU Instancing (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // to support GPU instancing and Single Pass Stereo rendering(VR), you should add the following section
            // but here we disabled them because they are not worth the memory usage and build time increase, 
            // and the shader didn't have the concept of instancing anyway
            //------------------------------------------------------------------------------------------------------------------------------
            //#pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON
            //------------------------------------------------------------------------------------------------------------------------------

            #pragma vertex VertexShaderAllWork
            #pragma fragment BaseColorAlphaClipTest_AndDepthNormalColorOutput // we need to do Clip(), and output normal as color

            // because Outline area should write to depth also, define "NiloToonDepthOnlyOrDepthNormalPass" to inject outline related code into VertexShaderAllWork()
            // if depth write is correct, outline area will process depth of field correctly also.
            #define NiloToonDepthOnlyOrDepthNormalPass 1 // currently we share DepthOnly pass's define using "NiloToonDepthOnlyOrDepthNormalPass", since these 2 passes are almost the same

            // all shader logic written inside this .hlsl, remember to write all #define BEFORE writing #include
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"
            ENDHLSL
        }

        // Copy and edited of URP17(Unity6 = Unity2023.3)'s Unlit.shader's MotionVectors pass 
        // https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/features/motion-vectors.html
        // TODO: we need to write an additional motion vector pass for "Classic Outline", else DLSS/TAA will have artifact on outline pixels
        Pass
        {
            // [PackageRequirements]
            // Force this motion vector pass only to run in URP16(Unity2023.2) or later
            // since URP versions before Unity2023.2 all have a default motion vector pass, 
            // although that default motion vector pass is not 100% correct with NiloToon(e.g., not matching to perspective removal/dither/dissolve)
            // we don't want to override it due to code complexity (each URP version has it's own unique motion vector pass)
            // https://docs.unity3d.com/Manual/SL-PackageRequirements.html
            PackageRequirements
            {
                "com.unity.render-pipelines.universal": "16.0.0"
            }
            
            Name "MotionVectors"
            Tags { "LightMode" = "MotionVectors" }
            ColorMask RG
            
            Cull [_Cull]

            HLSLPROGRAM
            //#pragma shader_feature_local _ALPHATEST_ON // already declared
            //#pragma multi_compile _ LOD_FADE_CROSSFADE // not supported by design
            #pragma shader_feature_local_vertex _ADD_PRECOMPUTED_VELOCITY

            //#include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"
            #include_with_pragmas "NiloToonCharacter_HLSL/NiloToonCharacter_ObjectMotionVectors.hlsl"
            ENDHLSL
        }

        Pass
        {
            // [PackageRequirements]
            // Force this motion vector pass only to run in URP16(Unity2023.2) or later
            // since URP versions before Unity2023.2 all have a default motion vector pass, 
            // although that default motion vector pass is not 100% correct with NiloToon(e.g., not matching to perspective removal/dither/dissolve)
            // we don't want to override it due to code complexity (each URP version has it's own unique motion vector pass)
            // https://docs.unity3d.com/Manual/SL-PackageRequirements.html
            PackageRequirements
            {
                "com.unity.render-pipelines.universal": "16.0.0"
            }
            
            Name "XRMotionVectors"
            Tags { "LightMode" = "XRMotionVectors" }
            ColorMask RGBA

            Cull [_Cull]
            
            // Stencil write for obj motion pixels
            Stencil
            {
                WriteMask 1
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            //#pragma shader_feature_local _ALPHATEST_ON // already declared
            //#pragma multi_compile _ LOD_FADE_CROSSFADE // not supported by design
            #pragma shader_feature_local_vertex _ADD_PRECOMPUTED_VELOCITY
            #define APLICATION_SPACE_WARP_MOTION 1
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"
            #include_with_pragmas "NiloToonCharacter_HLSL/NiloToonCharacter_ObjectMotionVectors.hlsl"
            ENDHLSL
        }

        // (X)No Meta Pass, this shader is designed for dynamic renderer only (not static/not lightmap)
        // {...}

        // (X)No Universal2D Pass, this shader is designed for 3D only
        // {...}

        // NiloToonSelfShadowCaster pass. Used for rendering NiloToon system's character self shadow map (not related to URP's shadow map system)
        Pass
        {
            Name "NiloToonSelfShadowCaster"
            Tags{"LightMode" = "NiloToonSelfShadowCaster"}

            // Explicit render state to avoid confusion
            ZWrite On // the only goal of this pass is to write depth!
            ZTest LEqual // early exit at Early-Z stage if possible (only possible if clip() does not exist)           
            ColorMask 0 // we don't care about color, we just want to write depth, ColorMask 0 will save some write bandwidth
            Cull [_CullNiloToonSelfShadowCaster]

            HLSLPROGRAM

            //#pragma exclude_renderers gles gles3 glcore // no need to exclude any renderers, all platforms will run the same Pass

            // no need to target 4.5, all platforms will run the same Pass(including WebGL = maximum 3.5) 
            // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
            //#pragma target 4.5

            // we need clip() related keywords in this pass, which is already defined inside the HLSLINCLUDE block in SubShader level
            // ...

            // no need to multi_compile for _NILOTOON_DITHER_FADEOUT, since we don't need it in this pass

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // GPU Instancing (you can always reference this section from URP's ComplexLit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // to support GPU instancing and Single Pass Stereo rendering(VR), you should add the following section
            // but here we disabled them because they are not worth the memory usage and build time increase, 
            // and the shader didn't have the concept of instancing anyway
            //------------------------------------------------------------------------------------------------------------------------------
            //#pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON
            //------------------------------------------------------------------------------------------------------------------------------

            #pragma vertex VertexShaderAllWork
            #pragma fragment BaseColorAlphaClipTest // we only need to do Clip(), no need color shading

            // because it is a NiloToonSelfShadowCaster pass, define "NiloToonCharSelfShadowCasterPass" to inject "remove shadow mapping artifact" code into VertexShaderAllWork().
            // We don't want to do outline extrude here because if we do it, the whole mesh will always receive self shadow, 
            #define NiloToonCharSelfShadowCasterPass 1

            // all shader logic written inside this .hlsl, remember to write all #define BEFORE writing #include
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"

            ENDHLSL
        }

        // [This pass is used when drawing to _NiloToonPrepassBufferTex texture]
        Pass
        {
            Name "NiloToonPrepassBuffer"
            Tags{"LightMode" = "NiloToonPrepassBuffer"}

            // Explicit render state to avoid confusion
            ZWrite On // one of the goal of this pass is to write depth!
            ZTest LEqual // early exit at Early-Z stage if possible (only possible if clip() does not exist)           
            //ColorMask 0 // we NEED to draw rgba data into color buffer, so don't write ColorMask 0!
            Cull [_Cull]

            HLSLPROGRAM

            //#pragma exclude_renderers gles gles3 glcore // no need to exclude any renderers, all platforms will run the same Pass

            // no need to target 4.5, all platforms will run the same Pass(including WebGL = maximum 3.5) 
            // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
            //#pragma target 4.5

            // we need clip() related keywords in this pass, which is already defined inside the HLSLINCLUDE block in SubShader level
            // ...

            #pragma shader_feature_local_vertex _OUTLINEWIDTHMAP

            // for push back zoffset depth write of face vertices, to hide face depth texture self shadow artifact
            #pragma shader_feature_local _ISFACE
            #pragma shader_feature_local _FACE_MASK_ON

            // DITHER_FADEOUT is needed for this pass, do not disable it
            #pragma multi_compile_local_fragment _ _NILOTOON_DITHER_FADEOUT

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // GPU Instancing (you can always reference this section from URP's Lit.shader)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // to support GPU instancing and Single Pass Stereo rendering(VR), add the following section
            // (disabled because not worth the memory usage and build time increase)
            //------------------------------------------------------------------------------------------------------------------------------
            // #pragma multi_compile_instancing 
            // #pragma multi_compile _ DOTS_INSTANCING_ON
            //------------------------------------------------------------------------------------------------------------------------------

            #pragma vertex VertexShaderAllWork
            #pragma fragment BaseColorAlphaClipTest_AndNiloToonPrepassBufferColorOutput // we need to do Clip(), and output normal as color

            #define NiloToonPrepassBufferPass 1

            // all shader logic written inside this .hlsl, remember to write all #define BEFORE writing #include
            #include "NiloToonCharacter_HLSL/NiloToonCharacter_Shared.hlsl"
            
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "LWGUI.LWGUI"
}
