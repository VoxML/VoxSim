using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
#if !UNITY_WEBGL
using VoxSimPlatform.Network;
# endif
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace Logging {
        public class StateExtractor : MonoBehaviour {
        	EventManager em;
#if !UNITY_WEBGL
			CommunicationsBridge commBridge;
#else
            NLU.INLParser commBridge;
#endif
            ObjectSelector objectSelector;

			// Use this for initialization
			void Start() {
        		em = gameObject.GetComponent<EventManager>();
#if !UNITY_WEBGL
				commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
#else
                commBridge = GameObject.Find("SimpleParser").GetComponent<NLU.SimpleParser>();
#endif        		
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

#if !UNITY_WEBGL
				if (commBridge != null)
				{
					Examples.SocketConnections.RelationExtractorSocket commander =
						(Examples.SocketConnections.RelationExtractorSocket)commBridge.FindSocketConnectionByLabel("Extractor");

					if (commander != null)
					{
						byte[] bytes = Encoding.ASCII.GetBytes("").ToArray<byte>();
						commander.Write(bytes);
					}
				} 
#endif
			}
        }
    }
}