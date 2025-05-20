using UnityEngine;
using EzSlice.Lib;
using Plane = EzSlice.Lib.Plane;

namespace EzSlice
{
	/// <summary>
	/// Extension methods for easy access to slicer functionality.
	/// </summary>
	public static class SlicerExtensions
	{
		// Methods for further slicing a SlicedHull.
		public static SlicedHull Slice(
			this GameObject obj,
			Plane plane,
			Material crossSectionMat = null)
		{
			TextureRegion texRegion = new(0.0f, 0.0f, 1.0f, 1.0f);
			return Slice(obj, plane, texRegion, crossSectionMat);
		}

		public static SlicedHull Slice(
			this GameObject obj,
			Vector3 position,
			Vector3 direction,
			Material crossSectionMat = null)
		{
			TextureRegion texRegion = new(0.0f, 0.0f, 1.0f, 1.0f);
			return Slice(obj, position, direction, texRegion, crossSectionMat);
		}

		public static SlicedHull Slice(
			this GameObject obj,
			Vector3 position,
			Vector3 direction,
			TextureRegion textureRegion,
			Material crossSectionMaterial = null)
		{
			Plane cuttingPlane = new();

			Matrix4x4 mat = obj.transform.worldToLocalMatrix;
			Matrix4x4 transpose = mat.transpose;
			Matrix4x4 inv = transpose.inverse;

			Vector3 refUp = inv.MultiplyVector(direction).normalized;
			Vector3 refPt = obj.transform.InverseTransformPoint(position);

			cuttingPlane.Compute(refPt, refUp);
			
			return Slice(obj, cuttingPlane, textureRegion, crossSectionMaterial);
		}

		public static SlicedHull Slice(
			this GameObject obj,
			Plane plane,
			TextureRegion textureRegion,
			Material crossSectionMat = null)
		{
			return Slicer.Slice(obj, plane, textureRegion, crossSectionMat);
		}

		// Methods returning the final instantiated GameObjects.
		public static GameObject[] SliceInstantiate(this GameObject obj, Plane plane)
		{
			TextureRegion texRegion = new(0.0f, 0.0f, 1.0f, 1.0f);
			return SliceInstantiate(obj, plane, texRegion);
		}

		public static GameObject[] SliceInstantiate(
			this GameObject obj, Vector3 position, Vector3 direction)
		{
			return SliceInstantiate(obj, position, direction, null);
		}

		public static GameObject[] SliceInstantiate(
			this GameObject obj,
			Vector3 position,
			Vector3 direction,
			Material crossSectionMat)
		{
			TextureRegion texRegion = new(0.0f, 0.0f, 1.0f, 1.0f);
			return SliceInstantiate(obj, position, direction, texRegion, crossSectionMat);
		}

		public static GameObject[] SliceInstantiate(
			this GameObject obj,
			Vector3 position,
			Vector3 direction,
			TextureRegion cuttingRegion,
			Material crossSectionMat = null)
		{
			Plane cuttingPlane = new();

			Matrix4x4 mat = obj.transform.worldToLocalMatrix;
			Matrix4x4 transpose = mat.transpose;
			Matrix4x4 inv = transpose.inverse;

			Vector3 refUp = inv.MultiplyVector(direction).normalized;
			Vector3 refPt = obj.transform.InverseTransformPoint(position);

			cuttingPlane.Compute(refPt, refUp);

			return SliceInstantiate(obj, cuttingPlane, cuttingRegion, crossSectionMat);
		}

		public static GameObject[] SliceInstantiate(
			this GameObject obj,
			Plane plane,
			TextureRegion cuttingRegion,
			Material crossSectionMat = null)
		{
			SlicedHull slice = Slicer.Slice(obj, plane, cuttingRegion, crossSectionMat);
			if (slice == null)
			{
				return null;
			}

			GameObject upperHull = slice.CreateUpperHull(obj, crossSectionMat);
			GameObject lowerHull = slice.CreateLowerHull(obj, crossSectionMat);
			
			// If neither upper nor lower hulls are null, return both.
			if (upperHull && lowerHull)
			{
				return new[] { upperHull, lowerHull };
			}
			// Otherwise, if upper hull is not null, return only upper hull
			if (upperHull)
			{
				return new[] { upperHull };
			}
			// Otherwise, if lower hull is not null, return only lower hull
			if (lowerHull != null)
			{
				return new[] { lowerHull };
			}
			
			return null;
		}
	}
}
