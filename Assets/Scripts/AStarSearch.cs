using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AssemblyCSharp;
using Global;
using Arc = Global.Pair<UnityEngine.Vector3, UnityEngine.Vector3>;
using RootMotion.Demos;
using RootMotion.FinalIK;

public class PathNode {
	public Vector3 position;
	public bool examined;
	public float scoreFromStart;
	public float heuristicScore;
	public PathNode cameFrom;

	public PathNode(Vector3 pos) {
		position.x = pos.x;
		position.y = pos.y;
		position.z = pos.z;
		examined = false;
		scoreFromStart = Mathf.Infinity;
		heuristicScore = Mathf.Infinity;
	}
}

public class AStarSearch : MonoBehaviour {
	public bool debugNodes = false;

	public GameObject embeddingSpace;
	Bounds embeddingSpaceBounds;
	List<GameObject> debugVisual = new List<GameObject> ();

	public Vector3 defaultIncrement = Vector3.one;
	public Vector3 increment;
	public List<PathNode> nodes = new List<PathNode>();
	public List<Global.Pair<PathNode,PathNode>> arcs = new List<Global.Pair<PathNode,PathNode>> ();
	public Dictionary<Vector3, bool> quantizedSpaceToClear = new Dictionary<Vector3, bool>();
	public List<Vector3> path;

	public Vector3 start = new Vector3();
	public Vector3 goal = new Vector3();

	public int counterMax = 20;

	public float rigAttractionWeight;
	public FullBodyBipedIK bodyIk;
		

	// Use this for initialization
	void Start () {
		Renderer r = embeddingSpace.GetComponent<Renderer> ();
		embeddingSpaceBounds = r.bounds;
		Debug.Log (embeddingSpaceBounds.min);
		Debug.Log (embeddingSpaceBounds.max);
	}
	
	// Update is called once per frame
	void Update () {
		/*if ((goal - start).magnitude > 0.0f) {
			Debug.Log ("Start: " + start);
			Debug.Log ("Goal: " + goal);

			//PlanPath (start, goal, out plannedPath);

			// clear debug visualization
			foreach (GameObject o in debugVisual) {
				GameObject.Destroy(o);
			}
				
			foreach (Vector3 coord in plannedPath) {
				if (nodes.Contains(coord)) {
					AddDebugCube(coord);
				}
			}

			goal = start;	// temp hack to stop reprints
		}*/
	}

	void AddDebugCube(Vector3 coord) {
		GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
		cube.transform.position = coord;
		cube.transform.localScale = new Vector3 (increment.x / 10, increment.y / 10, increment.z / 10);
		cube.tag = "UnPhysic";

		debugVisual.Add (cube);
	}

