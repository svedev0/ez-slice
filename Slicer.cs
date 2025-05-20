using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using EzSlice.Lib;
using Plane = EzSlice.Lib.Plane;

namespace EzSlice
{
	/// <summary>
	/// Contains methods for slicing GameObjects.
	/// </summary>
	public sealed class Slicer
	{
		/// <summary>
		/// Class for storing internal submesh values.
		/// </summary>
		private class SlicedSubmesh
		{
			public readonly List<Triangle> UpperHull = new();
			public readonly List<Triangle> LowerHull = new();

			// True if the submesh has had any UVs added.
			public bool HasUV()
			{
				if (UpperHull.Count > 0)
				{
					return UpperHull[0].HasUVs;
				}
				return LowerHull.Count > 0 && LowerHull[0].HasUVs;
			}

			// True if the submesh has had any normals added.
			public bool HasNormal()
			{
				if (UpperHull.Count > 0)
				{
					return UpperHull[0].HasNormals;
				}
				return LowerHull.Count > 0 && LowerHull[0].HasNormals;
			}

			// True if the submesh has had any tangents added.
			public bool HasTangent()
			{
				if (UpperHull.Count > 0)
				{
					return UpperHull[0].HasTangent;
				}
				return LowerHull.Count > 0 && LowerHull[0].HasTangent;
			}

			// True if proper slicing occured for this submesh (i.e. if there
			// are triangles in both the upper and lower hulls).
			public bool IsValid => UpperHull.Count > 0 && LowerHull.Count > 0;
		}

		/// <summary>
		/// Accept a GameObject which will transform the plane appropriately
		/// before the slice occurs.
		/// </summary>
		public static SlicedHull Slice(
			GameObject obj, Plane plane, TextureRegion crossRegion, Material crossMat)
		{
			if (!obj.TryGetComponent(out MeshFilter filter))
			{
				Debug.LogWarning("GameObject must have a MeshFilter");
				return null;
			}
			
			if (!obj.TryGetComponent(out MeshRenderer renderer))
			{
				Debug.LogWarning("GameObject must have a MeshRenderer");
				return null;
			}

			Material[] materials = renderer.sharedMaterials;
			Mesh mesh = filter.sharedMesh;
			if (!mesh)
			{
				Debug.LogWarning("GameObject must have a Mesh");
				return null;
			}

			int submeshCount = mesh.subMeshCount;
			if (materials.Length != submeshCount)
			{
				Debug.LogWarning("Materials array length must match submesh count");
				return null;
			}

			// Find index of cross-section material. Default to last array element.
			int crossIndex = materials.Length;

			// If the sliced material is null, append the cross-section to the
			// end of the submesh array, to allow setting or changing of the
			// material after slicing has occured.
			if (!crossMat)
			{
				return Slice(mesh, plane, crossRegion, crossIndex);
			}

			for (int i = 0; i < crossIndex; i++)
			{
				if (materials[i] != crossMat)
				{
					continue;
				}
				crossIndex = i;
				break;
			}

			return Slice(mesh, plane, crossRegion, crossIndex);
		}

