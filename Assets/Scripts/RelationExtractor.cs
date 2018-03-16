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
				commBridge.CommanderClient.Write (sb.ToString());
			}
		}
	}
}
