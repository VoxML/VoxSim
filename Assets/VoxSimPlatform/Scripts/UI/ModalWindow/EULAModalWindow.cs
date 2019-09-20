using UnityEngine;

using VoxSimPlatform.UI.ModalWindow;

public class EULAModalWindow : ModalWindow {
	[TextArea(3, 10)] public string licenseText =
		"VoxSim is provided free for use under the GNU Lesser General Public License (LGPL) " +
		"subject to additional permission added to version 3 of the GNU General Public License (GPL), " +
		"viewable at https://www.gnu.org/licenses/lgpl-3.0.en.html.\n\n" +
		"VoxSim is a product of the Brandeis University Department of Computer Science.  " +
		"This build of VoxSim is research software created specifically for use at the " +
		"2017 Meeting of the European Chapter of the Association for Computational Linguistics (EACL2017).  " +
		"Upon acceptance of this license, the user assumes all risk related to use of the software.  " +
		"Brandeis University hereby disclaims any responsibility for malfunction or unintended effect related " +
		"to the use of VoxSim in return for providing affirmation of a good faith effort made to test and tailor the " +
		"system for anticipated user platforms and needs.";

	public int fontSize = 12;

	GUIStyle buttonStyle = new GUIStyle("Button");

	public bool accepted = false;

	float fontSizeModifier;

	public float FontSizeModifier {
		get { return fontSizeModifier; }
		set { fontSizeModifier = value; }
	}

	void Start() {
		//persistent = true;

		buttonStyle = new GUIStyle("Button");

		fontSizeModifier = (int) (fontSize / defaultFontSize);
		buttonStyle.fontSize = fontSize;

		base.Start();
	}

	// Update is called once per frame
	void Update() {
	}

	protected override void OnGUI() {
//		if (GUI.Button (new Rect (Screen.width-(15 + (int)(110*fontSizeModifier/3)),
//			Screen.height-(10 + (int)(20*fontSizeModifier)), 38*fontSizeModifier, 20*fontSizeModifier), "Help", buttonStyle))
//			render = true;

		base.OnGUI();
	}

	public override void DoModalWindow(int windowID) {
		base.DoModalWindow(windowID);

		//makes GUI window scrollable
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		GUILayout.Label(licenseText);
		GUILayout.EndScrollView();

		string acceptanceText = "Accept the End User License Agreement";
		if (GUI.Button(new Rect(windowRect.width / 2 - GUI.skin.label.CalcSize(new GUIContent(acceptanceText)).x / 2,
			windowRect.height - (GUI.skin.label.CalcSize(new GUIContent(acceptanceText)).y + 4) - 4,
			GUI.skin.label.CalcSize(new GUIContent(acceptanceText)).x + 16,
			GUI.skin.label.CalcSize(new GUIContent(acceptanceText)).y + 4), acceptanceText)) {
			accepted = true;
			windowManager.UnregisterWindow(this);
			Destroy(this);
		}
	}

	void OnDestroy() {
		SendMessage("EULAAccepted", accepted);
	}
}
