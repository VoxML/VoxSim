using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Global;
using Satisfaction;

public class OutputController : FontManager {
	public enum Role {
		Planner,
		Affector
	}
	public Role role;

	public enum Alignment {
		Left,
		Right
	}
	public Alignment alignment;

	public int fontSize = 12;

	public String outputLabel;
	public String outputString;
	public int outputHeight = 25;
	public Rect outputRect = new Rect();

	GUIStyle labelStyle = new GUIStyle ("Label");
	GUIStyle textFieldStyle = new GUIStyle ("TextField");

	float fontSizeModifier;

	void Start() {
		labelStyle = new GUIStyle ("Label");
		textFieldStyle = new GUIStyle ("TextField");
		fontSizeModifier = (int)(fontSize / defaultFontSize);

		outputHeight = (int)(25 * fontSizeModifier);

		labelStyle.fontSize = fontSize;
		textFieldStyle.fontSize = fontSize;

		if (alignment == Alignment.Left) {
			outputRect = new Rect (5, outputRect.y, (int)(365*fontSizeModifier), outputHeight);
		}
		else if (alignment == Alignment.Right) {
			outputRect = new Rect (Screen.width - (5 + (int)(365*fontSizeModifier)), outputRect.y, (int)(365*fontSizeModifier), outputHeight);
		}
	}

	void Update() {
	}

	void OnGUI() {
		GUILayout.BeginArea (outputRect);
		GUILayout.BeginHorizontal();
		GUILayout.Label(outputLabel+":", labelStyle);
		outputString = GUILayout.TextArea(outputString, textFieldStyle, GUILayout.Width(300*fontSizeModifier), GUILayout.ExpandHeight (false));
		GUILayout.EndHorizontal ();
		GUILayout.EndArea();
	}
}

public static class OutputHelper {
	public static void PrintOutput(OutputController.Role role, String str) {
		OutputController[] outputs;
		outputs = GameObject.Find ("IOController").GetComponents<OutputController>();

		foreach (OutputController outputController in outputs) {
			if (outputController.role == role) {
				outputController.outputString = str;
			}
		}
	}

	public static string GetCurrentOutputString(OutputController.Role role) {
		string output = string.Empty;
		OutputController[] outputs;
		outputs = GameObject.Find ("IOController").GetComponents<OutputController>();

		foreach (OutputController outputController in outputs) {
			if (outputController.role == role) {
				output = outputController.outputString;
				break;
			}
		}

		return output;
	}
}
