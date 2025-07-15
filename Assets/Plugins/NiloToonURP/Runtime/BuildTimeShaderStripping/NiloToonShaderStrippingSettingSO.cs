using UnityEngine;

namespace NiloToon.NiloToonURP
{
    [CreateAssetMenu(fileName = "NiloToonShaderStrippingSettingSO", menuName = "NiloToon/NiloToonShaderStrippingSettingSO", order = 0)]
    public class NiloToonShaderStrippingSettingSO : ScriptableObject
    {
        // if user didn't assign stripping setting in NiloToonAllInOneRendererFeature, will use C# hardcode per platform settings here
        public Settings DefaultSettings = new Settings();

        // iOS/Android/WebGL platform will easily out of memory crash, so we strip all shader keywords by default, except _NILOTOON_RECEIVE_SELF_SHADOW
        [Header("Android Override")]
        public bool ShouldOverrideSettingForAndroid = true;
        public Settings AndroidSettings = new Settings(
            include_NILOTOON_DEBUG_SHADING: false,
            include_NILOTOON_FORCE_MINIMUM_SHADER: false,
            include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE: false,
            include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2: false,
            include_NILOTOON_RECEIVE_URP_SHADOWMAPPING: false,
            include_NILOTOON_RECEIVE_SELF_SHADOW: true, // enabled
            include_NILOTOON_DITHER_FADEOUT: false,
            include_NILOTOON_DISSOLVE: false,
            include_NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE: false);

        [Header("iOS Override")]
        public bool ShouldOverrideSettingForIOS = true;
        public Settings iOSSettings = new Settings(
            include_NILOTOON_DEBUG_SHADING: false,
            include_NILOTOON_FORCE_MINIMUM_SHADER: false,
            include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE: false,
            include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2: false,
            include_NILOTOON_RECEIVE_URP_SHADOWMAPPING: false,
            include_NILOTOON_RECEIVE_SELF_SHADOW: true, // enabled
            include_NILOTOON_DITHER_FADEOUT: false,
            include_NILOTOON_DISSOLVE: false,
            include_NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE: false);
        
        [Header("WebGL Override")]
        public bool ShouldOverrideSettingForWebGL = true;
        public Settings WebGLSettings = new Settings(
            include_NILOTOON_DEBUG_SHADING: false,
            include_NILOTOON_FORCE_MINIMUM_SHADER: false,
            include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE: false,
            include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2: false,
            include_NILOTOON_RECEIVE_URP_SHADOWMAPPING: false,
            include_NILOTOON_RECEIVE_SELF_SHADOW: true, // enabled
            include_NILOTOON_DITHER_FADEOUT: false,
            include_NILOTOON_DISSOLVE: false,
            include_NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE: false);

        [System.Serializable]
        public class Settings
        {
            [HelpBox(
                "If a feature is missing in build, enable it to include it in build.\n" +
                    "* Disabling unneeded features can reduce build time, build size and runtime memory usage drastically. (2^n)")] 
            
            //---------------------------------------------------------------------------------------
            [Header("Character Shadow Mapping")]
            
            [OverrideDisplayName("NiloToon character's self shadow map")]
            [Tooltip("Default enable for all platforms, since NiloToon's self shadow map is a core visual feature of NiloToon." +
                     "\n\n" +
                     "Keyword: _NILOTOON_RECEIVE_SELF_SHADOW\n\n" +
                     "Default value:\n" +
                     "- All platforms: true")]
            public bool include_NILOTOON_RECEIVE_SELF_SHADOW = true;
            
            [OverrideDisplayName("URP's main light shadow map")]
            [Tooltip("Default disable for all platforms, since NiloToon's self shadow map is usually better than URP's main light shadow map in terms of quality" +
                     "\n\n" +
                     "Keyword: _NILOTOON_RECEIVE_URP_SHADOWMAPPING\n\n" +
                     "Default value:\n" +
                     "- All platforms: false")]
            public bool include_NILOTOON_RECEIVE_URP_SHADOWMAPPING = false;
            
