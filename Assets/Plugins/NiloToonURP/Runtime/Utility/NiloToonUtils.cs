using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NiloToon.NiloToonURP
{
    /// <summary>
    /// Contains helper functions that you can use.
    /// </summary>
    public static class NiloToonUtils
    {
        public static bool NameHasKeyword(string name, string keyword)
        {
            return name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        
        public static Transform DepthSearch(Transform parent, string targetName, string[] banNameList)
        {
            foreach (Transform child in parent)
            {
                if (NiloToonUtils.NameHasKeyword(child.name, targetName))
                {
                    bool isBanned = false;

                    if (banNameList != null)
                    {
                        foreach (string banName in banNameList)
                        {
                            if (NiloToonUtils.NameHasKeyword(child.name, banName))
                            {
                                isBanned = true;
                                break;
                            }
                        }
                    }

                    if (!isBanned)
                    {
                        return child;
                    }
                }

                var result = DepthSearch(child, targetName, banNameList);
                if (result != null)
                {
                    return result;
                }
            }

            // find nothing
            return null;
        }
    }
}