	/**
	 * It is unclear why do you need a GameObject here
	 * 
	 * We shouldn't quantize the space before hand
	 */
	void QuantizeSpace(GameObject obj, Bounds embeddingSpaceBounds, Vector3 increment, params object[] constraints) {
		// fill the space with boxes!
		float xStart, yStart, zStart;
		float xEnd, yEnd, zEnd;
		Vector3 origin = obj.transform.position;
		Bounds objBounds = Helper.GetObjectWorldSize (obj);
		Vector3 originToCenterOffset = objBounds.center - origin;

		for (xStart = origin.x; xStart > embeddingSpaceBounds.min.x; xStart -= increment.x) {}
		xEnd = embeddingSpaceBounds.max.x;

		for (yStart = origin.y; yStart > embeddingSpaceBounds.min.y; yStart -= increment.y) {}
		yEnd = embeddingSpaceBounds.max.y;

		for (zStart = origin.z; zStart > embeddingSpaceBounds.min.z; zStart -= increment.z) {}
		zEnd = embeddingSpaceBounds.max.z;

//		Debug.Log(string.Format("X: ({0},{1})",xStart,xEnd));
//		Debug.Log(string.Format("Y: ({0},{1})",yStart,yEnd));
//		Debug.Log(string.Format("Z: ({0},{1})",zStart,zEnd));

		if (constraints.Length > 0) {
			foreach (object constraint in constraints) {
				Debug.Log (constraint);
				if (constraint is Bounds) {
					xStart = (((Bounds)constraint).min.x > xStart) ? ((Bounds)constraint).min.x : xStart;
					xEnd = (((Bounds)constraint).max.x < xEnd) ? ((Bounds)constraint).max.x : xEnd;

					yStart = (((Bounds)constraint).min.y > yStart) ? ((Bounds)constraint).min.y : yStart;
					yEnd = (((Bounds)constraint).max.y < yEnd) ? ((Bounds)constraint).max.y : yEnd;

					zStart = (((Bounds)constraint).min.z > zStart) ? ((Bounds)constraint).min.z : zStart;
					zEnd = (((Bounds)constraint).max.z < zEnd) ? ((Bounds)constraint).max.z : zEnd;
				}
				else if (constraint is string) {
					if ((constraint as string).Contains ('X')) {
						xStart = origin.x;
						xEnd = origin.x + increment.x;
					}

					if ((constraint as string).Contains ('Y')) {
						yStart = origin.y;
						yEnd = origin.y + increment.y;
					}

					if ((constraint as string).Contains ('Z')) {
						zStart = origin.z;
						zEnd = origin.z + increment.z;
					}
				}
			}
		}

//		Debug.Log(string.Format("X: ({0},{1})",xStart,xEnd));
//		Debug.Log(string.Format("Y: ({0},{1})",yStart,yEnd));
//		Debug.Log(string.Format("Z: ({0},{1})",zStart,zEnd));
			
		for (float fx = xStart; fx < xEnd; fx += increment.x) {
			for (float fy = yStart; fy < yEnd; fy += increment.y) {
				for (float fz = zStart; fz < zEnd; fz += increment.z) {

					// create test bounding box
					//Bounds testBounds = new Bounds(new Vector3 (fx+(increment.x/2), fy+(increment.y/2), fz+(increment.z/2)),
					//	new Vector3 (increment.x, increment.y, increment.z));

					Bounds testBounds = new Bounds(new Vector3 (fx,fy,fz)+originToCenterOffset,objBounds.size);
					// get all objects
					GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

					bool spaceClear = true;
					foreach (GameObject o in allObjects) {
						if ((o.tag != "UnPhysic") && (o.tag != "Ground")) {
							if (testBounds.Intersects (Helper.GetObjectWorldSize(o))) {
								spaceClear = false;
								break;
							}
						}
					}

					if (spaceClear) {
						// add node
						//Vector3 node = new Vector3 (fx + (increment.x / 2), fy + (increment.y / 2), fz + (increment.z / 2));
						Vector3 node = new Vector3 (fx, fy, fz);
						nodes.Add(new PathNode(node));

						//Debug.Log (node);

						if (debugNodes) {
							GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
							cube.transform.position = node;
							cube.transform.localScale = new Vector3 (increment.x / 10, increment.y / 10, increment.z / 10);
							cube.tag = "UnPhysic";
							cube.GetComponent<Renderer> ().enabled = true;
							Destroy (cube.GetComponent<Collider> ());

							debugVisual.Add (cube);
						}
					}
				}
			}
		}
	}

	bool testClear(GameObject obj, Vector3 curPoint){
		Bounds objBounds = Helper.GetObjectWorldSize (obj);
		Bounds testBounds = new Bounds(curPoint + objBounds.center - obj.transform.position, objBounds.size);
		// get all objects
		GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

		bool spaceClear = true;
		foreach (GameObject o in allObjects) {
			if ((o.tag != "UnPhysic") && (o.tag != "Ground")) {
				if (testBounds.Intersects (Helper.GetObjectWorldSize(o))) {
					spaceClear = false;
					break;
				}
			}
		}

		return spaceClear;
	}

	List<Vector3> getNeighborNodes(GameObject obj, Vector3 curPos, Vector3 increment, int step){
		// In general 
		// step * increment = size of object
		var neighbors = new List<Vector3> ();
		for (int i = -step; i <= step; i++)
			for (int j = -step; j <= step; j++)
				for (int k = -step; k <= step; k++) {
					// No overlapping between neighbor and curNode
					// at least one size equal |step|
					if (i*i == step*step || j*j == step*step || k*k == step*step) {
						Vector3 newNode = new Vector3 (curPos.x + i * increment.x, curPos.y + j * increment.y, curPos.z + k * increment.z);
						if (testClear(obj, newNode))
							neighbors.Add (newNode);
					}
				}

		// specialNodes are also neighbors
		neighbors.AddRange(specialNodes);
		return neighbors;
	}