		/// <summary>
		/// Slice the GameObject's mesh (if any) using the plane. This will
		/// generate a maximum of 2 other meshes. Recalculates new UV
		/// coordinates to ensure textures are applied properly. Returns null if
		/// no intersection has been found or the GameObject does not contain
		/// a valid mesh to cut.
		/// </summary>
		public static SlicedHull Slice(
			Mesh sharedMesh, Plane plane, TextureRegion region, int crossIndex)
		{
			if (!sharedMesh)
			{
				return null;
			}

			Vector3[] verts = sharedMesh.vertices;
			Vector2[] uvs = sharedMesh.uv;
			Vector3[] normals = sharedMesh.normals;
			Vector4[] tans = sharedMesh.tangents;

			int submeshCount = sharedMesh.subMeshCount;

			// Each submesh will be sliced and placed in its own array.
			SlicedSubmesh[] slices = new SlicedSubmesh[submeshCount];
			
			// The cross-section hull is common across all submeshes.
			List<Vector3> crossHull = new();

			// This object is reused for all intersection tests.
			IntersectionResult result = new();

			// Check whether to split the mesh using UVs, normals and tangents.
			bool genUVs = verts.Length == uvs.Length;
			bool genNormals = verts.Length == normals.Length;
			bool genTans = verts.Length == tans.Length;

			// Iterate over all submeshes. Vertices and indices are all shared
			// within the submesh.
			for (int submesh = 0; submesh < submeshCount; submesh++)
			{
				int[] indices = sharedMesh.GetTriangles(submesh);
				int indicesCount = indices.Length;

				SlicedSubmesh mesh = new();

				// Iterate over all mesh vertices, generating upper hulls,
				// lower hulls and all intersection points.
				for (int index = 0; index < indicesCount; index += 3)
				{
					int i0 = indices[index + 0];
					int i1 = indices[index + 1];
					int i2 = indices[index + 2];

					Triangle newTri = new(verts[i0], verts[i1], verts[i2]);

					// Generate UVs, normals, and tangents if available.
					if (genUVs)
					{
						newTri.SetUV(uvs[i0], uvs[i1], uvs[i2]);
					}
					if (genNormals)
					{
						newTri.SetNormal(normals[i0], normals[i1], normals[i2]);
					}
					if (genTans)
					{
						newTri.SetTangent(tans[i0], tans[i1], tans[i2]);
					}

					// Slice triangle using provided plane.
					if (newTri.Split(plane, result))
					{
						int upperHullCount = result.UpperHullCount;
						int lowerHullCount = result.LowerHullCount;
						int interHullCount = result.IntersectionPointsCount;

						for (int i = 0; i < upperHullCount; i++)
						{
							mesh.UpperHull.Add(result.UpperHull[i]);
						}
						for (int i = 0; i < lowerHullCount; i++)
						{
							mesh.LowerHull.Add(result.LowerHull[i]);
						}
						for (int i = 0; i < interHullCount; i++)
						{
							crossHull.Add(result.IntersectionPoints[i]);
						}
					}
					else
					{
						SideOfPlane sidaA = plane.SideOf(verts[i0]);
						SideOfPlane sideB = plane.SideOf(verts[i1]);
						SideOfPlane sideC = plane.SideOf(verts[i2]);

						SideOfPlane side = SideOfPlane.On;
						
						if (sidaA != SideOfPlane.On)
						{
							side = sidaA;
						}

						if (sideB != SideOfPlane.On)
						{
							Debug.Assert(side == SideOfPlane.On || side == sideB);
							side = sideB;
						}

						if (sideC != SideOfPlane.On)
						{
							Debug.Assert(side == SideOfPlane.On || side == sideC);
							side = sideC;
						}

						if (side == SideOfPlane.Up || side == SideOfPlane.On)
						{
							mesh.UpperHull.Add(newTri);
						}
						else
						{
							mesh.LowerHull.Add(newTri);
						}
					}
				}

				// Register into index.
				slices[submesh] = mesh;
			}

			// Check if slicing actually occured.
			foreach (SlicedSubmesh slicedSubmesh in slices)
			{
				// Check if at least one submesh was sliced. If so, break to go
				// through the generation step.
				if (slicedSubmesh is { IsValid: true })
				{
					return CreateFrom(
						slices,
						CreateFrom(crossHull, plane.Normal, region), crossIndex);
				}
			}

			// No slicing occured, return null.
			return null;
		}

