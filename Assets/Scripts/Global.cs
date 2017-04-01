using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;

namespace Global {
	/// <summary>
	/// Constants
	/// </summary>
	public static class Constants {
		public const float EPSILON = 0.003f;
		public static Vector3 xAxis = Vector3.right;
		public static Vector3 yAxis = Vector3.up;
		public static Vector3 zAxis = Vector3.forward;
		public static Dictionary<string,Vector3> Axes = new Dictionary<string,Vector3>{
			{"X", xAxis},
			{"Y", yAxis},
			{"Z", zAxis}
		};
	}

	public static class Data {
	#if UNITY_EDITOR
		public static string voxmlDataPath = Application.dataPath.Remove (Application.dataPath.LastIndexOf ('/') + 1) + string.Format ("Data/voxml");
	#elif UNITY_STANDALONE_OSX
		public static string voxmlDataPath = Application.dataPath.Remove (Application.dataPath.LastIndexOf('/', Application.dataPath.LastIndexOf('/') - 1)) + string.Format ("/Data/voxml");
	#elif UNITY_STANDALONE_WIN
		public static string voxmlDataPath = Application.dataPath.Remove (Application.dataPath.LastIndexOf ('/') + 1) + string.Format ("Data/voxml");
	#endif
	}

	/// <summary>
	/// Region class
	/// </summary>
	public class Region {
		Vector3 _min,_max,_center;

		public Vector3 min {
			get { return _min; }
			set { _min = value;
					_center = (min + max) / 2.0f;
				}
		}

		public Vector3 max {
			get { return _max; }
			set { _max = value; 
					_center = (min + max) / 2.0f;
				}
		}

		public Vector3 center {
			get { return _center; }
		}

		public Region() {
			min = Vector3.zero;
			max = Vector3.zero;
		}

		public bool Contains(Vector3 point) {
			return ((point.x >= min.x) && (point.x <= max.x) &&
				(point.y >= min.y) && (point.y <= max.y) &&
				(point.z >= min.z) && (point.z <= max.z));
		}
	}

	/// <summary>
	/// List comparer class
	/// </summary>
	public class ListComparer<T> : IEqualityComparer<List<T>>
	{
		public bool Equals(List<T> x, List<T> y)
		{
			return x.SequenceEqual(y);
		}

		public int GetHashCode(List<T> obj)
		{
			int hashcode = 0;
			foreach (T t in obj)
			{
				hashcode ^= t.GetHashCode();
			}
			return hashcode;
		}
	}

	/// <summary>
	/// Pair class
	/// </summary>
	public class Pair<T1, T2> : IEquatable<System.Object>{
		public T1 Item1{
			get;
			set;
		}

		public T2 Item2{
			get;
			set;
		}

		public Pair(T1 Item1, T2 Item2){
			this.Item1 = Item1;
			this.Item2 = Item2;
		}

		public override bool Equals(object obj) {
			if ( obj == null || (obj as Pair<T1, T2>) == null ) //if the object is null or the cast fails
				return false;
			else {
				Pair<T1,T2> tuple = (Pair<T1, T2>) obj;
				return Item1.Equals(tuple.Item1) && Item2.Equals(tuple.Item2);
			}
		}

		public override int GetHashCode() {
			return Item1.GetHashCode() ^ Item2.GetHashCode();
		}

		public static bool operator == (Pair<T1, T2> tuple1, Pair<T1, T2> tuple2) {
			return tuple1.Equals(tuple2);
		}

		public static bool operator != (Pair<T1, T2> tuple1, Pair<T1, T2> tuple2) {
			return !tuple1.Equals(tuple2);
		}
	}

	/// <summary>
	/// Triple class
	/// </summary>
	public class Triple<T1, T2, T3> : IEquatable<System.Object>{
		public T1 Item1{
			get;
			set;
		}
		
		public T2 Item2{
			get;
			set;
		}
		
		public T3 Item3{
			get;
			set;
		}
		
		public Triple(T1 Item1, T2 Item2, T3 Item3){
			this.Item1 = Item1;
			this.Item2 = Item2;
			this.Item3 = Item3;
		}
		
		public override bool Equals(object obj) {
			if ( obj == null || (obj as Triple<T1, T2, T3>) == null ) //if the object is null or the cast fails
				return false;
			else {
				Triple<T1,T2,T3> tuple = (Triple<T1, T2, T3>) obj;
				return Item1.Equals(tuple.Item1) && Item2.Equals(tuple.Item2) && Item3.Equals(tuple.Item3);
			}
		}
		
		public override int GetHashCode() {
			return Item1.GetHashCode() ^ Item2.GetHashCode() ^ Item3.GetHashCode();
		}
		
		public static bool operator == (Triple<T1, T2, T3> tuple1, Triple<T1, T2, T3> tuple2) {
			return tuple1.Equals(tuple2);
		}
		
		public static bool operator != (Triple<T1, T2, T3> tuple1, Triple<T1, T2, T3> tuple2) {
			return !tuple1.Equals(tuple2);
		}
	}

