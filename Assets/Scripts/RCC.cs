using UnityEngine;
using System;

using Global;

// RCC8 relations
// grossly underspecified for now
namespace RCC
{
	public static class RCC8
	{
		// disconnected
		public static bool DC(Bounds x, Bounds y) {
			bool dc = false;

			if (!EC (x, y) && !PO (x, y) && !EQ (x, y) &&
			    !TPP (x, y) && !NTPP (x, y) && !TPPi (x, y) && !NTPPi (x, y)) {
				dc = true;
			}

			return dc;
		}

		// externally connected
		public static bool EC(Bounds x, Bounds y) {
			bool ec = false;
			
			// if y and z dimensions overlap
			if (Mathf.Abs(x.center.y - y.center.y) * 2 < (x.size.y + y.size.y) &&
				(Mathf.Abs(x.center.z - y.center.z) * 2 < (x.size.z + y.size.z))){
				if ((Mathf.Abs(x.min.x-y.max.x) < Constants.EPSILON) ||	// if touching on x
					(Mathf.Abs(x.max.x-y.min.x) < Constants.EPSILON)) {
					ec = true;
				}
			}
			// if x and z dimensions overlap
			if (Mathf.Abs(x.center.x - y.center.x) * 2 < (x.size.x + y.size.x) &&
				(Mathf.Abs(x.center.z - y.center.z) * 2 < (x.size.z + y.size.z))){
				if ((Mathf.Abs(x.min.y-y.max.y) < Constants.EPSILON) ||	// if touching on y
					(Mathf.Abs(x.max.y-y.min.y) < Constants.EPSILON)) {
					ec = true;
				}
			}
			// if x and y dimensions overlap
			if (Mathf.Abs (x.center.x - y.center.x) * 2 < (x.size.x + y.size.x) &&
			    (Mathf.Abs (x.center.y - y.center.y) * 2 < (x.size.y + y.size.y))) {
				if ((Mathf.Abs (x.min.z - y.max.z) < Constants.EPSILON) ||	// if touching on z
					(Mathf.Abs (x.max.z - y.min.z) < Constants.EPSILON)) {
					ec = true;
				}
			}

			return ec;
		}

		public static bool PO(Bounds x, Bounds y) {
			bool po = false;

			if (x.Intersects (y)) {
				float xOff = Mathf.Abs(Mathf.Abs((x.center - y.center).x) - Mathf.Abs(((x.size.x / 2) + (y.size.x / 2))));
				float yOff = Mathf.Abs(Mathf.Abs((x.center - y.center).y) - Mathf.Abs(((x.size.y / 2) + (y.size.y / 2))));
				float zOff = Mathf.Abs(Mathf.Abs((x.center - y.center).z) - Mathf.Abs(((x.size.z / 2) + (y.size.z / 2))));
				// intersects but not too much, system is a little fuzzy
				if ((xOff > Constants.EPSILON) && (yOff > Constants.EPSILON) && (zOff > Constants.EPSILON)) {
					po = true;
				}
			}

			return po;
		}

		public static bool EQ(Bounds x, Bounds y) {
			bool eq = false;

			if (((x.min-y.min).magnitude < Constants.EPSILON) && ((x.max-y.max).magnitude < Constants.EPSILON)) {
				eq = true;
			}

			return eq;
		}

		public static bool TPP(Bounds x, Bounds y) {
			bool tpp = false;

			if (y.Contains (x.min) && y.Contains (x.max)) {
				if ((Mathf.Abs(x.min.x-y.min.x) < Constants.EPSILON) ||
					(Mathf.Abs(x.max.x-y.max.x) < Constants.EPSILON) ||
					(Mathf.Abs(x.min.y-y.min.y) < Constants.EPSILON) ||
					(Mathf.Abs(x.max.y-y.max.y) < Constants.EPSILON) ||
					(Mathf.Abs(x.min.z-y.min.z) < Constants.EPSILON) ||
					(Mathf.Abs(x.max.z-y.max.z) < Constants.EPSILON))
				tpp = true;
			}

			return tpp;
		}

		public static bool NTPP(Bounds x, Bounds y) {
			bool ntpp = false;

			if (!TPP (x, y)) {
				if (y.Contains (x.min) && y.Contains (x.max)) {
					ntpp = true;
				}
			}

			return ntpp;
		}

		public static bool TPPi(Bounds x, Bounds y) {
			bool tppi = false;

			if (TPP (y, x)) {
				tppi = true;
			}

			return tppi;
		}

		public static bool NTPPi(Bounds x, Bounds y) {
			bool ntppi = false;

			if (NTPP (y, x)) {
				ntppi = true;
			}

			return ntppi;
		}

		public static bool ProperPart(Bounds x, Bounds y) {
			bool properpart = false;

			if (TPP (x, y) || NTPP (x, y)) {
				properpart = true;
			}

			return properpart;
		}
	}
}

