using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;
using Agent;
using Global;
using RCC;
using Satisfaction;
using Vox;

using RootMotion.FinalIK;

/// <summary>
/// Semantics of each predicate should be explicated within the method itself
/// Could have an issue when it comes to functions for predicates of multiple valencies?
/// *Cannot have objects or subobjects named the same as any of these predicates*
/// </summary>

public class Predicates : MonoBehaviour {
	public List<Triple<String,String,String>> rdfTriples = new List<Triple<String,String,String>>();
	public bool cameraRelativeDirections = true;

	public Timer waitTimer = new Timer ();

	EventManager eventManager;
	AStarSearch aStarSearch;
	ObjectSelector objSelector;
	RelationTracker relationTracker;
	Macros macros;

	public event EventHandler PrepareLog;

	public void OnPrepareLog(object sender, EventArgs e)
	{
		if (PrepareLog != null)
		{
			PrepareLog(this, e);
		}
	}

	public event EventHandler ParamsCalculated;

	public void OnParamsCalculated(object sender, EventArgs e)
	{
		if (ParamsCalculated != null)
		{
			ParamsCalculated(this, e);
		}
	}

	void Start () {
		eventManager = gameObject.GetComponent<EventManager> ();
		aStarSearch = GameObject.Find ("BlocksWorld").GetComponent<AStarSearch> ();
		objSelector = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ();
		relationTracker = GameObject.Find ("BehaviorController").GetComponent<RelationTracker> ();
		macros = GameObject.Find ("BehaviorController").GetComponent<Macros> ();
	}

	/// <summary>
	/// Relations
	/// </summary>

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 ON(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// on an object
			GameObject obj = ((GameObject)args [0]);
			Bounds bounds = new Bounds ();

			// check if object is concave
			bool isConcave = false;
			Voxeme voxeme = obj.GetComponent<Voxeme> ();
			if (voxeme != null) {
				isConcave = (voxeme.voxml.Type.Concavity.Contains("Concave"));
				isConcave = (isConcave && Vector3.Dot(obj.transform.up,Vector3.up) > 0.5f);
			}
			//Debug.Log (isConcave);

			if ((isConcave) && (Concavity.IsEnabled(obj))) {	// on concave object
				// get surface with concavity
				// which side is concavity on? - assume +Y for now
				bounds = Helper.GetObjectWorldSize (obj);

				/*float concavityMinY = bounds.min.y;
				foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>()) {
					Debug.Log (renderer.gameObject.name + " " + Helper.GetObjectWorldSize (renderer.gameObject).min.y);
					if (Helper.GetObjectWorldSize (renderer.gameObject).min.y > concavityMinY) {
						concavityMinY = Helper.GetObjectWorldSize (renderer.gameObject).min.y;
					}
				}*/

				// **check if concavity exposed
				// flip(plate), try to put object on

				outValue = new Vector3 (obj.transform.position.x,
					PhysicsHelper.GetConcavityMinimum(obj),//bounds.min.y,
					obj.transform.position.z);
			}
			else {	// on convex or flat object
				/*bounds = Helper.GetObjectWorldSize (obj);

				Debug.Log (Helper.VectorToParsable(bounds.center));
				Debug.Log (Helper.VectorToParsable(bounds.min));
				Debug.Log (Helper.VectorToParsable(bounds.max));*/

				bounds = Helper.GetObjectWorldSize (obj);

				outValue = new Vector3 (obj.transform.position.x,
					bounds.max.y,
					obj.transform.position.z);

				//GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Plane);
				//mark.transform.position = outValue;
				//mark.transform.localScale = new Vector3 (.07f, .07f, .07f);
				//mark.GetComponent<MeshCollider> ().enabled = false;

			}

			/*Voxeme voxComponent = (args [0] as GameObject).GetComponent<Voxeme> ();
			if (voxComponent.isGrasped) {
				outValue = (outValue +
					(voxComponent.graspTracker.position - voxComponent.gameObject.transform.position));
			}*/

			Debug.Log ("on: " + Helper.VectorToParsable (outValue));
		}
		else if (args [0] is Vector3) {	// on a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 IN(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// on an object
			GameObject obj = ((GameObject)args [0]);
			Bounds bounds = new Bounds ();

			// check if object is concave
			bool isConcave = false;
			Voxeme voxeme = obj.GetComponent<Voxeme> ();
			if (voxeme != null) {
				isConcave = (voxeme.voxml.Type.Concavity.Contains("Concave"));
				isConcave = (isConcave && Vector3.Dot(obj.transform.up,Vector3.up) > 0.5f);
			}
			//Debug.Log (isConcave);

			if (isConcave) {	// concavity activated
				// get surface with concavity
				// which side is concavity on? - assume +Y for now
//				bounds = Helper.GetObjectWorldSize (obj);
//
//				float concavityMinY = bounds.min.y;
//				foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>()) {
//					Debug.Log (renderer.gameObject.name + " " + Helper.GetObjectWorldSize (renderer.gameObject).min.y);
//					if (Helper.GetObjectWorldSize (renderer.gameObject).min.y > concavityMinY) {
//						concavityMinY = Helper.GetObjectWorldSize (renderer.gameObject).min.y;
//					}
//				}

				float concavityMinY = PhysicsHelper.GetConcavityMinimum (obj);
					
				outValue = new Vector3 (obj.transform.position.x,
					concavityMinY,
					obj.transform.position.z);
			}
			else {	// concavity deactivated
				outValue = new Vector3(float.NaN,float.NaN,float.NaN);
			}
			Debug.Log ("in: " + Helper.VectorToParsable (outValue));
		}
		else if (args [0] is Vector3) {	// on a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 AGAINST(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// against an object
			GameObject obj = ((GameObject)args [0]);
			Bounds bounds = new Bounds ();

			outValue = obj.transform.position;
			Debug.Log ("against: " + Helper.VectorToParsable (outValue));
		}
		else if (args [0] is Vector3) {	// against a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 OVER(object[] args)
	{
		return ((GameObject)args[0]).transform.position;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 UNDER(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// under an object
			GameObject obj = ((GameObject)args [0]);

			Bounds bounds = new Bounds ();

			bounds = Helper.GetObjectWorldSize (obj);

			outValue = new Vector3(obj.transform.position.x,bounds.min.y,obj.transform.position.z);

			Debug.Log ("under: " + Helper.VectorToParsable (outValue));
		}
		else if (args [0] is Vector3) {	// behind a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 TO(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// to an object
			GameObject obj = args [0] as GameObject;
			outValue = obj.transform.position;
		}
		else if (args [0] is Vector3) {	// to a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 FOR(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// for an object
			GameObject obj = args [0] as GameObject;
			outValue = obj.transform.position;
		}
		else if (args [0] is Vector3) {	// for a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 BEHIND(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// behind an object
			GameObject obj = ((GameObject)args [0]);

			Bounds bounds = new Bounds ();

			bounds = Helper.GetObjectWorldSize (obj);

			GameObject camera = GameObject.Find ("Main Camera");
			float povDir = cameraRelativeDirections ? camera.transform.eulerAngles.y : 0.0f;
			Vector3 rayStart = new Vector3 (0.0f, 0.0f,
				Mathf.Abs(bounds.size.z));
			rayStart = Quaternion.Euler (0.0f, povDir, 0.0f) * rayStart;
			rayStart += obj.transform.position;
			outValue = Helper.RayIntersectionPoint (rayStart, obj.transform.position-rayStart);

			Debug.Log ("behind: " + Helper.VectorToParsable (outValue));
		}
		else if (args [0] is Vector3) {	// behind a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 IN_FRONT(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// in front of an object
			GameObject obj = ((GameObject)args [0]);

			Bounds bounds = new Bounds ();

			bounds = Helper.GetObjectWorldSize (obj);

			GameObject camera = GameObject.Find ("Main Camera");
			float povDir = cameraRelativeDirections ? camera.transform.eulerAngles.y : 0.0f;
			Vector3 rayStart = new Vector3 (0.0f, 0.0f,
				Mathf.Abs(bounds.size.z));
			rayStart = Quaternion.Euler (0.0f, povDir+180.0f, 0.0f) * rayStart;
			rayStart += obj.transform.position;
			outValue = Helper.RayIntersectionPoint (rayStart, obj.transform.position-rayStart);

			Debug.Log ("in_front: " + Helper.VectorToParsable (outValue));
		}
		else if (args [0] is Vector3) {	// in front of a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 LEFT(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// left of an object
			GameObject obj = ((GameObject)args [0]);

			Bounds bounds = new Bounds ();

			bounds = Helper.GetObjectWorldSize (obj);

			GameObject camera = GameObject.Find ("Main Camera");
			float povDir = cameraRelativeDirections ? camera.transform.eulerAngles.y : 0.0f;
			Vector3 rayStart = new Vector3 (0.0f, 0.0f,
				Mathf.Abs(bounds.size.z));
			rayStart = Quaternion.Euler (0.0f, povDir+270.0f, 0.0f) * rayStart;
			rayStart += obj.transform.position;
			outValue = Helper.RayIntersectionPoint (rayStart, obj.transform.position-rayStart);

			Debug.Log ("left: " + Helper.VectorToParsable (outValue));
		}
		else if (args [0] is Vector3) {	// left of a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 RIGHT(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// right of an object
			GameObject obj = ((GameObject)args [0]);

			Bounds bounds = new Bounds ();

			bounds = Helper.GetObjectWorldSize (obj);

			GameObject camera = GameObject.Find ("Main Camera");
			float povDir = cameraRelativeDirections ? camera.transform.eulerAngles.y : 0.0f;
			Vector3 rayStart = new Vector3 (0.0f, 0.0f,
				Mathf.Abs(bounds.size.z));
			rayStart = Quaternion.Euler (0.0f, povDir+90.0f, 0.0f) * rayStart;
			rayStart += obj.transform.position;
			outValue = Helper.RayIntersectionPoint (rayStart, obj.transform.position-rayStart);

			Debug.Log ("left: " + Helper.VectorToParsable (outValue));
		}
		else if (args [0] is Vector3) {	// right of a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 LEFTDC(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// left of an object
			GameObject obj = ((GameObject)args [0]);

			Bounds bounds = new Bounds ();

			bounds = Helper.GetObjectWorldSize (obj);

			GameObject camera = GameObject.Find ("Main Camera");
			float povDir = cameraRelativeDirections ? camera.transform.eulerAngles.y : 0.0f;
			Vector3 rayStart = new Vector3 (0.0f, 0.0f,
				Mathf.Abs(bounds.size.z));
			rayStart = Quaternion.Euler (0.0f, povDir+270.0f, 0.0f) * rayStart;
			rayStart += obj.transform.position;
			outValue = Helper.RayIntersectionPoint (rayStart, obj.transform.position-rayStart);

			Debug.Log ("left-dc: " + Helper.VectorToParsable (outValue));
		}
		else if (args [0] is Vector3) {	// left of a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 RIGHTDC(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// right of an object
			GameObject obj = ((GameObject)args [0]);

			Bounds bounds = new Bounds ();

			bounds = Helper.GetObjectWorldSize (obj);

			GameObject camera = GameObject.Find ("Main Camera");
			float povDir = cameraRelativeDirections ? camera.transform.eulerAngles.y : 0.0f;
			Vector3 rayStart = new Vector3 (0.0f, 0.0f,
				Mathf.Abs(bounds.size.z));
			rayStart = Quaternion.Euler (0.0f, povDir+90.0f, 0.0f) * rayStart;
			rayStart += obj.transform.position;
			outValue = Helper.RayIntersectionPoint (rayStart, obj.transform.position-rayStart);

			Debug.Log ("left-dc: " + Helper.VectorToParsable (outValue));
		}
		else if (args [0] is Vector3) {	// right of a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 TOUCHING(object[] args)
	{
		List<string> manners = new List<string> () {"LEFT",
			"RIGHT",
			"BEHIND",
			"IN_FRONT",
			"ON"
		};
		Vector3 outValue = Vector3.zero;

		int selected = new System.Random ().Next (manners.Count);
		MethodInfo methodToCall = this.GetType ().GetMethod (manners[selected]);

		if ((methodToCall != null) &&  (rdfTriples.Count > 0)) {
			Debug.Log ("ExecuteCommand: invoke " + methodToCall.Name);
			object obj = methodToCall.Invoke (this, new object[]{ args });
			outValue = (Vector3)obj;
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 NEAR(object[] args)
	{
		Debug.Log (args [0].GetType ());
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {	// near an object
			GameObject obj = ((GameObject)args [0]);

			Voxeme voxComponent = obj.GetComponent<Voxeme> ();

			if (voxComponent != null) {
				Region region = new Region();
				Vector3 closestSurfaceBoundary = Vector3.zero;
				do {
					region = Helper.FindClearRegion (voxComponent.supportingSurface.transform.root.gameObject, obj);
					closestSurfaceBoundary = Helper.ClosestExteriorPoint(voxComponent.supportingSurface.transform.root.gameObject,region.center);
//				Debug.Log (Vector3.Distance (obj.transform.position, region.center));
//				Debug.Log (Vector3.Distance(closestSurfaceBoundary,region.center));
				}while(Vector3.Distance(obj.transform.position,region.center) > Vector3.Distance(closestSurfaceBoundary,region.center));
				outValue = region.center;
			}

		}
		else if (args [0] is Vector3) {	// near a location
			outValue = (Vector3)args[0];
		}

		return outValue;
	}

	/// <summary>
	/// Functions
	/// </summary>

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 CENTER(object[] args)
	{	// identical to TOP for now
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {
			GameObject obj = ((GameObject)args[0]);
			Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
			Bounds bounds = Helper.GetObjectWorldSize(obj);

			Debug.Log("center: " + bounds.max.y);

			//Debug.Log (bounds.ToString());
			//Debug.Log (obj.transform.position.ToString());
			outValue = new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 EDGE(object[] args)
	{	// identical to TOP for now
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {
			GameObject obj = ((GameObject)args[0]);
			Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
			Bounds bounds = Helper.GetObjectWorldSize(obj);

			List<Vector3> edges = new List<Vector3> () { new Vector3 (bounds.max.x, bounds.center.y, bounds.center.z),
				new Vector3 (bounds.center.x, bounds.center.y, bounds.max.z)
			};
			//Debug.Log (bounds.ToString());
			//Debug.Log (obj.transform.position.ToString());
			System.Random random = new System.Random();
			outValue = edges[random.Next (edges.Count)];
		}

		return outValue;
	}

	// IN: Object (single element array)
	// OUT: Location
	public Vector3 TOP(object[] args)
	{
		Vector3 outValue = Vector3.zero;
		if (args [0] is GameObject) {
			GameObject obj = ((GameObject)args[0]);
			Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
			Bounds bounds = new Bounds();
			
			foreach (Renderer renderer in renderers) {
				if (renderer.bounds.max.y > bounds.max.y) {
					bounds = renderer.bounds;
				}
			}
			Debug.Log("top: " + bounds.max.y);
			
			//Debug.Log (bounds.ToString());
			//Debug.Log (obj.transform.position.ToString());
			outValue = new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);
		}

		return outValue;
	}

	/// <summary>
	/// Attributes
	/// </summary>

	// IN: String
	// OUT: String
	public String BROWN(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			List<GameObject> attrObjs = objs.FindAll (o => o.GetComponent<AttributeSet> ().attributes.Contains ("brown"));

			if (attrObjs.Count > 0) {
				objName = attrObjs [0].name;
			}
		}

		return objName;
	}

	public String BLUE(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			List<GameObject> attrObjs = objs.FindAll (o => o.GetComponent<AttributeSet> ().attributes.Contains ("blue"));

			if (attrObjs.Count > 0) {
				objName = attrObjs [0].name;
			}
		}

		return objName;
	}

	public String BLACK(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			List<GameObject> attrObjs = objs.FindAll (o => o.GetComponent<AttributeSet> ().attributes.Contains ("black"));

			if (attrObjs.Count > 0) {
				objName = attrObjs [0].name;
			}
		}

		return objName;
	}

	public String GREEN(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			List<GameObject> attrObjs = objs.FindAll (o => o.GetComponent<AttributeSet> ().attributes.Contains ("green"));

			if (attrObjs.Count > 0) {
				objName = attrObjs [0].name;
			}
		}

		return objName;
	}

	public String YELLOW(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			List<GameObject> attrObjs = objs.FindAll (o => o.GetComponent<AttributeSet> ().attributes.Contains ("yellow"));

			if (attrObjs.Count > 0) {
				objName = attrObjs [0].name;
			}
		}

		return objName;
	}

	public String RED(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			List<GameObject> attrObjs = objs.FindAll (o => o.GetComponent<AttributeSet> ().attributes.Contains ("red"));

			if (attrObjs.Count > 0) {
				objName = attrObjs [0].name;
			}
		}
	
		return objName;
	}

	public String ORANGE(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			List<GameObject> attrObjs = objs.FindAll (o => o.GetComponent<AttributeSet> ().attributes.Contains ("orange"));

			if (attrObjs.Count > 0) {
				objName = attrObjs [0].name;
			}
		}

