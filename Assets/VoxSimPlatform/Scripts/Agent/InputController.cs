using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using VoxSimPlatform.Core;
using VoxSimPlatform.Network;
using VoxSimPlatform.UI;

namespace VoxSimPlatform {
    namespace Agent {
        public class InputEventArgs : EventArgs {
        	public string InputString { get; set; }

        	public InputEventArgs(string str) {
        		this.InputString = str;
        	}
        }

        public class InputController : FontManager {
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

        	public String inputLabel;
        	public String inputString;
        	public int inputWidth;
        	public int inputMaxWidth;
        	public int inputHeight;
        	public int inputMargin;
        	public Rect inputRect = new Rect();

        	public bool textField = true;
        	public bool silenceAcknowledgment = false;
        	public bool allowToggleAgent = true;

        	String[] commands;
        	EventManager eventManager;
        	Macros macros;

        	CommunicationsBridge commBridge;

        	ObjectSelector objSelector;
        	//ExitToMenuUIButton exitToMenu;

        	String disableEnable;

        	GUIStyle textAreaStyle = new GUIStyle();

        	GUIStyle labelStyle;
        	GUIStyle textFieldStyle;
        	GUIStyle buttonStyle;

        	float fontSizeModifier;

        	TouchScreenKeyboard keyboard;

        	public event EventHandler InputReceived;

        	public void OnInputReceived(object sender, EventArgs e) {
        		if (InputReceived != null) {
        			InputReceived(this, e);
        		}
        	}

        	public event EventHandler ParseComplete;

        	public void OnParseComplete(object sender, EventArgs e) {
        		if (ParseComplete != null) {
        			ParseComplete(this, e);
        		}
        	}

        	void Start() {
        		GameObject bc = GameObject.Find("BehaviorController");
        		eventManager = bc.GetComponent<EventManager>();
        		macros = bc.GetComponent<Macros>();

        		objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
        		//exitToMenu = GameObject.Find ("VoxWorld").GetComponent<ExitToMenuUIButton> ();

        		commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();

        		labelStyle = new GUIStyle("Label");
        		textFieldStyle = new GUIStyle("TextField");
        		buttonStyle = new GUIStyle("Button");
        		labelStyle.fontSize = fontSize;
        		textFieldStyle.fontSize = fontSize;
        		buttonStyle.fontSize = fontSize;

        		fontSizeModifier = (int) (fontSize / defaultFontSize);

        		inputWidth = (Convert.ToInt32(Screen.width - inputMargin) > inputMaxWidth)
        			? inputMaxWidth
        			: Convert.ToInt32(Screen.width - inputMargin);
        		inputHeight = Convert.ToInt32(20.0f * (float) fontSizeModifier);

        		if (alignment == Alignment.Left) {
        			if (placement == Placement.Top) {
        				inputRect = new Rect(5, 5, inputWidth, inputHeight);
        			}
        			else if (placement == Placement.Bottom) {
        				inputRect = new Rect(5, Screen.height - inputHeight - 5, inputWidth, inputHeight);
        			}
        		}
        		else if (alignment == Alignment.Center) {
        			if (placement == Placement.Top) {
        				inputRect = new Rect((int) ((Screen.width / 2) - (inputWidth / 2)), 5, inputWidth, inputHeight);
        			}
        			else if (placement == Placement.Bottom) {
        				inputRect = new Rect((int) ((Screen.width / 2) - (inputWidth / 2)),
        					Screen.height - inputHeight - 5, inputWidth, inputHeight);
        			}
        		}
        		else if (alignment == Alignment.Right) {
        			if (placement == Placement.Top) {
        				inputRect = new Rect(Screen.width - (5 + inputWidth), 5, inputWidth, inputHeight);
        			}
        			else if (placement == Placement.Bottom) {
        				inputRect = new Rect(Screen.width - (5 + inputWidth),
        					Screen.height - inputHeight - 5, inputWidth, inputHeight);
        			}
        		}

        		//inputRect = new Rect (5, 5, 50, 25);
        //		inputHeight = (int)(20*fontSizeModifier);
        //
        //		inputRect = new Rect (5, 5, (int)(365*fontSizeModifier), inputHeight);
        	}