		/// <summary>
		/// Generates a single SlicedHull from a set of cut submeshes.
		/// </summary>
		private static SlicedHull CreateFrom(
			SlicedSubmesh[] meshes, List<Triangle> cross, int crossIndex)
		{
			int submeshCount = meshes.Length;
			int upperHullCount = 0;
			int lowerHullCount = 0;

			// Get sum of upper, lower and intersection counts.
			for (int submesh = 0; submesh < submeshCount; submesh++)
			{
				upperHullCount += meshes[submesh].UpperHull.Count;
				lowerHullCount += meshes[submesh].LowerHull.Count;
			}

			Mesh upperHull = CreateUpperHull(meshes, upperHullCount, cross, crossIndex);
			Mesh lowerHull = CreateLowerHull(meshes, lowerHullCount, cross, crossIndex);
			return new SlicedHull(upperHull, lowerHull);
		}

		private static Mesh CreateUpperHull(
			SlicedSubmesh[] mesh, int total, List<Triangle> crossSection, int crossIndex)
		{
			return CreateHull(mesh, total, crossSection, crossIndex, true);
		}

		private static Mesh CreateLowerHull(
			SlicedSubmesh[] mesh, int total, List<Triangle> crossSection, int crossIndex)
		{
			return CreateHull(mesh, total, crossSection, crossIndex, false);
		}

