using System;
using UnityEngine;
using System.Collections.Generic;
using System.Timers;
using UnityEngine.UI;
using VisionViz;

namespace Agent
{
	public class VisualMemory : MonoBehaviour
	{

		public SyntheticVision _vision;
		private JointGestureDemo _world;
		public Dictionary<Voxeme, GameObject> _memorized;
		private InteractionPrefsModalWindow _interactionPrefs;
		private ObjectSelector _objectSelector;

		public GameObject MemoryCanvas;

		private bool showMemory;
		public bool ShowMemory
		{
			get { return showMemory; }
			set { showMemory = value; }
		}

		private Timer _reactionTimer;
		private const float ReactionDelayInterval = 1000;
		private bool _surprise;

		private VisionEventArgs _surpriseArgs;

		public bool _perceivingInitialConfiguration;

		void Start()
		{
			_world = GameObject.Find("JointGestureDemo").GetComponent<JointGestureDemo>();
			_interactionPrefs = _world.GetComponent<InteractionPrefsModalWindow> ();
			_objectSelector = FindObjectOfType<ObjectSelector>();


			// set up a time for "surprise" reaction
			_reactionTimer = new Timer();
			_reactionTimer.Elapsed += Surprise;
			_reactionTimer.Interval = ReactionDelayInterval;
			// but don't start it yet
			_reactionTimer.Enabled = false;

			_perceivingInitialConfiguration = true;
			_memorized = new Dictionary<Voxeme, GameObject>();
			gameObject.GetComponent<Camera>().targetTexture = (RenderTexture) MemoryCanvas.GetComponentInChildren<RawImage>().texture;
		}

		// updating memory happens in LateUpdate after all visual perception happened in Update (See SyntheticVision)
		void Update()
		{
			ShowMemory = _interactionPrefs.showVisualMemory;
			if (!ShowMemory)
			{
				MemoryCanvas.SetActive(false);
			}
			else {
				MemoryCanvas.SetActive(true);
			}
			foreach (GameObject block in _world.blocks)
			{
				Voxeme voxeme = block.GetComponent<Voxeme>();
//				Debug.Log(voxeme + " is visible?");
				GameObject clone = null;
				if (_vision.IsVisible(voxeme))
				{
//					Debug.Log(voxeme + " is");
					if (!_memorized.ContainsKey(voxeme))
					{
						clone = GetVisualClone(block.gameObject);
						_memorized.Add(voxeme, clone);

						if (!_perceivingInitialConfiguration)
						{
							// don't do this when you initially populate knownObjects
							// but otherwise
							// surprise!
							// todo _surpriseArgs can be plural
							_surpriseArgs = new VisionEventArgs(voxeme, InconsistencyType.Present);
							StartCoroutine(clone.GetComponent<BoundBox>().Flash(10));
							Debug.Log(string.Format("{0} Surprise!", voxeme));
							_reactionTimer.Enabled = true;
						}
					}
					else
					{
						clone = _memorized[voxeme];

					}
				}
				// block is not visible
				else
				{
//					Debug.Log(voxeme + " is not ");
					// but I know about it
					if (_memorized.ContainsKey(voxeme))
					{
						// but can't see where it should be
						if (!_vision.IsVisible(_memorized[voxeme]))
						{
							clone = _memorized[voxeme];
						}
						// or I see it's not where it supposed to be!
						else
						{
							clone = _memorized[voxeme];
							// surprise!
							_surpriseArgs = new VisionEventArgs(voxeme, InconsistencyType.Missing);
							StartCoroutine(clone.GetComponent<BoundBox>().Flash(10));
							Destroy(_memorized[voxeme], 3);
							_memorized.Remove(voxeme);
							Debug.Log(string.Format("{0} Surprise!", voxeme));
							_reactionTimer.Enabled = true;
						}
					}
				}

				if (clone == null) continue;

				if (_objectSelector.disabledObjects.Contains(voxeme.gameObject))
				{
					clone.transform.parent = null;
					clone.SetActive(true);
				}
				else if (clone.transform.parent != null)
				{
					clone.transform.SetParent(voxeme.gameObject.transform);
				}

				BoundBox highlighter = clone.GetComponent<BoundBox>();
				if (_vision.IsVisible(voxeme))
				{
					highlighter.lineColor = new Color(0.0f, 1.0f, 0.0f, 0.2f);
				}
				else
				{
					highlighter.lineColor = new Color(1.0f, 0.0f, 0.0f, 0.8f);
				}
			}
			if (_memorized.Count > 0 && _perceivingInitialConfiguration) {
				// effectively this goes false after the first frame
				_perceivingInitialConfiguration = false;
			}

			if (_surprise) {
				NewInformation (_surpriseArgs);
				_surprise = false;
			}
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

		private GameObject GetVisualClone(GameObject obj)
		{

			GameObject clone = null;
			for (int i=0; i < obj.transform.childCount; i++)
			{
				Transform t = obj.transform.GetChild(i);
				if (t.name == obj.name + "*")
				{
					clone = Instantiate(t.gameObject);
					// obj = original blockX with `voxeme` attached
					// t = blockX* with physics
					clone.transform.SetParent(t.gameObject.transform);
					clone.transform.localScale = obj.transform.localScale;
					clone.transform.position = t.transform.position;
					Color originalColor = t.gameObject.GetComponent<Renderer>().material.color;
					originalColor.a = 0.3f;
					Renderer rend = clone.GetComponent<Renderer>();
					SetRenderingModeToTransparent(rend.material);
					rend.material.color = originalColor;
					clone.AddComponent<BoundBox>();
					Destroy(clone.GetComponent<Collider>());
					Destroy(clone.GetComponent<Rigidbody>());
					clone.layer = 11;
					break;
				}
			}
			return clone;
		}

		public bool IsKnown(Voxeme v)
		{
			return _memorized.ContainsKey(v);
		}

		public void Surprise(object source, ElapsedEventArgs ignored) {
			_reactionTimer.Interval = ReactionDelayInterval;
			_reactionTimer.Enabled = false;
			_surprise = true; // this will trigger NewInformation methods in the frame after the _reactionDelayInterval
		}

		public void NewInformation(VisionEventArgs e) {
			if (e.Inconsistency == InconsistencyType.Missing)
			{
				KnownUnseen(e.Voxeme);
			}
			else
			{
				UnknownSeen(e.Voxeme);
			}
		}

		private void KnownUnseen(Voxeme voxeme)
		{
			string color = voxeme.voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
			OutputHelper.PrintOutput (Role.Affector, string.Format ("Holy cow!  What happened to the {0} block?", color));
		}

		private void UnknownSeen(Voxeme voxeme)
		{
			string color = voxeme.voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
			OutputHelper.PrintOutput (Role.Affector, string.Format ("I didn't know that {0} block was there!", color));
		}
	}

	public enum InconsistencyType
	{
		Missing,
		Present
	}

	public class VisionEventArgs : EventArgs {

		public Voxeme Voxeme { get; set; }
		public InconsistencyType Inconsistency { get; set; }

		public VisionEventArgs(Voxeme voxeme, InconsistencyType inconsistency)
		{
			Voxeme = voxeme;
			Inconsistency = inconsistency;
		}
	}


}