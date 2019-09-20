using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using Random = UnityEngine.Random;
using VoxSimPlatform.Core;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace Global {
        /// <summary>
        /// RandomHelper class
        /// </summary>
        public static class RandomHelper {
            public enum RangeFlags {
                MinInclusive = 1,
                MaxInclusive = (1 << 1)
            }

            public static int RandomSign() {
                return (Random.Range(0, 2) * 2) - 1;
            }

            public static Vector3 RandomAxis() {
                System.Random random = new System.Random();
                return Constants.Axes[Constants.Axes.Keys.ToList()[random.Next(0, 3)]];
            }

            public static int RandomInt(int min, int max, int flags = (int) RangeFlags.MinInclusive) {
                int rangeMin = min;
                int rangeMax = max;

                if ((flags & (int) RangeFlags.MinInclusive) == 0) {
                    rangeMin = min + 1;
                }

                if (((flags & (int) RangeFlags.MaxInclusive) >> 1) == 1) {
                    rangeMax = max + 1;
                }

                return Random.Range(rangeMin, rangeMax);
            }

            public static float RandomFloat(float min, float max, int flags = (int) RangeFlags.MinInclusive) {
                float rangeMin = min;
                float rangeMax = max;

                if ((flags & (int) RangeFlags.MinInclusive) == 0) {
                    rangeMin = min + Constants.EPSILON;
                }

                if (((flags & (int) RangeFlags.MaxInclusive) >> 1) == 1) {
                    rangeMax = max + Constants.EPSILON;
                }

                return Random.Range(min, max);
            }

            public static GameObject RandomVoxeme() {
                List<Voxeme> allVoxemes = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>().allVoxemes.ToList();

                Voxeme voxeme = allVoxemes[RandomInt(0, allVoxemes.Count, (int) RangeFlags.MinInclusive)];
                while (Helper.GetMostImmediateParentVoxeme(voxeme.gameObject).gameObject.transform.parent != null) {
                    voxeme = allVoxemes[RandomInt(0, allVoxemes.Count, (int) RangeFlags.MinInclusive)];
                }

                return voxeme.gameObject;
            }

            public static GameObject RandomVoxeme(List<GameObject> exclude) {
                List<Voxeme> allVoxemes = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>().allVoxemes.ToList();

                Voxeme voxeme = allVoxemes[RandomInt(0, allVoxemes.Count, (int) RangeFlags.MinInclusive)];
                while ((Helper.GetMostImmediateParentVoxeme(voxeme.gameObject).gameObject.transform.parent != null) ||
                       (exclude.Contains(voxeme.gameObject))) {
                    voxeme = allVoxemes[RandomInt(0, allVoxemes.Count, (int) RangeFlags.MinInclusive)];
                }

                return voxeme.gameObject;
            }

            public static GameObject RandomVoxeme(List<GameObject> fromList, List<GameObject> exclude) {
                Voxeme voxeme = fromList[RandomInt(0, fromList.Count, (int) RangeFlags.MinInclusive)]
                    .GetComponent<Voxeme>();

                while ((Helper.GetMostImmediateParentVoxeme(voxeme.gameObject).gameObject.transform.parent != null) ||
                       (exclude.Contains(voxeme.gameObject))) {
                    voxeme = fromList[RandomInt(0, fromList.Count, (int) RangeFlags.MinInclusive)].GetComponent<Voxeme>();
                }

                return voxeme.gameObject;
            }
        }
    }
}