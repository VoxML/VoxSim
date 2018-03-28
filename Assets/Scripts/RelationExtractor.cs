using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Global;
using Vox;

public class RelationExtractor : MonoBehaviour {

	RelationTracker relationTracker;
	EventManager em;
	PluginImport commBridge;

	// Use this for initialization
	void Start () {
		relationTracker = gameObject.GetComponent<RelationTracker>();
		em = gameObject.GetComponent<EventManager>();
		commBridge = GameObject.Find ("CommunicationsBridge").GetComponent<PluginImport> ();

		em.QueueEmpty += QueueEmpty;
	}
	
	// Update is called once per frame
	void Update () {
	}

	void QueueEmpty(object sender, EventArgs e) {
		if (commBridge != null) {
			if (commBridge.CommanderClient != null) {
				StringBuilder sb = new StringBuilder ();
				foreach (string rel in relationTracker.relStrings) {
					sb = sb.AppendFormat (string.Format ("{0}\n", rel));
				}

				List<GameObject> objects = new List<GameObject> ();
				foreach (DictionaryEntry dictEntry in relationTracker.relations) {
					foreach (GameObject go in dictEntry.Key as List<GameObject>) {
						if (!objects.Contains (go)) {
							objects.Add (go);
						}
					}
				}

				foreach (GameObject go in objects) {
					sb = sb.AppendFormat (string.Format ("{0} {1}\n", go.name, Helper.VectorToParsable(go.transform.eulerAngles)));
				}
				commBridge.CommanderClient.Write (sb.ToString());
			}
		}
	}
}
