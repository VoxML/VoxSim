using System;
using System.Diagnostics;
//using System.Management;
using VoxSimPlatform.Network;
using UnityEngine;
using Debug = UnityEngine.Debug; // Ambiguity wooo

namespace VoxSimPlatform {
    namespace NLU {
        /// <summary>
        /// Interface for parsing input strings into commands.
        /// </summary>
    	public class PythonExternalParser : INLParser {
            public string NLParse(string rawSent) {
                string to_return = "";
                to_return = ExecuteCommand(rawSent);
                return to_return;
            }

            //public void InitParserService(NLUServerHandler nlu = null) {
            //    //Look, the SimpleParser doesn't do anything here either
            //    //It'll do URL lookups in a different class, I guess
            //}

            public static string ExecuteCommand(string command) {
                string to_return = "";
                Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "../PythonExternals/dist/SimpleParserPython"; // Application
                proc.StartInfo.Arguments = '"' + command + '"'; //Important that it is passed as one string instead of a bunch of args lol
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();

                
                //Debug.LogWarning(ProcessExecutablePath(process).ToString());
                
                while (!proc.StandardOutput.EndOfStream) {
                    to_return = proc.StandardOutput.ReadLine();
                    Debug.LogWarning(to_return);
                }
                return to_return;
            }

            public void InitParserService(NLUIOClient nluIO) {
                throw new NotImplementedException();
            }

            public string ConcludeNLParse() {
                throw new NotImplementedException();
            }

            //public static void Main(string[] args) {
            //    ExecuteCommand("gnome-terminal -x bash -ic 'cd $HOME; ls; bash'");
            //}

        }
    }
}