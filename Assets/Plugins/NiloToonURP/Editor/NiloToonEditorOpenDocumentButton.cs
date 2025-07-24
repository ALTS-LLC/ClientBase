using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NiloToon.NiloToonURP
{
    public class NiloToonEditorOpenDocumentButton
    {
        [MenuItem("Window/NiloToonURP/Open online document (latest version)", priority = 100)]
        public static void OpenNiloToonDocumentWebsite()
        {
            Application.OpenURL("https://docs.google.com/document/d/1iEh1E5xLXnXuICM0ElV3F3x_J2LND9Du2SSKzcIuPXw");
        }
        
        private static string pdfFileName = "NiloToonURP user document.pdf"; // unique file name

        [MenuItem("Window/NiloToonURP/Open offline document (current version)", priority = 100)]
        private static void OpenUniquePDF()
        {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string pdfPath = FindFile(projectPath, pdfFileName);

            if (!string.IsNullOrEmpty(pdfPath))
            {
                Application.OpenURL("file:///" + pdfPath);
            }
            else
            {
                EditorUtility.DisplayDialog("File Not Found", "The PDF could not be found.", "OK");
            }
        }
        
        [MenuItem("Window/NiloToonURP/Contact support", priority = 100)]
        public static void OpenNiloToonDocumentWebsit_SupportPage()
        {
            Application.OpenURL("https://docs.google.com/document/d/1iEh1E5xLXnXuICM0ElV3F3x_J2LND9Du2SSKzcIuPXw/edit#heading=h.d2xv4a6hrkb8");
        }
        
        [MenuItem("Window/NiloToonURP/Change log", priority = 100)]
        public static void OpenNiloToonDocumentWebsit_ChangeLogPage()
        {
            Application.OpenURL("https://docs.google.com/document/d/1iEh1E5xLXnXuICM0ElV3F3x_J2LND9Du2SSKzcIuPXw/edit#heading=h.vf7wy743tx4u");
        }
        
        private static string FindFile(string startDirectory, string fileName)
        {
            foreach (string file in Directory.GetFiles(startDirectory, fileName, SearchOption.AllDirectories))
            {
                return file; // Return the first match
            }
            return null; // If not found, return null
        }
    }
}