		return objName;
	}

	public String PINK(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			List<GameObject> attrObjs = objs.FindAll (o => o.GetComponent<AttributeSet> ().attributes.Contains ("pink"));

			if (attrObjs.Count > 0) {
				objName = attrObjs [0].name;
			}
		}

		return objName;
	}

	public String WHITE(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			List<GameObject> attrObjs = objs.FindAll (o => o.GetComponent<AttributeSet> ().attributes.Contains ("white"));

			if (attrObjs.Count > 0) {
				objName = attrObjs [0].name;
			}
		}

		return objName;
	}

	public String GRAY(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			List<GameObject> attrObjs = objs.FindAll (o => o.GetComponent<AttributeSet> ().attributes.Contains ("gray"));

			if (attrObjs.Count > 0) {
				objName = attrObjs [0].name;
			}
		}

		return objName;
	}

	public String PURPLE(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			List<GameObject> attrObjs = objs.FindAll (o => o.GetComponent<AttributeSet> ().attributes.Contains ("purple"));

			if (attrObjs.Count > 0) {
				objName = attrObjs [0].name;
			}
		}

		return objName;
	}

	public String BIG(object[] args) {
		String objName = "";
		GameObject obj = null;

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			obj = objs [0];

			foreach (GameObject o in objs) {
				if ((Helper.GetObjectWorldSize (o).size.x *
				    Helper.GetObjectWorldSize (o).size.y *
				    Helper.GetObjectWorldSize (o).size.z) >
				    (Helper.GetObjectWorldSize (obj).size.x *
				    Helper.GetObjectWorldSize (obj).size.y *
				    Helper.GetObjectWorldSize (obj).size.z)) {
					obj = o;
				}
			}
		}

		objName = obj.name;
		return objName;
	}

	public String SMALL(object[] args) {
		String objName = "";
		GameObject obj = null;

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList();
			obj = objs [0];

			foreach (GameObject o in objs) {
				if ((Helper.GetObjectWorldSize (o).size.x *
					Helper.GetObjectWorldSize (o).size.y *
					Helper.GetObjectWorldSize (o).size.z) <
					(Helper.GetObjectWorldSize (obj).size.x *
						Helper.GetObjectWorldSize (obj).size.y *
						Helper.GetObjectWorldSize (obj).size.z)) {
					obj = o;
				}
			}
		}

		objName = obj.name;
		return objName;
	}

	// IN: Objects
	// OUT: String
	public String THE(object[] args)
	{
		List<String> objNames = new List<String>();
		//System.Random random = new System.Random ();

		if (args [0] is GameObject) {	// assume all inputs are of same type
			//int index = random.Next(args.Length);
			for (int index = 0; index < args.Length; index++) {
				if (args [index] is GameObject) {
					objNames.Add ((args [index] as GameObject).name);
				}
			}
		}

		return string.Join(",",objNames.ToArray());
	}

	// IN: Objects
	// OUT: String
	public String A(object[] args)
	{
		String objName = "";
		System.Random random = new System.Random ();

		if (args [0] is GameObject) {	// assume all inputs are of same type
			int index = random.Next(args.Length);
			objName = (args [index] as GameObject).name;
		}

		return objName;
	}

	// IN: Objects
	// OUT: String
	public String TWO(object[] args)
	{
		//Debug.Log (args.Length);
		List<String> objNames = new List<String>();
		System.Random random = new System.Random ();

		if (args [0] is GameObject) {	// assume all inputs are of same type
			if (args.Length >= 2) {
				while (objNames.Count < 2) {
					int index = random.Next (args.Length);
					if (args [index].GetType () == args [0].GetType ()) {
						if (!objNames.Contains ((args [index] as GameObject).name)) {	// make sure all entries are distinct
							objNames.Add ((args [index] as GameObject).name);
						}
					}
				}
			}
		}

		//Debug.Log(string.Join(",",objNames.ToArray()));
		return string.Join(",",objNames.ToArray());
	}

	public String LEFTMOST(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList().OrderBy(o => o.transform.position.x).ToList();

			objName = objs [0].name;
		}

		return objName;
	}

	public String MIDDLE(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList().OrderBy(o => o.transform.position.x).ToList();

			objName = objs [(int)(objs.Count/2)].name;
		}

		return objName;
	}

	public String RIGHTMOST(object[] args) {
		String objName = "";

		if (args [0] is GameObject) {	// assume all inputs are of same type
			List<GameObject> objs = args.Cast<GameObject>().ToList().OrderBy(o => o.transform.position.x).ToList();

			objName = objs [objs.Count-1].name;
		}

		return objName;
	}

	// IN: Objects
	// OUT: String
	public String SELECTED(object[] args)
	{
		String objName = "";

		List<Voxeme> attrObjs = objSelector.allVoxemes.FindAll (v => v.gameObject.GetComponent<AttributeSet> ().attributes.Contains ("selected"));
		if (attrObjs.Count > 0) {
			objName = attrObjs [0].gameObject.name;
		}

		return objName;
	}

	/// <summary>
	/// Programs
	/// </summary>

	// IN: Objects, Location
	// OUT: none
	public void PUT(object[] args)
	{
		string originalEval = eventManager.events [0];

		Vector3 targetPosition = Vector3.zero;
		Vector3 targetRotation = Vector3.zero;
		Vector3 translocDir = Vector3.zero;
		float translocDist = 0.0f;
		string relOrientation = string.Empty;
		Vector3 relOffset = Vector3.zero;

		string prep = rdfTriples.Count > 0 ? rdfTriples [0].Item2.Replace ("put", "") : "";
		Debug.Log (prep);

		// look for agent
		GameObject agent = GameObject.FindGameObjectWithTag("Agent");

		// add agent-dependent preconditions
		if (agent != null) {
			// add preconditions
//			if (!SatisfactionTest.IsSatisfied (string.Format ("reach({0})", (args [0] as GameObject).name))) {
//				eventManager.InsertEvent (string.Format ("reach({0})", (args [0] as GameObject).name), 0);
//				eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 1);
//				eventManager.InsertEvent (eventManager.evalOrig [string.Format ("put({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 2);
//				eventManager.RemoveEvent (3);
//				return;
//			}
//			else {
				if (!SatisfactionTest.IsSatisfied (string.Format ("grasp({0})", (args [0] as GameObject).name))) {
					eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 0);
					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("put({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 1);
					eventManager.RemoveEvent (2);
					return;
				}
//			}
		}

		// add agent-independent preconditions
		if (prep == "_under") {
			if (args [0] is GameObject) {
				if ((args [0] as GameObject).GetComponent<Voxeme> () != null) {
					GameObject supportingSurface = (args [0] as GameObject).GetComponent<Voxeme> ().supportingSurface;
					if (supportingSurface != null) {
						//Debug.Log (rdfTriples [0].Item3);
						Bounds destBounds = Helper.GetObjectWorldSize (GameObject.Find (rdfTriples [0].Item3));
						destBounds.SetMinMax (destBounds.min + new Vector3 (Constants.EPSILON, 0.0f, Constants.EPSILON),
							destBounds.max - new Vector3 (Constants.EPSILON, Constants.EPSILON, Constants.EPSILON));
						//Debug.Log (Helper.VectorToParsable (bounds.min));
						//Debug.Log (Helper.VectorToParsable ((Vector3)args [1]));
						Bounds themeBounds = Helper.GetObjectWorldSize ((args [0] as GameObject));
						Vector3 min = (Vector3)args [1] - new Vector3 (0.0f, themeBounds.extents.y, 0.0f);
						Vector3 max = (Vector3)args [1] + new Vector3 (0.0f, themeBounds.extents.y, 0.0f);
						if ((min.y <= destBounds.min.y + Constants.EPSILON) && (max.y > destBounds.min.y + Constants.EPSILON)) {
							if (Mathf.Abs (Helper.GetObjectWorldSize (GameObject.Find (rdfTriples [0].Item3)).min.y -	// if no space between dest obj and dest obj's supporting surface
							   	Helper.GetObjectWorldSize (supportingSurface).max.y) < Constants.EPSILON) {
								Vector3 liftPos = GameObject.Find (rdfTriples [0].Item3).transform.position;
								liftPos += new Vector3 (0.0f, Helper.GetObjectWorldSize (args [0] as GameObject).size.y * 4, 0.0f);

								eventManager.InsertEvent (string.Format ("lift({0},{1})", GameObject.Find (rdfTriples [0].Item3).name,
									Helper.VectorToParsable (liftPos)), 0);

								Vector3 adjustedPosition = ((Vector3)args [1]);
								Debug.Log (adjustedPosition.y - (themeBounds.center.y - themeBounds.min.y));
								Debug.Log (Helper.GetObjectWorldSize(supportingSurface).max.y);
								if (adjustedPosition.y - (themeBounds.center.y - themeBounds.min.y) - ((args [0] as GameObject).transform.position.y - themeBounds.center.y) < 
									Helper.GetObjectWorldSize(supportingSurface).max.y) {	// if bottom of theme obj at this position is under the supporting surface's max
									adjustedPosition = new Vector3 (adjustedPosition.x,
									adjustedPosition.y + (themeBounds.center.y - themeBounds.min.y) + ((args [0] as GameObject).transform.position.y - themeBounds.center.y),
									adjustedPosition.z);
								}

								eventManager.InsertEvent (string.Format ("put({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable (adjustedPosition)), 1);
								eventManager.RemoveEvent (eventManager.events.Count - 1);
								eventManager.InsertEvent (string.Format ("put({0},on({1}))", rdfTriples [0].Item3, (args [0] as GameObject).name), 2);
								return;
							}
						}
					}
				}
			}
		}
		else if (prep == "_touching") {
			if (args [0] is GameObject) {
				if ((args [0] as GameObject).GetComponent<Voxeme> () != null) {
					List<string> manners = new List<string> () {"left",
						"right",
						"behind",
						"in_front",
						"on"
					};

					int selected = new System.Random ().Next (manners.Count);
					eventManager.InsertEvent (string.Format ("put({0},{1}({2}))", (args [0] as GameObject).name, manners[selected], rdfTriples [0].Item3), 1);
					eventManager.AbortEvent ();
					//eventManager.RemoveEvent (eventManager.events.Count - 1);

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {
							relOrientation = manners [selected];
							OnPrepareLog (this, new ParamsEventArgs ("RelOrientation", relOrientation));
						}
					}
					return;
				}
			}
		}


		if (agent != null) {
			// add agent-dependent postconditions
			if (args [args.Length - 1] is bool) {
				if ((bool)args [args.Length - 1] == true) {
					eventManager.InsertEvent (string.Format ("ungrasp({0})", (args [0] as GameObject).name), 1);
				}
			}
		}

		// override physics rigging
		foreach (object arg in args) {
			if (arg is GameObject) {
				Rigging rigging = (arg as GameObject).GetComponent<Rigging> ();
				if (rigging != null) {
					rigging.ActivatePhysics (false);
				}
			}
		}

		Helper.PrintRDFTriples (rdfTriples);

		if (prep == "_on") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					//Renderer[] renderers = obj.GetComponentsInChildren<Renderer> ();
					/*Bounds bounds = new Bounds ();
					
					foreach (Renderer renderer in renderers) {
						if (renderer.bounds.min.y - renderer.bounds.center.y < bounds.min.y - bounds.center.y) {
							bounds = renderer.bounds;
						}
					}*/

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					//Debug.Log (Helper.VectorToParsable(bounds.center));
					//Debug.Log (Helper.VectorToParsable(bounds.min));

					float yAdjust = (theme.transform.position.y - themeBounds.center.y);
					Debug.Log ("Y-size = " + (themeBounds.center.y - themeBounds.min.y));
					Debug.Log ("put_on: " + (theme.transform.position.y - themeBounds.min.y).ToString ());

					// compose computed on(a) into put(x,y) formula
					// if the glove don't fit, you must acquit! (recompute)
					Vector3 loc = ((Vector3)args [1]);	// computed coord of "on"
					Debug.Log(loc);

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {
							if (dest.GetComponent<Voxeme> ().voxml.Type.Concavity.Contains("Concave")) {	// putting on a concave object
								Bounds concavityBounds;
								if (dest.GetComponent<Voxeme> ().opVox.Type.Concavity.Item2 != null) {
									concavityBounds = Helper.GetObjectWorldSize (dest.GetComponent<Voxeme> ().opVox.Type.Concavity.Item2);
								}
								else {
									concavityBounds = destBounds;
								}
								if (!Helper.FitsIn (themeBounds, concavityBounds)) {
									loc = new Vector3 (dest.transform.position.x,
										concavityBounds.max.y,
										dest.transform.position.z);
									Debug.Log (destBounds.max.y);
								}
							}

							targetPosition = new Vector3 (loc.x,
								loc.y + (themeBounds.center.y - themeBounds.min.y) + yAdjust,
								loc.z);

							GameObject disablingObject;
							if ((theme.GetComponent<Voxeme> ().voxml.Type.Concavity.Contains("Concave")) &&	// this is a concave object
								(Concavity.IsEnabled (theme, loc, out disablingObject))) {
								if ((Mathf.Abs (Vector3.Dot (theme.transform.up, Vector3.up) + 1.0f) <= Constants.EPSILON) &&
								    (Helper.FitsIn (destBounds, themeBounds))) { // TODO: Run this through habitat verification
									// check if concavity is active
									Debug.Log (string.Format ("{0} upside down", theme.name));
									//Debug.Break ();
									if (disablingObject == dest) {
										if (themeBounds.size.y > destBounds.size.y) {
											targetPosition = new Vector3 (loc.x,
												loc.y + (themeBounds.center.y - themeBounds.min.y) - yAdjust - (destBounds.max.y - destBounds.min.y),
												loc.z);
											Debug.Log (Helper.VectorToParsable (targetPosition));
											//Debug.Break ();
											//flip(cup1);put(ball,under(cup1))
										}
										else {
											//Debug.Break ();
											Debug.Log (Helper.VectorToParsable (targetPosition));
											targetPosition = new Vector3 (loc.x,
												loc.y + (themeBounds.center.y - PhysicsHelper.GetConcavityMinimum (theme)) - yAdjust,
												loc.z);
											Debug.Log (Helper.VectorToParsable (targetPosition));
										}
									}
								}
							}
						} 
						else {
							targetPosition = loc;
						}

						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}
					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		} 
		else if (prep == "_in") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple in plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple in plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj

					//Debug.Log (Helper.VectorToParsable(bounds.center));
					//Debug.Log (Helper.VectorToParsable(bounds.min));

					float yAdjust = (theme.transform.position.y - themeBounds.center.y);
					Debug.Log ("Y-size = " + (themeBounds.center.y - themeBounds.min.y));
					Debug.Log ("put_in: " + (theme.transform.position.y - themeBounds.min.y).ToString ());

					// compose computed in(a) into put(x,y) formula
					Vector3 loc = ((Vector3)args [1]);	// coord of "in"
					if ((dest.GetComponent<Voxeme> ().voxml.Type.Concavity.Contains("Concave")) &&	// TODO: Run this through habitat verification
					    (Concavity.IsEnabled (dest)) && (Vector3.Dot (dest.transform.up, Vector3.up) > 0.5f)) {	// check if concavity is active
						if (!Helper.FitsIn (themeBounds, destBounds)) {	// if the glove don't fit, you must acquit! (rotate)
							// rotate to align longest major axis with container concavity axis
							Vector3 majorAxis = Helper.GetObjectMajorAxis (theme);
							Quaternion adjust = Quaternion.FromToRotation (theme.transform.rotation * majorAxis, Vector3.up);
							//Debug.Log (Helper.VectorToParsable (themeBounds.size));
							//Debug.Log (Helper.VectorToParsable (adjust * themeBounds.size));
							// create new test bounds with vector*quat
							Bounds testBounds = new Bounds (themeBounds.center, adjust * themeBounds.size);
							//if (args[args.Length-1] is bool) {
							//	if ((bool)args[args.Length-1] == true) {
							//		theme.GetComponent<Voxeme> ().targetRotation = Quaternion.LookRotation(majorAxis).eulerAngles;
							//	}
							//}
							if (Helper.FitsIn (testBounds, destBounds)) {	// check fit again
								targetRotation = Quaternion.FromToRotation (majorAxis, Vector3.up).eulerAngles;
							}
							else {	// if still won't fit, return garbage (NaN) rotation to signal that you can't do that
								targetRotation = new Vector3 (float.NaN, float.NaN, float.NaN);
							}
//							loc = new Vector3 (loc.x,
//								loc.y + (themeBounds.center.y - themeBounds.min.y) + yAdjust,
//								loc.z);
//							Debug.Log (destBounds.max.y);
						}
					} 
					else {
						targetRotation = new Vector3 (float.NaN, float.NaN, float.NaN);
					}

					if (!Helper.VectorIsNaN (targetRotation)) {
						if (args [args.Length - 1] is bool) {
							if ((bool)args [args.Length - 1] == false) {
								targetPosition = new Vector3 (loc.x,
									loc.y + (
										Mathf.Abs(new Bounds (themeBounds.center, Quaternion.Euler(targetRotation) * 
											Quaternion.Inverse(theme.transform.rotation) * themeBounds.size).center.y - 
											new Bounds (themeBounds.center, Quaternion.Euler(targetRotation) * 
											Quaternion.Inverse(theme.transform.rotation) * themeBounds.size).min.y)) + yAdjust,
									loc.z);
							} 
							else {
								targetPosition = loc;
							}
						}
					} 
					else {
						targetPosition = new Vector3 (float.NaN, float.NaN, float.NaN);
					}

					Debug.Log (Helper.VectorToParsable (targetPosition));

					if (voxComponent != null) {
						if (!voxComponent.enabled) {
							voxComponent.gameObject.transform.parent = null;
							voxComponent.enabled = true;
						}

						voxComponent.targetPosition = targetPosition;
						voxComponent.targetRotation = targetRotation;
					
						/*if (voxComponent.isGrasped) {
							voxComponent.targetPosition = voxComponent.targetPosition +
							(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
						}*/
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}

					if (voxComponent.turnSpeed == 0.0f) {
						voxComponent.turnSpeed = RandomHelper.RandomFloat (0.0f, 12.5f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		}
		else if (prep == "_under") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {

					// constraints for "under"
					// beneath dest obj (like other position relations)
					// on dest obj's supporting surface
					// no distance between dest and dest's support -> dest must be moved, displaced (precondition)
					// dest is concave -> theme can be placed in

					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple under plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					GameObject supportingSurface = dest.GetComponent<Voxeme>().supportingSurface;
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					//Debug.Log (Helper.VectorToParsable(bounds.center));
					//Debug.Log (Helper.VectorToParsable(bounds.min));

					float yAdjust = (theme.transform.position.y - themeBounds.center.y);
					Debug.Log ("Y-size = " + (themeBounds.max.y - themeBounds.center.y));
					Debug.Log ("put_under: " + (theme.transform.position.y - themeBounds.min.y).ToString ());

					// compose computed under(a) into put(x,y) formula
					Vector3 loc = ((Vector3)args [1]);	// coord of "under"

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {
							targetPosition = new Vector3 (loc.x,
								loc.y - (themeBounds.max.y - themeBounds.center.y) + yAdjust,
								loc.z);
						} 
						else {
							targetPosition = loc;
						}

						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		}
		else if (prep == "_behind") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					GameObject mainCamera = GameObject.Find ("Main Camera");
					float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

					float zAdjust = (theme.transform.position.z - themeBounds.center.z);

					Vector3 rayStart = new Vector3 (0.0f, 0.0f,
						                   Mathf.Abs (themeBounds.size.z));
					rayStart = Quaternion.Euler (0.0f, povDir + 180.0f, 0.0f) * rayStart;
					rayStart += theme.transform.position;
					Vector3 contactPoint = Helper.RayIntersectionPoint (rayStart, theme.transform.position-rayStart);

					Debug.Log ("Z-adjust = " + zAdjust);
					Debug.Log ("put_behind: " + Helper.VectorToParsable (contactPoint));

					Vector3 loc = ((Vector3)args [1]);	// coord of "behind"

					if (loc.y - themeBounds.extents.y < voxComponent.minYBound) {
						loc = new Vector3 (loc.x,voxComponent.minYBound+themeBounds.extents.y,loc.z);
					}

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {	// compute satisfaction condition
							Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x),
								              loc.y - (contactPoint.y - theme.transform.position.y),
								              loc.z - (contactPoint.z - theme.transform.position.z) + zAdjust) - loc;

							targetPosition = dir + loc;
						}
						else {
							targetPosition = loc;
						}

						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		} 
		else if (prep == "_in_front") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					GameObject mainCamera = GameObject.Find ("Main Camera");
					float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

					float zAdjust = (theme.transform.position.z - themeBounds.center.z);

					Vector3 rayStart = new Vector3 (0.0f, 0.0f,
						                   Mathf.Abs (themeBounds.size.z));
					rayStart = Quaternion.Euler (0.0f, povDir, 0.0f) * rayStart;
					rayStart += theme.transform.position;
					Vector3 contactPoint = Helper.RayIntersectionPoint (rayStart, theme.transform.position-rayStart);

					Debug.Log ("Z-adjust = " + zAdjust);
					Debug.Log ("put_in_front: " + Helper.VectorToParsable (contactPoint));

					Vector3 loc = ((Vector3)args [1]);	// coord of "in front"

					if (loc.y - themeBounds.extents.y < voxComponent.minYBound) {
						loc = new Vector3 (loc.x,voxComponent.minYBound+themeBounds.extents.y,loc.z);
					}

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {	// compute satisfaction condition
							Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x),
								              loc.y - (contactPoint.y - theme.transform.position.y),
								              loc.z - (contactPoint.z - theme.transform.position.z) + zAdjust) - loc;

							targetPosition = dir + loc;
						}
						else {
							targetPosition = loc;
						}

						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}
				
					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		} 
		else if (prep == "_left") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					GameObject mainCamera = GameObject.Find ("Main Camera");
					float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

					float xAdjust = (theme.transform.position.x - themeBounds.center.x);

					Vector3 rayStart = new Vector3 (0.0f, 0.0f,
						                   Mathf.Abs (themeBounds.size.z));
					rayStart = Quaternion.Euler (0.0f, povDir + 90.0f, 0.0f) * rayStart;
					rayStart += theme.transform.position;
					Vector3 contactPoint = Helper.RayIntersectionPoint (rayStart, theme.transform.position-rayStart);

					Debug.Log ("X-adjust = " + xAdjust);
					Debug.Log ("put_left: " + Helper.VectorToParsable (contactPoint));

					Vector3 loc = ((Vector3)args [1]);	// coord of "left"

					if (loc.y - themeBounds.extents.y < voxComponent.minYBound) {
						loc = new Vector3 (loc.x,voxComponent.minYBound+themeBounds.extents.y,loc.z);
					}

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {	// compute satisfaction condition
							Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x) + xAdjust,
								              loc.y - (contactPoint.y - theme.transform.position.y),
								              loc.z - (contactPoint.z - theme.transform.position.z)) - loc;

							targetPosition = dir + loc;
						} 
						else {
							targetPosition = loc;
						}

						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		}
		else if (prep == "_leftdc") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					GameObject mainCamera = GameObject.Find ("Main Camera");
					float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

					float xAdjust = (theme.transform.position.x - themeBounds.center.x)-
						(RandomHelper.RandomFloat(0.0f,1.0f)*(themeBounds.size.x*0.5f));

					Vector3 rayStart = new Vector3 (0.0f, 0.0f,
						Mathf.Abs (themeBounds.size.z));
					rayStart = Quaternion.Euler (0.0f, povDir + 90.0f, 0.0f) * rayStart;
					rayStart += theme.transform.position;
					Vector3 contactPoint = Helper.RayIntersectionPoint (rayStart, theme.transform.position-rayStart);

					Debug.Log ("X-adjust = " + xAdjust);
					Debug.Log ("put_leftdc: " + Helper.VectorToParsable (contactPoint));

					Vector3 loc = ((Vector3)args [1]);	// coord of "left"

					if (loc.y - themeBounds.extents.y < voxComponent.minYBound) {
						loc = new Vector3 (loc.x,voxComponent.minYBound+themeBounds.extents.y,loc.z);
					}

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {	// compute satisfaction condition
							Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x) + xAdjust,
								loc.y - (contactPoint.y - theme.transform.position.y),
								loc.z - (contactPoint.z - theme.transform.position.z)) - loc;

							targetPosition = dir + loc;
						} 
						else {
							targetPosition = loc;
						}

						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		}
		else if (prep == "_right") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					GameObject mainCamera = GameObject.Find ("Main Camera");
					float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

					float xAdjust = (theme.transform.position.x - themeBounds.center.x);

					Vector3 rayStart = new Vector3 (0.0f, 0.0f,
						                   Mathf.Abs (themeBounds.size.z));
					rayStart = Quaternion.Euler (0.0f, povDir + 270.0f, 0.0f) * rayStart;
					rayStart += theme.transform.position;
					Vector3 contactPoint = Helper.RayIntersectionPoint (rayStart, theme.transform.position-rayStart);

					Debug.Log ("X-adjust = " + xAdjust);
					Debug.Log ("put_right: " + Helper.VectorToParsable (contactPoint));

					Vector3 loc = ((Vector3)args [1]);	// coord of "left"

					if (loc.y - themeBounds.extents.y < voxComponent.minYBound) {
						loc = new Vector3 (loc.x,voxComponent.minYBound+themeBounds.extents.y,loc.z);
					}

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {
							Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x) + xAdjust,
								              loc.y - (contactPoint.y - theme.transform.position.y),
								              loc.z - (contactPoint.z - theme.transform.position.z)) - loc;

							targetPosition = dir + loc;
						} 
						else {
							targetPosition = loc;
						}
						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		}
		else if (prep == "_rightdc") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					GameObject mainCamera = GameObject.Find ("Main Camera");
					float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

					float xAdjust = (theme.transform.position.x - themeBounds.center.x)+
						(RandomHelper.RandomFloat(0.0f,1.0f)*(themeBounds.size.x*0.5f));

					Vector3 rayStart = new Vector3 (0.0f, 0.0f,
						Mathf.Abs (themeBounds.size.z));
					rayStart = Quaternion.Euler (0.0f, povDir + 270.0f, 0.0f) * rayStart;
					rayStart += theme.transform.position;
					Vector3 contactPoint = Helper.RayIntersectionPoint (rayStart, theme.transform.position-rayStart);

					Debug.Log ("X-adjust = " + xAdjust);
					Debug.Log ("put_rightdc: " + Helper.VectorToParsable (contactPoint));

					Vector3 loc = ((Vector3)args [1]);	// coord of "left"

					if (loc.y - themeBounds.extents.y < voxComponent.minYBound) {
						loc = new Vector3 (loc.x,voxComponent.minYBound+themeBounds.extents.y,loc.z);
					}

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {
							Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x) + xAdjust,
								loc.y - (contactPoint.y - theme.transform.position.y),
								loc.z - (contactPoint.z - theme.transform.position.z)) - loc;

							targetPosition = dir + loc;
						} 
						else {
							targetPosition = loc;
						}
						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		}
		else if (prep == "_near") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj

					Vector3 loc = ((Vector3)args [1]);	// coord of "near"

					float yAdjust = (theme.transform.position.y - themeBounds.center.y);

					targetPosition = new Vector3 (loc.x,
						loc.y + (themeBounds.center.y - themeBounds.min.y) + yAdjust,
						loc.z);

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {
							targetPosition = new Vector3 (loc.x, loc.y + (themeBounds.center.y - themeBounds.min.y) + yAdjust,
								loc.z);
						}
						else {
							targetPosition = loc;
						}
						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}

					translocDir = targetPosition - theme.transform.position;
					translocDist = Vector3.Magnitude(translocDir);
					relOffset = targetPosition - dest.transform.position;
				}
			}
		}
		else {
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj

					Vector3 loc = ((Vector3)args [1]);	// coord

					targetPosition = loc;
					//targetPosition = new Vector3(loc.x, loc.y + (themeBounds.center.y - themeBounds.min.y), loc.z);

					Debug.Log (Helper.VectorToParsable (targetPosition));

					Voxeme voxComponent = theme.GetComponent<Voxeme> ();
					if (voxComponent != null) {
						if (!voxComponent.enabled) {
							voxComponent.gameObject.transform.parent = null;
							voxComponent.enabled = true;
						}

						if (voxComponent.moveSpeed == 0.0f) {
							voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
						}
							
						RaycastHit[] hits = Physics.RaycastAll (
							new Vector3(targetPosition.x,targetPosition.y + Constants.EPSILON,
								targetPosition.z), -Constants.yAxis);
						List<RaycastHit> hitList = new List<RaycastHit> ((RaycastHit[])hits);
						hits = hitList.OrderBy (h => h.distance).ToArray ();

						GameObject supportingSurface = null;
						foreach (RaycastHit hit in hits) {
							if (hit.collider.gameObject.GetComponent<BoxCollider> () != null) {
								if ((!hit.collider.gameObject.GetComponent<BoxCollider> ().isTrigger) &&
									(!hit.collider.gameObject.transform.IsChildOf (gameObject.transform))) {
									if (!Helper.FitsIn (Helper.GetObjectWorldSize (hit.collider.gameObject),
										Helper.GetObjectWorldSize (gameObject), true)) {
										supportingSurface = hit.collider.gameObject;
										break;
									}
								}
							}
						}

						if (supportingSurface != null) {
							Debug.Log (targetPosition.y);
							Debug.Log ((themeBounds.center.y-themeBounds.min.y));
							Debug.Log (Helper.GetObjectWorldSize(supportingSurface).max.y);
							Debug.Log (supportingSurface.name);
							if (targetPosition.y - (themeBounds.center.y-themeBounds.min.y) < Helper.GetObjectWorldSize(supportingSurface).max.y) {
								targetPosition = new Vector3 (targetPosition.x,
									Helper.GetObjectWorldSize(supportingSurface).max.y + (themeBounds.center.y-themeBounds.min.y),
									targetPosition.z);
								Debug.Log (Helper.VectorToParsable (targetPosition));
							}
						}

						voxComponent.targetPosition = targetPosition;

						if (voxComponent.isGrasped) {
							voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
						}
					}
				}
			}
		}

		// update evalOrig dict
		string adjustedEval = "put(" + (args [0] as GameObject).name + "," + Helper.VectorToParsable (targetPosition) + ")";

		if (!eventManager.evalOrig.ContainsKey (adjustedEval)) {
			eventManager.evalOrig.Add (adjustedEval, eventManager.evalOrig [originalEval]);
			eventManager.evalOrig.Remove (originalEval);
			Debug.Log("Swapping " + originalEval + " for " + adjustedEval);
		}

		// add to events manager
		if (args[args.Length-1] is bool) {
			if ((bool)args[args.Length-1] == false) {
				//eventManager.eventsStatus.Add ("put("+(args [0] as GameObject).name+","+Helper.VectorToParsable(targetPosition)+")", false);
				eventManager.events[0] = "put("+(args [0] as GameObject).name+","+Helper.VectorToParsable(targetPosition)+")";
			}
			else{
				// record parameter values
				OnPrepareLog (this, new ParamsEventArgs ("TranslocSpeed", (args [0] as GameObject).GetComponent<Voxeme>().moveSpeed.ToString()));

				if (Vector3.Magnitude(translocDir) > 0.0f) {
					OnPrepareLog (this, new ParamsEventArgs ("TranslocDir", Helper.VectorToParsable (translocDir)));
					OnPrepareLog (this, new ParamsEventArgs ("RelOffset", Helper.VectorToParsable (relOffset)));
				}

				//				Debug.Log (eventManager.events [0]);
				//				Debug.Log (eventManager.evalOrig [eventManager.events [0]]);
				//if (eventManager.evalOrig.ContainsKey (eventManager.events [0])) {
				if ((Helper.GetTopPredicate(eventManager.lastParse) == Helper.GetTopPredicate(eventManager.events [0])) ||
					(PredicateParameters.IsSpecificationOf(Helper.GetTopPredicate(eventManager.events [0]),
						Helper.GetTopPredicate(eventManager.lastParse)))){
					OnParamsCalculated (null, null);
				}
				//}
			}
		}

		Debug.Log ("========== Before plan ========= " + targetPosition);

		// plan path to destination
		if (!Helper.VectorIsNaN (targetPosition)) { 
			if (aStarSearch.path.Count == 0) {
				aStarSearch.start = (args [0] as GameObject).transform.position;
				aStarSearch.goal = targetPosition;
				aStarSearch.PlanPath2(aStarSearch.start, aStarSearch.goal, out aStarSearch.path, (args [0] as GameObject), 
					GameObject.Find (rdfTriples [0].Item3) != null ? GameObject.Find (rdfTriples [0].Item3).GetComponent<Voxeme>() : null);

				foreach (Vector3 node in aStarSearch.path) {
					(args [0] as GameObject).GetComponent<Voxeme> ().interTargetPositions.Enqueue (node);
				}
			}
		}

		return;
	}

	// IN: Objects, Location
	// OUT: none
	public void MOVE(object[] args)
	{
		// override physics rigging
		foreach (object arg in args) {
			if (arg is GameObject) {
				Rigging rigging = (arg as GameObject).GetComponent<Rigging> ();
				if (rigging != null) {
					rigging.ActivatePhysics (false);
				}
			}
		}

		Vector3 targetPosition;

		Helper.PrintRDFTriples (rdfTriples);

		if (rdfTriples [0].Item2.Contains ("_to_top")) {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject obj = args [0] as GameObject;
					Renderer[] renderers = obj.GetComponentsInChildren<Renderer> ();
					Bounds bounds = new Bounds ();
					
					foreach (Renderer renderer in renderers) {
						if (renderer.bounds.min.y - renderer.bounds.center.y < bounds.min.y - bounds.center.y) {
							bounds = renderer.bounds;
						}
					}

					Debug.Log ("move_to_top: " + (bounds.center.y - bounds.min.y).ToString ());
					targetPosition = new Vector3 (((Vector3)args [1]).x,
					                              ((Vector3)args [1]).y + (bounds.center.y - bounds.min.y),
					                              ((Vector3)args [1]).z);

					Voxeme voxComponent = obj.GetComponent<Voxeme> ();
					if (voxComponent != null) {
						if (!voxComponent.enabled) {
							voxComponent.gameObject.transform.parent = null;
							voxComponent.enabled = true;
						}

						voxComponent.targetPosition = targetPosition;
					}
				}
			}
		}
		return;
	}

	// IN: Objects
	// OUT: none
	public void LIFT(object[] args)
	{
		// look for agent
		GameObject agent = GameObject.FindGameObjectWithTag("Agent");
		if (agent != null) {
			// add preconditions
//			if (!SatisfactionTest.IsSatisfied (string.Format ("reach({0})", (args [0] as GameObject).name))) {
//				eventManager.InsertEvent (string.Format ("reach({0})", (args [0] as GameObject).name), 0);
//				eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 1);
//				if (args.Length > 2) {
//					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("lift({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 1);
//				}
//				else {
//					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("lift({0})", (args [0] as GameObject).name)], 1);
//				}
//				eventManager.RemoveEvent (3);
//				return;
//			}
//			else {
				if (!SatisfactionTest.IsSatisfied (string.Format ("grasp({0})", (args [0] as GameObject).name))) {
					eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 0);
					if (args.Length > 2) {
						eventManager.InsertEvent (eventManager.evalOrig [string.Format ("lift({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 1);
					}
					else {
						eventManager.InsertEvent (eventManager.evalOrig [string.Format ("lift({0})", (args [0] as GameObject).name)], 1);
					}
					eventManager.RemoveEvent (2);
					return;
				}
//			}

			// add postconditions
//			if (args [args.Length - 1] is bool) {
//				if ((bool)args [args.Length - 1] == true) {
//					eventManager.InsertEvent (string.Format ("ungrasp({0})", (args [0] as GameObject).name), 1);
//				}
//			}
		}

		// override physics rigging
		foreach (object arg in args) {
			if (arg is GameObject) {
				Rigging rigging = (arg as GameObject).GetComponent<Rigging> ();
				if (rigging != null) {
					rigging.ActivatePhysics (false);
				}
			}
		}

		// unrig contained-but-not-supported objects
		foreach (DictionaryEntry pair in relationTracker.relations)
		{
			// support,contain cup1 ball
			if ((pair.Value as string).Contains ("contain") && (!(pair.Value as string).Contains ("support"))) {
				List<GameObject> objs = (pair.Key as List<GameObject>);
				if (objs [0] == (args [0] as GameObject)) {
					for (int i = 1; i < (pair.Key as List<GameObject>).Count; i++) {
						RiggingHelper.UnRig (objs [1], objs [0]);
						objs [1].GetComponent<Voxeme> ().targetPosition = objs [1].transform.position;
						objs [1].GetComponent<Voxeme> ().targetRotation = objs [1].transform.eulerAngles;
					}
				}
			}
		}

		Vector3 targetPosition = Vector3.zero;

		if (args [0] is GameObject) {
			GameObject obj = (args [0] as GameObject);
			Bounds bounds = Helper.GetObjectWorldSize (obj);
			Voxeme voxComponent = obj.GetComponent<Voxeme> ();
			if (voxComponent != null) {
				if (!voxComponent.enabled) {
					voxComponent.gameObject.transform.parent = null;
					voxComponent.enabled = true;
				}

				if (args [1] is Vector3) {
					targetPosition = (Vector3)args [1];
				}
				else {
					targetPosition = new Vector3 (obj.transform.position.x,
						obj.transform.position.y+bounds.size.y+RandomHelper.RandomFloat (0.0f, 0.5f, (int)RandomHelper.RangeFlags.MaxInclusive),
						obj.transform.position.z);
				}

				if (voxComponent.moveSpeed == 0.0f) {
					voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
				}
				voxComponent.targetPosition = targetPosition;
			}
		}


		// add to events manager
		if (args[args.Length-1] is bool) {
			if ((bool)args [args.Length - 1] == false) {
				eventManager.events [0] = "lift(" + (args [0] as GameObject).name + "," + Helper.VectorToParsable (targetPosition) + ")";
				Debug.Log (eventManager.events [0]);
			}
			else {
				// record parameter values
				OnPrepareLog (this, new ParamsEventArgs ("TranslocSpeed", (args [0] as GameObject).GetComponent<Voxeme>().moveSpeed.ToString()));
				OnPrepareLog (this, new ParamsEventArgs ("TranslocDir", Helper.VectorToParsable(targetPosition-(args [0] as GameObject).transform.position)));
				OnParamsCalculated (null, null);
			}
		}

		return;
	}

	// IN: Objects
	// OUT: none
	public void SLIDE(object[] args)
	{
		// look for agent
		GameObject agent = GameObject.FindGameObjectWithTag("Agent");
		if (agent != null) {
			// add preconditions
//			if (!SatisfactionTest.IsSatisfied (string.Format ("reach({0})", (args [0] as GameObject).name))) {
//				eventManager.InsertEvent (string.Format ("reach({0})", (args [0] as GameObject).name), 0);
//				eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 1);
//				if (args.Length > 2) {
//					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("slide({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 2);
//				}
//				else {
//					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("slide({0})", (args [0] as GameObject).name)], 2);
//				}
//				eventManager.RemoveEvent (3);
//				return;
//			}
//			else {
				if (!SatisfactionTest.IsSatisfied (string.Format ("grasp({0})", (args [0] as GameObject).name))) {
					eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 0);
					if (args.Length > 2) {
						eventManager.InsertEvent (eventManager.evalOrig [string.Format ("slide({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 2);
					}
					else {
						eventManager.InsertEvent (eventManager.evalOrig [string.Format ("slide({0})", (args [0] as GameObject).name)], 2);
					}
					eventManager.RemoveEvent (2);
					return;
				}
			//}

			// add postconditions
			if (args [args.Length - 1] is bool) {
				if ((bool)args [args.Length - 1] == true) {
					eventManager.InsertEvent (string.Format ("ungrasp({0})", (args [0] as GameObject).name), 1);
				}
			}
		}

		// override physics rigging
		if (agent != null) {
			foreach (object arg in args) {
				if (arg is GameObject) {
					Rigging rigging = (arg as GameObject).GetComponent<Rigging> ();
					if (rigging != null) {
						rigging.ActivatePhysics (false);
					}
				}
			}
		}

		Vector3 targetPosition = Vector3.zero;
		Vector3 translocDir = Vector3.zero;
		float translocDist = 0.0f;
		Vector3 relOffset = Vector3.zero;

		Helper.PrintRDFTriples (rdfTriples);

		string prep = rdfTriples.Count > 0 ? rdfTriples [0].Item2.Replace ("slide", "") : "";

		if (prep == "_behind") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					GameObject mainCamera = GameObject.Find ("Main Camera");
					float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

					float zAdjust = (theme.transform.position.z - themeBounds.center.z);

					Vector3 rayStart = new Vector3 (0.0f, 0.0f,
						Mathf.Abs (themeBounds.size.z));
					rayStart = Quaternion.Euler (0.0f, povDir + 180.0f, 0.0f) * rayStart;
					rayStart += theme.transform.position;
					Vector3 contactPoint = Helper.RayIntersectionPoint (rayStart, theme.transform.position-rayStart);

					Debug.Log ("Z-adjust = " + zAdjust);
					Debug.Log ("put_behind: " + Helper.VectorToParsable (contactPoint));

					Vector3 loc = ((Vector3)args [1]);	// coord of "behind"

					if (loc.y - themeBounds.extents.y < voxComponent.minYBound) {
						loc = new Vector3 (loc.x,voxComponent.minYBound+themeBounds.extents.y,loc.z);
					}

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {	// compute satisfaction condition
							Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x),
								loc.y - (contactPoint.y - theme.transform.position.y),
								loc.z - (contactPoint.z - theme.transform.position.z) + zAdjust) - loc;

							targetPosition = dir + loc;
						}
						else {
							targetPosition = loc;
						}

						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		} 
		else if (prep == "_in_front") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					GameObject mainCamera = GameObject.Find ("Main Camera");
					float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

					float zAdjust = (theme.transform.position.z - themeBounds.center.z);

					Vector3 rayStart = new Vector3 (0.0f, 0.0f,
						Mathf.Abs (themeBounds.size.z));
					rayStart = Quaternion.Euler (0.0f, povDir, 0.0f) * rayStart;
					rayStart += theme.transform.position;
					Vector3 contactPoint = Helper.RayIntersectionPoint (rayStart, theme.transform.position-rayStart);

					Debug.Log ("Z-adjust = " + zAdjust);
					Debug.Log ("put_in_front: " + Helper.VectorToParsable (contactPoint));

					Vector3 loc = ((Vector3)args [1]);	// coord of "in front"

					if (loc.y - themeBounds.extents.y < voxComponent.minYBound) {
						loc = new Vector3 (loc.x,voxComponent.minYBound+themeBounds.extents.y,loc.z);
					}

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {	// compute satisfaction condition
							Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x),
								loc.y - (contactPoint.y - theme.transform.position.y),
								loc.z - (contactPoint.z - theme.transform.position.z) + zAdjust) - loc;

							targetPosition = dir + loc;
						}
						else {
							targetPosition = loc;
						}

						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		} 
		else if (prep == "_left") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					GameObject mainCamera = GameObject.Find ("Main Camera");
					float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

					float xAdjust = (theme.transform.position.x - themeBounds.center.x);

					Vector3 rayStart = new Vector3 (0.0f, 0.0f,
						Mathf.Abs (themeBounds.size.z));
					rayStart = Quaternion.Euler (0.0f, povDir + 90.0f, 0.0f) * rayStart;
					rayStart += theme.transform.position;
					Vector3 contactPoint = Helper.RayIntersectionPoint (rayStart, theme.transform.position-rayStart);

					Debug.Log ("X-adjust = " + xAdjust);
					Debug.Log ("put_left: " + Helper.VectorToParsable (contactPoint));

					Vector3 loc = ((Vector3)args [1]);	// coord of "left"

					if (loc.y - themeBounds.extents.y < voxComponent.minYBound) {
						loc = new Vector3 (loc.x,voxComponent.minYBound+themeBounds.extents.y,loc.z);
					}

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {	// compute satisfaction condition
							Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x) + xAdjust,
								loc.y - (contactPoint.y - theme.transform.position.y),
								loc.z - (contactPoint.z - theme.transform.position.z)) - loc;

							targetPosition = dir + loc;
						} 
						else {
							targetPosition = loc;
						}

						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		} 
		else if (prep == "_right") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj
					Bounds destBounds = Helper.GetObjectWorldSize (dest);	// bounds of dest obj => alter to get interior enumerated by VoxML structure

					GameObject mainCamera = GameObject.Find ("Main Camera");
					float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

					float xAdjust = (theme.transform.position.x - themeBounds.center.x);

					Vector3 rayStart = new Vector3 (0.0f, 0.0f,
						Mathf.Abs (themeBounds.size.z));
					rayStart = Quaternion.Euler (0.0f, povDir + 270.0f, 0.0f) * rayStart;
					rayStart += theme.transform.position;
					Vector3 contactPoint = Helper.RayIntersectionPoint (rayStart, theme.transform.position-rayStart);

					Debug.Log ("X-adjust = " + xAdjust);
					Debug.Log ("put_right: " + Helper.VectorToParsable (contactPoint));

					Vector3 loc = ((Vector3)args [1]);	// coord of "left"

					if (loc.y - themeBounds.extents.y < voxComponent.minYBound) {
						loc = new Vector3 (loc.x,voxComponent.minYBound+themeBounds.extents.y,loc.z);
					}

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {
							Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x) + xAdjust,
								loc.y - (contactPoint.y - theme.transform.position.y),
								loc.z - (contactPoint.z - theme.transform.position.z)) - loc;

							targetPosition = dir + loc;
						} 
						else {
							targetPosition = loc;
						}
						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;

							/*if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
								(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}*/
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
			}
		}
		else if (prep == "_near") {	// fix for multiple RDF triples
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);	// get destination obj ("plate" in "put apple on plate")
					Voxeme voxComponent = theme.GetComponent<Voxeme> ();

					Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj

					Vector3 loc = ((Vector3)args [1]);	// coord of "near"

					float yAdjust = (theme.transform.position.y - themeBounds.center.y);

					targetPosition = new Vector3 (loc.x,
						loc.y + (themeBounds.center.y - themeBounds.min.y) + yAdjust,
						loc.z);

					if (args [args.Length - 1] is bool) {
						if ((bool)args [args.Length - 1] == false) {
							targetPosition = new Vector3 (loc.x, loc.y + (themeBounds.center.y - themeBounds.min.y) + yAdjust,
								loc.z);
						}
						else {
							targetPosition = loc;
						}
						Debug.Log (Helper.VectorToParsable (targetPosition));

						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							voxComponent.targetPosition = targetPosition;
						}
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}

					translocDir = targetPosition - theme.transform.position;
					translocDist = Vector3.Magnitude(translocDir);
					relOffset = targetPosition - dest.transform.position;
				}
			}
		}
		else { 
			if (args [0] is GameObject) {
				if (agent == null) {
					GameObject obj = (args [0] as GameObject);
					Voxeme voxComponent = obj.GetComponent<Voxeme> ();
					if (voxComponent != null) {
						if (!voxComponent.enabled) {
							voxComponent.gameObject.transform.parent = null;
							voxComponent.enabled = true;
						}

						if (args [1] is Vector3) {
							targetPosition = (Vector3)args [1];
						}
						else {
							targetPosition = new Vector3 (obj.transform.position.x + UnityEngine.Random.insideUnitSphere.x,
								obj.transform.position.y, obj.transform.position.z + UnityEngine.Random.insideUnitSphere.z);
						}

						voxComponent.targetPosition = targetPosition;
					}

					if (voxComponent.moveSpeed == 0.0f) {
						voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
				else {
					if (args [1] is Vector3) {
						GameObject theme = args [0] as GameObject;	// get theme obj ("apple" in "put apple on plate")
						Bounds themeBounds = Helper.GetObjectWorldSize (theme);	// bounds of theme obj

						Vector3 loc = ((Vector3)args [1]);	// coord

						targetPosition = loc;
						//targetPosition = new Vector3(loc.x, loc.y + (themeBounds.center.y - themeBounds.min.y), loc.z);

						Debug.Log (Helper.VectorToParsable (targetPosition));

						Voxeme voxComponent = theme.GetComponent<Voxeme> ();
						if (voxComponent != null) {
							if (!voxComponent.enabled) {
								voxComponent.gameObject.transform.parent = null;
								voxComponent.enabled = true;
							}

							if (voxComponent.moveSpeed == 0.0f) {
								voxComponent.moveSpeed = RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
							}

							RaycastHit[] hits = Physics.RaycastAll (
								new Vector3(targetPosition.x,targetPosition.y + Constants.EPSILON,
									targetPosition.z), -Constants.yAxis);
							List<RaycastHit> hitList = new List<RaycastHit> ((RaycastHit[])hits);
							hits = hitList.OrderBy (h => h.distance).ToArray ();

							GameObject supportingSurface = null;
							foreach (RaycastHit hit in hits) {
								if (hit.collider.gameObject.GetComponent<BoxCollider> () != null) {
									if ((!hit.collider.gameObject.GetComponent<BoxCollider> ().isTrigger) &&
										(!hit.collider.gameObject.transform.IsChildOf (gameObject.transform))) {
										if (!Helper.FitsIn (Helper.GetObjectWorldSize (hit.collider.gameObject),
											Helper.GetObjectWorldSize (gameObject), true)) {
											supportingSurface = hit.collider.gameObject;
											break;
										}
									}
								}
							}

							if (supportingSurface != null) {
								Debug.Log (targetPosition.y);
								Debug.Log ((themeBounds.center.y-themeBounds.min.y));
								Debug.Log (Helper.GetObjectWorldSize(supportingSurface).max.y);
								Debug.Log (supportingSurface.name);
								if (targetPosition.y - (themeBounds.center.y-themeBounds.min.y) < Helper.GetObjectWorldSize(supportingSurface).max.y) {
									targetPosition = new Vector3 (targetPosition.x,
										Helper.GetObjectWorldSize(supportingSurface).max.y + (themeBounds.center.y-themeBounds.min.y),
										targetPosition.z);
									Debug.Log (Helper.VectorToParsable (targetPosition));
								}
							}

							voxComponent.targetPosition = targetPosition;

							if (voxComponent.isGrasped) {
								voxComponent.targetPosition = voxComponent.targetPosition +
									(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
							}
						}
					}
				}
			}
		}

		// add to events manager
		if (args[args.Length-1] is bool) {
			if ((bool)args [args.Length - 1] == false) {
				eventManager.events [0] = "slide(" + (args [0] as GameObject).name + "," + Helper.VectorToParsable (targetPosition) + ")";
				Debug.Log (eventManager.events [0]);
			}
			else {
				// record parameter values
				OnPrepareLog (this, new ParamsEventArgs ("TranslocSpeed", (args [0] as GameObject).GetComponent<Voxeme>().moveSpeed.ToString()));
				OnPrepareLog (this, new ParamsEventArgs ("TranslocDir", Helper.VectorToParsable (targetPosition-(args [0] as GameObject).transform.position)));
				OnParamsCalculated (null, null);
			}
		}

		// plan path to destination
		if (!Helper.VectorIsNaN (targetPosition)) { 
			if (aStarSearch.path.Count == 0) {
				Bounds surfaceBounds = Helper.GetObjectWorldSize ((args [0] as GameObject).GetComponent<Voxeme> ().supportingSurface);
				Bounds objBounds = Helper.GetObjectWorldSize (args [0] as GameObject);
				Bounds embeddingSpaceBounds = new Bounds ();
				embeddingSpaceBounds.SetMinMax (new Vector3 (surfaceBounds.min.x+(objBounds.size.x/2), surfaceBounds.max.y, surfaceBounds.min.z+(objBounds.size.z/2)),
					new Vector3 (surfaceBounds.max.x, objBounds.max.y, surfaceBounds.max.z));

				aStarSearch.start = (args [0] as GameObject).transform.position;
				aStarSearch.goal = targetPosition;
				aStarSearch.PlanPath2 (aStarSearch.start, aStarSearch.goal, out aStarSearch.path, (args [0] as GameObject), 
					GameObject.Find (rdfTriples [0].Item3) != null ? GameObject.Find (rdfTriples [0].Item3).GetComponent<Voxeme>() : null, "Y");

				foreach (Vector3 node in aStarSearch.path) {
					(args [0] as GameObject).GetComponent<Voxeme> ().interTargetPositions.Enqueue (node);
				}
			}
		}

		return;
	}

	// IN: Objects, Location
	// OUT: none
	public void ROLL(object[] args)
	{
		// look for agent
		GameObject agent = GameObject.FindGameObjectWithTag("Agent");

		// add agent-dependent preconditions
		if (agent != null) {
			if (!SatisfactionTest.IsSatisfied (string.Format ("reach({0})", (args [0] as GameObject).name))) {
				eventManager.InsertEvent (string.Format ("reach({0})", (args [0] as GameObject).name), 0);
				eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 1);
				if (args.Length > 2) {
					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("roll({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 1);
				} else {
					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("roll({0})", (args [0] as GameObject).name)], 1);
				}
				eventManager.RemoveEvent (3);
				return;
			}
			else if (!SatisfactionTest.IsSatisfied (string.Format ("grasp({0})", (args [0] as GameObject).name))) {
				eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 0);
				if (args.Length > 2) {
					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("roll({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 1);
				} else {
					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("roll({0})", (args [0] as GameObject).name)], 1);
				}
				eventManager.RemoveEvent (2);
				return;
			}
		}

		// check and see if rigidbody orientations and main body orientations are getting out of sync
		// due to physics effects
		//if (args [args.Length - 1] is bool) {
		//	if ((bool)args [args.Length - 1] == false) {
				//PhysicsHelper.ResolvePhysicsDiscepancies (args [0] as GameObject);
		//	}
		//}
		// find the smallest displacement angle between an axis on the main body and an axis on this rigidbody
		/*float displacementAngle = 360.0f;
		Quaternion rigidbodyRotation = Quaternion.identity;
		Rigidbody[] rigidbodies = (args [0] as GameObject).GetComponentsInChildren<Rigidbody> ();
		foreach (Rigidbody rigidbody in rigidbodies) {
			foreach (Vector3 mainBodyAxis in Constants.Axes.Values) {
				foreach (Vector3 rigidbodyAxis in Constants.Axes.Values) {
					if (Vector3.Angle ((args [0] as GameObject).transform.rotation * mainBodyAxis, rigidbody.rotation * rigidbodyAxis) < displacementAngle) {
						displacementAngle = Vector3.Angle ((args [0] as GameObject).transform.rotation * mainBodyAxis, rigidbody.rotation * rigidbodyAxis);
						rigidbodyRotation = rigidbody.rotation;
					}
				}
			}
		}

		// if rigidbody is out of sync
		if (displacementAngle > Mathf.Rad2Deg * Constants.EPSILON) {
			Vector3 relativeDisplacement = (rigidbodyRotation * Quaternion.Inverse ((args [0] as GameObject).transform.rotation)).eulerAngles;
			Debug.Log (string.Format ("Displacement: {0}", relativeDisplacement));

			Quaternion resolve = Quaternion.identity;
			Quaternion resolveInv = Quaternion.identity;
			Voxeme voxComponent = (args [0] as GameObject).GetComponent<Voxeme> ();
			if (voxComponent != null) {
				foreach (Rigidbody rigidbody in rigidbodies) {
					if ((voxComponent.displacement.ContainsKey (rigidbody.gameObject)) && (voxComponent.rotationalDisplacement.ContainsKey (rigidbody.gameObject))) {
						// initial = initial rotational displacement
						Quaternion initial = Quaternion.Euler (voxComponent.rotationalDisplacement [rigidbody.gameObject]);
						Debug.Log (initial.eulerAngles);
						// current = current rotational displacement due to physics
						Quaternion current = rigidbody.transform.localRotation;// * Quaternion.Inverse ((args [0] as GameObject).transform.rotation));
						Debug.Log (current.eulerAngles);
						// resolve = rotation to get from initial rotational displacement to current rotational displacement
						resolve = current * Quaternion.Inverse (initial);
						Debug.Log (resolve.eulerAngles);
						//Debug.Log ((initial * resolve).eulerAngles);
						Debug.Log ((resolve * initial).eulerAngles);
						// resolveInv = rotation to get from final (current rigidbody) rotation back to initial (aligned with main obj) rotation
						resolveInv = initial * Quaternion.Inverse (current);
						//Debug.Log (resolveInv.eulerAngles);
						//rigidbody.transform.rotation = obj.transform.rotation * initial;
						rigidbody.transform.localRotation = initial;// * (args [0] as GameObject).transform.rotation;
						Debug.Log (rigidbody.transform.rotation.eulerAngles);

						//rigidbody.transform.localPosition = voxComponent.displacement [rigidbody.name];
						//rigidbody.transform.position = (args [0] as GameObject).transform.position + voxComponent.displacement [rigidbody.name];
					}
				}

				//Debug.Break ();

				//Debug.Log (resolve.eulerAngles);
				Debug.Log (Helper.VectorToParsable (rigidbodies [0].transform.position));
				//Debug.Log (Helper.VectorToParsable (rigidbodies [0].transform.localPosition));
				Debug.Log (Helper.VectorToParsable ((args [0] as GameObject).transform.position));
				(args [0] as GameObject).transform.position = rigidbodies [0].transform.position;// - voxComponent.displacement [rigidbodies[0].name];
				voxComponent.targetPosition = (args [0] as GameObject).transform.position;
				Debug.Log (Helper.VectorToParsable ((args [0] as GameObject).transform.position));

				Debug.Log (Helper.VectorToParsable (rigidbodies [0].transform.position));
				//Debug.Log (Helper.VectorToParsable (voxComponent.displacement [rigidbodies[0].name]));

				foreach (Rigidbody rigidbody in rigidbodies) {
					if ((voxComponent.displacement.ContainsKey (rigidbody.gameObject)) && (voxComponent.rotationalDisplacement.ContainsKey (rigidbody.gameObject))) {
						Debug.Log (rigidbody.name);
						rigidbody.transform.localPosition = voxComponent.displacement [rigidbody.gameObject];
					}
				}
			
				Debug.Log (Helper.VectorToParsable ((args [0] as GameObject).transform.position));
				Debug.Log (Helper.VectorToParsable (rigidbodies [0].transform.localPosition));

				Debug.Log ((args [0] as GameObject).transform.rotation.eulerAngles);
				foreach (Rigidbody rigidbody in rigidbodies) {
					Debug.Log (Helper.VectorToParsable (rigidbody.transform.localPosition));
				}

				(args [0] as GameObject).transform.rotation = resolve * (args [0] as GameObject).transform.rotation;
				voxComponent.targetRotation = (args [0] as GameObject).transform.rotation.eulerAngles;
				Debug.Log ((args [0] as GameObject).transform.rotation.eulerAngles);

				//Debug.Break ();
			}
		}*/

		Debug.Log (Helper.VectorToParsable((args [0] as GameObject).transform.position));

		// calc object properties
		float diameter = Helper.GetObjectWorldSize ((args [0] as GameObject)).size.y;	// bounds sphere diameter = world size.y
		float circumference = Mathf.PI * diameter;	// circumference = pi*diameter
		float revs = 0;

		// get the path
		Vector3 offset = Vector3.zero;

		while (offset.magnitude <= 0.5f * circumference) {
			offset = new Vector3 (UnityEngine.Random.insideUnitSphere.x, 0.0f, UnityEngine.Random.insideUnitSphere.z);	// random by default
			revs = offset.magnitude / circumference;	// # revolutions = path length/circumference
		}

		if (args [1] is Vector3) {
			Debug.Log ((Vector3)args [1]);
			Debug.Log ((args [0] as GameObject).transform.position);
			offset = ((Vector3)args [1]) - (args [0] as GameObject).transform.position;
			offset = new Vector3 (offset.x, 0.0f, offset.z);
		}
		Debug.Log (string.Format("Offset: {0}",offset));
		Debug.Log (offset.magnitude);
