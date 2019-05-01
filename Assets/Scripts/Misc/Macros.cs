using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;

public class Macros : MonoBehaviour {
	public Dictionary<String, String> commandMacros = new Dictionary<String, String>();

	public List<GameObject> lids = new List<GameObject>();

	// Use this for initialization
	void Start() {
	}

	public void PopulateMacros() {
		/* agh i hate this hack so much */
		// needs to be done in CLOSE predicate
		// how do you reason satisfaction conditions on a predicate you sub in?
		ObjectSelector objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
		GameObject mug = GameObject.Find("mug");
		GameObject table = GameObject.Find("square_table");

		if (table != null) {
			if (mug != null) {
				GameObject mugInterior = mug.transform.Find("mug*/cup/interior").gameObject;

				if (mugInterior != null) {
					foreach (Voxeme voxeme in objSelector.allVoxemes) {
						if (voxeme.gameObject.activeInHierarchy) {
							if ((voxeme.gameObject != mug) && (voxeme.gameObject != table)) {
								if ((Helper.GetObjectWorldSize(voxeme.gameObject).size.x >=
								     Helper.GetObjectWorldSize(mugInterior).size.x) &&
								    (Helper.GetObjectWorldSize(voxeme.gameObject).size.z >=
								     Helper.GetObjectWorldSize(mugInterior).size.z)) {
									lids.Add(voxeme.gameObject);
									lids = lids.OrderBy(o => (Helper.GetObjectWorldSize(o).size.x +
									                          Helper.GetObjectWorldSize(o).size.z) *
									                         Helper.GetObjectWorldSize(o).size.y).ToList();
								}
							}
						}
					}

					if (lids.Count > 0) {
						//commandMacros.Add ("close(mug)", string.Format ("put({0},on(mug))", lids [0].name));
					}
					else {
						//commandMacros.Add ("close(mug)", "flip(mug)");
					}
				}
			}
		}

		commandMacros.Add("flip(cups)", "flip(cup1);flip(cup2);flip(cup3)");
		commandMacros.Add("flip(the(cups))", "flip(cup1);flip(cup2);flip(cup3)");
		commandMacros.Add("switch(two(cups))", "switch(two(cup))");
		commandMacros.Add("shuffle(cups)",
			"switch(two(cup));switch(two(cup));switch(two(cup));switch(two(cup));switch(two(cup))");
		commandMacros.Add("shuffle(the(cups))",
			"switch(two(cup));switch(two(cup));switch(two(cup));switch(two(cup));switch(two(cup))");
		commandMacros.Add("stack(blocks)",
			"put(brown(block),on(red(block)));put(black(block),on(brown(block)));bind(red(block),black(block),brown(block),as(\"stack\"))");
		commandMacros.Add("stack(the(blocks))",
			"put(brown(block),on(red(block)));put(black(block),on(brown(block)));bind(red(block),black(block),brown(block),as(\"stack\"))");
		//commandMacros.Add ("stack(apple,plate,mug)", "put(apple,on(plate));put(plate,on(mug))");
		commandMacros.Add("build(staircase)",
			"put(green(block),left(black(block)));put(red(block),right(black(block)));put(yellow(block),on(red(block)));put(blue(block),on(black(block)));put(brown(block),on(yellow(block)))");
		commandMacros.Add("build(a(staircase))",
			"put(green(block),left(black(block)));put(red(block),right(black(block)));put(yellow(block),on(red(block)));put(blue(block),on(black(block)));put(brown(block),on(yellow(block)))");
		commandMacros.Add("build(pyramid)",
			"put(block1,left(block3));put(block2,right(block3));put(block4,behind(block3));put(block5,in_front(block3));put(block6,on(block3))");
		commandMacros.Add("build(a(pyramid))",
			"put(block1,left(block3));put(block2,right(block3));put(block4,behind(block3));put(block5,in_front(block3));put(block6,on(block3))");
	}

	public void ClearMacros() {
		lids.Clear();
		commandMacros.Clear();
	}

	// Update is called once per frame
	void Update() {
	}
}