	/// <summary>
	/// Helper class
	/// </summary>
	public static class Helper {
		public static Regex v = new Regex (@"<.*>");
		public static Regex cv = new Regex (@",<.*>");
		public static Regex l = new Regex ("[\'\"].*[\'\"]");

		// DATA METHODS
		public static Hashtable ParsePredicate(String predicate) {
			Hashtable predArgs = new Hashtable ();
			
			Queue<String> split = new Queue<String>(predicate.Split (new char[] {'('},2,StringSplitOptions.None));
			if (split.Count > 1) {
				String pred = split.ElementAt (0);
				String args = split.ElementAt (1);
				args = args.Remove (args.Length - 1);
				predArgs.Add (pred, args);
			}
			
			return predArgs;
		}

		public static String GetTopPredicate(String formula) {
			Queue<String> split = new Queue<String>(formula.Split (new char[] {'('},2,StringSplitOptions.None));
			return split.ElementAt (0);
		}

		public static void PrintKeysAndValues(Hashtable ht)  {
			foreach (DictionaryEntry entry in ht)
				Debug.Log(entry.Key + " : " + entry.Value);
		}

		public static String VectorToParsable(Vector3 vector) {
			return ("<"+vector.x.ToString ()+"; "+
			       	vector.y.ToString ()+"; "+
			        vector.z.ToString ()+">");
		}
		
		public static Vector3 ParsableToVector(String parsable) {
			List<String> components = new List<String> ((((String)parsable).Replace("<","").Replace(">","")).Split (new char[] {';'}));
			return new Vector3(System.Convert.ToSingle(components[0]),
			                   System.Convert.ToSingle(components[1]),
			                   System.Convert.ToSingle(components[2]));
		}

		public static String QuaternionToParsable(Quaternion quat) {
			return ("<"+quat.x.ToString ()+"; "+
			        quat.y.ToString ()+"; "+
			        quat.z.ToString ()+"; "+
			        quat.w.ToString ()+">");
		}
		
		public static Quaternion ParsableToQuaternion(String parsable) {
			List<String> components = new List<String> ((((String)parsable).Replace("<","").Replace(">","")).Split (new char[] {';'}));
			return new Quaternion(System.Convert.ToSingle(components[0]),
								System.Convert.ToSingle(components[1]),
			                  	System.Convert.ToSingle(components[2]),
			                  	System.Convert.ToSingle(components[3]));
		}

		public static String IntToString(int inInt) {
			return System.Convert.ToString(inInt);
		}

		public static int StringToInt(String inString) {
			return System.Convert.ToInt32(inString);
		}

