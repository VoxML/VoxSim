using UnityEngine;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using MajorAxes;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;

// RCC8 relations
// grossly underspecified for now
namespace VoxSimPlatform {
    namespace SpatialReasoning {
        namespace QSR {
            public static class RCC8 {
                // disconnected
                public static bool DC(Bounds x, Bounds y) {
                    bool dc = false;

                    if (!EC(x, y) && !PO(x, y) && !EQ(x, y) &&
                        !TPP(x, y) && !NTPP(x, y) && !TPPi(x, y) && !NTPPi(x, y)) {
                        dc = true;
                    }

                    return dc;
                }

                // externally connected
                public static bool EC(Bounds x, Bounds y) {
                    bool ec = false;

                    // if y and z dimensions overlap
                    if (Mathf.Abs(x.center.y - y.center.y) * 2 < (x.size.y + y.size.y) &&
                        (Mathf.Abs(x.center.z - y.center.z) * 2 < (x.size.z + y.size.z))) {
                        if ((Mathf.Abs(x.min.x - y.max.x) < Constants.EPSILON * 2) || // if touching on x
                            (Mathf.Abs(x.max.x - y.min.x) < Constants.EPSILON * 2)) {
                            ec = true;
                        }
                        else {
                            Debug.Log(Mathf.Abs(x.min.x - y.max.x));
                            Debug.Log(Mathf.Abs(x.max.x - y.min.x));
                        }
                    }

                    // if x and z dimensions overlap
                    if (Mathf.Abs(x.center.x - y.center.x) * 2 < (x.size.x + y.size.x) &&
                        (Mathf.Abs(x.center.z - y.center.z) * 2 < (x.size.z + y.size.z))) {
                        Debug.Log(x.min.y);
                        Debug.Log(y.max.y);
                        if ((Mathf.Abs(x.min.y - y.max.y) < Constants.EPSILON * 2) || // if touching on y
                            (Mathf.Abs(x.max.y - y.min.y) < Constants.EPSILON * 2)) {
                            ec = true;
                        }
                    }

                    // if x and y dimensions overlap
                    if (Mathf.Abs(x.center.x - y.center.x) * 2 < (x.size.x + y.size.x) &&
                        (Mathf.Abs(x.center.y - y.center.y) * 2 < (x.size.y + y.size.y))) {
                        if ((Mathf.Abs(x.min.z - y.max.z) < Constants.EPSILON * 2) || // if touching on z
                            (Mathf.Abs(x.max.z - y.min.z) < Constants.EPSILON * 2)) {
                            ec = true;
                        }
                    }

                    return ec;
                }

                public static bool EC(ObjBounds x, ObjBounds y) {
                    bool ec = false;

        //            if (Mathf.Abs (x.Center.y - y.Center.y) * 2 < ((x.Max(MajorAxis.Y).y-x.Center.y)*2 + (y.Max(MajorAxis.Y).y-y.Center.y)*2) &&
        //                (Mathf.Abs (x.Center.z - y.Center.z) * 2 < ((x.Max(MajorAxis.Z).z-x.Center.z)*2 + (y.Max(MajorAxis.Z).z-y.Center.z)*2))) {
                    if (x.Center.x <= y.Max(MajorAxis.X).x) {
        //                Debug.Log (Helper.VectorToParsable(x.Center));
        //                Debug.Log (Helper.VectorToParsable(y.Center));
                        foreach (Vector3 point in x.Points.Where(p => p.x >= x.Center.x).ToList()) {
        //                        Debug.Log (Helper.VectorToParsable (point));
                            RaycastHit hitInfo;
                            Vector3 origin = new Vector3(point.x - Constants.EPSILON,
                                (Mathf.Abs(point.y - x.Min(MajorAxis.Y).y) <= Constants.EPSILON) ? point.y + Constants.EPSILON :
                                (Mathf.Abs(point.y - x.Max(MajorAxis.Y).y) <= Constants.EPSILON) ? point.y - Constants.EPSILON :
                                point.y,
                                (Mathf.Abs(point.z - x.Min(MajorAxis.Z).z) <= Constants.EPSILON) ? point.x + Constants.EPSILON :
                                (Mathf.Abs(point.z - x.Max(MajorAxis.Z).z) <= Constants.EPSILON) ? point.z - Constants.EPSILON :
                                point.z);
                            bool hit = Physics.Raycast(origin, Vector3.right, out hitInfo);
                            if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform
                                    .position)) &&
                                (hitInfo.distance <= Constants.EPSILON * 3)) {
                                Debug.Log(string.Format("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
                                ec = true;
                            }
                        }
                    }
                    else if (x.Center.x >= y.Max(MajorAxis.X).x) {
                        foreach (Vector3 point in x.Points.Where(p => p.x <= x.Center.x).ToList()) {
                            RaycastHit hitInfo;
                            Vector3 origin = new Vector3(point.x + Constants.EPSILON,
                                (Mathf.Abs(point.y - x.Min(MajorAxis.Y).y) <= Constants.EPSILON) ? point.y + Constants.EPSILON :
                                (Mathf.Abs(point.y - x.Max(MajorAxis.Y).y) <= Constants.EPSILON) ? point.y - Constants.EPSILON :
                                point.y,
                                (Mathf.Abs(point.z - x.Min(MajorAxis.Z).z) <= Constants.EPSILON) ? point.x + Constants.EPSILON :
                                (Mathf.Abs(point.z - x.Max(MajorAxis.Z).z) <= Constants.EPSILON) ? point.z - Constants.EPSILON :
                                point.z);
                            bool hit = Physics.Raycast(origin, -Vector3.right, out hitInfo);
                            if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform
                                    .position)) &&
                                (hitInfo.distance <= Constants.EPSILON * 3)) {
                                Debug.Log(string.Format("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
                                ec = true;
                            }
                        }
                    }
        //            }

