using System;
using System.Diagnostics;
//using System.Management;
using UnityEngine;
using Debug = UnityEngine.Debug; // Ambiguity wooo
using Newtonsoft.Json;
using VoxSimPlatform;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

// Comment

//pyinstaller --onefile SimpleParserPython.py
// ^ useful script to turn a python file to a single executable file^

namespace VoxSimPlatform {
    namespace NLU {
        /// <summary>
        /// This guy sends out a string, gets back a serialized JSON, converts to dict and then string
        /// </summary>
    	public class PythonJSONParser : INLParser {
            public string NLParse(string rawSent) {
                string to_return = "";
                to_return = ExecuteCommand(rawSent);
                return to_return;
            }

            public void InitParserService(string address) {
                //Look, the SimpleParser doesn't do anything here either
                //It'll do URL lookups in a different class, I guess
            }

            public static string ExecuteCommand(string command) {
                string to_return = "";
                string json_result = "";
                Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "../PythonExternals/dist/jsonPythonParser"; //// Remember to rename
                proc.StartInfo.Arguments = '"' + command + '"'; //Important that it is passed as one string instead of a bunch of args lol
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();


                //Debug.LogWarning(ProcessExecutablePath(process).ToString());

                while (!proc.StandardOutput.EndOfStream) {
                    json_result = proc.StandardOutput.ReadLine();
                    Debug.LogWarning(json_result);
                    to_return = JsonToFormat(json_result);
                    Debug.LogWarning(to_return);
                }
                return to_return; // And here it'll crash lol
            }


            private static string JsonToFormat(string json_result) {
                string to_return = "";
                var settings = new JsonSerializerSettings();
                //settings.Converters.Add(new GenericSyntaxCreationConverter());
                //json_result = "{'V': 'grab', 'NP': {'Det': 'the', 'N': 'knife'}}";
                Debug.LogWarning(json_result);
                JObject jsonParsed = JsonConvert.DeserializeObject<JObject>(json_result, settings);
                //GenericSyntax jsonParsed = JsonConvert.DeserializeObject<GenericSyntax>(json_result);//.SerializeObject(mainPlayer);
                GenericSyntax syntax = new GenericSyntax(jsonParsed);
                Debug.LogWarning(jsonParsed.ToString());
                to_return = syntax.ExportTagOrWords(true);
                Debug.LogWarning(to_return);
                return to_return;
            }

        }


        /// <summary>
        /// Recursively defined data type to shove JSONs into ¯\_(ツ)_/¯
        /// </summary>
        [System.Serializable]
        public class GenericSyntax : JObject {
            public IDictionary<string, GenericSyntax> gs_dict = new Dictionary<string, GenericSyntax>()
            {
                {"S", null },
                {"PP", null },
                {"NP", null },
                {"VP", null }
            };
            public IDictionary<string, string> leaf_dict = new Dictionary<string, string>()
            {
                {"Det", string.Empty },
                {"N", string.Empty },
                {"V", string.Empty },
                {"P", string.Empty },
                {"Adj", string.Empty }
            };

            public GenericSyntax(JObject jo) {
                if(jo == null) {
                    return;
                }
                JsonReader reader = jo.CreateReader();
                var gs_copy = new Dictionary<string, GenericSyntax>(gs_dict);
                foreach (KeyValuePair<string, GenericSyntax> item in gs_copy) {
                    if(jo[item.Key] != null) {
                        // Set the value of that GenericSyntax and recurse
                        gs_dict[item.Key] = new GenericSyntax((JObject) jo[item.Key]);
                    }
                }
                var leaf_copy = new Dictionary<string, string>(leaf_dict);
                foreach (KeyValuePair<string, string> item in leaf_copy) {
                    if (jo[item.Key] != null) {
                        // Set the value of that GenericSyntax and recurse
                        leaf_dict[item.Key] = (string) jo[item.Key];
                    }
                }
            }

            public string ExportTagOrWords(bool top = false) {
                // This is a VERY BAD parser. Will turn a parse of
                // "Put the yellow knife on the plate"
                // into
                // "put(the(yellow(knife)),on(the(plate)))"
                // And do NOTHING ELSE correctly.
                string to_return = "";
                //GenericSyntax[] whichTag = { S, PP, NP, VP };
                //string[] whichWord = { Det, N, V, P, Adj };
                int count = 0; // Number of close parens

                if (gs_dict["S"] != null) {
                    to_return = to_return + gs_dict["S"].ExportTagOrWords();
                }
                if (gs_dict["VP"] != null) {
                    to_return = to_return + gs_dict["VP"].ExportTagOrWords();
                }
                
                if (leaf_dict["V"] != string.Empty) {
                    // VP is higher priority than others.
                    to_return = to_return + leaf_dict["V"] + "(";
                }
                if (leaf_dict["P"] != string.Empty) {
                    to_return = to_return + leaf_dict["P"] + "(";
                    count += 1;
                }
                //if (leaf_dict["Det"] != string.Empty) {
                //    // No determiners wanted
                //    to_return = to_return + leaf_dict["Det"] + "(";
                //    count += 1;
                //}
                if (leaf_dict["Adj"] != string.Empty) {
                    to_return = to_return + leaf_dict["Adj"] + "(";
                    count += 1;
                }
                if (leaf_dict["N"] != string.Empty) {
                    to_return = to_return + leaf_dict["N"];
                }

                if (gs_dict["NP"] != null) {
                    to_return = to_return + gs_dict["NP"].ExportTagOrWords();
                }
                if (gs_dict["PP"] != null) {
                    to_return = to_return + "," + gs_dict["PP"].ExportTagOrWords();
                }


                if (top) {
                    // close all parens
                    count = to_return.Replace(")", "").Length - to_return.Replace("(", "").Length;
                }

                string closeparens = new string(')', count); // Python does this so much better
                to_return = to_return + closeparens;

                

                return to_return;


                //var gs_copy = new Dictionary<string, GenericSyntax>(gs_dict);
                //foreach (KeyValuePair<string, GenericSyntax> item in gs_copy) {
                //    if (item.Value != null) {
                //        to_return = to_return + item.Value.ExportTagOrWords() + ":";
                //    }
                //}
                //var leaf_copy = new Dictionary<string, string>(leaf_dict);
                //foreach (KeyValuePair<string, string> item in leaf_dict) {
                //    if (item.Value != null) {
                //        to_return = to_return + item.Value;
                //    }
                //}

                //to_return = to_return + ")";
                //return to_return;
            }

            public static GenericSyntax CreateFromJSON(string jsonString) {
                return JsonUtility.FromJson<GenericSyntax>(jsonString);
            }
        }
    }
}
