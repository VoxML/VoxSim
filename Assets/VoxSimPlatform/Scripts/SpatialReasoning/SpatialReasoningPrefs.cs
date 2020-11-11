using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxSimPlatform {
    namespace SpatialReasoning {
        public enum AlignmentPreference
        {
            Centered,
            Quadrant,
            Stochastic
        };

        public class SpatialReasoningPrefs : MonoBehaviour {
            public AlignmentPreference alignmentPreference;
            public bool bindOnSupport;

            // Start is called before the first frame update
            void Start() {

            }

            // Update is called once per frame
            void Update() {

            }
        }
    }
}
