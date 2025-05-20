using UnityEngine;

namespace EzSlice.Lib
{
	/// <summary>
	/// Contains static functionality to perform geometric intersection tests.
	/// </summary>
	public sealed class Intersector
	{
		/// <summary>
		/// Perform an intersection between plane and line, storing the
		/// intersection point in reference q. Returns true if intersection was
		/// found.
		/// </summary>
		public static bool Intersect(Plane plane, Line line, out Vector3 q)
		{
			return Intersect(plane, line.PositionA, line.PositionB, out q);
		}

		public const float Epsilon = 0.0001f;

		/// <summary>
		/// Perform an intersection between plane and line between points a and
		/// b, storing the intersection point in reference q. Returns true if
		/// intersection was found.
		/// </summary>
		public static bool Intersect(Plane plane, Vector3 a, Vector3 b, out Vector3 q)
		{
			Vector3 normal = plane.Normal;
			Vector3 ab = b - a;
			
			float t = (plane.Dist - Vector3.Dot(normal, a)) / Vector3.Dot(normal, ab);
			
			// Compensation for floating point errors.
			if (t is >= -Epsilon and <= 1 + Epsilon)
			{
				q = a + t * ab;
				return true;
			}

			q = Vector3.zero;
			return false;
		}

		/// <summary>
		/// Computes twice the signed area of a 2D triangle.
		/// </summary>
		public static float TriArea2D(float x1, float y1, float x2, float y2, float x3, float y3)
		{
			return (x1 - x2) * (y2 - y3) - (x2 - x3) * (y1 - y2);
		}

