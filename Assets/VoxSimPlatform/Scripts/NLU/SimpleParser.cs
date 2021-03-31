using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using VoxSimPlatform.Global;
#if !UNITY_WEBGL
using VoxSimPlatform.Network;
#endif

namespace VoxSimPlatform {
    namespace NLU {
        public class SimpleParser : INLParser {
            private List<string> _events = new List<string>(new[] {
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
                "slide"
            });

            private List<string> _objects = new List<string>(new[] {
                "block",
                "ball",
                "cylinder",
                "plate",
                "cup",
                "cup1",
                "cup2",
                "cup3",
                "cups",
                "disc",
                "spoon",
                "fork",
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
                "block7",
                "blocks",
                "lid",
                "stack",
                "staircase",
                "pyramid",
                "cork",
            });

            private List<string> _objectVars = new List<string>(new[] {
                "{0}"
            });

            private List<string> _anaphorVars = new List<string>(new[] {
                "{2}"
            });

            /// <summary>
            /// A super simple mapping of plural to singular. Surprisingly, I'm not the one to name it this.
            /// To be deleted???
            /// </summary>
    		private Dictionary<string, string> shittyPorterStemmer = new Dictionary<string, string>() {
    			// not even a goddamn stemmer
    			{"blocks", "block.pl"},
                {"balls", "ball.pl"},
                {"cylinders", "cylinder.pl"},
                {"plates", "plate.pl"},
                {"cups", "cup.pl"},
                {"discs", "disc.pl"},
                {"spoons", "spoon.pl"},
                {"forks", "fork.pl"},
                {"books", "book.pl"},
                {"blackboards", "blackboard.pl"},
                {"bottles", "bottle.pl"},
                {"grapes", "grape.pl"},
                {"apples", "apple.pl"},
                {"bananas", "banana.pl"},
                {"tables", "table.pl"},
                {"bowls", "bowl.pl"},
                {"knives", "knife.pl"},
                {"pencils", "pencil.pl"},
                {"paper sheets", "paper_sheet.pl"},
                {"mugs", "mug.pl"},
                {"lids", "lid.pl"},
                {"stacks", "stack.pl"},
                {"starcases", "staircase.pl"},
                {"pyramids", "pyramid.pl"},
                {"corks", "cork.pl"}
    			// sorry about this, Keigh
    			// let us delete this when EACL is over and never speak of it again
                // 2019: still haven't deleted this crap (:
    		};


            private List<string> _relations = new List<string>(new[] {
                "touching",
                "in",
                "on",
                "atop", // prithee sirrah, put the black block atop the yellow block
                "port",
                "starboard",
                "afore",
                "astern",
                "at",
                "behind",
                "in front of",
                "beside",
                "near",
                "left of",
                "right of",
                "center of",
                "edge of",
                "under",
                "against",
                "here",
                "there"
            });

            private List<string> _relationVars = new List<string>(new[] {
                "{1}"
            });