        	void Update() {
        	}

        	void OnGUI() {
        		if (!textField) {
        			return;
        		}

        		labelStyle = new GUIStyle("Label");
        		textFieldStyle = new GUIStyle("TextField");
        		buttonStyle = new GUIStyle("Button");
        #if !UNITY_IOS
        		Event e = Event.current;
        		if (e.keyCode == KeyCode.Return) {
        			if (inputString != "") {
        				MessageReceived(inputString);

        				// warning: switching to TextArea here (and below) seems to cause crash
        				GUILayout.BeginArea(inputRect);
        				GUILayout.BeginHorizontal();
        				GUILayout.Label(inputLabel + ":", labelStyle);
        				inputString = GUILayout.TextField("", textFieldStyle, GUILayout.Width(300 * fontSizeModifier),
        					GUILayout.ExpandHeight(false));
        				GUILayout.EndHorizontal();
        				GUILayout.EndArea();

        				//GUI.Label (inputRect, inputLabel+":");
        				//inputString = GUI.TextField (inputRect, ""); 
        			}
        		}
        		else {
        			//GUI.Label (inputRect, inputLabel+":");
        			//inputString = GUI.TextField (inputRect, inputString);
        			GUILayout.BeginArea(inputRect);
        			GUILayout.BeginHorizontal();
        			GUILayout.Label(inputLabel + ":", labelStyle);
        			inputString = GUILayout.TextField(inputString, textFieldStyle, GUILayout.Width(300 * fontSizeModifier),
        				GUILayout.ExpandHeight(false));
        			GUILayout.EndHorizontal();
        			GUILayout.EndArea();
        		}
        #else
        		if (!TouchScreenKeyboard.visible) {
        			if (inputString != "") {
        				MessageReceived (inputString);

        				GUILayout.BeginArea (inputRect);
        				GUILayout.BeginHorizontal();
        				GUILayout.Label(inputLabel+":", labelStyle);
        				inputString =
         GUILayout.TextField("", textFieldStyle, GUILayout.Width(300*fontSizeModifier), GUILayout.ExpandHeight (false));
        				GUILayout.EndHorizontal ();
        				GUILayout.EndArea();
        			}
        			else {
        				// warning: switching to TextArea here (and below) seems to cause crash
        				GUILayout.BeginArea (inputRect);
        				GUILayout.BeginHorizontal();
        				GUILayout.Label(inputLabel+":", labelStyle);
        				inputString =
         GUILayout.TextField("", textFieldStyle, GUILayout.Width(300*fontSizeModifier), GUILayout.ExpandHeight (false));
        				GUILayout.EndHorizontal ();
        				GUILayout.EndArea();

        				//GUI.Label (inputRect, inputLabel+":");
        				//inputString = GUI.TextField (inputRect, ""); 
        			}
        		}
        		else {

        			//GUI.Label (inputRect, inputLabel+":");
        			//inputString = GUI.TextField (inputRect, inputString);
        			GUILayout.BeginArea (inputRect);
        			GUILayout.BeginHorizontal();
        			GUILayout.Label(inputLabel+":", labelStyle);
        			inputString =
         GUILayout.TextField(inputString, textFieldStyle, GUILayout.Width(300*fontSizeModifier), GUILayout.ExpandHeight (false));
        			GUILayout.EndHorizontal ();
        			GUILayout.EndArea();
        		}
        #endif


        		/* DEBUG BUTTONS */

        //		if (GUI.Button (new Rect (10, Screen.height - ((10 + (int)(20*exitToMenu.FontSizeModifier)) + (10 + (int)(40*fontSizeModifier))),
        //			100*fontSizeModifier, 20*fontSizeModifier), "Reset", buttonStyle)) {
        //			StartCoroutine(SceneHelper.LoadScene (UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name));
        //			return;
        //		}

        //		if (GameObject.FindGameObjectWithTag ("Agent") != null) {
        //			if (allowToggleAgent) {
        //				disableEnable = (objSelector.disabledObjects.FirstOrDefault (t => t.tag == "Agent") == null) ? "Disable Agent" : "Enable Agent";
        //				if (GUI.Button (new Rect (10, Screen.height - ((10 + (int)(20 * exitToMenu.FontSizeModifier)) + (10 + (int)(40 * fontSizeModifier))),
        //					   100 * fontSizeModifier, 20 * fontSizeModifier), disableEnable, buttonStyle)) {
        //					GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
        //					if (agent != null) {
        //						if (agent.activeInHierarchy) {
        //							eventManager.preds.DISABLE (new object[]{ agent });
        //						}
        //					} else {
        //						agent = objSelector.disabledObjects.First (t => t.tag == "Agent");
        //						eventManager.preds.ENABLE (new object[]{ agent });
        //					}
        //					return;
        //				}
        //			}
        //		}
        	}

