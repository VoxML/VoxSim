using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;

namespace NLU
{
	public class SimpleParser : INLParser {

		private List<string> _events = new List<string>(new[]
		{
			"grasp",
			"hold",
			"touch",
			"move",
			"turn",
			"roll",
			"spin",
			"stack",
			"put",
			"lean on",
			"lean against",
			"flip on edge",
			"flip at center",
			"flip",
			"close",
			"open",
			"lift",
			"drop",
			"reach",
			"slide"});

		private List<string> _objects = new List<string>(new[]
		{
			"block",
			"ball",
			"plate",
			"cup",
			"cup1",
			"cup2",
			"cup3",
			"cups",
			"disc",
			"spoon",
			"book",
			"blackboard",
			"bottle",
			"grape",
			"apple",
			"banana",
			"table",
			"bowl",
			"knife",
			"pencil",
			"paper_sheet",
			"hand",
			"arm",
			"mug",
			"block1",
			"block2",
			"block3",
			"block4",
			"block5",
			"block6",
			"blocks",
			"lid",
			"stack",
			"staircase",
			"pyramid",
			"cork",
		});

		private Dictionary<string,string> shittyPorterStemmer = new Dictionary<string, string> () {
			// not even a goddamn stemmer
			{"blocks","block"},
			{"balls","ball"},
			{"plates","plate"},
			{"cups","cup"},
			{"discs","disc"},
			{"spoons","spoon"},
			{"books","book"},
			{"blackboards","blackboard"},
			{"bottles","bottle"},
			{"grapes","grape"},
			{"apples","apple"},
			{"bananas","banana"},
			{"tables","table"},
			{"bowls","bowl"},
			{"knives","knife"},
			{"pencils","pencil"},
			{"paper sheets","paper_sheet"},
			{"mugs","mug"},
			{"lids","lid"},
			{"stack","stack"},
			{"starcases","staircase"},
			{"pyramids","pyramid"},
			{"corks","cork"}
			// sorry about this, Keigh
			// let us delete this when EACL is over and never speak of it again
		};


		private List<string> _relations = new List<string>(new[]
		{
			"touching",
			"in",
			"on",
			"at",
			"behind",
			"in front of",
			"near",
			"left of",
			"right of",
			"center of",
			"edge of",
			"under",
			"against"
		});

		private List<string> _attribs = new List<string>(new[]
		{
			"brown",
			"blue",
			"black",
			"green",
			"yellow",
			"red",
			"orange",
			"pink",
			"white",
			"gray",
			"purple",
			"leftmost",
			"middle",
			"rightmost"
		});

		private List<string> _determiners = new List<string>(new[]
		{
			"the",
			"a",
			"two"
		});

		private List<string> _exclude = new List<string>();

		private string[] SentSplit(string sent)
		{
			sent = sent.ToLower().Replace("paper sheet", "paper_sheet");
			var tokens = new List<string>(Regex.Split(sent, " +"));
			return tokens.Where(token => !_exclude.Contains(token)).ToArray();
		}

		public string NLParse(string rawSent)
		{
			foreach (string plural in shittyPorterStemmer.Keys) {
				rawSent = rawSent.Replace (plural, shittyPorterStemmer [plural]);
			}

			var tokens = SentSplit(rawSent);
			var form = tokens[0] + "(";
			var cur = 1;
			var end = tokens.Length;
			var lastObj = "";

			while (cur < end)
			{
				if (tokens[cur] == "and")
				{
					form += ",";
					cur++;
				}
				else if (cur + 2 < end &&
						 tokens[cur] == "in" && tokens[cur + 1] == "front" && tokens[cur + 2] == "of")
				{
					form += ",in_front(";
					cur += 3;
				}
				else if (cur + 1 < end &&
						 tokens[cur] == "left" && tokens[cur + 1] == "of")
				{
					form += ",left(";
					cur += 2;
				}
				else if (cur + 1 < end &&
						 tokens[cur] == "right" && tokens[cur + 1] == "of")
				{
					form += ",right(";
					cur += 2;
				}
				else if (cur + 1 < end &&
						 tokens[cur] == "center" && tokens[cur + 1] == "of")
				{
					form += ",center(";
					cur += 2;
				}
				else if (_relations.Contains(tokens[cur]))
				{
					if (form.EndsWith("("))
					{
						form += tokens[cur] + "(";
					}
					else
					{
						if (tokens[cur] == "at" && tokens[cur + 1] == "center")
						{
							form += ",center(" + lastObj;
						}
						else if (tokens[cur] == "on" && tokens[cur + 1] == "edge")
						{
							form += ",edge(" + lastObj;
						}
						else
						{
							form += "," + tokens[cur] + "(";
						}
					}
					cur += 1;
				}
				else if (_determiners.Contains(tokens[cur]))
				{
					form += tokens[cur] + "(";
					cur += ParseNextNP(tokens.Skip(cur+1).ToArray(), ref form, ref lastObj);
				}
				else if (_attribs.Contains(tokens[cur]))
				{
					cur += ParseNextNP(tokens.Skip(cur).ToArray(), ref form, ref lastObj);
				}
				else if (_objects.Contains(tokens[cur]))
				{
					lastObj = tokens[cur];
					form += lastObj;
					form = MatchParens(form);
					cur++;
				}
				else if (tokens[cur].StartsWith("v@"))
				{
					form += "," + tokens [cur].ToUpper();
					Debug.Log (form);
					cur++;
				}
				else
				{
					cur++;
				}
			}
			form = MatchParens(form);
//			form += string.Concat(Enumerable.Repeat(")", opens - closes));
			return form;
		}

		private string MatchParens(string input)
		{
			for (int i = input.Count(c => c == ')'); i < input.Count(c => c == '('); i++)
			{
				input += ")";
			}
			return input;
		}

		private int ParseNextNP(string[] restOfSent, ref string parsed, ref string lastObj)
		{
			var cur = 0;
		    var openParen = 0;
			var end = restOfSent.Length;
			while (cur < end)
			{
				if (_attribs.Contains(restOfSent[cur]))
				{
					// allows only one adjective per a parenthesis level
					parsed += restOfSent[cur] + "(";
				    openParen++;
					cur++;
				}
				else if (_objects.Contains(restOfSent[cur]))
				{
					lastObj = restOfSent[cur];
				    parsed += lastObj;
				    for (var i = 0; i < openParen; i++)
				    {
                        parsed += ")";
				    }
                    parsed += ")";
					cur++;
				}
				else if (restOfSent[cur] == "and")
				{
					parsed += ",";
					cur++;
				}
				else
				{
					MatchParens(parsed);
					break;
				}
			}
			return ++cur;
		}

		public void InitParserService(string address) {
			// do nothing
		}
	}
}