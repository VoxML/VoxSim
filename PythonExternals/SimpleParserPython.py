__author__ = 'mark'


# using System.Collections.Generic
# using System.Linq
# using System.Text.RegularExpressions
# using UnityEngine

import re
import sys

class INLParser:
    def __init__(self):
        pass

    def NLParse(self, rawSent):
        return rawSent

    def InitParserService(self, address):
        pass


class SimpleParser(INLParser):
    def __init__(self):
        super().__init__()
        self.events = []
        self.events.extend([
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
            "slide"])
        self._objects = []
        self._objects.extend([
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
            "blocks",
            "lid",
            "stack",
            "staircase",
            "pyramid",
            "cork"])

        self._objectVars = []
        self._objectVars.extend([
                "{0}"])

            # /// <summary>
            # /// A super simple mapping of plural to singular. Surprisingly, I'm not the one to name it this.
            # /// To be deleted???
            # /// </summary>
        self.shittyPorterStemmer = {
                "blocks" : "block",
                "balls" : "ball",
                "plates" : "plate",
                "cups" : "cup",
                "discs" : "disc",
                "spoons" : "spoon",
                "forks" : "fork",
                "books" : "book",
                "blackboards" : "blackboard",
                "bottles" : "bottle",
                "grapes" : "grape",
                "apples" : "apple",
                "bananas" : "banana",
                "tables" : "table",
                "bowls" : "bowl",
                "knives" : "knife",
                "pencils" : "pencil",
                "paper sheets" : "paper_sheet",
                "mugs" : "mug",
                "lids" : "lid",
                "stack" : "stack",
                "starcases" : "staircase",
                "pyramids" : "pyramid",
                "corks" : "cork"
            }


        self._relations = [
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
            ]

        self._relationVars = [
                "{1}"
            ]

        self._attribs = [
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
            ]

            #// A far from exhaustive list. of determiners
        self._determiners = [
                "the",
                "a",
                "this",
                "that",
                "two"
            ]

        self._exclude = []

            # /// <summary>
            # /// Only called in one place. Splits on any amount of spaces
            # /// "paper sheet" is some kind of special case
            # /// </summary>
            # /// <param name="sent"></param>
            # /// <returns>a list of tokens (as strings) </returns>
    def SentSplit(self, sent):
        sent = sent.lower().replace("paper sheet", "paper_sheet")
        tokens = re.split(" +", sent)
        return [tok for tok in tokens if tok not in self._exclude]
        #return tokens.Where(token => !_exclude.Contains(token)).ToArray()

    def NLParse(self, rawSent):
        #//No plurals allowed
        for plural in self.shittyPorterStemmer.keys():
            rawSent = rawSent.replace(plural, self.shittyPorterStemmer[plural])
        tokens = self.SentSplit(rawSent)
        form = tokens[0] + "("
        cur = 1
        end = len(tokens)
        lastObj = ""

        while (cur < end):
            if (tokens[cur] == "and"):
                form += ","
                cur += 1

            # // 'in front of X' > in_front(X)
            # // And other such prepositional mappings
            elif (cur + 2 < end and
                     tokens[cur] == "in" and tokens[cur + 1] == "front" and tokens[cur + 2] == "of"):
                form += ",in_front("
                cur += 3

            elif (cur + 1 < end and
                     tokens[cur] == "left" and tokens[cur + 1] == "of"):
                form += ",left("
                cur += 2

            elif (cur + 1 < end and
                     tokens[cur] == "right" and tokens[cur + 1] == "of"):
                form += ",right("
                cur += 2

            elif (cur + 1 < end and
                     tokens[cur] == "center" and tokens[cur + 1] == "of"):
                form += ",center("
                cur += 2

            elif (tokens[cur] in self._relations):
                if (form[-1] == "("):
                    form += tokens[cur] + "("

                else:
                    if (tokens[cur] == "at" and tokens[cur + 1] == "center"):
                        form += ",center(" + lastObj

                    elif (tokens[cur] == "on" and tokens[cur + 1] == "edge"):
                        form += ",edge(" + lastObj

                    else:
                        form += "," + tokens[cur] + "("

                cur += 1


                    # /// Lots of potential categories.
                    # //??? Just "{1}"
            elif (tokens[cur] in self._relationVars):
                form += "," + tokens[cur]
                cur += 1

            elif (tokens[cur] in self._determiners):
                form += tokens[cur] + "("
                #cur += self.ParseNextNP(tokens.Skip(cur + 1).ToArray(), form, lastObj)
                cur += self.ParseNextNP(tokens[cur+1:], form, lastObj)

            elif (tokens[cur] in self._attribs):
                form += tokens[cur] + "("
                cur += self.ParseNextNP(tokens[cur+1:], form, lastObj)

            elif (tokens[cur] in self._objects):
                lastObj = tokens[cur]
                form += lastObj
                #//form = MatchParens(form)
                cur += 1

            elif (tokens[cur] in self._objectVars):
                lastObj = tokens[cur]
                form += lastObj
                #//form = MatchParens(form)
                cur += 1

            elif (tokens[cur][:3] == "v@"):
                form += "," + tokens[cur].upper()
                cur += 1

            else:
                cur += 1


            # Debug.LogWarning(cur)
            # Debug.LogWarning(form)


        form = self.MatchParens(form)
        #//            form += string.Concat(Enumerable.Repeat(")", opens - closes))

        if (form[-2:] == "()"):
            form = form.replace("()", "")


        #Debug.Log(form)
        return form

            # /// <summary>
            # /// Fills in all the parentheses needed to get out to top level
            # /// </summary>
            # /// <param name="input"></param>
            # /// <returns></returns>
    def MatchParens(self, input):
        i = input.count('(') - input.count(')')
        input += ")" * i
        return input


    def ParseNextNP(self, restOfSent, parsed, lastObj):
        cur = 0
        openParen = 0
        end = len(restOfSent)
        while (cur < end):
            if (restOfSent[cur] in self._attribs):
                #// allows only one adjective per a parenthesis level
                parsed += restOfSent[cur] + "("
                openParen+=1
                cur+=1

            elif (restOfSent[cur] in self._objects):
                lastObj = restOfSent[cur]
                parsed += lastObj
                #//Debug.Log(parsed)
                parsed += ")" * openParen
                #//Debug.Log(parsed)
                cur+=1

            elif (restOfSent[cur] in self._objectVars):
                lastObj = restOfSent[cur]
                parsed += lastObj
                #//Debug.Log(parsed)
                parsed += ")" * openParen
                cur+=1

            elif (restOfSent[cur] == "and"):
                parsed += ","
                cur+=1
            else:
                self.MatchParens(parsed)
                break


        cur += 1
        return(cur)


    def InitParserService(self,address):
        pass


if __name__ == "__main__":
    parser = SimpleParser()
    if len(sys.argv) > 2:
        test_sent = sys.argv[2]
    else:
        test_sent = "put knife on plate"
    result = parser.NLParse(test_sent)
    print(result)
    #result = parser.ParseNextNP(test_sent)
    #print(result)