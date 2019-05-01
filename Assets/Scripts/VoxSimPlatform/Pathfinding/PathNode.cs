using UnityEngine;

namespace VoxSimPlatform {
    namespace Pathfinding {
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
    }
}