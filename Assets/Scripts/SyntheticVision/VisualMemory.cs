using UnityEngine;
using System.Collections.Generic;
using SyntheticVision;

namespace Agent
{
	public class VisualMemory : MonoBehaviour {

		public List<GameObject> memory;
		public SyntheticVision vision;
		public Dictionary<Voxeme, GameObject> memorized;

		void Start()
		{
			memorized = new Dictionary<Voxeme, GameObject>();
			vision = GameObject.Find("DianaVision").GetComponent<SyntheticVision>();
		}

		void Update()
		{
			foreach (GameObject block in GameObject.Find("JointGestureDemo").GetComponent<JointGestureDemo>().blocks)
			{
				Voxeme voxeme = block.GetComponent<Voxeme>();
				GameObject clone;
				if (!memorized.ContainsKey(voxeme))
				{
					clone = GetVisualClone(block);

					memorized.Add(voxeme, clone);
					memory.Add(clone);
				}
				else
				{
					clone = memorized[voxeme];

				}

				BoundBox highlighter = clone.GetComponent<BoundBox>();
				if (vision.IsVisible(voxeme.gameObject))
				{
					highlighter.lineColor = Color.green;
				}
				else
				{
					highlighter.lineColor = Color.red;
				}
            }
		}

		private GameObject GetVisualClone(GameObject obj)
		{

			GameObject clone = null;
			for (int i=0; i < obj.transform.childCount; i++)
			{
				Transform t = obj.transform.GetChild(i);
				if (t.name == obj.name + "*")
				{
					clone = Instantiate(t.gameObject);
					clone.transform.SetParent(t.gameObject.transform);
					clone.transform.position = t.transform.position;
                    Color originalColor = t.gameObject.GetComponent<Renderer>().material.color;
                    originalColor.a = 0.1f;
					Renderer rend = clone.GetComponent<Renderer>();
                    rend.material.color = originalColor;
                    BoundBox boxer = clone.AddComponent<BoundBox>();
					boxer.setupOnAwake = true;
                    clone.GetComponent<Collider>().enabled = false;
					clone.GetComponent<Rigidbody>().useGravity = false;
					clone.GetComponent<Rigidbody>().useGravity = false;
                    clone.layer = 11;
					break;
				}
			}
			return clone;
		}

		public bool IsKnown(GameObject obj) {
			return memorized.ContainsKey(obj.GetComponent<Voxeme>());
		}


	}
}