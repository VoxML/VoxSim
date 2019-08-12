using UnityEngine;

using MajorAxes;
using VoxSimPlatform.Global;

// generic qualitative relations
// grossly underspecified for now
namespace VoxSimPlatform {
    namespace SpatialReasoning {
        namespace QSR {
            public static class RectAlgebra {

                // RECTANGLE ALGEBRA
                // less than
                public static bool LessThan(Bounds x, Bounds y, MajorAxis axis, bool invert = false) {
                    bool lessthan = false;

                    switch (axis) {
                        case MajorAxis.X:
                            if (invert) {
                                if (x.min.x + Constants.EPSILON > y.max.x - Constants.EPSILON) {
                                    lessthan = true;
                                }
                            }
                            else {
                                if (x.max.x - Constants.EPSILON < y.min.x + Constants.EPSILON) {
                                    lessthan = true;
                                }
                            }

                            break;

                        case MajorAxis.Y:
                            if (invert) {
                                if (x.min.y + Constants.EPSILON > y.max.y - Constants.EPSILON) {
                                    lessthan = true;
                                }
                            }
                            else {
                                if (x.max.y - Constants.EPSILON < y.min.y + Constants.EPSILON) {
                                    lessthan = true;
                                }
                            }

                            break;

                        case MajorAxis.Z:
                            if (invert) {
                                if (x.min.z + Constants.EPSILON > y.max.z - Constants.EPSILON) {
                                    lessthan = true;
                                }
                            }
                            else {
                                if (x.max.z - Constants.EPSILON < y.min.z + Constants.EPSILON) {
                                    lessthan = true;
                                }
                            }

                            break;

                        default:
                            break;
                    }

                    return lessthan;
                }

                // equal
                public static bool Equal(Bounds x, Bounds y, MajorAxis axis, bool invert = false) {
                    bool equal = false;

                    switch (axis) {
                        case MajorAxis.X:
                            if ((Mathf.Abs(x.min.x - y.min.x) <= Constants.EPSILON) &&
                                (Mathf.Abs(x.max.x - y.max.x) <= Constants.EPSILON)) {
                                equal = true;
                            }

                            break;

                        case MajorAxis.Y:
                            if ((Mathf.Abs(x.min.y - y.min.y) <= Constants.EPSILON) &&
                                (Mathf.Abs(x.max.y - y.max.y) <= Constants.EPSILON)) {
                                equal = true;
                            }

                            break;

                        case MajorAxis.Z:
                            if ((Mathf.Abs(x.min.z - y.min.z) <= Constants.EPSILON) &&
                                (Mathf.Abs(x.max.z - y.max.z) <= Constants.EPSILON)) {
                                equal = true;
                            }

                            break;

                        default:
                            break;
                    }

                    return equal;
                }

                // meets
                public static bool Meets(Bounds x, Bounds y, MajorAxis axis, bool invert = false) {
                    bool meets = false;

                    switch (axis) {
                        case MajorAxis.X:
                            if (invert) {
                                if (Mathf.Abs(x.min.x - y.max.x) <= Constants.EPSILON) {
                                    meets = true;
                                }
                            }
                            else {
                                if (Mathf.Abs(x.max.x - y.min.x) <= Constants.EPSILON) {
                                    meets = true;
                                }
                            }

                            break;

                        case MajorAxis.Y:
                            if (invert) {
                                if (Mathf.Abs(x.min.y - y.max.y) <= Constants.EPSILON) {
                                    meets = true;
                                }
                            }
                            else {
                                if (Mathf.Abs(x.max.y - y.min.y) <= Constants.EPSILON) {
                                    meets = true;
                                }
                            }

                            break;

                        case MajorAxis.Z:
                            if (invert) {
                                if (Mathf.Abs(x.min.z - y.max.z) <= Constants.EPSILON) {
                                    meets = true;
                                }
                            }
                            else {
                                if (Mathf.Abs(x.max.z - y.min.z) <= Constants.EPSILON) {
                                    meets = true;
                                }
                            }

                            break;

                        default:
                            break;
                    }

                    return meets;
                }

                // overlaps
                public static bool Overlaps(Bounds x, Bounds y, MajorAxis axis, bool invert = false) {
                    bool overlaps = false;

                    switch (axis) {
                        case MajorAxis.X:
                            if (invert) {
                                if ((x.min.x + Constants.EPSILON < y.max.x - Constants.EPSILON) &&
                                    (x.min.x + Constants.EPSILON > y.min.x + Constants.EPSILON)) {
                                    overlaps = true;
                                }
                            }
                            else {
                                if ((x.max.x - Constants.EPSILON > y.min.x + Constants.EPSILON) &&
                                    (x.max.x - Constants.EPSILON < y.max.x - Constants.EPSILON)) {
                                    overlaps = true;
                                }
                            }

                            break;

                        case MajorAxis.Y:
                            if (invert) {
                                if ((x.min.y + Constants.EPSILON < y.max.y - Constants.EPSILON) &&
                                    (x.min.y + Constants.EPSILON > y.min.y + Constants.EPSILON)) {
                                    overlaps = true;
                                }
                            }
                            else {
                                if ((x.max.y - Constants.EPSILON > y.min.y + Constants.EPSILON) &&
                                    (x.max.y - Constants.EPSILON < y.max.y - Constants.EPSILON)) {
                                    overlaps = true;
                                }
                            }

                            break;

                        case MajorAxis.Z:
                            if (invert) {
                                if ((x.min.z + Constants.EPSILON < y.max.z - Constants.EPSILON) &&
                                    (x.min.z + Constants.EPSILON > y.min.z + Constants.EPSILON)) {
                                    overlaps = true;
                                }
                            }
                            else {
                                if ((x.max.z - Constants.EPSILON > y.min.z + Constants.EPSILON) &&
                                    (x.max.z - Constants.EPSILON < y.max.z - Constants.EPSILON)) {
                                    overlaps = true;
                                }
                            }

                            break;

                        default:
                            break;
                    }

                    return overlaps;
                }

