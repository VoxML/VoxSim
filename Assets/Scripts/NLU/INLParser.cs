namespace NLU
{
	public interface INLParser
	{
		string NLParse(string rawSent);

		void InitParserService(string address);
	}
}