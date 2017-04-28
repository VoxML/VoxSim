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

			if (x.min.x >= y.max.x-Constants.EPSILON) {
				left = true;
			}

			return left;
		}

		// right
		public static bool Right(Bounds x, Bounds y) {
			bool right = false;

			if (x.max.x <= y.min.x+Constants.EPSILON) {
				right = true;
			}

			return right;
		}

		// behind
		public static bool Behind(Bounds x, Bounds y) {
			bool behind = false;

			if (x.min.z >= y.max.z-Constants.EPSILON) {
				behind = true;
			}

			return behind;
		}

		// in front
		public static bool InFront(Bounds x, Bounds y) {
			bool inFront = false;

			if (x.max.z <= y.min.z+Constants.EPSILON) {
				inFront = true;
			}

			return inFront;
		}
			
		// below
		public static bool Below(Bounds x, Bounds y) {
			bool below = false;

			if (x.max.y <= y.min.y+Constants.EPSILON) {
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