//		System.Random rand = new System.Random();
//		if (rand.Next(0, 2) == 0)
//			offset = new Vector3 (0.0f,0.0f,1.0f);
//		else
//			offset = new Vector3 (-1.0f,0.0f,0.0f);
		//offset = new Vector3 (0.5f,0.0f,0.5f);

		// compute axis of rotation
		Vector3 planeNormal = Constants.yAxis;	// TODO: compute normal of surface
		Vector3 worldRotAxis = Vector3.Cross (offset.normalized, planeNormal);

		// rotate object such that an axis of rotation is perpendicular to the intended path and coplanar with the surface (TODO assuming surface normal of Y-axis for now)
		// determine axis of symmetry from VoxML
		Vector3 objRotAxis = Vector3.zero;
		float angleToRot = 360.0f;
		Debug.Log (worldRotAxis);

//		Debug.Log (worldRotAxis);
//
//		if ((args [0] as GameObject).GetComponent<Voxeme> () != null) {
//			if (!(args [0] as GameObject).GetComponent<Voxeme> ().enabled) {
//				(args [0] as GameObject).GetComponent<Voxeme> ().gameObject.transform.parent = null;
//				(args [0] as GameObject).GetComponent<Voxeme> ().enabled = true;
//			}
//
//			foreach (string s in (args [0] as GameObject).GetComponent<Voxeme> ().opVox.Type.RotatSym) {
//				if (Vector3.Angle ((args [0] as GameObject).transform.rotation * Constants.Axes [s], worldRotAxis) < angleToRot) {
//					angleToRot = Vector3.Angle ((args [0] as GameObject).transform.rotation * Constants.Axes [s], worldRotAxis);
//					objRotAxis = Constants.Axes [s];
//				}
//				Debug.Log ((args [0] as GameObject).transform.rotation * objRotAxis);
//				//Debug.Log (angleToRot);
//			}
//		}

		// add agent-independent preconditions
		if (args [0] is GameObject) {
			GameObject obj = (args [0] as GameObject);
			Voxeme voxComponent = obj.GetComponent<Voxeme> ();
			if (voxComponent != null) {
				if (!voxComponent.enabled) {
					voxComponent.gameObject.transform.parent = null;
					voxComponent.enabled = true;
				}

				foreach (string s in voxComponent.opVox.Type.RotatSym) {
					if (Constants.Axes.ContainsKey (s)) {
						if (Vector3.Angle (obj.transform.rotation * Constants.Axes [s], worldRotAxis) < angleToRot) {
							angleToRot = Vector3.Angle (obj.transform.rotation * Constants.Axes [s], worldRotAxis);
							objRotAxis = Constants.Axes [s];
							//objRotAxis = obj.transform.rotation * Constants.Axes [s];
						}
					}
					else {
						voxComponent.targetPosition = new Vector3 (float.NaN, float.NaN, float.NaN);
						return;
					}
				}

				//Debug.Break ();
				Debug.Log (obj.transform.rotation * objRotAxis);
				Debug.Log (angleToRot);

				//Debug.Log (Quaternion.FromToRotation (objRotAxis, worldRotAxis).eulerAngles);

				if (!SatisfactionTest.IsSatisfied (string.Format ("turn({0},{1},{2})", (args [0] as GameObject).name,
					    Helper.VectorToParsable (objRotAxis), Helper.VectorToParsable (worldRotAxis)))) {
					Debug.Log (string.Format ("turn({0},{1},{2})", (args [0] as GameObject).name,
						Helper.VectorToParsable (obj.transform.rotation * objRotAxis), Helper.VectorToParsable (worldRotAxis)));
					eventManager.InsertEvent (string.Format ("turn({0},{1},{2})", (args [0] as GameObject).name,
						Helper.VectorToParsable (objRotAxis), Helper.VectorToParsable (worldRotAxis)), 0);
					Debug.Log (string.Format ("roll({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((args [0] as GameObject).transform.position + offset)));
					eventManager.InsertEvent (string.Format ("roll({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((args [0] as GameObject).transform.position + offset)), 1);
					eventManager.RemoveEvent (eventManager.events.Count - 1);

					// update subobject rigidbody rotations
					// TODO: UpdateSubObjectRigidbodyRotations()
//					Rigidbody[] rigidbodies = obj.gameObject.GetComponentsInChildren<Rigidbody> ();
//					foreach (Rigidbody rigidbody in rigidbodies) {
//						if (voxComponent.staticStateRotations.ContainsKey (rigidbody.name)) {
//							Debug.Log(rigidbody.name);
//							Debug.Log(rigidbody.rotation.eulerAngles);
//							voxComponent.staticStateRotations [rigidbody.name] = rigidbody.rotation.eulerAngles;
//						}
//					}
					// record parameter values
					OnPrepareLog (this, new ParamsEventArgs ("TranslocDir", Helper.VectorToParsable (offset)));
					OnParamsCalculated (null, null);
					return;
				}
				else {
					Debug.Log ("Turn already satisfied");
				}
			}
		}

		if (agent != null) {
			// add agent-dependent postconditions
			if (args [args.Length - 1] is bool) {
				if ((bool)args [args.Length - 1] == true) {
					eventManager.InsertEvent (string.Format ("ungrasp({0})", (args [0] as GameObject).name), 1);
				}
			}
		}

		// override physics rigging
		// TODO: for programs with implied surfaces in the VoxML encoding, don't deactivate physics?
//		foreach (object arg in args) {
//			if (arg is GameObject) {
//				(arg as GameObject).GetComponent<Rigging> ().ActivatePhysics(false);
//			}
//		}

		Vector3 targetPosition = Vector3.zero;
		Quaternion targetRotation = Quaternion.identity;

		Helper.PrintRDFTriples (rdfTriples);

		string prep = rdfTriples.Count > 0 ? rdfTriples [0].Item2.Replace ("roll", "") : "";

		if (args [0] is GameObject) {
			GameObject obj = (args [0] as GameObject);
			Voxeme voxComponent = obj.GetComponent<Voxeme> ();
			if (voxComponent != null) {
				if (!voxComponent.enabled) {
					voxComponent.gameObject.transform.parent = null;
					voxComponent.enabled = true;
				}

				targetRotation = obj.transform.rotation;

				if (args [1] is Vector3) {
					targetPosition  = (Vector3)args [1];
				}
				else {
					targetPosition = obj.transform.position + offset;
				}

				// calculate how many revolutions object will make
				float degrees = -180.0f * revs;
				//Debug.Log (degrees);

				//Vector3 transverseAxis = Quaternion.AngleAxis (90.0f, Vector3.up) * offset.normalized;	// the axis parallel to the surface

				//Debug.Log (worldRotAxis);

				while (degrees < -90.0f) {
					targetRotation = Quaternion.AngleAxis (-80.0f, worldRotAxis) * targetRotation;
					//Debug.Log (targetRotation.eulerAngles);
					voxComponent.interTargetRotations.Enqueue (targetRotation.eulerAngles);
					degrees += 90.0f;
				}

				targetRotation = Quaternion.AngleAxis (degrees, worldRotAxis) * targetRotation;
				//Debug.Log (targetRotation.eulerAngles);
				voxComponent.interTargetRotations.Enqueue (targetRotation.eulerAngles);
				//	}
				//}


				if (voxComponent.moveSpeed == 0.0f) {
					voxComponent.moveSpeed = 1.0f;//RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
				}
				// calculate turnSpeed (angular velocity)
				// estimate where obj will be next time step
				Vector3 normalizedOffset = offset.normalized;
				Vector3 lookAheadPos = new Vector3 (obj.transform.position.x - normalizedOffset.x * Time.deltaTime * voxComponent.moveSpeed,
					obj.transform.position.y - normalizedOffset.y * Time.deltaTime * voxComponent.moveSpeed,
					obj.transform.position.z - normalizedOffset.z * Time.deltaTime * voxComponent.moveSpeed);
				// appox distance to be traveled over next timestep
				float distPerTimestep = (lookAheadPos - obj.transform.position).magnitude;
				//Debug.Log(distPerTimestep);
				// estimate approx timesteps to traverse path
				float time = (offset.magnitude*Time.deltaTime/distPerTimestep);
				//Debug.Log(time);
				// velocity = dist/time
				float vel = offset.magnitude/time;
				//Debug.Log(vel);
				// w = v/d
				float angularVelocity = vel / Helper.GetObjectWorldSize (obj).size.y;
				//Debug.Log(angularVelocity);

				voxComponent.targetPosition = targetPosition;
				voxComponent.targetRotation = targetRotation.eulerAngles;
				voxComponent.turnSpeed = angularVelocity;
			}
		}

		// add to events manager
		if (args[args.Length-1] is bool) {
			if ((bool)args[args.Length-1] == false) {
				eventManager.events[0] = "roll("+(args [0] as GameObject).name+","+Helper.VectorToParsable(targetPosition)+")";
				//Debug.Log (eventManager.events [0]);
			}
		}

		return;
	}

	// IN: Objects
	// OUT: none
	public void FLIP(object[] args)
	{
		// override physics rigging
		foreach (object arg in args) {
			if (arg is GameObject) {
				Rigging rigging = (arg as GameObject).GetComponent<Rigging> ();
				if (rigging != null) {
					rigging.ActivatePhysics (false);
				}
			}
		}

		System.Random random = new System.Random ();

		Vector3 targetRotation = Vector3.zero;
		Vector3 objAxis = Vector3.zero;
		Vector3 worldAxis = Vector3.zero;
		Vector3 targetDir = Vector3.zero;
		Vector3 objRotAxis = Vector3.zero;
		Vector3 worldRotAxis = Vector3.zero;

		Helper.PrintRDFTriples (rdfTriples);

		if (args [0] is GameObject) {
			GameObject obj = args [0] as GameObject;

			Voxeme voxComponent = obj.GetComponent<Voxeme> ();

			if (args [1] is Vector3) {
				Debug.Log(Helper.GetObjectWorldSize (obj).max.y);
				Debug.Log(((Vector3)args [1]).y);
				if (Mathf.Abs(((Vector3)args [1]).y-Helper.GetObjectWorldSize (obj).max.y) < Constants.EPSILON) {
					// flip at center

					// take any axis of rotational symmetry and reverse it
					// if no such axis exists, pick any axis

					List<Vector3> rotatSymAxes = new List<Vector3> ();
					foreach (string s in voxComponent.opVox.Type.RotatSym) {
						if (Constants.Axes.ContainsKey (s)) {
							rotatSymAxes.Add (Constants.Axes [s]);
						}
					}

					if (rotatSymAxes.Count == 0) {
						foreach (Vector3 vec in Constants.Axes.Values) {
							rotatSymAxes.Add(vec);
						}
					}

					objAxis = rotatSymAxes [random.Next (rotatSymAxes.Count)];
					worldAxis = objAxis;
					targetDir = -worldAxis;

					List<Vector3> normalAxes = new List<Vector3> ();
					foreach (Vector3 vec in Constants.Axes.Values) {
						if (vec != worldAxis) {
							normalAxes.Add (vec);
						}
					}
					objRotAxis = normalAxes [random.Next (normalAxes.Count)];
					worldRotAxis = objRotAxis;
				}
				else {
					// flip on edge

					List<Vector3> rotatSymAxes = new List<Vector3> ();
					foreach (string s in voxComponent.opVox.Type.RotatSym) {
						if (Constants.Axes.ContainsKey (s)) {
							rotatSymAxes.Add (Constants.Axes [s]);
						}
					}

					if (rotatSymAxes.Count == 0) {
						foreach (Vector3 vec in Constants.Axes.Values) {
							rotatSymAxes.Add(vec);
						}
					}

					objAxis = rotatSymAxes [random.Next (rotatSymAxes.Count)];
					worldAxis = obj.transform.rotation * objAxis;

					targetDir = (obj.transform.position-(Vector3)args[1]).normalized;

					objRotAxis = Vector3.Cross (objAxis, targetDir);
					worldRotAxis = Vector3.Cross (worldAxis, targetDir);
				}
			}
			else {
				// take any axis of rotational symmetry and reverse it
				// if no such axis exists, pick any axis

				List<Vector3> rotatSymAxes = new List<Vector3> ();
				foreach (string s in voxComponent.opVox.Type.RotatSym) {
					if (Constants.Axes.ContainsKey (s)) {
						rotatSymAxes.Add (Constants.Axes [s]);
					}
				}

				if (rotatSymAxes.Count == 0) {
					foreach (Vector3 vec in Constants.Axes.Values) {
						rotatSymAxes.Add(vec);
					}
				}

				objAxis = rotatSymAxes [random.Next (rotatSymAxes.Count)];
				worldAxis = objAxis;
				targetDir = -worldAxis;

				List<Vector3> normalAxes = new List<Vector3> ();
				foreach (Vector3 vec in Constants.Axes.Values) {
					if (vec != worldAxis) {
						normalAxes.Add (vec);
					}
				}
				objRotAxis = normalAxes [random.Next (normalAxes.Count)];
				worldRotAxis = objRotAxis;
			}

			if (voxComponent != null) {
				if (!voxComponent.enabled) {
					voxComponent.gameObject.transform.parent = null;
					voxComponent.enabled = true;
				}

				//voxComponent.targetRotation = targetRotation;
			}
		}

		// add to events manager
		if (args[args.Length-1] is bool) {
			if ((bool)args[args.Length-1] == false) {
				//eventManager.eventsStatus.Add ("flip("+(args [0] as GameObject).name+","+Helper.VectorToParsable(targetRotation)+")", false);
				//eventManager.events[0] = "flip("+(args [0] as GameObject).name+","+Helper.VectorToParsable(targetRotation)+")";
				eventManager.events [0] = string.Format ("turn({0},{1},{2},{3})", (args [0] as GameObject).name,
					Helper.VectorToParsable (worldAxis),
					Helper.VectorToParsable ((args [0] as GameObject).transform.rotation * targetDir),
					Helper.VectorToParsable ((args [0] as GameObject).transform.rotation * worldRotAxis));
					//flip("+(args [0] as GameObject).name+","+Helper.VectorToParsable(targetRotation)+")";

				// record parameter values
				Dictionary<string,string> paramValues = PredicateParameters.InitPredicateParametersCollection();
				Debug.Log (Helper.VectorToParsable (objAxis));
				Debug.Log (Helper.VectorToParsable (objRotAxis));
				Debug.Log(Mathf.Abs(Vector3.Angle(objRotAxis, -Constants.xAxis)));
				Debug.Log(Mathf.Abs(Vector3.Angle(objRotAxis, Constants.xAxis)));
				Debug.Log(Mathf.Abs(Vector3.Angle(objRotAxis, -Constants.zAxis)));
				Debug.Log(Mathf.Abs(Vector3.Angle(objRotAxis, Constants.zAxis)));
				Debug.Log(Mathf.Rad2Deg * Constants.EPSILON);
				OnPrepareLog (this, new ParamsEventArgs ("SymmetryAxis", Constants.Axes.FirstOrDefault (a => 
					(Helper.AngleCloseEnough(objAxis,a.Value) || Helper.AngleCloseEnough(-objAxis,a.Value))).Key.ToString()));
				KeyValuePair<string,Vector3> rotAxis = Constants.Axes.FirstOrDefault (a => 
					(Helper.AngleCloseEnough (objRotAxis, a.Value) || Helper.AngleCloseEnough (-objRotAxis, a.Value)));
				if (rotAxis.Value == Vector3.zero) {
					rotAxis = Constants.Axes.FirstOrDefault(a => a.Value == Constants.xAxis);
				}
				OnPrepareLog (this, new ParamsEventArgs ("RotAxis", rotAxis.Key.ToString()));
				OnParamsCalculated (null, null);
				return;
			}
		}

		return;
	}

	// IN: Objects
	// OUT: none
	public void TURN(object[] args)
	{
		string originalEval = eventManager.events [0];

		Vector3 targetRotation = Vector3.zero;
		float sign = 1.0f;
		float angle = 0.0f;
		string rotAxis = "";

		// look for agent
		GameObject agent = null;//GameObject.FindGameObjectWithTag("Agent");
		if (agent != null) {
			// add preconditions
			if (!SatisfactionTest.IsSatisfied (string.Format ("grasp({0})", (args [0] as GameObject).name))) {
				eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 0);
				if (args.Length > 4) {
					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("turn({0},{1},{2},{3})", (args [0] as GameObject).name, 
						Helper.VectorToParsable ((Vector3)args [1]), Helper.VectorToParsable ((Vector3)args [2]),
						Helper.VectorToParsable ((Vector3)args [3]))], 1);
				}
				else if (args.Length > 3) {
					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("turn({0},{1},{2})", (args [0] as GameObject).name, 
						Helper.VectorToParsable ((Vector3)args [1]), Helper.VectorToParsable ((Vector3)args [2]))], 1);
				}
				else if (args.Length > 2) {
					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("turn({0},{1})", (args [0] as GameObject).name, 
						Helper.VectorToParsable ((Vector3)args [1]))], 1);
				}
				else {
					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("turn({0})", (args [0] as GameObject).name)], 1);
				}	
				eventManager.RemoveEvent (2);
				return;
			}
