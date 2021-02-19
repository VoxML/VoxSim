#if !UNITY_WEBGL
using UnityEngine;
using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VoxSimPlatform.Network;
using VoxSimPlatform.NLU;

namespace VoxSimPlatform
{
	namespace Examples
	{
		namespace NLParsers
		{
			/// <summary>
			/// This guy sends out a string, gets back a serialized JSON, converts to dict and then string
			/// </summary>
			public class PythonJSONParser : INLParser
			{
				//NLUServerHandler nlu_server = null;
				SocketConnection nluSocketConnection = null;
				RESTClient nluRESTClient = null;
				string route = ""; // Don't expect to be setting it anytime soon
				Type syntaxType = null;

				public void InitParserService(SocketConnection socketConnection = null, Type expectedSyntax = null)
				{
					// set the connection instance
					nluSocketConnection = socketConnection;
					// set the expected syntax type (custom defined class or null)
					syntaxType = expectedSyntax;
				}

				public void InitParserService(RESTClient restClient = null, Type expectedSyntax = null)
				{
					// set the REST client instance
					nluRESTClient = restClient;
					// set the expected syntax type (custom defined class or null)
					syntaxType = expectedSyntax;
				}

				public string NLParse(string rawSent)
				{
					if (nluSocketConnection != null)
					{
						// do stuff here
					}
					else if (nluRESTClient != null)
					{
						RESTDataContainer result = new RESTDataContainer(nluRESTClient.owner, nluRESTClient.Post(route, rawSent));
						Debug.Log("Parse result: " + result.result);
					}
					return "WAIT";
				}

				/// <summary>
				/// Grab result from server, parse, send up.
				/// </summary>
				/// <returns></returns>
				public string ConcludeNLParse()
				{
					string returnVal = "";

					String toPrint = "";
					if (nluSocketConnection != null)
					{
						// Grab the result
						// do stuff here
					}
					else if (nluRESTClient != null)
					{
						// Grab the result
						//to_print = nluRestClient.last_read;
						toPrint = nluRESTClient.webRequest.downloadHandler.text;
					}

					if (toPrint == "empty" || toPrint == null || toPrint == "")
					{
						return "";
					}
					returnVal = JsonToFormat(toPrint);
					return returnVal; // And here it'll crash lol   //??
				}

				private string JsonToFormat(string jsonResult)
				{
					string toReturn = "";
					var settings = new JsonSerializerSettings();

					JObject jsonParsed = JsonConvert.DeserializeObject<JObject>(jsonResult, settings);
					IGenericSyntax syntax = (IGenericSyntax)Activator.CreateInstance(syntaxType, new object[] { jsonParsed });
					toReturn = syntax.ExportTagOrWords(true);
					return toReturn;
				}
			}
		}
	}
} 
#endif