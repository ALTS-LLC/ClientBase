using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ClientBaseUtility
{
    public static class PathUtility
    {
        public readonly static string ConfigSBPath = "Assets/ManagerAsset/Data/Config/Config.asset";
        public readonly static string ConfigsSBDirectory = "Assets/ManagerAsset/Data/Config";

        public static class ExcludeExtensions
        {
            public readonly static string AssetFile = "*.asset";
        }
    }
}