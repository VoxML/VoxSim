using UnityEngine;
using System;
using System.Linq;

using Global;
using MajorAxes;

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
				if ((Mathf.Abs (x.min.x - y.max.x) < Constants.EPSILON * 2) ||	// if touching on x
					(Mathf.Abs (x.max.x - y.min.x) < Constants.EPSILON * 2)) {
					ec = true;
				}
				else {
					Debug.Log (Mathf.Abs (x.min.x - y.max.x));
					Debug.Log (Mathf.Abs (x.max.x - y.min.x));
				}
			}
			// if x and z dimensions overlap
			if (Mathf.Abs(x.center.x - y.center.x) * 2 < (x.size.x + y.size.x) &&
				(Mathf.Abs(x.center.z - y.center.z) * 2 < (x.size.z + y.size.z))){
				Debug.Log (x.min.y);
				Debug.Log (y.max.y);
				if ((Mathf.Abs(x.min.y-y.max.y) < Constants.EPSILON * 2) ||	// if touching on y
					(Mathf.Abs(x.max.y-y.min.y) < Constants.EPSILON * 2)) {
					ec = true;
				}
			}
			// if x and y dimensions overlap
			if (Mathf.Abs (x.center.x - y.center.x) * 2 < (x.size.x + y.size.x) &&
			    (Mathf.Abs (x.center.y - y.center.y) * 2 < (x.size.y + y.size.y))) {
				if ((Mathf.Abs (x.min.z - y.max.z) < Constants.EPSILON * 2) ||	// if touching on z
					(Mathf.Abs (x.max.z - y.min.z) < Constants.EPSILON * 2)) {
					ec = true;
				}
			}

			return ec;
		}

		public static bool EC(ObjBounds x, ObjBounds y) {
			bool ec = false;

//			if (Mathf.Abs (x.Center.y - y.Center.y) * 2 < ((x.Max(MajorAxis.Y).y-x.Center.y)*2 + (y.Max(MajorAxis.Y).y-y.Center.y)*2) &&
//				(Mathf.Abs (x.Center.z - y.Center.z) * 2 < ((x.Max(MajorAxis.Z).z-x.Center.z)*2 + (y.Max(MajorAxis.Z).z-y.Center.z)*2))) {
				if (x.Center.x <= y.Max(MajorAxis.X).x) {
//				Debug.Log (Helper.VectorToParsable(x.Center));
//				Debug.Log (Helper.VectorToParsable(y.Center));
					foreach (Vector3 point in x.Points.Where(p => p.x >= x.Center.x).ToList()) {
//						Debug.Log (Helper.VectorToParsable (point));
						RaycastHit hitInfo;
						Vector3 origin = new Vector3 (point.x-Constants.EPSILON, point.y == x.Min (MajorAxis.Y).y ? point.y + Constants.EPSILON : point.y == x.Max (MajorAxis.Y).y ? point.y - Constants.EPSILON : point.y,
							point.z == x.Min (MajorAxis.Z).z ? point.x + Constants.EPSILON : point.z == x.Max (MajorAxis.Z).z ? point.z - Constants.EPSILON : point.z);
						bool hit = Physics.Raycast (origin, Vector3.right, out hitInfo);
						if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform.position)) &&
							(hitInfo.distance <= Constants.EPSILON * 3) ) {
							Debug.Log (string.Format ("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
							ec = true;
						}
					}
				} 
				else if (x.Center.x >= y.Max(MajorAxis.X).x) {
					foreach (Vector3 point in x.Points.Where(p => p.x <= x.Center.x).ToList()) {
						RaycastHit hitInfo;
						Vector3 origin = new Vector3 (point.x+Constants.EPSILON, point.y == x.Min (MajorAxis.Y).y ? point.y + Constants.EPSILON : point.y == x.Max (MajorAxis.Y).y ? point.y - Constants.EPSILON : point.y,
							point.z == x.Min (MajorAxis.Z).z ? point.x + Constants.EPSILON : point.z == x.Max (MajorAxis.Z).z ? point.z - Constants.EPSILON : point.z);
						bool hit = Physics.Raycast (origin, -Vector3.right, out hitInfo);
						if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform.position)) && 
							(hitInfo.distance <= Constants.EPSILON * 3)) {
							Debug.Log (string.Format ("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
							ec = true;
						}
					}
				}
//			}

