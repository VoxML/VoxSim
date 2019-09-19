using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

using MajorAxes;
using Random = UnityEngine.Random;
using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Core;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace Global {
        /// <summary>
        /// Helper class
        /// </summary>
        public static class Helper {
            public static Regex vec = new Regex(@"<.*>");               // vector form regex
            public static Regex listVec = new Regex(@"\[<.*>\]");       // list of vectors form regex
            public static Regex commaVec = new Regex(@",<.*>");         // comma + vector form regex
            public static Regex emptyList = new Regex(@"\[\]");         // empty list form regex (not really a regex but just doing this for consistency
            public static Regex quoted = new Regex("[\'\"].*[\'\"]");   // quoted form regex
            public static Regex pred = new Regex(@"[^(]+\(.+\)");       // predicate form regex

            // DATA METHODS
            // IN: String: predicateString representing the entire predicate-argument format of the input
            // OUT: Hashtable of format { predicate : argument }
            public static Hashtable ParsePredicate(String predicateString) {
                Hashtable predArgs = new Hashtable();

                // split predicateString on open paren, returning a maximum of two substrings
                List<String> split =
                    new List<String>(predicateString.Split(new char[] {'('}, 2, StringSplitOptions.None));
                if (split.Count > 1) {
                    String pred = split.ElementAt(0);   // portion before (
                    String args = split.ElementAt(1);   // portion after (
                    // remove the close paren from the end of args
                    //  this is the close paren corresponding to the open paren on which we split the string
                    args = args.Remove(args.Length - 1);
                    predArgs.Add(pred, args);   // add to hashtable
                }

                return predArgs;
            }

            public static String GetTopPredicate(String formula) {
                List<String> split = new List<String>(formula.Split(new char[] {'('}, 2, StringSplitOptions.None));
                return split.ElementAt(0);
            }

            public static void PrintKeysAndValues(string name, Hashtable ht) {
                if (ht.Count == 0) {
                    return;
                }

                List<string> output = new List<string>();
                foreach (DictionaryEntry entry in ht) {
                    output.Add(entry.Key + " : " + entry.Value);
                }

                Debug.Log(name + ": { " + string.Join(", ", output.ToArray()) + " }");
            }

            public static String VectorToParsable(Vector3 vector) {
                return ("<" + vector.x + "; " +
                        vector.y + "; " +
                        vector.z + ">");
            }

            public static Vector3 ParsableToVector(String parsable) {
                Vector3 retVal = new Vector3();
                List<String> components =
                    new List<String>((parsable.Replace("<", "").Replace(">", "")).Split(';'));
                try {
                    retVal = new Vector3(Convert.ToSingle(components[0]),
                        Convert.ToSingle(components[1]),
                        Convert.ToSingle(components[2]));
                }
                catch (Exception ex) {
                    if (ex is FormatException) {
                        Debug.LogError(string.Format("Input string \"{0}\" was not in a correct format", parsable));
                    }
                    throw ex;
                }

                return retVal; 
            }

            public static String QuaternionToParsable(Quaternion quat) {
                return ("<" + quat.x + "; " +
                        quat.y + "; " +
                        quat.z + "; " +
                        quat.w + ">");
            }

            public static Quaternion ParsableToQuaternion(String parsable) {
                List<String> components =
                    new List<String>((parsable.Replace("<", "").Replace(">", "")).Split(';'));
                return new Quaternion(Convert.ToSingle(components[0]),
                    Convert.ToSingle(components[1]),
                    Convert.ToSingle(components[2]),
                    Convert.ToSingle(components[3]));
            }

            public static String IntToString(int inInt) {
                return Convert.ToString(inInt);
            }

            public static int StringToInt(String inString) {
                return Convert.ToInt32(inString);
            }

            public static List<int> FindAllIndicesOf(this string str, string value) {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentException("String to find may not be empty", nameof(value));
                List<int> indexes = new List<int>();
                for (int index = 0;; index += value.Length) {
                    index = str.IndexOf(value, index);
                    if (index == -1)
                        return indexes;
                    indexes.Add(index);
                }
            }

            public static string ReplaceFirst(this string text, string search, string replace) {
                int pos = text.IndexOf(search);
                if (pos < 0) {
                    return text;
                }
                return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
            }

            public static string ReplaceFirstStartingAt(this string text, int startIndex, string search, string replace) {
                int pos = text.Substring(startIndex, text.Length-startIndex).IndexOf(search);
                if (pos < 0) {
                    return text;
                }
                return text.Substring(0, startIndex + pos) + replace + text.Substring(startIndex + pos + search.Length);
            }

            public static Triple<String, String, String> MakeRDFTriples(String formula) {
                // fix for multiple RDF triples
                Triple<String, String, String> triple = new Triple<String, String, String>("", "", "");
                Debug.Log("MakeRDFTriple: " + formula);
                formula = commaVec.Replace(formula, "");
                String[] components = formula.Replace('(', '/').Replace(')', '/').Replace(',', '/').Split('/');

        //          //Debug.Log (components.Length);
        //          foreach (String s in components) {
        //              Debug.Log (s);
        //          }
                //Debug.Break ();

                if (components.Length <= 3) {
                    triple.Item1 = "NULL";
                }

                foreach (String s in components) {
                    if (s.Length > 0) {
                        //Debug.Log (s);
                        GameObject obj = GameObject.Find(s);
                        if (obj != null) {
                            if (components[0] == "def") {
                                if (triple.Item1 == "") {
                                    triple.Item1 = s;
                                }
                            }
                            else {
                                if (triple.Item1 == "") {
                                    triple.Item1 = s;
                                }
                                else if (triple.Item3 == "") {
                                    triple.Item3 = s;
                                }
                            }
                        }
                        else {
                            if (components[0] == "repeat") {
                                int i;
                                if (int.TryParse(s, out i)) {
                                    if (triple.Item3 == "") {
                                        triple.Item3 = s;
                                    }
                                }
                                else if (quoted.IsMatch(s)) {
                                    if (triple.Item1 == "") {
                                        triple.Item1 = s;
                                    }
                                }
                                else {
                                    triple.Item2 = triple.Item2 + s + "_";
                                }
                            }
                            else if (vec.IsMatch(s)) {
                                if (triple.Item3 == "") {
                                    triple.Item3 = s;
                                }
                            }
                            else if (quoted.IsMatch(s)) {
                                if (triple.Item3 == "") {
                                    triple.Item3 = s;
                                }
                            }
                            else {
                                triple.Item2 = triple.Item2 + s + "_";
                            }
                        }
                    }
                }

                if (triple.Item2.Length > 0) {
                    triple.Item2 = triple.Item2.Remove(triple.Item2.Length - 1, 1);
                }

                Debug.Log(triple.Item1 + " " + triple.Item2 + " " + triple.Item3);
                return triple;
            }

            public static void PrintRDFTriples(List<Triple<String, String, String>> triples) {
                if (triples.Count == 0) {
                    Debug.Log("No RDF triples to print");
                }

                foreach (Triple<String, String, String> triple in triples) {
                    Debug.Log(triple.Item1 + " " + triple.Item2 + " " + triple.Item3);
                }
            }

            public static Hashtable DiffHashtables(Hashtable baseline, Hashtable comparand) {
                Hashtable diff = new Hashtable();

                foreach (DictionaryEntry entry in comparand) {
                    if (!baseline.ContainsKey(entry.Key)) {
                        object key = entry.Key;
                        if (entry.Key is List<object>) {
                            key = string.Join(",", ((List<object>) entry.Value).Cast<string>().ToArray());
                        }

                        object value = entry.Value;
                        if (entry.Value is List<object>) {
                            value = string.Join(",", ((List<object>) entry.Value).Cast<string>().ToArray());
                        }

                        diff.Add(key, value);
                    }
                    else if (baseline[entry.Key] != entry.Value) {
                        object key = entry.Key;
                        if (entry.Key is List<object>) {
                            key = string.Join(",", ((List<object>) entry.Key).Cast<string>().ToArray());
                        }

                        object value = entry.Value;
                        if (entry.Value is List<object>) {
                            value = string.Join(",", ((List<object>) entry.Value).Cast<string>().ToArray());
                        }

                        diff.Add(key, value);
                    }
                }

                return diff;
            }

            public static string HashtableToString(Hashtable ht) {
                string output = string.Empty;
                foreach (DictionaryEntry entry in ht) {
                    output += string.Format("{0} {1};", entry.Key, entry.Value);
                }

                return output;
            }

            public static string PrintByteArray(byte[] bytes) {
                var sb = new StringBuilder("new byte[] { ");
                foreach (var b in bytes) {
                    sb.Append(b + ", ");
                }

                sb.Append("}");

                return sb.ToString();
            }

            public static List<object> DiffLists(List<object> baseline, List<object> comparand) {
                return comparand.Except(baseline).ToList();
            }

            public static byte[] SerializeObjectToBinary(object obj) {
                BinaryFormatter binFormatter = new BinaryFormatter();
                MemoryStream mStream = new MemoryStream();
                binFormatter.Serialize(mStream, obj);

                //This gives you the byte array.
                return mStream.ToArray();
            }

            public static T DeserializeObjectFromBinary<T>(byte[] bytes) {
                MemoryStream mStream = new MemoryStream();
                BinaryFormatter binFormatter = new BinaryFormatter();

                // Where 'bytes' is your byte array.
                mStream.Write(bytes, 0, bytes.Length);
                mStream.Position = 0;

                return (T) binFormatter.Deserialize(mStream);
            }

            public static string SerializeObjectToJSON(object obj) {
                string json = JsonUtility.ToJson(obj);
        //          Debug.Log (json);
        //          Debug.Break ();

                return json;
            }

            public static bool CheckAllObjectsOfType(object[] array, Type type) {
                bool consistent = true;

                if (array != null) {
                    foreach (object element in array) {
                        if (element != null) {
                            consistent &= (element.GetType() == type);
                        }
                    }
                }

                return consistent;
            }

            // VECTOR METHODS
            public static bool VectorIsNaN(Vector3 vec) {
                return (float.IsNaN(vec.x) || float.IsNaN(vec.y) || float.IsNaN(vec.z));
            }

            public static bool PointInRect(Vector2 point, Rect rect) {
                return (point.x >= rect.xMin && point.x <= rect.xMax && point.y >= rect.yMin && point.y <= rect.yMax);
            }

            public static bool PointOutsideMaskedAreas(Vector2 point, Rect[] rects) {
                bool outside = true;

                foreach (Rect r in rects) {
                    if (PointInRect(point, r)) {
                        outside = false;
                    }
                }

                return outside;
            }

            public static Vector3 RayIntersectionPoint(Vector3 rayStart, Vector3 direction) {
                //Collider[] colliders = obj.GetComponentsInChildren<Collider> ();
                List<RaycastHit> hits = new List<RaycastHit>();

                //foreach (Collider c in colliders) {
                RaycastHit hitInfo = new RaycastHit();
                Physics.Raycast(rayStart, direction.normalized, out hitInfo);
                hits.Add(hitInfo);
                //}

                RaycastHit closestHit = hits[0];

                foreach (RaycastHit hit in hits) {
                    if (hit.distance < closestHit.distance) {
                        closestHit = hit;
                    }
                }

                return closestHit.point;
            }

            public static bool Parallel(Vector3 v1, Vector3 v2) {
                return (1 - Vector3.Dot(v1, v2) < Constants.EPSILON * 10);
            }

            public static bool Perpendicular(Vector3 v1, Vector3 v2) {
                return (Mathf.Abs(Vector3.Dot(v1, v2)) < Constants.EPSILON * 10);
            }

            public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 eulerAngles) {
                Vector3 dir = point - pivot; // get point direction relative to pivot
                dir = Quaternion.Euler(eulerAngles) * dir; // rotate it
                point = dir + pivot; // calculate rotated point
                return point; // return it
            }

            // OBJECT METHODS
            // returns origin-centered object bounds
            public static Bounds GetObjectSize(GameObject obj) {
                MeshFilter[] meshes = obj.GetComponentsInChildren<MeshFilter>();

                Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);

                foreach (MeshFilter mesh in meshes) {
                    Bounds temp = new Bounds(Vector3.zero, mesh.mesh.bounds.size);
                    //Debug.Log(mesh.gameObject.name);
                    //Debug.Log(temp);
                    Vector3 min = new Vector3(temp.min.x * mesh.gameObject.transform.lossyScale.x,
                        temp.min.y * mesh.gameObject.transform.lossyScale.y,
                        temp.min.z * mesh.gameObject.transform.lossyScale.z);
                    Vector3 max = new Vector3(temp.max.x * mesh.gameObject.transform.lossyScale.x,
                        temp.max.y * mesh.gameObject.transform.lossyScale.y,
                        temp.max.z * mesh.gameObject.transform.lossyScale.z);
                    //Debug.Log (Helper.VectorToParsable(min));
                    //Debug.Log (Helper.VectorToParsable(max));
                    //Debug.Log(mesh.gameObject.transform.root.GetChild(0).localScale);
                    //Debug.Log(mesh.gameObject.name);
                    //Debug.Log(mesh.gameObject.transform.localEulerAngles);
                    //temp.center = obj.transform.position;
                    //temp.SetMinMax (min,max);
                    temp.SetMinMax(RotatePointAroundPivot(min, Vector3.zero, mesh.gameObject.transform.localEulerAngles),
                        RotatePointAroundPivot(max, Vector3.zero, mesh.gameObject.transform.localEulerAngles));
                    //Debug.Log (temp);
                    //Debug.Log (combinedBounds);
                    //Debug.Log (temp);
                    combinedBounds.Encapsulate(temp);
                    //Debug.Log (combinedBounds);
                }

                /*combinedBounds = new Bounds (obj.transform.position, Vector3.zero);
                Bounds bounds = GetObjectWorldSize (obj);

                Debug.Log (obj.transform.eulerAngles);
                Quaternion invRot = Quaternion.Inverse (obj.transform.rotation);
                Debug.Log (invRot.eulerAngles);
                Debug.Log (Helper.VectorToParsable(bounds.min));
                Debug.Log (Helper.VectorToParsable(bounds.max));
                Vector3 bmin = RotatePointAroundPivot (bounds.min, obj.transform.position, invRot.eulerAngles);
                Vector3 bmax = RotatePointAroundPivot (bounds.max, obj.transform.position, invRot.eulerAngles);
                Debug.Log (Helper.VectorToParsable(bmin));
                Debug.Log (Helper.VectorToParsable(bmax));

                combinedBounds.Encapsulate(bmin);
                combinedBounds.Encapsulate(bmax);*/
                return combinedBounds;
            }

            // returns current position-centered object bounds
            public static ObjBounds GetObjectOrientedSize(GameObject obj, bool excludeChildren = false) {
                MeshFilter[] meshes = obj.GetComponentsInChildren<MeshFilter>();

                List<GameObject> exclude = new List<GameObject>();

                if (excludeChildren) {
                    // me: I hate computer scientists!  They never document their code properly!
                    // also me: I'm going to extract the information I need using this quadruply-embedded list comprehension
                    //          Debug.Log (obj1);
                    MeshFilter[] children = obj.GetComponentsInChildren<MeshFilter>().Where(
                        m => (GetMostImmediateParentVoxeme(m.gameObject) != obj) &&
                             (m.gameObject.GetComponent<Voxeme>() != null) &&
                             (!obj.GetComponent<Voxeme>().opVox.Type.Components.Select(
                                 c => c.Item2).ToList().Contains(m.gameObject))).ToArray();
                    foreach (MeshFilter mesh in children) {
                        Debug.Log(mesh.gameObject);
                        exclude.Add(mesh.gameObject);
                    }
                }

                Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);

                foreach (MeshFilter mesh in meshes) {
                    if (!exclude.Contains(mesh.gameObject)) {
                        Debug.Log(string.Format("{0}: Adding {1} to combined bounds", obj, mesh.gameObject));
                        // create a temp bounds of size of mesh, centered on origin
                        Bounds temp = new Bounds(Vector3.zero, mesh.mesh.bounds.size);
                        // scale it by object size
                        Vector3 min = new Vector3(temp.min.x * mesh.gameObject.transform.lossyScale.x,
                            temp.min.y * mesh.gameObject.transform.lossyScale.y,
                            temp.min.z * mesh.gameObject.transform.lossyScale.z);
                        min = RotatePointAroundPivot(min, temp.center, mesh.gameObject.transform.eulerAngles);
                        Vector3 max = new Vector3(temp.max.x * mesh.gameObject.transform.lossyScale.x,
                            temp.max.y * mesh.gameObject.transform.lossyScale.y,
                            temp.max.z * mesh.gameObject.transform.lossyScale.z);
                        max = RotatePointAroundPivot(max, temp.center, mesh.gameObject.transform.eulerAngles);
                        // set min and max
                        temp.SetMinMax(min, max);
                        // combined bounds = current combined bounds stretched to encapsulate this temp
                        combinedBounds.Encapsulate(temp);
                    }
                }

                //combinedBounds.SetMinMax(combinedBounds.center + obj.transform.position - combinedBounds.extents,
                //    combinedBounds.center + obj.transform.position + combinedBounds.extents);

                combinedBounds.SetMinMax(
                    combinedBounds.center + GetObjectWorldSize(obj).center - combinedBounds.extents,
                    combinedBounds.center + GetObjectWorldSize(obj).center + combinedBounds.extents);

                List<Vector3> pts = new List<Vector3>(new Vector3[] {
                    new Vector3(combinedBounds.min.x, combinedBounds.min.y, combinedBounds.min.z),
                    new Vector3(combinedBounds.min.x, combinedBounds.min.y, combinedBounds.max.z),
                    new Vector3(combinedBounds.min.x, combinedBounds.max.y, combinedBounds.min.z),
                    new Vector3(combinedBounds.min.x, combinedBounds.max.y, combinedBounds.max.z),
                    new Vector3(combinedBounds.max.x, combinedBounds.min.y, combinedBounds.min.z),
                    new Vector3(combinedBounds.max.x, combinedBounds.min.y, combinedBounds.max.z),
                    new Vector3(combinedBounds.max.x, combinedBounds.max.y, combinedBounds.min.z),
                    new Vector3(combinedBounds.max.x, combinedBounds.max.y, combinedBounds.max.z),
                    new Vector3(combinedBounds.min.x, combinedBounds.center.y, combinedBounds.center.z),
                    new Vector3(combinedBounds.max.x, combinedBounds.center.y, combinedBounds.center.z),
                    new Vector3(combinedBounds.center.x, combinedBounds.min.y, combinedBounds.center.z),
                    new Vector3(combinedBounds.center.x, combinedBounds.max.y, combinedBounds.center.z),
                    new Vector3(combinedBounds.center.x, combinedBounds.center.y, combinedBounds.min.z),
                    new Vector3(combinedBounds.center.x, combinedBounds.center.y, combinedBounds.max.z)
                });

                Bounds bounds = new Bounds(pts[0], Vector3.zero);
                ObjBounds objBounds = new ObjBounds(combinedBounds.center);
                List<Vector3> points = new List<Vector3>();
                foreach (Vector3 pt in pts) {
                    points.Add(pt);
                    bounds.Encapsulate(pt);
                }

                Debug.Log(VectorToParsable(bounds.size));

                objBounds.Points = new List<Vector3>(points);

                return objBounds;

                //ObjBounds objBounds = new ObjBounds(combinedBounds.center);
                //List<Vector3> points = new List<Vector3>();
                //foreach (Vector3 pt in pts) {
                //    points.Add(RotatePointAroundPivot(pt, objBounds.Center, obj.transform.eulerAngles));
                //}

                //objBounds.Points = new List<Vector3>(points);

                //return objBounds;
            }

            // returns current position-centered object bounds
            //public static ObjBounds GetObjectOrientedSize(GameObject obj, bool excludeChildren) {
            //    MeshFilter[] meshes = obj.GetComponentsInChildren<MeshFilter>();

            //    // me: I hate computer scientists!  They never document their code properly!
            //    // also me: I'm going to extract the information I need using this quadruply-embedded list comprehension
            //    //          Debug.Log (obj1);
            //    MeshFilter[] children = obj.GetComponentsInChildren<MeshFilter>().Where(
            //        m => (GetMostImmediateParentVoxeme(m.gameObject) != obj) &&
            //             (m.gameObject.GetComponent<Voxeme>() != null) &&
            //             (!obj.GetComponent<Voxeme>().opVox.Type.Components.Select(
            //                 c => c.Item2).ToList().Contains(m.gameObject))).ToArray();
            //    List<GameObject> exclude = new List<GameObject>();
            //    foreach (MeshFilter mesh in children) {
            //        Debug.Log(mesh.gameObject);
            //        exclude.Add(mesh.gameObject);
            //    }

            //    Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);

            //    foreach (MeshFilter mesh in meshes) {
            //        if (!exclude.Contains(mesh.gameObject)) {
            //            Debug.Log(string.Format("{0}: Adding {1} to combined bounds", obj, mesh.gameObject));
            //            // create a temp bounds of size of mesh, centered on origin
            //            Bounds temp = new Bounds(Vector3.zero, mesh.mesh.bounds.size);
            //            // scale it by object size
            //            Vector3 min = new Vector3(temp.min.x * mesh.gameObject.transform.lossyScale.x,
            //                temp.min.y * mesh.gameObject.transform.lossyScale.y,
            //                temp.min.z * mesh.gameObject.transform.lossyScale.z);
            //            min = RotatePointAroundPivot(min, temp.center, mesh.gameObject.transform.eulerAngles);
            //            Vector3 max = new Vector3(temp.max.x * mesh.gameObject.transform.lossyScale.x,
            //                temp.max.y * mesh.gameObject.transform.lossyScale.y,
            //                temp.max.z * mesh.gameObject.transform.lossyScale.z);
            //            max = RotatePointAroundPivot(max, temp.center, mesh.gameObject.transform.eulerAngles);
            //            // set min and max
            //            temp.SetMinMax(min, max);
            //            // combined bounds = current combined bounds stretched to encapsulate this temp
            //            combinedBounds.Encapsulate(temp);
            //        }
            //    }

            //    //Debug.Log(string.Format("center({0}):{1}", obj,Helper.VectorToParsable(combinedBounds.center)));
            //    //Debug.Log(string.Format("min({0}):{1}", obj, Helper.VectorToParsable(combinedBounds.min)));
            //    //Debug.Log(string.Format("max({0}):{1}", obj, Helper.VectorToParsable(combinedBounds.max)));
            //    combinedBounds.SetMinMax(
            //        combinedBounds.center + GetObjectWorldSize(obj).center - combinedBounds.extents,
            //        combinedBounds.center + GetObjectWorldSize(obj).center + combinedBounds.extents);
            //    //Debug.Log(string.Format("center({0}):{1}", obj, Helper.VectorToParsable(combinedBounds.center)));
            //    //Debug.Log(string.Format("min({0}):{1}", obj, Helper.VectorToParsable(combinedBounds.min)));
            //    //Debug.Log(string.Format("max({0}):{1}", obj, Helper.VectorToParsable(combinedBounds.max)));

            //    Debug.Log(VectorToParsable(combinedBounds.size));

            //    List<Vector3> pts = new List<Vector3>(new Vector3[] {
            //        new Vector3(combinedBounds.min.x, combinedBounds.min.y, combinedBounds.min.z),
            //        new Vector3(combinedBounds.min.x, combinedBounds.min.y, combinedBounds.max.z),
            //        new Vector3(combinedBounds.min.x, combinedBounds.max.y, combinedBounds.min.z),
            //        new Vector3(combinedBounds.min.x, combinedBounds.max.y, combinedBounds.max.z),
            //        new Vector3(combinedBounds.max.x, combinedBounds.min.y, combinedBounds.min.z),
            //        new Vector3(combinedBounds.max.x, combinedBounds.min.y, combinedBounds.max.z),
            //        new Vector3(combinedBounds.max.x, combinedBounds.max.y, combinedBounds.min.z),
            //        new Vector3(combinedBounds.max.x, combinedBounds.max.y, combinedBounds.max.z),
            //        new Vector3(combinedBounds.min.x, combinedBounds.center.y, combinedBounds.center.z),
            //        new Vector3(combinedBounds.max.x, combinedBounds.center.y, combinedBounds.center.z),
            //        new Vector3(combinedBounds.center.x, combinedBounds.min.y, combinedBounds.center.z),
            //        new Vector3(combinedBounds.center.x, combinedBounds.max.y, combinedBounds.center.z),
            //        new Vector3(combinedBounds.center.x, combinedBounds.center.y, combinedBounds.min.z),
            //        new Vector3(combinedBounds.center.x, combinedBounds.center.y, combinedBounds.max.z)
            //    });

            //    Bounds bounds = new Bounds(pts[0], Vector3.zero);
            //    ObjBounds objBounds = new ObjBounds(combinedBounds.center);
            //    //Debug.Log(string.Format("center({0}):{1}", obj, Helper.VectorToParsable(objBounds.Center)));
            //    List<Vector3> points = new List<Vector3>();
            //    foreach (Vector3 pt in pts) {
            //        //Debug.Log(string.Format("{0}:{1}", obj, Helper.VectorToParsable(pt)));
            //        //Debug.Log(string.Format("{0}:{1}", obj, Helper.VectorToParsable(RotatePointAroundPivot(pt, objBounds.Center, obj.transform.eulerAngles))));
            //        //points.Add (RotatePointAroundPivot (pt, objBounds.Center, obj.transform.eulerAngles));
            //        points.Add(pt);
            //        bounds.Encapsulate(pt);
            //    }

            //    Debug.Log(VectorToParsable(bounds.size));

            //    objBounds.Points = new List<Vector3>(points);

            //    return objBounds;
            //}

            // get the bounds of the object in the current world
            public static Bounds GetObjectWorldSize(GameObject obj, bool excludeChildren = false) {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

                List<GameObject> exclude = new List<GameObject>();

                if (excludeChildren) {
                    // me: I hate computer scientists!  They never document their code properly!
                    // also me: I'm going to extract the information I need using this quadruply-embedded list comprehension
                    //          Debug.Log (obj1);
                    MeshFilter[] children = obj.GetComponentsInChildren<MeshFilter>().Where(
                        m => (GetMostImmediateParentVoxeme(m.gameObject) != obj) &&
                             (m.gameObject.GetComponent<Voxeme>() != null) &&
                             (!obj.GetComponent<Voxeme>().opVox.Type.Components.Select(
                                 c => c.Item2).ToList().Contains(m.gameObject))).ToArray();
                    foreach (MeshFilter mesh in children) {
                        Debug.Log(mesh.gameObject);
                        exclude.Add(mesh.gameObject);
                    }
                }

                Bounds combinedBounds = new Bounds(obj.transform.position, Vector3.zero);

                foreach (Renderer renderer in renderers) {
                    if (!exclude.Contains(renderer.transform.gameObject)) {
                        combinedBounds.Encapsulate(renderer.bounds);
                    }
                }

                return combinedBounds;
            }

            // get the bounds of the object in the current world, excluding any children listed
            public static Bounds GetObjectWorldSize(GameObject obj, List<GameObject> exclude) {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

                Bounds combinedBounds = new Bounds(obj.transform.position, Vector3.zero);

                foreach (Renderer renderer in renderers) {
                    if (!exclude.Contains(renderer.transform.gameObject)) {
                        //Debug.Log (renderer.transform.gameObject.name);
                        combinedBounds.Encapsulate(renderer.bounds);
                    }
                }

                return combinedBounds;
            }


            // get the collective bounds of the objects in the current world
            public static Bounds GetObjectWorldSize(List<GameObject> objs) {
                Bounds combinedBounds = new Bounds(objs[0].transform.position, Vector3.zero);

                foreach (GameObject obj in objs) {
                    Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

                    foreach (Renderer renderer in renderers) {
                        combinedBounds.Encapsulate(renderer.bounds);
                    }
                }

                return combinedBounds;
            }

            // get the collective bounds of the objects in the current world
            public static Bounds GetPointsWorldSize(List<Vector3> pts) {
                Bounds combinedBounds = new Bounds(pts[0], Vector3.zero);

                foreach (Vector3 pt in pts) {
                    combinedBounds.Encapsulate(pt);
                }

                return combinedBounds;
            }

            public static Bounds GetObjectSize(object[] objs) {
                Bounds starterBounds = (objs[0] as GameObject).GetComponentsInChildren<BoxCollider>()[0].bounds;
                Bounds combinedBounds = starterBounds;
                //Debug.Log (combinedBounds.size);

                foreach (object obj in objs) {
                    if (obj is GameObject) {
                        //Debug.Log ((obj as GameObject).name);
                        Renderer[] renderers = (obj as GameObject).GetComponentsInChildren<Renderer>();

                        foreach (Renderer renderer in renderers) {
                            //Bounds bounds = new Bounds(renderer.bounds.center,
                            //                           renderer.bounds.size);
                            //localBounds.center = renderer.gameObject.transform.position;
                            //localBounds.size = new Vector3(localBounds.size.x * renderer.gameObject.transform.localScale.x,
                            //                               localBounds.size.y * renderer.gameObject.transform.localScale.y,
                            //                               localBounds.size.z * renderer.gameObject.transform.localScale.z);
                            combinedBounds.Encapsulate(renderer.bounds);

                            //Debug.Log (combinedBounds.center);
                            //Debug.Log (combinedBounds.size);
                        }
                    }
                }

                return combinedBounds;
            }

            public static Vector3 GetObjectMajorAxis(GameObject obj) {
                Bounds bounds = GetObjectSize(obj);
                Debug.Log(bounds);

                List<float> dims = new List<float>(new float[] {bounds.size.x, bounds.size.y, bounds.size.z});

                int longest = dims.IndexOf(dims.Max());
                //Debug.Log (bounds.size.x);
                //Debug.Log (bounds.size.y);
                //Debug.Log (bounds.size.z);
                Debug.Log(longest);

                Vector3 axis = Vector3.zero;
                if (longest == 0) {
                    // x
                    axis = Vector3.right;
                }
                else if (longest == 1) {
                    // y
                    axis = Vector3.up;
                }
                else if (longest == 2) {
                    // z
                    axis = Vector3.forward;
                }

                return axis;
            }

            public static Vector3 GetObjectMinorAxis(GameObject obj) {
                Bounds bounds = GetObjectSize(obj);
                Debug.Log(bounds);

                List<float> dims = new List<float>(new float[] {bounds.size.x, bounds.size.y, bounds.size.z});

                int shortest = dims.IndexOf(dims.Min());
                Debug.Log(bounds.size.x);
                Debug.Log(bounds.size.y);
                Debug.Log(bounds.size.z);
                Debug.Log(shortest);

                Vector3 axis = Vector3.zero;
                if (shortest == 0) {
                    // x
                    axis = Vector3.right;
                }
                else if (shortest == 1) {
                    // y
                    axis = Vector3.up;
                }
                else if (shortest == 2) {
                    // z
                    axis = Vector3.forward;
                }

                return axis;
            }

            public static Vector3 ClosestExteriorPoint(GameObject obj, Vector3 pos) {
                Vector3 point = Vector3.zero;
                Bounds bounds = GetObjectWorldSize(obj);

                if (bounds.ClosestPoint(pos) == pos) {
                    // if point inside bounding box
                    List<Vector3> boundsPoints = new List<Vector3>(new Vector3[] {
                        bounds.min,
                        new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                        new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                        new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                        new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                        new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                        new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                        bounds.max
                    });

                    float dist = float.MaxValue;
                    foreach (Vector3 boundsPoint in boundsPoints) {
                        float testDist = Vector3.Distance(pos, boundsPoint);
                        if (testDist < dist) {
                            dist = testDist;
                            point = boundsPoint;
                        }
                    }
                }
                else {
                    point = pos;
                }

                return point;
            }

        //      public static Vector3 GetAxisOfSeparation (GameObject obj1, GameObject obj2) {
        //          Vector3 axis = Vector3.zero;
        //
        //
        //      }

            // if obj1 fits inside obj2
            public static bool FitsIn(Bounds obj1, Bounds obj2, bool threeDimensional = false) {
                bool fits = true;

                if (
                    (obj1.size.x >=
                     obj2.size.x) || // check for object bounds exceeding along all axes but the axis of major orientaton
                    //(obj1.size.y > obj2.size.y) ||
                    (obj1.size.z >= obj2.size.z)) {
                    fits = false;

                    if (threeDimensional) {
                        fits &= (obj1.size.y < obj2.size.y);
                    }
                }

                return fits;
            }

            // if obj1 covers obj2
            public static bool Covers(Bounds obj1, Bounds obj2, Vector3 dir) {
                bool covers = true;

                if (Parallel(dir, Constants.xAxis)) {
                    if ((obj1.size.y + Constants.EPSILON <
                         obj2.size.y) || // check for object bounds exceeding along all axes but dir
                        (obj1.size.z + Constants.EPSILON < obj2.size.z)) {
                        covers = false;
                    }
                }
                else if (Parallel(dir, Constants.yAxis)) {
                    if ((obj1.size.x + Constants.EPSILON <
                         obj2.size.x) || // check for object bounds exceeding along all axes but dir
                        (obj1.size.z + Constants.EPSILON < obj2.size.z)) {
                        covers = false;
                    }
                }
                else if (Parallel(dir, Constants.zAxis)) {
                    if ((obj1.size.x + Constants.EPSILON <
                         obj2.size.x) || // check for object bounds exceeding along all axes but dir
                        (obj1.size.y + Constants.EPSILON < obj2.size.y)) {
                        covers = false;
                    }
                }

                return covers;
            }

            // obj2 is somewhere in obj1's supportingSurface hierarchy
            public static bool IsSupportedBy(GameObject obj1, GameObject obj2) {
                bool supported = false;
                GameObject obj = obj1;

                while (obj.GetComponent<Voxeme>().supportingSurface != null) {
                    //Debug.Log (obj);
                    if (obj.GetComponent<Voxeme>().supportingSurface.gameObject.transform.root.gameObject == obj2) {
                        supported = true;
                        break;
                    }

                    obj = obj.GetComponent<Voxeme>().supportingSurface.gameObject.transform.root.gameObject;
                    //Debug.Log (obj);
                }

                return supported;
            }

            public static Region FindClearRegion(GameObject surface, GameObject testObj, float overhang = 0.0f) {
                Region region = new Region();

                Bounds surfaceBounds = GetObjectWorldSize(surface);
                Bounds testBounds = GetObjectWorldSize(testObj);

                region.min = new Vector3(surfaceBounds.min.x - (testBounds.size.x * overhang) + testBounds.extents.x,
                    surfaceBounds.max.y, surfaceBounds.min.z - (testBounds.size.z * overhang) + testBounds.extents.z);
                region.max = new Vector3(surfaceBounds.max.x + (testBounds.size.x * overhang) - testBounds.extents.x,
                    surfaceBounds.max.y, surfaceBounds.max.z + (testBounds.size.z * overhang) - testBounds.extents.z);

                ObjectSelector objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();

                Vector3 testPoint = new Vector3(Random.Range(region.min.x, region.max.x),
                    Random.Range(region.min.y, region.max.y),
                    Random.Range(region.min.z, region.max.z));
                bool clearRegionFound = false;
                while (!clearRegionFound) {
                    testBounds.center = testPoint;
                    bool regionClear = true;
                    foreach (Voxeme voxeme in objSelector.allVoxemes) {
                        if ((voxeme.gameObject != surface) && (voxeme.gameObject.activeInHierarchy)) {
                            if (testBounds.Intersects(GetObjectWorldSize(voxeme.gameObject))) {
                                regionClear = false;
                                break;
                            }
                        }
                    }

                    if (regionClear) {
                        clearRegionFound = true;
                        break;
                    }

                    testPoint = new Vector3(Random.Range(region.min.x, region.max.x),
                        Random.Range(region.min.y, region.max.y),
                        Random.Range(region.min.z, region.max.z));
                }

                region.min = testPoint - testBounds.extents;
                region.max = testPoint + testBounds.extents;

                return region;
            }

            public static Region FindClearRegion(GameObject surface, Region region, GameObject testObj) {
                Bounds testBounds = GetObjectWorldSize(testObj);

                ObjectSelector objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();

                Vector3 testPoint = new Vector3(Random.Range(region.min.x, region.max.x),
                    Random.Range(region.min.y, region.max.y),
                    Random.Range(region.min.z, region.max.z));
                bool clearRegionFound = false;
                while (!clearRegionFound) {
                    testBounds.center = testPoint;
                    bool regionClear = true;
                    foreach (Voxeme voxeme in objSelector.allVoxemes) {
                        if (voxeme.gameObject != surface) {
                            if (testBounds.Intersects(GetObjectWorldSize(voxeme.gameObject))) {
                                regionClear = false;
                                break;
                            }
                        }
                    }

                    if (regionClear) {
                        clearRegionFound = true;
                        break;
                    }

                    testPoint = new Vector3(Random.Range(region.min.x, region.max.x),
                        Random.Range(region.min.y, region.max.y),
                        Random.Range(region.min.z, region.max.z));
                }

                region.min = testPoint - testBounds.extents;
                region.max = testPoint + testBounds.extents;

                return region;
            }

            public static Region FindClearRegion(GameObject surface, Region[] regions, GameObject testObj) {
                Region region = new Region();
                Bounds testBounds = GetObjectWorldSize(testObj);

                ObjectSelector objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();

                Region intersection = new Region();
                intersection.min = regions[0].min;
                intersection.max = regions[0].max;

                foreach (Region r in regions) {
                    if (r.min.x > intersection.min.x) {
                        intersection.min = new Vector3(r.min.x, intersection.min.y, intersection.min.z);
                    }

                    if (r.min.y > intersection.min.y) {
                        intersection.min = new Vector3(intersection.min.x, r.min.y, intersection.min.z);
                    }

                    if (r.min.z > intersection.min.z) {
                        intersection.min = new Vector3(intersection.min.x, intersection.min.y, r.min.z);
                    }

                    if (r.max.x < intersection.max.x) {
                        intersection.max = new Vector3(r.max.x, intersection.max.y, intersection.max.z);
                    }

                    if (r.max.y < intersection.max.y) {
                        intersection.max = new Vector3(intersection.max.x, r.max.y, intersection.max.z);
                    }

                    if (r.max.z < intersection.max.z) {
                        intersection.max = new Vector3(intersection.max.x, intersection.max.y, r.max.z);
                    }
                }

                Vector3 testPoint = new Vector3(Random.Range(intersection.min.x, intersection.max.x),
                    Random.Range(intersection.min.y, intersection.max.y),
                    Random.Range(intersection.min.z, intersection.max.z));
                bool clearRegionFound = false;
                while (!clearRegionFound) {
                    testBounds.center = testPoint;
                    bool regionClear = true;
                    foreach (Voxeme voxeme in objSelector.allVoxemes) {
                        if (voxeme.gameObject != surface) {
                            if (testBounds.Intersects(GetObjectWorldSize(voxeme.gameObject))) {
                                regionClear = false;
                                break;
                            }
                        }
                    }

                    if (regionClear) {
                        clearRegionFound = true;
                        break;
                    }

                    testPoint = new Vector3(Random.Range(intersection.min.x, intersection.max.x),
                        Random.Range(intersection.min.y, intersection.max.y),
                        Random.Range(intersection.min.z, intersection.max.z));
                }

                region.min = testPoint - testBounds.extents;
                region.max = testPoint + testBounds.extents;

                return region;
            }

            public static Region RegionOfIntersection(Region r1, Region r2, MajorAxis axis) {
                Region intersection = new Region();

                if (axis == MajorAxis.X) {
        //              Debug.Log ("X");
                    intersection.min = new Vector3(r1.max.x, Mathf.Max(r1.min.y, r2.min.y), Mathf.Max(r1.min.z, r2.min.z));
                    intersection.max = new Vector3(r1.max.x, Mathf.Min(r1.max.y, r2.max.y), Mathf.Min(r1.max.z, r2.max.z));
                }
                else if (axis == MajorAxis.Y) {
        //              Debug.Log ("Y");
                    intersection.min = new Vector3(Mathf.Max(r1.min.x, r2.min.x), r1.max.y, Mathf.Max(r1.min.z, r2.min.z));
                    intersection.max = new Vector3(Mathf.Min(r1.max.x, r2.max.x), r1.max.y, Mathf.Min(r1.max.z, r2.max.z));
                }
                else if (axis == MajorAxis.Z) {
        //              Debug.Log ("Z");
                    intersection.min = new Vector3(Mathf.Max(r1.min.x, r2.min.x), Mathf.Max(r1.min.y, r2.min.y), r1.max.z);
                    intersection.max = new Vector3(Mathf.Min(r1.max.x, r2.max.x), Mathf.Min(r1.max.y, r2.max.y), r1.max.z);
                }

                return intersection;
            }

            public static string RegionToString(Region region) {
                if (region == null) {
                    return "null";
                }
                else {
                    return string.Format("Region(min:{0}, max:{1})", region.min.ToString(), region.max.ToString());
                }
            }

            public static bool RegionsEqual(Region r1, Region r2) {
                if (r1 == null && r2 == null) {
                    return true;
                }
                else if (r1 == null || r2 == null) {
                    return false;
                }
                else if (CloseEnough(r1.min, r2.min) && CloseEnough(r1.max, r2.max)) {
                    return true;
                }
                else {
                    return false;
                }
            }

            // two vectors are within epsilon
            public static bool CloseEnough(Vector3 v1, Vector3 v2) {
                return ((v1 - v2).magnitude < Constants.EPSILON);
            }

            // two vectors are within epsilon (angle)
            public static bool AngleCloseEnough(Vector3 v1, Vector3 v2) {
                return (Mathf.Abs(Vector3.Angle(v1, v2)) < Mathf.Rad2Deg * Mathf.Rad2Deg * Constants.EPSILON);
            }

            // two quaternions are within epsilon
            public static bool CloseEnough(Quaternion q1, Quaternion q2) {
                return (Quaternion.Angle(q1, q2) < Constants.EPSILON);
            }

            public static bool IsTopmostVoxemeInHierarchy(GameObject obj) {
                Voxeme voxeme = obj.GetComponent<Voxeme>();
                bool r = false;

                if (voxeme != null) {
                    r = (voxeme.gameObject.transform.parent == null);
                }

                return r;
            }

            public static GameObject GetMostImmediateParentVoxeme(GameObject obj) {
                /*GameObject voxObject = obj;

                while (voxObject.transform.parent != null) {
                    voxObject = voxObject.transform.parent.gameObject;
                    if (voxObject.GetComponent<Rigging> () != null) {
                        if (voxObject.GetComponent<Rigging> ().enabled) {
                            break;
                        }
                    }
                }*/

                GameObject voxObject = obj;
                GameObject testObject = obj;

                while (testObject.transform.parent != null) {
                    testObject = testObject.transform.parent.gameObject;
                    if (testObject.GetComponent<Rigging>() != null) {
                        if (testObject.GetComponent<Rigging>().enabled) {
                            voxObject = testObject;
                            break;
                        }
                    }
                }

                return voxObject;
            }

            public static List<GameObject> ContainingObjects(Vector3 point) {
                List<GameObject> objs = new List<GameObject>();

                GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

                foreach (GameObject o in allObjects) {
                    if (GetObjectWorldSize(o).Contains(point)) {
                        objs.Add(o);
                    }
                }

                return objs;
            }

            public static float GetMinYBoundAtTarget(GameObject obj, Vector3 targetPoint) {
                float minYBound = 0.0f;

                Bounds objBounds = GetObjectWorldSize(obj);

                Debug.Log(VectorToParsable(targetPoint));
                Vector3 rayStartX = new Vector3(objBounds.min.x - Constants.EPSILON,
                    objBounds.min.y + Constants.EPSILON, objBounds.center.z);
                Vector3 contactPointX = RayIntersectionPoint(rayStartX, Vector3.right);
                //contactPointX = new Vector3 (contactPointX.x, transform.position.y, contactPointX.z);

                Vector3 rayStartZ = new Vector3(objBounds.center.x,
                    objBounds.min.y + Constants.EPSILON, objBounds.min.z - Constants.EPSILON);
                Vector3 contactPointZ = RayIntersectionPoint(rayStartZ, Vector3.forward);

                //Debug.Log(Helper.VectorToParsable(rayStartX));
                //Debug.Log(Helper.VectorToParsable(rayStartZ));
                //Debug.Log(Helper.VectorToParsable(contactPointX));
                //Debug.Log(Helper.VectorToParsable(contactPointZ));
                //Debug.Log(Helper.VectorToParsable(contactPointX - objBounds.center));
                //Debug.Log(Helper.VectorToParsable(contactPointZ - objBounds.center));
                //Vector3 contactPoint = (contactPointZ.y < contactPointX.y) ?
                //new Vector3((contactPointZ.x-objBounds.center.x) + (targetPoint.x-objBounds.center.x),
                //    targetPoint.y, (contactPointZ.z-objBounds.center.z + (targetPoint.z-objBounds.center.z))) :
                //new Vector3((contactPointX.x-objBounds.center.x) + (targetPoint.x - objBounds.center.x),
                //targetPoint.y, (contactPointX.z-objBounds.center.z + (targetPoint.z - objBounds.center.z)));
                Vector3 contactPoint = (contactPointZ.y < contactPointX.y)
                    ? targetPoint + (contactPointZ - objBounds.center)
                    : targetPoint + (contactPointX - objBounds.center);
                contactPoint = new Vector3(contactPoint.x, targetPoint.y, contactPoint.z);
                Debug.Log(VectorToParsable(contactPoint));

                RaycastHit[] hits;

                //      hits = Physics.RaycastAll (transform.position, AxisVector.negYAxis);
                hits = Physics.RaycastAll(contactPoint, AxisVector.negYAxis);
                List<RaycastHit> hitList = new List<RaycastHit>(hits);
                hits = hitList.OrderBy(h => h.distance).ToArray();
                foreach (RaycastHit hit in hits) {
                    if (hit.collider.gameObject.GetComponent<BoxCollider>() != null) {
                        Debug.Log(hit.collider.gameObject);
                        if (!hit.collider.gameObject.GetComponent<BoxCollider>().isTrigger) {
                            Debug.Log(hit.collider.gameObject);
                            if (!FitsIn(GetObjectWorldSize(hit.collider.gameObject),
                                GetObjectWorldSize(obj), true)) {
                                Debug.Log(hit.collider.gameObject);
                                GameObject supportingSurface = hit.collider.gameObject;
                                minYBound = GetObjectWorldSize(supportingSurface).max.y;

                                break;
                            }
                        }
                    }
                }

                return minYBound;
            }
        }
    }
}