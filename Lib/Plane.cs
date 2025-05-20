using UnityEngine;

namespace EzSlice.Lib
{
	/// <summary>
	/// Enum for where the point lies on the plane.
	/// </summary>
	public enum SideOfPlane
	{
		Up,   // Upwards from the normal.
		Down, // Downwards from the normal.
		On    // Point lies straight on the plane.
	}

	/// <summary>
	/// Represents a simple 3D Plane structure with a position and direction
	/// which extends infinitely in its axis. This provides an optimal
	/// structure for collision tests for the slicing framework.
	/// </summary>
	public struct Plane
	{
		private Vector3 _normal;
		private float _dist;
		
		#if UNITY_EDITOR
		// Editor-only DEBUG functionality.
		private Transform _transformRef;
		#endif

		public Plane(Vector3 position, Vector3 normal)
		{
			_normal = normal;
			_dist = Vector3.Dot(normal, position);

			#if UNITY_EDITOR
			_transformRef = null;
			#endif
		}

		public Plane(Vector3 normal, float dot)
		{
			_normal = normal;
			_dist = dot;

			#if UNITY_EDITOR
			_transformRef = null;
			#endif
		}

		public Plane(Vector3 a, Vector3 b, Vector3 c)
		{
			_normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
			_dist = -Vector3.Dot(_normal, a);

			#if UNITY_EDITOR
			_transformRef = null;
			#endif
		}
		
		public Vector3 Normal => _normal;
		public float Dist => _dist;

		public void Compute(Vector3 position, Vector3 normal)
		{
			_normal = normal;
			_dist = Vector3.Dot(normal, position);
		}

		public void Compute(Transform trans)
		{
			Compute(trans.position, trans.up);
			
			#if UNITY_EDITOR
			_transformRef = trans;
			#endif
		}

		public void Compute(GameObject obj)
		{
			Compute(obj.transform);
		}

		/// <summary>
		/// Checks which side of the plane the point lies on.
		/// </summary>
		public SideOfPlane SideOf(Vector3 point)
		{
			float result = Vector3.Dot(_normal, point) - _dist;
			if (result > Intersector.Epsilon)
			{
				return SideOfPlane.Up;
			}
			if (result < -Intersector.Epsilon)
			{
				return SideOfPlane.Down;
			}
			return SideOfPlane.On;
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
			if (!_transformRef)
			{
				return;
			}

			Color prevColor = Gizmos.color;
			Matrix4x4 prevMatrix = Gizmos.matrix;
			
			Gizmos.matrix = Matrix4x4.TRS(
				_transformRef.position,
				_transformRef.rotation,
				_transformRef.localScale);
			Gizmos.color = drawColor;

			Gizmos.DrawWireCube(Vector3.zero, new Vector3(1.0f, 0.0f, 1.0f));

			Gizmos.color = prevColor;
			Gizmos.matrix = prevMatrix;
			#endif
		}
	}
}