//			if (!SatisfactionTest.IsSatisfied (string.Format ("reach({0})", (args [0] as GameObject).name))) {
//				eventManager.InsertEvent (string.Format ("reach({0})", (args [0] as GameObject).name), 0);
//				eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 1);
//				if (args.Length > 2) {
//					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("turn({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 1);
//				}
//				else {
//					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("turn({0})", (args [0] as GameObject).name)], 1);
//				}
//				eventManager.RemoveEvent (3);
//				return;
//			}
//			else {
//				if (!SatisfactionTest.IsSatisfied (string.Format ("grasp({0})", (args [0] as GameObject).name))) {
//					eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 0);
//					if (args.Length > 2) {
//						eventManager.InsertEvent (eventManager.evalOrig [string.Format ("turn({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 1);
//					}
//					else {
//						eventManager.InsertEvent (eventManager.evalOrig [string.Format ("turn({0})", (args [0] as GameObject).name)], 1);
//					}
//					eventManager.RemoveEvent (2);
//					return;
//				}
//			}

			// add postconditions
			if (args [args.Length - 1] is bool) {
				if ((bool)args [args.Length - 1] == true) {
					eventManager.InsertEvent (string.Format ("ungrasp({0})", (args [0] as GameObject).name), 1);
				}
			}
		}