        //            if (Mathf.Abs (x.Center.x - y.Center.x) * 2 < ((x.Max (MajorAxis.X).x - x.Center.x) * 2 + (y.Max (MajorAxis.X).x - y.Center.x) * 2) &&
        //                (Mathf.Abs (x.Center.z - y.Center.z) * 2 < ((x.Max (MajorAxis.Z).z - x.Center.z) * 2 + (y.Max (MajorAxis.Z).z - y.Center.z) * 2))) {
                    //if (x.Center.x <= y.Center.x) {
                    if (x.Center.y <= y.Min(MajorAxis.Y).y) {
                        Debug.Log(Helper.VectorToParsable(x.Center));
                        Debug.Log(Helper.VectorToParsable(y.Center));
                        foreach (Vector3 point in x.Points.Where(p => p.y >= x.Center.y).ToList()) {
                            RaycastHit hitInfo;
                            Vector3 origin = new Vector3((Mathf.Abs(point.x - x.Min(MajorAxis.X).x) <= Constants.EPSILON)
                                    ? point.x + Constants.EPSILON
                                    : (Mathf.Abs(point.x - x.Max(MajorAxis.X).x) <= Constants.EPSILON)
                                        ? point.x - Constants.EPSILON
                                        : point.x,
                                point.y - Constants.EPSILON, (Mathf.Abs(point.z - x.Min(MajorAxis.Z).z) <= Constants.EPSILON)
                                    ? point.x + Constants.EPSILON
                                    : (Mathf.Abs(point.z - x.Max(MajorAxis.Z).z) <= Constants.EPSILON)
                                        ? point.z - Constants.EPSILON
                                        : point.z);
                            bool hit = Physics.Raycast(origin, Vector3.up, out hitInfo);
        //                            if (y.Contains (Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject).transform.position)) {
        //                                Debug.Log (hitInfo.collider.gameObject);
        //                                Debug.Log (hitInfo.distance);
        //                                Debug.Log (Helper.VectorToParsable(hitInfo.collider.gameObject.transform.position));
        //                                Debug.Log (Helper.VectorToParsable (Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject).transform.position));
        //                            }
                            if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform
                                    .position)) &&
                                (hitInfo.distance <= Constants.EPSILON * 3)) {
                                Debug.Log(string.Format("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
                                ec = true;
                            }
                        }
                    }
                    else if (x.Center.y >= y.Max(MajorAxis.Y).y) {
                        Debug.Log(Helper.VectorToParsable(x.Center));
                        Debug.Log(Helper.VectorToParsable(y.Center));
                        foreach (Vector3 point in x.Points.Where(p => p.y <= x.Center.y).ToList()) {
                            Debug.Log(Helper.VectorToParsable(point));
                            RaycastHit hitInfo;
                            Vector3 origin = new Vector3((Mathf.Abs(point.x - x.Min(MajorAxis.X).x) <= Constants.EPSILON)
                                    ? point.x + Constants.EPSILON
                                    : (Mathf.Abs(point.x - x.Max(MajorAxis.X).x) <= Constants.EPSILON)
                                        ? point.x - Constants.EPSILON
                                        : point.x,
                                point.y + Constants.EPSILON, (Mathf.Abs(point.z - x.Min(MajorAxis.Z).z) <= Constants.EPSILON)
                                    ? point.x + Constants.EPSILON
                                    : (Mathf.Abs(point.z - x.Max(MajorAxis.Z).z) <= Constants.EPSILON)
                                        ? point.z - Constants.EPSILON
                                        : point.z);
                            bool hit = Physics.Raycast(origin, -Vector3.up, out hitInfo);
                            if (hit) {
                                Debug.Log(hitInfo.collider.gameObject);
                            }

                            //if (y.Contains (Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject).transform.position)) {
                            //    Debug.Log (hitInfo.collider.gameObject);
                            //    Debug.Log (hitInfo.distance);
                            //    Debug.Log (Helper.VectorToParsable(hitInfo.collider.gameObject.transform.position));
                            //    Debug.Log (Helper.VectorToParsable (Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject).transform.position));
                            //}
                            if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform
                                    .position)) &&
                                (hitInfo.distance <= Constants.EPSILON * 3)) {
                                Debug.Log(string.Format("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
                                ec = true;
                            }
                        }
                    }
                    //}
        //            }