		/// <summary>
		/// Perform an intersection between a plane and a triangle. Builds a
		/// hull hierarchy useful for decimation. This comes at the cost of
		/// more complex code and runtime checks, but the returned results are
		/// more flexible. Results are populated into the IntersectionResult
		/// reference. Check result.isValid() for the final results.
		/// </summary>
		public static void Intersect(Plane plane, Triangle tri, IntersectionResult result)
		{
			// Clear previous results.
			result.Clear();

			// Store local variables for easier access.
			Vector3 posA = tri.PositionA;
			Vector3 posB = tri.PositionB;
			Vector3 posC = tri.PositionC;
			
			bool generateUVs = tri.HasUVs;
			bool generateNormals = tri.HasNormals;
			bool generateTangents = tri.HasTangent;

			// Check which side of the plane the points are on. SideOf
			// operation is a simple dot product and some comparison
			// operations, so these are a very quick checks.
			SideOfPlane sideA = plane.SideOf(posA);
			SideOfPlane sideB = plane.SideOf(posB);
			SideOfPlane sideC = plane.SideOf(posC);

			// Intersection is not possible if all triangle points are on the
			// same side of the plane.
			if (sideA == sideB && sideB == sideC)
			{
				return;
			}

			// If two points are straight on the plane, then the plane is
			// parallel with one of the triangle's edges.
			if ((sideA == SideOfPlane.On && sideA == sideB) ||
			    (sideA == SideOfPlane.On && sideA == sideC) || 
			    (sideB == SideOfPlane.On && sideB == sideC))
			{
				return;
			}

			// If one point is on the plane and the other two are on the same side.
			if ((sideA == SideOfPlane.On && sideB != SideOfPlane.On && sideB == sideC) ||
			    (sideB == SideOfPlane.On && sideA != SideOfPlane.On && sideA == sideC) ||
			    (sideC == SideOfPlane.On && sideA != SideOfPlane.On && sideA == sideB))
			{
				return;
			}

			// Intersection points are shared by both the upper and lower
			// hulls, thus they both lay on the plane that cut them.
			Vector3 qA;
			Vector3 qB;

			// If the points of the triangle lay on the plane itself, then
			// there will only be two triangles. One for the upper hull and the
			// other for the lower. Find which points to put in either hull.
			if (sideA == SideOfPlane.On)
			{
				// If point A is on the plane, check if line B-C intersects it.
				if (!Intersect(plane, posB, posC, out qA))
				{
					return;
				}

				// Construct triangles and return appropriately.
				result.AddIntersectionPoint(qA);
				result.AddIntersectionPoint(posA);

				// The two generated triangles.
				Triangle tA = new(posA, posB, qA);
				Triangle tB = new(posA, qA, posC);

				// Generate UV coordinates, normals, and tangents, if any.
				if (generateUVs)
				{
					Vector2 pQ = tri.GenerateUV(qA);
					Vector2 pA = tri.UVA;
					Vector2 pB = tri.UVB;
					Vector2 pC = tri.UVC;

					tA.SetUV(pA, pB, pQ);
					tB.SetUV(pA, pQ, pC);
				}
				if (generateNormals)
				{
					Vector3 pQ = tri.GenerateNormal(qA);
					Vector3 pA = tri.NormalA;
					Vector3 pB = tri.NormalB;
					Vector3 pC = tri.NormalC;

					tA.SetNormal(pA, pB, pQ);
					tB.SetNormal(pA, pQ, pC);
				}
				if (generateTangents)
				{
					Vector4 pQ = tri.GenerateTangent(qA);
					Vector4 pA = tri.TangentA;
					Vector4 pB = tri.TangentB;
					Vector4 pC = tri.TangentC;

					tA.SetTangent(pA, pB, pQ);
					tB.SetTangent(pA, pQ, pC);
				}

				if (sideB == SideOfPlane.Up)
				{
					// Point B lies on the "up" side of the plane.
					result.AddUpperHull(tA).AddLowerHull(tB);
				}
				else if (sideB == SideOfPlane.Down)
				{
					// Point B lies on the "down" side of the plane.
					result.AddUpperHull(tB).AddLowerHull(tA);
				}
			}
			else if (sideB == SideOfPlane.On)
			{
				// If point B is on the plane, check if line A-C intersects it.
				if (!Intersect(plane, posA, posC, out qA))
				{
					return;
				}

				// Construct triangles and return appropriately.
				result.AddIntersectionPoint(qA);
				result.AddIntersectionPoint(posB);

				// The two generated triangles.
				Triangle tA = new(posA, posB, qA);
				Triangle tB = new(qA, posB, posC);

				// Generate UV coordinates, normals, and tangents, if any.
				if (generateUVs)
				{
					Vector2 pQ = tri.GenerateUV(qA);
					Vector2 pA = tri.UVA;
					Vector2 pB = tri.UVB;
					Vector2 pC = tri.UVC;

					tA.SetUV(pA, pB, pQ);
					tB.SetUV(pQ, pB, pC);
				}
				if (generateNormals)
				{
					Vector3 pQ = tri.GenerateNormal(qA);
					Vector3 pA = tri.NormalA;
					Vector3 pB = tri.NormalB;
					Vector3 pC = tri.NormalC;

					tA.SetNormal(pA, pB, pQ);
					tB.SetNormal(pQ, pB, pC);
				}
				if (generateTangents)
				{
					Vector4 pQ = tri.GenerateTangent(qA);
					Vector4 pA = tri.TangentA;
					Vector4 pB = tri.TangentB;
					Vector4 pC = tri.TangentC;

					tA.SetTangent(pA, pB, pQ);
					tB.SetTangent(pQ, pB, pC);
				}
					
				if (sideA == SideOfPlane.Up)
				{
					// Point A lies on the "up" side of the plane.
					result.AddUpperHull(tA).AddLowerHull(tB);
				}
				else if (sideA == SideOfPlane.Down)
				{
					// Point A lies on the "down" side of the plane.
					result.AddUpperHull(tB).AddLowerHull(tA);
				}
			}
			else if (sideC == SideOfPlane.On)
			{
				// If point C is on the plane, check if line A-B intersects it.
				if (!Intersect(plane, posA, posB, out qA))
				{
					return;
				}

				// Construct triangles and return appropriately.
				result.AddIntersectionPoint(qA);
				result.AddIntersectionPoint(posC);

				// The two generated triangles.
				Triangle tA = new(posA, qA, posC);
				Triangle tB = new(qA, posB, posC);

				// Generate UV coordinates, normals, and tangents, if any.
				if (generateUVs)
				{
					Vector2 pQ = tri.GenerateUV(qA);
					Vector2 pA = tri.UVA;
					Vector2 pB = tri.UVB;
					Vector2 pC = tri.UVC;

					tA.SetUV(pA, pQ, pC);
					tB.SetUV(pQ, pB, pC);
				}
				if (generateNormals)
				{
					Vector3 pQ = tri.GenerateNormal(qA);
					Vector3 pA = tri.NormalA;
					Vector3 pB = tri.NormalB;
					Vector3 pC = tri.NormalC;

					tA.SetNormal(pA, pQ, pC);
					tB.SetNormal(pQ, pB, pC);
				}
				if (generateTangents)
				{
					Vector4 pQ = tri.GenerateTangent(qA);
					Vector4 pA = tri.TangentA;
					Vector4 pB = tri.TangentB;
					Vector4 pC = tri.TangentC;

					tA.SetTangent(pA, pQ, pC);
					tB.SetTangent(pQ, pB, pC);
				}
					
				if (sideA == SideOfPlane.Up)
				{
					// Point A lies on the "up" side of the plane.
					result.AddUpperHull(tA).AddLowerHull(tB);
				}
				else if (sideA == SideOfPlane.Down)
				{
					// Point A lies on the "up" side of the plane.
					result.AddUpperHull(tB).AddLowerHull(tA);
				}
			}

			// At this point, most edge-cases have been tested and failed. Now,
			// full intersection tests can be performed against the lines. From
			// this point on, three triangles will be generated.
			else if (sideA != sideB && Intersect(plane, posA, posB, out qA))
			{
				// Intersection found against line A-B.
				result.AddIntersectionPoint(qA);

				// Check which other lines to check (only one more line needs
				// to be checked) for intersection. The line to check will be
				// the line against the point which lies on the other side of
				// the plane.
				if (sideA == sideC)
				{
					// There is likely an intersection against line B-C which
					// will complete this loop
					if (!Intersect(plane, posB, posC, out qB))
					{
						return;
					}

					result.AddIntersectionPoint(qB);

					// Construct triangles. Two of these will end up on
					// either the upper or the lower hulls.
					Triangle tA = new(qA, posB, qB);
					Triangle tB = new(posA, qA, qB);
					Triangle tC = new(posA, qB, posC);

					// Generate UV coordinates, normals, and tangents, if any.
					if (generateUVs)
					{
						Vector2 pqA = tri.GenerateUV(qA);
						Vector2 pqB = tri.GenerateUV(qB);
						Vector2 pA = tri.UVA;
						Vector2 pB = tri.UVB;
						Vector2 pC = tri.UVC;

						tA.SetUV(pqA, pB, pqB);
						tB.SetUV(pA, pqA, pqB);
						tC.SetUV(pA, pqB, pC);
					}
					if (generateNormals)
					{
						Vector3 pqA = tri.GenerateNormal(qA);
						Vector3 pqB = tri.GenerateNormal(qB);
						Vector3 pA = tri.NormalA;
						Vector3 pB = tri.NormalB;
						Vector3 pC = tri.NormalC;

						tA.SetNormal(pqA, pB, pqB);
						tB.SetNormal(pA, pqA, pqB);
						tC.SetNormal(pA, pqB, pC);
					}
					if (generateTangents)
					{
						Vector4 pqA = tri.GenerateTangent(qA);
						Vector4 pqB = tri.GenerateTangent(qB);
						Vector4 pA = tri.TangentA;
						Vector4 pB = tri.TangentB;
						Vector4 pC = tri.TangentC;

						tA.SetTangent(pqA, pB, pqB);
						tB.SetTangent(pA, pqA, pqB);
						tC.SetTangent(pA, pqB, pC);
					}

					if (sideA == SideOfPlane.Up)
					{
						result.AddUpperHull(tB).AddUpperHull(tC).AddLowerHull(tA);
					}
					else
					{
						result.AddLowerHull(tB).AddLowerHull(tC).AddUpperHull(tA);
					}
				}
				else
				{
					// In this scenario, point A is a "lone" point which lies
					// in either upper or lower hull. Another intersection must
					// be performed to find the last point.
					if (!Intersect(plane, posA, posC, out qB))
					{
						return;
					}

					result.AddIntersectionPoint(qB);

					// Construct triangles. Two of these will end up on
					// either the upper or the lower hulls.
					Triangle tA = new(posA, qA, qB);
					Triangle tB = new(qA, posB, posC);
					Triangle tC = new(qB, qA, posC);

					// Generate UV coordinates, normals, and tangents, if any.
					if (generateUVs)
					{
						Vector2 pqA = tri.GenerateUV(qA);
						Vector2 pqB = tri.GenerateUV(qB);
						Vector2 pA = tri.UVA;
						Vector2 pB = tri.UVB;
						Vector2 pC = tri.UVC;

						tA.SetUV(pA, pqA, pqB);
						tB.SetUV(pqA, pB, pC);
						tC.SetUV(pqB, pqA, pC);
					}
					if (generateNormals)
					{
						Vector3 pqA = tri.GenerateNormal(qA);
						Vector3 pqB = tri.GenerateNormal(qB);
						Vector3 pA = tri.NormalA;
						Vector3 pB = tri.NormalB;
						Vector3 pC = tri.NormalC;

						tA.SetNormal(pA, pqA, pqB);
						tB.SetNormal(pqA, pB, pC);
						tC.SetNormal(pqB, pqA, pC);
					}
					if (generateTangents)
					{
						Vector4 pqA = tri.GenerateTangent(qA);
						Vector4 pqB = tri.GenerateTangent(qB);
						Vector4 pA = tri.TangentA;
						Vector4 pB = tri.TangentB;
						Vector4 pC = tri.TangentC;

						tA.SetTangent(pA, pqA, pqB);
						tB.SetTangent(pqA, pB, pC);
						tC.SetTangent(pqB, pqA, pC);
					}

					if (sideA == SideOfPlane.Up)
					{
						result.AddUpperHull(tA).AddLowerHull(tB).AddLowerHull(tC);
					}
					else
					{
						result.AddLowerHull(tA).AddUpperHull(tB).AddUpperHull(tC);
					}
				}
			}

			// If the line A-B did not intersect (or they lay on the same side
			// of the plane) this simplifies the problem. This means there is
			// an intersection in the line A-C and B-C, which can be used to
			// build new upper and lower hulls. Both of these intersection
			// tests are expected to pass.
			else if (Intersect(plane, posC, posA, out qA) && Intersect(plane, posC, posB, out qB))
			{
				// Here, the line A-B must lay on the same side of the plane.
				result.AddIntersectionPoint(qA);
				result.AddIntersectionPoint(qB);

				// Construct triangles. Two of these will end up on
				// either the upper or the lower hulls.
				Triangle tA = new(qA, qB, posC);
				Triangle tB = new(posA, qB, qA);
				Triangle tC = new(posA, posB, qB);

				// Generate UV coordinates, normals, and tangents, if any.
				if (generateUVs)
				{
					Vector2 pqA = tri.GenerateUV(qA);
					Vector2 pqB = tri.GenerateUV(qB);
					Vector2 pA = tri.UVA;
					Vector2 pB = tri.UVB;
					Vector2 pC = tri.UVC;

					tA.SetUV(pqA, pqB, pC);
					tB.SetUV(pA, pqB, pqA);
					tC.SetUV(pA, pB, pqB);
				}
				if (generateNormals)
				{
					Vector3 pqA = tri.GenerateNormal(qA);
					Vector3 pqB = tri.GenerateNormal(qB);
					Vector3 pA = tri.NormalA;
					Vector3 pB = tri.NormalB;
					Vector3 pC = tri.NormalC;

					tA.SetNormal(pqA, pqB, pC);
					tB.SetNormal(pA, pqB, pqA);
					tC.SetNormal(pA, pB, pqB);
				}
				if (generateTangents)
				{
					Vector4 pqA = tri.GenerateTangent(qA);
					Vector4 pqB = tri.GenerateTangent(qB);
					Vector4 pA = tri.TangentA;
					Vector4 pB = tri.TangentB;
					Vector4 pC = tri.TangentC;

					tA.SetTangent(pqA, pqB, pC);
					tB.SetTangent(pA, pqB, pqA);
					tC.SetTangent(pA, pB, pqB);
				}

				if (sideA == SideOfPlane.Up)
				{
					result.AddUpperHull(tB).AddUpperHull(tC).AddLowerHull(tA);
				}
				else
				{
					result.AddLowerHull(tB).AddLowerHull(tC).AddUpperHull(tA);
				}
			}
		}
	}
}