//		if (args [args.Length - 1] is bool) {
//			if ((bool)args [args.Length - 1] == true) {
//				// resolve subobject rigidbody rotations
//				// TODO: ResolveSubObjectRigidbodyRotations()
//				Debug.Log ((args [0] as GameObject).name);
//				Debug.Log ((args [0] as GameObject).transform.rotation.eulerAngles);
//				Rigidbody[] rigidbodies = (args [0] as GameObject).GetComponentsInChildren<Rigidbody> ();
//				Quaternion resolveInv = Quaternion.identity;
//				foreach (Rigidbody rigidbody in rigidbodies) {
//					if ((args [0] as GameObject).GetComponent<Voxeme> ().staticStateRotations.ContainsKey (rigidbody.name)) {
//						Debug.Log (rigidbody.name);
//						Quaternion initial = Quaternion.Euler ((args [0] as GameObject).GetComponent<Voxeme> ().staticStateRotations [rigidbody.name]);
//						Debug.Log (initial.eulerAngles);
//						Quaternion final = rigidbody.rotation;
//						Debug.Log (final.eulerAngles);
//						// resolve = rotation to get from final resting orientation back to initial orientation before physics effects
//						Quaternion resolve = final * Quaternion.Inverse (initial);
//						Debug.Log (resolve.eulerAngles);
//						resolveInv = initial * Quaternion.Inverse (final);
//						rigidbody.MoveRotation (resolve * rigidbody.rotation);
//					}
//				}
//
//				(args [0] as GameObject).transform.rotation = resolveInv * (args [0] as GameObject).transform.rotation;
//			}
//		}

		// override physics rigging
		foreach (object arg in args) {
			if (arg is GameObject) {
				Rigging rigging = (arg as GameObject).GetComponent<Rigging> ();
				if (rigging != null) {
					rigging.ActivatePhysics (false);
				}
			}
		}

		Helper.PrintRDFTriples (rdfTriples);

		string prep = rdfTriples.Count > 0 ? rdfTriples [0].Item2.Replace ("turn", "") : "";

		if (args [0] is GameObject) {
			GameObject obj = (args [0] as GameObject);
			Voxeme voxComponent = obj.GetComponent<Voxeme> ();
			if (voxComponent != null) {
				if (!voxComponent.enabled) {
					voxComponent.gameObject.transform.parent = null;
					voxComponent.enabled = true;
				}

				if (args [1] is Vector3 && args [2] is Vector3){
					// args[1] is local space axis
					// args[2] is world space axis
					if (args [3] is Vector3) {
						// args[3] is world space axis
						Debug.Log ((Vector3)args [1]);
						Debug.Log (obj.transform.rotation*(Vector3)args [1]);
						Debug.Log ((Vector3)args [2]);
						Debug.Log ((Vector3)args [3]);
						Debug.Log (obj.transform.rotation*(Vector3)args [3]);
						//Vector3 cross = Vector3.Cross (obj.transform.rotation * (Vector3)args [1], (Vector3)args [2]);

						// sign = direction of rotation = cross product of (local space) axis being tracked and (local space) target axis
						//float sign = Mathf.Sign (Vector3.Cross (obj.transform.rotation * (Vector3)args [1], Quaternion.Inverse(obj.transform.rotation) * (Vector3)args [2]).y);


						//sign = Mathf.Sign (Vector3.Cross (obj.transform.rotation * (Vector3)args [1], (Vector3)args [2]).y);
						sign = Mathf.Sign (Vector3.Dot(Vector3.Cross (obj.transform.rotation * (Vector3)args [1], (Vector3)args [2]), (Vector3)args[3]));

						Debug.Log (Vector3.Dot(Vector3.Cross (obj.transform.rotation * (Vector3)args [1], (Vector3)args [2]), (Vector3)args[3]));
						Debug.Log (sign * (Vector3)args [2]);
						angle = Vector3.Angle (obj.transform.rotation * (Vector3)args [1], (Vector3)args [2]);
						Debug.Log (angle);
						Debug.Log ((Quaternion.AngleAxis (angle, (Vector3)args [3]).eulerAngles));
						Debug.Log ((Quaternion.Inverse(obj.transform.rotation) * Quaternion.AngleAxis (angle, (Vector3)args [3])).eulerAngles);
						Debug.Log ((Quaternion.AngleAxis (angle, (Vector3)args [3]) * Quaternion.Inverse(obj.transform.rotation)).eulerAngles);
						Debug.Log ((Quaternion.AngleAxis (angle, (Vector3)args [3]) * obj.transform.rotation).eulerAngles);
						Debug.Log ((obj.transform.rotation * Quaternion.AngleAxis (angle, (Vector3)args [3])).eulerAngles);

						// rotation from object axis [1] to world axis [2] around world axis [3]

						if (voxComponent.turnSpeed == 0.0f) {
							voxComponent.turnSpeed = RandomHelper.RandomFloat (0.0f, 12.5f, (int)RandomHelper.RangeFlags.MaxInclusive);
						}

						targetRotation = (Quaternion.AngleAxis (sign * angle, (Vector3)args [3]) * obj.transform.rotation).eulerAngles;
						rotAxis = Constants.Axes.FirstOrDefault (a => a.Value == (Vector3)args [3]).Key;
						Debug.Log (targetRotation);
					}
					else {
						Debug.Log ((Vector3)args [1]);
						Debug.Log (obj.transform.rotation * (Vector3)args [1]);
						Debug.Log ((Vector3)args [2]);

						// rotation from object axis[1] to world axis [2]

						if (voxComponent.turnSpeed == 0.0f) {
							voxComponent.turnSpeed = RandomHelper.RandomFloat (0.0f, 12.5f, (int)RandomHelper.RangeFlags.MaxInclusive);
						}

						targetRotation = Quaternion.FromToRotation((Vector3)args [1], (Vector3)args [2]).eulerAngles;
						angle = Vector3.Angle ((Vector3)args [1], (Vector3)args [2]);
						//targetRotation = Quaternion.LookRotation(obj.transform.rotation * (Vector3)args [1],(Vector3)args [2]).eulerAngles;
					}
				}
				else {
					if (voxComponent.turnSpeed == 0.0f) {
						voxComponent.turnSpeed = RandomHelper.RandomFloat (0.0f, 12.5f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}

					targetRotation = (obj.transform.rotation * UnityEngine.Random.rotation).eulerAngles;
					angle = Quaternion.Angle(transform.rotation,Quaternion.Euler(targetRotation));
				}

				voxComponent.targetRotation = targetRotation;
				Debug.Log (Helper.VectorToParsable (voxComponent.targetRotation));
			}
		}

		// add to events manager
		if (args[args.Length-1] is bool) {
			if ((bool)args[args.Length-1] == false) {
				if (args [1] is Vector3 && args [2] is Vector3) {
					if (args [3] is Vector3) {
						eventManager.events [0] = "turn(" + (args [0] as GameObject).name + "," + Helper.VectorToParsable ((Vector3)args [1]) +
							"," + Helper.VectorToParsable ((Vector3)args [2]) + "," + Helper.VectorToParsable ((Vector3)args [3]) + ")";
					}
					else {
						eventManager.events [0] = "turn(" + (args [0] as GameObject).name + "," + Helper.VectorToParsable ((Vector3)args [1]) +
							"," + Helper.VectorToParsable ((Vector3)args [2]) + ")";
					}
				}
				else {
					eventManager.events [0] = "turn(" + (args [0] as GameObject).name + "," +
						Helper.VectorToParsable ((args [0] as GameObject).transform.rotation * Constants.yAxis) + "," +
						Helper.VectorToParsable ((args [0] as GameObject).transform.rotation * Quaternion.Euler(targetRotation) * Constants.yAxis) + ")";
				}
				Debug.Log (eventManager.events [0]);

				// record parameter values
				OnPrepareLog (this, new ParamsEventArgs ("RotSpeed", (args [0] as GameObject).GetComponent<Voxeme>().turnSpeed.ToString()));

				if (angle > 0.0f) {
					OnPrepareLog (this, new ParamsEventArgs ("RotAngle", angle.ToString()));
					OnPrepareLog (this, new ParamsEventArgs ("RotDir", sign.ToString ()));
				}

				if (rotAxis != string.Empty) {
					OnPrepareLog (this, new ParamsEventArgs ("RotAxis", rotAxis));
				}

				Debug.Log (eventManager.events [0]);

				//if (eventManager.evalOrig.ContainsKey (eventManager.events [0])) {
					if ((Helper.GetTopPredicate(eventManager.lastParse) == Helper.GetTopPredicate(eventManager.events [0])) ||
						(PredicateParameters.IsSpecificationOf(Helper.GetTopPredicate(eventManager.events [0]),
						Helper.GetTopPredicate(eventManager.lastParse)))){
						OnParamsCalculated (null, null);
					}
				//}
			}
		}

		return;
	}

	// IN: Objects
	// OUT: none
	public void SPIN(object[] args)
	{
		// look for agent
//		GameObject agent = GameObject.FindGameObjectWithTag("Agent");
//		if (agent != null) {
//			// add preconditions
//			if (!SatisfactionTest.IsSatisfied (string.Format ("reach({0})", (args [0] as GameObject).name))) {
//				eventManager.InsertEvent (string.Format ("reach({0})", (args [0] as GameObject).name), 0);
//				eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 1);
//				if (args.Length > 2) {
//					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("spin({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 1);
//				}
//				else {
//					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("spin({0})", (args [0] as GameObject).name)], 1);
//				}
//				eventManager.RemoveEvent (3);
//				return;
//			}
//			else {
//				if (!SatisfactionTest.IsSatisfied (string.Format ("grasp({0})", (args [0] as GameObject).name))) {
//					eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 0);
//					if (args.Length > 2) {
//						eventManager.InsertEvent (eventManager.evalOrig [string.Format ("spin({0},{1})", (args [0] as GameObject).name, Helper.VectorToParsable ((Vector3)args [1]))], 1);
//					}
//					else {
//						eventManager.InsertEvent (eventManager.evalOrig [string.Format ("spin({0})", (args [0] as GameObject).name)], 1);
//					}
//					eventManager.RemoveEvent (2);
//					return;
//				}
//			}
//
//			// add postconditions
//			if (args [args.Length - 1] is bool) {
//				if ((bool)args [args.Length - 1] == true) {
//					eventManager.InsertEvent (string.Format ("ungrasp({0})", (args [0] as GameObject).name), 1);
//				}
//			}
//		}

		Quaternion targetRotation = Quaternion.identity;
		List<Vector3> orientations = new List<Vector3> ();
		Vector3 trackAxis = Vector3.zero;
		Vector3 worldRotAxis = Vector3.zero;
		Vector3 objRotAxis = Vector3.zero;
		float angle = 0.0f;
		int sign = 0;

		if (args [0] is GameObject) {
			GameObject obj = (args [0] as GameObject);
			Voxeme voxComponent = obj.GetComponent<Voxeme> ();

			// find object axis perpendicular to world Y
			List<float> dotProducts = new List<float>();

			foreach (Vector3 testAxis in Constants.Axes.Values) {
				dotProducts.Add(Mathf.Abs(Vector3.Dot(obj.transform.rotation * testAxis,Constants.yAxis)));
			}

			int perpendicular = dotProducts.IndexOf(dotProducts.Min());
			int parallel = dotProducts.IndexOf(dotProducts.Max());

			if (perpendicular == 0) {		// x
				trackAxis = Constants.xAxis;
			}
			else if (perpendicular == 1) {	// y
				trackAxis = Constants.yAxis;
			}
			else if (perpendicular == 2) {	// z
				trackAxis = Constants.zAxis;
			}

			if (parallel == 0) {		// x
				objRotAxis = Constants.xAxis;
				worldRotAxis = obj.transform.rotation * Constants.xAxis;
			}
			else if (parallel == 1) {	// y
				objRotAxis = Constants.yAxis;
				worldRotAxis = obj.transform.rotation * Constants.yAxis;
			}
			else if (parallel == 2) {	// z
				objRotAxis = Constants.zAxis;
				worldRotAxis = obj.transform.rotation * Constants.zAxis;
			}

			Debug.Log (trackAxis);
			Debug.Log (worldRotAxis);
			//Debug.Break ();

			orientations.Add (obj.transform.rotation * trackAxis);

			sign = RandomHelper.RandomSign ();
			angle = sign * (180.0f + UnityEngine.Random.rotation.eulerAngles.y);

			if (voxComponent.turnSpeed == 0.0f) {
				voxComponent.turnSpeed = RandomHelper.RandomFloat (0.0f, 12.5f, (int)RandomHelper.RangeFlags.MaxInclusive);
			}

			float degrees = angle;


			//targetRotation *= obj.transform.rotation;

			if (sign > 0) {
				while (degrees > 90.0f) {
					//targetRotation = targetRotation * Quaternion.AngleAxis (90.0f, Constants.yAxis);
					orientations.Add (Quaternion.AngleAxis (90.0f, worldRotAxis) * orientations [orientations.Count - 1]);
					degrees -= 90.0f;
				}
			}
			else {
				while (degrees < -90.0f) {
					//targetRotation = targetRotation * Quaternion.AngleAxis (90.0f, Constants.yAxis);
					orientations.Add (Quaternion.AngleAxis (-90.0f, worldRotAxis) * orientations [orientations.Count - 1]);
					degrees += 90.0f;
				}
			}

			//targetRotation = targetRotation * Quaternion.AngleAxis(degrees, Constants.yAxis);
			orientations.Add (Quaternion.AngleAxis (degrees, worldRotAxis) * orientations[orientations.Count-1]);
		}

		if (args [args.Length - 1] is bool) {
			if ((bool)args [args.Length - 1] == false) {
				for (int i = 0; i < orientations.Count; i++) {
					eventManager.InsertEvent (string.Format ("turn({0},{1},{2},{3})", (args [0] as GameObject).name, 
						Helper.VectorToParsable (trackAxis), Helper.VectorToParsable (orientations [i]), Helper.VectorToParsable (worldRotAxis)), 1 + i);
				}

				// record parameter values
				OnPrepareLog (this, new ParamsEventArgs ("RotSpeed", (args [0] as GameObject).GetComponent<Voxeme>().turnSpeed.ToString()));
				OnPrepareLog (this, new ParamsEventArgs ("RotAxis", Constants.Axes.FirstOrDefault (a => a.Value == objRotAxis).Key));
				OnPrepareLog (this, new ParamsEventArgs ("RotAngle", angle.ToString()));
				OnPrepareLog (this, new ParamsEventArgs ("RotDir", (sign > 0) ? "+" : "-"));
				OnParamsCalculated (null, null);
				//Debug.Break ();
			}
		}

		// override physics rigging
//		foreach (object arg in args) {
//			if (arg is GameObject) {
//				(arg as GameObject).GetComponent<Rigging> ().ActivatePhysics(false);
//			}
//		}
//			
//		Quaternion targetRotation = Quaternion.identity;
//
//		Helper.PrintRDFTriples (rdfTriples);
//
//		string prep = rdfTriples.Count > 0 ? rdfTriples [0].Item2.Replace ("spin", "") : "";
//
//		if (args [0] is GameObject) {
//			float degrees = 180.0f + UnityEngine.Random.rotation.eulerAngles.y;
//
//			GameObject obj = (args [0] as GameObject);
//			Voxeme voxComponent = obj.GetComponent<Voxeme> ();
//
//			if (voxComponent != null) {
//				if (!voxComponent.enabled) {
//					voxComponent.gameObject.transform.parent = null;
//					voxComponent.enabled = true;
//				}
//
//				//float degrees = 0.0f;
//
//				if (args [1] is Vector3 &&  args [2] is Vector3){
//					Debug.Log ((Vector3)args [1]);
//					Debug.Log (obj.transform.rotation * (Vector3)args [1]);
//					Debug.Log ((Vector3)args [2]);
//					//targetRotation = Quaternion.AngleAxis (Vector3.Angle ((Vector3)args [1], (Vector3)args [2]), Constants.yAxis);
//					targetRotation = Quaternion.FromToRotation((Vector3)args [1],(Vector3)args [2]) * obj.transform.rotation;
//					//targetRotation = Quaternion.LookRotation(obj.transform.rotation * (Vector3)args [1],(Vector3)args [2]).eulerAngles;
//				}
//				else {
//					float degrees = 180.0f + UnityEngine.Random.rotation.eulerAngles.y;
//
//					Debug.Log (targetRotation.eulerAngles);
//					Debug.Log (obj.transform.eulerAngles);
//					targetRotation *= obj.transform.rotation;
//					Debug.Log (targetRotation.eulerAngles);
//
//					while (degrees > 90.0f) {
//						targetRotation = Quaternion.AngleAxis (90.0f, Constants.yAxis) * targetRotation;
//						Debug.Log (Helper.VectorToParsable (targetRotation.eulerAngles));
//						voxComponent.interTargetRotations.Enqueue (targetRotation.eulerAngles);
//						degrees -= 90.0f;
//					}
//
//					targetRotation = Quaternion.AngleAxis(degrees, Constants.yAxis) * targetRotation;
//					Debug.Log (Helper.VectorToParsable (targetRotation.eulerAngles));
//				}
//
//				voxComponent.targetRotation = targetRotation.eulerAngles;
//				Debug.Log (Helper.VectorToParsable (voxComponent.targetRotation));
//				Debug.Log (Helper.VectorToParsable (targetRotation * Constants.xAxis));
//
//			}
//		}
//
//		// add to events manager
//		if (args[args.Length-1] is bool) {
//			if ((bool)args[args.Length-1] == false) {
//				if (args [1] is Vector3 && args [2] is Vector3) {
//					eventManager.events [0] = "spin(" + (args [0] as GameObject).name + "," + Helper.VectorToParsable ((Vector3)args [1]) + "," + Helper.VectorToParsable ((Vector3)args [2]) + ")";
//				}
//				else {
//					eventManager.events [0] = "spin(" + (args [0] as GameObject).name + "," +
//						Helper.VectorToParsable ((args [0] as GameObject).transform.rotation * Constants.xAxis) + "," +
//						Helper.VectorToParsable (targetRotation * Constants.xAxis) + ")";
//				}
//				Debug.Log (eventManager.events [0]);
//			}
//		}
//
//		return;
	}

	// IN: Objects
	// OUT: none
	public void STACK(object[] args)
	{
		bool areObjs = true;
		for (int i = 0; i < args.Length-1; i++) {
			if (!(args[i] is GameObject)) {
				areObjs = false;
				break;
			}
		}

		if (args [args.Length - 1] is bool) {
			if ((bool)args [args.Length - 1] == false) {
				if (areObjs) {
					List<GameObject> objs = new List<GameObject> ();
					foreach (object arg in args) {
						if (arg is GameObject) {
							objs.Add (arg as GameObject);
						}
					}
					System.Random rand = new System.Random ();
					objs = objs.OrderBy (item => rand.Next ()).ToList ();
					int i;
					for (i = 0; i < objs.Count - 1; i++) {
						eventManager.InsertEvent (string.Format ("put({0},on({1}))", objs [i + 1].name, objs [i].name), 1 + i);
					}
					//eventManager.RemoveEvent (i);

					// record parameter values
					string[] placementOrder = objs.Select (o => o.name).ToArray ();
					OnPrepareLog (this, new ParamsEventArgs ("PlacementOrder", string.Join(",",placementOrder)));
					OnParamsCalculated (null, null);
				}
			}
		}
	}

	// IN: Objects
	// OUT: none
	public void LEAN(object[] args)
	{
		// override physics rigging
		foreach (object arg in args) {
			if (arg is GameObject) {
				Rigging rigging = (arg as GameObject).GetComponent<Rigging> ();
				if (rigging != null) {
					rigging.ActivatePhysics (false);
				}
			}
		}

		Vector3 targetPosition = Vector3.zero;
		float leanAngle = UnityEngine.Random.Range (25.0f, 65.0f);
		//float leanAngle = UnityEngine.Random.Range (0.0f, 0.0f);

		// add agent-independent precondition (turn)
		if (args [0] is GameObject) {
			if (args [1] is Vector3) {
				GameObject theme = (args [0] as GameObject);
				GameObject dest = GameObject.Find (rdfTriples [0].Item3);
				Voxeme voxComponent = theme.GetComponent<Voxeme> ();
				Vector3 objMajorAxis = Helper.GetObjectMajorAxis (theme);
				Vector3 objMinorAxis = Helper.GetObjectMinorAxis (theme);
				Debug.Log (objMajorAxis);
				Debug.Log (objMinorAxis);
				Debug.Log ((theme.transform.position.x - dest.transform.position.x));

				// turn the longest axis to $tilt degrees off from +Y axis
				//	and the shortest axis perpendicular to the longest axis and coplanar with the plane that bisects the dest obj
				Vector3 minorTilt = new Vector3 (0.0f, 0.0f, (theme.transform.position.x < dest.transform.position.x) ? 
					-(leanAngle - 90.0f) : (leanAngle - 90.0f));
				Debug.Log (minorTilt);
				Debug.Log (Quaternion.Euler (minorTilt) * Constants.yAxis);

				Vector3 majorTilt = new Vector3 (0.0f, 0.0f, (theme.transform.position.x < dest.transform.position.x) ?
					-leanAngle : leanAngle);
				Debug.Log (majorTilt);
				Debug.Log (Quaternion.Euler (majorTilt) * Constants.yAxis);

				Vector3 themeContactPoint = theme.transform.position;	// computed coordinate of relation over dest
				Vector3 destContactPoint = (Vector3)args [1];	// computed coordinate of relation over dest
				//Bounds themeBounds = Helper.GetObjectWorldSize (theme);
				Bounds themeBounds = Helper.GetObjectSize (theme);
				Bounds destBounds = Helper.GetObjectWorldSize (dest);

				float majorAxisLength = 0.0f;
				if (objMajorAxis == Constants.xAxis) {
					majorAxisLength = Helper.GetObjectSize (theme).size.x;
				}
				else if (objMajorAxis == Constants.yAxis) {
					majorAxisLength = Helper.GetObjectSize (theme).size.y;
				}
				else if (objMajorAxis == Constants.zAxis) {
					majorAxisLength = Helper.GetObjectSize (theme).size.z;
				}

				float minorAxisLength = 0.0f;
				if (objMinorAxis == Constants.xAxis) {
					minorAxisLength = Helper.GetObjectSize (theme).size.x;
				}
				else if (objMinorAxis == Constants.yAxis) {
					minorAxisLength = Helper.GetObjectSize (theme).size.y;
				}
				else if (objMinorAxis == Constants.zAxis) {
					minorAxisLength = Helper.GetObjectSize (theme).size.z;
				}

				//Debug.Log (minorAxisLength);

				// given the height of the object standing up (unrotated) (= hypotenuse of triangle between leaned theme, dest, and supporting surface)
				// heightAgainstDest = majorAxisLength*sin(90.0f-leanAngle)
				// offset = (minorAxisLength/2)/sin(90.0f-leanAngle)	// x distance from the side of dest to the central axis of theme (rotated)
				// adjacent = sqrt(((minorAxisLength/2)^2)+(offset^2))
				//

				if (theme.transform.position.x < dest.transform.position.x) {	// place theme to left of dest
					float themeHeightAgainstDest = majorAxisLength * Mathf.Cos (Mathf.Deg2Rad * leanAngle);	// the y-extent of theme's rotated major axis
																									// the opposite side of the triangle where
																									//	the supporting surface is the adjacent side
																									//	and theme's rotated major axis is the hypotenuse 
					float horizontalOffset = 0.0f;
					//float descent = Mathf.Sin (Mathf.Deg2Rad * (90.0f - leanAngle)) * adjacent;	// vertical offset
																				//	the length of the altitude between the right angle
																				//	of the above triangle and the hypotenuse
					float verticalOffset = (minorAxisLength / 2.0f) * Mathf.Sin (Mathf.Deg2Rad * leanAngle);
					//Debug.Log (verticalOffset);
					//Debug.Log (hypotenuse);

					//Debug.Log (descent);
					//Debug.Log (horizontalOffset);
					if (themeHeightAgainstDest > destBounds.size.y) {	// if theme is taller than dest
						float destHeightAgainstTheme = destBounds.size.y;
						float hypotenuse = destHeightAgainstTheme / Mathf.Cos (Mathf.Deg2Rad * leanAngle) - (verticalOffset/Mathf.Cos(Mathf.Deg2Rad * leanAngle));
						horizontalOffset = (minorAxisLength / 2.0f) / Mathf.Cos (Mathf.Deg2Rad * leanAngle);	// x distance from the side of dest to the central axis of theme (rotated)

						if (objMajorAxis == Constants.xAxis) {
							themeContactPoint = new Vector3 (themeBounds.min.x + hypotenuse, themeBounds.center.y, themeBounds.center.z);
						}
						else if (objMajorAxis == Constants.yAxis) {
							themeContactPoint = new Vector3 (themeBounds.center.x, themeBounds.min.y + hypotenuse, themeBounds.center.z);
						}
						else if (objMajorAxis == Constants.zAxis) {
							themeContactPoint = new Vector3 (themeBounds.center.x, themeBounds.center.y, themeBounds.min.z + hypotenuse);
						}

						destContactPoint = new Vector3(destBounds.min.x,destBounds.max.y,destContactPoint.z);
					}
					else {	// if theme is shorter than dest
						float destHeightAgainstTheme = (majorAxisLength * Mathf.Cos(Mathf.Deg2Rad * leanAngle));
						//horizontalOffset = 0.0f;

						if (objMajorAxis == Constants.xAxis) {
							if (objMinorAxis == Constants.yAxis) {
								themeContactPoint = new Vector3 (themeBounds.max.x, themeBounds.min.y, themeBounds.center.z);
							}
							else if (objMinorAxis == Constants.zAxis) {
								themeContactPoint = new Vector3 (themeBounds.max.x, themeBounds.center.y, themeBounds.min.z);
							}
						}
						else if (objMajorAxis == Constants.yAxis) {
							if (objMinorAxis == Constants.xAxis) {
								themeContactPoint = new Vector3 (themeBounds.min.x, themeBounds.max.y, themeBounds.center.z);
							}
							else if (objMinorAxis == Constants.zAxis) {
								themeContactPoint = new Vector3 (themeBounds.center.x, themeBounds.max.y, themeBounds.min.z);
							}
						}
						else if (objMajorAxis == Constants.zAxis) {
							if (objMinorAxis == Constants.xAxis) {
								themeContactPoint = new Vector3 (themeBounds.min.x, themeBounds.center.y, themeBounds.max.z);
							}
							else if (objMinorAxis == Constants.yAxis) {
								themeContactPoint = new Vector3 (themeBounds.center.x, themeBounds.min.y, themeBounds.max.z);
							}
						}

						destContactPoint = new Vector3(destBounds.min.x,destBounds.min.y + destHeightAgainstTheme,destContactPoint.z);
					}
					//Debug.Log (Helper.VectorToParsable(themeContactPoint));

					Quaternion rot1 = Quaternion.FromToRotation(objMinorAxis, Quaternion.Euler (minorTilt) * Constants.yAxis);

					float sign = Mathf.Sign (Vector3.Dot(Vector3.Cross (rot1 * objMajorAxis, Quaternion.Euler (majorTilt) * Constants.yAxis),
						Quaternion.Euler (minorTilt) * Constants.yAxis));
					float angle = Vector3.Angle (rot1 * objMajorAxis, Quaternion.Euler (majorTilt) * Constants.yAxis);
					Quaternion rot2 = Quaternion.AngleAxis (sign * angle, Quaternion.Euler (minorTilt) * Constants.yAxis) /* rot1*/;

					//Vector3 transformedThemeContactPoint = Helper.RotatePointAroundPivot (Helper.RotatePointAroundPivot (themeContactPoint, 
					//	themeBounds.center, rot1.eulerAngles), themeBounds.center, rot2.eulerAngles);
					Vector3 transformedThemeContactPoint = Helper.RotatePointAroundPivot (themeContactPoint, themeBounds.center, (rot2 * rot1).eulerAngles);
					transformedThemeContactPoint = new Vector3 (transformedThemeContactPoint.x + horizontalOffset, transformedThemeContactPoint.y, transformedThemeContactPoint.z) +
						(args [0] as GameObject).transform.position;
					//Debug.Log (Helper.VectorToParsable(transformedThemeContactPoint-(args [0] as GameObject).transform.position));
					//Debug.Log (Helper.VectorToParsable(transformedThemeContactPoint));


					//Debug.Log (Helper.VectorToParsable(destContactPoint));
//					Debug.Log (Helper.VectorToParsable(theme.transform.position - Helper.GetObjectWorldSize(theme).center));
//					Debug.Break ();
					Vector3 displacement = destContactPoint - transformedThemeContactPoint;
					targetPosition = (args [0] as GameObject).transform.position + displacement;
					//targetPosition = new Vector3 (targetPosition.x, targetPosition.y, targetPosition.z);
					//Debug.Log (Helper.VectorToParsable (displacement));
					//Debug.Log (Helper.VectorToParsable (targetPosition + (transformedThemeContactPoint-(args [0] as GameObject).transform.position)));
					//Debug.Log (Helper.VectorToParsable (destContactPoint-targetPosition));
				}
				else {	// place theme to right of dest
					float themeHeightAgainstDest = majorAxisLength * Mathf.Cos (Mathf.Deg2Rad * leanAngle);	// the y-extent of theme's rotated major axis
					// the opposite side of the triangle where
					//	the supporting surface is the adjacent side
					//	and theme's rotated major axis is the hypotenuse 
					float horizontalOffset = 0.0f;
					//float descent = Mathf.Sin (Mathf.Deg2Rad * (90.0f - leanAngle)) * adjacent;	// vertical offset
					//	the length of the altitude between the right angle
					//	of the above triangle and the hypotenuse
					float verticalOffset = (minorAxisLength / 2.0f) * Mathf.Sin (Mathf.Deg2Rad * leanAngle);
					//Debug.Log (verticalOffset);
					//Debug.Log (hypotenuse);

					//Debug.Log (descent);
					//Debug.Log (horizontalOffset);
					if (themeHeightAgainstDest > destBounds.size.y) {	// if theme is taller than dest
						float destHeightAgainstTheme = destBounds.size.y;
						float hypotenuse = destHeightAgainstTheme / Mathf.Cos (Mathf.Deg2Rad * leanAngle) - (verticalOffset/Mathf.Cos(Mathf.Deg2Rad * leanAngle));
						horizontalOffset = (minorAxisLength / 2.0f) / Mathf.Cos (Mathf.Deg2Rad * leanAngle);	// x distance from the side of dest to the central axis of theme (rotated)

						if (objMajorAxis == Constants.xAxis) {
							themeContactPoint = new Vector3 (themeBounds.min.x + hypotenuse, themeBounds.center.y, themeBounds.center.z);
						}
						else if (objMajorAxis == Constants.yAxis) {
							themeContactPoint = new Vector3 (themeBounds.center.x, themeBounds.min.y + hypotenuse, themeBounds.center.z);
						}
						else if (objMajorAxis == Constants.zAxis) {
							themeContactPoint = new Vector3 (themeBounds.center.x, themeBounds.center.y, themeBounds.min.z + hypotenuse);
						}

						destContactPoint = new Vector3(destBounds.max.x,destBounds.max.y,destContactPoint.z);
					}
					else {	// if theme is shorter than dest
						float destHeightAgainstTheme = (majorAxisLength * Mathf.Cos(Mathf.Deg2Rad * leanAngle));
						//horizontalOffset = 0.0f;

						if (objMajorAxis == Constants.xAxis) {
							if (objMinorAxis == Constants.yAxis) {
								themeContactPoint = new Vector3 (themeBounds.max.x, themeBounds.min.y, themeBounds.center.z);
							}
							else if (objMinorAxis == Constants.zAxis) {
								themeContactPoint = new Vector3 (themeBounds.max.x, themeBounds.center.y, themeBounds.min.z);
							}
						}
						else if (objMajorAxis == Constants.yAxis) {
							if (objMinorAxis == Constants.xAxis) {
								themeContactPoint = new Vector3 (themeBounds.min.x, themeBounds.max.y, themeBounds.center.z);
							}
							else if (objMinorAxis == Constants.zAxis) {
								themeContactPoint = new Vector3 (themeBounds.center.x, themeBounds.max.y, themeBounds.min.z);
							}
						}
						else if (objMajorAxis == Constants.zAxis) {
							if (objMinorAxis == Constants.xAxis) {
								themeContactPoint = new Vector3 (themeBounds.min.x, themeBounds.center.y, themeBounds.max.z);
							}
							else if (objMinorAxis == Constants.yAxis) {
								themeContactPoint = new Vector3 (themeBounds.center.x, themeBounds.min.y, themeBounds.max.z);
							}
						}

						destContactPoint = new Vector3(destBounds.max.x,destBounds.min.y + destHeightAgainstTheme,destContactPoint.z);
					}
					//Debug.Log (Helper.VectorToParsable(themeContactPoint));

					Quaternion rot1 = Quaternion.FromToRotation(objMinorAxis, Quaternion.Euler (minorTilt) * Constants.yAxis);

					float sign = Mathf.Sign (Vector3.Dot(Vector3.Cross (rot1 * objMajorAxis, Quaternion.Euler (majorTilt) * Constants.yAxis),
						Quaternion.Euler (minorTilt) * Constants.yAxis));
					float angle = Vector3.Angle (rot1 * objMajorAxis, Quaternion.Euler (majorTilt) * Constants.yAxis);
					Quaternion rot2 = Quaternion.AngleAxis (sign * angle, Quaternion.Euler (minorTilt) * Constants.yAxis) /* rot1*/;

					//Vector3 transformedThemeContactPoint = Helper.RotatePointAroundPivot (Helper.RotatePointAroundPivot (themeContactPoint, 
					//	themeBounds.center, rot1.eulerAngles), themeBounds.center, rot2.eulerAngles);
					Vector3 transformedThemeContactPoint = Helper.RotatePointAroundPivot (themeContactPoint, themeBounds.center, (rot2 * rot1).eulerAngles);
					transformedThemeContactPoint = new Vector3 (transformedThemeContactPoint.x - horizontalOffset, transformedThemeContactPoint.y, transformedThemeContactPoint.z) +
						(args [0] as GameObject).transform.position;
					//Debug.Log (Helper.VectorToParsable(transformedThemeContactPoint-(args [0] as GameObject).transform.position));
					//Debug.Log (Helper.VectorToParsable(transformedThemeContactPoint));

					Vector3 displacement = destContactPoint - transformedThemeContactPoint;
					targetPosition = (args [0] as GameObject).transform.position + displacement;
//					targetPosition = new Vector3 (targetPosition.x + (theme.transform.position.x - themeBounds.center.x),
//						targetPosition.y + (theme.transform.position.y - themeBounds.center.y),
//						targetPosition.z + (theme.transform.position.z - themeBounds.center.z));
					//targetPosition = new Vector3 (targetPosition.x, targetPosition.y, targetPosition.z);
					//Debug.Log (Helper.VectorToParsable (displacement));
					//Debug.Log (Helper.VectorToParsable (targetPosition + (transformedThemeContactPoint-(args [0] as GameObject).transform.position)));
					//Debug.Log (Helper.VectorToParsable (destContactPoint-targetPosition));
				}

				// E1: turn the theme object to that shortest axis is 90 degrees from desired $tilt angle away from the dest surface
				// E2: turn the theme object to that longest axis is $tilt angle away from the dest surface
				if (!SatisfactionTest.IsSatisfied (string.Format ("turn({0},{1},{2},{3})", (args [0] as GameObject).name,
					   Helper.VectorToParsable (objMajorAxis), Helper.VectorToParsable (Quaternion.Euler (majorTilt) * Constants.yAxis),
					   Helper.VectorToParsable ((args [0] as GameObject).transform.rotation * objMinorAxis)))) {
					eventManager.InsertEvent (string.Format ("turn({0},{1},{2})", (args [0] as GameObject).name,
						Helper.VectorToParsable (objMinorAxis), Helper.VectorToParsable (Quaternion.Euler (minorTilt) * Constants.yAxis)), 0);
					eventManager.InsertEvent (string.Format ("turn({0},{1},{2},{3})", (args [0] as GameObject).name,
						Helper.VectorToParsable (objMajorAxis), Helper.VectorToParsable (Quaternion.Euler (majorTilt) * Constants.yAxis),
						Helper.VectorToParsable (Quaternion.Euler (minorTilt) * Constants.yAxis)), 1);
					//eventManager.InsertEvent (string.Format ("lean({0},against({1}))", (args [0] as GameObject).name, dest.name), 2);
					eventManager.InsertEvent (string.Format ("put({0},{1})", (args [0] as GameObject).name,
						Helper.VectorToParsable (targetPosition)), 2);
					eventManager.RemoveEvent (eventManager.events.Count - 1);
					if (args[args.Length-1] is bool) {
						if ((bool)args[args.Length-1] == false) {
							// record parameter values
							OnPrepareLog (this, new ParamsEventArgs ("RotAngle", leanAngle.ToString()));
							OnParamsCalculated (null, null);
						}
					}
					return;
				}
			}
		}

		Helper.PrintRDFTriples (rdfTriples);

		targetPosition = Vector3.zero;

		string prep = rdfTriples.Count > 0 ? rdfTriples [0].Item2.Replace ("lean", "") : "";

		if (prep == "_on") {
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = (args [0] as GameObject);
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);
					Vector3 targetPoint = (Vector3)args [1];
					Debug.Log (targetPoint);
					Bounds themeBounds = Helper.GetObjectWorldSize (theme);
					Bounds destBounds = Helper.GetObjectWorldSize (dest);

					if (theme.transform.position.x < dest.transform.position.x) {	// place to left

						// calc right side of theme
						GameObject mainCamera = GameObject.Find ("Main Camera");
						float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

						float xAdjust = (theme.transform.position.x - themeBounds.center.x);

						Vector3 loc = ((Vector3)args [1]);	// coord of "on"
						Vector3 rayStart;
						Vector3 contactPoint;

						if (themeBounds.size.y > destBounds.size.y) {
							loc = new Vector3(loc.x-destBounds.extents.x,loc.y,loc.z);	// projected to left side of dest
						}
						else {
							rayStart = new Vector3 (themeBounds.max.x-themeBounds.center.x-Constants.EPSILON, Mathf.Abs (themeBounds.size.y), 0.0f);
							rayStart += theme.transform.position;
							contactPoint = Helper.RayIntersectionPoint (rayStart, Vector3.down);

							loc = new Vector3(loc.x-destBounds.extents.x,contactPoint.y,loc.z);	// projected to left side of dest and top of theme
						}

						rayStart = new Vector3 (0.0f, 0.0f,Mathf.Abs (themeBounds.size.z));
						rayStart = Quaternion.Euler (0.0f, povDir + 90.0f, 0.0f) * rayStart;
						rayStart += theme.transform.position;
						Debug.Log (loc.y);
						Debug.Log (themeBounds.max.y);
						rayStart = new Vector3 (rayStart.x, loc.y, rayStart.z);
						contactPoint = Helper.RayIntersectionPoint (rayStart, Vector3.left);	//** this ray is angled downward when it should be angled straight along the x-axis
						Debug.Log (contactPoint.x);
						Debug.Log (contactPoint.y);

						Debug.Log ("X-adjust = " + xAdjust);
						Debug.Log ("lean_on: " + Helper.VectorToParsable (contactPoint));

						if (args [args.Length - 1] is bool) {
							if ((bool)args [args.Length - 1] == false) {	// compute satisfaction condition
								Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x) + xAdjust,
									loc.y - (contactPoint.y - theme.transform.position.y),
									loc.z - (contactPoint.z - theme.transform.position.z)) - loc;

								targetPosition = dir + loc;
							}
							else {
								targetPosition = loc;
							}

							Debug.Log (Helper.VectorToParsable (targetPosition));

							Voxeme voxComponent = theme.GetComponent<Voxeme> ();
							if (voxComponent != null) {
								if (!voxComponent.enabled) {
									voxComponent.gameObject.transform.parent = null;
									voxComponent.enabled = true;
								}

								voxComponent.targetPosition = targetPosition;
							}
						}
					}
					else if (theme.transform.position.x > dest.transform.position.x) {	// place to right

						// calc left side of theme
						GameObject mainCamera = GameObject.Find ("Main Camera");
						float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

						float xAdjust = (theme.transform.position.x - themeBounds.center.x);

						Vector3 loc = ((Vector3)args [1]);	// coord of "on"
						Vector3 rayStart;
						Vector3 contactPoint;

						if (themeBounds.size.y > destBounds.size.y) {
							loc = new Vector3(loc.x+destBounds.extents.x,loc.y,loc.z);	// projected to right side of dest
						}
						else {
							rayStart = new Vector3 (themeBounds.min.x-themeBounds.center.x+Constants.EPSILON, Mathf.Abs (themeBounds.size.y), 0.0f);
							rayStart += theme.transform.position;
							contactPoint = Helper.RayIntersectionPoint (rayStart, Vector3.down);
								
							loc = new Vector3(loc.x+destBounds.extents.x,themeBounds.max.y,loc.z);	// projected to right side of dest and top of theme
						}

						rayStart = new Vector3 (0.0f, 0.0f, Mathf.Abs (themeBounds.size.z));
						rayStart = Quaternion.Euler (0.0f, povDir + 270.0f, 0.0f) * rayStart;
						rayStart += theme.transform.position;
						rayStart = new Vector3 (rayStart.x, loc.y, rayStart.z);
						contactPoint = Helper.RayIntersectionPoint (rayStart, Vector3.right);

						Debug.Log ("X-adjust = " + xAdjust);
						Debug.Log ("lean_against: " + Helper.VectorToParsable (contactPoint));

						if (args [args.Length - 1] is bool) {
							if ((bool)args [args.Length - 1] == false) {	// compute satisfaction condition
								Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x) + xAdjust,
									loc.y - (contactPoint.y - theme.transform.position.y),
									loc.z - (contactPoint.z - theme.transform.position.z)) - loc;

								targetPosition = dir + loc;
							}
							else {
								targetPosition = loc;
							}

							Debug.Log (Helper.VectorToParsable (targetPosition));

							Voxeme voxComponent = theme.GetComponent<Voxeme> ();
							if (voxComponent != null) {
								if (!voxComponent.enabled) {
									voxComponent.gameObject.transform.parent = null;
									voxComponent.enabled = true;
								}

								voxComponent.targetPosition = targetPosition;
							}
						}
					}
				}
			}
		}
		else if (prep == "_against") {
			if (args [0] is GameObject) {
				if (args [1] is Vector3) {
					GameObject theme = (args [0] as GameObject);
					GameObject dest = GameObject.Find (rdfTriples [0].Item3);
					Vector3 targetPoint = (Vector3)args [1];
					Debug.Log (targetPoint);
					Bounds themeBounds = Helper.GetObjectWorldSize (theme);
					Bounds destBounds = Helper.GetObjectWorldSize (dest);

					if (theme.transform.position.x < dest.transform.position.x) {	// place to left

						// calc right side of theme
						GameObject mainCamera = GameObject.Find ("Main Camera");
						float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

						float xAdjust = (theme.transform.position.x - themeBounds.center.x);

						Vector3 loc = ((Vector3)args [1]);	// coord of "against"
						Vector3 rayStart;
						Vector3 contactPoint;

						if (themeBounds.size.y > destBounds.size.y) {
							loc = new Vector3(loc.x-destBounds.extents.x,destBounds.max.y,loc.z);	// projected to left side of dest
						}
						else {
							rayStart = new Vector3 (themeBounds.max.x-themeBounds.center.x-Constants.EPSILON, Mathf.Abs (themeBounds.size.y), 0.0f);
							rayStart += theme.transform.position;
							contactPoint = Helper.RayIntersectionPoint (rayStart, Vector3.down);

							loc = new Vector3(loc.x-destBounds.extents.x,contactPoint.y,loc.z);	// projected to left side of dest and top of theme
						}

						rayStart = new Vector3 (0.0f, 0.0f, Mathf.Abs (themeBounds.size.z));
						rayStart = Quaternion.Euler (0.0f, povDir + 90.0f, 0.0f) * rayStart;
						rayStart += theme.transform.position;
						Debug.Log (loc.y);
						//Debug.Break ();
						Debug.Log (themeBounds.max.y);
						rayStart = new Vector3 (rayStart.x, loc.y, rayStart.z);
						contactPoint = Helper.RayIntersectionPoint (rayStart, Vector3.left);
						Debug.Log (Helper.VectorToParsable(rayStart));
						Debug.Log (contactPoint.x);
						Debug.Log (contactPoint.y);

						Debug.Log ("X-adjust = " + xAdjust);
						Debug.Log ("lean_against: " + Helper.VectorToParsable (contactPoint));

						if (args [args.Length - 1] is bool) {
							if ((bool)args [args.Length - 1] == false) {	// compute satisfaction condition
								Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x) + xAdjust,
									loc.y - (contactPoint.y - theme.transform.position.y),
									loc.z - (contactPoint.z - theme.transform.position.z)) - loc;

								targetPosition = dir + loc;
							}
							else {
								targetPosition = loc;
							}

							Debug.Log (Helper.VectorToParsable (targetPosition));

							Voxeme voxComponent = theme.GetComponent<Voxeme> ();
							if (voxComponent != null) {
								if (!voxComponent.enabled) {
									voxComponent.gameObject.transform.parent = null;
									voxComponent.enabled = true;
								}

								voxComponent.targetPosition = targetPosition;
							}
						}
					}
					else if (theme.transform.position.x > dest.transform.position.x) {	// place to right

						// calc left side of theme
						GameObject mainCamera = GameObject.Find ("Main Camera");
						float povDir = cameraRelativeDirections ? mainCamera.transform.eulerAngles.y : 0.0f;

						float xAdjust = (theme.transform.position.x - themeBounds.center.x);

						Vector3 loc = ((Vector3)args [1]);	// coord of "against"
						Vector3 rayStart;
						Vector3 contactPoint;

						if (themeBounds.size.y > destBounds.size.y) {
							loc = new Vector3(loc.x+destBounds.extents.x,destBounds.max.y,loc.z);	// projected to right side of dest
						}
						else {
							rayStart = new Vector3 (themeBounds.min.x-themeBounds.center.x+Constants.EPSILON, Mathf.Abs (themeBounds.size.y), 0.0f);
							rayStart += theme.transform.position;
							contactPoint = Helper.RayIntersectionPoint (rayStart, Vector3.down);

							loc = new Vector3(loc.x+destBounds.extents.x,themeBounds.max.y,loc.z);	// projected to right side of dest and top of theme
						}

						rayStart = new Vector3 (0.0f, 0.0f, Mathf.Abs (themeBounds.size.z));
						rayStart = Quaternion.Euler (0.0f, povDir + 270.0f, 0.0f) * rayStart;
						rayStart += theme.transform.position;
						rayStart = new Vector3 (rayStart.x, loc.y, rayStart.z);
						contactPoint = Helper.RayIntersectionPoint (rayStart, Vector3.right);

						Debug.Log ("X-adjust = " + xAdjust);
						Debug.Log ("lean_against: " + Helper.VectorToParsable (contactPoint));

						if (args [args.Length - 1] is bool) {
							if ((bool)args [args.Length - 1] == false) {	// compute satisfaction condition
								Vector3 dir = new Vector3 (loc.x - (contactPoint.x - theme.transform.position.x) + xAdjust,
									loc.y - (contactPoint.y - theme.transform.position.y),
									loc.z - (contactPoint.z - theme.transform.position.z)) - loc;

								targetPosition = dir + loc;
							}
							else {
								targetPosition = loc;
							}

							Debug.Log (Helper.VectorToParsable (targetPosition));

							Voxeme voxComponent = theme.GetComponent<Voxeme> ();
							if (voxComponent != null) {
								if (!voxComponent.enabled) {
									voxComponent.gameObject.transform.parent = null;
									voxComponent.enabled = true;
								}

								voxComponent.targetPosition = targetPosition;
							}
						}
					}
				}
			}
		}
		else {
		}

		if (args[args.Length-1] is bool) {
			if ((bool)args[args.Length-1] == false) {
				// record parameter values
//				Dictionary<string,string> paramValues = PredicateParameters.InitPredicateParametersCollection();
//				paramValues ["TiltAngle"] = leanAngle.ToString();
//				paramValues ["TranslocSpeed"] = (args [0] as GameObject).GetComponent<Voxeme> ().moveSpeed.ToString();
//				OnParamsCalculated (this, new ParamsEventArgs (paramValues));
//
//				// add to events manager
//				eventManager.events[0] = "put("+(args [0] as GameObject).name+","+Helper.VectorToParsable(targetPosition)+")";
				//Debug.Log (eventManager.events [0]);
			}
		}

		return;
	}

	// IN: Objects
	// OUT: none