//			if (Mathf.Abs (x.Center.x - y.Center.x) * 2 < ((x.Max (MajorAxis.X).x - x.Center.x) * 2 + (y.Max (MajorAxis.X).x - y.Center.x) * 2) &&
//				(Mathf.Abs (x.Center.z - y.Center.z) * 2 < ((x.Max (MajorAxis.Z).z - x.Center.z) * 2 + (y.Max (MajorAxis.Z).z - y.Center.z) * 2))) {
				//if (x.Center.x <= y.Center.x) {
					if (x.Center.y <= y.Min(MajorAxis.Y).y) {
						Debug.Log (Helper.VectorToParsable(x.Center));
						Debug.Log (Helper.VectorToParsable(y.Center));
						foreach (Vector3 point in x.Points.Where(p => p.y >= x.Center.y).ToList()) {
							RaycastHit hitInfo;
							Vector3 origin = new Vector3 (point.x == x.Min (MajorAxis.X).x ? point.x + Constants.EPSILON : point.x == x.Max (MajorAxis.X).x ? point.x - Constants.EPSILON : point.x,
								point.y-Constants.EPSILON, point.z == x.Min (MajorAxis.Z).z ? point.x + Constants.EPSILON : point.z == x.Max (MajorAxis.Z).z ? point.z - Constants.EPSILON : point.z);
							bool hit = Physics.Raycast (origin, Vector3.up, out hitInfo);
//							if (y.Contains (Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject).transform.position)) {
//								Debug.Log (hitInfo.collider.gameObject);
//								Debug.Log (hitInfo.distance);
//								Debug.Log (Helper.VectorToParsable(hitInfo.collider.gameObject.transform.position));
//								Debug.Log (Helper.VectorToParsable (Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject).transform.position));
//							}
							if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform.position)) &&
								(hitInfo.distance <= Constants.EPSILON * 3)) {
								Debug.Log (string.Format ("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
								ec = true;
							}
						}
					}
					else if (x.Center.y >= y.Max(MajorAxis.Y).y) {
						Debug.Log (Helper.VectorToParsable(x.Center));
						Debug.Log (Helper.VectorToParsable(y.Center));
						foreach (Vector3 point in x.Points.Where(p => p.y <= x.Center.y).ToList()) {
							Debug.Log(point);
							RaycastHit hitInfo;
							Vector3 origin = new Vector3 (point.x == x.Min (MajorAxis.X).x ? point.x + Constants.EPSILON : point.x == x.Max (MajorAxis.X).x ? point.x - Constants.EPSILON : point.x,
								point.y+Constants.EPSILON, point.z == x.Min (MajorAxis.Z).z ? point.x + Constants.EPSILON : point.z == x.Max (MajorAxis.Z).z ? point.z - Constants.EPSILON : point.z);
							bool hit = Physics.Raycast (origin, -Vector3.up, out hitInfo);
							Debug.Log (hitInfo.collider.gameObject);
							if (y.Contains (Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject).transform.position)) {
								Debug.Log (hitInfo.collider.gameObject);
								Debug.Log (hitInfo.distance);
								Debug.Log (Helper.VectorToParsable(hitInfo.collider.gameObject.transform.position));
								Debug.Log (Helper.VectorToParsable (Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject).transform.position));
							}
							if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform.position)) &&
								(hitInfo.distance <= Constants.EPSILON * 3)) {
								Debug.Log (string.Format ("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
								ec = true;
							}
						}
					}
				//}
//			}

//			if (Mathf.Abs (x.Center.x - y.Center.x) * 2 < ((x.Max (MajorAxis.X).x - x.Center.x) * 2 + (y.Max (MajorAxis.X).x - y.Center.x) * 2) &&
//				(Mathf.Abs (x.Center.y - y.Center.y) * 2 < ((x.Max (MajorAxis.Y).y - x.Center.y) * 2 + (y.Max (MajorAxis.Y).y - y.Center.y) * 2))) {
				if (x.Center.z <= y.Min(MajorAxis.Z).z) {
					foreach (Vector3 point in x.Points.Where(p => p.z >= x.Center.z).ToList()) {
						RaycastHit hitInfo;
						Vector3 origin = new Vector3 (point.x == x.Min (MajorAxis.X).x ? point.x + Constants.EPSILON : point.x == x.Max (MajorAxis.X).x ? point.x - Constants.EPSILON : point.x,
							point.y == x.Min (MajorAxis.Y).y ? point.y + Constants.EPSILON : point.y == x.Max (MajorAxis.Y).y ? point.y - Constants.EPSILON : point.y, point.z-Constants.EPSILON);
						bool hit = Physics.Raycast (origin, Vector3.forward, out hitInfo);
						if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform.position)) &&
							(hitInfo.distance <= Constants.EPSILON * 3)) {
							Debug.Log (string.Format ("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
							ec = true;
						}
					}
				}
				else if (x.Center.z >= y.Max(MajorAxis.X).z) {
					foreach (Vector3 point in x.Points.Where(p => p.z <= x.Center.z).ToList()) {
						RaycastHit hitInfo;
						Vector3 origin = new Vector3 (point.x == x.Min (MajorAxis.X).x ? point.x + Constants.EPSILON : point.x == x.Max (MajorAxis.X).x ? point.x - Constants.EPSILON : point.x,
							point.y == x.Min (MajorAxis.Y).y ? point.y + Constants.EPSILON : point.y == x.Max (MajorAxis.Y).y ? point.y - Constants.EPSILON : point.y, point.z+Constants.EPSILON);
						bool hit = Physics.Raycast (origin, -Vector3.forward, out hitInfo);
						if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform.position)) &&
							(hitInfo.distance <= Constants.EPSILON * 3)) {
							Debug.Log (string.Format ("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
							ec = true;
						}
					}
				}
//			}

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

