using UnityEngine;
using System;

using VoxSimPlatform.UI;

// TODO: to be deprecated by end of refactor

namespace VoxSimPlatform {
    namespace Agent {
        public class OutputController : FontManager {
        	public Role role;

        	public enum Alignment {
        		Left,
        		Center,
        		Right
        	}

        	public Alignment alignment;

        	public enum Placement {
        		Top,
        		Bottom
        	}

        	public Placement placement;

        	public int fontSize = 12;

        	public String outputLabel;
        	public String outputString;
        	public int outputWidth;
        	public int outputMaxWidth;
        	public int outputHeight;
        	public int outputMargin;
        	public Rect outputRect = new Rect();

        	public bool textField = true;

        	GUIStyle labelStyle;
        	GUIStyle textFieldStyle;

        	float fontSizeModifier;

        	void Start() {
        		fontSizeModifier = (float) ((float) fontSize / (float) defaultFontSize);

        		outputWidth = (Convert.ToInt32(Screen.width - outputMargin) > outputMaxWidth)
        			? outputMaxWidth
        			: Convert.ToInt32(Screen.width - outputMargin);
        		outputHeight = Convert.ToInt32(20.0f * (float) fontSizeModifier);

        		if (alignment == Alignment.Left) {
        			if (placement == Placement.Top) {
        				outputRect = new Rect(5, 5, outputWidth, outputHeight);
        			}
        			else if (placement == Placement.Bottom) {
        				outputRect = new Rect(5, Screen.height - outputHeight - 5, outputWidth, outputHeight);
        			}
        		}
        		else if (alignment == Alignment.Center) {
        			if (placement == Placement.Top) {
        				outputRect = new Rect((int) ((Screen.width / 2) - (outputWidth / 2)), 5, outputWidth, outputHeight);
        			}
        			else if (placement == Placement.Bottom) {
        				outputRect = new Rect((int) ((Screen.width / 2) - (outputWidth / 2)),
        					Screen.height - outputHeight - 5, outputWidth, outputHeight);
        			}
        		}
        		else if (alignment == Alignment.Right) {
        			if (placement == Placement.Top) {
        				outputRect = new Rect(Screen.width - (5 + outputWidth), 5, outputWidth, outputHeight);
        			}
        			else if (placement == Placement.Bottom) {
        				outputRect = new Rect(Screen.width - (5 + outputWidth),
        					Screen.height - outputHeight - 5, outputWidth, outputHeight);
        			}
        		}
        	}

        	void Update() {
        	}

        	void OnGUI() {
        		if (!textField) {
        			return;
        		}

        		labelStyle = new GUIStyle("Label");
        		textFieldStyle = new GUIStyle("TextField");

        		labelStyle.fontSize = fontSize;
        		textFieldStyle.fontSize = fontSize;

        		GUILayout.BeginArea(outputRect);
        		GUILayout.BeginHorizontal();

        		if (outputLabel != "") {
        			GUILayout.Label(outputLabel + ":", labelStyle);
        			outputString = GUILayout.TextArea(outputString, textFieldStyle,
        				GUILayout.Width(outputWidth - (65 * fontSizeModifier)), GUILayout.ExpandHeight(false));
        		}
        		else {
        			outputString = GUILayout.TextArea(outputString, textFieldStyle, GUILayout.Width(outputWidth),
        				GUILayout.ExpandHeight(false));
        		}

        		GUILayout.EndHorizontal();
        		GUILayout.EndArea();
        	}
        }

        public static class OutputHelper {
        	public static void PrintOutput(Role role, String str, bool forceSpeak = false) {
        		OutputController[] outputs;
        		outputs = GameObject.Find("IOController").GetComponents<OutputController>();

        		foreach (OutputController outputController in outputs) {
        //			Debug.Log (str);
        //			Debug.Log (GetCurrentOutputString (role));
        //			Debug.Log (outputController.outputString);
        			if (outputController.role == role) {
        				if (GetCurrentOutputString(role) != str) {
        					outputController.outputString = str;

        					// TODO 6/6/2017-23:17 krim - need a dedicated "agent" game object, not a general "IOcontroller"
        					VoiceController[] voices = GameObject.Find("IOController").GetComponents<VoiceController>(); // Should be on agent
        					foreach (VoiceController voice in voices) {
        						if (voice.role == role) {
        							Debug.Log(string.Format("Speaking: \"{0}\"", str));
        							voice.Speak(str);
        						}
        					}
        				}
        				else if (forceSpeak) {
        					ForceRepeat(role);
        				}
        			}
        		}
        	}

        	public static string GetCurrentOutputString(Role role) {
        		string output = string.Empty;
        		OutputController[] outputs;
        		outputs = GameObject.Find("IOController").GetComponents<OutputController>();

        		foreach (OutputController outputController in outputs) {
        			if (outputController.role == role) {
        				output = outputController.outputString;
        				break;
        			}
        		}

        		return output;
        	}

        	public static void ForceRepeat(Role role) {
        		OutputController[] outputs;
        		outputs = GameObject.Find("IOController").GetComponents<OutputController>();

        		foreach (OutputController outputController in outputs) {
        			if (outputController.role == role) {
        				// TODO 6/6/2017-23:17 krim - need a dedicated "agent" game object, not a general "IOcontroller"
        				VoiceController[] voices = GameObject.Find("IOController").GetComponents<VoiceController>();
        				foreach (VoiceController voice in voices) {
        					if (voice.role == role) {
        						Debug.Log(string.Format("Speaking: \"{0}\"", outputController.outputString));
        						voice.Speak(outputController.outputString);
        					}
        				}
        			}
        		}
        	}
        }
    }
}