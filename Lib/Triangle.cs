using UnityEngine;

namespace EzSlice.Lib
{
	/// <summary>
	/// 3D Triangle structure with position and UV map. UVs are required if the
	/// slicer needs to recalculate the new UV position for texture mapping.
	/// </summary>
	public struct Triangle
	{
		// The points which represent this triangle. Required and immutable.
		private readonly Vector3 _posA;
		private readonly Vector3 _posB;
		private readonly Vector3 _posC;

		// The UV coordinates of this triangle. Optional.
		private bool _hasUVs;
		private Vector2 _uvA;
		private Vector2 _uvB;
		private Vector2 _uvC;

		// The normals of the vertices. Optional.
		private bool _hasNormals;
		private Vector3 _normalA;
		private Vector3 _normalB;
		private Vector3 _normalC;

		// The tangents of the vertices. Optional.
		private bool _hasTangents;
		private Vector4 _tanA;
		private Vector4 _tanB;
		private Vector4 _tanC;

		public Triangle(Vector3 posA, Vector3 posB, Vector3 posC)
		{
			_posA = posA;
			_posB = posB;
			_posC = posC;
			
			_hasUVs = false;
			_uvA = Vector2.zero;
			_uvB = Vector2.zero;
			_uvC = Vector2.zero;
			
			_hasNormals = false;
			_normalA = Vector3.zero;
			_normalB = Vector3.zero;
			_normalC = Vector3.zero;
			
			_hasTangents = false;
			_tanA = Vector4.zero;
			_tanB = Vector4.zero;
			_tanC = Vector4.zero;
		}

		public Vector3 PositionA => _posA;
		public Vector3 PositionB => _posB;
		public Vector3 PositionC => _posC;
		
		public bool HasUVs => _hasUVs;
		public Vector2 UVA => _uvA;
		public Vector2 UVB => _uvB;
		public Vector2 UVC => _uvC;
		
		public bool HasNormals => _hasNormals;
		public Vector3 NormalA => _normalA;
		public Vector3 NormalB => _normalB;
		public Vector3 NormalC => _normalC;

		public bool HasTangent => _hasTangents;
		public Vector4 TangentA => _tanA;
		public Vector4 TangentB => _tanB;
		public Vector4 TangentC => _tanC;

		public void SetUV(Vector2 uvA, Vector2 uvB, Vector2 uvC)
		{
			_uvA = uvA;
			_uvB = uvB;
			_uvC = uvC;
			_hasUVs = true;
		}
		
		public void SetNormal(Vector3 normalA, Vector3 normalB, Vector3 normalC)
		{
			_normalA = normalA;
			_normalB = normalB;
			_normalC = normalC;
			_hasNormals = true;
		}

		public void SetTangent(Vector4 tanA, Vector4 tanB, Vector4 tanC)
		{
			_tanA = tanA;
			_tanB = tanB;
			_tanC = tanC;
			_hasTangents = true;
		}

		/// <summary>
		/// Compute and set tangents of this triangle.
		/// Derived from https://answers.unity.com/questions/7789/calculating-tangents-vector4.html
		/// </summary>
		public void ComputeTangents()
		{
			// Computing tangents requires both UVs and normals to be set.
			if (!_hasNormals || !_hasUVs)
			{
				return;
			}

			Vector3 v1 = _posA;
			Vector3 v2 = _posB;
			Vector3 v3 = _posC;

			Vector2 w1 = _uvA;
			Vector2 w2 = _uvB;
			Vector2 w3 = _uvC;

			float x1 = v2.x - v1.x;
			float x2 = v3.x - v1.x;
			float y1 = v2.y - v1.y;
			float y2 = v3.y - v1.y;
			float z1 = v2.z - v1.z;
			float z2 = v3.z - v1.z;

			float s1 = w2.x - w1.x;
			float s2 = w3.x - w1.x;
			float t1 = w2.y - w1.y;
			float t2 = w3.y - w1.y;

			float r = 1.0f / (s1 * t2 - s2 * t1);

			Vector3 sDir = new(
				(t2 * x1 - t1 * x2) * r,
				(t2 * y1 - t1 * y2) * r,
				(t2 * z1 - t1 * z2) * r);
			Vector3 tDir = new(
				(s1 * x2 - s2 * x1) * r,
				(s1 * y2 - s2 * y1) * r,
				(s1 * z2 - s2 * z1) * r);

			Vector3 n1 = _normalA;
			Vector3 nt1 = sDir;

			Vector3.OrthoNormalize(ref n1, ref nt1);
			float tanAw = Vector3.Dot(Vector3.Cross(n1, nt1), tDir) < 0.0f ? -1.0f : 1.0f;
			Vector4 tanA = new(nt1.x, nt1.y, nt1.z, tanAw);

			Vector3 n2 = _normalB;
			Vector3 nt2 = sDir;

			Vector3.OrthoNormalize(ref n2, ref nt2);
			float tanBw = Vector3.Dot(Vector3.Cross(n2, nt2), tDir) < 0.0f ? -1.0f : 1.0f;
			Vector4 tanB = new(nt2.x, nt2.y, nt2.z, tanBw);

			Vector3 n3 = _normalC;
			Vector3 nt3 = sDir;

			Vector3.OrthoNormalize(ref n3, ref nt3);
			float tanCw = Vector3.Dot(Vector3.Cross(n3, nt3), tDir) < 0.0f ? -1.0f : 1.0f;
			Vector4 tanC = new(nt3.x, nt3.y, nt3.z, tanCw);
			
			SetTangent(tanA, tanB, tanC);
		}