		/// <summary>
		/// Generate a single mesh hull of either the upper or lower hulls.
		/// </summary>
		private static Mesh CreateHull(
			SlicedSubmesh[] meshes,
			int total,
			List<Triangle> crossSection,
			int crossIndex,
			bool isUpper)
		{
			if (total <= 0)
			{
				return null;
			}

			int submeshCount = meshes.Length;
			int crossCount = crossSection?.Count ?? 0;

			Mesh newMesh = new()
			{
				indexFormat = IndexFormat.UInt32
			};

			int arrayLen = (total + crossCount) * 3;

			bool hasUV = meshes[0].HasUV();
			bool hasNormal = meshes[0].HasNormal();
			bool hasTangent = meshes[0].HasTangent();

			// Vertices and UVs are common for all submeshes.
			Vector3[] newVertices = new Vector3[arrayLen];
			Vector2[] newUvs = hasUV ? new Vector2[arrayLen] : null;
			Vector3[] newNormals = hasNormal ? new Vector3[arrayLen] : null;
			Vector4[] newTangents = hasTangent ? new Vector4[arrayLen] : null;

			// Each index refers to submesh triangles.
			List<int[]> triangles = new(submeshCount);

			int vIndex = 0;

			// Generate all vertices, UVs and triangles.
			for (int submesh = 0; submesh < submeshCount; submesh++)
			{
				// Get correct hull.
				List<Triangle> hull = isUpper
					? meshes[submesh].UpperHull
					: meshes[submesh].LowerHull;
				
				int hullCount = hull.Count;
				int[] indices = new int[hullCount * 3];

				// Populate mesh arrays.
				for (int i = 0, triIndex = 0; i < hullCount; i++, triIndex += 3)
				{
					Triangle newTri = hull[i];

					int i0 = vIndex + 0;
					int i1 = vIndex + 1;
					int i2 = vIndex + 2;

					// Add vertices.
					newVertices[i0] = newTri.PositionA;
					newVertices[i1] = newTri.PositionB;
					newVertices[i2] = newTri.PositionC;

					// Add UV coordinates, normals, and tangents, if any.
					if (hasUV)
					{
						newUvs[i0] = newTri.UVA;
						newUvs[i1] = newTri.UVB;
						newUvs[i2] = newTri.UVC;
					}
					if (hasNormal)
					{
						newNormals[i0] = newTri.NormalA;
						newNormals[i1] = newTri.NormalB;
						newNormals[i2] = newTri.NormalC;
					}
					if (hasTangent)
					{
						newTangents[i0] = newTri.TangentA;
						newTangents[i1] = newTri.TangentB;
						newTangents[i2] = newTri.TangentC;
					}

					// Triangles are returned in clock-wise order from the
					// intersector. No need to sort them.
					indices[triIndex] = i0;
					indices[triIndex + 1] = i1;
					indices[triIndex + 2] = i2;

					vIndex += 3;
				}

				// Add triangles to index for later generation.
				triangles.Add(indices);
			}

			// Generate cross-section required for this hull.
			if (crossSection != null && crossCount > 0)
			{
				int[] crossIndices = new int[crossCount * 3];

				for (int i = 0, triIndex = 0; i < crossCount; i++, triIndex += 3)
				{
					Triangle newTri = crossSection[i];

					int i0 = vIndex + 0;
					int i1 = vIndex + 1;
					int i2 = vIndex + 2;

					// Add vertices.
					newVertices[i0] = newTri.PositionA;
					newVertices[i1] = newTri.PositionB;
					newVertices[i2] = newTri.PositionC;

					// Add UV coordinates, normals, and tangents, if any.
					if (hasUV)
					{
						newUvs[i0] = newTri.UVA;
						newUvs[i1] = newTri.UVB;
						newUvs[i2] = newTri.UVC;
					}
					if (hasNormal)
					{
						// Invert normals depending on upper/lower hull.
						if (isUpper)
						{
							newNormals[i0] = -newTri.NormalA;
							newNormals[i1] = -newTri.NormalB;
							newNormals[i2] = -newTri.NormalC;
						}
						else
						{
							newNormals[i0] = newTri.NormalA;
							newNormals[i1] = newTri.NormalB;
							newNormals[i2] = newTri.NormalC;
						}
					}
					if (hasTangent)
					{
						newTangents[i0] = newTri.TangentA;
						newTangents[i1] = newTri.TangentB;
						newTangents[i2] = newTri.TangentC;
					}

					// Add triangles in clockwise order for upper hulls and
					// counter clock-wise for lower hulls to ensure the mesh is
					// facing the right direction.
					if (isUpper)
					{
						crossIndices[triIndex] = i0;
						crossIndices[triIndex + 1] = i1;
						crossIndices[triIndex + 2] = i2;
					}
					else
					{
						crossIndices[triIndex] = i0;
						crossIndices[triIndex + 1] = i2;
						crossIndices[triIndex + 2] = i1;
					}

					vIndex += 3;
				}

				// Add triangles to index for later generation.
				if (triangles.Count <= crossIndex)
				{
					triangles.Add(crossIndices);
				}
				else
				{
					// Otherwise, merge triangles for the provided subsection.
					int[] prevTriangles = triangles[crossIndex];
					int[] merged = new int[prevTriangles.Length + crossIndices.Length];

					Array.Copy(prevTriangles, merged, prevTriangles.Length);
					Array.Copy(crossIndices, 0, merged, prevTriangles.Length, crossIndices.Length);

					// Replace previous array with new (merged) array.
					triangles[crossIndex] = merged;
				}
			}

			int totalTriangles = triangles.Count;
			newMesh.subMeshCount = totalTriangles;
			
			// Populate mesh structure.
			newMesh.vertices = newVertices;
			if (hasUV)
			{
				newMesh.uv = newUvs;
			}
			if (hasNormal)
			{
				newMesh.normals = newNormals;
			}
			if (hasTangent)
			{
				newMesh.tangents = newTangents;
			}

			// Add submeshes.
			for (int i = 0; i < totalTriangles; i++)
			{
				newMesh.SetTriangles(triangles[i], i, false);
			}

			return newMesh;
		}

		/// <summary>
		/// Generate a two-mesh (upper and lower) cross-section from a set of
		/// intersection points and a plane normal. The intersection points may
		/// be unordered.
		/// </summary>
		private static List<Triangle> CreateFrom(
			List<Vector3> intPoints, Vector3 planeNormal, TextureRegion region)
		{
			if (Triangulator.MonotoneChain(
				    intPoints, planeNormal, out List<Triangle> tris, region))
			{
				return tris;
			}
			return null;
		}
	}
}
