using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

using MajorAxes;
using Object = System.Object;
using Random = UnityEngine.Random;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace Global {
    	/// <summary>
    	/// Constants
    	/// </summary>
    	public static class Constants {
    		public const float EPSILON = 0.003f;
    		public static Vector3 xAxis = Vector3.right;
    		public static Vector3 yAxis = Vector3.up;
    		public static Vector3 zAxis = Vector3.forward;

    		public static Dictionary<string, Vector3> Axes = new Dictionary<string, Vector3> {
    			{"X", xAxis},
    			{"Y", yAxis},
    			{"Z", zAxis}
    		};
    	}

    	public static class Data {
    #if UNITY_EDITOR
    		public static string voxmlDataPath = Application.dataPath.Remove(Application.dataPath.LastIndexOf('/') + 1) +
    		                                     string.Format("Data/voxml");
    #elif UNITY_STANDALONE_OSX
    		public static string voxmlDataPath =
     Application.dataPath.Remove (Application.dataPath.LastIndexOf('/', Application.dataPath.LastIndexOf('/') - 1)) + string.Format ("/Data/voxml");
    #elif UNITY_STANDALONE_WIN
    		public static string voxmlDataPath =
     Application.dataPath.Remove (Application.dataPath.LastIndexOf ('/') + 1) + string.Format ("Data/voxml");
    #elif UNITY_IOS
    		public static string voxmlDataPath =
     Application.dataPath.Remove (Application.dataPath.LastIndexOf ('/') + 1) + string.Format ("/VoxML/voxml");
    #endif

    		public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs) {
    			// Get the subdirectories for the specified directory.
    			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

    			if (!dir.Exists) {
    				throw new DirectoryNotFoundException(
    					"Source directory does not exist or could not be found: "
    					+ sourceDirName);
    			}

    			DirectoryInfo[] dirs = dir.GetDirectories();
    			// If the destination directory doesn't exist, create it.
    			if (!Directory.Exists(destDirName)) {
    				Directory.CreateDirectory(destDirName);
    			}

    			// Get the files in the directory and copy them to the new location.
    			FileInfo[] files = dir.GetFiles();
    			foreach (FileInfo file in files) {
    				string temppath = Path.Combine(destDirName, file.Name);
    				file.CopyTo(temppath, true);
    			}

    			// If copying subdirectories, copy them and their contents to new location.
    			if (copySubDirs) {
    				foreach (DirectoryInfo subdir in dirs) {
    					string temppath = Path.Combine(destDirName, subdir.Name);
    					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
    				}
    			}
    		}
    	}

    	/// <summary>
    	/// Region class
    	/// </summary>
    	public class Region {
    		Vector3 _min, _max, _center;

    		public Vector3 min {
    			get { return _min; }
    			set {
    				_min = value;
    				_center = (min + max) / 2.0f;
    			}
    		}

    		public Vector3 max {
    			get { return _max; }
    			set {
    				_max = value;
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

    		public Region(Region clone) {
    			min = clone.min;
    			max = clone.max;
    		}

    		public Region(Vector3 minimum, Vector3 maximum) {
    			min = minimum;
    			max = maximum;
    		}

    		public Region(Vector3 center, float sideLength) {
    			// creates a square region centered on center with side length sideLength
    			min = center - new Vector3(sideLength / 2.0f, 0.0f, sideLength / 2.0f);
    			max = center + new Vector3(sideLength / 2.0f, 0.0f, sideLength / 2.0f);
    		}

    		public bool Contains(Vector3 point) {
    			return ((point.x >= min.x) && (point.x <= max.x) &&
    			        (point.y >= min.y) && (point.y <= max.y) &&
    			        (point.z >= min.z) && (point.z <= max.z));
    		}

    		public bool Contains(GameObject obj) {
    			return ((obj.transform.position.x >= min.x) && (obj.transform.position.x <= max.x) &&
    			        (obj.transform.position.z >= min.z) && (obj.transform.position.z <= max.z));
    		}

    		public float Area() {
    			float area = 0.0f;

    			if (min.x == max.x) {
    				area = (max.y - min.y) * (max.z - min.z);
    			}
    			else if (min.y == max.y) {
    				area = (max.x - min.x) * (max.z - min.z);
    			}
    			else if (min.z == max.z) {
    				area = (max.x - min.x) * (max.y - min.y);
    			}
    			else {
    				area = (max.x - min.x) * (max.y - min.y) * (max.z - min.z); // volume
    			}

    			return area;
    		}
    	}

    	/// <summary>
    	/// Object oriented bounds class
    	/// </summary>
    	public class ObjBounds {
    		Vector3 _center;
    		List<Vector3> _points;

    		public Vector3 Center {
    			get { return _center; }
    			set { _center = value; }
    		}

    		public List<Vector3> Points {
    			get { return _points; }
    			set { _points = value; }
    		}

    		public ObjBounds() {
    			_center = Vector3.zero;
    			_points = new List<Vector3>(new Vector3[] {
    				Vector3.zero, Vector3.zero,
    				Vector3.zero, Vector3.zero,
    				Vector3.zero, Vector3.zero,
    				Vector3.zero, Vector3.zero
    			});
    		}

    		public ObjBounds(Vector3 center) {
    			_center = center;
    			_points = new List<Vector3>(new Vector3[] {
    				Vector3.zero, Vector3.zero,
    				Vector3.zero, Vector3.zero,
    				Vector3.zero, Vector3.zero,
    				Vector3.zero, Vector3.zero
    			});
    		}

    		public ObjBounds(List<Vector3> points) {
    			_center = Vector3.zero;
    			_points = new List<Vector3>(points);
    		}

    		public ObjBounds(Vector3 center, List<Vector3> points) {
    			_center = center;
    			_points = new List<Vector3>(points);
    		}

    		public Vector3 Min(MajorAxis axis = MajorAxis.None) {
    			List<Vector3> pts = new List<Vector3>();
    			if (axis == MajorAxis.None) {
    				// default to Y
    				pts = Points.OrderBy(v => v.y).ToList();
    			}
    			else {
    				if (axis == MajorAxis.X) {
    					pts = Points.OrderBy(v => v.x).ToList();
    				}
    				else if (axis == MajorAxis.Y) {
    					pts = Points.OrderBy(v => v.y).ToList();
    				}
    				else if (axis == MajorAxis.Z) {
    					pts = Points.OrderBy(v => v.z).ToList();
    				}
    			}

    			return pts[0];
    		}

    		public Vector3 Max(MajorAxis axis = MajorAxis.None) {
    			List<Vector3> pts = new List<Vector3>();
    			if (axis == MajorAxis.None) {
    				// default to Y
    				pts = Points.OrderByDescending(v => v.y).ToList();
    			}
    			else {
    				if (axis == MajorAxis.X) {
    					pts = Points.OrderByDescending(v => v.x).ToList();
    				}
    				else if (axis == MajorAxis.Y) {
    					pts = Points.OrderByDescending(v => v.y).ToList();
    				}
    				else if (axis == MajorAxis.Z) {
    					pts = Points.OrderByDescending(v => v.z).ToList();
    				}
    			}

    			return pts[0];
    		}

    		public bool Contains(Vector3 point) {
    			bool contains = true;

    			Vector3 closestPoint = Points.OrderBy(p => (p - point).magnitude).ToList()[0];

    			List<Vector3> colinearPoints = Points.Where(p => p != closestPoint)
    				.OrderBy(p => (p - closestPoint).magnitude).Take(3).ToList();

    			foreach (Vector3 pt in colinearPoints) {
    				contains &= (Vector3.Dot((point - closestPoint).normalized, (pt - closestPoint).normalized) >= 0.0f);
    			}

    			return contains;
    		}
                
            public bool BoundsEqual(ObjBounds other) {
                bool boundsEqual = true;

                if (!Helper.CloseEnough(this.Center, other.Center)) {
                    boundsEqual = false;
                }

                for (int i = 0; i < this.Points.Count; i++) {
                    if (!Helper.CloseEnough(this.Points[i], other.Points[i])) {
                        boundsEqual = false;
                    }
                }

                return boundsEqual;
            }
        }

    	/// <summary>
    	/// List comparer class
    	/// </summary>
    	public class ListComparer<T> : IEqualityComparer<List<T>> {
    		public bool Equals(List<T> x, List<T> y) {
    			return x.SequenceEqual(y);
    		}

    		public int GetHashCode(List<T> obj) {
    			int hashcode = 0;
    			foreach (T t in obj) {
    				hashcode ^= t.GetHashCode();
    			}

    			return hashcode;
    		}
    	}

    	/// <summary>
    	/// Pair class
    	/// </summary>
    	public class Pair<T1, T2> : IEquatable<Object> {
    		public T1 Item1 { get; set; }

    		public T2 Item2 { get; set; }

    		public Pair(T1 Item1, T2 Item2) {
    			this.Item1 = Item1;
    			this.Item2 = Item2;
    		}

    		public override bool Equals(object obj) {
    			if (obj == null || (obj as Pair<T1, T2>) == null) //if the object is null or the cast fails
    				return false;
    			else {
    				Pair<T1, T2> tuple = (Pair<T1, T2>) obj;
    				return Item1.Equals(tuple.Item1) && Item2.Equals(tuple.Item2);
    			}
    		}

    		public Pair<T2, T1> Reverse() {
    			Pair<T2, T1> tuple = new Pair<T2, T1>(Item2, Item1);
    			return tuple;
    		}

    		public override int GetHashCode() {
    			return Item1.GetHashCode() ^ Item2.GetHashCode();
    		}

    		public static bool operator ==(Pair<T1, T2> tuple1, Pair<T1, T2> tuple2) {
    			return tuple1.Equals(tuple2);
    		}

    		public static bool operator !=(Pair<T1, T2> tuple1, Pair<T1, T2> tuple2) {
    			return !tuple1.Equals(tuple2);
    		}
    	}

    	/// <summary>
    	/// Triple class
    	/// </summary>
    	public class Triple<T1, T2, T3> : IEquatable<Object> {
    		public T1 Item1 { get; set; }

    		public T2 Item2 { get; set; }

    		public T3 Item3 { get; set; }

    		public Triple(T1 Item1, T2 Item2, T3 Item3) {
    			this.Item1 = Item1;
    			this.Item2 = Item2;
    			this.Item3 = Item3;
    		}

    		public override bool Equals(object obj) {
    			if (obj == null || (obj as Triple<T1, T2, T3>) == null) //if the object is null or the cast fails
    				return false;
    			else {
    				Triple<T1, T2, T3> tuple = (Triple<T1, T2, T3>) obj;
    				return Item1.Equals(tuple.Item1) && Item2.Equals(tuple.Item2) && Item3.Equals(tuple.Item3);
    			}
    		}

    		public override int GetHashCode() {
    			return Item1.GetHashCode() ^ Item2.GetHashCode() ^ Item3.GetHashCode();
    		}

    		public static bool operator ==(Triple<T1, T2, T3> tuple1, Triple<T1, T2, T3> tuple2) {
    			return tuple1.Equals(tuple2);
    		}

    		public static bool operator !=(Triple<T1, T2, T3> tuple1, Triple<T1, T2, T3> tuple2) {
    			return !tuple1.Equals(tuple2);
    		}
        }
    }
}