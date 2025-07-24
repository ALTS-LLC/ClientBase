using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NiloToon.NiloToonURP
{
    public class NiloToonEditorUpdateButton
    {
        [MenuItem("Window/NiloToonURP/Update to latest version", priority = 100)]
        public static void OpenNiloToonDownloadWebsite()
        {
            Application.OpenURL("https://drive.google.com/drive/u/2/folders/1dcP5gxSI1sdESHiTyxiToYLK1mFVdZLD");
        }
    }
}