using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace NiloToonUPMImporterCollections
{
    [InitializeOnLoad]
    public static class NiloToonUPMImporterCollections
    {
        static NiloToonUPMImporterCollections()
        {
            // URP installed them already
            //Install("com.unity.burst");
            //Install("com.unity.mathematics");

            // we only need this
            Install("com.unity.collections");

            // this will mark the project as Experimental, don't include it
            //Install("com.unity.jobs");
        }

        public static bool Install(string id)
        {
            Debug.Log($"[NiloToon]Package Manager Auto Install->{id}");
            var request = Client.Add(id);
            while (!request.IsCompleted) { };
            if (request.Error != null) Debug.LogError(request.Error.message);
            return request.Error == null;
        }
    }
}
