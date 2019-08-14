using UnityEngine;
using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VoxSimPlatform.Network;

namespace VoxSimPlatform {
    namespace NLU {

        /// <summary>
        /// This guy sends out a string, gets back a serialized JSON, converts to dict and then string
        /// </summary>
        public class PythonJSONParser : INLParser {
            //NLUServerHandler nlu_server = null;
            SocketConnection nluSocketConnection = null;
            RestClient nluRestClient = null;
            string route = ""; // Don't expect to be setting it anytime soon

            public void InitParserService(SocketConnection socketConnection = null) {
                //Look, the SimpleParser doesn't do anything here either
                nluSocketConnection = socketConnection;
            }

            public void InitParserService(RestClient restClient = null) {
                //Look, the SimpleParser doesn't do anything here either
                nluRestClient = restClient;
            }

            public string NLParse(string rawSent) {
                string to_return = "";
                //to_return = ExecuteCommand(rawSent);
                if (nluSocketConnection != null) {
                    // do stuff here
                }
                else if (nluRestClient != null) {
                    nluRestClient.Post(route, rawSent);
                }
                return "WAIT";
            }

            /// <summary>
            /// Grab result from server, parse, send up.
            /// </summary>
            /// <returns></returns>
            public string ConcludeNLParse() {
                string to_return = "";

                String to_print = "";
                if (nluSocketConnection != null) {
                    // Grab the result
                    // do stuff here
                }
                else if (nluRestClient != null) {
                    // Grab the result
                    //to_print = nluRestClient.last_read;
                    to_print = nluRestClient.webRequest.downloadHandler.text;
                }

                if (to_print == "empty" || to_print == null || to_print == "") {
                    return "";
                }
                to_return = JsonToFormat(to_print);
                return to_return; // And here it'll crash lol            }
            }

            private static string JsonToFormat(string json_result) {
                string to_return = "";
                var settings = new JsonSerializerSettings();

                JObject jsonParsed = JsonConvert.DeserializeObject<JObject>(json_result, settings);
                GenericSyntax syntax = new GenericSyntax(jsonParsed);
                to_return = syntax.ExportTagOrWords(true);
                return to_return;
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
                    if (jo == null) {
                        return;
                    }
                    JsonReader reader = jo.CreateReader();
                    var gs_copy = new Dictionary<string, GenericSyntax>(gs_dict);
                    foreach (KeyValuePair<string, GenericSyntax> item in gs_copy) {
                        if (jo[item.Key] != null) {
                            // Set the value of that GenericSyntax and recurse
                            gs_dict[item.Key] = new GenericSyntax((JObject)jo[item.Key]);
                        }
                    }
                    var leaf_copy = new Dictionary<string, string>(leaf_dict);
                    foreach (KeyValuePair<string, string> item in leaf_copy) {
                        if (jo[item.Key] != null) {
                            // Set the value of that GenericSyntax and recurse
                            leaf_dict[item.Key] = (string)jo[item.Key];
                        }
                    }
                }

                public string ExportTagOrWords(bool top = false) {
                    // This is a VERY BAD parser. Will turn a parse of
                    // "Put the yellow knife on the plate"
                    // into
                    // "put(the(yellow(knife)),on(the(plate)))"
                    // And do NOTHING ELSE correctly.
                    // But by part of speech, that's close to what Diana's good at *shrug*
                    string to_return = "";
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
                }

                public static GenericSyntax CreateFromJSON(string jsonString) {
                    return JsonUtility.FromJson<GenericSyntax>(jsonString);
                }
            }
        }
    }
}