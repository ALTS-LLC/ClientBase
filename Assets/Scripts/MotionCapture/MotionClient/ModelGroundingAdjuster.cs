using UnityEngine;

public class ModelGroundingAdjuster : MonoBehaviour
{
	private float _yOffsetToGround = 0;
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.A))
		{
			float lowestVertexYWorld = GetLowestVertexYCoordinate(gameObject);
			 _yOffsetToGround = -lowestVertexYWorld;		
		}

		gameObject.transform.position = new Vector3()
		{
			x = gameObject.transform.position.x,
			y = gameObject.transform.position.y + _yOffsetToGround,
			z = gameObject.transform.position.z
		};
	}

	public float GetLowestVertexYCoordinate(GameObject targetGameObject)
	{
		float currentMinY = float.MaxValue;

		MeshFilter meshFilter = targetGameObject.GetComponent<MeshFilter>();
		if (meshFilter != null && meshFilter.sharedMesh != null)
		{
			Vector3[] meshVertices = meshFilter.sharedMesh.vertices;
			Matrix4x4 localToWorldMatrix = targetGameObject.transform.localToWorldMatrix;

			foreach (Vector3 localVertex in meshVertices)
			{
				Vector3 worldVertex = localToWorldMatrix.MultiplyPoint3x4(localVertex);
				if (worldVertex.y < currentMinY)
				{
					currentMinY = worldVertex.y;
				}
			}
		}

		SkinnedMeshRenderer skinnedMeshRenderer = targetGameObject.GetComponent<SkinnedMeshRenderer>();
		if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
		{
			Mesh bakedMesh = new Mesh();
			skinnedMeshRenderer.BakeMesh(bakedMesh);

			Vector3[] skinnedMeshVertices = bakedMesh.vertices;
			Matrix4x4 localToWorldMatrix = targetGameObject.transform.localToWorldMatrix;

			for (int i = 0; i < skinnedMeshVertices.Length; i++)
			{
				Vector3 worldVertex = localToWorldMatrix.MultiplyPoint3x4(skinnedMeshVertices[i]);
				if (worldVertex.y < currentMinY)
				{
					currentMinY = worldVertex.y;
				}
			}
			Destroy(bakedMesh);
		}
		else
		{
			foreach (Transform childTransform in targetGameObject.transform)
			{
				float childMinY = GetLowestVertexYCoordinate(childTransform.gameObject);
				if (childMinY < currentMinY)
				{
					currentMinY = childMinY;
				}
			}
		}

		return currentMinY;
	}
}
