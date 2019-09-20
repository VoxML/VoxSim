using UnityEngine;
using System;

using MajorAxes;
using VoxSimPlatform.Global;

// generic qualitative relations
// grossly underspecified for now
namespace VoxSimPlatform {
    namespace SpatialReasoning {
        namespace QSR {
        	public static class QSR {
        		// TODO: Make camera relative
        		// left
        		public static bool Left(Bounds x, Bounds y) {
        			bool left = false;

        			if (x.min.x + Constants.EPSILON >= y.max.x - Constants.EPSILON) {
        				left = true;
        			}

        			return left;
        		}

        		// right
        		public static bool Right(Bounds x, Bounds y) {
        			bool right = false;

        			if (x.max.x - Constants.EPSILON <= y.min.x + Constants.EPSILON) {
        				right = true;
        			}

        			return right;
        		}

        		// behind
        		public static bool Behind(Bounds x, Bounds y) {
        			bool behind = false;

        			if (x.min.z + Constants.EPSILON >= y.max.z - Constants.EPSILON) {
        				behind = true;
        			}

        			return behind;
        		}

        		// in front
        		public static bool InFront(Bounds x, Bounds y) {
        			bool inFront = false;

        			if (x.max.z - Constants.EPSILON <= y.min.z + Constants.EPSILON) {
        				inFront = true;
        			}

        			return inFront;
        		}

        		// below
        		public static bool Below(Bounds x, Bounds y) {
        			bool below = false;

        			if (x.max.y - Constants.EPSILON <= y.min.y + Constants.EPSILON) {
        				below = true;
        			}

        			return below;
        		}

        		// above
        		public static bool Above(Bounds x, Bounds y) {
        			bool above = false;

        			if (x.min.y + Constants.EPSILON >= y.max.y - Constants.EPSILON) {
        				above = true;
        			}

        			return above;
                }

                // Custom composition
                public static bool ComposeQSR(Bounds x, Bounds y) {
                    throw new NotImplementedException();

                    return false;
                }
        	}
        }
    }
}