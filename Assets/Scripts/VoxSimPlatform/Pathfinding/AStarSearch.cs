using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Arc = VoxSimPlatform.Global.Pair<UnityEngine.Vector3, UnityEngine.Vector3>;
using Debug = UnityEngine.Debug;
using RootMotion.FinalIK;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace Pathfinding {
        public static class AStarSearch {
            // not referenced anywhere
        	//void AddDebugCube(Vector3 coord) {
        	//	GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        	//	cube.transform.position = coord;
        	//	cube.transform.localScale = new Vector3(increment.x / 10, increment.y / 10, increment.z / 10);
        	//	cube.tag = "UnPhysic";

        	//	debugVisual.Add(cube);
        	//}

        	static bool TestClear(GameObject obj, Vector3 curPoint) {
        		Bounds objBounds = Helper.GetObjectWorldSize(obj);
        		Bounds testBounds = new Bounds(curPoint + objBounds.center - obj.transform.position, objBounds.size);
        		// get all objects
        		GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

        		bool spaceClear = true;
        		foreach (GameObject o in allObjects) {
        			if ((o.tag != "UnPhysic") && (o.tag != "Ground")) {
        				if (testBounds.Intersects(Helper.GetObjectWorldSize(o))) {
        					spaceClear = false;
        					break;
        				}
        			}
        		}

        		return spaceClear;
        	}

        	static List<Vector3> GetNeighborNodes(GameObject obj, Vector3 curPos, Vector3 increment, int step,
                HashSet<Vector3> specialNodes) {
        		// In general 
        		// step * increment = size of object
        		var neighbors = new List<Vector3>();
        		for (int i = -step; i <= step; i++)
        		for (int j = -step; j <= step; j++)
        		for (int k = -step; k <= step; k++) {
        			// No overlapping between neighbor and curNode
        			// at least one size equal |step|
        			if (i * i == step * step || j * j == step * step || k * k == step * step) {
        				Vector3 newNode = new Vector3(curPos.x + i * increment.x, curPos.y + j * increment.y,
        					curPos.z + k * increment.z);
        				if (TestClear(obj, newNode))
        					neighbors.Add(newNode);
        			}
        		}

        		// specialNodes are also neighbors
        		neighbors.AddRange(specialNodes);
        		return neighbors;
        	}

        	/*
        	 * Check if the path from first to second with objBounds width is blocked
        	 */
        	static bool IsBlocked(Bounds objBounds, Vector3 first, Vector3 second) {
        		RaycastHit hitInfo;
        		Vector3 dir = (second - first);
        		float dist = dir.magnitude;
        		bool blocked = Physics.Raycast(first, dir.normalized, out hitInfo, dist);
        		blocked |= Physics.Raycast(first - objBounds.extents, dir.normalized, out hitInfo, dist);
        		blocked |= Physics.Raycast(first + objBounds.extents, dir.normalized, out hitInfo, dist);

        		return blocked;
            }

        	public class ComparisonHeuristic : Comparer<Vector3> {
        		Dictionary<Vector3, float> gScore;
        		Dictionary<Vector3, float> hScore;

        		public ComparisonHeuristic(Dictionary<Vector3, float> gScore, Dictionary<Vector3, float> hScore) {
        			this.gScore = gScore;
        			this.hScore = hScore;
        		}

        		// Compares by Length, Height, and Width.
        		public override int Compare(Vector3 x, Vector3 y) {
        			if (gScore[x] + hScore[x] < gScore[y] + hScore[y])
        				return -1;
        			if (gScore[x] + hScore[x] > gScore[y] + hScore[y])
        				return 1;
        			return 0;
        		}
        	}

        	/*
        	 * Look for closest point to goalPos that make a quantized distance to obj
        	 */
        	static Vector3 LookForClosest(Vector3 goalPos, GameObject obj, Vector3 increment) {
        		float dist = Mathf.Infinity;
        		Vector3 closest = goalPos;

        		var distance = goalPos - obj.transform.position;
        		var quantizedDistance =
        			new Vector3(distance.x / increment.x, distance.y / increment.y, distance.z / increment.z);

        		var quantizedDistanceX = (int) quantizedDistance.x;
        		var quantizedDistanceY = (int) quantizedDistance.y;
        		var quantizedDistanceZ = (int) quantizedDistance.z;

        		for (int x = quantizedDistanceX; x <= quantizedDistanceX + 1; x++) {
        			for (int y = quantizedDistanceY; y <= quantizedDistanceY + 1; y++) {
        				for (int z = quantizedDistanceZ; z <= quantizedDistanceZ + 1; z++) {
        					Vector3 candidate = new Vector3(x * increment.x + obj.transform.position.x,
        						y * increment.y + obj.transform.position.y, z * increment.z + obj.transform.position.z);

        					if (TestClear(obj, candidate)) {
        						float temp = (candidate - goalPos).magnitude;

        						if (dist > temp) {
        							dist = temp;
        							closest = candidate;
        						}
        					}
        				}
        			}
        		}

        		return closest;
        	}

        	static List<Vector3> ReconstructPath(Vector3 firstNode, Vector3 lastNode,
                Dictionary<Vector3, Vector3> cameFrom) {
                List<Vector3> path = new List<Vector3>();
        		Vector3 node = lastNode;

        		//path.Add (lastNode.position);

        		while (node != firstNode) {
        			path.Insert(0, node);
        			node = cameFrom[node];
        		}

        		return path;
        	}

            static float GetErgonomicScore(FullBodyBipedIK bodyIk, Vector3 point) {
        		return (bodyIk.solver.rightArmChain.nodes[0].transform.position - point).magnitude;
        	}

        	static float GetGScoreErgonomic(GameObject agent, Vector3 fromPoint, Vector3 explorePoint, float rigAttractionWeight,
                Dictionary<Vector3, float> gScoreDict) {
                FullBodyBipedIK bodyIk = null;

                if (agent != null) {
                    bodyIk = agent.GetComponent<FullBodyBipedIK>();
                }
        		
                if (bodyIk != null) {
        			return gScoreDict[fromPoint] + (explorePoint - fromPoint).magnitude *
        			       (1 + rigAttractionWeight * (GetErgonomicScore(bodyIk, fromPoint) + GetErgonomicScore(bodyIk, explorePoint)));
        		}
        		else {
        			return gScoreDict[fromPoint] + (explorePoint - fromPoint).magnitude;
        		}
        	}

        	static float GetHScoreErgonomic(GameObject agent, Vector3 explorePoint, Vector3 goalPoint, float rigAttractionWeight) {
                FullBodyBipedIK bodyIk = null;

                if (agent != null) {
                    bodyIk = agent.GetComponent<FullBodyBipedIK>();
                }

        		// a discount factor of 2 so that the algorithm is faster
        		if (bodyIk != null) {
        			return (goalPoint - explorePoint).magnitude *
        			       (1 + rigAttractionWeight / 2 * (GetErgonomicScore(bodyIk, goalPoint) + GetErgonomicScore(bodyIk, explorePoint)));
        		}
        		else {
        			return (goalPoint - explorePoint).magnitude;
        		}
        	}

        	// A plan path that run faster and more smooth
        	public static List<Vector3> PlanPath(Vector3 startPos, Vector3 goalPos, GameObject obj,
        		params object[] constraints) {

                EventManager eventManager = GameObject.Find("BehaviorController").GetComponent<EventManager>();
                AStarSearchPrefs prefs = GameObject.Find("VoxWorld").GetComponent<AStarSearchPrefs>();

        		Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();
        		Dictionary<Vector3, float> gScore = new Dictionary<Vector3, float>();
        		Dictionary<Vector3, float> hScore = new Dictionary<Vector3, float>();
        		HashSet<Vector3> specialNodes = new HashSet<Vector3>();

                // init empty path
                List<Vector3> path = new List<Vector3>();

                Debug.Log("========== In plan ========= " + Helper.VectorToParsable(goalPos));
                // the compare method in ComparisonHeuristic class is called in 
                // Dominates method in VoxSimPlatform.Global.MinHeap class.
                // Dominates is called in bubble up and bubble down operation of the heap, 
                // which are called when openSet.Add(startPos),openSet.Add(neighbor) and openSet.Update(neighbor) are called. 
                MinHeap<Vector3> openSet = new MinHeap<Vector3>(new ComparisonHeuristic(gScore, hScore));
        		var openSetForCheck = new HashSet<Vector3>();

        		// Closed set can be used because euclidean distance is monotonic
        		var closedSet = new HashSet<Vector3>();

        		var objectBound = Helper.GetObjectWorldSize(obj);

        		Vector3 size = Helper.GetObjectWorldSize(obj).size;

        		Vector3 increment = prefs.defaultIncrement;

        		foreach (object constraint in constraints) {
        			if (constraint is string) {
        				if ((constraint as string).Contains('X')) {
        					increment = new Vector3(0.0f, increment.y, increment.z);
        				}

        				if ((constraint as string).Contains('Y')) {
        					increment = new Vector3(increment.x, 0.0f, increment.z);
        				}

        				if ((constraint as string).Contains('Z')) {
        					increment = new Vector3(increment.x, increment.y, 0.0f);
        				}
        			}
        		}

        		int step = 1;

        		Debug.Log(" ======== size.magnitude ====== " + size.magnitude);
        		Debug.Log(" ======== defaultIncrement.magnitude ====== " + prefs.defaultIncrement.magnitude);


        //		if (size.magnitude > defaultIncrement.magnitude) {
        //			step = (int) (size.magnitude / defaultIncrement.magnitude) + 1;
        //
        //			increment = new Vector3 (size.x / step, size.y / step, size.z / step);
        //		}

        		Debug.Log(" ======== increment ====== " + increment);
        		Debug.Log(" ======== step ====== " + step);

        		openSet.Add(startPos);
        		openSetForCheck.Add(startPos);

        		Vector3 endPos = new Vector3();
        		// if constraints contain a voxeme
        		Voxeme testTarget = constraints.OfType<Voxeme>().FirstOrDefault();
        		if (testTarget != null) {
        			Debug.Log(testTarget);
        			// if that object is concave (e.g. cup)
        			// if goalPos is within the bounds of target (e.g. in cup)
        			if (testTarget.voxml.Type.Concavity.Contains("Concave") &&
        			    Helper.GetObjectWorldSize(testTarget.gameObject).Contains(goalPos)) {
        				// This endPos is special, and requires a special handling to avoid path not found
        				var specialPos = new Vector3(goalPos.x, Helper.GetObjectWorldSize(testTarget.gameObject).max.y + size.y,
        					goalPos.z);
        				endPos = specialPos;
        				specialNodes.Add(specialPos);
        				Debug.Log(" ======== special ====== " + Helper.VectorToParsable(specialPos));
        			}
        			else {
        				endPos = LookForClosest(goalPos, obj, increment);
        			}
        		}
        		else {
        			endPos = LookForClosest(goalPos, obj, increment);
        		}

        		gScore[startPos] = 0;
        		//hScore [startPos] = new Vector3 (endPos.x - startPos.x, endPos.y - startPos.y, endPos.z - startPos.z).magnitude;
        		hScore[startPos] = GetHScoreErgonomic(eventManager.GetActiveAgent(), startPos, goalPos, prefs.rigAttractionWeight);

        		Debug.Log(" ========= obj.transform.position ======== " + Helper.VectorToParsable(obj.transform.position));
        		Debug.Log(" ======== start ====== " + Helper.VectorToParsable(startPos));
        		Debug.Log(" ======== goal ====== " + Helper.VectorToParsable(goalPos));
        		Debug.Log(" ======== end ====== " + Helper.VectorToParsable(endPos));

        		// starting with startNode, for each neighborhood node of last node, assess A* heuristic
        		// using best node found until endNode reached

        		int counter = 0;

        		Vector3 curPos = new Vector3();

        		float bestMagnitude = Mathf.Infinity;
        		Vector3 bestLastPos = new Vector3();

        		if ((goalPos - startPos).magnitude > (goalPos - endPos).magnitude) {
        			Debug.Log(string.Format("{0}-{1}={2}", Helper.VectorToParsable(goalPos), Helper.VectorToParsable(startPos),
        				(goalPos - startPos).magnitude));
        			Debug.Log(string.Format("{0}-{1}={2}", Helper.VectorToParsable(goalPos), Helper.VectorToParsable(endPos),
        				(goalPos - endPos).magnitude));
        			while (openSet.Count > 0 && counter < prefs.counterMax) {
        				// O(1)
        				curPos = openSet.TakeMin();

        				Debug.Log(counter + " ======== curNode ====== (" + Helper.VectorToParsable(curPos) + ") " +
        				          gScore[curPos] + " " + hScore[curPos] + " " + (gScore[curPos] + hScore[curPos]));

        				float currentDistance = (curPos - endPos).magnitude;
        				if (currentDistance < bestMagnitude) {
        					bestMagnitude = currentDistance;
        					bestLastPos = curPos;
        				}

        				// short cut
        				// if reached end node
        				if ((curPos - endPos).magnitude < Constants.EPSILON) {
        					Debug.Log("=== counter === " + counter);
        					// extend path to goal node (goal position)
        					cameFrom[goalPos] = curPos;
        					path = ReconstructPath(startPos, goalPos, cameFrom);
                            Debug.Log(string.Format("====== path ====== {0}", string.Join(",", path.Select(n => Helper.VectorToParsable(n)).ToArray())));

                            return path;
        				}

        				closedSet.Add(curPos);

        				var neighbors = GetNeighborNodes(obj, curPos, increment, step, specialNodes);

        				foreach (var neighbor in neighbors) {
        					if (!closedSet.Contains(neighbor) && !IsBlocked(objectBound, curPos, neighbor)) {
        						float tentativeGScore = GetGScoreErgonomic(eventManager.GetActiveAgent(), curPos, neighbor, prefs.rigAttractionWeight, gScore);

        						if (gScore.ContainsKey(neighbor) && tentativeGScore > gScore[neighbor])
        							continue;

        						cameFrom[neighbor] = curPos;
        						gScore[neighbor] = tentativeGScore;
        						hScore[neighbor] = GetHScoreErgonomic(eventManager.GetActiveAgent(), neighbor, goalPos, prefs.rigAttractionWeight);
        						// Debug.Log ("=== candidate === (" + neighbor + ") " + gScore [neighbor] + " " + hScore [neighbor] + " " + (gScore [neighbor] + hScore [neighbor]));

        						// If neighbor is not yet in openset 
        						// Add it
        						// Heap is automatically rearranged
        						if (!openSet.Has(neighbor)) {
        							Debug.Log("=== Add candidate === (" + Helper.VectorToParsable(neighbor) + ")");
        							openSet.Add(neighbor);
        						}
        						else {
        							// If neighbor is already there, update the heap
        							Debug.Log("=== Update candidate === (" + Helper.VectorToParsable(neighbor) + ")");
        							openSet.Update(neighbor);
        						}
        					}
        				}

        				counter += 1;
        			}
        		}
        		else // if the dist from startPos to goalPos < dist from goalPos to endPos (aka closest non-start node to endPos)
        		{
        			cameFrom[goalPos] = startPos;
        			bestLastPos = goalPos;
        		}

        		path = ReconstructPath(startPos, bestLastPos, cameFrom);
        		Debug.Log(string.Format("====== path ====== {0}",string.Join(",",path.Select(n => Helper.VectorToParsable(n)).ToArray())));

                return path;
        	}
        }
    }
}