            //---------------------------------------------------------------------------------------
            [Header("NiloToonPerCharacterRenderController features")]
            
            [OverrideDisplayName("Dither Fadeout")]
            [Tooltip("It is enabled by default because Warudo (PC Standalone) relies on it." +
                     "\n\n" +
                     "keyword: _NILOTOON_DITHER_FADEOUT\n\n" +
                     "Default value:\n" +
                     "- Default: true\n" +
                     "- Adnroid/iOS/WebGL: false")]
            public bool include_NILOTOON_DITHER_FADEOUT = true;
            
            [OverrideDisplayName("Dissolve")]
            [Tooltip("It is enabled by default because Warudo (PC Standalone) relies on it." +
                     "\n\n" +
                     "keyword: _NILOTOON_DISSOLVE\n\n" +
                     "Default value:\n" +
                     "- Default: true\n" +
                     "- Adnroid/iOS/WebGL: false")]
            public bool include_NILOTOON_DISSOLVE = true;
            
            [OverrideDisplayName("BaseMap Override")]
            [Tooltip("keyword: _NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE\n\n" +
                     "Default value:\n" +
                     "- All platforms: false")]
            public bool include_NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE = false;
            
            //---------------------------------------------------------------------------------------
            [Header("Screen Space Outline")]
            
            [OverrideDisplayName("Screen Space Outline V1")]
            [Tooltip("keyword: _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE\n\n" +
                     "Default value:\n" +
                     "- All platforms: false")]
            public bool include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE = false;
            
            [OverrideDisplayName("Screen Space Outline V2")]
            [Tooltip("keyword: _NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2\n\n" +
                     "Default value:\n" +
                     "- All platforms: false")]
            public bool include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2 = false;
            
            //---------------------------------------------------------------------------------------
            [Header("Character Debug")]
            
            [OverrideDisplayName("Debug Shading")]
            [Tooltip("keyword: _NILOTOON_DEBUG_SHADING\n\n" +
                     "Default value:\n" +
                     "- All platforms: false")]
            public bool include_NILOTOON_DEBUG_SHADING = false;
            
            [OverrideDisplayName("Force Minimum Shader")]
            [Tooltip("keyword: _NILOTOON_FORCE_MINIMUM_SHADER\n\n" +
                     "Default value:\n" +
                     "- All platforms: false")]
            public bool include_NILOTOON_FORCE_MINIMUM_SHADER = false;  

            public Settings() { }
            public Settings(
                bool include_NILOTOON_DEBUG_SHADING,
                bool include_NILOTOON_FORCE_MINIMUM_SHADER,
                bool include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE,
                bool include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2,
                bool include_NILOTOON_RECEIVE_URP_SHADOWMAPPING,
                bool include_NILOTOON_RECEIVE_SELF_SHADOW,
                bool include_NILOTOON_DITHER_FADEOUT,
                bool include_NILOTOON_DISSOLVE,
                bool include_NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE
                )
            {
                this.include_NILOTOON_DEBUG_SHADING = include_NILOTOON_DEBUG_SHADING;
                this.include_NILOTOON_FORCE_MINIMUM_SHADER = include_NILOTOON_FORCE_MINIMUM_SHADER;
                this.include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE = include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE;
                this.include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2 = include_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2;
                this.include_NILOTOON_RECEIVE_URP_SHADOWMAPPING = include_NILOTOON_RECEIVE_URP_SHADOWMAPPING;
                this.include_NILOTOON_RECEIVE_SELF_SHADOW = include_NILOTOON_RECEIVE_SELF_SHADOW;
                this.include_NILOTOON_DITHER_FADEOUT = include_NILOTOON_DITHER_FADEOUT;
                this.include_NILOTOON_DISSOLVE = include_NILOTOON_DISSOLVE;
                this.include_NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE = include_NILOTOON_PERCHARACTER_BASEMAP_OVERRIDE;
            }
        }
    }
}