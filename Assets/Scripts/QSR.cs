using UnityEngine;
using System;

using Global;

// RCC8 relations
// grossly underspecified for now
namespace QSR
{
	public static class QSR
	{
		// TODO: Make camera relative
		// left
		public static bool Left(Bounds x, Bounds y) {
			bool left = false;
			Vector3 offset = x.center - y.center;

			if (x.center.x > y.center.x) {
				left = true;
			}

			return left;
		}

		// right
		public static bool Right(Bounds x, Bounds y) {
			bool right = false;

			if (x.center.x < y.center.x) {
				right = true;
			}

			return right;
		}

		// behind
		public static bool Behind(Bounds x, Bounds y) {
			bool behind = false;

			if (x.center.z > y.center.z) {
				behind = true;
			}

			return behind;
		}

		// in front
		public static bool InFront(Bounds x, Bounds y) {
			bool inFront = false;

			if (x.center.z < y.center.z) {
				inFront = true;
			}

			return inFront;
		}
			
		// below
		public static bool Below(Bounds x, Bounds y) {
			bool below = false;

			if (x.max.y <= y.min.y-Constants.EPSILON) {
				below = true;
			}

			return below;
		}

		// above
		public static bool Above(Bounds x, Bounds y) {
			bool above = false;

			if (x.min.y >= y.max.y-Constants.EPSILON) {
				above = true;
			}

			return above;
		}
	}
}