	/*
	* PlotArcs is not necessary 
	*/
	public void PlotArcs(Bounds objBounds) {
		RaycastHit hitInfo;
		for (int i = 0; i < nodes.Count-1; i++) {
			for (int j = i+1; j < nodes.Count; j++) {
				Vector3 dir = (nodes [j].position - nodes [i].position);
				float dist = dir.magnitude;
				bool blocked = Physics.Raycast (nodes [i].position, dir.normalized, out hitInfo, dist);
				blocked |= Physics.Raycast (nodes [i].position-objBounds.extents, dir.normalized, out hitInfo, dist);
				blocked |= Physics.Raycast (nodes [i].position+objBounds.extents, dir.normalized, out hitInfo, dist);
				if (!blocked) {
					arcs.Add (new Global.Pair<PathNode, PathNode> (nodes [i], nodes [j]));
				}
			}
		}
	}

	/*
	 * Check if the path from first to second with objBounds width is blocked
	 */ 
	bool isBlock(Bounds objBounds, Vector3 first, Vector3 second) {
		RaycastHit hitInfo;
		Vector3 dir = (second - first);
		float dist = dir.magnitude;
		bool blocked = Physics.Raycast (first, dir.normalized, out hitInfo, dist);
		blocked |= Physics.Raycast (first-objBounds.extents, dir.normalized, out hitInfo, dist);
		blocked |= Physics.Raycast (first+objBounds.extents, dir.normalized, out hitInfo, dist);

		return blocked;
	}

	Dictionary<Vector3, Vector3> cameFrom;
	Dictionary<Vector3, float> gScore;
	Dictionary<Vector3, float> hScore;
	HashSet<Vector3> specialNodes = new HashSet<Vector3>();

	public class BetterHeuristic : Comparer<Vector3> 
	{
		Dictionary<Vector3, float> gScore;
		Dictionary<Vector3, float> hScore;

		public BetterHeuristic(Dictionary<Vector3, float> gScore, Dictionary<Vector3, float> hScore) {
			this.gScore = gScore;
			this.hScore = hScore;
		}

		// Compares by Length, Height, and Width.
		public override int Compare(Vector3 x, Vector3 y)
		{
			if ( gScore[x] + hScore[x] < gScore[y] + hScore[y]) 
				return -1;
			if ( gScore[x] + hScore[x] > gScore[y] + hScore[y]) 
				return 1;
			return 0;
		}

	}

	public class BetterHeuristicOld : Comparer<PathNode> 
	{
		
		// Compares by Length, Height, and Width.
		public override int Compare(PathNode x, PathNode y)
		{
			if ( x.scoreFromStart + x.heuristicScore < y.scoreFromStart + y.heuristicScore) 
				return -1;
			if ( x.scoreFromStart + x.heuristicScore < y.scoreFromStart + y.heuristicScore) 
				return 1;
			return 0;
		}

	}

	/*
	 * Look for closest point to goalPos that make a quantized distance to obj
	 */ 
	Vector3 lookForClosest( Vector3 goalPos, GameObject obj, Vector3 increment) {
		float dist = Mathf.Infinity;
		Vector3 closest = goalPos;

		var distance = goalPos - obj.transform.position;
		var quantizedDistance = new Vector3( distance.x / increment.x, distance.y / increment.y, distance.z / increment.z);

		var quantizedDistanceX = (int)quantizedDistance.x;
		var quantizedDistanceY = (int)quantizedDistance.y;
		var quantizedDistanceZ = (int)quantizedDistance.z;

		for (int x = quantizedDistanceX; x <= quantizedDistanceX + 1; x++)
			for (int y = quantizedDistanceY; y <= quantizedDistanceY + 1; y++)
				for (int z = quantizedDistanceZ; z <= quantizedDistanceZ + 1; z++) {
					var candidate = new Vector3 (x * increment.x + obj.transform.position.x, y * increment.y + obj.transform.position.y, z * increment.z + obj.transform.position.z);

					if (testClear (obj, candidate)) {
						float temp = (candidate - goalPos).magnitude;

						if (dist > temp) {
							dist = temp;
							closest = candidate;
						}
					}
				}
		
		return closest;
	}

