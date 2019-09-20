using UnityEngine;
using System;

using VoxSimPlatform.UI;

namespace VoxSimPlatform {
    namespace Agent {
        public class AgentTextController : FontManager {
            //// For handling the fonty, texty bits of output. Gonna name it fontman.
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
            public String outputString;
            public String outputLabel;
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
                fontSizeModifier = (float)((float)fontSize / (float)this.defaultFontSize);

                outputWidth = (Convert.ToInt32(Screen.width - outputMargin) > outputMaxWidth)
                    ? outputMaxWidth
                    : Convert.ToInt32(Screen.width - outputMargin);
                outputHeight = Convert.ToInt32(20.0f * (float)fontSizeModifier);

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
                        outputRect = new Rect((int)((Screen.width / 2) - (outputWidth / 2)), 5, outputWidth, outputHeight);
                    }
                    else if (placement == Placement.Bottom) {
                        outputRect = new Rect((int)((Screen.width / 2) - (outputWidth / 2)),
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
    }
}
