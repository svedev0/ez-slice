using System;
using UnityEngine;

namespace EzSlice
{
	/// <summary>
	/// The final generated data structure from a slice operation. Provides
	/// easy access to utility functions and the final Mesh data for each
	/// section of the hull.
	/// </summary>
	public sealed class SlicedHull
	{
		private Mesh _upperHull;
		private Mesh _lowerHull;

		public SlicedHull(Mesh upperHull, Mesh lowerHull)
		{
			_upperHull = upperHull;
			_lowerHull = lowerHull;
		}
		
		public Mesh UpperHull => _upperHull;
		public Mesh LowerHull => _lowerHull;

		/// <summary>
		/// Creates the upper half of the hull.
		/// </summary>
		public GameObject CreateUpperHull(GameObject original, Material crossSectionMat = null)
		{
			// Generate a new GameObject from the upper hull of the mesh.
			GameObject newObject = CreateEmptyObject("Upper_Hull", _upperHull);
			if (!newObject)
			{
				return newObject;
			}

			newObject.transform.parent = original.transform.parent;
			newObject.transform.localPosition = original.transform.localPosition;
			newObject.transform.localRotation = original.transform.localRotation;
			newObject.transform.localScale = original.transform.localScale;

			Material[] shared = original.GetComponent<MeshRenderer>().sharedMaterials;
			Mesh mesh = original.GetComponent<MeshFilter>().sharedMesh;

			// Nothing changed in the hierarchy, the cross-section must have
			// been batched with the submeshes, return as is, no need for any
			// changes.
			if (mesh.subMeshCount == _upperHull.subMeshCount)
			{
				newObject.GetComponent<Renderer>().sharedMaterials = shared;
				return newObject;
			}

			// Otherwise, the cross-section was added to the back of the
			// submesh array because it uses a different material. We need to
			// take this into account.
			Material[] newShared = new Material[shared.Length + 1];

			// Copy over material arrays using native copy (faster than a loop).
			Array.Copy(shared, newShared, shared.Length);
			newShared[shared.Length] = crossSectionMat;
			
			newObject.GetComponent<Renderer>().sharedMaterials = newShared;
			return newObject;
		}

		/// <summary>
		/// Creates the lower half of the hull.
		/// </summary>
		public GameObject CreateLowerHull(GameObject original, Material crossSectionMat = null)
		{
			// Generate a new GameObject from the lower hull of the mesh.
			GameObject newObject = CreateEmptyObject("Lower_Hull", _lowerHull);
			if (!newObject)
			{
				return newObject;
			}

			newObject.transform.parent = original.transform.parent;
			newObject.transform.localPosition = original.transform.localPosition;
			newObject.transform.localRotation = original.transform.localRotation;
			newObject.transform.localScale = original.transform.localScale;

			Material[] shared = original.GetComponent<MeshRenderer>().sharedMaterials;
			Mesh mesh = original.GetComponent<MeshFilter>().sharedMesh;

			// Nothing changed in the hierarchy, the cross-section must have
			// been batched with the submeshes, return as is, no need for any
			// changes.
			if (mesh.subMeshCount == _lowerHull.subMeshCount)
			{
				newObject.GetComponent<Renderer>().sharedMaterials = shared;
				return newObject;
			}

			// Otherwise, the cross-section was added to the back of the
			// submesh array because it uses a different material. We need to
			// take this into account.
			Material[] newShared = new Material[shared.Length + 1];

			// Copy over material arrays using native copy (faster than a loop).
			Array.Copy(shared, newShared, shared.Length);
			newShared[shared.Length] = crossSectionMat;
			
			newObject.GetComponent<Renderer>().sharedMaterials = newShared;
			return newObject;
		}

		/// <summary>
		/// Helper function which creates a new GameObject to be able to add a
		/// new mesh for rendering.
		/// </summary>
		private static GameObject CreateEmptyObject(string name, Mesh hull)
		{
			if (!hull)
			{
				return null;
			}

			GameObject newObject = new(name);
			newObject.AddComponent<MeshRenderer>();
			
			MeshFilter filter = newObject.AddComponent<MeshFilter>();
			filter.mesh = hull;
			
			return newObject;
		}
	}
}