	List<Vector3> ReconstructPath2(Vector3 firstNode, Vector3 lastNode) {
		path = new List<Vector3> ();
		Vector3 node = lastNode;

		//path.Add (lastNode.position);

		while (node != firstNode) {
			path.Insert (0, node);
			node = cameFrom[node];
		}

		return path;
	}

	float getGScore( Vector3 fromPoint, Vector3 explorePoint) {
		return gScore[fromPoint] + (explorePoint - fromPoint).magnitude;
	}

	float getHScore( Vector3 explorePoint, Vector3 goalPoint) {
		return (goalPoint - explorePoint).magnitude;
	}

	float getErgonomicScore( Vector3 point) {
		return (bodyIk.solver.rightArmChain.nodes [0].transform.position - point).magnitude;
	}

	float getGScoreErgonomic( Vector3 fromPoint, Vector3 explorePoint) {
		if (bodyIk != null) {
			return gScore [fromPoint] + (explorePoint - fromPoint).magnitude * (1 + rigAttractionWeight * (getErgonomicScore (fromPoint) + getErgonomicScore (explorePoint)));
		}
		else {
			return gScore [fromPoint] + (explorePoint - fromPoint).magnitude;
		}
	}

	float getHScoreErgonomic( Vector3 explorePoint, Vector3 goalPoint) {
		// a discount factor of 2 so that the algorith would be faster
		if (bodyIk != null) {
			return (goalPoint - explorePoint).magnitude * (1 + rigAttractionWeight / 2 * (getErgonomicScore (goalPoint) + getErgonomicScore (explorePoint)));
		}
		else {
			return (goalPoint - explorePoint).magnitude;
		}
	}

