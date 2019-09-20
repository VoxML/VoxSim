using UnityEngine;
using System;

using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;

// TODO: to be deprecated by end of refactor

namespace VoxSimPlatform {
    namespace Agent {
    	public class VoiceController : MonoBehaviour {
    		private static readonly String MAC_F = "Samantha";
    		private static readonly String MAC_M = "Fred";
    		private static readonly String WIN_F = "Microsoft Zira Desktop";
    		private static readonly String WIN_M = "Microsoft David Desktop";
    		private static readonly String LANG = "en-US";
    		private static readonly String DUMMY = "not-important";

    		// TODO 6/6/2017-23:20 these fields can be inherited from a proper "agent" game object
    		// , which is not implemented yet.
    		// Currently these enums are in "agent" namespace
    		public Role role;
    		public Gender Gender;

    		private LiveSpeaker _speaker;
    		private string _outputstring;
    		private Voice v;

    		void Start() {
    			_speaker = gameObject.AddComponent<LiveSpeaker>();
    			string vName = null;
    			Crosstales.RTVoice.Model.Enum.Gender vGender = Crosstales.RTVoice.Model.Enum.Gender.UNKNOWN;
    			if (Gender == Gender.Female) {
    				vGender = Crosstales.RTVoice.Model.Enum.Gender.FEMALE;
    				if (SystemInfo.operatingSystemFamily ==
    				    OperatingSystemFamily.Windows) {
    					vName = WIN_F;
    				}
    				else if (SystemInfo.operatingSystemFamily ==
    				         OperatingSystemFamily.MacOSX) {
    					vName = MAC_F;
    				}
    			}
    			else if (Gender == Gender.Male) {
    				vGender = Crosstales.RTVoice.Model.Enum.Gender.MALE;
    				if (SystemInfo.operatingSystemFamily ==
    				    OperatingSystemFamily.Windows) {
    					vName = WIN_M;
    				}
    				else if (SystemInfo.operatingSystemFamily ==
    				         OperatingSystemFamily.MacOSX) {
    					vName = MAC_M;
    				}
    			}

    			if (vName != null) {
    				v = new Voice(vName, "", vGender, DUMMY, LANG);
    			}

    			// TODO 6/6/2017-23:19 this is jsut for test, delete later
    			//_speaker.Speak(new Wrapper(text: "Ta-da, Mic testing", voice: v));
    		}

    		public void Speak(string text) {
    			_outputstring = text;
    			if (v != null && text != null && text.Length > 0) {
    				_speaker.Silence();
    				_speaker.Speak(new Wrapper(text, v));
    			}
    		}

    		public bool IsAnyoneSpeaking() {
    			return GameObject.Find("RTVoice").GetComponent<AudioSource>() != null;
    		}

    		void OnGUI() {
    		}
    	}
    }
}