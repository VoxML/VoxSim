using System;

#if !UNITY_WEBGL
using VoxSimPlatform.Network;
#endif

namespace VoxSimPlatform {
    namespace NLU {
        /// <summary>
        /// Interface for parsing input strings into commands.
        /// </summary>
    	public interface INLParser {
    		string NLParse(string rawSent); // Allow result of "WAIT"
            string ConcludeNLParse();


            //void InitParserService(NLUServerHandler nlu_server = null);
#if !UNITY_WEBGL

			void InitParserService(SocketConnection socketConnection = null, Type expectedSyntax = null);
			void InitParserService(RESTClient restClient = null, Type expectedSyntax = null); 
#endif
        }
    }
}