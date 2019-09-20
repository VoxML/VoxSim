using UnityEngine;
using System;
using System.Collections.Generic;

using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Network;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace Logging {
        public class StateExtractor : MonoBehaviour {
        	EventManager em;
        	CommunicationsBridge commBridge;
        	ObjectSelector objectSelector;

        	// Use this for initialization
        	void Start() {
        		em = gameObject.GetComponent<EventManager>();
        		commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
        		objectSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();

        		em.QueueEmpty += QueueEmpty;
        	}

        	// Update is called once per frame
        	void Update() {
        	}

        	void QueueEmpty(object sender, EventArgs e) {
        		List<GameObject> objList = new List<GameObject>();

        		foreach (Voxeme voxeme in objectSelector.allVoxemes) {
        			if (!objectSelector.disabledObjects.Contains(Helper.GetMostImmediateParentVoxeme(voxeme.gameObject))) {
        				objList.Add(Helper.GetMostImmediateParentVoxeme(voxeme.gameObject));
        			}
        		}

        		if (commBridge != null) {
                    CommanderSocket commander = (CommanderSocket)commBridge.FindSocketConnectionByLabel("Commander");

                    if (commander != null) {
                        commander.Write("");
        			}
        		}
        	}
        }
    }
}