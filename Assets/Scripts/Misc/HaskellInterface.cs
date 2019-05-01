using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class HaskellInterface : MonoBehaviour {
	[DllImport("HaskellInterface", CallingConvention = CallingConvention.Cdecl)]
	private static extern void hs_init(IntPtr argc, IntPtr argv);

	[DllImport("HaskellInterface", CallingConvention = CallingConvention.Cdecl)]
	private static extern void hs_exit();

	[DllImport("HaskellInterface", CallingConvention = CallingConvention.Cdecl)]
	private static extern string hs_test(string str);

	void Start() {
		Debug.Log("Initializing runtime...");
		hs_init(IntPtr.Zero, IntPtr.Zero);

		try {
			Debug.Log("Calling to Haskell...");
			string result = hs_test("C#");
			Debug.Log(string.Format("Got result: {0}", result));
		}
		catch (Exception e) {
			Debug.Log(e.Message);
		}
		finally {
		}
	}

	void Update() {
	}

	void OnDestroy() {
//		Debug.Log("Exiting runtime...");
//		hs_exit();
	}

	void OnApplicationQuit() {
		//OnDestroy();
	}
}