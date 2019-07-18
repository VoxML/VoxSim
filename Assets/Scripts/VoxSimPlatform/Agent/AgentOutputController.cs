using UnityEngine;
using System;

using VoxSimPlatform.UI;

namespace VoxSimPlatform {
    namespace Agent {

        /// <summary>
        /// Manages either the output text box or the voice (or both) , as the case may be.
        /// OutputController and OutputFontManager and/or VoiceController2 are attached to the same agent
        /// </summary>
        public class AgentOutputController : MonoBehaviour { //// Make it so it has a FontManager instead

            public Role role;
            public AgentTextController fontManager; // Based on former implementation of outputcontroller
            public AgentVoiceController voice; // Also attached to the agent
            public String outputString; // The string currently being expressed.

            private void Start() {
                if (gameObject.GetComponent<AgentTextController>()) {
                    fontManager = gameObject.GetComponent<AgentTextController>();
                }
                if (gameObject.GetComponent<AgentVoiceController>()) {
                    voice = gameObject.GetComponent<AgentVoiceController>();
                }
                if (!fontManager && !voice) {
                    Debug.LogWarning("No FontManager or VoiceController on agent. I have no mouth and I must scream.");
                }
            }

            void Update() {
            }

            // Speak whether or not the string is new.
            // May be extraneous with forceSpeak parameter in SpeakOutput.
            internal void ForceRepeat() {
                Debug.Log(string.Format("Speaking: \"{0}\"", outputString));
                voice.Speak(outputString);
            }

            internal void PrintOutput(String str) {
                outputString = str;
                if (fontManager && fontManager.outputString != outputString) {
                    fontManager.outputString = outputString;
                }
                else {
                    Debug.LogWarning("No output text box to print to");
                }
            }

            internal void SpeakOutput(String str, bool forceSpeak = false) {
                if (voice) {
                    if(str != outputString || forceSpeak) {
                        Debug.Log(string.Format("Speaking: \"{0}\"", str));
                        outputString = str;
                        voice.Speak(str);
                    }
                }
                else {
                    Debug.LogWarning("No voice to speak of");
                }
            }

            // Generified version of SpeakOutPut and PrintOutput, so you don't need to know
            // the available output formats in order to use them.
            internal void PromptOutput(String str, bool forceSpeak = false) {
                SpeakOutput(str, forceSpeak); // Note: Order matters here
                PrintOutput(str); // since outputString gets changed in PrintOutput
            }
        }

        // Simply finds the controller on a given agent, then passes commands to it
        // Useful as a static class to call in demos
        public static class AgentOutputHelper {
            /// <param name="role"> Whether speaker is a Planner or Affector .</param>
            /// <param name="str"> The output string.</param>
            /// <param name="agentName">Agent's name.</param>
            /// <param name="forceSpeak">If set to true, force speak even if identical to previous utterance.</param>
            public static void PrintOutput(Role role, String str, string agentName, bool forceSpeak = false) {
                AgentOutputController outputController;
                // Find output controller(s) attached to agent with the right name
                outputController = GameObject.Find(agentName).GetComponent<AgentOutputController>();
                if (outputController && outputController.role == role) {
                    outputController.PrintOutput(str);
                }
            }

            /// <param name="role"> Whether speaker is a Planner or Affector .</param>
            /// <param name="str"> The output string.</param>
            /// <param name="agentName">Agent's name.</param>
            /// <param name="forceSpeak">If set to true, force speak even if identical to previous utterance.</param>
            public static void SpeakOutput(Role role, String str, string agentName, bool forceSpeak = false) {
                AgentOutputController outputController;
                //// Find Diana specifically, then find in her components
                outputController = GameObject.Find(agentName).GetComponent<AgentOutputController>(); //attach to Diana instead
                if (outputController && outputController.role == role) {
                    outputController.SpeakOutput(str, forceSpeak);
                }
            }

            /// returns the current output string by grabbing it from OutputController2
            /// If no such agent or controller, returns empty string
            /// <param name="role">Role.</param>
            /// <param name="agentName">Agent name.</param>
            public static string GetCurrentOutputString(Role role, String agentName) {
                string output = string.Empty;
                AgentOutputController outputController;
                outputController = GameObject.Find(agentName).GetComponent<AgentOutputController>();
                if(outputController && outputController.role == role) {
                    output = outputController.outputString;
                }
                return output;
            }

            /// Forces last statement to repeat even if identical to previous one
            /// May be unnecessary with forceSpeak parameter in SpeakOutput
            /// <param name="role">Role.</param>
            /// <param name="agentName">Agent name.</param>
            public static void ForceRepeat(Role role, String agentName) {
                AgentOutputController outputController;
                outputController = GameObject.Find(agentName).GetComponent<AgentOutputController>();
                if (outputController && outputController.role == role) {
                    outputController.ForceRepeat();
                }
            }
        }
    }
}