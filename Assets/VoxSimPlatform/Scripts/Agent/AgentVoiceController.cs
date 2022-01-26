using UnityEngine;
using System;

//using Crosstales.RTVoice;
//using Crosstales.RTVoice.Model;

namespace VoxSimPlatform {
    namespace Agent {
        public  enum MacMaleVoice
        {
            Alex,
            Daniel,
            Diego,
            Fred,
            Jorge,
            Juan,
            Luca,
            Maged,
            Rishi,
            Thomas,
            Xander,
            Yuri
        };

        public enum MacFemaleVoice
        {
            Alice,
            Alva,
            Amelie,
            Anna,
            Carmit,
            Damayanti,
            Ellen,
            Fiona,
            Ioana,
            Joana,
            Kanya,
            Karen,
            Kyoko,
            Laura,
            Lekha,
            Luciana,
            Mariska,
            Mei_Jia,
            Melina,
            Milena,
            Moira,
            Monica,
            Nora,
            Paulina,
            Samantha,
            Sara,
            Satu,
            Sin_ji,
            Tessa,
            Ting_Ting,
            Veena,
            Victoria,
            Yelda,
            Yuna,
            Zosia,
            Zuzana
        };

        public class AgentVoiceController : MonoBehaviour {/*
            /// <summary>
            /// For attaching to the agent insead of to an IOController
            /// </summary>


            public MacFemaleVoice MacFemaleVoice;
            public MacMaleVoice MacMaleVoice;
            private static readonly String WinFemaleVoice = "Microsoft Zira Desktop";
            private static readonly String WinMaleVoice = "Microsoft David Desktop";
            private static readonly String LANG = "en-US";
            private static readonly String DUMMY = "not-important";

            // TODO 6/6/2017-23:20 these fields can be inherited from a proper "agent" game object
            // , which is not implemented yet.
            // Currently these enums are in "agent" namespace
            public Role role;
            public Gender Gender;

             LiveSpeaker _speaker;
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
                        vName = WinFemaleVoice;
                    }
                    else if (SystemInfo.operatingSystemFamily ==
                             OperatingSystemFamily.MacOSX) {
                        vName = MacFemaleVoice.ToString().Replace('_', '-');
                    }
                }
                else if (Gender == Gender.Male) {
                    vGender = Crosstales.RTVoice.Model.Enum.Gender.MALE;
                    if (SystemInfo.operatingSystemFamily ==
                        OperatingSystemFamily.Windows) {
                        vName = WinMaleVoice;
                    }
                    else if (SystemInfo.operatingSystemFamily ==
                             OperatingSystemFamily.MacOSX) {
                        vName = MacMaleVoice.ToString().Replace('_', '-');
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
                    _speaker.Speak(new Wrapper(text, v, 1, 1, 1, true));
                }
            }

            public bool IsAnyoneSpeaking() {
                return GameObject.Find("RTVoice").GetComponent<AudioSource>() != null;
            }

            void OnGUI() {
            }*/
        }
    }
}