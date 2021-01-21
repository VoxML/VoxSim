#if !UNITY_WEBGL

using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Network;
using VoxSimPlatform.SpatialReasoning;

namespace VoxSimPlatform
{
	namespace Logging
	{
		public class RelationExtractor : MonoBehaviour
		{
#if UNITY_EDITOR
			// Todo: How many of these fields are actually in active use?
			[CustomEditor(typeof(RelationExtractor))]
			public class DebugPreview : Editor
			{
				public override void OnInspectorGUI()
				{
					var bold = new GUIStyle();
					bold.fontStyle = FontStyle.Bold;

					GUILayout.BeginHorizontal();
					if (GUILayout.Button("Write Relations"))
					{
						((RelationExtractor)target).WriteRelations(this, null);
					}
					GUILayout.EndHorizontal();
				}
			}
#endif
			RelationTracker relationTracker;
			EventManager em;
			CommunicationsBridge commBridge;
			Examples.SocketConnections.RelationExtractorSocket extractor;

			// Use this for initialization
			void Start()
			{
				relationTracker = gameObject.GetComponent<RelationTracker>();
				//em = gameObject.GetComponent<EventManager>();
				commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();

				//em.QueueEmpty += WriteRelations;
			}

			// Update is called once per frame
			void Update()
			{
			}

			public void WriteRelations(object sender, EventArgs e)
			{
				if (commBridge != null)
				{
					Debug.Log(commBridge.FindSocketConnectionByLabel("Extractor"));
					extractor = (Examples.SocketConnections.RelationExtractorSocket)commBridge.FindSocketConnectionByLabel("Extractor");
					Debug.Log(extractor);
					if (extractor != null)
					{
						StringBuilder sb = new StringBuilder();
						foreach (string rel in relationTracker.relStrings)
						{
							sb = sb.AppendFormat(string.Format("{0}\n", rel));
						}

						List<GameObject> objects = new List<GameObject>();
						foreach (DictionaryEntry dictEntry in relationTracker.relations)
						{
							foreach (GameObject go in dictEntry.Key as List<GameObject>)
							{
								if (!objects.Contains(go))
								{
									objects.Add(go);
								}
							}
						}

						foreach (GameObject go in objects)
						{
							sb = sb.AppendFormat(string.Format("{0} {1}\n", go.name,
								GlobalHelper.VectorToParsable(go.transform.eulerAngles)));
						}

						Debug.Log(string.Format("Writing data to {0}:{1}: {2}", extractor.Address, extractor.Port,
							sb.ToString()));
						byte[] bytes = Encoding.ASCII.GetBytes(sb.ToString()).ToArray<byte>();
						extractor.Write(bytes);
					}
				}
			}
		}
	}
} 
#endif