	// A plan path that run faster and more smooth
	public void PlanPath2(Vector3 startPos, Vector3 goalPos, out List<Vector3> path, GameObject obj, params object[] constraints) {
		Debug.Log ("========== In plan ========= " + goalPos);
		cameFrom = new Dictionary<Vector3, Vector3> ();
		gScore = new Dictionary<Vector3, float> ();
		hScore = new Dictionary<Vector3, float> ();
		specialNodes = new HashSet<Vector3>();

		// init empty path
		path = new List<Vector3>();

		MinHeap<Vector3> openSet = new MinHeap<Vector3>(new BetterHeuristic(gScore, hScore));
		var openSetForCheck = new HashSet<Vector3> ();

		// Closed set can be used because euclidean distance is monotonic
		var closedSet = new HashSet<Vector3> ();

		var objectBound = Helper.GetObjectWorldSize (obj);

		Vector3 size = Helper.GetObjectWorldSize (obj).size;

		Vector3 increment = defaultIncrement;

		foreach (object constraint in constraints) {
			if (constraint is string) {
				if ((constraint as string).Contains ('X')) {
					increment = new Vector3(0.0f,increment.y,increment.z);
				}

				if ((constraint as string).Contains ('Y')) {
					increment = new Vector3(increment.x,0.0f,increment.z);
				}

				if ((constraint as string).Contains ('Z')) {
					increment = new Vector3(increment.x,increment.y,0.0f);
				}
			}
		}

		int step = 1;

		Debug.Log (" ======== size.magnitude ====== " + size.magnitude);
		Debug.Log (" ======== defaultIncrement.magnitude ====== " + defaultIncrement.magnitude);


//		if (size.magnitude > defaultIncrement.magnitude) {
//			step = (int) (size.magnitude / defaultIncrement.magnitude) + 1;
//
//			increment = new Vector3 (size.x / step, size.y / step, size.z / step);
//		}

		Debug.Log (" ======== increment ====== " + increment);
		Debug.Log (" ======== step ====== " + step);

		openSet.Add (startPos);
		openSetForCheck.Add (startPos);

		Vector3 endPos = new Vector3();
		// if constraints contain a voxeme
		Voxeme testTarget = constraints.OfType<Voxeme> ().FirstOrDefault ();
		if (testTarget != null) {
			// if that object is concave (e.g. cup)
			// if goalPos is within the bounds of target (e.g. in cup)
			if (testTarget.voxml.Type.Concavity.Contains ("Concave") && Helper.GetObjectWorldSize (testTarget.gameObject).Contains (goalPos)) {
				// This endPos is special, and requires a special handling to avoid path not found
				var specialPos = new Vector3(goalPos.x, Helper.GetObjectWorldSize (testTarget.gameObject).max.y+size.y, goalPos.z);
				endPos = specialPos;
				specialNodes.Add (specialPos);
			}
			else {
				endPos = lookForClosest (goalPos, obj, increment);
			}
		}
		else {
			endPos = lookForClosest (goalPos, obj, increment);
		}

		gScore [startPos] = 0 ;
		//hScore [startPos] = new Vector3 (endPos.x - startPos.x, endPos.y - startPos.y, endPos.z - startPos.z).magnitude;
		hScore [startPos] = getHScoreErgonomic( startPos, goalPos ) ;

		Debug.Log (" ========= obj.transform.position ======== " + obj.transform.position);
		Debug.Log (" ======== start ====== " + startPos);
		Debug.Log (" ======== goal ====== " + goalPos);
		Debug.Log (" ======== end ====== " + endPos);

		// starting with startNode, for each neighborhood node of last node, assess A* heuristic
		// using best node found until endNode reached

		int counter = 0;

		Vector3 curPos = new Vector3();

		float bestMagnitude = Mathf.Infinity;
		Vector3 bestLastPos = new Vector3();

		while (openSet.Count > 0 && counter < counterMax) {
			// O(1)
			curPos = openSet.TakeMin ();

			Debug.Log (counter + " ======== curNode ====== (" + curPos + ") " + gScore [curPos] + " " + hScore [curPos] + " " + (gScore[curPos] + hScore[curPos]) );

			float currentDistance = (curPos - endPos).magnitude;
			if (currentDistance < bestMagnitude){
				bestMagnitude = currentDistance;
				bestLastPos = curPos;
			}

			// short cut
			// if reached end node
			if ((curPos - endPos).magnitude < Constants.EPSILON) {
				Debug.Log ("=== counter === " + counter);
				// extend path to goal node (goal position)
				cameFrom[goalPos] = curPos;
				path = ReconstructPath2 (startPos, goalPos);
				Debug.Log ("====== path ===== ");
				foreach (var point in path) {
					Debug.Log (point);
				}
				return;
			}
				
			closedSet.Add (curPos);

			var neighbors = getNeighborNodes (obj, curPos, increment, step);

			foreach (var neighbor in neighbors) {
				if (!closedSet.Contains (neighbor) && !isBlock(objectBound, curPos, neighbor) ) {
					float tentativeGScore = getGScoreErgonomic (curPos, neighbor);

					if (gScore.ContainsKey (neighbor) && tentativeGScore > gScore [neighbor])
						continue;

					cameFrom[neighbor] = curPos;
					gScore[neighbor] = tentativeGScore;
					hScore[neighbor] = getHScoreErgonomic(neighbor, goalPos);
					// Debug.Log ("=== candidate === (" + neighbor + ") " + gScore [neighbor] + " " + hScore [neighbor] + " " + (gScore [neighbor] + hScore [neighbor]));

					// If neighbor is not yet in openset 
					// Add it
					// Heap is automatically rearranged
					if (!openSet.Has (neighbor)) {
//						Debug.Log ("=== Add candidate === (" + neighbor + ")");
						openSet.Add (neighbor);
					} else {
						// If neighbor is already there, update the heap
//						Debug.Log ("=== Update candidate === (" + neighbor + ")");
						openSet.Update (neighbor);
					}
				}
			}

			counter += 1;
		}

		path = ReconstructPath2 (startPos, bestLastPos);
		Debug.Log ("====== path ===== ");
		foreach (var point in path) {
			Debug.Log (point);
		}
	}


