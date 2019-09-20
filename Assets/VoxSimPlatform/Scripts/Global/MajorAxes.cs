using UnityEngine;

namespace MajorAxes {
	public enum MajorAxis {
		None,
		X,
		Y,
		Z
	};

	public class AxisVector {
		public static Vector3 posXAxis = new Vector3(1.0f, 0.0f, 0.0f);
		public static Vector3 posYAxis = new Vector3(0.0f, 1.0f, 0.0f);
		public static Vector3 posZAxis = new Vector3(0.0f, 0.0f, 1.0f);
		public static Vector3 negXAxis = new Vector3(-1.0f, 0.0f, 0.0f);
		public static Vector3 negYAxis = new Vector3(0.0f, -1.0f, 0.0f);
		public static Vector3 negZAxis = new Vector3(0.0f, 0.0f, -1.0f);
	}
}