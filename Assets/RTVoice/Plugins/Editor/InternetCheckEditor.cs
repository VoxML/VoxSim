using UnityEngine;
using UnityEditor;

namespace Crosstales.RTVoice.EditorExt
{
    /// <summary>Custom editor for the 'InternetCheck'-class.</summary>
    [InitializeOnLoad]
    [CustomEditor(typeof(Tool.InternetCheck))]
    public class InternetCheckEditor : Editor
    {

        #region Variables

        private Tool.InternetCheck script;

        #endregion


        #region Editor methods

        public void OnEnable()
        {
            script = (Tool.InternetCheck)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorHelper.SeparatorUI();

            if (script.isActiveAndEnabled)
            {
                GUILayout.Label("Information", EditorStyles.boldLabel);

                GUILayout.Label("Internet Available:\t" + (Tool.InternetCheck.isInternetAvailable ? "Yes" : "No"));
                //GUILayout.Label("Internet Available:\t" + (Tool.InternetCheck.isInternetAvailable ? "☒" : "☐"));

                GUILayout.Label("Last Check:\t" + Tool.InternetCheck.LastCheck);

                GUILayout.Label("Number Of Checks:\t" + Tool.InternetCheck.CheckCounter);

                if (!Util.Helper.isWebPlatform && !Util.Helper.isEditorMode)
                    GUILayout.Label("Downloaded Data:\t" + Util.Helper.FormatBytesToHRF(Tool.InternetCheck.DownloadedData));

                if (Util.Helper.isEditorMode)
                {
                    EditorHelper.SeparatorUI();

                    if (GUILayout.Button(new GUIContent(" Refresh", EditorHelper.Icon_Reset, "Restart the Internet availability check.")))
                    {
                        Tool.InternetCheck.Refresh();
                    }
                }
            }
            else
            {
                GUILayout.Label("Script is disabled!", EditorStyles.boldLabel);
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        #endregion

    }
}
// © 2017 crosstales LLC (https://www.crosstales.com)