	// constraints should be the target object!
	// Plan path uses a heuristic function h(n) of manhattan distance
	// Just change to the Euclidian distance for better performance
	public void PlanPath(Vector3 startPos, Vector3 goalPos, out List<Vector3> path, GameObject obj, params object[] constraints) {
		Debug.Log ("====== startPos ===== " + startPos);
		Debug.Log ("====== goalPos ===== " + goalPos);
		// clear nodes
		nodes.Clear ();

		// init empty path
		List<PathNode> plannedPath = new List<PathNode>();

		//List<PathNode> openSet = new List<PathNode> ();
		MinHeap<PathNode> openSet = new MinHeap<PathNode>(new BetterHeuristicOld());
		List<PathNode> closedSet = new List<PathNode> ();

		PathNode endNode = null;

		path = new List<Vector3>();

		Vector3 size = Helper.GetObjectWorldSize (obj).size;
		//increment = size.magnitude/2  > defaultIncrement.magnitude ? new Vector3(size.x/2, size.y/2, size.z/2) : defaultIncrement;
		increment = size.magnitude  > defaultIncrement.magnitude ? size : defaultIncrement;

		var watch = System.Diagnostics.Stopwatch.StartNew ();

		PathNode startNode = new PathNode (startPos);
		startNode.scoreFromStart = 0;

		// Manhattan distance
		startNode.heuristicScore = Mathf.Abs(goalPos.x - startNode.position.x) + Mathf.Abs(goalPos.y - startNode.position.y) +
			Mathf.Abs(goalPos.z - startNode.position.z);
		// Euclidan distance
//		startNode.heuristicScore = new Vector3 (goalPos.x - startNode.position.x, goalPos.y - startNode.position.y, goalPos.z - startNode.position.z).magnitude;
		nodes.Add(startNode);
		openSet.Add (startNode);
		QuantizeSpace (obj, embeddingSpaceBounds, increment, constraints);	// set increment to moving object size, clean up after each run

		watch.Stop ();
		Debug.Log ("========= Time to quantize space " + watch.ElapsedMilliseconds);
		// find closest node to goal
		// TODO: if goal is inside concave object (concave voxeme provided as constraint)
		//	find closest node to goal such that arc from testNode to goal
		//	does not intersect non-concave component of object

		// if constraints contain a voxeme
		Voxeme testTarget = constraints.OfType<Voxeme> ().FirstOrDefault ();
		if (testTarget != null) {
			// if that object is concave (e.g. cup)
			// if goalPos is within the bounds of target (e.g. in cup)
			if (testTarget.voxml.Type.Concavity.Contains ("Concave") && Helper.GetObjectWorldSize (testTarget.gameObject).Contains (goalPos)) {
				endNode = new PathNode (new Vector3(goalPos.x,
					Helper.GetObjectWorldSize (testTarget.gameObject).max.y+size.y, goalPos.z));
				nodes.Add (endNode);
				//Debug.Break();
			}
			else {
				float dist = Mathf.Infinity;
				foreach (PathNode node in nodes) {
					if ((node.position - goalPos).magnitude < dist) {	// if dist from this node to goal < dstance from previous node in list to goal
						dist = (node.position - goalPos).magnitude;
						endNode = node;
					}
				}
			}
		}
		else {
			float dist = Mathf.Infinity;
			foreach (PathNode node in nodes) {
				if ((node.position - goalPos).magnitude < dist) {	// if dist from this node to goal < dstance from previous node in list to goal
					dist = (node.position - goalPos).magnitude;
					endNode = node;
				}
			}
		}

		Debug.Log ("========= endNode ========== " + endNode);

		//PathNode endNode = new PathNode(goalPos);
		//nodes.Add (endNode);

		watch = System.Diagnostics.Stopwatch.StartNew ();

		PlotArcs (Helper.GetObjectWorldSize (obj));

		watch.Stop ();

		Debug.Log ("========= Time to plot arcs " + watch.ElapsedMilliseconds);
		//return;

		//path.Add(startPos);

		PathNode nextNode = new PathNode(embeddingSpaceBounds.max+Vector3.one);

		plannedPath.Add (new PathNode(startPos));

		// starting with startNode, for each neighborhood node of last node, assess A* heuristic
		// using best node found until endNode reached
		while (openSet.Count > 0) {
			Debug.Log (" ======== openSet.Count ====== " + openSet.Count);

			// O(1)
			PathNode curNode = openSet.TakeMin ();

			// O(n)
			//			PathNode curNode = null;
//
//			float testHeuristicScore = Mathf.Infinity;
//			foreach (PathNode node in openSet) {
//				if (node.heuristicScore < testHeuristicScore) {
//					testHeuristicScore = node.heuristicScore;
//					curNode = node;
//				}
//			}

			//nextNode = embeddingSpaceBounds.max + Vector3.one;
			//dist = (nextNode - endNode).magnitude;
			//float dist = ((embeddingSpaceBounds.max + Vector3.one)-endNode.position).magnitude;

			// if reached end node
			if ((curNode.position - endNode.position).magnitude < Constants.EPSILON) {
				// extend path to goal node (goal position)
				PathNode goalNode = new PathNode (goalPos);
				goalNode.cameFrom = curNode;
				path = ReconstructPath (startNode, goalNode);
				Debug.Log ("====== path ===== ");
				foreach (var point in path) {
					Debug.Log (point);
				}
				break;
			}
				
			closedSet.Add (curNode);



			var arcList = arcs.Where(n => ((n.Item1.position - curNode.position).magnitude < Constants.EPSILON) || 
				(n.Item2.position-curNode.position).magnitude < Constants.EPSILON).ToList();
			foreach (var arc in arcList) {
				float testScore;
				if ((arc.Item1.position - curNode.position).magnitude < Constants.EPSILON) {
					if (!closedSet.Contains(arc.Item2)) {
						testScore = curNode.scoreFromStart + (arc.Item2.position - curNode.position).magnitude;
						if (testScore < arc.Item2.scoreFromStart) {
							nextNode = arc.Item2;
							arc.Item2.cameFrom = curNode;
							arc.Item2.scoreFromStart = testScore;
							// Manhattan distance
//							arc.Item2.heuristicScore = arc.Item2.scoreFromStart + 
//								(Mathf.Abs(goalPos.x - arc.Item2.position.x) + Mathf.Abs(goalPos.y - arc.Item2.position.y) +
//									Mathf.Abs(goalPos.z - arc.Item2.position.z));

							// Euclidian distance
							arc.Item2.heuristicScore = arc.Item2.scoreFromStart +
							new Vector3 (goalPos.x - arc.Item2.position.x, goalPos.y - arc.Item2.position.y, goalPos.z - arc.Item2.position.z).magnitude;

							if (!openSet.Contains (arc.Item2)) {
								openSet.Add (arc.Item2);
							}
						}
					}
				}
				else if ((arc.Item2.position - curNode.position).magnitude < Constants.EPSILON) {
					if (!closedSet.Contains(arc.Item1)) {
						testScore = curNode.scoreFromStart + (arc.Item1.position - curNode.position).magnitude;
						if (testScore < arc.Item1.scoreFromStart) {
							nextNode = arc.Item1;
							arc.Item1.cameFrom = curNode;
							arc.Item1.scoreFromStart = testScore;

							// Manhattan distance
//							arc.Item1.heuristicScore = arc.Item1.scoreFromStart + 
//								(Mathf.Abs(goalPos.x - arc.Item1.position.x) + Mathf.Abs(goalPos.y - arc.Item1.position.y) +
//									Mathf.Abs(goalPos.z - arc.Item1.position.z));

							// Euclidian distance
							arc.Item1.heuristicScore = arc.Item1.scoreFromStart +
								new Vector3 (goalPos.x - arc.Item1.position.x, goalPos.y - arc.Item1.position.y, goalPos.z - arc.Item1.position.z).magnitude;

							if (!openSet.Contains (arc.Item1)) {
								openSet.Add (arc.Item1);
							}
						}
					}
				}
			}
		} 
	}

	List<Vector3> ReconstructPath(PathNode firstNode, PathNode lastNode) {
		path = new List<Vector3> ();
		PathNode node = lastNode;

		//path.Add (lastNode.position);

		while (node != firstNode) {
			path.Insert (0, node.position);
			node = node.cameFrom;
		}

		return path;
	}
}