//	public void SWITCH(object[] args)
//	{
//		if ((args [0] is GameObject) && (args [1] is GameObject)) {
//			Debug.Log ((args [0] is GameObject));
//			Debug.Log ((args [1] is GameObject));
//			Vector3[] startPos = new Vector3[] { (args [0] as GameObject).transform.position,(args [1] as GameObject).transform.position };
//
//			if (startPos [0].x < startPos [1].x) {	// if args[0] is left of args[1]
//				eventManager.InsertEvent (string.Format ("slide({0},{1})", (args [0] as GameObject).name,
//					Helper.VectorToParsable (startPos [1] + (Vector3.right*.8f))), 1);
//				eventManager.InsertEvent (string.Format ("slide({0},{1})", (args [1] as GameObject).name,
//					Helper.VectorToParsable (startPos [0])), 2);
//				eventManager.InsertEvent (string.Format ("slide({0},{1})", (args [0] as GameObject).name,
//					Helper.VectorToParsable (startPos [1])), 3);
//			}
//			else if (startPos [0].x > startPos [1].x) {	// if args[0] is right of args[1]
//				eventManager.InsertEvent (string.Format ("slide({0},{1})", (args [0] as GameObject).name,
//					Helper.VectorToParsable (startPos [1] + (Vector3.left*.8f))), 1);
//				eventManager.InsertEvent (string.Format ("slide({0},{1})", (args [1] as GameObject).name,
//					Helper.VectorToParsable (startPos [0])), 2);
//				eventManager.InsertEvent (string.Format ("slide({0},{1})", (args [0] as GameObject).name,
//					Helper.VectorToParsable (startPos [1])), 3);
//			}
//			//eventManager.RemoveEvent (3);
//		}
//	}

	// IN: Objects
	// OUT: none
	public void WAIT(object[] args)
	{
		if (eventManager.eventWaitTime > 0) {
			waitTimer.Interval = eventManager.eventWaitTime;
			waitTimer.Enabled = true;
			waitTimer.Elapsed += eventManager.WaitComplete;
		}
	}

	// IN: Objects
	// OUT: none
	public void BIND(object[] args)
	{
		//Vector3 targetRotation;
		
		//Helper.PrintRDFTriples (rdfTriples);
		bool r = false;
		foreach (object arg in args) {
			if (arg == null) {
				r = true;
				break;
			}
		}

		if (r) {
			return;
		}

		GameObject container = null;
		Vector3 boundsCenter = Vector3.zero,boundsSize = Vector3.zero;

		if (args [args.Length - 1] is bool) {
			if ((bool)args [args.Length - 1] == true) {
				if (args [args.Length - 2] is String) {
					container = new GameObject ((args [args.Length - 2] as String).Replace("\"",""));
				}
				else {
					container = new GameObject ("bind");
				}

				if (args.Length-1 == 0) {
					container.transform.position = Vector3.zero;
				}

				// get bounds of composite to be created
				List<GameObject> objs = new List<GameObject> ();
				foreach (object arg in args) {
					if (arg is GameObject) {
						objs.Add (arg as GameObject);
					}
				}
				Bounds bounds = Helper.GetObjectWorldSize (objs);
				boundsCenter = container.transform.position = bounds.center;
				boundsSize = bounds.size;

				// nuke any relations between objects to be bound
				RelationTracker relationTracker = (RelationTracker)GameObject.Find("BehaviorController").GetComponent("RelationTracker");
				Dictionary<List<GameObject>,string> toRemove = new Dictionary<List<GameObject>, string>();

				foreach (DictionaryEntry pair in relationTracker.relations) {
					if (objs.Contains ((pair.Key as List<GameObject>) [0]) && objs.Contains ((pair.Key as List<GameObject>) [1])) {
						toRemove.Add (pair.Key as List<GameObject>,pair.Value as string);
					}
				}

				foreach (KeyValuePair<List<GameObject>,string> pair in toRemove) {
					relationTracker.RemoveRelation (pair.Key as List<GameObject>, pair.Value as string);
				}

				// bind objects
				foreach (object arg in args) {
					if (arg is GameObject) {
						(arg as GameObject).GetComponent<Voxeme>().enabled = false;

						Rigging rigging = (arg as GameObject).GetComponent<Rigging> ();
						if (rigging != null) {
							rigging.ActivatePhysics (false);
						}

						Collider[] colliders = (arg as GameObject).GetComponentsInChildren<Collider> ();
						foreach (Collider collider in colliders) {
							collider.isTrigger = false;
						}

						(arg as GameObject).transform.parent = container.transform;
						if (!(args [args.Length - 2] is String)) {
							container.name = container.name + " " + (arg as GameObject).name;
						}
					}
				}
			}
		}

		if (container != null) {
			container.AddComponent<Voxeme> ();
			container.AddComponent<Rigging> ();
			BoxCollider collider = container.AddComponent<BoxCollider> ();
			//collider.center = boundsCenter;
			collider.size = boundsSize;
			collider.isTrigger = true;
		}
	}

	// IN: Objects
	// OUT: none
	public void CLOSE(object[] args)
	{
		// close = seal interior
		// if concave - seal it somehow
		// if not - seal interior (e.g. close the cover of a book)
		// can't do that - closure impossible
		List<GameObject> lids = new List<GameObject> ();
		GameObject cover = null;
		bool hasInteriorComponent = false;

		if (args [0] is GameObject) {
			GameObject theme = (args [0] as GameObject);

			Voxeme voxComponent = theme.GetComponent<Voxeme> ();

			if (voxComponent != null) {
				if (!voxComponent.voxml.Type.Concavity.Contains("Concave")) {
					// find interior component
					// current: "interior" label
					// TODO: reason from "close" affordance
					GameObject interior = null;

					if (voxComponent.opVox.Type.Components.FindIndex (c => c.Item2.name == "interior") != -1) {
						interior = voxComponent.opVox.Type.Components.Find (c => c.Item2.name == "interior").Item2;
					}

					if (interior != null) {
						hasInteriorComponent = true;
						//Transform subVox = theme.transform.FindChild (theme.name + "*/");
						// find other components of theme not touching interior
						foreach (Triple<string, GameObject, int> component in voxComponent.opVox.Type.Components) {
							if ((component.Item2 != interior) && (component.Item2 != theme.gameObject)) {
								if (component.Item2.GetComponent<Voxeme>() != null) {
									Debug.Log (component.Item2.name);
									Debug.Log (Helper.GetObjectWorldSize (component.Item2).size);
	 								Debug.Log (Helper.GetObjectSize (component.Item2).size);
									Debug.Log (Helper.GetObjectWorldSize (interior).size);
									Debug.Log (component.Item2.transform.localPosition);

									// align comp minor axis to theme interior axis by rotating around comp major axis
									Vector3 compMajorAxis = Helper.GetObjectMajorAxis (component.Item2);
									Vector3 compMinorAxis = Helper.GetObjectMinorAxis (component.Item2);
									Vector3 interiorAxis = Constants.zAxis;
									Bounds compBounds = Helper.GetObjectSize (component.Item2);
									Bounds destBounds = Helper.GetObjectSize (interior); 

									Debug.Log (component.Item2.transform.rotation * compMinorAxis);
									Debug.Log (interiorAxis);
									Debug.Log (Vector3.Cross (component.Item2.transform.localRotation * compMinorAxis,
										interiorAxis));
									//float angle = Vector3.Angle (component.Item2.transform.rotation * compMinorAxis,
									//	interiorAxis * Mathf.Sign(Vector3.Cross(component.Item2.transform.rotation * compMinorAxis,
									//	interiorAxis).y));
									float angle = Vector3.Angle (component.Item2.transform.localRotation * compMinorAxis, interiorAxis);
									Debug.Log (angle);
									//Debug.Break ();
									Debug.Log (component.Item2.transform.localEulerAngles);
									Quaternion compAdjust = Quaternion.AngleAxis (angle, component.Item2.transform.localRotation * compMajorAxis) * component.Item2.transform.localRotation;
									Quaternion destAdjust = Quaternion.identity;//Quaternion.AngleAxis (angle, component.Item2.transform.rotation * compMajorAxis);
									Debug.Log (Helper.VectorToParsable (compAdjust.eulerAngles));

									Debug.Log (Helper.VectorToParsable (compBounds.size));
									Debug.Log (Helper.VectorToParsable (compAdjust * compBounds.size));
									Debug.Log (Helper.VectorToParsable (destBounds.size));

									Vector3 compAdjustedSize = compAdjust * compBounds.size;
									compAdjustedSize = new Vector3(Mathf.Abs(compAdjustedSize.x),Mathf.Abs(compAdjustedSize.y),Mathf.Abs(compAdjustedSize.z));

									Vector3 destAdjustedSize = destAdjust * destBounds.size;
									destAdjustedSize = new Vector3(Mathf.Abs(destAdjustedSize.x),Mathf.Abs(destAdjustedSize.y),Mathf.Abs(destAdjustedSize.z));

									// create new test bounds with vector*quat
									compBounds = new Bounds (compBounds.center, compAdjustedSize);
									Debug.Log (Helper.VectorToParsable (compBounds.size));

									destBounds = new Bounds (destBounds.center, destAdjustedSize);
									Debug.Log (Helper.VectorToParsable (destBounds.size));

									if (Helper.Covers (compBounds, destBounds, interiorAxis)) {	// check fit again
										cover = component.Item2;
										//Debug.Log (component.Item2.name);
										//component.Item2.GetComponent<Voxeme> ().targetRotation = (Quaternion.identity * theme.transform.rotation).eulerAngles;
										//return;
									}
								}
							}
						}
					}
					else {
						voxComponent.targetPosition = new Vector3 (float.NaN, float.NaN, float.NaN);
						return;
					}
				}
				else {

					if (voxComponent.supportingSurface != null) {
						if (theme != null) {
							// bug list: need to support nesting in OpVox (mug -> cup -> interior)
							GameObject interior = voxComponent.opVox.Type.Concavity.Item2;
							Debug.Log (interior.name);

							if (interior != null) {
								if (Concavity.IsEnabled(interior)){
									foreach (Voxeme voxeme in objSelector.allVoxemes) {
										//if (voxeme.gameObject.activeInHierarchy) {
											if ((voxeme.gameObject != theme) && (!Helper.IsSupportedBy(voxComponent.gameObject, voxeme.gameObject)) &&
												(voxeme.gameObject.transform.parent == null)) {
												if ((Helper.GetObjectWorldSize (voxeme.gameObject).size.x >= Helper.GetObjectWorldSize (interior).size.x) &&
													(Helper.GetObjectWorldSize (voxeme.gameObject).size.z >= Helper.GetObjectWorldSize (interior).size.z)) {
													lids.Add (voxeme.gameObject);
													lids = lids.OrderBy (o => (Helper.GetObjectWorldSize (o).size.x +
														Helper.GetObjectWorldSize (o).size.z) + Helper.GetObjectWorldSize (o).size.y).ToList ();
												}
											}
										//}
									}
								}
								else {
									voxComponent.targetPosition = new Vector3 (float.NaN, float.NaN, float.NaN);
									return;
								}
							}
						}
					}
				}
			}
		}

		// add to events manager
		if (args[args.Length-1] is bool) {
			if ((bool)args [args.Length - 1] == false) {
				GameObject movingComponent = null;
				float motionSpeed = 0.0f;
				string mannerString = string.Empty;
				if (!hasInteriorComponent) {
					if (lids.Count > 0) {
						eventManager.InsertEvent (string.Format ("put({0},on({1}))", lids [0].name, (args [0] as GameObject).name), 1);
						mannerString = string.Format ("put({0},on({1}))", lids [0].name, (args [0] as GameObject).name);
						movingComponent = lids [0];
						motionSpeed = movingComponent.GetComponent<Voxeme> ().moveSpeed = 
							RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);

						eventManager.OnSatisfactionCalculated (eventManager, new EventManagerArgs (eventManager.events [1]));
					}
					else {
						eventManager.InsertEvent (string.Format ("flip({0})", (args [0] as GameObject).name), 1);
						mannerString = string.Format ("flip({0})", (args [0] as GameObject).name);
						movingComponent = (args [0] as GameObject);
						motionSpeed = movingComponent.GetComponent<Voxeme> ().turnSpeed = 
							RandomHelper.RandomFloat (0.0f, 12.5f, (int)RandomHelper.RangeFlags.MaxInclusive);
					}
				}
				else {
					eventManager.InsertEvent (string.Format ("turn({0},{1},{2},{3})", cover.name,
						Helper.VectorToParsable (Constants.xAxis),
						Helper.VectorToParsable ((args [0] as GameObject).transform.rotation * Constants.xAxis),
						Helper.VectorToParsable ((args [0] as GameObject).transform.rotation * Constants.yAxis)), 1);
					mannerString = string.Format ("turn({0})", cover.name);
					movingComponent = cover;
					motionSpeed = movingComponent.GetComponent<Voxeme> ().turnSpeed = 
						RandomHelper.RandomFloat (0.0f, 12.5f, (int)RandomHelper.RangeFlags.MaxInclusive);
				}
					
				// record parameter values
				OnPrepareLog (this, new ParamsEventArgs ("MotionManner", mannerString));
				OnPrepareLog (this, new ParamsEventArgs ("MotionSpeed", motionSpeed.ToString ()));
				OnParamsCalculated (null, null);
			}
		}

		return;
	}

	// IN: Objects
	// OUT: none
	public void OPEN(object[] args)
	{
		// TODO: Rotate around a local axis: rotation = oldrotation * quaternion
		//Rotate around a world axis: rotation = quaternion * oldrotation

		// open = expose interior
		// if concave - unseal it somehow
		// if not - unseal interior (e.g. open the cover of a book)
		// can't do that - opening impossible
		GameObject lid = null;
		GameObject cover = null;
		bool hasInteriorComponent = false;
		Quaternion targetRotation = Quaternion.identity;
		Vector3 removeLocation = Vector3.zero;

		float coverOpenAngle = UnityEngine.Random.Range (-1.0f, -179.0f);

		if (args [0] is GameObject) {
			GameObject theme = (args [0] as GameObject);

			Voxeme voxComponent = theme.GetComponent<Voxeme> ();

			if (voxComponent != null) {
				if (!voxComponent.voxml.Type.Concavity.Contains("Concave")) {
					// find interior component
					// current: "interior" label
					// TODO: reason from "close" affordance
					GameObject interior = null;

					if (voxComponent.opVox.Type.Components.FindIndex (c => c.Item2.name == "interior") != -1) {
						interior = voxComponent.opVox.Type.Components.Find (c => c.Item2.name == "interior").Item2;
					}

					if (interior != null) {
						hasInteriorComponent = true;
						//Transform subVox = theme.transform.FindChild (theme.name + "*/");
						// find other components of theme not touching interior
						foreach (Triple<string, GameObject, int> component in voxComponent.opVox.Type.Components) {
							if ((component.Item2 != interior) && (component.Item2 != theme.gameObject)) {
								if (component.Item2.GetComponent<Voxeme>() != null) {
									Debug.Log (component.Item2.name);
									Debug.Log (Helper.GetObjectWorldSize (component.Item2).size);
									Debug.Log (Helper.GetObjectSize (component.Item2).size);
									Debug.Log (Helper.GetObjectWorldSize (interior).size);
									Debug.Log (component.Item2.transform.localPosition);

									// align comp minor axis to theme interior axis by rotating around comp major axis
									Vector3 compMajorAxis = Helper.GetObjectMajorAxis (component.Item2);
									Vector3 compMinorAxis = Helper.GetObjectMinorAxis (component.Item2);
									Vector3 interiorAxis = Constants.zAxis;
									Bounds compBounds = Helper.GetObjectSize (component.Item2);
									Bounds destBounds = Helper.GetObjectSize (interior); 

									Debug.Log (component.Item2.transform.rotation * compMinorAxis);
									Debug.Log (interiorAxis);
									Debug.Log (Vector3.Cross (component.Item2.transform.localRotation * compMinorAxis,
										interiorAxis));
									//float angle = Vector3.Angle (component.Item2.transform.rotation * compMinorAxis,
									//	interiorAxis * Mathf.Sign(Vector3.Cross(component.Item2.transform.rotation * compMinorAxis,
									//	interiorAxis).y));
									float angle = Vector3.Angle (component.Item2.transform.localRotation * compMinorAxis, interiorAxis);
									Debug.Log (angle);
									//Debug.Break ();
									Debug.Log (component.Item2.transform.localEulerAngles);
									Quaternion compAdjust = Quaternion.AngleAxis (angle, component.Item2.transform.localRotation * compMajorAxis) * component.Item2.transform.localRotation;
									Quaternion destAdjust = Quaternion.identity;//Quaternion.AngleAxis (angle, component.Item2.transform.rotation * compMajorAxis);
									Debug.Log (Helper.VectorToParsable (compAdjust.eulerAngles));

									Debug.Log (Helper.VectorToParsable (compBounds.size));
									Debug.Log (Helper.VectorToParsable (compAdjust * compBounds.size));
									Debug.Log (Helper.VectorToParsable (destBounds.size));

									Vector3 compAdjustedSize = compAdjust * compBounds.size;
									compAdjustedSize = new Vector3(Mathf.Abs(compAdjustedSize.x),Mathf.Abs(compAdjustedSize.y),Mathf.Abs(compAdjustedSize.z));

									Vector3 destAdjustedSize = destAdjust * destBounds.size;
									destAdjustedSize = new Vector3(Mathf.Abs(destAdjustedSize.x),Mathf.Abs(destAdjustedSize.y),Mathf.Abs(destAdjustedSize.z));

									// create new test bounds with vector*quat
									compBounds = new Bounds (compBounds.center, compAdjustedSize);
									Debug.Log (Helper.VectorToParsable (compBounds.size));

									destBounds = new Bounds (destBounds.center, destAdjustedSize);
									Debug.Log (Helper.VectorToParsable (destBounds.size));

									if (Helper.Covers (compBounds, destBounds, interiorAxis)) {	// check fit again
										cover = component.Item2;
										targetRotation = (cover.transform.rotation) * Quaternion.Euler(new Vector3 (0.0f, coverOpenAngle, 0.0f));
										Debug.Log (targetRotation.eulerAngles);
										Debug.Log (targetRotation * Constants.xAxis);

										//component.Item2.GetComponent<Voxeme> ().targetRotation = (Quaternion.identity * theme.transform.rotation).eulerAngles;
										//return;
									}
								}
							}
						}
					}
					else {
						voxComponent.targetPosition = new Vector3 (float.NaN, float.NaN, float.NaN);
						return;
					}
				}
				else {
					if (voxComponent.supportingSurface != null) {
						if (theme != null) {
							GameObject interior = voxComponent.opVox.Type.Concavity.Item2;

							if (interior != null) {
								if (!Concavity.IsEnabled(theme, out lid)){
									Debug.Log (lid.name);
									if (lid != voxComponent.supportingSurface.transform.root.gameObject) {
										Region region = Helper.FindClearRegion (voxComponent.supportingSurface.transform.root.gameObject, lid);
										Debug.Log (Helper.VectorToParsable(region.min));
										Debug.Log (Helper.VectorToParsable(region.max));
										Debug.Log (Helper.VectorToParsable(region.center));
										Bounds lidBounds = Helper.GetObjectWorldSize (lid);
										removeLocation = new Vector3(region.center.x,
											region.center.y + (lidBounds.center.y - lidBounds.min.y) + (lid.transform.position.y - lidBounds.center.y),
											region.center.z);
									}
									else {
										lid = null;
									}
									
//									foreach (Voxeme voxeme in objSelector.allVoxemes) {
//										if (voxeme.gameObject.activeInHierarchy) {
//											if ((voxeme.gameObject != theme) && (!Helper.IsSupportedBy(voxComponent.gameObject, voxeme.gameObject)) &&
//												(voxeme.gameObject.transform.parent == null)) {
//												if ((Helper.GetObjectWorldSize (voxeme.gameObject).size.x >= Helper.GetObjectWorldSize (interior).size.x) &&
//													(Helper.GetObjectWorldSize (voxeme.gameObject).size.z >= Helper.GetObjectWorldSize (interior).size.z)) {
//													lids.Add (voxeme.gameObject);
//													lids = lids.OrderBy (o => (Helper.GetObjectWorldSize (o).size.x +
//														Helper.GetObjectWorldSize (o).size.z) * Helper.GetObjectWorldSize (o).size.y).ToList ();
//												}
//											}
//										}
//									}
								}
								else {
									voxComponent.targetPosition = new Vector3 (float.NaN, float.NaN, float.NaN);
									return;
								}
							}
						}
					}
				}
			}
		}

		// add to events manager
		if (args[args.Length-1] is bool) {
			if ((bool)args[args.Length-1] == false) {
				GameObject movingComponent = null;
				float motionSpeed = 0.0f;
				Vector3 translocDir = Vector3.zero;
				float translocDist = 0.0f;
				float rotAngle = 0.0f;
				string mannerString = string.Empty;
				if (!hasInteriorComponent) {
					if (lid != null) {
						eventManager.InsertEvent (string.Format ("put({0},{1})", lid.name, Helper.VectorToParsable(removeLocation)), 1);
						mannerString = string.Format ("move({0})", lid.name);
						movingComponent = lid;
						motionSpeed = movingComponent.GetComponent<Voxeme> ().moveSpeed = 
							RandomHelper.RandomFloat (0.0f, 5.0f, (int)RandomHelper.RangeFlags.MaxInclusive);
						translocDir = removeLocation - movingComponent.transform.position;

						eventManager.OnSatisfactionCalculated (eventManager, new EventManagerArgs (eventManager.events [1]));
					} 
					else {
						eventManager.InsertEvent (string.Format ("flip({0})", (args [0] as GameObject).name), 1);
						mannerString = string.Format ("flip({0})", (args [0] as GameObject).name);
						movingComponent = (args [0] as GameObject);
						motionSpeed = movingComponent.GetComponent<Voxeme> ().turnSpeed = 
							RandomHelper.RandomFloat (0.0f, 12.5f, (int)RandomHelper.RangeFlags.MaxInclusive);
						rotAngle = 180.0f;
					}
				} 
				else {
					eventManager.InsertEvent (string.Format ("turn({0},{1},{2},{3})", cover.name,
						Helper.VectorToParsable(Constants.xAxis),
						Helper.VectorToParsable(targetRotation * Constants.xAxis),
						Helper.VectorToParsable((args [0] as GameObject).transform.rotation * Constants.yAxis)), 1);
					mannerString = string.Format ("turn({0})", cover.name);
					movingComponent = cover;
					motionSpeed = movingComponent.GetComponent<Voxeme> ().turnSpeed = 
						RandomHelper.RandomFloat (0.0f, 12.5f, (int)RandomHelper.RangeFlags.MaxInclusive);
					rotAngle = Quaternion.Angle(movingComponent.transform.rotation,targetRotation);
				}
					
				// record parameter values						
				OnPrepareLog (this, new ParamsEventArgs ("MotionManner", mannerString));
				OnPrepareLog (this, new ParamsEventArgs ("MotionSpeed", motionSpeed.ToString()));

				if (Vector3.Magnitude(translocDir) > 0.0f) {
					OnPrepareLog (this, new ParamsEventArgs ("TranslocDir", Helper.VectorToParsable(translocDir)));
				}

				if (rotAngle > 0.0f) {
					OnPrepareLog (this, new ParamsEventArgs ("RotAngle", rotAngle.ToString()));
				}
				OnParamsCalculated (null, null);
			}
		}

		return;
	}

	// IN: Objects
	// OUT: none
	public void UNBIND(object[] args)
	{
		if (args [0] is GameObject) {
			GameObject obj = (args [0] as GameObject);

			foreach (Transform transform in obj.GetComponentsInChildren<Transform>()) {
				transform.parent = null;
				Rigging rigging = transform.gameObject.GetComponent<Rigging> ().GetComponent<Rigging> ();
				if (rigging != null) {
					rigging.ActivatePhysics (true);
					transform.gameObject.GetComponent<Voxeme> ().enabled = true;
				}
			}

			GameObject.Destroy (obj);
		}
	}

	// IN: Objects
	// OUT: none
	public void ENABLE(object[] args)
	{	
		foreach (object obj in args) {
			if (obj is GameObject) {
				//Debug.Log (obj);
				objSelector.disabledObjects.Remove((obj as GameObject));
				(obj as GameObject).SetActive (true);
//				foreach (Renderer renderer in (obj as GameObject).GetComponentsInChildren<Renderer>()) {
//					renderer.enabled = true;
//				}
			}
		}

		macros.ClearMacros ();
		macros.PopulateMacros ();
	}

	// IN: Objects
	// OUT: none
	public void DISABLE(object[] args)
	{
		//Debug.Break ();
		foreach (object obj in args) {
			if (obj is GameObject) {
				if (!objSelector.disabledObjects.Contains ((obj as GameObject))) {
					objSelector.disabledObjects.Add ((obj as GameObject));
					(obj as GameObject).SetActive (false);
				}
			}
		}

		macros.ClearMacros ();
		macros.PopulateMacros ();
	}

	/* AGENT-DEPENDENT BEHAVIORS */

	// IN: Objects
	// OUT: none
	public void POINT(object[] args)
	{
		GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
		if (agent != null) {
			Animator anim = agent.GetComponentInChildren<Animator> ();
			GameObject leftGrasper = agent.GetComponent<FullBodyBipedIK>().references.leftHand.gameObject;
			GameObject rightGrasper = agent.GetComponent<FullBodyBipedIK>().references.rightHand.gameObject;
			GameObject grasper;

			if (args [args.Length - 1] is bool) {
				if ((bool)args [args.Length - 1] == true) {
					foreach (object arg in args) {
						if (arg is GameObject) {
							// find bounds corner closest to grasper
							Bounds bounds = Helper.GetObjectWorldSize((arg as GameObject));

							// which hand is closer?
							float leftToGoalDist = (leftGrasper.transform.position-bounds.ClosestPoint(leftGrasper.transform.position)).magnitude;
							float rightToGoalDist = (rightGrasper.transform.position-bounds.ClosestPoint(rightGrasper.transform.position)).magnitude;

							if (leftToGoalDist < rightToGoalDist) {
								grasper = leftGrasper;
								agent.GetComponent<GraspScript>().grasper = (int)Gestures.HandPose.LeftPoint;
							}
							else {
								grasper = rightGrasper;
								agent.GetComponent<GraspScript>().grasper = (int)Gestures.HandPose.RightPoint;
							}
								
							IKControl ikControl = agent.GetComponent<IKControl> ();
							if (ikControl != null) {
								Vector3 target;
								if (grasper == leftGrasper) {
									target = new Vector3 (bounds.min.x, bounds.min.y, bounds.center.z);
									ikControl.leftHandObj.transform.position = target;
								}
								else {
									target = new Vector3 (bounds.max.x, bounds.min.y, bounds.center.z);
									ikControl.rightHandObj.transform.position = target;
								}
							}
						}
					}
				}
			}
		}
	}

	// IN: Objects
	// OUT: none
	public void REACH(object[] args)
	{
		GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
		if (agent != null) {
			Animator anim = agent.GetComponentInChildren<Animator> ();
			GameObject leftGrasper = agent.GetComponent<FullBodyBipedIK>().references.leftHand.gameObject;
			GameObject rightGrasper = agent.GetComponent<FullBodyBipedIK>().references.rightHand.gameObject;
			GameObject grasper;
			GraspScript graspController = agent.GetComponent<GraspScript> ();
			Transform leftGrasperCoord = graspController.leftGrasperCoord;
			Transform rightGrasperCoord = graspController.rightGrasperCoord;
			Vector3 offset = graspController.graspTrackerOffset;

			if (args [args.Length - 1] is bool) {
				if ((bool)args [args.Length - 1] == true) {
					foreach (object arg in args) {
						if (arg is GameObject) {
							// find bounds corner closest to grasper
							Bounds bounds = Helper.GetObjectWorldSize((arg as GameObject));

							// which hand is closer?
							float leftToGoalDist = (leftGrasper.transform.position-bounds.ClosestPoint(leftGrasper.transform.position)).magnitude;
							float rightToGoalDist = (rightGrasper.transform.position-bounds.ClosestPoint(rightGrasper.transform.position)).magnitude;

							if (leftToGoalDist < rightToGoalDist) {
								grasper = leftGrasper;
							}
							else {
								grasper = rightGrasper;
							}
								
							IKControl ikControl = agent.GetComponent<IKControl> ();
							if (ikControl != null) {
								Vector3 target;
								if (grasper == leftGrasper) {
									//agent.GetComponent<GraspScript>().grasper = (int)Gestures.HandPose.LeftClaw;
									if ((grasper.GetComponent<BoxCollider> ().bounds.size.x > bounds.size.x) &&
									    (grasper.GetComponent<BoxCollider> ().bounds.size.z > bounds.size.z)) {
										target = new Vector3 (bounds.center.x, bounds.center.y, bounds.center.z);
									}
									else {
										target = new Vector3 (bounds.min.x, bounds.center.y, bounds.center.z);
									}
									ikControl.leftHandObj.transform.position = target+offset;
								}
								else {
									//agent.GetComponent<GraspScript>().grasper = (int)Gestures.HandPose.RightClaw;
									if ((grasper.GetComponent<BoxCollider> ().bounds.size.x > bounds.size.x) &&
										(grasper.GetComponent<BoxCollider> ().bounds.size.z > bounds.size.z)) {
										target = new Vector3 (bounds.center.x, bounds.center.y, bounds.center.z);
									}
									else {
										target = new Vector3 (bounds.max.x, bounds.center.y, bounds.center.z);
									}
									ikControl.rightHandObj.transform.position = target+offset;
								}
							}
						}
					}
				}
			}
		}
	}

	// IN: Objects
	// OUT: none
	public void GRASP(object[] args)
	{
		GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
		GameObject leftGrasper = agent.GetComponent<FullBodyBipedIK>().references.leftHand.gameObject;
		GameObject rightGrasper = agent.GetComponent<FullBodyBipedIK>().references.rightHand.gameObject;

		if (agent != null) {
			if (args [args.Length - 1] is bool) {
				if ((bool)args [args.Length - 1] == true) {
					foreach (object arg in args) {
						if (arg is GameObject) {
							InteractionObject interactionObject = (arg as GameObject).GetComponent<InteractionObject> ();
							if (interactionObject != null) {
								if (InteractionHelper.GetCloserHand (agent, (arg as GameObject)) == leftGrasper) {
									foreach (InteractionTarget interactionTarget in (arg as GameObject).GetComponentsInChildren<InteractionTarget>()) {
										if (interactionTarget.gameObject.name == "lHand") {
											interactionTarget.gameObject.SetActive (true);
										}
										else if (interactionTarget.gameObject.name == "rHand") {
											interactionTarget.gameObject.SetActive (false);
										}
									}

									Debug.Log (string.Format ("Starting {0} interaction with {1}", leftGrasper.name, (arg as GameObject).name));

									InteractionHelper.SetLeftHandTarget (agent, (arg as GameObject).GetComponentInChildren<InteractionTarget> ().transform);
									agent.GetComponent<InteractionSystem> ().StartInteraction (FullBodyBipedEffector.LeftHand, interactionObject, true);
								} 
								else if (InteractionHelper.GetCloserHand (agent, (arg as GameObject)) == rightGrasper) {
									foreach (InteractionTarget interactionTarget in (arg as GameObject).GetComponent<Voxeme>().interactionTargets) {
										if (interactionTarget.gameObject.name == "lHand") {
											interactionTarget.gameObject.SetActive (false);
										}
										else if (interactionTarget.gameObject.name == "rHand") {
											interactionTarget.gameObject.SetActive (true);
										}
									}

									Debug.Log (string.Format ("Starting {0} interaction with {1}", rightGrasper.name, (arg as GameObject).name));

									InteractionHelper.SetRightHandTarget (agent, (arg as GameObject).GetComponentInChildren<InteractionTarget> ().transform);
									agent.GetComponent<InteractionSystem> ().StartInteraction (FullBodyBipedEffector.RightHand, interactionObject, true);
								}
							}
							else {
								OutputHelper.PrintOutput (Role.Affector, "I can't interact with that object."); 
							}
						}
					}
				}
			}
		}

//			Bounds bounds = Helper.GetObjectWorldSize((args[0] as GameObject));
//			Animator anim = agent.GetComponentInChildren<Animator> ();
//			GameObject leftGrasper = agent.GetComponent<FullBodyBipedIK>().references.leftHand.gameObject;
//			GameObject rightGrasper = agent.GetComponent<FullBodyBipedIK>().references.rightHand.gameObject;
//			GameObject grasper;
//			Transform leftGraspTracker = agent.GetComponent<IKControl> ().leftHandObj;
//			Transform rightGraspTracker = agent.GetComponent<IKControl> ().rightHandObj;
//			Vector3 offset = agent.GetComponent<GraspScript> ().graspTrackerOffset;
//
//			// make sure we're reaching toward the object first
//			if (!bounds.Contains(leftGraspTracker.position-offset) && 
//				!bounds.Contains(rightGraspTracker.position-offset)) {
//				eventManager.InsertEvent (string.Format ("reach({0})", (args[0] as GameObject).name), 0);
//				//eventManager.RemoveEvent (eventManager.events.Count - 1);
//				return;
//			}
//
//			if (args [args.Length - 1] is bool) {
//				if ((bool)args [args.Length - 1] == true) {
//					foreach (object arg in args) {
//						if (arg is GameObject) {
//							//Debug.Log (rightGrasper.GetComponent<BoxCollider> ().bounds);
//							//Debug.Log (bounds);
//							if (leftGrasper.GetComponent<BoxCollider>().bounds.Intersects(bounds)) {
//								Rigging rigging = (arg as GameObject).GetComponent<Rigging> ();
//								if (rigging != null) {
//									rigging.ActivatePhysics (false);
//								}
//
//								RiggingHelper.RigTo ((arg as GameObject), leftGrasper);
//								Voxeme voxeme = (arg as GameObject).GetComponent<Voxeme> ();
//								voxeme.enabled = true;
//								voxeme.isGrasped = true;
//								voxeme.graspTracker = agent.GetComponent<IKControl>().leftHandObj;
//								voxeme.grasperCoord = agent.GetComponent<GraspScript>().leftGrasperCoord;
//							}
//							else if (rightGrasper.GetComponent<BoxCollider>().bounds.Intersects(bounds)) {
//								Rigging rigging = (arg as GameObject).GetComponent<Rigging> ();
//								if (rigging != null) {
//									rigging.ActivatePhysics (false);
//								}
//
//								RiggingHelper.RigTo ((arg as GameObject), rightGrasper);
//								Voxeme voxeme = (arg as GameObject).GetComponent<Voxeme> ();
//								voxeme.enabled = true;
//								voxeme.isGrasped = true;
//								voxeme.graspTracker = agent.GetComponent<IKControl>().rightHandObj;
//								voxeme.grasperCoord = agent.GetComponent<GraspScript>().rightGrasperCoord;
//							}
//							else {
//								OutputHelper.PrintOutput(Role.Affector,"I can't grasp the " + (arg as GameObject).name + ".  I'm not touching it."); 
//							}
//						}
//					}
//				}
//			}
//		}
	}

	// IN: Objects
	// OUT: none
	public void UNGRASP(object[] args)
	{
		GameObject agent = GameObject.FindGameObjectWithTag ("Agent");

		FullBodyBipedIK ik = agent.GetComponent<FullBodyBipedIK> ();
		InteractionSystem interactionSystem = agent.GetComponent<InteractionSystem> ();
		IKControl ikControl = agent.GetComponent<IKControl> ();

		GameObject leftGrasper = ik.references.leftHand.gameObject;
		GameObject rightGrasper = ik.references.rightHand.gameObject;

		if (agent != null) {
			if (args [args.Length - 1] is bool) {
				if ((bool)args [args.Length - 1] == true) {
					foreach (object arg in args) {
						if (arg is GameObject) {
							InteractionObject interactionObject = (arg as GameObject).GetComponent<InteractionObject> ();
							if (interactionObject != null) {
								if (interactionSystem.IsPaused(FullBodyBipedEffector.LeftHand)) {
									Debug.Log (string.Format ("Ending {0} interaction with {1}", leftGrasper.name, (arg as GameObject).name));

									//InteractionHelper.SetLeftHandTarget (agent, null);

									//InteractionHelper.SetLeftHandTarget (agent, ikControl.leftHandObj);
									InteractionHelper.SetLeftHandTarget (agent, null);
									ik.solver.SetEffectorWeights (FullBodyBipedEffector.LeftHand, 0.0f, 0.0f);
									agent.GetComponent<InteractionSystem> ().StopInteraction (FullBodyBipedEffector.LeftHand);
								}
								else if (interactionSystem.IsPaused(FullBodyBipedEffector.RightHand)) {
									Debug.Log (string.Format ("Ending {0} interaction with {1}", rightGrasper.name, (arg as GameObject).name));

									//InteractionHelper.SetRightHandTarget (agent, null);

									//InteractionHelper.SetRightHandTarget (agent, ikControl.rightHandObj);
									InteractionHelper.SetRightHandTarget (agent, null);
									ik.solver.SetEffectorWeights (FullBodyBipedEffector.RightHand, 0.0f, 0.0f);
									agent.GetComponent<InteractionSystem> ().StopInteraction (FullBodyBipedEffector.RightHand);

//									Debug.Log (ik.solver.GetEffector (FullBodyBipedEffector.RightHand).positionWeight);
//									Debug.Log (ik.solver.GetEffector (FullBodyBipedEffector.RightHand).rotationWeight);
								}

								foreach (InteractionTarget interactionTarget in (arg as GameObject).GetComponent<Voxeme>().interactionTargets) {
									interactionTarget.gameObject.SetActive (true);
								}
							}
							else {
								OutputHelper.PrintOutput (Role.Affector, "I can't interact with that object."); 
							}
						}
					}
				}
			}
		}
//		GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
//		if (agent != null) {
//			Animator anim = agent.GetComponentInChildren<Animator> ();
//			GameObject leftGrasper = agent.GetComponent<FullBodyBipedIK>().references.leftHand.gameObject;
//			GameObject rightGrasper = agent.GetComponent<FullBodyBipedIK>().references.rightHand.gameObject;
//			GameObject grasper = null;
//			Transform leftGrasperCoord = agent.GetComponent<GraspScript>().leftGrasperCoord;
//			Transform rightGrasperCoord = agent.GetComponent<GraspScript>().rightGrasperCoord;
//			GraspScript graspController = agent.GetComponent<GraspScript> ();
//
//			if (args [args.Length - 1] is bool) {
//				if ((bool)args [args.Length - 1] == true) {
//					foreach (object arg in args) {
//						if (arg is GameObject) {
//							Voxeme voxComponent = (arg as GameObject).GetComponent<Voxeme> ();
//							if (voxComponent != null) {
//								if (voxComponent.isGrasped) {
//									//voxComponent.transform.position = voxComponent.transform.position + 
//									//	(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);
//
//									if (voxComponent.grasperCoord == leftGrasperCoord) {
//										grasper = leftGrasper;
//									}
//									else if (voxComponent.grasperCoord == rightGrasperCoord) {
//										grasper = rightGrasper;
//									}
//									RiggingHelper.UnRig ((arg as GameObject), grasper);
//									graspController.grasper = (int)Gestures.HandPose.Neutral;
//									//agent.GetComponent<GraspScript>().isGrasping = false;
//									agent.GetComponent<IKControl> ().leftHandObj.position = graspController.leftDefaultPosition;
//									agent.GetComponent<IKControl> ().rightHandObj.position = graspController.rightDefaultPosition;
//
//									voxComponent.isGrasped = false;
//									voxComponent.graspTracker = null;
//									voxComponent.grasperCoord = null;
//								}
//							}
//						}
//					}
//				}
//			}
//		}
	}

	// IN: Objects
	// OUT: none
	public void DROP(object[] args)
	{
		GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
		if (agent != null) {
			Animator anim = agent.GetComponentInChildren<Animator> ();
			GameObject leftGrasper = agent.GetComponent<FullBodyBipedIK>().references.leftHand.gameObject;
			GameObject rightGrasper = agent.GetComponent<FullBodyBipedIK>().references.rightHand.gameObject;
			GameObject grasper = null;
			Transform leftGrasperCoord = agent.GetComponent<GraspScript>().leftGrasperCoord;
			Transform rightGrasperCoord = agent.GetComponent<GraspScript>().rightGrasperCoord;
			GraspScript graspController = agent.GetComponent<GraspScript> ();

			if (args [args.Length - 1] is bool) {
				if ((bool)args [args.Length - 1] == true) {
					foreach (object arg in args) {
						if (arg is GameObject) {
							Voxeme voxComponent = (arg as GameObject).GetComponent<Voxeme> ();
							if (voxComponent != null) {
								if (voxComponent.isGrasped) {
									//voxComponent.transform.position = voxComponent.transform.position + 
									//	(voxComponent.grasperCoord.position - voxComponent.gameObject.transform.position);

									if (voxComponent.grasperCoord == leftGrasperCoord) {
										grasper = leftGrasper;
									}
									else if (voxComponent.grasperCoord == rightGrasperCoord) {
										grasper = rightGrasper;
									}
									RiggingHelper.UnRig ((arg as GameObject), grasper);
									Rigging rigging = (arg as GameObject).GetComponent<Rigging> ();
									if (rigging != null) {
										rigging.ActivatePhysics (true);
									}
									graspController.grasper = (int)Gestures.HandPose.Neutral;
									//agent.GetComponent<GraspScript>().isGrasping = false;
									//agent.GetComponent<IKControl> ().leftHandObj.position = graspController.leftDefaultPosition;
									//agent.GetComponent<IKControl> ().rightHandObj.position = graspController.rightDefaultPosition;

									voxComponent.isGrasped = false;
									voxComponent.graspTracker = null;
									voxComponent.grasperCoord = null;
								}
							}
						}
					}
				}
			}
		}
	}

	// IN: Objects
	// OUT: none
	public void HOLD(object[] args)
	{
		// look for agent
		GameObject agent = GameObject.FindGameObjectWithTag("Agent");
		if (agent != null) {
			// add preconditions
			if (!SatisfactionTest.IsSatisfied (string.Format ("reach({0})", (args [0] as GameObject).name))) {
				eventManager.InsertEvent (string.Format ("reach({0})", (args [0] as GameObject).name), 0);
				eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 1);
				eventManager.InsertEvent (eventManager.evalOrig [string.Format ("hold({0})", (args [0] as GameObject).name)], 1);
				eventManager.RemoveEvent (3);
				return;
			}
			else {
				if (!SatisfactionTest.IsSatisfied (string.Format ("grasp({0})", (args [0] as GameObject).name))) {
					eventManager.InsertEvent (string.Format ("grasp({0})", (args [0] as GameObject).name), 0);
					eventManager.InsertEvent (eventManager.evalOrig [string.Format ("hold({0})", (args [0] as GameObject).name)], 1);
					eventManager.RemoveEvent (2);
					return;
				}
			}
		}
	}

	// IN: Objects
	// OUT: none
	public void TOUCH(object[] args)
	{
		GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
		if (agent != null) {
			Animator anim = agent.GetComponentInChildren<Animator> ();
			GameObject leftGrasper = anim.GetBoneTransform (HumanBodyBones.LeftHand).transform.gameObject;
			GameObject rightGrasper = anim.GetBoneTransform (HumanBodyBones.RightHand).transform.gameObject;
			GameObject grasper;
			GraspScript graspController = agent.GetComponent<GraspScript> ();
			Transform leftGrasperCoord = graspController.leftGrasperCoord;
			Transform rightGrasperCoord = graspController.rightGrasperCoord;
			Transform leftFingerCoord = graspController.leftFingerCoord;
			Transform rightFingerCoord = graspController.rightFingerCoord;

			if (args [args.Length - 1] is bool) {
				if ((bool)args [args.Length - 1] == true) {
					foreach (object arg in args) {
						if (arg is GameObject) {
							// find bounds corner closest to grasper
							Bounds bounds = Helper.GetObjectWorldSize((arg as GameObject));

							// which hand is closer?
							Vector3 leftClosestPoint = bounds.ClosestPoint(leftGrasper.transform.position);
							Vector3 rightClosestPoint = bounds.ClosestPoint(rightGrasper.transform.position);
							float leftToGoalDist = (leftGrasper.transform.position-leftClosestPoint).magnitude;
							float rightToGoalDist = (rightGrasper.transform.position-rightClosestPoint).magnitude;

							if (leftToGoalDist < rightToGoalDist) {
								grasper = leftGrasper;
							}
							else {
								grasper = rightGrasper;
							}

							IKControl ikControl = agent.GetComponent<IKControl> ();
							if (ikControl != null) {
								Vector3 target;
								if (grasper == leftGrasper) {
									target = leftClosestPoint;
									Vector3 dir = leftFingerCoord.position - leftGrasper.transform.position;
									dir = leftGrasper.transform.rotation * dir;
									ikControl.leftHandObj.transform.position = dir + target;//-leftFingerCoord.localPosition;//-(leftGrasper.transform.rotation*leftFingerCoord.localPosition);
								}
								else {
									target = rightClosestPoint;
									ikControl.rightHandObj.transform.position = target;//-rightFingerCoord.position;
								}
							}
						}
					}
				}
			}
		}
	}

	// IN: Objects
	// OUT: bool
	public bool IF(object[] args)
	{
		bool r = false;
		//TestRelation();

		return r;
	}

	// IN: Objects
	// OUT: bool
	public bool ADD(object[] args)
	{
		bool r = false;

		return r;
	}

	// IN: Object (single element array)
	// OUT: Vector3
	public Vector3 LOC(object[] args)
	{
		Vector3 loc = Vector3.zero;

		if (args [0] is GameObject) {
			loc = (args [0] as GameObject).transform.position;
		}

		return loc;
	}

	// IN: Object (single element array)
	// OUT: none
	public void DEF(object[] args)
	{
		Debug.Log (args [1].GetType ());
		if (args [1] is string) {
			string val = ((string)args [1]).Replace ("\"", "").Replace ("\'", "");
			Debug.Log (string.Format ("{0} : {1}",val, args [0]));
			if (!eventManager.globalVars.ContainsKey (val)) {
				eventManager.globalVars.Add (val, args [0]);
			}
			else {
				eventManager.globalVars[val] = args [0];
			}
		}

		foreach (string key in eventManager.globalVars.Keys) {
			Debug.Log (string.Format ("{0} : {1}",key, eventManager.globalVars[key]));
		}

		return;
	}

	// IN: Object (single element array)
	// OUT: String
	public String AS(object[] args)
	{
		Debug.Log (args [0].ToString ());
		return args[0].ToString();
	}

	// IN: Object (single element array)
	// OUT: String
	public void CLEAR_GLOBALS(object[] args)
	{
		eventManager.globalVars.Clear ();

		return;
	}

	// IN: Object (single element array)
	// OUT: none
	public void REPEAT(object[] args)
	{
		if (args [args.Length - 1] is bool) {
			if ((bool)args [args.Length - 1] == false) {
				if (args [0] is string) {
					int i;
					if (int.TryParse ((string)args [0], out i)) {
						Debug.Log (i);
						if (args [1] is string) {
							Debug.Log ((string)args [1]);
							for (int j = 0; j < i; j++) {
								eventManager.InsertEvent (((string)args [1]).Replace ("{", "(").Replace ("}", ")").Replace (":", ",")
									.Replace ("\"", "").Replace ("\'", ""), eventManager.events.Count);
								eventManager.InsertEvent ("clear_globals()",eventManager.events.Count);
							}
						}
					}
				}
			}
		}

		return;
	}

	public void ComposeSubevents(VoxML voxml, object[] args) {
		//List<GameObject> typedArgs = new List<GameObject> ();

		for (int i = 0; i < voxml.Type.Args.Count; i++) {
			VoxTypeArg arg = voxml.Type.Args [i];
			Debug.Log (arg.Value.Split (':') [0]);
			Debug.Log (arg.Value.Split (':') [1]);
		
			if (arg.Value.Split (':') [0].Contains ("[]")) {
				List<GameObject> filteredArgs = args.Where (a => (a.GetType () == typeof(GameObject))).Cast<GameObject> ().ToList ();
				filteredArgs = filteredArgs.Where (a => a.GetComponent<Voxeme> () != null).ToList ();
				filteredArgs = filteredArgs.Where (a => a.GetComponent<Voxeme> ().voxml.Lex.Type.Contains (arg.Value.Split (':') [1])).ToList ();
				filteredArgs = filteredArgs.Where (a => !eventManager.globalVars.ContainsValue (a)).ToList ();

				if (filteredArgs.Count > 0) {
					eventManager.globalVars.Add (arg.Value.Split (':') [0], filteredArgs);
				}
			}
			else {
				List<GameObject> filteredArgs = args.Where (a => (a.GetType () == typeof(GameObject))).Cast<GameObject> ().ToList ();
				filteredArgs = filteredArgs.Where (a => a.GetComponent<Voxeme> () != null).ToList ();
				filteredArgs = filteredArgs.Where (a => a.GetComponent<Voxeme> ().voxml.Lex.Type.Contains (arg.Value.Split (':') [1])).ToList ();
				filteredArgs = filteredArgs.Where (a => !eventManager.globalVars.ContainsValue (a)).ToList ();

				if (filteredArgs.Count > 0) {
					//typedArgs.Add (filteredArgs [0]);
					eventManager.globalVars.Add (arg.Value.Split (':') [0], filteredArgs [0]);
				}
			}
		}

		foreach (string key in eventManager.globalVars.Keys) {
			Debug.Log (string.Format ("{0} : {1}", key, eventManager.globalVars [key]));
		}
			
		int index = 1;
		foreach (VoxTypeSubevent subevent in voxml.Type.Body) {
			string[] commands = subevent.Value.Split (';');
			foreach (string command in commands) {
				string modifiedCommand = command;
				Regex q = new Regex ("[\'\"].*[\'\"]");
				MatchCollection matches = q.Matches (command);
				for (int i = 0; i < matches.Count; i++) {
					String match = matches [i].Value;
					String replace = match.Replace ("(", "{").Replace (")", "}").Replace (",", ":");
					modifiedCommand = command.Replace (match, replace);
				}
				eventManager.InsertEvent (modifiedCommand, index);
				index++;
				//Debug.Log (eventManager.EvaluateCommand (command));
			}
		}
	}
}
