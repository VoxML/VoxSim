using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Network;
using VoxSimPlatform.SpatialReasoning;

namespace VoxSimPlatform {
    namespace Logging {
        public class RelationExtractor : MonoBehaviour {
        	RelationTracker relationTracker;
        	EventManager em;
        	CommunicationsBridge commBridge;

        	// Use this for initialization
        	void Start() {
        		relationTracker = gameObject.GetComponent<RelationTracker>();
        		em = gameObject.GetComponent<EventManager>();
        		commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();

        		em.QueueEmpty += QueueEmpty;
        	}

        	// Update is called once per frame
        	void Update() {
        	}

        	void QueueEmpty(object sender, EventArgs e) {
        		if (commBridge != null) {
                    CommanderSocket commander = (CommanderSocket)commBridge.FindSocketConnectionByLabel("Commander");
        			if (commander != null) {
        				StringBuilder sb = new StringBuilder();
        				foreach (string rel in relationTracker.relStrings) {
        					sb = sb.AppendFormat(string.Format("{0}\n", rel));
        				}

        				List<GameObject> objects = new List<GameObject>();
        				foreach (DictionaryEntry dictEntry in relationTracker.relations) {
        					foreach (GameObject go in dictEntry.Key as List<GameObject>) {
        						if (!objects.Contains(go)) {
        							objects.Add(go);
        						}
        					}
        				}

        				foreach (GameObject go in objects) {
        					sb = sb.AppendFormat(string.Format("{0} {1}\n", go.name,
        						Helper.VectorToParsable(go.transform.eulerAngles)));
        				}

                        commander.Write(sb.ToString());
        			}
        		}
        	}
        }
    }
}