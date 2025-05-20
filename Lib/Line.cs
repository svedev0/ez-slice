using UnityEngine;

namespace EzSlice.Lib
{
	public readonly struct Line
	{
		private readonly Vector3 _posA;
		private readonly Vector3 _posB;

		public Line(Vector3 pointA, Vector3 pointB)
		{
			_posA = pointA;
			_posB = pointB;
		}
		
		public Vector3 PositionA => _posA;
		public Vector3 PositionB => _posB;
		public float Dist => Vector3.Distance(_posA, _posB);
		public float DistSq => (_posA - _posB).sqrMagnitude;
	}
}
