using UnityEngine;
using System.Collections.Generic;
using VisionViz;

namespace Agent
{
	public class VisualMemory : MonoBehaviour {

		public SyntheticVision vision;
		public Dictionary<Voxeme, GameObject> memorized;

		void Start()
		{
			memorized = new Dictionary<Voxeme, GameObject>();
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
				}
				else
				{
					clone = memorized[voxeme];

				}

				BoundBox highlighter = clone.GetComponent<BoundBox>();
				if (vision.IsVisible(voxeme.gameObject))
				{
					highlighter.lineColor = new Color(0.0f, 1.0f, 0.0f, 0.1f);
				}
				else
				{
					highlighter.lineColor = new Color(1.0f, 0.0f, 0.0f, 0.8f);
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
                    originalColor.a = 0.3f;
					Renderer rend = clone.GetComponent<Renderer>();
					SetRenderingModeToTransparent(rend.material);
                    rend.material.color = originalColor;
                    clone.AddComponent<BoundBox>();
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

		private void SetRenderingModeToTransparent(Material mat)
		{
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			mat.SetInt("_ZWrite", 0);
			mat.DisableKeyword("_ALPHATEST_ON");
			mat.DisableKeyword("_ALPHABLEND_ON");
			mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
			mat.renderQueue = 3000;
		}


	}
}