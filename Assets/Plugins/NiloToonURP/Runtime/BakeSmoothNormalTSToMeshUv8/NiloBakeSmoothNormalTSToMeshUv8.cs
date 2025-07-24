using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NiloBakeSmoothNormalTSToMeshUv8
{
    // you can turn on when debug is needed
    public static bool LOG_ERROR = false;
        
    public static void GenGOSmoothedNormalToUV8(GameObject inputGO)
    {
        void GenMeshSmoothedNormalToUV8(Mesh mesh)
        {
            // only generate if uv#8 is missing and unused
            // to prevent overwriting user's data / NiloToon editor generated uv8 data
            if (mesh != null && mesh.uv8.Length == 0)
            {
                GenSmoothedNormalsToUV8(mesh);
            }
        }
        
        foreach (var item in inputGO.GetComponentsInChildren<MeshFilter>())
        {
            GenMeshSmoothedNormalToUV8(item.sharedMesh);
        }
        foreach (var item in inputGO.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            GenMeshSmoothedNormalToUV8(item.sharedMesh);
        }
    }
    
    struct NormalWeight 
    {
        public Vector3 normal;
        public float weight;
    }
    
    public static bool IsUnitVector(Vector3 vector)
    {
        return Mathf.Abs(vector.magnitude - 1f) <= 1e-6f;
    }
    
    // method source: https://www.bilibili.com/read/cv27148724/
    // 2024-03-18: tested on 18 VSPO! characters, although the result is different to Unity's fbx smooth normal, but no quality regression in outline quality
    public static void GenSmoothedNormalsToUV8(Mesh mesh)
    {
        Dictionary<Vector3, List<NormalWeight>> normalDict = new Dictionary<Vector3, List<NormalWeight>>();
        var triangles = mesh.triangles;
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var tangents = mesh.tangents;
        var smoothNormals = mesh.normals;

        if (tangents.Length == 0)
        {
            Debug.LogError($"[NiloToon]No tangent from Mesh({mesh}), abort generate smooth normal");
            return;
        }

        // for each 3 vertices of each triangle, calculate each vertex's:
        // - cross product hard normal
        // - the weight based on angle between 2 edges of a triangle connecting that vertex
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int[] triangle = {triangles[i], triangles[i+1], triangles[i+2]};
            for (int j = 0; j < 3; j++)
            {
                int vertexIndex = triangle[j];
                Vector3 vertex = vertices[vertexIndex];
                if (!normalDict.ContainsKey(vertex))
                {
                    normalDict.Add(vertex, new List<NormalWeight>());
                }

                NormalWeight nw;
                Vector3 lineA = Vector3.zero;
                Vector3 lineB = Vector3.zero;
                if (j == 0)
                {
                    lineA = vertices[triangle[1]] - vertex;
                    lineB = vertices[triangle[2]] - vertex;
                }
                else if (j == 1)
                {
                    lineA = vertices[triangle[2]] - vertex;
                    lineB = vertices[triangle[0]] - vertex;
                }
                else
                {
                    lineA = vertices[triangle[0]] - vertex;
                    lineB = vertices[triangle[1]] - vertex;
                }

                // line *1000f to help line.normalized getting better result,
                // due to small triangle(very short line's .normalized may produce wrong result.
                // * see Unity's implementation of Vector3.Normalize in IL code below
                //public static Vector3 Normalize(Vector3 value)
                //{
                //    float num = Vector3.Magnitude(value);
                //    return (double) num > 9.999999747378752E-06 ? value / num : Vector3.zero;
                //}
                lineA *= 1000f;
                lineB *= 1000f;
                lineA = lineA.normalized;
                lineB = lineB.normalized;
                
                // validate the result of line.normalized
                if (!IsUnitVector(lineA))
                {
                    if(LOG_ERROR) Debug.LogError($"Mesh ({mesh})'s triangle edge line not normalized correctly!", mesh);
                }
                if (!IsUnitVector(lineB))
                {
                    if(LOG_ERROR) Debug.LogError($"Mesh ({mesh})'s triangle edge line not normalized correctly!", mesh);
                }
                
                // Acos returns "smaller angle between 2 vectors, in radians"
                // the larger the angle, the larger the weight in the later "average sum" step, so the correct smoothed normal can be calculated at the end
                float angle = Mathf.Acos(Mathf.Max(Mathf.Min(Vector3.Dot(lineA, lineB), 1), -1));
                nw.normal = Vector3.Cross(lineA, lineB).normalized;
                nw.weight = angle;
                
                // validate the result of triangleNormalVector.normalized
                if (!IsUnitVector(nw.normal))
                {
                    nw.normal = normals[vertexIndex]; // temp fallback
                    nw.weight = 0;
                    if(LOG_ERROR) Debug.LogError($"Mesh ({mesh})'s calculated polygon normal not normalized correctly! {lineA},{lineB}", mesh);
                }
                
                normalDict[vertex].Add(nw);
            }
        }

        // for each real vertex, get all "triangle hard normal" and "weight" from that vertex position,
        // then calculate the final smoothed normal by a weighted sum
        for (int i = 0; i < vertices.Length; i++) 
        {
            Vector3 vertex = vertices[i];
            
            // validate
            if (!normalDict.ContainsKey(vertex)) 
            {
                if(LOG_ERROR) Debug.LogError("Should not happen!");
                continue;
            }
            List<NormalWeight> normalList = normalDict[vertex];
            if (normalList.Count == 0)
            {
                if(LOG_ERROR) Debug.LogError("Should not happen!");
            }

            Vector3 smoothNormal = Vector3.zero;
            float weightSum = 0f;

            for (int j = 0; j < normalList.Count; j++)
            {
                NormalWeight nw = normalList[j];
                weightSum += nw.weight;
                smoothNormal += nw.normal * nw.weight;
            }

            if (weightSum != 0f) // To avoid division by zero
            {
                smoothNormal /= weightSum;
            }

            smoothNormal = smoothNormal.normalized;
            smoothNormals[i] = smoothNormal;

            var normal = normals[i];
            var tangent = tangents[i];
            var binormal = (Vector3.Cross(normal, tangent) * tangent.w).normalized;
            var tbn = new Matrix4x4(tangent, binormal, normal, Vector3.zero);
            tbn = tbn.transpose;
            smoothNormals[i] = tbn.MultiplyVector(smoothNormals[i]).normalized;
        }
        mesh.SetUVs(7, smoothNormals);
    }
}