        	public void MessageReceived(String inputString) {
        		Regex r = new Regex(@".*\(.*\)");
        		Regex v = new Regex("<.*?;.*?;.*?>");
        		string functionalCommand = "";

        		if (inputString != "") {
        			InputEventArgs inputArgs = new InputEventArgs(inputString);
        			OnInputReceived(this, inputArgs);

        			Debug.Log("User entered: " + inputString);

                    if (inputString.StartsWith("qsr:")) {
                        SpatialReasoning.QSR.QSRLibSocket qsrLibSocket =
                            (SpatialReasoning.QSR.QSRLibSocket)commBridge.FindSocketConnectionByType(typeof(SpatialReasoning.QSR.QSRLibIOClient));
                        qsrLibSocket.SendQSRRequest(inputString);
                        return;
                    }

        			Dictionary<string, string> vectors = new Dictionary<string, string>();

        			foreach (Match match in v.Matches(inputString)) {
        				vectors.Add(string.Format("V@{0}", match.Index), match.Value);
        				Debug.Log(string.Format("{0}:{1}", string.Format("V@{0}", match.Index), match.Value));
        				inputString = v.Replace(inputString, string.Format("V@{0}", match.Index), 1);
        			}

        			Debug.Log("Formatted as: " + inputString);

        			if (!r.IsMatch(inputString)) {
        				// is not already functional form
        				// parse into functional form
        				String[] inputs = inputString.Split(new char[] {'.', ',', '!'});
        				List<String> commands = new List<String>();
        				foreach (String s in inputs) {
        					if (s != String.Empty) {
        						commands.Add(commBridge.NLParse(s.Trim().ToLower()));
        					}
        				}

        				functionalCommand = String.Join(";", commands.ToArray());
        			}
        			else {
        				functionalCommand = inputString;
        			}

        			Debug.Log(functionalCommand);

        			if (functionalCommand.Count(x => x == '(') == functionalCommand.Count(x => x == ')')) {
        				//eventManager.ClearEvents ();
        				if (macros != null) {
        					foreach (KeyValuePair<String, String> kv in macros.commandMacros) {
        						// if input is a macro
        						if (functionalCommand == kv.Key) {
        							// sub in value
        							functionalCommand = kv.Value;
        							break;
        						}
        					}
        				}

        				Debug.Log("Raw input parsed as: " + functionalCommand);
        				InputEventArgs parseArgs = new InputEventArgs(functionalCommand);
        				OnParseComplete(this, parseArgs);

        				if (!silenceAcknowledgment) {
        					OutputHelper.PrintOutput(Role.Affector, "OK.");
        					OutputHelper.PrintOutput(Role.Planner, "");
        				}
                            
        				commands = functionalCommand.Split(new char[] { ';', ':' });
        				foreach (String commandString in commands) {
        					string command = commandString;
        					foreach (string vector in vectors.Keys) {
        						command = command.Replace(vector, vectors[vector]);
        					}

        					// add to queue
        					eventManager.QueueEvent(command);
        				}

        				if (eventManager.immediateExecution) {
        					eventManager.ExecuteNextCommand();
        				}
        			}
        		}
        	}
        }
    }
}