		public static Triple<String,String,String> MakeRDFTriples(String formula) {		// fix for multiple RDF triples
			Triple<String,String,String> triple = new Triple<String,String,String> ("","","");
			Debug.Log ("MakeRDFTriple: " + formula);
			formula = cv.Replace (formula, "");
			String[] components = formula.Replace ('(', '/').Replace (')', '/').Replace (',','/').Split ('/');

//			//Debug.Log (components.Length);
//			foreach (String s in components) {
//				Debug.Log (s);
//			}
			//Debug.Break ();

			if (components.Length <= 3) {
				triple.Item1 = "NULL";
			}

			foreach (String s in components) {
				if (s.Length > 0) {
					//Debug.Log (s);
					GameObject obj = GameObject.Find (s);
					if (obj != null) {
						if (components [0] == "def") {
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
						if (v.IsMatch (s)) {
							if (triple.Item3 == "") {
								triple.Item3 = s;
							}
						}
						else if (l.IsMatch (s)) {
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
				triple.Item2 = triple.Item2.Remove (triple.Item2.Length - 1, 1);
			}
			
			Debug.Log (triple.Item1 + " " + triple.Item2 + " " + triple.Item3);
			return triple;
		}

		public static void PrintRDFTriples(List<Triple<String,String,String>> triples) {
			if (triples.Count == 0) {
				Debug.Log ("No RDF triples to print");
			}

			foreach (Triple<String,String,String> triple in triples) {
				Debug.Log (triple.Item1 + " " + triple.Item2 + " " + triple.Item3);
			}
		}

		public static Hashtable DiffHashtables(Hashtable baseline, Hashtable comparand) {
			Hashtable diff = new Hashtable ();

			foreach (DictionaryEntry entry in comparand) {
				if (!baseline.ContainsKey (entry.Key)) {
					object key = entry.Key;
					if (entry.Key is List<object>) {
						key = string.Join (",", ((List<object>)entry.Value).Cast<string>().ToArray ());
					}

					object value = entry.Value;
					if (entry.Value is List<object>) {
						value = string.Join (",", ((List<object>)entry.Value).Cast<string>().ToArray ());
					}

					diff.Add (key, value);
				}
				else if (baseline[entry.Key] != entry.Value) {
					object key = entry.Key;
					if (entry.Key is List<object>) {
						key = string.Join (",", ((List<object>)entry.Key).Cast<string>().ToArray ());
					}

					object value = entry.Value;
					if (entry.Value is List<object>) {
						value = string.Join (",", ((List<object>)entry.Value).Cast<string>().ToArray ());
					}

					diff.Add(key,value);
				}
			}

			return diff;
		}

		public static string HashtableToString(Hashtable ht) {
			string output = string.Empty;
			foreach (DictionaryEntry entry in ht) {
				output += string.Format ("{0} {1};", entry.Key, entry.Value);
			}

			return output;
		}

		public static List<object> DiffLists(List<object> baseline, List<object> comparand) {
			return comparand.Except (baseline).ToList ();
		}

		public static byte[] SerializeObjectToBinary(object obj) {
			BinaryFormatter binFormatter = new BinaryFormatter ();
			MemoryStream mStream = new MemoryStream ();
			binFormatter.Serialize (mStream, obj);

			//This gives you the byte array.
			return mStream.ToArray();
		}

		public static T DeserializeObjectFromBinary<T>(byte[] bytes) {
			MemoryStream mStream = new MemoryStream();
			BinaryFormatter binFormatter = new BinaryFormatter();

			// Where 'bytes' is your byte array.
			mStream.Write (bytes, 0, bytes.Length);
			mStream.Position = 0;

			return (T)binFormatter.Deserialize(mStream);
		}

		public static string SerializeObjectToJSON(object obj) {
			string json = JsonUtility.ToJson (obj);
//			Debug.Log (json);
//			Debug.Break ();

			return json;
		}

		// VECTOR METHODS
		public static bool VectorIsNaN(Vector3 vec) {
			return (float.IsNaN (vec.x) || float.IsNaN (vec.y) || float.IsNaN (vec.z));
		}

		public static bool PointInRect(Vector2 point, Rect rect) {
			return (point.x >= rect.xMin && point.x <= rect.xMax && point.y >= rect.yMin && point.y <= rect.yMax);
		}

		public static bool PointOutsideMaskedAreas(Vector2 point, Rect[] rects) {
			bool outside = true;
			
			foreach (Rect r in rects) {
				if (PointInRect(point,r)) {
					outside = false;
				}
			}
			
			return outside;
		}

		public static Vector3 RayIntersectionPoint(Vector3 rayStart, Vector3 direction) {
			//Collider[] colliders = obj.GetComponentsInChildren<Collider> ();
			List<RaycastHit> hits = new List<RaycastHit> ();

			//foreach (Collider c in colliders) {
			RaycastHit hitInfo = new RaycastHit ();
			Physics.Raycast (rayStart, direction.normalized, out hitInfo);
			hits.Add (hitInfo);
			//}
				
			RaycastHit closestHit = hits [0];

			foreach (RaycastHit hit in hits) {
				if (hit.distance < closestHit.distance) {
					closestHit = hit;
				}
			}

			return closestHit.point;
		}

		public static bool Parallel(Vector3 v1, Vector3 v2) {
			return (1-Vector3.Dot (v1, v2) < Constants.EPSILON * 10);
		}

		public static bool Perpendicular(Vector3 v1, Vector3 v2) {
			return (Mathf.Abs (Vector3.Dot (v1, v2)) < Constants.EPSILON * 10);
		}

		public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 eulerAngles) {
			Vector3 dir = point - pivot; // get point direction relative to pivot
			dir = Quaternion.Euler(eulerAngles) * dir; // rotate it
			point = dir + pivot; // calculate rotated point
			return point; // return it
		}

		// OBJECT METHODS
		public static Bounds GetObjectSize(GameObject obj) {
			MeshFilter[] meshes = obj.GetComponentsInChildren<MeshFilter>();

			Bounds combinedBounds = new Bounds (Vector3.zero, Vector3.zero);
			
			foreach (MeshFilter mesh in meshes) {
				Bounds temp = new Bounds (Vector3.zero,mesh.mesh.bounds.size);
				//Debug.Log(mesh.gameObject.name);
				//Debug.Log(temp);
				Vector3 min = new Vector3 (temp.min.x * mesh.gameObject.transform.lossyScale.x,
					temp.min.y * mesh.gameObject.transform.lossyScale.y,
					temp.min.z * mesh.gameObject.transform.lossyScale.z);
				Vector3 max = new Vector3 (temp.max.x * mesh.gameObject.transform.lossyScale.x,
					temp.max.y * mesh.gameObject.transform.lossyScale.y,
					temp.max.z * mesh.gameObject.transform.lossyScale.z);
				//Debug.Log (Helper.VectorToParsable(min));
				//Debug.Log (Helper.VectorToParsable(max));
				//Debug.Log(mesh.gameObject.transform.root.GetChild(0).localScale);
				//Debug.Log(mesh.gameObject.name);
				//Debug.Log(mesh.gameObject.transform.localEulerAngles);
				//temp.center = obj.transform.position;
				//temp.SetMinMax (min,max);
				temp.SetMinMax (RotatePointAroundPivot(min,Vector3.zero,mesh.gameObject.transform.localEulerAngles),
					RotatePointAroundPivot(max,Vector3.zero,mesh.gameObject.transform.localEulerAngles));
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

		// get the bounds of the object in the current world
		public static Bounds GetObjectWorldSize(GameObject obj) {
			Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

			Bounds combinedBounds = new Bounds (obj.transform.position, Vector3.zero);

			foreach (Renderer renderer in renderers) {
				combinedBounds.Encapsulate(renderer.bounds);
			}

			return combinedBounds;
		}

		// get the bounds of the object in the current world, excluding any children listed
		public static Bounds GetObjectWorldSize(GameObject obj, List<GameObject> exclude) {
			Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

			Bounds combinedBounds = new Bounds (obj.transform.position, Vector3.zero);

			foreach (Renderer renderer in renderers) {
				if (!exclude.Contains (renderer.transform.gameObject)) {
					combinedBounds.Encapsulate (renderer.bounds);
				}
			}

			return combinedBounds;
		}

		// get the collective bounds of the objects in the current world
		public static Bounds GetObjectWorldSize(List<GameObject> objs) {
			Bounds combinedBounds = new Bounds (objs[0].transform.position, Vector3.zero);

			foreach (GameObject obj in objs) {
				Renderer[] renderers = obj.GetComponentsInChildren<Renderer> ();

				foreach (Renderer renderer in renderers) {
					combinedBounds.Encapsulate (renderer.bounds);
				}
			}

			return combinedBounds;
		}

		// get the collective bounds of the objects in the current world
		public static Bounds GetPointsWorldSize(List<Vector3> pts) {
			Bounds combinedBounds = new Bounds (pts[0], Vector3.zero);

			foreach (Vector3 pt in pts) {
				combinedBounds.Encapsulate (pt);
			}

			return combinedBounds;
		}

		public static Bounds GetObjectSize(object[] objs) {
			Bounds starterBounds = (objs [0] as GameObject).GetComponentsInChildren<BoxCollider> ()[0].bounds;
			Bounds combinedBounds = starterBounds;
			//Debug.Log (combinedBounds.size);

			foreach (object obj in objs) {
				if (obj is GameObject) {
					//Debug.Log ((obj as GameObject).name);
					Renderer[] renderers = (obj as GameObject).GetComponentsInChildren<Renderer> ();

					foreach (Renderer renderer in renderers) {
						//Bounds bounds = new Bounds(renderer.bounds.center,
						//                           renderer.bounds.size);
						//localBounds.center = renderer.gameObject.transform.position;
						//localBounds.size = new Vector3(localBounds.size.x * renderer.gameObject.transform.localScale.x,
						//                               localBounds.size.y * renderer.gameObject.transform.localScale.y,
						//                               localBounds.size.z * renderer.gameObject.transform.localScale.z);
						combinedBounds.Encapsulate (renderer.bounds);

						//Debug.Log (combinedBounds.center);
						//Debug.Log (combinedBounds.size);
					}
				}
			}
			
			return combinedBounds;
		}

		public static Vector3 GetObjectMajorAxis (GameObject obj) {
			Bounds bounds = GetObjectSize (obj);
			Debug.Log (bounds);

			List<float> dims = new List<float>(new float[]{bounds.size.x, bounds.size.y, bounds.size.z});

			int longest = dims.IndexOf(dims.Max());
			//Debug.Log (bounds.size.x);
			//Debug.Log (bounds.size.y);
			//Debug.Log (bounds.size.z);
			Debug.Log (longest);

			Vector3 axis = Vector3.zero;
			if (longest == 0) {			// x
				axis = Vector3.right;
			}
			else if (longest == 1) {	// y
				axis = Vector3.up;
			}
			else if (longest == 2) {	// z
				axis = Vector3.forward;
			}

			return axis;
		}

		public static Vector3 GetObjectMinorAxis (GameObject obj) {
			Bounds bounds = GetObjectSize (obj);
			Debug.Log (bounds);

			List<float> dims = new List<float>(new float[]{bounds.size.x, bounds.size.y, bounds.size.z});

			int shortest = dims.IndexOf(dims.Min());
			Debug.Log (bounds.size.x);
			Debug.Log (bounds.size.y);
			Debug.Log (bounds.size.z);
			Debug.Log (shortest);

			Vector3 axis = Vector3.zero;
			if (shortest == 0) {		// x
				axis = Vector3.right;
			}
			else if (shortest == 1) {	// y
				axis = Vector3.up;
			}
			else if (shortest == 2) {	// z
				axis = Vector3.forward;
			}

			return axis;
		}

		public static Vector3 ClosestExteriorPoint (GameObject obj, Vector3 pos) {
			Vector3 point = Vector3.zero;
			Bounds bounds = GetObjectWorldSize (obj);

			if (bounds.ClosestPoint (pos) == pos) {	// if point inside bounding box
				List<Vector3> boundsPoints = new List<Vector3> (new Vector3[] {
					bounds.min,
					new Vector3 (bounds.min.x, bounds.min.y, bounds.max.z),
					new Vector3 (bounds.min.x, bounds.max.y, bounds.min.z),
					new Vector3 (bounds.min.x, bounds.max.y, bounds.max.z),
					new Vector3 (bounds.max.x, bounds.min.y, bounds.min.z),
					new Vector3 (bounds.max.x, bounds.min.y, bounds.max.z),
					new Vector3 (bounds.max.x, bounds.max.y, bounds.min.z),
					bounds.max });

				float dist = float.MaxValue;
				foreach (Vector3 boundsPoint in boundsPoints) {
					float testDist = Vector3.Distance (pos, boundsPoint);
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

//		public static Vector3 GetAxisOfSeparation (GameObject obj1, GameObject obj2) {
//			Vector3 axis = Vector3.zero;
//
//
//		}

		// if obj1 fits inside obj2
		public static bool FitsIn(Bounds obj1, Bounds obj2, bool threeDimensional = false) {
			bool fits = true;

			if ((obj1.size.x >= obj2.size.x) ||	// check for object bounds exceeding along all axes but the axis of major orientaton
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

			if (Helper.Parallel(dir, Constants.xAxis)) {
				if ((obj1.size.y+Constants.EPSILON < obj2.size.y) ||	// check for object bounds exceeding along all axes but dir
					(obj1.size.z+Constants.EPSILON < obj2.size.z)) {
					covers = false;
				}
			}
			else if (Helper.Parallel(dir, Constants.yAxis)) {
				if ((obj1.size.x+Constants.EPSILON < obj2.size.x) ||	// check for object bounds exceeding along all axes but dir
					(obj1.size.z+Constants.EPSILON < obj2.size.z)) {
					covers = false;
				}
			}
			else if (Helper.Parallel(dir, Constants.zAxis)) {
				if ((obj1.size.x+Constants.EPSILON < obj2.size.x) ||	// check for object bounds exceeding along all axes but dir
					(obj1.size.y+Constants.EPSILON < obj2.size.y)) {
					covers = false;
				}
			}

			return covers;
		}

		// obj2 is somewhere in obj1's supportingSurface hierarchy
		public static bool IsSupportedBy(GameObject obj1, GameObject obj2) {
			bool supported = false;
			GameObject obj = obj1;

			while (obj.GetComponent<Voxeme> ().supportingSurface != null) {
				//Debug.Log (obj);
				if (obj.GetComponent<Voxeme> ().supportingSurface.gameObject.transform.root.gameObject == obj2) {
					supported = true;
					break;
				}
				obj = obj.GetComponent<Voxeme> ().supportingSurface.gameObject.transform.root.gameObject;
				//Debug.Log (obj);
			}

			return supported;
		}

		public static Region FindClearRegion(GameObject surface, GameObject testObj) {
			Region region = new Region ();

			Bounds surfaceBounds = GetObjectWorldSize (surface);
			Bounds testBounds = GetObjectWorldSize (testObj);

			region.min = new Vector3 (surfaceBounds.min.x, surfaceBounds.max.y, surfaceBounds.min.z);
			region.max = new Vector3 (surfaceBounds.max.x, surfaceBounds.max.y, surfaceBounds.max.z);

			ObjectSelector objSelector = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ();

			Vector3 testPoint = new Vector3 (UnityEngine.Random.Range (region.min.x, region.max.x),
				                    UnityEngine.Random.Range (region.min.y, region.max.y),
				                    UnityEngine.Random.Range (region.min.z, region.max.z));
			bool clearRegionFound = false;
			while (!clearRegionFound) {
				testBounds.center = testPoint;
				bool regionClear = true;
				foreach (Voxeme voxeme in objSelector.allVoxemes) {
					if (voxeme.gameObject != surface) {
						if (testBounds.Intersects(Helper.GetObjectWorldSize(voxeme.gameObject))) {
							regionClear = false;
							break;
						}
					}
				}

				if (regionClear) {
					clearRegionFound = true;
					break;
				}

				testPoint = new Vector3 (UnityEngine.Random.Range (region.min.x, region.max.x),
					UnityEngine.Random.Range (region.min.y, region.max.y),
					UnityEngine.Random.Range (region.min.z, region.max.z));
			}

			region.min = testPoint - testBounds.extents;
			region.max = testPoint + testBounds.extents;
					
			return region;
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
			return (Quaternion.Angle(q1,q2) < Constants.EPSILON);
		}

		public static bool IsTopmostVoxemeInHierarchy(GameObject obj) {
			Voxeme voxeme = obj.GetComponent<Voxeme> ();
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
				if (testObject.GetComponent<Rigging> () != null) {
					if (testObject.GetComponent<Rigging> ().enabled) {
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
				if (Helper.GetObjectWorldSize (o).Contains (point)) {
					objs.Add(o);
				}
			}

			return objs;
		}
	}


	/// <summary>
	/// Physics helper.
	/// </summary>
	public static class PhysicsHelper {
		public static void ResolveAllPhysicsDiscepancies(bool macroEventSatisfied) {
			ObjectSelector objSelector = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ();
			foreach (Voxeme voxeme in objSelector.allVoxemes) {
				ResolvePhysicsDiscepancies (voxeme.gameObject, macroEventSatisfied);
			}
		}

		public static void ResolvePhysicsDiscepancies(GameObject obj, bool macroEventSatisfied) {
			// check and see if rigidbody orientations and main body orientations are getting out of sync
			// due to physics effects
			ResolvePhysicsPositionDiscepancies(obj, macroEventSatisfied);
			ResolvePhysicsRotationDiscepancies(obj, macroEventSatisfied);
//			ResolvePhysicsPositionDiscepancies(obj);
		}

		public static void ResolvePhysicsRotationDiscepancies(GameObject obj, bool macroEventSatisfied) {
			Voxeme voxComponent = obj.GetComponent<Voxeme> ();

			// find the smallest displacement angle between an axis on the main body and an axis on this rigidbody
			float displacementAngle = 360.0f;
			Quaternion rigidbodyRotation = Quaternion.identity;
			Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody> ();
			foreach (Rigidbody rigidbody in rigidbodies) {
				foreach (Vector3 mainBodyAxis in Constants.Axes.Values) {
					foreach (Vector3 rigidbodyAxis in Constants.Axes.Values) {
						if (Vector3.Angle (obj.transform.rotation * mainBodyAxis, rigidbody.rotation * rigidbodyAxis) < displacementAngle) {
							displacementAngle = Vector3.Angle (obj.transform.rotation * mainBodyAxis, rigidbody.rotation * rigidbodyAxis);
							rigidbodyRotation = rigidbody.rotation;
						}
					}
				}
			}

			if (displacementAngle == 360.0f) {
				displacementAngle = 0.0f;
			}

			if (displacementAngle > Mathf.Rad2Deg * Constants.EPSILON) {
				//Debug.Break ();
				//Debug.Log (obj.name);
				//Debug.Log (displacementAngle);
				Quaternion resolve = Quaternion.identity;
				Quaternion resolveInv = Quaternion.identity;
				if (voxComponent != null) {
					if (rigidbodies.Length > 0) {
//						foreach (Rigidbody rigidbody in rigidbodies) {
//							if (voxComponent.rotationalDisplacement.ContainsKey (rigidbody.gameObject)) {
//								Debug.Log (rigidbody.name);
//								// initial = initial rotational displacement
//								Quaternion initial = Quaternion.Euler (voxComponent.rotationalDisplacement [rigidbody.gameObject]);
//								Debug.Log (initial.eulerAngles);
//								// current = current rotational displacement due to physics
//								Quaternion current = rigidbody.transform.localRotation;// * Quaternion.Inverse ((args [0] as GameObject).transform.rotation));
//								Debug.Log (current.eulerAngles);
//								// resolve = rotation to get from initial rotational displacement to current rotational displacement
//								resolve = current * Quaternion.Inverse (initial);
//								Debug.Log (resolve.eulerAngles);
//								Debug.Log ((initial * resolve).eulerAngles);
//								Debug.Log ((resolve * initial).eulerAngles);
//								// resolveInv = rotation to get from final (current rigidbody) rotation back to initial (aligned with main obj) rotation
//								resolveInv = initial * Quaternion.Inverse (current);
//								//Debug.Log (resolveInv.eulerAngles);
//								//rigidbody.transform.rotation = obj.transform.rotation * initial;
//								//rigidbody.transform.localRotation = initial;// * (args [0] as GameObject).transform.rotation;
//								//Debug.Log (rigidbody.transform.rotation.eulerAngles);
//
//								//rigidbody.transform.localPosition = voxComponent.displacement [rigidbody.name];
//								//rigidbody.transform.position = (args [0] as GameObject).transform.position + voxComponent.displacement [rigidbody.name];
//							}
//						}

						//Debug.Break ();

						//Debug.Log (obj.transform.rotation.eulerAngles);
						//foreach (Rigidbody rigidbody in rigidbodies) {
						//Debug.Log (Helper.VectorToParsable (rigidbody.transform.localPosition));
						//}

//						obj.transform.rotation = obj.transform.rotation *
//							(rigidbodies [0].transform.localRotation * 
//								Quaternion.Inverse (Quaternion.Euler (voxComponent.rotationalDisplacement [rigidbodies [0].gameObject])));
							//(rigidbodies [0].transform.localRotation *
//							obj.transform.rotation * Quaternion.Inverse (Quaternion.Euler (voxComponent.rotationalDisplacement [rigidbodies [0].gameObject])));
						obj.transform.rotation = rigidbodies [0].transform.rotation *
							Quaternion.Inverse (Quaternion.Euler (voxComponent.rotationalDisplacement [rigidbodies [0].gameObject]));
						voxComponent.targetRotation = obj.transform.rotation.eulerAngles;
						//Debug.Log (obj.transform.rotation.eulerAngles);

						foreach (Rigidbody rigidbody in rigidbodies) {
							if (voxComponent.rotationalDisplacement.ContainsKey (rigidbody.gameObject)) {
								//Debug.Log (rigidbody.name);
								rigidbody.transform.localEulerAngles = voxComponent.rotationalDisplacement [rigidbody.gameObject];
							}
						}

//						rigidbodyRotation = Quaternion.identity;
//						rigidbodies = obj.GetComponentsInChildren<Rigidbody> ();
//						foreach (Rigidbody rigidbody in rigidbodies) {
//							foreach (Vector3 mainBodyAxis in Constants.Axes.Values) {
//								foreach (Vector3 rigidbodyAxis in Constants.Axes.Values) {
//									if (Vector3.Angle (obj.transform.rotation * mainBodyAxis, rigidbody.rotation * rigidbodyAxis) < displacementAngle) {
//										displacementAngle = Vector3.Angle (obj.transform.rotation * mainBodyAxis, rigidbody.rotation * rigidbodyAxis);
//										rigidbodyRotation = rigidbody.rotation;
//									}
//								}
//							}
//						}
					}
				}
			}

			// TODO: Abstract away
			if (voxComponent != null) {
				if (voxComponent.children != null) {
					foreach (Voxeme child in voxComponent.children) {
						if (child.isActiveAndEnabled) {
							if (child.gameObject != voxComponent.gameObject) {
//						ResolvePhysicsPositionDiscepancies (child.gameObject);
//						ResolvePhysicsRotationDiscepancies (child.gameObject);

								if (macroEventSatisfied) {
									child.transform.localRotation = voxComponent.parentToChildRotationOffset [child.gameObject];
									//voxComponent.parentToChildRotationOffset [child.gameObject] = child.transform.localRotation;
									child.transform.rotation = voxComponent.gameObject.transform.rotation * child.transform.localRotation;
								} else {
									voxComponent.parentToChildRotationOffset [child.gameObject] = child.transform.localRotation;
									child.targetRotation = child.transform.rotation.eulerAngles;
								}
								child.transform.localPosition = Helper.RotatePointAroundPivot (voxComponent.parentToChildPositionOffset [child.gameObject],
									Vector3.zero, voxComponent.gameObject.transform.eulerAngles);
								//child.transform.localPosition = Helper.RotatePointAroundPivot (child.transform.localEulerAngles,
								//	Vector3.zero, voxComponent.gameObject.transform.eulerAngles);
								child.transform.position = voxComponent.gameObject.transform.position + child.transform.localPosition;
								child.targetPosition = child.transform.position;
							}
						}
					}
				}
			}
		}

		public static void ResolvePhysicsPositionDiscepancies(GameObject obj, bool macroEventSatisfied) {
			Voxeme voxComponent = obj.GetComponent<Voxeme> ();
			//Debug.Break ();
			// find the displacement between the main body and this rigidbody
			float displacement = float.MaxValue;
			Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody> ();
			//Debug.Log (obj.name);
			foreach (Rigidbody rigidbody in rigidbodies) {
				//if (voxComponent.displacement.ContainsKey (rigidbody.gameObject)) {
				//	if (rigidbody.transform.localPosition.magnitude > voxComponent.displacement [rigidbody.gameObject].magnitude+Constants.EPSILON) {
				if (rigidbody.transform.localPosition.magnitude < displacement) {
//					Debug.Log (rigidbody.name);
//					Debug.Log (Helper.VectorToParsable (obj.transform.position));
//					Debug.Log (Helper.VectorToParsable (rigidbody.transform.position));
					displacement = rigidbody.transform.localPosition.magnitude;
				}
				//	}
				//}
			}

			if (displacement == float.MaxValue) {
				displacement = 0.0f;
			}

			if (displacement > Constants.EPSILON) {
				//Debug.Log (obj.name);
				//Debug.Log (displacement);
				if (voxComponent != null) {
					if (rigidbodies.Length > 0) {
//						Debug.Log (rigidbodies [0].name);
//						Debug.Log (rigidbodies [0].transform.position);
//						Debug.Log (Helper.VectorToParsable(voxComponent.displacement [rigidbodies[0].gameObject]));
						obj.transform.position = rigidbodies [0].transform.position - (obj.transform.rotation * voxComponent.displacement [rigidbodies[0].gameObject]);
						//Debug.Log (obj.name);
						//Debug.Log (obj.transform.position);
						//obj.transform.position = rigidbodies [0].transform.localPosition - voxComponent.displacement [rigidbodies[0].gameObject] +
						//	obj.transform.position;
						voxComponent.targetPosition = obj.transform.position;
						//						Debug.Log (Helper.VectorToParsable (rigidbodies [0].transform.position));
						//						Debug.Log (Helper.VectorToParsable (rigidbodies [0].transform.localPosition));
						//						Debug.Log (Helper.VectorToParsable (obj.transform.position));

						//Debug.Log (Helper.VectorToParsable (rigidbodies [0].transform.position));
						//Debug.Log (Helper.VectorToParsable (voxComponent.displacement [rigidbodies[0].name]));

						foreach (Rigidbody rigidbody in rigidbodies) {
							if (voxComponent.displacement.ContainsKey (rigidbody.gameObject)) {
								//								Debug.Log (rigidbody.name);
								rigidbody.transform.localPosition = voxComponent.displacement [rigidbody.gameObject];
							}
						}
					}
				}
			}

			// TODO: Abstract away
			if (voxComponent != null) {
				if (voxComponent.children != null) {
					foreach (Voxeme child in voxComponent.children) {
						if (child.isActiveAndEnabled) {
							if (child.gameObject != voxComponent.gameObject) {
								//						ResolvePhysicsPositionDiscepancies (child.gameObject);
								//						ResolvePhysicsRotationDiscepancies (child.gameObject);
								//Debug.Log ("Moving child: " + gameObject.name);
								child.transform.localPosition = voxComponent.parentToChildPositionOffset [child.gameObject];
								child.targetPosition = child.transform.position;
							}
						}
					}
				}
			}
		}

		public static float GetConcavityMinimum(GameObject obj) {
			Bounds bounds = Helper.GetObjectSize (obj);

			Vector3 concavityMin = bounds.min;
			foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>()) {
//				Debug.Log (renderer.gameObject.name + " " + Helper.GetObjectWorldSize (renderer.gameObject).min.y);
				if (Helper.GetObjectSize (renderer.gameObject).min.y > concavityMin.y) {
					concavityMin = Helper.GetObjectSize (renderer.gameObject).min;
				}
			}

			concavityMin = Helper.RotatePointAroundPivot (concavityMin, bounds.center, obj.transform.eulerAngles) + obj.transform.position;

//			Debug.Log (obj.transform.eulerAngles);
//			Debug.Log (concavityMin.y);
			return concavityMin.y;

			/*
			Bounds bounds = Helper.GetObjectWorldSize (obj);

			float concavityMinY = bounds.min.y;
			foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>()) {
				//Debug.Log (renderer.gameObject.name + " " + Helper.GetObjectWorldSize (renderer.gameObject).min.y);
				if (Helper.GetObjectWorldSize (renderer.gameObject).min.y > concavityMinY) {
					concavityMinY = Helper.GetObjectWorldSize (renderer.gameObject).min.y;
				}
			}

			return concavityMinY;
			 */
		}
	}

	/// <summary>
	/// RandomHelper class
	/// </summary>
	public static class RandomHelper {
		public enum RangeFlags
		{
			MinInclusive = 1,
			MaxInclusive = (1 << 1)
		}

		public static int RandomSign() {
			return (UnityEngine.Random.Range (0, 2) * 2) - 1;
		}

		public static Vector3 RandomAxis() {
			System.Random random = new System.Random ();
			return Constants.Axes[Constants.Axes.Keys.ToList()[random.Next(0,3)]];
		}

		public static int RandomInt(int min, int max, int flags = (int)RangeFlags.MinInclusive) {
			int rangeMin = min;
			int rangeMax = max;

			if ((flags & (int)RangeFlags.MinInclusive) == 0) {
				rangeMin = min + 1;
			}

			if (((flags & (int)RangeFlags.MaxInclusive) >> 1) == 1) {
				rangeMax = max + 1;
			}

			return UnityEngine.Random.Range(min, max);
		}

		public static float RandomFloat(float min, float max, int flags = (int)RangeFlags.MinInclusive) {
			float rangeMin = min;
			float rangeMax = max;

			if ((flags & (int)RangeFlags.MinInclusive) == 0) {
				rangeMin = min + Constants.EPSILON;
			}

			if (((flags & (int)RangeFlags.MaxInclusive) >> 1) == 1) {
				rangeMax = max + Constants.EPSILON;
			}

			return UnityEngine.Random.Range (min, max);
		}

		public static GameObject RandomVoxeme() {
			List<Voxeme> allVoxemes = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ().allVoxemes.ToList();

			Voxeme voxeme = allVoxemes [RandomInt (0, allVoxemes.Count, (int)RangeFlags.MinInclusive)];
			while (Helper.GetMostImmediateParentVoxeme(voxeme.gameObject).gameObject.transform.parent != null) {
				voxeme = allVoxemes [RandomInt (0, allVoxemes.Count, (int)RangeFlags.MinInclusive)];
			}

			return voxeme.gameObject;
		}

		public static GameObject RandomVoxeme(List<GameObject> exclude) {
			List<Voxeme> allVoxemes = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ().allVoxemes.ToList();

			Voxeme voxeme = allVoxemes [RandomInt (0, allVoxemes.Count, (int)RangeFlags.MinInclusive)];
			while ((Helper.GetMostImmediateParentVoxeme(voxeme.gameObject).gameObject.transform.parent != null) ||
				(exclude.Contains(voxeme.gameObject))) {
				voxeme = allVoxemes [RandomInt (0, allVoxemes.Count, (int)RangeFlags.MinInclusive)];
			}

			return voxeme.gameObject;
		}

		public static GameObject RandomVoxeme(List<GameObject> fromList, List<GameObject> exclude) {
			Voxeme voxeme = fromList [RandomInt (0, fromList.Count, (int)RangeFlags.MinInclusive)].GetComponent<Voxeme>();

			while ((Helper.GetMostImmediateParentVoxeme(voxeme.gameObject).gameObject.transform.parent != null) ||
				(exclude.Contains(voxeme.gameObject))) {
				voxeme = fromList [RandomInt (0, fromList.Count, (int)RangeFlags.MinInclusive)].GetComponent<Voxeme>();
			}

			return voxeme.gameObject;
		}
	}

	/// <summary>
	/// SceneHelper class
	/// </summary>
	public static class SceneHelper {
		public static IEnumerator LoadScene(string sceneName) {
			yield return null;

			AsyncOperation ao = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync (sceneName);
			ao.allowSceneActivation = true;

			while (!ao.isDone)
			{
				yield return null;
			}

			PluginImport commBridge = GameObject.Find ("CommunicationsBridge").GetComponent<PluginImport> ();
			commBridge.OpenPortInternal (commBridge.port);
		}
	}
}
