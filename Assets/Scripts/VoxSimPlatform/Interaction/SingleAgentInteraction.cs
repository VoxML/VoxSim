using System;
using UnityEngine;

namespace VoxSimPlatform {
    namespace Interaction {
    	public class CharacterLogicEventArgs : EventArgs {
    		public string InputSymbolName { get; set; }
    		public object InputSymbolContent { get; set; }

    		public CharacterLogicEventArgs(string inputSymbolName, object inputSymbolContent) {
    			InputSymbolName = inputSymbolName;
    			InputSymbolContent = inputSymbolContent;
    		}
    	}

    	public class SingleAgentInteraction : MonoBehaviour {
    		private bool useTeaching;

    		public bool UseTeaching {
    			get { return useTeaching; }
    			set { useTeaching = value; }
    		}

    		public event EventHandler CharacterLogicInput;

    		public void OnCharacterLogicInput(object sender, EventArgs e) {
    			if (CharacterLogicInput != null) {
    				CharacterLogicInput(this, e);
    			}
    		}

    		void Start() {
    		}

    		void Update() {
    		}
    	}
    }
}