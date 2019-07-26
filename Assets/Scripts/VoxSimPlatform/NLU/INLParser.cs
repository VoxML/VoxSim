using VoxSimPlatform.Network;

namespace VoxSimPlatform {
    namespace NLU {
        /// <summary>
        /// Interface for parsing input strings into commands.
        /// </summary>
    	public interface INLParser {
    		string NLParse(string rawSent); // Allow result of "WAIT"
            string ConcludeNLParse();


    		//void InitParserService(NLUServerHandler nlu_server = null);
            void InitParserService(NLUIOClient nluIO);
        }
    }
}