		/// <summary>
		/// Calculate the Barycentric coordinate weight values u-v-w for a
		/// point relative to the triangle. This is useful for computing new
		/// UV coordinates for arbitrary points.
		/// </summary>
		public Vector3 Barycentric(Vector3 p)
		{
			Vector3 a = _posA;
			Vector3 b = _posB;
			Vector3 c = _posC;

			Vector3 m = Vector3.Cross(b - a, c - a);

			float nu;
			float nv;
			float ood;

			float x = Mathf.Abs(m.x);
			float y = Mathf.Abs(m.y);
			float z = Mathf.Abs(m.z);

			// Compute areas of plane with the largest projections.
			if (x >= y && x >= z)
			{
				// Area of PBC in YZ plane.
				nu = Intersector.TriArea2D(p.y, p.z, b.y, b.z, c.y, c.z);
				// Area of PCA in YZ plane.
				nv = Intersector.TriArea2D(p.y, p.z, c.y, c.z, a.y, a.z);
				// 1/2 * area of ABC in YZ plane.
				ood = 1.0f / m.x;
			}
			else if (y >= x && y >= z)
			{
				// Project in XZ plane.
				nu = Intersector.TriArea2D(p.x, p.z, b.x, b.z, c.x, c.z);
				nv = Intersector.TriArea2D(p.x, p.z, c.x, c.z, a.x, a.z);
				ood = 1.0f / -m.y;
			}
			else
			{
				// Project in XY plane.
				nu = Intersector.TriArea2D(p.x, p.y, b.x, b.y, c.x, c.y);
				nv = Intersector.TriArea2D(p.x, p.y, c.x, c.y, a.x, a.y);
				ood = 1.0f / m.z;
			}

			float u = nu * ood;
			float v = nv * ood;
			float w = 1.0f - u - v;

			return new Vector3(u, v, w);
		}

		/// <summary>
		/// Generate a set of new UV coordinates for the provided point
		/// relative to the triangle. Uses weight values for the computation,
		/// so triangle must have UVs set in order to return the correct
		/// results. Otherwise, Vector2.zero will be returned.
		/// </summary>
		public Vector2 GenerateUV(Vector3 point)
		{
			if (!_hasUVs)
			{
				return Vector2.zero;
			}
			Vector3 weights = Barycentric(point);
			return (weights.x * _uvA) + (weights.y * _uvB) + (weights.z * _uvC);
		}

		/// <summary>
		/// Generate a set of new normal coordinates for the provided point
		/// relative to the triangle. Uses weight values for the computation,
		/// so triangle must have normals set in order to return the correct
		/// results. Otherwise, Vector3.zero will be returned.
		/// </summary>
		public Vector3 GenerateNormal(Vector3 point)
		{
			if (!_hasNormals)
			{
				return Vector3.zero;
			}
			Vector3 weights = Barycentric(point);
			return (weights.x * _normalA) + (weights.y * _normalB) + (weights.z * _normalC);
		}

		/// <summary>
		/// Generate a set of new tangent coordinates for the provided point
		/// relative to the triangle. Uses weight values for the computation,
		/// so triangle must have tangents set in order to return the correct
		/// results. Otherwise, Vector4.zero will be returned.
		/// </summary>
		public Vector4 GenerateTangent(Vector3 point)
		{
			if (!_hasNormals)
			{
				return Vector4.zero;
			}
			Vector3 weights = Barycentric(point);
			return (weights.x * _tanA) + (weights.y * _tanB) + (weights.z * _tanC);
		}

		/// <summary>
		/// Split triangle with provided plane and store the results inside the
		/// IntersectionResult structure. Returns true on success.
		/// </summary>
		public bool Split(Plane plane, IntersectionResult result)
		{
			Intersector.Intersect(plane, this, result);
			return result.IsValid;
		}

		/// <summary>
		/// Return true if the triangle has a clock-wise winding order.
		/// </summary>
		public bool IsClockWise()
		{
			return SignedSquare(_posA, _posB, _posC) >= float.Epsilon;
		}

		/// <summary>
		/// Returns the signed square of a triangle. Useful for checking
		/// winding order.
		/// </summary>
		private static float SignedSquare(Vector3 a, Vector3 b, Vector3 c)
		{
			return
				a.x * (b.y * c.z - b.z * c.y) - 
				a.y * (b.x * c.z - b.z * c.x) + 
				a.z * (b.x * c.y - b.y * c.x);
		}

		/// <summary>
		/// Editor-only DEBUG functionality.
		/// </summary>
		public void OnDebugDraw()
		{
			OnDebugDraw(Color.white);
		}

		public void OnDebugDraw(Color drawColor)
		{
			#if UNITY_EDITOR
			Color prevColor = Gizmos.color;
			Gizmos.color = drawColor;

			Gizmos.DrawLine(PositionA, PositionB);
			Gizmos.DrawLine(PositionB, PositionC);
			Gizmos.DrawLine(PositionC, PositionA);
			Gizmos.color = prevColor;
			#endif
		}
	}
}
