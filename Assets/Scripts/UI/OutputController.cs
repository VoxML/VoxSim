using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Agent;
using Global;
using Satisfaction;

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
	public int outputHeight;
	public Rect outputRect = new Rect();

	GUIStyle labelStyle = new GUIStyle ("Label");
	GUIStyle textFieldStyle = new GUIStyle ("TextField");

	float fontSizeModifier;

	void Start() {
		labelStyle = new GUIStyle ("Label");
		textFieldStyle = new GUIStyle ("TextField");
		fontSizeModifier = (float)((float)fontSize / (float)defaultFontSize);

		outputWidth = System.Convert.ToInt32(385.0f * (float)fontSizeModifier);
		outputHeight = System.Convert.ToInt32(25.0f * (float)fontSizeModifier);

		labelStyle.fontSize = fontSize;
		textFieldStyle.fontSize = fontSize;

		if (alignment == Alignment.Left) {
			if (placement == Placement.Top) {
				outputRect = new Rect (5, 5, outputWidth, outputHeight);
			}
			else if (placement == Placement.Bottom) {
				outputRect = new Rect (5, Screen.height - outputHeight - 5, outputWidth, outputHeight);
			}
		}
		else if (alignment == Alignment.Center) {
			if (placement == Placement.Top) {
				outputRect = new Rect ((int)((Screen.width / 2) - (outputWidth / 2)), 5, outputWidth, outputHeight);
			}
			else if (placement == Placement.Bottom) {
				outputRect = new Rect ((int)((Screen.width / 2) - (outputWidth / 2)), Screen.height - outputHeight - 5, outputWidth, outputHeight);
			}
		}
		else if (alignment == Alignment.Right) {
			if (placement == Placement.Top) {
				outputRect = new Rect (Screen.width - (5 + outputWidth), 5, outputWidth, outputHeight);
			}
			else if (placement == Placement.Bottom) {
				outputRect = new Rect (Screen.width - (5 + outputWidth), Screen.height - outputHeight - 5, outputWidth, outputHeight);
			}
		}
	}

	void Update() {
	}

	void OnGUI() {
		GUILayout.BeginArea (outputRect);
		GUILayout.BeginHorizontal();
		GUILayout.Label(outputLabel+":", labelStyle);
		outputString = GUILayout.TextArea(outputString, textFieldStyle, GUILayout.Width(outputWidth-(65*fontSizeModifier)), GUILayout.ExpandHeight (false));
		GUILayout.EndHorizontal ();
		GUILayout.EndArea();
	}
}

public static class OutputHelper {
	public static void PrintOutput(Role role, String str) {
		OutputController[] outputs;
		outputs = GameObject.Find ("IOController").GetComponents<OutputController>();

		foreach (OutputController outputController in outputs) {
			if (outputController.role == role) {
				outputController.outputString = str;
			}
		}
	}

	public static string GetCurrentOutputString(Role role) {
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