            private List<string> _attribs = new List<string>(new[] {
                "b",
                "c",
                "d",
                "e",
                "f",
                "g",
                "h",
                "i",
                "j",
                "k",
                "l",
                "m",
                "n",
                "o",
                "p",
                "q",
                "r",
                "s",
                "t",
                "u",
                "v",
                "w",
                "x",
                "y",
                "z",
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

            // A far from exhaustive list of determiners
            private List<string> _determiners = new List<string>(new[] {
                "the",
                "a",
                "an",
                "another",
                "this",
                "that",
                "two",
                "all"
            });

            private Dictionary<string, string> pluralDeterminers = new Dictionary<string, string>() {
                {"the", "the_pl"},
                {"a", "some"},
                {"an", "some"},
                {"another", "some_other"},
                {"this", "these"},
                {"that", "those"},
                {"two", "two"},
                {"all", "all"}
            };

            private List<string> _exclude = new List<string>();

            /// <summary>
            /// Only called in one place. Splits on any amount of spaces
            /// "paper sheet" is some kind of special case
            /// </summary>
            /// <param name="sent"></param>
            /// <returns>a list of tokens (as strings) </returns>
    		private string[] SentSplit(string sent) {
    			sent = sent.ToLower().Replace("paper sheet", "paper_sheet");
    			var tokens = new List<string>(Regex.Split(sent, " +"));
    			return tokens.Where(token => !_exclude.Contains(token)).ToArray();
    		}

    		public string NLParse(string rawSent) {
                //No plurals allowed
    			foreach (string plural in shittyPorterStemmer.Keys) {
    				rawSent = rawSent.Replace(plural, shittyPorterStemmer[plural]);
    			}

    			var tokens = SentSplit(rawSent);
    			var form = tokens[0] + "(";
    			var cur = 1;
    			var end = tokens.Length;
    			var lastObj = "";

    			while (cur < end) {
    				if (tokens[cur] == "and") {
    					form += ",";
    					cur++;
    				}
                    // 'in front of X' > in_front(X)
                    // And other such prepositional mappings
    				else if (cur + 2 < end &&
    				         tokens[cur] == "in" && tokens[cur + 1] == "front" && tokens[cur + 2] == "of") {
    					form += ",in_front(";
    					cur += 3;
    				}
                    else if (cur + 1 == end &&
                             tokens[cur] == "forward") {
                        form += ",{1}(front)";
                        cur += 1;
                    }
                    else if (cur + 1 < end &&
    				         tokens[cur] == "left" && tokens[cur + 1] == "of") {
    					form += ",left(";
    					cur += 2;
    				}
                    else if (cur + 1 == end &&
                             tokens[cur] == "left") {
                        form += ",{1}(left)";
                        cur += 1;
                    }
                    else if (cur + 1 < end &&
    				         tokens[cur] == "right" && tokens[cur + 1] == "of") {
    					form += ",right(";
    					cur += 2;
    				}
                    else if (cur + 1 == end &&
                             tokens[cur] == "right") {
                        form += ",{1}(right)";
                        cur += 1;
                    }
                    else if (cur + 1 == end &&
                             tokens[cur] == "back") {
                        form += ",{1}(back)";
                        cur += 1;
                    }
                    else if (cur + 1 < end &&
    				         tokens[cur] == "center" && tokens[cur + 1] == "of") {
    					form += ",center(";
    					cur += 2;
    				}
    				else if (_relations.Contains(tokens[cur])) {
    					if (form.EndsWith("(")) {
    						form += tokens[cur] + "(";
    					}
    					else {
    						if (tokens[cur] == "at" && tokens[cur + 1] == "center") {
    							form += ",center(" + lastObj;
    						}
    						else if (tokens[cur] == "on" && tokens[cur + 1] == "edge") {
    							form += ",edge(" + lastObj;
    						}
    						else {
    							form += "," + tokens[cur] + "(";
    						}
    					}

    					cur += 1;
    				}

                    /// Lots of potential categories.
                    //??? Just "{1}"
    				else if (_relationVars.Contains(tokens[cur])) {
    					form += "," + tokens[cur];
    					cur += 1;
    				}
    				else if (_determiners.Contains(tokens[cur])) {
                        string det = tokens[cur];
                        form += tokens[cur] + "(";
    					cur += ParseNextNP(tokens.Skip(cur + 1).ToArray(), ref form, ref lastObj);
                        List<int> indices = form.FindAllIndicesOf(det);
                        if (cur < tokens.Length) {
                            if (tokens[cur].Split('.').Length > 1) {
                                if (tokens[cur].Split('.')[1] == "pl") {
                                    if (indices.Count > 0) {
                                        form = form.Remove(indices.Last(), det.Length).Insert(indices.Last(), pluralDeterminers[det]);
                                    }
                                }
                            }
                        }
                    }
                    else if (_attribs.Contains(tokens[cur])) {
    					form += tokens[cur] + "(";
    					cur += ParseNextNP(tokens.Skip(cur + 1).ToArray(), ref form, ref lastObj);
    				}
    				else if (_objects.Contains(tokens[cur].Split('.')[0])) {
    					lastObj = tokens[cur].Split('.')[0];
    					form += lastObj;
    					//form = MatchParens(form);
    					cur++;
    				}
    				else if (_objectVars.Contains(tokens[cur])) {
    					lastObj = tokens[cur];
    					form += lastObj;
    					//form = MatchParens(form);
    					cur++;
    				}
                    else if (_anaphorVars.Contains(tokens[cur])) {
                        lastObj = tokens[cur];
                        form += lastObj;
                        //form = MatchParens(form);
                        cur++;
                    }
    				else if (tokens[cur].StartsWith("v@")) {
    					form += "," + tokens[cur].ToUpper();
    					cur++;
    				}
    				else {
    					cur++;
    				}

                    Debug.LogWarning(cur);
                    Debug.LogWarning(form);
                }

    			form = MatchParens(form);
    			//			form += string.Concat(Enumerable.Repeat(")", opens - closes));

    			if (form.EndsWith("()")) {
    				form = form.Replace("()", "");
    			}

    			Debug.Log(form);
    			return form;
    		}

            /// <summary>
            /// Fills in all the parentheses needed to get out to top level 
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
    		private string MatchParens(string input) {
    			for (int i = input.Count(c => c == ')'); i < input.Count(c => c == '('); i++) {
    				input += ")";
    			}

    			return input;
    		}

    		private int ParseNextNP(string[] restOfSent, ref string parsed, ref string lastObj) {
    			var cur = 0;
    			var openParen = 0;
    			var end = restOfSent.Length;
    			while (cur < end) {
    				if (_attribs.Contains(restOfSent[cur])) {
    					// allows only one adjective per a parenthesis level
    					parsed += restOfSent[cur] + "(";
    					openParen++;
    					cur++;
    				}
    				else if (_objects.Contains(restOfSent[cur])) {
    					lastObj = restOfSent[cur];
    					parsed += lastObj;
    					//Debug.Log(parsed);
    					for (var i = 0; i < openParen; i++) {
    						parsed += ")";
    						//Debug.Log(parsed);
    					}

    					parsed += ")";
    					//Debug.Log(parsed);
    					cur++;
    				}
    				else if (_objectVars.Contains(restOfSent[cur])) {
    					lastObj = restOfSent[cur];
    					parsed += lastObj;
    					//Debug.Log(parsed);
    					for (var i = 0; i < openParen; i++) {
    						parsed += ")";
    						//Debug.Log(parsed);
    					}

    					parsed += ")";
    					//Debug.Log(parsed);
    					cur++;
    				}
                    else if (_anaphorVars.Contains(restOfSent[cur])) {
                        lastObj = restOfSent[cur];
                        parsed += lastObj;
                        //Debug.Log(parsed);
                        for (var i = 0; i < openParen; i++) {
                            parsed += ")";
                            //Debug.Log(parsed);
                        }

                        parsed += ")";
                        //Debug.Log(parsed);
                        cur++;
                    }
    				else if (restOfSent[cur] == "and") {
    					parsed += ",";
    					cur++;
    				}
    				else {
    					MatchParens(parsed);
    					break;
    				}
    			}

    			return ++cur;
    		}

#if !UNITY_WEBGL
			public void InitParserService(SocketConnection socketConnection, Type expectedSyntax)
			{
				throw new System.NotImplementedException();
			} 

			public void InitParserService(RESTClient restClient, Type expectedSyntax) {
                throw new System.NotImplementedException();
            }
#endif

            public string ConcludeNLParse() {
                throw new System.NotImplementedException();
            }
        }
    }
}