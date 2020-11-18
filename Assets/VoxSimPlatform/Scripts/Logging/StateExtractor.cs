using UnityEngine;
using System;
using System.Linq;
using System.Text;
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
        			if (!objectSelector.disabledObjects.Contains(GlobalHelper.GetMostImmediateParentVoxeme(voxeme.gameObject))) {
        				objList.Add(GlobalHelper.GetMostImmediateParentVoxeme(voxeme.gameObject));
        			}
        		}

        		if (commBridge != null) {
                    Examples.SocketConnections.RelationExtractorSocket commander =
                        (Examples.SocketConnections.RelationExtractorSocket)commBridge.FindSocketConnectionByLabel("Extractor");

                    if (commander != null) {
                        byte[] bytes = Encoding.ASCII.GetBytes("").ToArray<byte>();
                        commander.Write(bytes);
        			}
        		}
        	}
        }
    }
}