        //            if (Mathf.Abs (x.Center.x - y.Center.x) * 2 < ((x.Max (MajorAxis.X).x - x.Center.x) * 2 + (y.Max (MajorAxis.X).x - y.Center.x) * 2) &&
        //                (Mathf.Abs (x.Center.y - y.Center.y) * 2 < ((x.Max (MajorAxis.Y).y - x.Center.y) * 2 + (y.Max (MajorAxis.Y).y - y.Center.y) * 2))) {
                    if (x.Center.z <= y.Min(MajorAxis.Z).z) {
                        foreach (Vector3 point in x.Points.Where(p => p.z >= x.Center.z).ToList()) {
                            RaycastHit hitInfo;
                            Vector3 origin = new Vector3((Mathf.Abs(point.x - x.Min(MajorAxis.X).x) <= Constants.EPSILON)
                                    ? point.x + Constants.EPSILON
                                    : (Mathf.Abs(point.x - x.Max(MajorAxis.X).x) <= Constants.EPSILON)
                                        ? point.x - Constants.EPSILON
                                        : point.x,
                                (Mathf.Abs(point.y - x.Min(MajorAxis.Y).y) <= Constants.EPSILON) ? point.y + Constants.EPSILON :
                                (Mathf.Abs(point.y - x.Max(MajorAxis.Y).y) <= Constants.EPSILON) ? point.y - Constants.EPSILON :
                                point.y, point.z - Constants.EPSILON);
                            bool hit = Physics.Raycast(origin, Vector3.forward, out hitInfo);
                            if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform
                                    .position)) &&
                                (hitInfo.distance <= Constants.EPSILON * 3)) {
                                Debug.Log(string.Format("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
                                ec = true;
                            }
                        }
                    }
                    else if (x.Center.z >= y.Max(MajorAxis.X).z) {
                        foreach (Vector3 point in x.Points.Where(p => p.z <= x.Center.z).ToList()) {
                            RaycastHit hitInfo;
                            Vector3 origin = new Vector3((Mathf.Abs(point.x - x.Min(MajorAxis.X).x) <= Constants.EPSILON)
                                    ? point.x + Constants.EPSILON
                                    : (Mathf.Abs(point.x - x.Max(MajorAxis.X).x) <= Constants.EPSILON)
                                        ? point.x - Constants.EPSILON
                                        : point.x,
                                (Mathf.Abs(point.y - x.Min(MajorAxis.Y).y) <= Constants.EPSILON) ? point.y + Constants.EPSILON :
                                (Mathf.Abs(point.y - x.Max(MajorAxis.Y).y) <= Constants.EPSILON) ? point.y - Constants.EPSILON :
                                point.y, point.z + Constants.EPSILON);
                            bool hit = Physics.Raycast(origin, -Vector3.forward, out hitInfo);
                            if ((hit) && (y.Contains(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).transform
                                    .position)) &&
                                (hitInfo.distance <= Constants.EPSILON * 3)) {
                                Debug.Log(string.Format("{0}:{1}", hitInfo.collider.gameObject, hitInfo.distance));
                                ec = true;
                            }
                        }
                    }
        //            }

                    return ec;
                }

                public static Vector3 EC(ObjBounds y, params object[] constraints) {
                    Vector3 ec = Vector3.zero;

                    // EC is underspecified
                    // assumptions: 1) x will be aligned with y on all non-constrained axes
                    //  2) underspecification in relative orientation will be randomly selected
                    //  (or chosen by model if one exists and is specified)

                    // Todo: need a strategy for specifying underspecified relations
                    //  when exploiting the "functional" character of relational predicates
                    // Could base it on this? (from Predicates.ComposeRelation):
                    // // convert Vector3 to ObjBounds
                    // // assume the provided coordinates are the center of the bounds object
                    // // take the extents from the other object in the relation
                    // // (that is, the first object in relation of type ObjBounds,
                    // // or create ObjBounds of 0 extents if no ObjBounds to copy exists)

                    // 1) generate possible candidates
                    List<Vector3> candidates = new List<Vector3>();
                    candidates.Add(new Vector3(y.Min(MajorAxis.X).x,y.Center.y,y.Center.z));    // leftmost X point, aligned on Y and Z
                    candidates.Add(new Vector3(y.Max(MajorAxis.X).x,y.Center.y,y.Center.z));    // rightmost X point, aligned on Y and Z
                    candidates.Add(new Vector3(y.Center.x,y.Min(MajorAxis.Y).y,y.Center.z));    // bottommost Y point, aligned on X and Z
                    candidates.Add(new Vector3(y.Center.x,y.Max(MajorAxis.Y).y,y.Center.z));    // topmost Y point, aligned on X and Z
                    candidates.Add(new Vector3(y.Center.x,y.Center.y,y.Min(MajorAxis.Z).z));    // rearmost Z point, aligned on X and Y
                    candidates.Add(new Vector3(y.Center.x,y.Center.y,y.Max(MajorAxis.Z).z));    // frontmost Z point, aligned on X and Y

                    Debug.Log(string.Format("RCC8.EC: {0} candidates: [{1}]", candidates.Count,
                        string.Join(", ", candidates.Select(c => Helper.VectorToParsable(c)))));

                    // 2) prune candidates that don't satisfy all constraints
                    //  including global standing constraints like "this candidate location is blocked by another object"

                    ObjectSelector objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
                    Predicates preds = GameObject.Find("BehaviorController").GetComponent<Predicates>();
                    EventManager em = GameObject.Find("BehaviorController").GetComponent<EventManager>();
                    List<Vector3> pruneCandidates = new List<Vector3>();

                    foreach (Vector3 candidate in candidates) {
                        foreach (Voxeme voxeme in objSelector.allVoxemes) {
                            if (!y.BoundsEqual(Helper.GetObjectOrientedSize(voxeme.gameObject, true))) {
                                Bounds testBounds = new Bounds(Helper.GetObjectWorldSize(voxeme.gameObject).center,
                                    new Vector3(Helper.GetObjectWorldSize(voxeme.gameObject, true).size.x + 2 * Constants.EPSILON,
                                        Helper.GetObjectWorldSize(voxeme.gameObject, true).size.y + 2 * Constants.EPSILON,
                                        Helper.GetObjectWorldSize(voxeme.gameObject, true).size.z + 2 * Constants.EPSILON));
                                if (testBounds.Contains(candidate)) {
                                    Debug.Log(string.Format("Adding {0} to prune list (intersects bounds({1}))",
                                        Helper.VectorToParsable(candidate), voxeme.name));
                                    pruneCandidates.Add(candidate);
                                }
                            }
                        }

                        List<string> evaluatedConstraints = new List<string>(constraints.Length);

                        for (int i = 0; i < constraints.Length; i++) {
                            if (constraints[i] is string) {
                                string constraintForm = ((string)constraints[i]).Replace("x", Helper.VectorToParsable(candidate)).
                                        Replace("y", Helper.VectorToParsable(y.Center));
                                //Debug.Log(constraintForm);

                                //string[] operators = new string[] { "<", "<=", "=", "!=", ">=", ">", "^", "|" };
                                Regex operators = new Regex(@"(?<![()])\w?([<>!]=?|[\^|=])\w?(?![()])");

                                string[] constraintValues = operators.Split(constraintForm).Select(c => c.Trim()).ToArray();

                                foreach (string value in constraintValues) {
                                    if (Helper.pred.IsMatch(value)) {
                                        List<object> objs = em.ExtractObjects(Helper.GetTopPredicate(value),
                                                (string)Helper.ParsePredicate(value)[Helper.GetTopPredicate(value)]);

                                        MethodInfo methodToCall = preds.GetType().GetMethod(Helper.GetTopPredicate(value));
                                        
                                        if (methodToCall != null) {
                                            object result = methodToCall.Invoke(preds, new object[]{ objs.ToArray() });
                                            //Debug.Log(value);
                                            //Debug.Log(result);

                                            constraintForm = constraintForm.Replace(value, ((float)result).ToString());
                                        }
                                    }
                                }

                                evaluatedConstraints.Add(constraintForm);
                            }
                        }

                        foreach (string eval in evaluatedConstraints) {
                            string expression = eval.Replace("^", " AND ").Replace("|", " OR ");
                            DataTable dt = new DataTable();
                            bool result = (bool)dt.Compute(expression, null);
                            Debug.Log(string.Format("Result of {0}: {1}", expression, result));

                            if ((!result) && (!pruneCandidates.Contains(candidate))) {
                                Debug.Log(string.Format("Adding {0} to prune list (violates {1})",
                                    Helper.VectorToParsable(candidate), eval));
                                pruneCandidates.Add(candidate);
                            }
                        }
                    }

                    candidates = candidates.Except(pruneCandidates).ToList(); 

                    Debug.Log(string.Format("RCC8.EC: {0} candidates: [{1}]", candidates.Count,
                        string.Join(", ", candidates.Select(c => Helper.VectorToParsable(c)))));

                    // 3) random/model-derived assignment from remaining choices (if > 1)
                    //  if there's only one, this will return that one

                    ec = candidates[RandomHelper.RandomInt(0, candidates.Count)];

                    return ec;
                }

                public static bool PO(Bounds x, Bounds y) {
                    bool po = false;

                    if (x.Intersects(y)) {
                        float xOff =
                            Mathf.Abs(Mathf.Abs((x.center - y.center).x) - Mathf.Abs(((x.size.x / 2) + (y.size.x / 2))));
                        float yOff =
                            Mathf.Abs(Mathf.Abs((x.center - y.center).y) - Mathf.Abs(((x.size.y / 2) + (y.size.y / 2))));
                        float zOff =
                            Mathf.Abs(Mathf.Abs((x.center - y.center).z) - Mathf.Abs(((x.size.z / 2) + (y.size.z / 2))));
                        // intersects but not too much, system is a little fuzzy
                        if ((xOff > Constants.EPSILON) && (yOff > Constants.EPSILON) && (zOff > Constants.EPSILON)) {
                            po = true;
                        }
                    }

                    return po;
                }

                public static bool EQ(Bounds x, Bounds y) {
                    bool eq = false;

                    if (((x.min - y.min).magnitude < Constants.EPSILON) && ((x.max - y.max).magnitude < Constants.EPSILON)) {
                        eq = true;
                    }

                    return eq;
                }

                public static bool EQ(ObjBounds x, ObjBounds y) {
                    bool eq = true;

                    for (int i = 0; i < x.Points.Count; i++) {
                        if ((x.Points[i]-y.Points[i]).magnitude > Constants.EPSILON) {
                            eq = false;
                        }
                    }

                    return eq;
                }

                public static bool TPP(Bounds x, Bounds y) {
                    bool tpp = false;

                    if (y.Contains(x.min) && y.Contains(x.max)) {
                        if ((Mathf.Abs(x.min.x - y.min.x) < Constants.EPSILON) ||
                            (Mathf.Abs(x.max.x - y.max.x) < Constants.EPSILON) ||
                            (Mathf.Abs(x.min.y - y.min.y) < Constants.EPSILON) ||
                            (Mathf.Abs(x.max.y - y.max.y) < Constants.EPSILON) ||
                            (Mathf.Abs(x.min.z - y.min.z) < Constants.EPSILON) ||
                            (Mathf.Abs(x.max.z - y.max.z) < Constants.EPSILON))
                            tpp = true;
                    }

                    return tpp;
                }

                public static bool NTPP(Bounds x, Bounds y) {
                    bool ntpp = false;

                    if (!TPP(x, y)) {
                        if (y.Contains(x.min) && y.Contains(x.max)) {
                            ntpp = true;
                        }
                    }

                    return ntpp;
                }

                public static bool TPPi(Bounds x, Bounds y) {
                    bool tppi = false;

                    if (TPP(y, x)) {
                        tppi = true;
                    }

                    return tppi;
                }

                public static bool NTPPi(Bounds x, Bounds y) {
                    bool ntppi = false;

                    if (NTPP(y, x)) {
                        ntppi = true;
                    }

                    return ntppi;
                }

                public static bool ProperPart(Bounds x, Bounds y) {
                    bool properpart = false;

                    if (TPP(x, y) || NTPP(x, y)) {
                        properpart = true;
                    }

                    return properpart;
                }
            }
        }
    }
}