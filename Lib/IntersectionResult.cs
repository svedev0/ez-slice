using UnityEngine;

namespace EzSlice.Lib
{
	/// <summary>
	/// A structure which contains intersection information for Plane->Triangle
	/// intersection tests.
	/// TO-DO: This structure can be optimized to hold less data via an
	/// optional indices array. This could lead to faster intersection tests.
	/// </summary>
	public sealed class IntersectionResult
	{
		// General tag to check if this structure is valid.
		private bool _isSuccess;

		// Intersection points/triangles.
		private readonly Triangle[] _upperHull;
		private readonly Triangle[] _lowerHull;
		private readonly Vector3[] _intersectionPoints;

		// Counters. Raw arrays used for performance reasons.
		private int _upperHullCount;
		private int _lowerHullCount;
		private int _intersectionPointCount;

		public IntersectionResult()
		{
			_isSuccess = false;

			_upperHull = new Triangle[2];
			_lowerHull = new Triangle[2];
			_intersectionPoints = new Vector3[2];

			_upperHullCount = 0;
			_lowerHullCount = 0;
			_intersectionPointCount = 0;
		}
		
		public bool IsValid => _isSuccess;

		public Triangle[] UpperHull => _upperHull;
		public Triangle[] LowerHull => _lowerHull;
		public Vector3[] IntersectionPoints => _intersectionPoints;

		public int UpperHullCount => _upperHullCount;
		public int LowerHullCount => _lowerHullCount;
		public int IntersectionPointsCount => _intersectionPointCount;

		/// <summary>
		/// Used by intersector, adds a new triangle to the upper hull section.
		/// </summary>
		public IntersectionResult AddUpperHull(Triangle tri)
		{
			_upperHull[_upperHullCount++] = tri;
			_isSuccess = true;
			return this;
		}

		/// <summary>
		/// Used by intersector, adds a new triangle to the lower gull section.
		/// </summary>
		public IntersectionResult AddLowerHull(Triangle tri)
		{
			_lowerHull[_lowerHullCount++] = tri;
			_isSuccess = true;
			return this;
		}

		/// <summary>
		/// Used by intersector, adds a new intersection point which is shared
		/// by both upper & lower hulls.
		/// </summary>
		public void AddIntersectionPoint(Vector3 point)
		{
			_intersectionPoints[_intersectionPointCount++] = point;
		}

		/// <summary>
		/// Clear the current state of this object.
		/// </summary>
		public void Clear()
		{
			_isSuccess = false;
			_upperHullCount = 0;
			_lowerHullCount = 0;
			_intersectionPointCount = 0;
		}

		/// <summary>
		/// Editor-only DEBUG functionality.
		/// </summary>
		public void OnDebugDraw()
		{
			OnDebugDraw(Color.white);
		}

		private void OnDebugDraw(Color drawColor)
		{
			#if UNITY_EDITOR
			if (!IsValid)
			{
				return;
			}

			Color prevColor = Gizmos.color;
			Gizmos.color = drawColor;

			// Draw intersection points.
			for (int i = 0; i < IntersectionPointsCount; i++)
			{
				Gizmos.DrawSphere(IntersectionPoints[i], 0.1f);
			}

			// Draw upper hull in red.
			for (int i = 0; i < UpperHullCount; i++)
			{
				UpperHull[i].OnDebugDraw(Color.red);
			}

			// Draw lower hull in blue.
			for (int i = 0; i < LowerHullCount; i++)
			{
				LowerHull[i].OnDebugDraw(Color.blue);
			}

			Gizmos.color = prevColor;
			#endif
		}
	}
}
