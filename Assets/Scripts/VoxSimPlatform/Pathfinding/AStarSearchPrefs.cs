using UnityEngine;
using System.Collections;

namespace VoxSimPlatform {
    namespace Pathfinding {
        public class AStarSearchPrefs : MonoBehaviour {
            public GameObject embeddingSpace;
            Bounds embeddingSpaceBounds;

            public Vector3 defaultIncrement = Vector3.one;

            public int counterMax = 20;

            public float rigAttractionWeight;

            // Use this for initialization
            void Start() {
                Renderer r = embeddingSpace.GetComponent<Renderer>();
                embeddingSpaceBounds = r.bounds;
                Debug.Log(embeddingSpaceBounds.min);
                Debug.Log(embeddingSpaceBounds.max);
            }

            // Update is called once per frame
            void Update() {

            }
        }
    }
}