                // during
                public static bool During(Bounds x, Bounds y, MajorAxis axis, bool invert = false) {
                    bool during = false;

                    switch (axis) {
                        case MajorAxis.X:
                            if (invert) {
                                if ((x.min.x + Constants.EPSILON < y.min.x + Constants.EPSILON) &&
                                    (x.max.x - Constants.EPSILON > y.max.x - Constants.EPSILON)) {
                                    during = true;
                                }
                            }
                            else {
                                if ((x.min.x + Constants.EPSILON > y.min.x + Constants.EPSILON) &&
                                    (x.max.x - Constants.EPSILON < y.max.x - Constants.EPSILON)) {
                                    during = true;
                                }
                            }

                            break;

                        case MajorAxis.Y:
                            if (invert) {
                                if ((x.min.y + Constants.EPSILON < y.min.y + Constants.EPSILON) &&
                                    (x.max.y - Constants.EPSILON > y.max.y - Constants.EPSILON)) {
                                    during = true;
                                }
                            }
                            else {
                                if ((x.min.y + Constants.EPSILON > y.min.y + Constants.EPSILON) &&
                                    (x.max.y - Constants.EPSILON < y.max.y - Constants.EPSILON)) {
                                    during = true;
                                }
                            }

                            break;

                        case MajorAxis.Z:
                            if (invert) {
                                if ((x.min.z + Constants.EPSILON < y.min.z + Constants.EPSILON) &&
                                    (x.max.z - Constants.EPSILON > y.max.z - Constants.EPSILON)) {
                                    during = true;
                                }
                            }
                            else {
                                if ((x.min.z + Constants.EPSILON > y.min.z + Constants.EPSILON) &&
                                    (x.max.z - Constants.EPSILON < y.max.z - Constants.EPSILON)) {
                                    during = true;
                                }
                            }

                            break;

                        default:
                            break;
                    }

                    return during;
                }

                // starts
                public static bool Starts(Bounds x, Bounds y, MajorAxis axis, bool invert = false) {
                    bool starts = false;

                    switch (axis) {
                        case MajorAxis.X:
                            if (invert) {
                                if ((Mathf.Abs(x.min.x - y.min.x) <= Constants.EPSILON) &&
                                    (x.max.x > y.max.x + Constants.EPSILON)) {
                                    starts = true;
                                }
                            }
                            else {
                                if ((Mathf.Abs(x.min.x - y.min.x) <= Constants.EPSILON) &&
                                    (x.max.x < y.max.x - Constants.EPSILON)) {
                                    starts = true;
                                }
                            }

                            break;

                        case MajorAxis.Y:
                            if (invert) {
                                if ((Mathf.Abs(x.min.y - y.min.y) <= Constants.EPSILON) &&
                                    (x.max.y > y.max.y + Constants.EPSILON)) {
                                    starts = true;
                                }
                            }
                            else {
                                if ((Mathf.Abs(x.min.y - y.min.y) <= Constants.EPSILON) &&
                                    (x.max.y < y.max.y - Constants.EPSILON)) {
                                    starts = true;
                                }
                            }

                            break;

                        case MajorAxis.Z:
                            if (invert) {
                                if ((Mathf.Abs(x.min.z - y.min.z) <= Constants.EPSILON) &&
                                    (x.max.z > y.max.z + Constants.EPSILON)) {
                                    starts = true;
                                }
                            }
                            else {
                                if ((Mathf.Abs(x.min.z - y.min.z) <= Constants.EPSILON) &&
                                    (x.max.z < y.max.z - Constants.EPSILON)) {
                                    starts = true;
                                }
                            }

                            break;

                        default:
                            break;
                    }

                    return starts;
                }

                // finishes
                public static bool Finishes(Bounds x, Bounds y, MajorAxis axis, bool invert = false) {
                    bool finishes = false;

                    switch (axis) {
                        case MajorAxis.X:
                            if (invert) {
                                if ((Mathf.Abs(x.max.x - y.max.x) <= Constants.EPSILON) &&
                                    (x.min.x < y.min.x - Constants.EPSILON)) {
                                    finishes = true;
                                }
                            }
                            else {
                                if ((Mathf.Abs(x.max.x - y.max.x) <= Constants.EPSILON) &&
                                    (x.min.x > y.min.x + Constants.EPSILON)) {
                                    finishes = true;
                                }
                            }

                            break;

                        case MajorAxis.Y:
                            if (invert) {
                                if ((Mathf.Abs(x.max.y - y.max.y) <= Constants.EPSILON) &&
                                    (x.min.y < y.min.y - Constants.EPSILON)) {
                                    finishes = true;
                                }
                            }
                            else {
                                if ((Mathf.Abs(x.max.y - y.max.y) <= Constants.EPSILON) &&
                                    (x.min.y > y.min.y + Constants.EPSILON)) {
                                    finishes = true;
                                }
                            }

                            break;

                        case MajorAxis.Z:
                            if (invert) {
                                if ((Mathf.Abs(x.max.z - y.max.z) <= Constants.EPSILON) &&
                                    (x.min.z < y.min.z - Constants.EPSILON)) {
                                    finishes = true;
                                }
                            }
                            else {
                                if ((Mathf.Abs(x.max.z - y.max.z) <= Constants.EPSILON) &&
                                    (x.min.z > y.min.z + Constants.EPSILON)) {
                                    finishes = true;
                                }
                            }

                            break;

                        default:
                            break;
                    }

                    return finishes;
                }
            }
        }
    }
}