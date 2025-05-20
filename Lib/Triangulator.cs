using System;
using System.Collections.Generic;
using UnityEngine;

namespace EzSlice.Lib
{
	/// <summary>
	/// Contains static functionality for performing triangulation on arbitrary vertices.
	/// </summary>
	public sealed class Triangulator
	{
		/// <summary>
		/// Represents a 3D vertex which has been mapped onto a 2D surface.
		/// Mainly used in MonotoneChain to triangulate a set of vertices
		/// against a plane.
		/// </summary>
		private readonly struct Mapped2D
		{
			private readonly Vector3 _original;
			private readonly Vector2 _mapped;

			public Mapped2D(Vector3 original, Vector3 u, Vector3 v)
			{
				_original = original;
				_mapped = new Vector2(
					Vector3.Dot(original, u),
					Vector3.Dot(original, v));
			}

			public Vector2 MappedValue => _mapped;
			public Vector3 OriginalValue => _original;
		}

		/// <summary>
		/// Calculate UV coordinates of triangles between 0.0 and 1.0 (default).
		/// </summary>
		public static bool MonotoneChain(
			List<Vector3> vertices, Vector3 normal, out List<Triangle> tri)
		{
			TextureRegion texRegion = new(0.0f, 0.0f, 1.0f, 1.0f);
			return MonotoneChain(vertices, normal, out tri, texRegion);
		}

		/// <summary>
		/// O(n * log(n)) convex hull algorithm. Accepts a list of vertices as
		/// Vector3 and triangulates them according to a projection plane
		/// defined as planeNormal. Algorithm will output vertices, indices and
		/// UV coordinates as arrays.
		/// https://www.geeksforgeeks.org/convex-hull-monotone-chain-algorithm/
		/// </summary>
		public static bool MonotoneChain(
			List<Vector3> vertices,
			Vector3 normal,
			out List<Triangle> triangles,
			TextureRegion texRegion)
		{
			int count = vertices.Count;

			// Triangulation is not possible with less than 3 points.
			if (count < 3)
			{
				triangles = null;
				return false;
			}

			// Map 3D points into a 2D plane represented by the provided normal.
			Vector3 u = Vector3.Normalize(Vector3.Cross(normal, Vector3.up));
			if (Vector3.zero == u)
			{
				u = Vector3.Normalize(Vector3.Cross(normal, Vector3.forward));
			}
			Vector3 v = Vector3.Cross(u, normal);

			// Generate an array of mapped values.
			Mapped2D[] mapped = new Mapped2D[count];

			// These values will be used to generate new UV coordinates later.
			float maxDivX = float.MinValue;
			float maxDivY = float.MinValue;
			float minDivX = float.MaxValue;
			float minDivY = float.MaxValue;

			// Map 3D vertices into the 2D mapped values.
			for (int i = 0; i < count; i++)
			{
				Vector3 vertToAdd = vertices[i];
				
				Mapped2D newMappedValue = new(vertToAdd, u, v);
				Vector2 mapVal = newMappedValue.MappedValue;

				// Get maximal values to map UVs in a proper range.
				maxDivX = Mathf.Max(maxDivX, mapVal.x);
				maxDivY = Mathf.Max(maxDivY, mapVal.y);
				minDivX = Mathf.Min(minDivX, mapVal.x);
				minDivY = Mathf.Min(minDivY, mapVal.y);

				mapped[i] = newMappedValue;
			}

			// Sort generated array.
			Array.Sort(mapped, (a, b) =>
			{
				Vector2 x = a.MappedValue;
				Vector2 p = b.MappedValue;
				return x.x < p.x || (Mathf.Approximately(x.x, p.x) && x.y < p.y) ? -1 : 1;
			});

			// Final hull mappings will be stored in here.
			Mapped2D[] hulls = new Mapped2D[count + 1];

			int k = 0;

			// Build lower hull of the chain.
			for (int i = 0; i < count; i++)
			{
				while (k >= 2)
				{
					Vector2 mA = hulls[k - 2].MappedValue;
					Vector2 mB = hulls[k - 1].MappedValue;
					Vector2 mC = mapped[i].MappedValue;
					if (Intersector.TriArea2D(mA.x, mA.y, mB.x, mB.y, mC.x, mC.y) > 0.0f)
					{
						break;
					}

					k--;
				}

				hulls[k++] = mapped[i];
			}

			// Build upper hull of the chain.
			for (int i = count - 2, t = k + 1; i >= 0; i--)
			{
				while (k >= t)
				{
					Vector2 mA = hulls[k - 2].MappedValue;
					Vector2 mB = hulls[k - 1].MappedValue;
					Vector2 mC = mapped[i].MappedValue;
					if (Intersector.TriArea2D(mA.x, mA.y, mB.x, mB.y, mC.x, mC.y) > 0.0f)
					{
						break;
					}

					k--;
				}

				hulls[k++] = mapped[i];
			}

			// Build mesh, generate and populate values of variables.
			int vertsCount = k - 1;
			int trisCount = (vertsCount - 2) * 3;

			// This should never happen, but just in case it does.
			if (vertsCount < 3)
			{
				triangles = null;
				return false;
			}

			// Ensure size of list does not dynamically grow to prevent
			// performing copy operations each time.
			triangles = new List<Triangle>(trisCount / 3);

			float width = maxDivX - minDivX;
			float height = maxDivY - minDivY;

			int indexCount = 1;

			// Generate both vertices and UVs.
			for (int i = 0; i < trisCount; i += 3)
			{
				// Vertices in triangle.
				Mapped2D posA = hulls[0];
				Mapped2D posB = hulls[indexCount];
				Mapped2D posC = hulls[indexCount + 1];

				// UV maps.
				Vector2 uvA = posA.MappedValue;
				Vector2 uvB = posB.MappedValue;
				Vector2 uvC = posC.MappedValue;

				uvA.x = (uvA.x - minDivX) / width;
				uvA.y = (uvA.y - minDivY) / height;

				uvB.x = (uvB.x - minDivX) / width;
				uvB.y = (uvB.y - minDivY) / height;

				uvC.x = (uvC.x - minDivX) / width;
				uvC.y = (uvC.y - minDivY) / height;

				Triangle newTriangle = new(
					posA.OriginalValue, posB.OriginalValue, posC.OriginalValue);

				// Ensure UV coordinates are mapped into the TextureRegion.
				newTriangle.SetUV(
					texRegion.Map(uvA), texRegion.Map(uvB), texRegion.Map(uvC));

				// Normals are the same for all vertices since the final mesh
				// is completely flat.
				newTriangle.SetNormal(normal, normal, normal);
				newTriangle.ComputeTangents();

				triangles.Add(newTriangle);
				indexCount++;
			}

			return true;
		}
	}
}
